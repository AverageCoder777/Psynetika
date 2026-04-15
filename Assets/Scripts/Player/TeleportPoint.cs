using System;
using UnityEngine;

public class TeleportPoint : MonoBehaviour
{
    public bool isChecked = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("player check true");
            isChecked = true;
        }
    }
}
