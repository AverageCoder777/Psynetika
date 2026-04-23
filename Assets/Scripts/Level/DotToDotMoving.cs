using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform Dot1;
    [SerializeField] private Transform Dot2;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float waitTime = 1f;

    private Vector2 dot1Position;
    private Vector2 dot2Position;
    private Vector2 currentTargetPosition;
    private float waitCounter = 0f;
    private bool isWaiting = false;
    private bool movingToDot2 = true;
    private Rigidbody2D rb;
    void Start()
    {
        if (Dot1 == null || Dot2 == null)
        {
            Debug.LogError("Необходимо назначить обе точки (Dot1 и Dot2)!", gameObject);
            enabled = false;
            return;
        }
        dot1Position = (Vector2)Dot1.position;
        dot2Position = (Vector2)Dot2.position;
        currentTargetPosition = dot2Position;
        transform.position = dot1Position;
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (isWaiting)
        {
            waitCounter -= Time.deltaTime;
            if (waitCounter <= 0f)
            {
                isWaiting = false;
                if (movingToDot2)
                {
                    currentTargetPosition = dot1Position;
                    movingToDot2 = false;
                }
                else
                {
                    currentTargetPosition = dot2Position;
                    movingToDot2 = true;
                }
            }
            return;
        }

        Vector2 direction = (currentTargetPosition - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, currentTargetPosition);

        if (distance <= speed * Time.deltaTime)
        {
            MovePlatformTo(currentTargetPosition);
            isWaiting = true;
            waitCounter = waitTime;
        }
        else
        {
            MovePlatformTo((Vector2)transform.position + speed * Time.deltaTime * direction);
        }
    }

    private void MovePlatformTo(Vector2 newPosition)
    {
        rb.MovePosition(newPosition);
    }

    // Визуализация пути между точками
    void OnDrawGizmosSelected()
    {
        if (Dot1 == null || Dot2 == null)
            return;

        // Рисуем линию между точками
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(Dot1.position, Dot2.position);

        // Рисуем сферы в точках
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Dot1.position, 0.3f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Dot2.position, 0.3f);
    }
}