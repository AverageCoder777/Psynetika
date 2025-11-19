using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ChestScript : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds0_02 = new WaitForSeconds(0.02f);

    [Header("Spawn settings")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private Transform spawnParent; // optional parent for spawned coins

    [Header("Spawn area")]
    [Tooltip("If set, coins will spawn at random positions inside this BoxCollider2D bounds. If null, fallback to circular spawnRadius.")]
    [SerializeField] private BoxCollider2D spawnBox;
    [SerializeField] private float spawnRadius = 0.5f; // fallback when spawnBox == null

    [Header("Count")]
    [SerializeField] private int minCoins = 3;
    [SerializeField] private int maxCoins = 8;

    [Header("Scatter")]
    [SerializeField] private float minForce = 2f;
    [SerializeField] private float maxForce = 6f;
    [SerializeField, Range(0f, 1f)] private float upwardBias = 0.6f; // how much force is directed upward

    [Header("Pop animation (2D)")]
    [Tooltip("Duration of the pop animation in seconds")]
    [SerializeField] private float popDuration = 0.45f;
    [Tooltip("Animation curve used for pop interpolation (0..1)")]
    [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("If true, Rigidbody2D will be switched to Dynamic after pop and receive a small impulse")]
    [SerializeField] private bool enablePhysicsAfterPop = true;
    [Tooltip("Impulse applied after pop when physics enabled")]
    [SerializeField] private float postPopImpulse = 0.5f;
    [SerializeField] private Animator animator;

    [Header("Behavior")]
    [SerializeField] private bool destroyChestAfterOpen = false;
    [SerializeField] private float destroyDelay = 2f;

    [Header("Player detection")]
    [Tooltip("Tag to identify player. Chest opens when an object with this tag is inside trigger and Interact action is performed.")]
    [SerializeField] private string playerTag = "Player";
    public UnityEvent onOpened;
    private bool opened = false;
    private GameObject playerInRangeObj;

    private void OnValidate()
    {
        if (minCoins < 0) minCoins = 0;
        if (maxCoins < minCoins) maxCoins = minCoins;
        spawnRadius = Mathf.Max(0f, spawnRadius);
        minForce = Mathf.Max(0f, minForce);
        maxForce = Mathf.Max(minForce, maxForce);
        upwardBias = Mathf.Clamp01(upwardBias);
        popDuration = Mathf.Max(0.01f, popDuration);
        popCurve ??= AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerCollider2D(other))
            playerInRangeObj = other.transform.root.gameObject;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (playerInRangeObj != null && other.transform.root.gameObject == playerInRangeObj)
            playerInRangeObj = null;
    }

    private bool IsPlayerCollider2D(Collider2D c)
    {
        if (c == null) return false;
        if (string.IsNullOrEmpty(playerTag)) return false;
        return c.transform.root.CompareTag(playerTag);
    }

    public void Interact(GameObject interactor)
    {
        Debug.Log("ChestScript: Interact called by " + interactor.name);
        if (interactor != null && interactor == playerInRangeObj)
        {
            ChestOpen();
        }
    }

    private void ChestOpen()
    {
        if (opened) return;
        opened = true;
        if (animator != null)
            animator.SetTrigger("Open");
        Debug.Log("ChestScript: Chest opened");
        // запустить спавн монеток корутиной (2D физика)
        StartCoroutine(SpawnCoinsRoutine());
        onOpened?.Invoke();
    }

    private IEnumerator SpawnCoinsRoutine()
    {
        if (coinPrefab == null) yield break;

        int count = Random.Range(minCoins, maxCoins + 1);
        // if spawnBox provided, use its bounds to get random spawn positions
        bool useBox = spawnBox != null;

        for (int i = 0; i < count; i++)
        {
            Vector2 spawnPos2D;
            if (useBox)
            {
                Bounds b = spawnBox.bounds; // world-space AABB of the box collider
                float x = Random.Range(b.min.x, b.max.x);
                float y = Random.Range(b.min.y, b.max.y);
                spawnPos2D = new Vector2(x, y);
            }
            else
            {
                Vector2 offset = Random.insideUnitCircle * spawnRadius;
                spawnPos2D = (Vector2)transform.position + offset;
            }

            Vector3 spawnPos = (Vector3)spawnPos2D;

            // создаём префаб без вращения (спрайт будет ориентирован как в префабе)
            GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity, spawnParent);
            coin.SetActive(true);
            coin.transform.rotation = Quaternion.identity;

            if (coin.TryGetComponent<Rigidbody2D>(out var rb2))
            {
                // сброс состояний и фиксируем ротацию, чтобы спрайт не крутился
                rb2.linearVelocity = Vector2.zero;
                rb2.angularVelocity = 0f;
                rb2.constraints = RigidbodyConstraints2D.FreezeRotation;

                // направление от центра сундука к спавн-позиции (или вверх если совпадает)
                Vector2 dir = ((Vector2)spawnPos - (Vector2)transform.position).magnitude > 0.01f
                    ? ((Vector2)spawnPos - (Vector2)transform.position).normalized
                    : Vector2.up;

                dir = (dir + Vector2.up * upwardBias).normalized;

                // добавим небольшую вариацию угла для "разных" направлений
                float angleVariation = Random.Range(-30f, 30f);
                dir = (Quaternion.Euler(0f, 0f, angleVariation) * dir).normalized;

                float force = Random.Range(minForce, maxForce);
                rb2.AddForce(dir * force, ForceMode2D.Impulse);
            }

            // небольшой интервал между появлениями для лучшей визуалки
            yield return _waitForSeconds0_02;
        }
    }
}
