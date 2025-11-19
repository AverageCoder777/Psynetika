using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("Camera Bounds")]
    private BoxCollider2D roomCollider;
    private void Awake()
    {
        roomCollider = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (Camera.main.TryGetComponent<CameraScript>(out var cameraScript))
            {
                float minX = roomCollider.bounds.min.x;
                float maxX = roomCollider.bounds.max.x;
                float minY = roomCollider.bounds.min.y;
                float maxY = roomCollider.bounds.max.y;
                cameraScript.SetRoomBounds(minX, maxX, minY, maxY);
            }
        }
    }
}
