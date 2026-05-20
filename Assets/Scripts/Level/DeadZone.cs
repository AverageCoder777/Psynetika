using UnityEngine;

// Instant kill zone for the player.
// Destroys the player or triggers death when entering the trigger area.
public class DeadZone : MonoBehaviour
{
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private bool _destroyGameObjectIfNoHealth = true;
    private UIScript _uiScript;

    public void Start(){
        _uiScript = FindFirstObjectByType<UIScript>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(_playerTag))
        {
            return;
        }

        HandlePlayerDeath(collision.gameObject);
    }

    // Attempts to kill the player through the Health component.
    // Falls back to destroying the GameObject if Health component is not found.
    private void HandlePlayerDeath(GameObject playerObject)
    {
        Player playerScript = playerObject.GetComponent<Player>();

        if (playerScript != null)
        {
            _uiScript.GameOver();
            return;
        }

        if (_destroyGameObjectIfNoHealth)
        {
            Debug.LogWarning($"Health component not found on {playerObject.name}. Destroying player directly.", playerObject);
            Destroy(playerObject);
        }
        else
        {
            Debug.LogError($"Player {playerObject.name} has no Health component and fallback destruction is disabled.", playerObject);
        }
    }
}