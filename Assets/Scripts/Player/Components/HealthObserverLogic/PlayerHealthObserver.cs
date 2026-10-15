using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerController))]
public class PlayerHealthObserver : MonoBehaviour
{
    private PlayerHealth health;
    private UIScript ui;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        ui = FindFirstObjectByType<UIScript>();
    }


    private void OnEnable()
    {
        if (health == null) return;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.Died -= OnDied;
    }

    private void OnDied(PlayerCharacterType type)
    {
        if (ui == null) return;
        if (health != null)
        {
            int dogHp = health.GetCurrentHPOfCharacter(PlayerCharacterType.Dog);
            int satanHp = health.GetCurrentHPOfCharacter(PlayerCharacterType.Satan);
            if (dogHp > 0 || satanHp > 0) return;
        }
        ui.GameOver();
    }
}


