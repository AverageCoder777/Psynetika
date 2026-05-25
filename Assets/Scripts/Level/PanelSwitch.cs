using UnityEngine;

public class PanelSwitch : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            UIScript uiScript = FindFirstObjectByType<UIScript>();
            if (uiScript != null)
            {
                uiScript.EnableFinishPanel();
            }
        }
    }
}
