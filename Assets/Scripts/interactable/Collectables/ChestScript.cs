using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

// Сундук: открывается по Interact, когда игрок рядом, и выбрасывает монеты (см. CoinDrop).
public class ChestScript : MonoBehaviour, IInteractable
{
    private static readonly int CanOpenHash = Animator.StringToHash("CanOpen");
    private static readonly int OpenHash = Animator.StringToHash("Open");

    [Header("Монеты")]
    [SerializeField] private CoinDrop coins = new() { minCount = 5, maxCount = 8, minForce = 5f, maxForce = 8f };

    [Tooltip("Монеты появляются в случайной точке внутри этой области. Пусто = из центра сундука")]
    [SerializeField] private BoxCollider2D spawnBox;

    [Tooltip("Пауза между монетами, сек — монеты вылетают струйкой, а не одной кучей")]
    [Min(0f)] [SerializeField] private float spawnInterval = 0.02f;

    [Header("Прочее")]
    [SerializeField] private Animator animator;

    [Tooltip("Сундук открывается, когда объект с этим тегом (по корню) стоит в триггере и нажат Interact")]
    [SerializeField] private string playerTag = "Player";

    public UnityEvent onOpened;

    private bool opened;
    private GameObject playerInRangeObj;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerCollider2D(other))
        {
            playerInRangeObj = other.transform.root.gameObject;
            SetCanOpen(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (playerInRangeObj != null && other.transform.root.gameObject == playerInRangeObj)
        {
            playerInRangeObj = null;
            SetCanOpen(false);
        }
    }

    private bool IsPlayerCollider2D(Collider2D c)
    {
        if (c == null || string.IsNullOrEmpty(playerTag)) return false;
        return c.transform.root.CompareTag(playerTag);
    }

    public void Interact()
    {
        if (playerInRangeObj != null)
        {
            Open();
        }
    }

    private void Open()
    {
        if (opened) return;
        opened = true;

        if (animator != null)
        {
            animator.SetTrigger(OpenHash);
        }

        if (coins.coinPrefab == null)
        {
            Debug.LogWarning($"[ChestScript] {name}: не назначен префаб монеты.");
        }
        else
        {
            SpawnCoins(this.GetCancellationTokenOnDestroy()).Forget();
        }

        onOpened?.Invoke();
    }

    private async UniTaskVoid SpawnCoins(CancellationToken token)
    {
        int count = coins.RollCount();
        int delayMs = Mathf.RoundToInt(spawnInterval * 1000f);

        try
        {
            for (int i = 0; i < count; i++)
            {
                if (i > 0 && delayMs > 0)
                {
                    await UniTask.Delay(delayMs, cancellationToken: token);
                }

                coins.SpawnOne(GetSpawnPoint());
            }
        }
        catch (OperationCanceledException) { }
    }

    private Vector2 GetSpawnPoint()
    {
        if (spawnBox == null) return transform.position;

        Bounds b = spawnBox.bounds;
        return new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));
    }

    private void SetCanOpen(bool value)
    {
        if (animator != null)
        {
            animator.SetBool(CanOpenHash, value);
        }
    }
}
