using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/*
Арена Мамы паука — геометрия боя в сцене и сам бой как событие.

Геометрия: стены (у каждой — точка у подножия и точки на стене снизу вверх, по одной на уровень
высоты; под каждый уровень в арене стоят платформы) и место на земле для фазы щита.
Паучиха ползает только по маршрутам из этих точек: вдоль стены — напрямую, на другую стену
и на землю — через подножия стен.

Бой: игрок вошёл в fightArea — включаются объекты enableDuringFight (ворота, камера арены),
появляется полоска здоровья, босс просыпается. Босс погиб — всё обратно, ворота открыты,
а объекты enableAfterVictory (в Level 1 — триггер финиша уровня) включаются.
Босс лежит дочерним объектом арены (или указан в поле boss).
*/
public class SpiderMotherArena : MonoBehaviour
{
    [Tooltip("Босс арены. Пусто = ищется среди дочерних объектов")]
    [SerializeField] private SpiderMotherController boss;

    [Header("Геометрия")]
    public List<SpiderMotherWall> walls = new();

    [Tooltip("Где паучиха стоит на земле, пока держит щит и рожает паучков")]
    public Transform groundSpot;

    [Tooltip("С какой стены и точки (индексы с 0) паучиха начинает бой")]
    [Min(0)] public int startWall = 0;
    [Min(0)] public int startPerch = 1;

    [Header("Бой")]
    [Tooltip("Прямоугольник арены относительно её объекта: игрок вошёл — бой начался")]
    public Vector2 fightAreaOffset = new(0f, 8f);
    public Vector2 fightAreaSize = new(36f, 18f);

    [Tooltip("Включаются на время боя: ворота, камера арены…")]
    public List<GameObject> enableDuringFight = new();

    [Tooltip("Выключаются на время боя")]
    public List<GameObject> disableDuringFight = new();

    [Tooltip("Выключены, пока босс жив, и включаются после победы: финиш уровня, выход, награда…")]
    public List<GameObject> enableAfterVictory = new();

    public bool showHealthBar = true;

    private Transform player;
    private bool fightStarted;
    private bool fightOver;
    private BossHealthBarUI healthBar;

    public SpiderMotherController Boss => boss;
    public bool FightActive => fightStarted && !fightOver;
    public SpiderSpot StartSpot => ClampSpot(new SpiderSpot(startWall, startPerch));

    private void Awake()
    {
        if (boss == null) boss = GetComponentInChildren<SpiderMotherController>();
        SetFightObjects(false);
        SetVictoryObjects(false);
    }

    private void Start()
    {
        if (boss == null)
        {
            Debug.LogError($"[SpiderMotherArena] {name}: босс не найден — положи SpiderMotherController дочерним объектом или в поле Boss.");
            enabled = false;
            return;
        }
        if (boss.Health != null) boss.Health.Died += OnBossDied;
    }

    private void OnDestroy()
    {
        if (boss != null && boss.Health != null) boss.Health.Died -= OnBossDied;
        if (healthBar != null) healthBar.Hide();
    }

    private void Update()
    {
        if (fightStarted || fightOver) return;

        if (player == null)
        {
            GameObject playerGo = GameObject.FindWithTag("Player");
            if (playerGo == null) return;
            player = playerGo.transform;
        }

        if (FightArea.Contains(player.position))
        {
            StartFight();
        }
    }

    private Rect FightArea => new((Vector2)transform.position + fightAreaOffset - fightAreaSize * 0.5f, fightAreaSize);

    public void StartFight()
    {
        if (fightStarted || boss == null || boss.Health == null || !boss.Health.IsAlive) return;

        fightStarted = true;
        SetFightObjects(true);
        if (showHealthBar)
        {
            string title = boss.Boss != null ? boss.Boss.bossTitle : boss.name;
            healthBar = BossHealthBarUI.Show(title, boss.Health, boss.Shield);
        }
        boss.BeginFight();
    }

    private void OnBossDied()
    {
        fightOver = true;
        SetFightObjects(false);
        SetVictoryObjects(true);
        if (healthBar != null)
        {
            healthBar.Hide();
            healthBar = null;
        }
    }

    private void SetFightObjects(bool fighting)
    {
        foreach (GameObject go in enableDuringFight)
        {
            if (go != null) go.SetActive(fighting);
        }
        foreach (GameObject go in disableDuringFight)
        {
            if (go != null) go.SetActive(!fighting);
        }
    }

    private void SetVictoryObjects(bool won)
    {
        foreach (GameObject go in enableAfterVictory)
        {
            if (go != null) go.SetActive(won);
        }
    }

    #region Точки и маршруты
    public bool IsValid(SpiderSpot spot) =>
        spot.IsGround
            ? groundSpot != null
            : spot.Wall < walls.Count && walls[spot.Wall] != null && spot.Perch < walls[spot.Wall].perches.Count
              && walls[spot.Wall].perches[spot.Perch] != null;

    public Vector2 PositionOf(SpiderSpot spot)
    {
        if (!IsValid(spot)) return transform.position;
        return spot.IsGround ? groundSpot.position : walls[spot.Wall].perches[spot.Perch].position;
    }

    // Сторона, к которой прижата паучиха в этой точке: +1 правая стена, -1 левая, 0 земля.
    public int SideOf(SpiderSpot spot) => spot.IsGround || !IsValid(spot) ? 0 : (int)walls[spot.Wall].side;

    /*
    Маршрут от точки from до точки to, без стартовой точки.
    Вдоль одной стены — сразу в цель; на другую стену и на землю — через подножия стен.
    У каждой точки маршрута — сторона стены, к которой паучиха прижмётся на вертикальном участке.
    */
    public void BuildRoute(SpiderSpot from, SpiderSpot to, List<SpiderRoutePoint> route)
    {
        route.Clear();
        if (!IsValid(to) || from.Equals(to)) return;

        bool sameWall = !from.IsGround && !to.IsGround && from.Wall == to.Wall;
        if (sameWall)
        {
            route.Add(new SpiderRoutePoint(PositionOf(to), SideOf(to)));
            return;
        }

        // Слезть со своей стены.
        if (!from.IsGround && IsValid(from) && walls[from.Wall].foot != null)
        {
            route.Add(new SpiderRoutePoint(walls[from.Wall].foot.position, SideOf(from)));
        }

        if (to.IsGround)
        {
            route.Add(new SpiderRoutePoint(groundSpot.position, 0));
            return;
        }

        // Дойти по земле до чужой стены и залезть.
        SpiderMotherWall target = walls[to.Wall];
        if (target.foot != null)
        {
            route.Add(new SpiderRoutePoint(target.foot.position, (int)target.side));
        }
        route.Add(new SpiderRoutePoint(PositionOf(to), SideOf(to)));
    }

    // Случайная точка на стене, кроме current. С шансом otherWallChance — на другой стене.
    public SpiderSpot RandomPerch(SpiderSpot current, float otherWallChance)
    {
        List<SpiderSpot> sameWall = new();
        List<SpiderSpot> otherWalls = new();
        for (int w = 0; w < walls.Count; w++)
        {
            if (walls[w] == null) continue;
            for (int p = 0; p < walls[w].perches.Count; p++)
            {
                SpiderSpot spot = new(w, p);
                if (!IsValid(spot) || spot.Equals(current)) continue;
                (w == current.Wall ? sameWall : otherWalls).Add(spot);
            }
        }

        bool goOther = otherWalls.Count > 0 && (sameWall.Count == 0 || Random.value < otherWallChance);
        List<SpiderSpot> pool = goOther ? otherWalls : sameWall;
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : current;
    }

    private SpiderSpot ClampSpot(SpiderSpot spot)
    {
        if (IsValid(spot)) return spot;
        return RandomPerch(SpiderSpot.Ground, 1f);
    }
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Rect area = FightArea;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.35f);
        Gizmos.DrawWireCube(area.center, area.size);

        foreach (SpiderMotherWall wall in walls)
        {
            if (wall == null) continue;
            Gizmos.color = new Color(0.8f, 0.4f, 1f, 0.9f);
            Vector3 previous = wall.foot != null ? wall.foot.position : Vector3.zero;
            bool hasPrevious = wall.foot != null;
            if (hasPrevious) Gizmos.DrawWireCube(previous, Vector3.one * 0.5f);

            foreach (Transform perch in wall.perches)
            {
                if (perch == null) continue;
                Gizmos.DrawWireSphere(perch.position, 0.6f);
                // Куда прижата спина паучихи: стрелка в стену.
                Gizmos.DrawLine(perch.position, perch.position + Vector3.right * ((int)wall.side * 0.9f));
                if (hasPrevious) Gizmos.DrawLine(previous, perch.position);
                previous = perch.position;
                hasPrevious = true;
            }

            if (wall.foot != null && groundSpot != null)
            {
                Gizmos.color = new Color(0.8f, 0.4f, 1f, 0.35f);
                Gizmos.DrawLine(wall.foot.position, groundSpot.position);
            }
        }

        if (groundSpot != null)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.8f);
            Gizmos.DrawWireSphere(groundSpot.position, 1.2f);
        }
    }
#endif
}

public enum SpiderWallSide
{
    Left = -1,
    Right = 1
}

[Serializable]
public class SpiderMotherWall
{
    [Tooltip("Подпись для читаемости списка в инспекторе")]
    public string label;

    [Tooltip("Где стена: справа от арены (паучиха смотрит влево) или слева")]
    public SpiderWallSide side = SpiderWallSide.Right;

    [Tooltip("Точка у подножия стены на земле: здесь паучиха слезает со стены и залезает на неё")]
    public Transform foot;

    [Tooltip("Точки на стене снизу вверх — по одной на уровень высоты; под каждый уровень в арене есть платформа")]
    public List<Transform> perches = new();
}

// Место паучихи: точка Perch на стене Wall или земля.
public readonly struct SpiderSpot : IEquatable<SpiderSpot>
{
    public readonly int Wall;
    public readonly int Perch;

    public SpiderSpot(int wall, int perch)
    {
        Wall = wall;
        Perch = perch;
    }

    public static SpiderSpot Ground => new(-1, -1);
    public bool IsGround => Wall < 0;

    public bool Equals(SpiderSpot other) => Wall == other.Wall && Perch == other.Perch;
    public override bool Equals(object obj) => obj is SpiderSpot other && Equals(other);
    public override int GetHashCode() => Wall * 397 ^ Perch;
}

// Точка маршрута: куда ползти и к какой стене прижаться (+1/-1; 0 — земля).
public readonly struct SpiderRoutePoint
{
    public readonly Vector2 Position;
    public readonly int Side;

    public SpiderRoutePoint(Vector2 position, int side)
    {
        Position = position;
        Side = side;
    }
}
