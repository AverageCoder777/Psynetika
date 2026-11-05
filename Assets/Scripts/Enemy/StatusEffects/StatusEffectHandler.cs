using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// Работает с любым носителем IDirectDamageReceiver (+ опционально IAbilityStatOwner для замедления Glitch):
// врагом, игроком или другим объектом.
// Burn/Glitch накладываются уроном соответствующего типа (MaybeApplyStatusFromDamage),
// Poison — явно источником с его параметрами (TryApplyPoison).
// Wind статуса не накладывает: попадание ветром по носителю Burn/Glitch разносит их на соседей той же команды.
public class StatusEffectHandler : MonoBehaviour
{
    // Хардкод-дефолты на случай, если config не назначен — статусы работают без редакторской настройки,
    // VFX просто не отображаются.
    private const float DefaultBurnDuration = 6f;
    private const float DefaultBurnTickInterval = 1f;
    private const int DefaultBurnTickDamage = 5;
    private const float DefaultGlitchDuration = 6f;
    private const float DefaultGlitchSlowPerStack = 0.05f;
    private const int DefaultGlitchMaxStacks = 3;
    private const float DefaultExplosionMultiplier = 0.5f;
    private const float DefaultExplosionVfxLifetime = 2f;
    private const float DefaultWindSpreadRadius = 3f;
    private const int DefaultWindSpreadMaxTargets = 3;
    private const float DefaultWindSpreadVfxLifetime = 1f;

    private static readonly List<Collider2D> OverlapBuffer = new(32);

    [SerializeField] private StatusEffectConfig config;

    private IDirectDamageReceiver damageSink;
    private IAbilityStatOwner statOwner;
    private bool dependenciesResolved;
    private BurnRuntime burn;
    private GlitchRuntime glitch;
    private PoisonRuntime poison;
    private bool reactionConsumedThisHit;
    private bool configMissingLogged;

    private float BurnDuration => config != null ? config.burnDuration : DefaultBurnDuration;
    private float BurnTickInterval => config != null ? config.burnTickInterval : DefaultBurnTickInterval;
    private int BurnTickDamage => config != null ? config.burnTickDamage : DefaultBurnTickDamage;
    private float GlitchDuration => config != null ? config.glitchDuration : DefaultGlitchDuration;
    private float GlitchSlowPerStack => config != null ? config.glitchSlowPerStack : DefaultGlitchSlowPerStack;
    private int GlitchMaxStacks => config != null ? config.glitchMaxStacks : DefaultGlitchMaxStacks;
    private float ExplosionMultiplier => config != null ? config.explosionDamageMultiplier : DefaultExplosionMultiplier;
    private float ExplosionVfxLifetime => config != null ? config.explosionVfxLifetime : DefaultExplosionVfxLifetime;
    private float WindSpreadRadius => config != null ? config.windSpreadRadius : DefaultWindSpreadRadius;
    private int WindSpreadMaxTargets => config != null ? config.windSpreadMaxTargets : DefaultWindSpreadMaxTargets;
    private float WindSpreadVfxLifetime => config != null ? config.windSpreadVfxLifetime : DefaultWindSpreadVfxLifetime;
    private GameObject BurnVfxPrefab => config != null ? config.burnVfxPrefab : null;
    private GameObject GlitchVfxPrefab => config != null ? config.glitchVfxPrefab : null;
    private GameObject ExplosionVfxPrefab => config != null ? config.explosionVfxPrefab : null;
    private GameObject WindSpreadVfxPrefab => config != null ? config.windSpreadVfxPrefab : null;

    private void Awake()
    {
        ResolveDependencies();
    }

    // Носителя урона и статов ищем лениво: EnemyController может доставить их уже после нашего Awake.
    private void ResolveDependencies()
    {
        if (dependenciesResolved) return;

        damageSink = GetComponent<IDirectDamageReceiver>();
        statOwner = GetComponent<IAbilityStatOwner>();
        dependenciesResolved = damageSink != null && statOwner != null;
    }

    // Инъекция конфига из EnemyConfig; ссылка, заданная на префабе вручную, имеет приоритет.
    public void SetConfigIfEmpty(StatusEffectConfig cfg)
    {
        if (config == null)
        {
            config = cfg;
        }
    }

    private void WarnIfConfigMissing()
    {
        if (config != null || configMissingLogged) return;
        configMissingLogged = true;
        Debug.LogWarning($"[StatusEffectHandler] {name}: 'config' не назначен. Статусы работают на дефолтах, но VFX не будут отображаться. Создай StatusEffectConfig через Create → Psynetika → Status Effect Config и положи ссылку в EnemyConfig или в поле Config на префабе.");
    }

    public float ProcessIncomingDamage(DamageEvent ev)
    {
        reactionConsumedThisHit = false;
        ResolveDependencies();
        WarnIfConfigMissing();

        if (ev.Type == DamageType.Fire && glitch != null)
        {
            float bonus = ev.Amount * ExplosionMultiplier;
            SpawnExplosionVfx();
            ClearGlitch();
            reactionConsumedThisHit = true;
            return ev.Amount + bonus;
        }

        if (ev.Type == DamageType.Glitch && burn != null)
        {
            float bonus = ev.Amount * ExplosionMultiplier;
            SpawnExplosionVfx();
            ClearBurn();
            reactionConsumedThisHit = true;
            return ev.Amount + bonus;
        }

        if (ev.Type == DamageType.Wind)
        {
            SpreadByWind(ev);
        }

        return ev.Amount;
    }

    public void MaybeApplyStatusFromDamage(DamageEvent ev)
    {
        if (reactionConsumedThisHit)
        {
            return;
        }

        switch (ev.Type)
        {
            case DamageType.Fire:
                ApplyBurnInternal(new BurnSource(ev.Attacker, ev.SourceAbility));
                break;
            case DamageType.Glitch:
                ApplyGlitchInternal();
                break;
        }
    }

    private void OnDestroy()
    {
        ClearBurn();
        ClearGlitch();
        ClearPoison();
    }

    /*
    Отравить объект, которому принадлежит коллайдер/компонент hit. Носителю урона обработчик статусов
    не обязателен заранее: у игрока его на префабе нет, поэтому он доставляется при первом отравлении.
    */
    public static bool TryApplyPoison(Component hit, PoisonSettings settings)
    {
        if (hit == null || settings == null) return false;
        IDirectDamageReceiver receiver = hit.GetComponentInParent<IDirectDamageReceiver>();
        if (receiver == null) return false;
        if (receiver is IAbilityTarget target && !target.IsAlive) return false;

        GameObject owner = ((Component)receiver).gameObject;
        if (!owner.TryGetComponent(out StatusEffectHandler handler))
        {
            handler = owner.AddComponent<StatusEffectHandler>();
        }
        handler.ApplyPoison(settings);
        return true;
    }

    // Повторное отравление перезапускает эффект с новыми параметрами, а не складывает тики.
    public void ApplyPoison(PoisonSettings settings)
    {
        if (settings == null) return;

        ResolveDependencies();
        ClearPoison();
        poison = new PoisonRuntime(this, settings);
        poison.Start();
    }

    private void ApplyBurnInternal(BurnSource source)
    {
        ClearBurn();
        burn = new BurnRuntime(this, source);
        burn.Start();
    }

    private void ApplyGlitchInternal()
    {
        if (glitch == null)
        {
            glitch = new GlitchRuntime(this);
            glitch.Start(1);
        }
        else
        {
            glitch.AddStack();
        }
    }

    /*
    Ветер разносит статусы носителя на ближайших соседей той же команды; сам носитель их не теряет.
    Соседям статус передаётся напрямую, мимо ReceiveDamage, поэтому дальше по цепочке он сам не расходится —
    только следующим попаданием ветра.
    */
    private void SpreadByWind(DamageEvent ev)
    {
        BurnSource? burnSource = burn?.Source;
        int glitchStacks = glitch != null ? glitch.Stacks : 0;
        if (burnSource == null && glitchStacks <= 0) return;

        Team team = damageSink is IAbilityTarget carrier ? carrier.Team : Team.Neutral;
        Vector2 center = transform.position;
        float radius = Mathf.Max(0.1f, WindSpreadRadius);

        ContactFilter2D filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
        int hitCount = Physics2D.OverlapCircle(center, radius, filter, OverlapBuffer);

        // Свой список, а не общий буфер: взрыв-реакция у соседа может синхронно запустить ещё один порыв.
        List<(StatusEffectHandler handler, float sqrDistance)> candidates = new();
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = OverlapBuffer[i];
            if (hit == null) continue;

            IDirectDamageReceiver receiver = hit.GetComponentInParent<IDirectDamageReceiver>();
            if (receiver == null || ReferenceEquals(receiver, damageSink)) continue;
            if (receiver is IAbilityTarget target && (!target.IsAlive || target.Team != team)) continue;

            // Коллайдер может быть большим триггером (зона агро), поэтому радиус меряем по самому носителю.
            Transform owner = ((Component)receiver).transform;
            float sqrDistance = ((Vector2)owner.position - center).sqrMagnitude;
            if (sqrDistance > radius * radius) continue;

            if (!owner.TryGetComponent(out StatusEffectHandler handler))
            {
                handler = owner.gameObject.AddComponent<StatusEffectHandler>();
            }
            if (candidates.Exists(c => c.handler == handler)) continue;

            candidates.Add((handler, sqrDistance));
        }

        candidates.Sort((a, b) => a.sqrDistance.CompareTo(b.sqrDistance));
        int maxTargets = WindSpreadMaxTargets;
        int count = maxTargets > 0 ? Mathf.Min(maxTargets, candidates.Count) : candidates.Count;
        for (int i = 0; i < count; i++)
        {
            candidates[i].handler.ReceiveWindSpread(burnSource, glitchStacks, ev);
        }
    }

    // Принесённый ветром статус, встретив противоположный, взрывается так же, как от удара;
    // бонус считается от силы порыва и идёт сырым уроном, чтобы не запустить пайплайн заново.
    // Горение переносится с исходным источником: тики считаются от его бафов, как у оригинала.
    private void ReceiveWindSpread(BurnSource? burnSource, int glitchStacks, DamageEvent ev)
    {
        ResolveDependencies();
        WarnIfConfigMissing();
        if (!IsCarrierAlive) return;

        bool reacted = false;
        if (burnSource != null && glitch != null)
        {
            ClearGlitch();
            reacted = true;
        }
        if (glitchStacks > 0 && burn != null)
        {
            ClearBurn();
            reacted = true;
        }

        if (reacted)
        {
            SpawnExplosionVfx();
            damageSink?.ApplyDamage(new DamageEvent
            {
                Attacker = ev.Attacker,
                Target = damageSink as IAbilityTarget,
                Amount = ev.Amount * ExplosionMultiplier,
                Type = ev.Type,
                SourceAbility = ev.SourceAbility
            });
            return;
        }

        if (burnSource != null)
        {
            ApplyBurnInternal(burnSource.Value);
        }
        if (glitchStacks > 0)
        {
            if (glitch == null)
            {
                glitch = new GlitchRuntime(this);
                glitch.Start(glitchStacks);
            }
            else
            {
                glitch.SetStacks(Mathf.Max(glitch.Stacks, glitchStacks));
            }
        }
        SpawnWindSpreadVfx();
    }

    private void ClearBurn()
    {
        burn?.Stop();
        burn = null;
    }

    private void ClearGlitch()
    {
        glitch?.Stop();
        glitch = null;
    }

    private void ClearPoison()
    {
        poison?.Stop();
        poison = null;
    }

    private bool IsCarrierAlive => damageSink is not IAbilityTarget target || target.IsAlive;

    private void SpawnExplosionVfx()
    {
        if (ExplosionVfxPrefab == null)
        {
            return;
        }

        // Спавним как ребёнка врага, чтобы взрыв унаследовал тот же масштаб, что Burn/Glitch VFX.
        // Иначе врага рисуют с lossyScale > 1, а взрыв остаётся в корне сцены с localScale 1 и выглядит маленьким.
        GameObject vfx = Instantiate(ExplosionVfxPrefab, transform.position, Quaternion.identity, transform);
        Destroy(vfx, Mathf.Max(0.1f, ExplosionVfxLifetime));
    }

    private void SpawnWindSpreadVfx()
    {
        if (WindSpreadVfxPrefab == null)
        {
            return;
        }

        GameObject vfx = Instantiate(WindSpreadVfxPrefab, transform.position, Quaternion.identity, transform);
        // Пивот врага бывает ниже ног, а угольки поднимаются от низа кадра — ставим низ эффекта на низ тела.
        if (TryGetBodyBounds(out Bounds body) && vfx.TryGetComponent(out SpriteRenderer sprite))
        {
            Vector3 position = vfx.transform.position;
            position.x = body.center.x;
            position.y += body.min.y - sprite.bounds.min.y;
            vfx.transform.position = position;
        }
        Destroy(vfx, Mathf.Max(0.1f, WindSpreadVfxLifetime));
    }

    private bool TryGetBodyBounds(out Bounds bounds)
    {
        foreach (Collider2D collider in GetComponents<Collider2D>())
        {
            if (collider.enabled && !collider.isTrigger)
            {
                bounds = collider.bounds;
                return true;
            }
        }
        bounds = default;
        return false;
    }

    // Кто и чем поджёг. Разнесённое ветром горение несёт тот же источник, поэтому ведёт себя как исходное.
    private readonly struct BurnSource
    {
        public readonly IAbilityCaster Attacker;
        public readonly AbilityDefinition Ability;

        public BurnSource(IAbilityCaster attacker, AbilityDefinition ability)
        {
            Attacker = attacker;
            Ability = ability;
        }

        // Бафы урона поджигателя действуют на тики; читаются на каждом тике, так что баф, взятый
        // уже во время горения, тоже учитывается. Без источника (или если он уничтожен) — множитель 1.
        public float DamageMultiplier
        {
            get
            {
                if (Attacker is not IAbilityStatOwner statOwner) return 1f;
                if (statOwner is Object unityObject && unityObject == null) return 1f;
                return Mathf.Max(0f, statOwner.GetStatMult(StatMultId.CurrentDamageMult));
            }
        }
    }

    private class BurnRuntime
    {
        private readonly StatusEffectHandler handler;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private GameObject vfxInstance;

        public BurnSource Source { get; }

        public BurnRuntime(StatusEffectHandler handler, BurnSource source)
        {
            this.handler = handler;
            Source = source;
        }

        public void Start()
        {
            if (handler.BurnVfxPrefab != null)
            {
                vfxInstance = Object.Instantiate(handler.BurnVfxPrefab, handler.transform);
            }
            Run().Forget();
        }

        public void Stop()
        {
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
            cts.Dispose();
            if (vfxInstance != null)
            {
                Object.Destroy(vfxInstance);
                vfxInstance = null;
            }
        }

        private async UniTaskVoid Run()
        {
            CancellationToken token = cts.Token;
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, handler.BurnDuration);
            float tickInterval = Mathf.Max(0.05f, handler.BurnTickInterval);
            int tickDamage = Mathf.Max(1, handler.BurnTickDamage);

            try
            {
                while (elapsed < duration)
                {
                    int delayMs = Mathf.Max(1, Mathf.RoundToInt(tickInterval * 1000f));
                    await UniTask.Delay(delayMs, cancellationToken: token);
                    elapsed += tickInterval;

                    if (handler == null || handler.damageSink == null)
                    {
                        return;
                    }
                    // Тик обязан идти через ApplyDamage (сырой урон), не через ReceiveDamage —
                    // иначе Fire-тик бесконечно перенакладывал бы Burn на самого себя.
                    // Attacker не передаём: тики статусов не начисляют энергию ульты.
                    handler.damageSink.ApplyDamage(new DamageEvent
                    {
                        Amount = Mathf.Max(1, Mathf.RoundToInt(tickDamage * Source.DamageMultiplier)),
                        Type = DamageType.Fire,
                        SourceAbility = Source.Ability
                    });
                }
            }
            catch (System.OperationCanceledException) { }

            if (handler != null && handler.burn == this)
            {
                handler.ClearBurn();
            }
        }
    }

    private class PoisonRuntime
    {
        private readonly StatusEffectHandler handler;
        private readonly PoisonSettings settings;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private GameObject vfxInstance;

        public PoisonRuntime(StatusEffectHandler handler, PoisonSettings settings)
        {
            this.handler = handler;
            this.settings = settings;
        }

        public void Start()
        {
            if (settings.vfxPrefab != null)
            {
                vfxInstance = Object.Instantiate(settings.vfxPrefab, handler.transform);
            }
            Run().Forget();
        }

        public void Stop()
        {
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
            cts.Dispose();
            if (vfxInstance != null)
            {
                Object.Destroy(vfxInstance);
                vfxInstance = null;
            }
        }

        private async UniTaskVoid Run()
        {
            CancellationToken token = cts.Token;
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, settings.duration);
            float tickInterval = Mathf.Max(0.05f, settings.tickInterval);
            int tickDamage = Mathf.Max(1, settings.tickDamage);
            int delayMs = Mathf.Max(1, Mathf.RoundToInt(tickInterval * 1000f));

            try
            {
                while (elapsed < duration)
                {
                    await UniTask.Delay(delayMs, cancellationToken: token);
                    elapsed += tickInterval;

                    // Мёртвого не травим: PlayerHealth на каждый урон при 0 HP заново поднимает Died.
                    if (handler == null || handler.damageSink == null || !handler.IsCarrierAlive)
                    {
                        break;
                    }
                    // Как и Burn — сырой урон мимо статус-пайплайна.
                    handler.damageSink.ApplyDamage(new DamageEvent
                    {
                        Amount = tickDamage,
                        Type = DamageType.Poison
                    });
                }
            }
            catch (System.OperationCanceledException) { }

            if (handler != null && handler.poison == this)
            {
                handler.ClearPoison();
            }
        }
    }

    private class GlitchRuntime
    {
        private readonly StatusEffectHandler handler;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private GameObject vfxInstance;
        private int stacks;

        public GlitchRuntime(StatusEffectHandler handler)
        {
            this.handler = handler;
        }

        public int Stacks => stacks;

        public void Start(int initialStacks)
        {
            if (handler.GlitchVfxPrefab != null)
            {
                vfxInstance = Object.Instantiate(handler.GlitchVfxPrefab, handler.transform);
            }
            stacks = Mathf.Clamp(initialStacks, 1, Mathf.Max(1, handler.GlitchMaxStacks));
            ApplyMultipliers();
            Run(cts.Token).Forget();
        }

        public void AddStack()
        {
            SetStacks(stacks + 1);
        }

        // Новое число стаков с перезапуском таймера.
        public void SetStacks(int value)
        {
            stacks = Mathf.Clamp(value, 1, Mathf.Max(1, handler.GlitchMaxStacks));
            ApplyMultipliers();

            cts.Cancel();
            cts = new CancellationTokenSource();
            Run(cts.Token).Forget();
        }

        public void Stop()
        {
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
            cts.Dispose();
            if (vfxInstance != null)
            {
                Object.Destroy(vfxInstance);
                vfxInstance = null;
            }
            ResetMultipliers();
        }

        private void ApplyMultipliers()
        {
            if (handler == null || handler.statOwner == null) return;
            float slow = Mathf.Clamp01(handler.GlitchSlowPerStack);
            float multiplier = Mathf.Pow(1f - slow, stacks);
            handler.statOwner.SetStatMult(StatMultId.CurrentMoveSpeedMult, multiplier);
            handler.statOwner.SetStatMult(StatMultId.CurrentAttackSpeedMult, multiplier);
        }

        private void ResetMultipliers()
        {
            if (handler == null || handler.statOwner == null) return;
            handler.statOwner.SetStatMult(StatMultId.CurrentMoveSpeedMult, 1f);
            handler.statOwner.SetStatMult(StatMultId.CurrentAttackSpeedMult, 1f);
        }

        private async UniTaskVoid Run(CancellationToken token)
        {
            float duration = Mathf.Max(0.1f, handler.GlitchDuration);
            int delayMs = Mathf.Max(1, Mathf.RoundToInt(duration * 1000f));
            try
            {
                await UniTask.Delay(delayMs, cancellationToken: token);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }

            if (handler != null && handler.glitch == this)
            {
                handler.ClearGlitch();
            }
        }
    }
}
