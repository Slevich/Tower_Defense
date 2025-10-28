using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour, IReset
{
    #region Fields
    [Header("Settings.")]
    [SerializeField, Range(1, 1000)] 
    private uint _maxHealth = 100;
    [SerializeField, Range(0, 1000)]
    private uint _currentHealth = 100;
    #endregion

    #region Properties
    public bool IsDead => _currentHealth == 0;
    public UnityEvent OnDeathEvent { get; set; } = new UnityEvent();
    #endregion

    #region Methods
    private void OnValidate()
    {
       if(_currentHealth > _maxHealth)
            _currentHealth = _maxHealth;
    }

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void CauseDamage(uint Damage)
    {
        if (IsDead)
            return;

        uint clampedDamage = (uint)Mathf.Clamp(Damage, 0, _currentHealth);
        uint leftHealth = _currentHealth - clampedDamage;
        _currentHealth = leftHealth;
        
        if(leftHealth == 0)
        {
            OnDeathEvent?.Invoke();
        }
    }

    public void Reset() => _currentHealth = _maxHealth;
    #endregion
}
