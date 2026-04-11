using UnityEngine;
using System;


public class HealthPlayerManager
{
    private bool isActive;
    private float maxHP;
    private float currentHP;
    public Action PlayerDeath;
    
    public HealthPlayerManager(float fullHP, bool active)
    {
        currentHP = maxHP = fullHP;
        isActive = active;
    }
    public void ChangeActive(bool isActive)
    {
        this.isActive = isActive;
    }
    
    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP <= 0)
        {
            
        }
    }
    
    
}
