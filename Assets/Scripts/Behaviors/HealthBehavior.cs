using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBehavior : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _currentHp;
    [SerializeField] private int _maxHp;


    [Header("Debug")]
    [SerializeField] private bool _isDebugActive = true;
    [SerializeField] private int _cmdValue;
    [SerializeField] private bool _cmdTakeDamage = false;
    [SerializeField] private bool _cmdRecoverHealth = false;
    [SerializeField] private bool _cmdFullRestore = false;

    private UnitBehavior _unitBehaviour;

    public delegate void HealthChangeEvent(int newValue);
    public delegate void HealthEvent();
    public event HealthChangeEvent OnHealthChanged;
    public event HealthEvent OnDamaged;



    private void Awake()
    {
        _unitBehaviour = GetComponent<UnitBehavior>();
        FullRestore();
    }

    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();
    }



    //internals
    private void ChangeCurrentHp(int additiveAmount)
    {
        _currentHp = Mathf.Clamp(additiveAmount,0,_maxHp);
        OnHealthChanged?.Invoke(_currentHp);
    }


    //externals
    public void TakeDamage(int amount)
    {
        ChangeCurrentHp(_currentHp - amount);
        OnDamaged?.Invoke();
        //Debug.Log($"Dmg taken: {amount}");

        if (_currentHp == 0)
        {
            _unitBehaviour.EnterKOed();
        }
    }
    public void RecoverHealth(int amount)
    {
        ChangeCurrentHp(_currentHp + amount);
    }
    public void FullRestore()
    {
        ChangeCurrentHp(_maxHp);
    }
    public void SetMaxHp(int amount)
    {
        _maxHp = Mathf.Max(1,amount);
    }

    public int GetMaxHp() { return _maxHp; }
    public int GetCurrentHp() { return _currentHp; }


    //debug
    private void ListenForDebugCommands()
    {
        if (_cmdTakeDamage)
        {
            _cmdTakeDamage = false;
            TakeDamage(_cmdValue);
            return;
        }

        if (_cmdRecoverHealth)
        {
            _cmdRecoverHealth = false;
            RecoverHealth(_cmdValue);
            return;
        }

        if (_cmdFullRestore)
        {
            _cmdFullRestore = false;
            FullRestore();
            return;
        }
    }


}
