using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [SerializeField]
    private Transform player;

    [Tooltip("Смещение центра рамки относительно центра игрока (в мировых единицах)")]
    [SerializeField]
    private Vector2 offset;
    [Header("Тонкие комнаты (узкие коридоры)")]
    [SerializeField] private float thinRoomThreshold = 12f; // Если bounds.height < этого → фиксируем Y
    [SerializeField] private bool debugThinRooms = true;

    [Tooltip("Время сглаживания: меньше = камера быстрее подстраивается")]
    [SerializeField]
    private float smoothTime = 0.3f;

    [Tooltip(
        "Размер невидимой рамки в локальных единицах камеры (ширина, высота). Камера следует, только если цель выходит за рамку."
    )]
    [SerializeField]
    private LayerMask roomLayerMask = 0;

    private Vector3 camMinBounds;
    private Vector3 camMaxBounds;
    private Collider2D cachedRoomCollider = null;
    private float lastRoomUpdateTime = 0f;
    private const float roomUpdateInterval = 0.1f; // Как часто обновлять комнату
    private Camera cam;
    private bool isThinRoom;
    private float fixedY;
    private Vector3 velocityX = Vector3.zero;
    private Vector3 velocityY = Vector3.zero;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
            Debug.LogError("Player не найден! Tag='Player' обязателен.");

        if (roomLayerMask == 0)
        { // Авто-назначить layer "Rooms" если не задан
            roomLayerMask = LayerMask.GetMask("Rooms");
            Debug.LogWarning("Rooms назначен");
        }
        else
            Debug.Log("Rooms уже назначен");
        camMinBounds = new Vector3(float.MinValue, float.MinValue, 0);
        camMaxBounds = new Vector3(float.MaxValue, float.MaxValue, 0);
    }

    void LateUpdate()
    {
        if (player == null || cam == null)
        {
            Debug.LogError("Player или Camera не назначены!");
            return;
        }
        if (Time.time - lastRoomUpdateTime > roomUpdateInterval)
        {
            UpdateCameraBounds();
            lastRoomUpdateTime = Time.time;
        }
        // Учитываем размеры камеры
        Vector3 desiredPosition = new Vector3(
            player.position.x + offset.x,
            player.position.y + offset.y,
            transform.position.z
        );

        // Clamp по bounds
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, camMinBounds.x, camMaxBounds.x);
        if (isThinRoom)
        {
            desiredPosition.y = fixedY; // Фиксируем Y в центре комнаты
        }
        else
        {
            // Нормальный clamp Y
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, camMinBounds.y, camMaxBounds.y);
        }

        // Плавно
        Vector3 currentPos = transform.position;
        currentPos.x = Mathf.SmoothDamp(
            currentPos.x,
            desiredPosition.x,
            ref velocityX.x,
            smoothTime
        );
        currentPos.y = Mathf.SmoothDamp(
            currentPos.y,
            desiredPosition.y,
            ref velocityY.y,
            smoothTime
        );
        transform.position = currentPos;
        if (Time.frameCount % 60 == 0)
        {
            string roomName = cachedRoomCollider
                ? cachedRoomCollider.name
                : "NONE (infinite bounds)";
            Debug.Log(
                $"Player: {player.position:F1} | Cam: {transform.position:F1} | Room: {roomName} | MinB: {camMinBounds.x:F1},{camMinBounds.y:F1} | MaxB: {camMaxBounds.x:F1},{camMaxBounds.y:F1}"
            );
        }
    }

    private void UpdateCameraBounds()
    {
        Collider2D roomCol = Physics2D.OverlapPoint(player.position, roomLayerMask);
        if (roomCol != null && roomCol.GetComponent<BoxCollider2D>() != null)
        {
            cachedRoomCollider = roomCol;
            BoxCollider2D boxCol = roomCol.GetComponent<BoxCollider2D>();
            camMinBounds = boxCol.bounds.min;
            camMaxBounds = boxCol.bounds.max;

            float boundsHeight = camMaxBounds.y - camMinBounds.y;

            // ← ПРОВЕРКА ТОНКОСТИ
            isThinRoom = boundsHeight < thinRoomThreshold;
            if (isThinRoom)
            {
                fixedY = (camMinBounds.y + camMaxBounds.y) * 0.5f; // Центр по Y
                if (debugThinRooms)
                    Debug.Log($"THIN ROOM: {roomCol.name} (H={boundsHeight:F1} < {thinRoomThreshold}) → Y fixed at {fixedY:F1}");
            }
            else if (debugThinRooms)
            {
                Debug.Log($"NORMAL ROOM: {roomCol.name} (H={boundsHeight:F1})");
            }
        }
        else
        {
            // Fallback infinite
            isThinRoom = false;
            camMinBounds = new Vector3(float.MinValue, float.MinValue, 0);
            camMaxBounds = new Vector3(float.MaxValue, float.MaxValue, 0);
        }
    }
}
