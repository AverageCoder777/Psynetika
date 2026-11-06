using System;
using UnityEngine;

/*
Щит-паутина Мамы паука: дочерний объект босса с коллайдером на слое Enemy.
Пули и удары игрока попадают в щит как во врага (IAbilityTarget, команда Enemy), а сам босс под
щитом неуязвим (EnemyHealth.IsInvulnerable). Щит разбит — событие Broken, босс уходит в стан.

Сквозь щит игрок проходит: его физический коллайдер исключает слой Player, иначе на куполе можно стоять.
*/
public class SpiderWebShield : MonoBehaviour, IAbilityTarget
{
    public event Action Broken;
    public event Action<int, int> HpChanged;

    [SerializeField] private Color hitColor = new(1f, 0.55f, 0.55f, 1f);
    [SerializeField, Min(0.01f)] private float hitFlashTime = 0.1f;

    [Tooltip("Пульсация прозрачности поднятого щита: амплитуда и частота")]
    [SerializeField, Range(0f, 1f)] private float pulseAmplitude = 0.15f;
    [SerializeField, Min(0f)] private float pulseRate = 1.5f;

    private SpriteRenderer sprite;
    private Color baseColor = Color.white;
    private float flashUntil;
    private int hp;
    private int maxHp = 1;

    public int CurrentHp => hp;
    public int MaxHp => maxHp;
    public bool IsUp => hp > 0 && gameObject.activeSelf;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) baseColor = sprite.color;

        int playerMask = LayerMask.GetMask("Player");
        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            if (!col.isTrigger)
            {
                col.excludeLayers = col.excludeLayers.value | playerMask;
            }
        }
    }

    public void Raise(int hitPoints)
    {
        maxHp = Mathf.Max(1, hitPoints);
        hp = maxHp;
        gameObject.SetActive(true);
        HpChanged?.Invoke(hp, maxHp);
    }

    // Снять щит без события Broken (босс сам ушёл с земли или погиб).
    public void Drop()
    {
        hp = 0;
        gameObject.SetActive(false);
        HpChanged?.Invoke(hp, maxHp);
    }

    private void Update()
    {
        if (sprite == null) return;

        if (Time.time < flashUntil)
        {
            sprite.color = hitColor;
            return;
        }

        float alpha = baseColor.a * (1f - pulseAmplitude + pulseAmplitude * Mathf.Sin(Time.time * pulseRate * Mathf.PI * 2f));
        sprite.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
    }

    Transform IAbilityTarget.Transform => transform;
    bool IAbilityTarget.IsAlive => IsUp;
    Team IAbilityTarget.Team => Team.Enemy;

    // Статусы щиту не страшны: только урон.
    void IAbilityTarget.ReceiveDamage(DamageEvent ev)
    {
        if (!IsUp) return;

        int amount = Mathf.Max(0, Mathf.RoundToInt(ev.Amount));
        if (amount <= 0) return;

        int before = hp;
        hp = Mathf.Max(0, hp - amount);
        flashUntil = Time.time + hitFlashTime;
        HpChanged?.Invoke(hp, maxHp);
        DamageHelper.NotifyDamageDealt(ev, before - hp);

        if (hp <= 0)
        {
            gameObject.SetActive(false);
            Broken?.Invoke();
        }
    }
}
