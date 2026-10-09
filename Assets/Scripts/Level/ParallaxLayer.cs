using Unity.Cinemachine;
using UnityEngine;

/*
Слой параллакса: сдвигается вслед за камерой с коэффициентом.

    позиция = базовая + (камера − якорь) × factor

  factor = 0       — слой стоит в мире, как обычный арт;
  factor = 1       — слой приклеен к камере (небо);
  0 < factor < 1   — фон: чем ближе к 1, тем дальше он кажется;
  factor < 0       — передний план: движется навстречу камере быстрее мира.

Якорь — центр нарисованного содержимого слоя на старте. Когда камера смотрит в него, слой стоит
ровно так, как нарисован в Aseprite, поэтому композиция художника в Scene View не искажается.

Слой двигается строго после того, как CinemachineBrain посчитал кадр (CameraUpdatedEvent), иначе
фон отставал бы от камеры на кадр и дрожал. Камера без Brain обслуживается из LateUpdate.
Вне Play Mode компонент ничего не делает — сцену не пачкаем.
*/
public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("Коэффициент по осям: 0 — стоит в мире, 1 — приклеен к камере, " +
             "0..1 — фон (ближе к 1 — дальше), меньше 0 — передний план")]
    public Vector2 factor = new(0.5f, 0.5f);

    [Tooltip("Камера, за которой следует слой. Пусто — Camera.main")]
    public Camera targetCamera;

    private Vector3 basePosition;
    private Vector2 anchor;
    private bool driven;

    // Для какой камеры уже выяснено, есть ли на ней CinemachineBrain.
    private Camera checkedCamera;
    private bool cameraHasBrain;

    private void OnEnable()
    {
        basePosition = transform.position;

        Renderer ownRenderer = GetComponent<Renderer>();
        anchor = ownRenderer != null ? ownRenderer.bounds.center : basePosition;

        CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
        driven = true;
    }

    // Возврат на место: повторное включение и выход из Play Mode не копят сдвиг.
    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);

        if (driven)
        {
            transform.position = basePosition;
            driven = false;
        }
    }

    private void OnCameraUpdated(CinemachineBrain brain)
    {
        Camera target = ResolveCamera();

        if (brain != null && target != null && brain.OutputCamera == target)
        {
            Follow(target);
        }
    }

    // Фолбэк для камеры без CinemachineBrain: её позицию к LateUpdate уже выставили.
    private void LateUpdate()
    {
        Camera target = ResolveCamera();

        if (target != null && !cameraHasBrain)
        {
            Follow(target);
        }
    }

    /*
    Камера ищется не в OnEnable, а при каждом обновлении, пока не найдётся: слой может включиться
    раньше камеры, и тогда Camera.main в OnEnable вернул бы null — слой так и стоял бы на месте.
    */
    private Camera ResolveCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != checkedCamera)
        {
            checkedCamera = targetCamera;
            cameraHasBrain = targetCamera != null && targetCamera.TryGetComponent(out CinemachineBrain _);
        }

        return targetCamera;
    }

    private void Follow(Camera target)
    {
        Vector2 offset = Vector2.Scale((Vector2)target.transform.position - anchor, factor);
        transform.position = basePosition + (Vector3)offset;
    }
}
