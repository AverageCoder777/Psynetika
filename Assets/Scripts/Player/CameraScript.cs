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

    void LateUpdate()
    {
        if (target == null) return;

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
        transform.position = transform.position + worldDelta;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
