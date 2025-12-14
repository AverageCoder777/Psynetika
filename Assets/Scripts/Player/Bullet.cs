using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;  // Скорость полета
    public float lifetime = 5f; 
    public int damage = 10;  // Урон (передадим из Player)
    private LayerMask enemyMask;  // Маска врагов

    private void Start()
    {
        enemyMask = LayerMask.GetMask("Enemy");
        Destroy(gameObject, lifetime);  // Автоуничтожение
    }

    public void SetDirection(float dir)
    {
        GetComponent<Rigidbody2D>().linearVelocity = new Vector2(dir * speed, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((enemyMask.value & (1 << other.gameObject.layer)) > 0)
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log("Bullet hit enemy with " + damage + " damage");
            }
            Destroy(gameObject);  // Уничтожить пулю при попадании
        }
    }
}