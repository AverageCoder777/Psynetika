using UnityEngine;
using System.Collections;

public class EnemyFluid : MonoBehaviour
{
    [SerializeField] private float damageTime = 1.5f;
    [SerializeField] private int damageAmount = 10;
    private Coroutine damageCoroutine;
    private PlayerController currentPlayer;
    private bool damageActive = false;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (damageActive == false && other.CompareTag("Player"))
        {
            currentPlayer = other.GetComponent<PlayerController>();
            currentPlayer.TakeDamage(damageAmount);
            damageCoroutine = StartCoroutine(DamageOverTime(currentPlayer));
            damageActive = true;
            Debug.Log("Player has entered enemy fluid! Status of damageactive: "+ damageActive);

        }
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (damageActive == true && other.CompareTag("Player"))
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
                damageActive = false;
            }

            currentPlayer = null;
            Debug.Log("Player has exited enemy fluid! Status of damageactive: " + damageActive);
        }
    }
    private IEnumerator DamageOverTime(PlayerController player)
    {
        if (damageTime <= 0f || damageActive == false)
            yield break;

        while (player != null)
        {
            player.TakeDamage(damageAmount);
            yield return new WaitForSeconds(damageTime);
        }
    }
}