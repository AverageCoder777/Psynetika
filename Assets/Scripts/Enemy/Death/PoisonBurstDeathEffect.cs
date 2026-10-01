using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

/*
Взрыв тела после смерти: через случайную задержку тело разрывает, и игрок в радиусе взрыва
получает отравление (и, если задан, мгновенный урон). Перед взрывом тело мигает предупреждающим
цветом, чтобы игрок успел отойти.
*/
[Serializable]
[AddTypeMenu("Взрыв тела с отравлением")]
public class PoisonBurstDeathEffect : EnemyDeathEffect
{
    private static readonly List<Collider2D> OverlapBuffer = new(16);

    [Header("Тайминг")]
    [Tooltip("Задержка от смерти до взрыва: от min до max, сек")]
    [Min(0f)] public float delayMin = 2f;
    [Min(0f)] public float delayMax = 3f;

    [Tooltip("Сколько секунд перед взрывом тело мигает предупреждающим цветом. 0 = без предупреждения")]
    [Min(0f)] public float warningTime = 0.6f;
    public Color warningColor = new Color(0.55f, 1f, 0.35f, 1f);
    [Tooltip("Миганий в секунду")]
    [Min(0.1f)] public float warningBlinkRate = 6f;

    [Header("Взрыв")]
    [Tooltip("Радиус взрыва от центра тела")]
    [Min(0.1f)] public float radius = 1.5f;

    [Tooltip("Мгновенный урон взрыва. 0 = только отравление")]
    [Min(0)] public int damage = 0;

    public PoisonSettings poison = new();

    [Tooltip("VFX взрыва. Пусто = без VFX")]
    public GameObject vfxPrefab;
    [Min(0.1f)] public float vfxLifetime = 1.5f;

    [Tooltip("Убрать тело в момент взрыва (иначе оно доживает deathDespawnDelay)")]
    public bool removeBody = true;

    public override float Begin(EnemyController controller)
    {
        float delay = Random.Range(Mathf.Min(delayMin, delayMax), Mathf.Max(delayMin, delayMax));
        Run(controller, delay).Forget();

        // Запас, чтобы плановое уничтожение тела не опередило сам взрыв.
        return delay + 0.1f;
    }

    private async UniTaskVoid Run(EnemyController controller, float delay)
    {
        CancellationToken token = controller.GetCancellationTokenOnDestroy();
        SpriteRenderer sprite = controller.GetComponent<SpriteRenderer>();
        Color originalColor = sprite != null ? sprite.color : Color.white;

        try
        {
            float warning = Mathf.Min(warningTime, delay);
            int quietMs = Mathf.RoundToInt((delay - warning) * 1000f);
            if (quietMs > 0)
            {
                await UniTask.Delay(quietMs, cancellationToken: token);
            }

            float warningStart = Time.time;
            while (Time.time - warningStart < warning)
            {
                if (sprite != null)
                {
                    float t = Mathf.PingPong((Time.time - warningStart) * warningBlinkRate * 2f, 1f);
                    sprite.color = Color.Lerp(originalColor, warningColor, t);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (controller == null) return;
        if (sprite != null) sprite.color = originalColor;

        Burst(controller);
    }

    private void Burst(EnemyController controller)
    {
        Collider2D body = controller.Movement != null ? controller.Movement.BodyCollider : null;
        Vector2 center = body != null ? (Vector2)body.bounds.center : (Vector2)controller.transform.position;

        if (vfxPrefab != null)
        {
            GameObject vfx = Object.Instantiate(vfxPrefab, center, Quaternion.identity);
            Object.Destroy(vfx, vfxLifetime);
        }

        ContactFilter2D filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
        int hitCount = Physics2D.OverlapCircle(center, radius, filter, OverlapBuffer);

        // У цели бывает несколько коллайдеров — травим каждую один раз.
        HashSet<IAbilityTarget> affected = new();
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = OverlapBuffer[i];
            if (hit == null) continue;

            IAbilityTarget target = hit.GetComponentInParent<IAbilityTarget>();
            // Взрыв задевает только игрока: соседние пауки команды от него не страдают.
            if (target == null || target.Team != Team.Player || !target.IsAlive || !affected.Add(target))
            {
                continue;
            }

            if (damage > 0)
            {
                DamageHelper.TryDamage((Component)target, damage, DamageType.Poison, controller.Attack);
            }
            StatusEffectHandler.TryApplyPoison(hit, poison);
        }

        if (removeBody)
        {
            Object.Destroy(controller.gameObject);
        }
    }
}
