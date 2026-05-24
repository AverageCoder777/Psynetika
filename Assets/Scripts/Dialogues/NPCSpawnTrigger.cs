using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class NPCSpawnTrigger : MonoBehaviour
{
    [Header("NPC")]
    [Tooltip("Готовый NPC на сцене, который будет включён при срабатывании триггера. Должен быть отключён изначально.")]
    [SerializeField] private GameObject npcToActivate;

    [Tooltip("Если задан — будет создан Instantiate в spawnPoint. Используется вместо npcToActivate.")]
    [SerializeField] private GameObject npcPrefab;

    [Tooltip("Точка появления NPC (если пусто — позиция самого триггера).")]
    [SerializeField] private Transform spawnPoint;

    [Header("Dialogue")]
    [Tooltip("LinearDialogue или InkDialogue (ScriptableObject, реализующий IDialogueSource).")]
    [SerializeField] private ScriptableObject dialogueSource;

    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Сработать только один раз.")]
    [SerializeField] private bool triggerOnce = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onNpcSpawned;

    private bool hasFired;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || !other.CompareTag(playerTag))
        {
            return;
        }

        if (triggerOnce && hasFired)
        {
            return;
        }

        SpawnNpc();
    }

    private void SpawnNpc()
    {
        GameObject npc = null;

        if (npcPrefab != null)
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
            npc = Instantiate(npcPrefab, pos, rot);
        }
        else if (npcToActivate != null)
        {
            if (spawnPoint != null)
            {
                npcToActivate.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }
            npcToActivate.SetActive(true);
            npc = npcToActivate;
        }
        else
        {
            Debug.LogWarning($"NPCSpawnTrigger '{name}': не назначен ни npcToActivate, ни npcPrefab.", this);
            return;
        }

        AssignDialogue(npc);

        hasFired = true;
        onNpcSpawned?.Invoke();

        if (triggerOnce)
        {
            gameObject.SetActive(false);
        }
    }

    private void AssignDialogue(GameObject npc)
    {
        if (npc == null || dialogueSource == null)
        {
            return;
        }

        if (dialogueSource is not IDialogueSource)
        {
            Debug.LogWarning($"NPCSpawnTrigger '{name}': dialogueSource не реализует IDialogueSource.", this);
            return;
        }

        DialogueInteractable interactable = npc.GetComponentInChildren<DialogueInteractable>(true);
        if (interactable == null)
        {
            Debug.LogWarning($"NPCSpawnTrigger '{name}': на NPC '{npc.name}' нет DialogueInteractable — диалог не назначен.", this);
            return;
        }

        interactable.SetDialogueSource(dialogueSource);
    }

    private void OnValidate()
    {
        if (dialogueSource != null && dialogueSource is not IDialogueSource)
        {
            Debug.LogWarning($"NPCSpawnTrigger '{name}': '{dialogueSource.name}' не реализует IDialogueSource.", this);
        }
    }
}
