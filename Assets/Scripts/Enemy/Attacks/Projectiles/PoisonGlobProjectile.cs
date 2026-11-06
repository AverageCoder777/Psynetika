using System;
using System.Collections.Generic;
using UnityEngine;

/*
Ядовитый плевок: летит дугой в заранее выбранную точку на земле (туда, где стоял игрок) и
растекается там лужей. Задел игрока по дороге — растекается прямо под ним.
От плевка уворачиваются, просто уходя с точки приземления.

Траектория считается вручную (парабола по времени), а попадание по игроку — OverlapCircle:
снаряду не нужен ни Rigidbody2D, ни коллайдер.
*/
public class PoisonGlobProjectile : MonoBehaviour
{
    private static readonly List<Collider2D> HitBuffer = new(4);

    [Tooltip("Радиус плевка для попаданий по игроку")]
    [SerializeField, Min(0.05f)] private float radius = 0.3f;

    private Vector2 start;
    private Vector2 landing;
    private float arcHeight;
    private float duration;
    private float elapsed;
    private LayerMask groundMask;
    private Action<Vector2> onLand;
    private bool launched;

    /*
    landingPoint — точка на поверхности, куда плевок упадёт; flightTime — время полёта.
    groundMask нужен, чтобы при попадании в игрока найти землю под ним.
    landed получает точку поверхности, где растекается лужа.
    */
    public void Launch(Vector2 landingPoint, float flightTime, float arc, LayerMask ground, Action<Vector2> landed)
    {
        start = transform.position;
        landing = landingPoint;
        duration = Mathf.Max(0.05f, flightTime);
        arcHeight = arc;
        groundMask = ground;
        onLand = landed;
        elapsed = 0f;
        launched = true;
    }

    private void FixedUpdate()
    {
        if (!launched) return;

        elapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        Vector2 position = Vector2.Lerp(start, landing, t) + Vector2.up * (arcHeight * 4f * t * (1f - t));
        transform.position = position;

        if (t >= 1f)
        {
            Land(landing);
            return;
        }

        if (TryHitPlayer(position, out Collider2D player))
        {
            Land(FindGroundBelow(player.bounds.center, player.bounds.min.y));
        }
    }

    private bool TryHitPlayer(Vector2 position, out Collider2D player)
    {
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Player")
        };

        int count = Physics2D.OverlapCircle(position, radius, filter, HitBuffer);
        player = count > 0 ? HitBuffer[0] : null;
        return player != null;
    }

    private Vector2 FindGroundBelow(Vector2 from, float fallbackY)
    {
        RaycastHit2D hit = Physics2D.Raycast(from, Vector2.down, 30f, groundMask);
        return hit.collider != null ? hit.point : new Vector2(from.x, fallbackY);
    }

    private void Land(Vector2 point)
    {
        launched = false;
        onLand?.Invoke(point);
        Destroy(gameObject);
    }
}
