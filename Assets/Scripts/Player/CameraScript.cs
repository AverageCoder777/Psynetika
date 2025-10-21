using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f);
    [SerializeField] private bool followRotation = false;

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + offset;

        if (followRotation)
            transform.rotation = target.rotation;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
