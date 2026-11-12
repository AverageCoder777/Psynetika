using UnityEngine;

// «Упругость» героя: тянется вверх в прыжке и в полёте, сплющивается при приземлении
// и пружинисто возвращается к форме. Масштабирует только дочерний визуал активного персонажа
// (Satan/Dog) — коллайдер и Rigidbody на корне не меняются. Низ спрайта остаётся на месте,
// поэтому при сплющивании ноги не отрываются от земли.
// PlayerController добавляет компонент сам, если его нет на префабе. Настройки — PlayerStaticSettings.squash.
[DisallowMultipleComponent]
public class PlayerSquashStretch : MonoBehaviour
{
    // Пружину интегрируем мелкими шагами, чтобы она не разлеталась при просадке FPS.
    private const float MaxStep = 1f / 60f;

    private PlayerController player;
    private PlayerMoving movement;
    private PlayerCharacterManager charManager;
    private SquashStretchSettings s;

    private Transform visual;
    private SpriteRenderer visualSR;
    private Vector3 baseScale;
    private Vector3 basePosition;

    // Множитель высоты визуала: >1 — вытянут, <1 — сплющен. Ширина считается так, чтобы площадь сохранялась.
    private float stretch = 1f;
    private float stretchVelocity;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        movement = GetComponent<PlayerMoving>();
        charManager = GetComponent<PlayerCharacterManager>();
        s = Resources.Load<PlayerStaticSettings>("PlayerDefaultSettings").squash;
    }

    private void OnEnable()
    {
        if (movement == null) return;
        movement.Jumped += OnJumped;
        movement.Landed += OnLanded;
    }

    private void OnDisable()
    {
        if (movement != null)
        {
            movement.Jumped -= OnJumped;
            movement.Landed -= OnLanded;
        }
        RestoreVisual();
        visual = null;
    }

    private void OnJumped()
    {
        if (s == null || !s.enabled) return;
        stretch = Mathf.Max(stretch, 1f + s.jumpStretch);
        stretchVelocity = 0f;
    }

    private void OnLanded(float fallSpeed)
    {
        if (s == null || !s.enabled) return;
        float t = Mathf.Clamp01(fallSpeed / s.landSquashFullSpeed);
        stretch = Mathf.Min(stretch, 1f - s.maxLandSquash * t);
        stretchVelocity = 0f;
    }

    private void LateUpdate()
    {
        SyncVisual();
        if (visual == null || s == null) return;

        if (!s.enabled)
        {
            stretch = 1f;
            stretchVelocity = 0f;
            Apply();
            return;
        }

        // В полёте герой слегка вытянут по скорости — так видно направление и силу движения.
        float target = 1f;
        if (player.PlayerSM?.CurrentPlayerState is AirStates)
        {
            target += Mathf.Min(Mathf.Abs(movement.Rb.linearVelocity.y) * s.airStretchPerSpeed, s.maxAirStretch);
        }

        float remaining = Time.deltaTime;
        while (remaining > 0f)
        {
            float dt = Mathf.Min(remaining, MaxStep);
            float accel = -s.stiffness * (stretch - target) - s.damping * stretchVelocity;
            stretchVelocity += accel * dt;
            stretch += stretchVelocity * dt;
            remaining -= dt;
        }
        stretch = Mathf.Clamp(stretch, 0.5f, 1.5f);
        Apply();
    }

    private void Apply()
    {
        float y = stretch;
        float x = 1f / y;
        visual.localScale = new Vector3(baseScale.x * x, baseScale.y * y, baseScale.z);

        // Держим низ спрайта на месте: компенсируем сдвиг от масштабирования вокруг pivot.
        float bottom = visualSR != null && visualSR.sprite != null ? visualSR.sprite.bounds.min.y : 0f;
        visual.localPosition = basePosition + Vector3.up * (bottom * baseScale.y * (1f - y));
    }

    // Персонаж переключился (Satan ↔ Dog) — возвращаем старому визуалу исходную форму и берём новый.
    private void SyncVisual()
    {
        GameObject active = charManager != null ? charManager.ActiveCharacter : null;
        Transform current = active != null ? active.transform : null;
        if (current == visual) return;

        RestoreVisual();
        visual = current;
        stretch = 1f;
        stretchVelocity = 0f;
        if (visual == null) return;

        baseScale = visual.localScale;
        basePosition = visual.localPosition;
        visualSR = visual.GetComponent<SpriteRenderer>();
    }

    private void RestoreVisual()
    {
        if (visual == null) return;
        visual.localScale = baseScale;
        visual.localPosition = basePosition;
    }
}
