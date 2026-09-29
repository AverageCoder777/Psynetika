using UnityEngine;

/*
Территория конкретного врага в сцене: прямоугольник, который растягивается мышкой в Scene View.

  • Игрок внутри зоны — враг его замечает. Зона заменяет обычную зону агро этого врага
    (радиус followRange или триггер на префабе); зона удара остаётся прежней.
  • Враг сам не выходит за границу зоны — ни в погоне, ни в патруле, ни по дороге домой.
  • Игрок вышел из зоны — враг стоит у границы loseDelay секунд, потом теряет его.

Как и маршрут патруля, зона хранится смещением от врага: в редакторе едет вместе с ним,
в игре отсчитывается от точки спавна.
*/
[RequireComponent(typeof(EnemyController))]
public class EnemyAggroZone : MonoBehaviour
{
    [Tooltip("Центр зоны относительно врага")]
    public Vector2 center = new Vector2(0f, 1f);

    [Tooltip("Ширина и высота зоны")]
    public Vector2 size = new Vector2(12f, 4f);

    [Tooltip("Сколько секунд враг ждёт у границы после того, как игрок вышел из зоны")]
    [Min(0f)] public float loseDelay = 1f;

    private EnemyMovement movement;

    // В игре — от точки спавна (сам враг уже ушёл), в редакторе — от текущей позиции.
    public Vector2 Origin
    {
        get
        {
            if (Application.isPlaying)
            {
                if (movement == null) movement = GetComponent<EnemyMovement>();
                if (movement != null) return movement.SpawnPosition;
            }
            return transform.position;
        }
    }

    public Rect WorldRect
    {
        get
        {
            Vector2 absSize = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
            return new Rect(Origin + center - absSize * 0.5f, absSize);
        }
    }

    public bool Contains(Vector2 point) => WorldRect.Contains(point);

    // Ближайшая к point точка внутри зоны.
    public Vector2 Clamp(Vector2 point)
    {
        Rect rect = WorldRect;
        return new Vector2(Mathf.Clamp(point.x, rect.xMin, rect.xMax), Mathf.Clamp(point.y, rect.yMin, rect.yMax));
    }

#if UNITY_EDITOR
    // Зона видна и у невыделенных врагов (бледно) — удобно смотреть территории всего уровня.
    private void OnDrawGizmos() => DrawZone(0.3f);

    private void OnDrawGizmosSelected() => DrawZone(1f);

    private void DrawZone(float alpha)
    {
        Rect rect = WorldRect;
        Gizmos.color = new Color(1f, 0.45f, 0.2f, alpha);
        Gizmos.DrawWireCube(rect.center, rect.size);
    }
#endif
}
