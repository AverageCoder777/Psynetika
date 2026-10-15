using UnityEngine;
using UnityEngine.UI;
public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;
    private PlayerCharacterManager charManager;
    private Image SatanHPBar;
    private Image DogHPBar;
    private GameObject SatanHPOverlay;
    private GameObject DogHPOverlay;

    private void Awake()
    {
        GameObject healthBarSatanObj = GameObject.Find("SatanHPBar");
        if (healthBarSatanObj != null)
            SatanHPBar = healthBarSatanObj.GetComponent<Image>();

        GameObject healthBarDogObj = GameObject.Find("DogHPBar");
        if (healthBarDogObj != null)
            DogHPBar = healthBarDogObj.GetComponent<Image>();

        SatanHPOverlay = GameObject.Find("SatanHPOverlay");
        DogHPOverlay = GameObject.Find("DogHPOverlay");

        charManager = FindFirstObjectByType<PlayerCharacterManager>();
    }
    private void Start()
    {
        RefreshActiveCharacter(charManager.GetCurrentCharacterType());
    }


    private void OnEnable()
    {
        if (health == null) return;
        health.HpChanged += OnHpChanged;

        if (charManager == null) return;
        charManager.OnCharacterSwitched += OnActiveCharacterChanged;
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.HpChanged -= OnHpChanged;

        if (charManager == null) return;
        charManager.OnCharacterSwitched -= OnActiveCharacterChanged;
    }

    private void OnActiveCharacterChanged(PlayerCharacterType type)
    {
        RefreshActiveCharacter(type);
    }


    private void OnHpChanged(PlayerCharacterType type, int current, int max)
    {
        UpdateVisuals(type, current, max);
    }

    public void RefreshActiveCharacter(PlayerCharacterType activeType)
    {
        if (health == null) return;

        int current = health.GetCurrentHPOfCharacter(activeType);
        int max = health.GetMaxHPOfCharacter(activeType);
        UpdateVisuals(activeType, current, max);
    }

    private void UpdateVisuals(PlayerCharacterType type, int current, int max)
    {
        if (max <= 0) return;

        float fill = (float)current / max;

        if (type == PlayerCharacterType.Satan)
        {
            if (SatanHPOverlay != null) SatanHPOverlay.SetActive(true);
            if (DogHPOverlay != null) DogHPOverlay.SetActive(false);
            if (SatanHPBar != null) SatanHPBar.fillAmount = fill;
        }
        else
        {
            if (DogHPOverlay != null) DogHPOverlay.SetActive(true);
            if (SatanHPOverlay != null) SatanHPOverlay.SetActive(false);
            if (DogHPBar != null) DogHPBar.fillAmount = fill;
        }
    }
}



