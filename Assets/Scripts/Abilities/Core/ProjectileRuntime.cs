using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ProjectileRuntime : MonoBehaviour
{
    private Rigidbody2D rb;
    private ProjectileDefinition definition;
    private AbilityContext originContext;
    private float direction;
    private bool resolved;
    private CancellationTokenSource lifeCts;
    private Action<ProjectileRuntime> releaseToPool;
    private readonly List<(Collider2D projectile, Collider2D other)> ignoredPairs = new();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(ProjectileDefinition projectileDefinition, AbilityContext context,
        float facingDirection, Action<ProjectileRuntime> releaseCallback = null)
    {
        definition = projectileDefinition;
        originContext = context;
        releaseToPool = releaseCallback;
        direction = Mathf.Approximately(facingDirection, 0f) ? 1f : Mathf.Sign(facingDirection);
        resolved = false;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        // Зеркалим весь префаб по направлению полёта — флипает спрайт + любые дочерние VFX/трейлы.
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;

        // Игнорируем коллизии с кастером — иначе пуля резолвится сразу о коллайдер игрока.
        if (originContext?.Owner?.Transform != null)
        {
            IgnoreCollisionsWith(originContext.Owner.Transform);
        }

        lifeCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        rb.linearVelocity = new Vector2(direction * definition.speed, 0f);
        RunEventNodes(definition.onSpawn, null).Forget();
        ExpireAfterLifetime(lifeCts.Token).Forget();
    }

    private void IgnoreCollisionsWith(Transform root)
    {
        Collider2D[] projectileColliders = GetComponentsInChildren<Collider2D>();
        Collider2D[] otherColliders = root.GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < projectileColliders.Length; i++)
        {
            Collider2D pc = projectileColliders[i];
            if (pc == null) continue;
            for (int j = 0; j < otherColliders.Length; j++)
            {
                Collider2D oc = otherColliders[j];
                if (oc == null) continue;
                Physics2D.IgnoreCollision(pc, oc, true);
                ignoredPairs.Add((pc, oc));
            }
        }
    }

    // Возвращает коллизии в исходное состояние перед возвратом в пул: у следующего
    // владельца снаряда будет другой кастер и другие союзники.
    private void RestoreIgnoredCollisions()
    {
        for (int i = 0; i < ignoredPairs.Count; i++)
        {
            (Collider2D pc, Collider2D oc) = ignoredPairs[i];
            if (pc != null && oc != null)
            {
                Physics2D.IgnoreCollision(pc, oc, false);
            }
        }
        ignoredPairs.Clear();
    }

    private async UniTaskVoid ExpireAfterLifetime(CancellationToken token)
    {
        if (definition == null)
        {
            return;
        }

        int delayMs = Mathf.Max(1, Mathf.RoundToInt(definition.lifetime * 1000f));
        try
        {
            await UniTask.Delay(delayMs, cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (this == null || resolved)
        {
            return;
        }

        resolved = true;
        await RunEventNodes(definition.onExpire, null);
        Despawn();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (resolved || definition == null)
        {
            return;
        }

        IAbilityTarget target = other.collider.GetComponentInParent<IAbilityTarget>();
        if (target != null && IsAlly(target))
        {
            // Союзников пролетаем насквозь, а не резолвимся об них.
            IgnoreCollisionsWith(target.Transform);
            rb.linearVelocity = new Vector2(direction * definition.speed, 0f);
            return;
        }

        ResolveHit(target).Forget();
    }

    private bool IsAlly(IAbilityTarget target)
    {
        IAbilityCaster owner = originContext?.Owner;
        return owner != null && target.Team != Team.Neutral && target.Team == owner.Team;
    }

    private async UniTaskVoid ResolveHit(IAbilityTarget target)
    {
        resolved = true;
        rb.linearVelocity = Vector2.zero;
        await RunEventNodes(definition.onHit, target);
        Despawn();
    }

    private async UniTask RunEventNodes(List<AbilityNode> nodes, IAbilityTarget target)
    {
        if (nodes == null || nodes.Count == 0 || originContext == null)
        {
            return;
        }

        // Один контекст на событие: Blackboard общий с кастом, породившим снаряд,
        // а cleanup-действия — свои и отрабатывают сразу после списка нод.
        AbilityContext ctx = new AbilityContext
        {
            Owner = originContext.Owner,
            ResolvedTarget = target,
            AimPosition = transform.position,
            Direction = direction,
            Definition = originContext.Definition,
            Instance = originContext.Instance,
            Services = originContext.Services,
            Token = lifeCts != null ? lifeCts.Token : destroyCancellationToken,
            Blackboard = originContext.Blackboard
        };

        try
        {
            await AbilityNodeList.Run(nodes, ctx);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            ctx.RunCleanups();
        }
    }

    private void Despawn()
    {
        if (this == null)
        {
            return;
        }

        lifeCts?.Cancel();
        lifeCts?.Dispose();
        lifeCts = null;
        RestoreIgnoredCollisions();
        rb.linearVelocity = Vector2.zero;

        if (releaseToPool != null)
        {
            releaseToPool(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        lifeCts?.Cancel();
        lifeCts?.Dispose();
        lifeCts = null;
    }
}
