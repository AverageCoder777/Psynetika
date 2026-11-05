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
    private readonly HashSet<IAbilityTarget> piercedTargets = new();
    private Vector2 spawnPosition;

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
        piercedTargets.Clear();
        spawnPosition = transform.position;

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
        // Гравитацию задаёт определение, а не префаб: один префаб из пула может служить и пулей, и гранатой.
        rb.gravityScale = definition.gravityScale;
        rb.linearVelocity = new Vector2(direction * definition.speed, definition.launchUpSpeed);
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

        if (this == null)
        {
            return;
        }

        Expire().Forget();
    }

    private async UniTaskVoid Expire()
    {
        if (resolved)
        {
            return;
        }

        resolved = true;
        rb.linearVelocity = Vector2.zero;
        await RunEventNodes(definition.onExpire, null);
        Despawn();
    }

    private void FixedUpdate()
    {
        if (resolved || definition == null || definition.maxDistance <= 0f)
        {
            return;
        }

        if (((Vector2)transform.position - spawnPosition).sqrMagnitude >= definition.maxDistance * definition.maxDistance)
        {
            Expire().Forget();
        }
    }

    // Пробивающий снаряд с триггер-коллайдером: цели не толкают его, а onHit срабатывает по каждой один раз.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (resolved || definition == null || !definition.pierceTargets)
        {
            return;
        }

        // Чужие триггеры (зоны агро врагов, интерактивы) — не тела, сквозь них летим молча.
        if (other.isTrigger)
        {
            return;
        }

        IAbilityTarget target = other.GetComponentInParent<IAbilityTarget>();
        if (target == null)
        {
            if (definition.stopOnObstacles)
            {
                Expire().Forget();
            }
            return;
        }

        if (IsAlly(target) || !target.IsAlive || !piercedTargets.Add(target))
        {
            return;
        }

        RunEventNodes(definition.onHit, target).Forget();
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
            // Брошенный по дуге снаряд сохраняет вертикальную скорость, пуля — летит строго горизонтально.
            float verticalSpeed = definition.gravityScale > 0f ? rb.linearVelocity.y : 0f;
            rb.linearVelocity = new Vector2(direction * definition.speed, verticalSpeed);
            return;
        }

        // Пробивающий снаряд, у которого коллайдер всё же не триггер: ведём себя как с союзником, но бьём цель.
        if (target != null && definition.pierceTargets)
        {
            IgnoreCollisionsWith(target.Transform);
            float verticalSpeed = definition.gravityScale > 0f ? rb.linearVelocity.y : 0f;
            rb.linearVelocity = new Vector2(direction * definition.speed, verticalSpeed);
            if (target.IsAlive && piercedTargets.Add(target))
            {
                RunEventNodes(definition.onHit, target).Forget();
            }
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
