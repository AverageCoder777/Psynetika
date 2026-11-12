using UnityEngine;

public class PlayerCharacterManager : MonoBehaviour
{
    [Header("Visual & Controllers")]
    [SerializeField] private Animator visualAnimator;
    [SerializeField] private SpriteRenderer visualSpriteRenderer;
    [SerializeField] private RuntimeAnimatorController satanController;
    [SerializeField] private RuntimeAnimatorController dogController;
    private GameObject visualObject;
    private PlayerCharacterType activePlayerCharacterType;
    public event System.Action<PlayerCharacterType> OnCharacterSwitched;
    public bool SatanActive = true;
    public bool DogActive = true;

    // Старые свойства для совместимости, отрефакторить в будущем
    public Animator ActiveAnimator { get => visualAnimator; set => visualAnimator = value; }
    public SpriteRenderer ActiveSR { get => visualSpriteRenderer; set => visualSpriteRenderer = value; }
    public GameObject ActiveCharacter { get => visualObject; set => visualObject = value; }
    public GameObject Satan { get => visualObject; set => visualObject = value; }
    public GameObject Dog { get => visualObject; set => visualObject = value; }

    private void Awake()
    {
        SatanActive = true;
        DogActive = true;
        // Если ссылки не назначены в инспекторе, ищем дочерний объект Visual автоматически
        if (visualAnimator == null)
        {
            Transform visualTransform = transform.Find("Visual");
            if (visualTransform != null)
            {
                visualObject = visualTransform.gameObject;
                visualAnimator = visualObject.GetComponent<Animator>();
                visualSpriteRenderer = visualObject.GetComponent<SpriteRenderer>();
            }
        }
        else
        {
            visualObject = visualAnimator.gameObject;
            if (visualSpriteRenderer == null)
                visualSpriteRenderer = visualObject.GetComponent<SpriteRenderer>();
        }

        // Стартовое состояние — Сатана
        activePlayerCharacterType = PlayerCharacterType.Satan;
        if (visualAnimator != null && satanController != null)
        {
            visualAnimator.runtimeAnimatorController = satanController;
        }
    }

    public PlayerCharacterType GetCurrentCharacterType()
    {
        return activePlayerCharacterType;
    }

    public void SetCurrentCharacterType(PlayerCharacterType type)
    {
        if (activePlayerCharacterType == type) return;
        activePlayerCharacterType = type;
        if (visualAnimator != null)
        {
            visualAnimator.runtimeAnimatorController = type == PlayerCharacterType.Dog
                ? dogController
                : satanController;

            // Сбрасываем кэш состояний аниматора под новый контроллер
            visualAnimator.Rebind();
            visualAnimator.Update(0f);
        }
        OnCharacterSwitched?.Invoke(type);
    }
}