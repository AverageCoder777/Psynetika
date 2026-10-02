using UnityEngine;
using UnityEngine.Serialization;

/*
Монета: подбирается, когда игрок касается её триггера.

Префаб: Rigidbody2D + твёрдый коллайдер (лежать на полу; игрока, врагов и другие монеты
он должен исключать через Exclude Layers) + триггер подбора.

Монета, выброшенная через Launch (сундук, лут врага), первые pickupDelay секунд не подбирается —
иначе игрок собирает её прямо в момент разлёта. Пока подбор закрыт, триггер выключен: включение
заново создаёт контакты, поэтому стоящий на монете игрок подберёт её сразу, как задержка истечёт.
Монеты, расставленные на уровне руками, подбираются сразу.
*/
public class Coin : MonoBehaviour
{
    [FormerlySerializedAs("amountCoins")]
    [Min(1)] public int amount = 1;

    [Tooltip("Триггер подбора. Пусто = первый коллайдер-триггер на объекте")]
    [SerializeField] private Collider2D pickupTrigger;

    [SerializeField] private string playerTag = "Player";

    private Rigidbody2D body;
    private float pickupUnlockTime = float.NegativeInfinity;
    private bool collected;

    public bool CanBePickedUp => !collected && Time.time >= pickupUnlockTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        if (pickupTrigger == null)
        {
            foreach (Collider2D candidate in GetComponents<Collider2D>())
            {
                if (candidate.isTrigger)
                {
                    pickupTrigger = candidate;
                    break;
                }
            }
        }

        if (pickupTrigger == null)
        {
            Debug.LogError($"[Coin] {name}: нет коллайдера-триггера — монету невозможно подобрать.");
        }
    }

    // Выбросить монету: импульс + задержка подбора, чтобы монеты успели разлететься.
    public void Launch(Vector2 impulse, float pickupDelay)
    {
        pickupUnlockTime = Time.time + Mathf.Max(0f, pickupDelay);
        if (pickupTrigger != null && pickupDelay > 0f)
        {
            pickupTrigger.enabled = false;
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.AddForce(impulse, ForceMode2D.Impulse);
        }
    }

    private void Update()
    {
        if (pickupTrigger != null && !pickupTrigger.enabled && CanBePickedUp)
        {
            pickupTrigger.enabled = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!CanBePickedUp || !other.CompareTag(playerTag)) return;

        Inventory inventory = other.GetComponentInParent<Inventory>();
        if (inventory == null) return;

        collected = true;
        inventory.AddCoins(amount);
        Destroy(gameObject);
    }
}
