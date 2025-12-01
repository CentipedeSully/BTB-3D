using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public abstract class AbstractBehavior : MonoBehaviour, IBehavior
{
    [Header("General Settings")]
    [SerializeField] protected string _behaviorName;
    [SerializeField] protected int _priority;
    [Tooltip("The default verbage that's used to describe what the behavior is currently doing, or attempting to do. " +
        "Readable from the UnitBrain if this behavior is currently driving the unit. ")]
    [SerializeField] protected string _defaultActionVerb;
    [SerializeField] protected bool _isPassive = false;
    protected IIdentity _identity;

    [Header("State")]
    [SerializeField] protected bool _isDrivingUnit = false;


    [Header("Debug")]
    [SerializeField] protected bool _isDebugActive = false;
    [SerializeField] private bool _cmdForceTriggerEvent = false;
    [SerializeField] private bool _cmdForceCompletionEvent = false;



    public event Action OnBehaviorCompleted;
    public event Action<IBehavior> OnBehaviorTriggered;
    public event Action<string> OnVerbUpdated;


    //monobehaviors
    private void Awake()
    {
        IIdentity detectedIdentity = GetComponent<IIdentity>();
        if (detectedIdentity != null)
            _identity = detectedIdentity;
        RunChildAwakeUtils();
    }
    private void OnEnable()
    {
        OnBehaviorCompleted += ResetBehavior;
        RunChildEnableUtils();
    }
    private void OnDisable()
    {
        OnBehaviorCompleted -= ResetBehavior;
        RunChildDisableUtils();
    }
    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();

        RunChildUpdateUtils();
    }




    //internals
    protected virtual void Cancelbehavior()
    {
        //ignore if behavior isn't currently driving
        if (!_isDrivingUnit)
            return;

        ResetBehavior();
        
    }
    protected virtual void StartBehavior()
    {
        //ignore if behavior is already driving
        if (_isDrivingUnit)
            return;

        _isDrivingUnit = true;
        UpdateVerb(_defaultActionVerb);
    }
    protected virtual void ResetBehavior()
    {
        _isDrivingUnit = false;
    }
    protected virtual void UpdateVerb(string newVerb)
    {
        _defaultActionVerb = newVerb;
        OnVerbUpdated?.Invoke(_defaultActionVerb);
    }
    protected virtual void RunChildAwakeUtils(){}
    protected virtual void RunChildEnableUtils(){}
    protected virtual void RunChildDisableUtils(){}
    protected virtual void RunChildUpdateUtils(){}


    //externals
    public string GetBehaviorName() {return _behaviorName;}
    public int GetBehaviorPriority(){ return _priority;}
    public virtual string GetBehaviorVerb(){ return _defaultActionVerb;}
    public GameObject GetGameObject(){return gameObject;}
    public void InterruptBehavior(){Cancelbehavior();}
    public bool IsBehaviorDrivingBrain(){ return _isDrivingUnit; }
    public bool IsBehaviourPassive(){ return _isPassive; }
    public void StartBehaviorAsDriver(){StartBehavior();}
    public IIdentity GetUnitIdentity(){return _identity;}
    public void SetUnitIdentity(IIdentity newIdentity){_identity = newIdentity;}


    //debug
    protected virtual void ListenForDebugCommands()
    {
        if (_cmdForceTriggerEvent)
        {
            _cmdForceTriggerEvent = false;
            OnBehaviorTriggered?.Invoke(this);
        }

        if (_cmdForceCompletionEvent)
        {
            _cmdForceCompletionEvent = false;
            OnBehaviorCompleted?.Invoke();
        }
    }
}
