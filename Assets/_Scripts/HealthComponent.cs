using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthComponent : NetworkBehaviour
{
    [Header("Setting")]
    [SerializeField] private int maxHealth = 100;
    public int MaxHealth => maxHealth;

    //[Networked,OnChangedRender(nameof(OnHealthChange))]

    [Networked] public int CurrentHealth { get; set; }

    public event Action<float> OnHealthChangedEvent; // Trả về % máu (0.0 đến 1.0)
    public event Action OnDeathEvent;//sự kiện khi died
    public event Action<int> OnDamageTakeEvent;// Sự kiện khi bị đau

    public bool IsDead => CurrentHealth <= 0;

    public override void Spawned()
    {
        if(Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
        }

        OnHealthChangedEvent?.Invoke((float)CurrentHealth / maxHealth);  
    }

    public void TakeDamage(int damageAmount)
    {
        if (IsDead && !Object.HasStateAuthority) return;

        CurrentHealth -= damageAmount;

        if(CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        if (IsDead && !Object.HasStateAuthority) return;

        CurrentHealth += healAmount;
        if(CurrentHealth >= maxHealth)
        {
            CurrentHealth = maxHealth;
        }
    }


    private void Die()
    {
        OnDeathEvent?.Invoke();

        Runner.Despawn(Object);
    }

    //void OnHealthChange()
    //{
    //    float healthPercent = (float)CurrentHealth/maxHealth;

    //    OnHealthChangedEvent?.Invoke(healthPercent);
    //}

}
