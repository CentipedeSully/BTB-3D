using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;


public enum AtkState
{
    Unset,
    Standby,
    WarmingUp,
    Hitting,
    CoolingDown
}

public interface IAttackable
{
    int GetUnitID();
    void TakeDamage(int damage);
}

public interface IAttack
{
    void PerformAttack();
    bool IsTargetInRange(int targetID);
    void SetUnitID(int ID);
    void AddIDToIgnoreList(int ID);
    void RemoveIDFromIgnoreList(int ID);
    int GetDamage();
    void SetDamage(int newValue);
    HashSet<int> GetIgnoreList();
    LayerMask GetHittableLayers();
    GameObject GetGameObject();
    IInteractionBehavior GetAsInteractBehavior();
}

public abstract class AbstractAttack : MonoBehaviour, IAttack, IInteractionBehavior
{
    [Header("General Attack Settings")]
    [SerializeField] protected int _unitID;
    [SerializeField] protected int _damage;
    protected HashSet<int> _ignoreList= new();
    [SerializeField] protected AtkState _atkState = AtkState.Unset;
    [SerializeField] protected string _interactionVerb = "(Abstract) Attacking";
    [SerializeField] protected float _atkWarmup;
    [SerializeField] protected float _atkHitTime;
    [SerializeField] protected float _atkCooldown;
    [SerializeField] protected LayerMask _hittableLayers;
    [SerializeField] protected Transform _rangeCheckTransform;
    [SerializeField] protected float _rangeScanRadius;
    protected Collider[] _detectionsWithinRange;
    protected IAttackable _detectedAttackable; 
    protected IEnumerator _attackCounter;
    

    [Header("Detection Watchables")]
    [SerializeField] protected List<int> _attackableIdsWithinRange = new();
    [SerializeField] protected List<int> _attackedIds = new();



    [Header("General Debug Commands")]
    [SerializeField] protected bool _isDebugActive = false;
    [SerializeField] protected bool _declareEventPassage = false;
    [SerializeField] protected bool _cmdStartAtk = false;
    [SerializeField] protected bool _cmdInterruptAtk = false;
    [SerializeField] protected bool _cmdCaptureObjectsInRange = false;
    [SerializeField] protected bool _cmdDeclareAtkState = false;



    public delegate void AttackEvent();
    public AttackEvent OnAtkStarted;
    public AttackEvent OnAtkInterrupted;

    public AttackEvent OnWarmupEntered;
    public AttackEvent OnHitStepEntered;
    public AttackEvent OnCooldownEntered;
    public AttackEvent OnStandByEntered;

    public event Action OnInteractionCompleted;

    private  void OnEnable()
    {
        OnAtkStarted += LogAtkStart;
        OnAtkInterrupted += LogAtkInterrupt;

        OnWarmupEntered += LogWarmupEntered;
        OnHitStepEntered += LogHitStepEntered;
        OnCooldownEntered += LogCooldownEntered;
        OnStandByEntered += LogStandbyEntered;

        SetupChildOnEnableUtils();
    }

    private void OnDisable()
    {
        OnAtkStarted -= LogAtkStart;
        OnAtkInterrupted -= LogAtkInterrupt;

        OnWarmupEntered -= LogWarmupEntered;
        OnHitStepEntered -= LogHitStepEntered;
        OnCooldownEntered += LogCooldownEntered;
        OnStandByEntered -= LogStandbyEntered;

        SetupChildOnDisableUtils();
    }


    private void Start()
    {
        _atkState = AtkState.Standby;
        OnStandByEntered?.Invoke();

        SetupChildStartUtils();
    }

    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();
    }




    //internals
    protected virtual void SetupChildOnEnableUtils() { }
    protected virtual void SetupChildOnDisableUtils() { }
    protected virtual void SetupChildStartUtils() { }
    

    private IEnumerator CountAttackTimeChain()
    {
        //enter the warmup
        _atkState = AtkState.WarmingUp;
        OnWarmupEntered?.Invoke();
        yield return new WaitForSeconds(_atkWarmup);


        //enter the hit step
        _atkState = AtkState.Hitting;
        _attackedIds.Clear();
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
        RaiseCompletionEvent();
    }


    private void StartAttackChain()
    {
        if (_attackCounter == null)
        {
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



    protected virtual void ScanRange()
    {
        _attackableIdsWithinRange.Clear();

        _detectionsWithinRange = Physics.OverlapSphere(_rangeCheckTransform.position, _rangeScanRadius);
        foreach (Collider collider in _detectionsWithinRange)
        {
            //cache the detected attackable for clarity, if it exists
            _detectedAttackable = collider.GetComponent<IAttackable>();
            if (_detectedAttackable != null)
            {
                //only report attackables that aren't on the ignore list
                int detectedId = _detectedAttackable.GetUnitID();
                if (!_ignoreList.Contains(detectedId))
                    _attackableIdsWithinRange.Add(detectedId);
            }
        }
    }

    protected void RaiseCompletionEvent()
    {
        OnInteractionCompleted?.Invoke();
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

    public bool IsTargetInRange(int targetID)
    {
        ScanRange();

        return (_attackableIdsWithinRange.Contains(targetID));
    }

    public GameObject GetGameObject() { return gameObject; }
    public virtual void SetUnitID(int id) {  _unitID = id; AddIDToIgnoreList(_unitID); }
    public void AddIDToIgnoreList(int ID) { _ignoreList.Add(ID); }
    public void RemoveIDFromIgnoreList(int ID) { _ignoreList.Remove(ID); }
    public HashSet<int> GetIgnoreList() { return _ignoreList; }
    public LayerMask GetHittableLayers() {  return _hittableLayers; }
    public int GetDamage() { return _damage; }
    public void SetDamage(int newDamage) { _damage = newDamage; }
    public void PerformAttack(){EnterAttack();}
    public virtual string GetInteractionVerb() { return _interactionVerb; }

    public virtual bool IsInteractionInProgress() { return _atkState != AtkState.Standby; }

    public void PerformInteraction() {EnterAttack();}

    public void InterruptInteraction() { CancelAttack(); }

    public bool IsTargetInRange(IInteractable interactible) { return IsTargetInRange(interactible.GetIdentityInterface().GetID()); }
    public IInteractionBehavior GetAsInteractBehavior() { return this; }



    //debug
    private void LogAtkStart(){ if (_isDebugActive && _declareEventPassage) Debug.Log("Atk Started"); }
    private void LogAtkInterrupt() { if (_isDebugActive && _declareEventPassage) Debug.Log("Atk Interrupted"); }
    private void LogWarmupEntered() { if (_isDebugActive && _declareEventPassage) Debug.Log("Atk Warming"); }
    private void LogHitStepEntered() { if (_isDebugActive && _declareEventPassage) Debug.Log("Atk HitStep Entered"); }
    private void LogCooldownEntered() { if (_isDebugActive && _declareEventPassage) Debug.Log("Atk Cooldown Entered"); }
    private void LogStandbyEntered() { if (_isDebugActive && _declareEventPassage) Debug.Log("Atk Standby Entered"); }

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

        if (_cmdCaptureObjectsInRange)
        {
            _cmdCaptureObjectsInRange = false;
            ScanRange();
        }

        if (_cmdDeclareAtkState)
        {
            _cmdDeclareAtkState = false;
            Debug.Log("AtkState: " + _atkState);
        }
    }

  
}
