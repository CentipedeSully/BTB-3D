using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public enum AtkState
{
    Unset,
    Standby,
    WarmingUp,
    Hitting,
    CoolingDown
}


public class CoreAtkDriver : MonoBehaviour
{
    [Header("General States")]
    [SerializeField] private AtkState _atkState = AtkState.Unset;
    [SerializeField] protected float _atkWarmup;
    [SerializeField] protected float _atkHitTime;
    [SerializeField] protected float _atkCooldown;
    [SerializeField] protected LayerMask _hittableLayers;
    protected IEnumerator _attackCounter;


    [Header("General Debug Commands")]
    [SerializeField] protected bool _isDebugActive = false;
    [SerializeField] protected bool _debugDeclareEventPassage = false;
    [SerializeField] protected bool _cmdStartAtk = false;
    [SerializeField] protected bool _cmdInterruptAtk = false;




    public Action OnAtkStarted;
    public Action OnAtkInterrupted;

    public Action OnWarmupEntered;
    public Action OnHitStepEntered;
    public Action OnCooldownEntered;
    public Action OnStandByEntered;




    //monobehaviours
    private void OnEnable()
    {
        OnStandByEntered += LogStandbyEntered;
        OnWarmupEntered += LogWarmupEntered;
        OnHitStepEntered += LogHitStepEntered;
        OnCooldownEntered += LogCooldownEntered;

        OnAtkStarted += LogAtkStart;
        OnAtkInterrupted += LogAtkInterrupt;
    }
    private void OnDisable()
    {
        OnStandByEntered -= LogStandbyEntered;
        OnWarmupEntered -= LogWarmupEntered;
        OnHitStepEntered -= LogHitStepEntered;
        OnCooldownEntered -= LogCooldownEntered;

        OnAtkStarted -= LogAtkStart;
        OnAtkInterrupted -= LogAtkInterrupt;
    }
    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();
    }




    //internals
    private IEnumerator CountAttackTimeChain()
    {
        //enter the warmup
        _atkState = AtkState.WarmingUp;
        OnWarmupEntered?.Invoke();
        yield return new WaitForSeconds(_atkWarmup);


        //enter the hit step
        _atkState = AtkState.Hitting;
        OnHitStepEntered?.Invoke();
        yield return new WaitForSeconds(_atkHitTime);


        //enter the cooldown
        _atkState = AtkState.CoolingDown;
        OnCooldownEntered?.Invoke();
        yield return new WaitForSeconds(_atkCooldown);


        //enter standby and clear the counter's reference
        _attackCounter = null;
        _atkState = AtkState.Standby;
        OnStandByEntered?.Invoke();
    }


    private void StartAttackChain()
    {
        if (_attackCounter == null)
        {
            OnAtkStarted?.Invoke();
            _attackCounter = CountAttackTimeChain();
            StartCoroutine(_attackCounter);
        }

    }
    private void InterruptAttack()
    {
        if (_attackCounter != null)
        {
            StopCoroutine(_attackCounter);
            _attackCounter = null;
            _atkState = AtkState.Standby;

            OnAtkInterrupted?.Invoke();
            OnStandByEntered?.Invoke();
        }
    }






    //externals
    public void EnterAttack(){StartAttackChain();}
    public void CancelAttack(){InterruptAttack();}
    public AtkState GetAtkState() { return _atkState; }




    //debug
    private void ListenForDebugCommands()
    {
        if (_cmdStartAtk)
        {
            _cmdStartAtk = false;
            EnterAttack();
        }

        if (_cmdInterruptAtk)
        {
            _cmdInterruptAtk = false;
            CancelAttack();
        }
    }

    private void LogAtkStart() { if (_isDebugActive && _debugDeclareEventPassage) Debug.Log("Atk Started"); }
    private void LogAtkInterrupt() { if (_isDebugActive && _debugDeclareEventPassage) Debug.Log("Atk Interrupted"); }
    private void LogWarmupEntered() { if (_isDebugActive && _debugDeclareEventPassage) Debug.Log("Atk Warming"); }
    private void LogHitStepEntered() { if (_isDebugActive && _debugDeclareEventPassage) Debug.Log("Atk HitStep Entered"); }
    private void LogCooldownEntered() { if (_isDebugActive && _debugDeclareEventPassage) Debug.Log("Atk Cooldown Entered"); }
    private void LogStandbyEntered() { if (_isDebugActive && _debugDeclareEventPassage) Debug.Log("Atk Standby Entered"); }

}
