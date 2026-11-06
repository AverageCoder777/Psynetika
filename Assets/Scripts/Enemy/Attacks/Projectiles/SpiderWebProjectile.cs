using System.Collections.Generic;
using UnityEngine;

/*
Плевок паутиной. Летит по прямой туда, где был игрок в момент плевка, — от неё уходят шагом
или прыжком, а перекат проскакивает сквозь неё (если rollDodges).

Попала — опутывает: игрок уходит в GrabbedState и проходит QTE, как при захвате нейрончика.
Захватчик (IPlayerGrabber) — сама паутина: висит на игроке, пока захват не кончится, и исчезает.

Столкновения считаются CircleCast'ом по шагу, а не физикой: снаряду не нужен ни Rigidbody2D,
ни коллайдер, и он не путается в триггерах сенсоров. Односторонние платформы паутина пролетает:
в obstacleMask их нет.
*/
public class SpiderWebProjectile : MonoBehaviour, IPlayerGrabber
{
    private static readonly List<RaycastHit2D> HitBuffer = new(8);

    [Tooltip("Радиус паутины для попаданий")]
    [SerializeField, Min(0.05f)] private float radius = 0.35f;

    [Tooltip("Обо что паутина разбивается: пол и стены. Платформы не включать — сквозь них она пролетает")]
    [SerializeField] private LayerMask obstacleMask;

    [Tooltip("Вращение спрайта в полёте, град/сек")]
    [SerializeField] private float spinSpeed = 540f;

    [Tooltip("Насколько паутина больше на опутанном игроке")]
    [SerializeField, Min(0.1f)] private float stuckScale = 1.8f;

    private Vector2 velocity;
    private float dieAt;
    private QteSettings qte;
    private bool rollDodges;
    private bool launched;
    private PlayerController stuckTo;
    private Collider2D stuckCollider;
    private SpriteRenderer sprite;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    public void Launch(Vector2 flightVelocity, float lifetime, QteSettings qteSettings, bool dodgeByRoll)
    {
        velocity = flightVelocity;
        dieAt = Time.time + Mathf.Max(0.1f, lifetime);
        qte = qteSettings;
        rollDodges = dodgeByRoll;
        launched = true;
    }

    private void FixedUpdate()
    {
        if (!launched || stuckTo != null) return;

        if (Time.time >= dieAt)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 position = transform.position;
        Vector2 step = velocity * Time.fixedDeltaTime;
        float distance = step.magnitude;
        if (distance > 0f && TryHit(position, step / distance, distance))
        {
            return;
        }

        transform.position = position + step;
        transform.Rotate(0f, 0f, spinSpeed * Time.fixedDeltaTime);
    }

    private void LateUpdate()
    {
        if (stuckTo == null) return;

        // Опутанный игрок может падать (схватили в прыжке) — паутина висит на нём.
        transform.position = stuckCollider != null ? stuckCollider.bounds.center : stuckTo.transform.position;
    }

    // true — снаряд закончил полёт (опутал игрока или разбился).
    private bool TryHit(Vector2 origin, Vector2 direction, float distance)
    {
        int playerMask = LayerMask.GetMask("Player");
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = playerMask | obstacleMask.value
        };

        int count = Physics2D.CircleCast(origin, radius, direction, filter, HitBuffer, distance);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = HitBuffer[i].collider;
            if (hit == null) continue;

            if ((playerMask & (1 << hit.gameObject.layer)) == 0)
            {
                Destroy(gameObject);
                return true;
            }

            PlayerController player = hit.GetComponentInParent<PlayerController>();
            if (player == null) continue;

            // Перекат проскакивает сквозь паутину: летим дальше, как будто игрока нет.
            if (rollDodges && player.PlayerSM != null && player.PlayerSM.CurrentPlayerState == player.RollingState)
            {
                continue;
            }

            if (player.TryGrab(this, qte))
            {
                StickTo(player);
            }
            else
            {
                // Игрок уже опутан или мёртв — паутина просто разбивается.
                Destroy(gameObject);
            }
            return true;
        }
        return false;
    }

    private void StickTo(PlayerController player)
    {
        stuckTo = player;
        stuckCollider = player.GetComponent<Collider2D>();
        velocity = Vector2.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale *= stuckScale;

        // Поверх героя: его спрайты сидят в SortingGroup на корне игрока.
        UnityEngine.Rendering.SortingGroup group = player.GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (sprite != null && group != null)
        {
            sprite.sortingLayerID = group.sortingLayerID;
            sprite.sortingOrder = group.sortingOrder + 1;
        }
        LateUpdate();
    }

    void IPlayerGrabber.OnGrabEnded(GrabOutcome outcome)
    {
        stuckTo = null;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Паутину уничтожили посреди захвата (смена сцены) — игрок не должен остаться опутанным.
        if (stuckTo != null)
        {
            PlayerController player = stuckTo;
            stuckTo = null;
            player.ReleaseGrab(this);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
