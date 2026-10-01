using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// Работает с любым носителем IDirectDamageReceiver (+ опционально IAbilityStatOwner для замедления Glitch):
// врагом, игроком или другим объектом.
// Burn/Glitch накладываются уроном соответствующего типа (MaybeApplyStatusFromDamage),
// Poison — явно источником с его параметрами (TryApplyPoison).
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
    private GameObject BurnVfxPrefab => config != null ? config.burnVfxPrefab : null;
    private GameObject GlitchVfxPrefab => config != null ? config.glitchVfxPrefab : null;
    private GameObject ExplosionVfxPrefab => config != null ? config.explosionVfxPrefab : null;

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
                ApplyBurnInternal();
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

    private void ApplyBurnInternal()
    {
        ClearBurn();
        burn = new BurnRuntime(this);
        burn.Start();
    }

    private void ApplyGlitchInternal()
    {
        if (glitch == null)
        {
            glitch = new GlitchRuntime(this);
            glitch.Start();
        }
        else
        {
            glitch.AddStack();
        }
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

    private class BurnRuntime
    {
        private readonly StatusEffectHandler handler;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private GameObject vfxInstance;

        public BurnRuntime(StatusEffectHandler handler)
        {
            this.handler = handler;
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
                    handler.damageSink.ApplyDamage(new DamageEvent
                    {
                        Amount = tickDamage,
                        Type = DamageType.Fire
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

        public void Start()
        {
            if (handler.GlitchVfxPrefab != null)
            {
                vfxInstance = Object.Instantiate(handler.GlitchVfxPrefab, handler.transform);
            }
            stacks = 1;
            ApplyMultipliers();
            Run(cts.Token).Forget();
        }

        public void AddStack()
        {
            stacks = Mathf.Min(stacks + 1, Mathf.Max(1, handler.GlitchMaxStacks));
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
