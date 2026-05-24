using System.Collections;
using UnityEngine;

public class DialogueExitDespawn : MonoBehaviour
{
    [Tooltip("Animator, в котором будет вызван триггер. Если пусто — берётся с этого GameObject.")]
    [SerializeField] private Animator animator;

    [Tooltip("Имя триггера в Animator Controller. Если пусто — триггер не вызывается.")]
    [SerializeField] private string exitTrigger = "Exit";

    [Tooltip("GameObject, который будет удалён. Если пусто — удаляется этот объект (или его корень).")]
    [SerializeField] private GameObject targetToDestroy;

    [Tooltip("Удалять корень префаба, а не сам компонент.")]
    [SerializeField] private bool destroyRoot = true;

    [Tooltip("Задержка перед удалением (даём анимации проиграться).")]
    [SerializeField] private float destroyDelay = 1.0f;

    [Tooltip("Автоматически сработать при ближайшем завершении любого диалога. Удобно, если диалог запускается через DialogueTriggerZone и привязать UnityEvent нельзя.")]
    [SerializeField] private bool autoOnDialogueEnd = true;

    private bool isExiting;
    private bool subscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (!autoOnDialogueEnd || subscribed)
        {
            return;
        }

        if (!DialogueManager.HasInstance)
        {
            return;
        }

        DialogueManager.Instance.DialogueEnded += OnAnyDialogueEnded;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        if (DialogueManager.HasInstance)
        {
            DialogueManager.Instance.DialogueEnded -= OnAnyDialogueEnded;
        }

        subscribed = false;
    }

    private void OnAnyDialogueEnded()
    {
        Unsubscribe();
        PlayExitAndDespawn();
    }

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void PlayExitAndDespawn()
    {
        if (isExiting)
        {
            return;
        }

        isExiting = true;

        Animator anim = animator != null ? animator : GetComponentInChildren<Animator>();
        if (anim == null)
        {
            Debug.LogWarning($"DialogueExitDespawn '{name}': Animator не найден.", this);
        }
        else if (string.IsNullOrEmpty(exitTrigger))
        {
            Debug.LogWarning($"DialogueExitDespawn '{name}': Exit Trigger не задан.", this);
        }
        else if (!HasTriggerParam(anim, exitTrigger))
        {
            Debug.LogWarning($"DialogueExitDespawn '{name}': в Animator '{anim.runtimeAnimatorController?.name}' нет Trigger-параметра '{exitTrigger}'. Проверь имя и тип параметра (Trigger, регистр важен).", this);
        }
        else if (!anim.gameObject.activeInHierarchy || !anim.enabled)
        {
            Debug.LogWarning($"DialogueExitDespawn '{name}': Animator выключен или GameObject неактивен — SetTrigger не сработает.", this);
        }
        else
        {
            anim.ResetTrigger(exitTrigger);
            anim.SetTrigger(exitTrigger);
            Debug.Log($"DialogueExitDespawn '{name}': SetTrigger('{exitTrigger}') вызван.", this);
        }

        StartCoroutine(DespawnAfterDelay());
    }

    private static bool HasTriggerParam(Animator anim, string paramName)
    {
        if (anim.runtimeAnimatorController == null)
        {
            return false;
        }

        var parameters = anim.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Trigger &&
                parameters[i].name == paramName)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerator DespawnAfterDelay()
    {
        if (destroyDelay > 0f)
        {
            yield return new WaitForSeconds(destroyDelay);
        }

        GameObject target = targetToDestroy;
        if (target == null)
        {
            target = destroyRoot ? transform.root.gameObject : gameObject;
        }

        Destroy(target);
    }
}
