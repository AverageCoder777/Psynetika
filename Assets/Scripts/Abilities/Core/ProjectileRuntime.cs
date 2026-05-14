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
    private CancellationToken destroyToken;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        destroyToken = this.GetCancellationTokenOnDestroy();
    }

    public void Initialize(ProjectileDefinition projectileDefinition, AbilityContext context, float facingDirection)
    {
        definition = projectileDefinition;
        originContext = context;
        direction = Mathf.Approximately(facingDirection, 0f) ? 1f : Mathf.Sign(facingDirection);

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        rb.linearVelocity = new Vector2(direction * definition.speed, 0f);
        ExecuteNodeList(definition.onSpawn, null).Forget();
        ExpireAfterLifetime().Forget();
    }

    private async UniTaskVoid ExpireAfterLifetime()
    {
        if (definition == null)
        {
            return;
        }

        int delayMs = Mathf.Max(1, Mathf.RoundToInt(definition.lifetime * 1000f));
        await UniTask.Delay(delayMs, cancellationToken: destroyToken);

        if (this == null || resolved)
        {
            return;
        }

        resolved = true;
        await ExecuteNodeList(definition.onExpire, null);
        if (this != null)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (resolved || definition == null)
        {
            return;
        }

        IAbilityTarget target = other.gameObject.GetComponent<IAbilityTarget>();
        ResolveHit(target).Forget();
    }

    private async UniTaskVoid ResolveHit(IAbilityTarget target)
    {
        resolved = true;
        await ExecuteNodeList(definition.onHit, target);
        if (this != null)
        {
            Destroy(gameObject);
        }
    }

    private async UniTask ExecuteNodeList(System.Collections.Generic.List<AbilityNode> nodes, IAbilityTarget target)
    {
        if (nodes == null || nodes.Count == 0)
        {
            return;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            AbilityNode node = nodes[i];
            if (node == null)
            {
                continue;
            }

            AbilityContext context = new AbilityContext
            {
                Owner = originContext.Owner,
                ResolvedTarget = target,
                AimPosition = transform.position,
                Direction = direction,
                Definition = originContext.Definition,
                Instance = originContext.Instance,
                Services = originContext.Services,
                Token = destroyToken
            };

            NodeResult result = await node.Execute(context);
            if (result != NodeResult.Success)
            {
                break;
            }
        }
    }
}
