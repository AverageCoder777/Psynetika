using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Tooltip("Смещение центра рамки относительно центра игрока (в мировых единицах)")]
    [SerializeField] private Vector3 targetOffset = Vector3.zero;

    [Tooltip("Время сглаживания: меньше = камера быстрее подстраивается")]
    [SerializeField] private float smoothTime = 0.15f;

    [Tooltip("Размер невидимой рамки в локальных единицах камеры (ширина, высота). Камера следует, только если цель выходит за рамку.")]
    [SerializeField] private Vector2 deadZoneSize = new Vector2(2f, 1f);

    private float minY, maxY, minX, maxX;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null || !cam.orthographic)
        {
            Debug.LogError("CameraScript: Камера должна быть ортографической!");
        }
    }

    void LateUpdate()
    {
        if (target == null || cam == null) return;

        // Положение цели в локальных координатах камеры
        Vector3 localTargetPos = transform.InverseTransformPoint(target.position + targetOffset);
        Vector2 half = deadZoneSize * 0.5f;

        // Вычисление смещения в локальных координатах, если цель вышла за рамку
        Vector3 localDelta = Vector3.zero;

        if (localTargetPos.x > half.x)
            localDelta.x = localTargetPos.x - half.x;
        else if (localTargetPos.x < -half.x)
            localDelta.x = localTargetPos.x + half.x;

        if (localTargetPos.y > half.y)
            localDelta.y = localTargetPos.y - half.y;
        else if (localTargetPos.y < -half.y)
            localDelta.y = localTargetPos.y + half.y;

        // Переводим локальное смещение в мировые координаты
        Vector3 worldDelta = transform.TransformVector(localDelta);

        // Целевая позиция камеры — текущая позиция + смещение (движение только на необходимую дельту)
        Vector3 targetPosition = transform.position + worldDelta;

        // Учитываем размеры камеры
        float cameraHalfHeight = cam.orthographicSize;
        float cameraHalfWidth = cam.aspect * cameraHalfHeight;

        // Ограничиваем положение камеры так, чтобы её края не выходили за границы
        targetPosition.x = Mathf.Clamp(targetPosition.x, minX + cameraHalfWidth, maxX - cameraHalfWidth);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY + cameraHalfHeight, maxY - cameraHalfHeight);

        // Применяем новое положение камеры
        transform.position = targetPosition;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetRoomBounds(float newMinX, float newMaxX, float newMinY, float newMaxY)
    {
        minX = newMinX;
        maxX = newMaxX;
        minY = newMinY;
        maxY = newMaxY;
    }
}
