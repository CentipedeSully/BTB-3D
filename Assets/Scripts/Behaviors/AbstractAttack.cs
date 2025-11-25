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

public abstract class AbstractAttack : MonoBehaviour
{
    [Header("General Attack Settings")]
    [SerializeField] protected AtkState _atkState = AtkState.Unset;
    [SerializeField] protected float _atkWarmup;
    [SerializeField] protected float _atkHitTime;
    private float _currentHitTime;
    private float _normalizedHitTime;
    [SerializeField] protected float _atkCooldown;
    [SerializeField] protected float _effectiveRangeRadius;
    [SerializeField] protected AnimationCurve _xHitPath;
    [SerializeField] protected AnimationCurve _yHitPath;
    [SerializeField] protected AnimationCurve _zHitPath;

    [Header("Hit Area Visualization")]
    private Vector3 _currentHitAreaPosition;
    [SerializeField] protected bool _showHitAreaGizmo = false;
    [SerializeField] protected Color _inactiveColor = Color.gray;
    [SerializeField] protected Color _activeColor = Color.red;

    [Header("General Debug Commands")]
    [SerializeField] protected bool _isDebugActive = false;
    [SerializeField] private bool _cmdStartAtk = false;
    [SerializeField] private bool _cmdInterruptAtk = false;



    public delegate void AttackEvent();
    public AttackEvent OnAtkStarted;
    public AttackEvent OnAtkInterrupted;

    public AttackEvent OnWarmupEntered;
    public AttackEvent OnHitStepEntered;
    public AttackEvent OnCooldownEntered;
    public AttackEvent OnStandByEntered;



    private void OnDrawGizmosSelected()
    {
        DrawHitAreaGizmo();
    }

    private void OnEnable()
    {
        OnAtkStarted += LogAtkStart;
        OnAtkInterrupted += LogAtkInterrupt;

        OnWarmupEntered += LogWarmupEntered;
        OnHitStepEntered += LogHitStepEntered;
        OnCooldownEntered += LogCooldownEntered;
        OnStandByEntered += LogStandbyEntered;
    }

    private void OnDisable()
    {
        OnAtkStarted -= LogAtkStart;
        OnAtkInterrupted -= LogAtkInterrupt;

        OnWarmupEntered -= LogWarmupEntered;
        OnHitStepEntered -= LogHitStepEntered;
        OnCooldownEntered += LogCooldownEntered;
        OnStandByEntered -= LogStandbyEntered;
    }



    private void Start()
    {
        EnterStandby();
    }

    private void Update()
    {
        if (_atkState == AtkState.Hitting)
            TrackHitAreaDuringHitStep();

        if (_isDebugActive)
            ListenForDebugCommands();
    }




    //internals
    private void StartAttackChain()
    {
        if (_atkState == AtkState.Standby)
        {
            OnAtkStarted?.Invoke();
            EnterWarmup();
        }
            
    }
    private void InterruptAttack()
    {
        if (_atkState != AtkState.Standby)
        {
            CancelInvoke();
            OnAtkInterrupted?.Invoke();

            EnterStandby();
        }
    }


    private void EnterWarmup()
    {
        _atkState = AtkState.WarmingUp;
        OnWarmupEntered?.Invoke();

        Invoke(nameof(EnterHitStep), _atkWarmup);
    }
    private void EnterHitStep()
    {
        _atkState = AtkState.Hitting;
        _currentHitTime = 0;
        OnHitStepEntered?.Invoke();

        Invoke(nameof(EnterCooldown), _atkHitTime);
    }
    private void EnterCooldown()
    {
        _atkState = AtkState.CoolingDown;
        OnCooldownEntered?.Invoke();

        Invoke(nameof(EnterStandby), _atkCooldown);
    }
    private void EnterStandby()
    {
        _atkState = AtkState.Standby;
        OnStandByEntered?.Invoke();
    }


    private void DrawHitAreaGizmo()
    {
        if (_showHitAreaGizmo)
        {
            if (_atkState != AtkState.Hitting)
            {
                Gizmos.color = _inactiveColor;

                //Update the hitArea's position to the starting position if we aren't trying to hit anything right now
                _currentHitAreaPosition = transform.InverseTransformPoint(new Vector3(_xHitPath.Evaluate(0),_yHitPath.Evaluate(0),_zHitPath.Evaluate(0)));
            }

            else 
                Gizmos.color = _activeColor;

            Gizmos.DrawWireSphere(_currentHitAreaPosition, _effectiveRangeRadius);

        }
    }

    private void TrackHitAreaDuringHitStep()
    {
        _currentHitTime += Time.deltaTime;
        _normalizedHitTime = _currentHitTime / _atkHitTime;

        //calculate the raw position
        _currentHitAreaPosition = new Vector3(_xHitPath.Evaluate(_normalizedHitTime),_yHitPath.Evaluate(_normalizedHitTime),_zHitPath.Evaluate(_normalizedHitTime));

        //convert the raw position into a local position
        _currentHitAreaPosition = transform.InverseTransformPoint(_currentHitAreaPosition);
    }
    



    //Externals
    public void EnterAttack()
    {
        StartAttackChain();
    }

    public void CancelAttack()
    {
        InterruptAttack();
    }

    public AtkState GetAtkState() {  return _atkState; }


    //debug
    private void LogAtkStart(){ if (_isDebugActive) Debug.Log("Atk Started"); }
    private void LogAtkInterrupt() { if (_isDebugActive) Debug.Log("Atk Interrupted"); }
    private void LogWarmupEntered() { if (_isDebugActive) Debug.Log("Atk Warming"); }
    private void LogHitStepEntered() { if (_isDebugActive) Debug.Log("Atk HitStep Entered"); }
    private void LogCooldownEntered() { if (_isDebugActive) Debug.Log("Atk Cooldown Entered"); }
    private void LogStandbyEntered() { if (_isDebugActive) Debug.Log("Atk Standby Entered"); }

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
}
