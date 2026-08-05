using UnityEngine;

public class PlayerCharacterManager : MonoBehaviour
{
    private Animator activeAnimator;
    private SpriteRenderer activeSR;
    private GameObject satan;
    private GameObject dog;
    private GameObject activeCharacter;
    public event System.Action<PlayerCharacterType> OnCharacterSwitched;
    private PlayerCharacterType activePlayerCharacterType;
    public PlayerCharacterType GetCurrentCharacterType()
    {
        return activePlayerCharacterType;
    }
    public void SetCurrentCharacterType(PlayerCharacterType type)
    {
        if (activePlayerCharacterType == type) return;
        activePlayerCharacterType = type;
        activeCharacter = type == PlayerCharacterType.Dog ? dog : satan; 
        activeAnimator = activeCharacter.GetComponent<Animator>();
        OnCharacterSwitched?.Invoke(type);
    }
    public Animator ActiveAnimator { get => activeAnimator; set => activeAnimator = value; }
    public SpriteRenderer ActiveSR { get => activeSR; set => activeSR = value; }
    public GameObject ActiveCharacter { get => activeCharacter; set => activeCharacter = value; }
    public GameObject Satan { get => satan; set => satan = value; }
    public GameObject Dog { get => dog; set => dog = value; }
    public bool SatanActive;
    public bool DogActive;
    private void Awake()
    {
        satan = transform.Find("Satan").gameObject;
        dog = transform.Find("Dog").gameObject;
        activeCharacter = satan;
        satan.SetActive(true);
        dog.SetActive(false);
        DogActive = true;
        SatanActive = true;
        activePlayerCharacterType = PlayerCharacterType.Satan;
        activeAnimator = activeCharacter.GetComponent<Animator>();
        activeSR = activeCharacter.GetComponent<SpriteRenderer>();
    }
}