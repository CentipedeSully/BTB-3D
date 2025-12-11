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
public interface IAtk
{
    void SetAtkSpeed(float newValue);
    float GetAtkSpeed();
    void CastAtk(bool newState);
    bool IsCastingAtk();
    LayerMask GetLayerMask();
    void SetLayerMask(LayerMask mask);
    string GetAtkName();
    void SetAtkName(string name);
    event Action<HashSet<IIdentity>> OnHitsDetected;
    GameObject GetGameObject();
    public float GetWarmTime();
    public float GetHitTime();
    public float GetCoolTime();
    void EnterWarmup();
    void EnterHitStep();
    void EnterCooldown();
    void AtkCompleted();
    void AtkInterrupted();
    bool IsEntityInRange(IIdentity target);
    string GetAtkAnimationParameterName();


}


public class CoreAtkDriver : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("All attack objects should exist here")]
    [SerializeField] private Transform _knownAtksContainer;
    private Dictionary<string, IAtk> _knownAtks = new();
    private CoreAnimator _coreAnimator;

    [Header("General States")]
    [SerializeField] private List<string> _knownAtksSerialized = new();
    [SerializeField] private AtkState _atkState = AtkState.Unset;
    [SerializeField] private string _currentAtkName;
    private string _emptyAtkName = "[none]";
    [SerializeField] private float _atkWarmup;
    [SerializeField] private float _atkHitTime;
    [SerializeField] private float _atkCooldown;
    [SerializeField] private LayerMask _hittableLayers;
    private IEnumerator _attackCounter;
    private IAtk _currentAtk;
    


    [Header("General Debug Commands")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private bool _debugDeclareEventPassage = false;
    [SerializeField] private string _paramAtkName = "";
    [SerializeField] private bool _cmdStartAtk = false;
    [SerializeField] private bool _cmdInterruptAtk = false;
    [SerializeField] private bool _cmdRebuildKnownAtks = false;



    public Action OnAtkStarted;
    public Action OnAtkInterrupted;

    public Action OnWarmupEntered;
    public Action OnHitStepEntered;
    public Action OnCooldownEntered;
    public Action OnStandByEntered;

    public Action<HashSet<IIdentity>> OnHitsDetected;




    //monobehaviours
    private void Awake()
    {
        _coreAnimator = GetComponent<CoreAnimator>();
    }
    private void OnEnable()
    {
        OnStandByEntered += LogStandbyEntered;
        OnWarmupEntered += LogWarmupEntered;
        
        OnHitStepEntered += LogHitStepEntered;

        OnCooldownEntered += LogCooldownEntered;

        OnAtkStarted += LogAtkStart;
        OnAtkStarted += StartAtkAnimation;
        OnAtkInterrupted += LogAtkInterrupt;

        OnHitsDetected += LogHitsDetectedResponse;
    }
    private void OnDisable()
    {
        OnStandByEntered -= LogStandbyEntered;
        OnWarmupEntered -= LogWarmupEntered;
        
        OnHitStepEntered -= LogHitStepEntered;
        
        OnCooldownEntered -= LogCooldownEntered;

        OnAtkStarted -= LogAtkStart;
        OnAtkStarted -= StartAtkAnimation;
        OnAtkInterrupted -= LogAtkInterrupt;

        OnHitsDetected -= LogHitsDetectedResponse;

        ClearAtk();//this unsubs from the current atk's OnHitsDetected Event, if it exists.
    }
    private void Start()
    {
        _currentAtkName = _emptyAtkName;
        ReadAtkCollectionForKnownAtks();
    }
    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();
    }




    //internals
    private void ReadAtkCollectionForKnownAtks()
    {
        //refresh the collection
        _knownAtks.Clear();
        _knownAtksSerialized.Clear();

        //read each child object for an IAtk component, and save any known atks
        foreach (IAtk atk in _knownAtksContainer.GetComponentsInChildren<IAtk>())
        {
            if (!_knownAtks.ContainsKey(atk.GetAtkName()))
            {
                _knownAtks.Add(atk.GetAtkName(), atk);
                _knownAtksSerialized.Add(atk.GetAtkName());
            }
            else
                Debug.LogWarning($"Found A duplicate atkName. Attacks must have unique names,ignoring duplicate atk '{atk.GetAtkName()} [object: {atk.GetGameObject()}]'");
        }
    }
    private IEnumerator CountAttackTimeChain()
    {
        //enter the warmup
        _atkState = AtkState.WarmingUp;
        OnWarmupEntered?.Invoke();
        _currentAtk.EnterWarmup();
        yield return new WaitForSeconds(_atkWarmup);


        //enter the hit step
        _atkState = AtkState.Hitting;
        OnHitStepEntered?.Invoke();
        _currentAtk.EnterHitStep();
        yield return new WaitForSeconds(_atkHitTime);


        //enter the cooldown
        _atkState = AtkState.CoolingDown;
        OnCooldownEntered?.Invoke();
        _currentAtk.EnterCooldown();
        yield return new WaitForSeconds(_atkCooldown);


        //Clear our current atk utilities
        _currentAtk.AtkCompleted();
        ClearAtk();

        //enter standby and clear the counter's reference
        _attackCounter = null;
        _atkState = AtkState.Standby;
        OnStandByEntered?.Invoke();
    }


    private void StartAttackChain(string atkName)
    {
        //only start an attack if we aren't already attacking
        if (_attackCounter == null && _knownAtks.ContainsKey(atkName))
        {
            SetAtk(atkName);
            OnAtkStarted?.Invoke();
            _attackCounter = CountAttackTimeChain();
            StartCoroutine(_attackCounter);
        }

    }
    private void InterruptAttack()
    {
        if (_attackCounter != null)
        {
            _currentAtk.AtkInterrupted();
            ClearAtk();
            StopCoroutine(_attackCounter);
            _attackCounter = null;
            _atkState = AtkState.Standby;

            OnAtkInterrupted?.Invoke();
            OnStandByEntered?.Invoke();
        }
    }


    private void SetAtk(string atkName)
    {
        _currentAtkName = atkName;
        _currentAtk = _knownAtks[atkName];

        //sub to the atk's OnHitsDetected Event
        _currentAtk.OnHitsDetected += RespondToAtksOnHitsDetectedEvent;

        //convert phase times from relative endtime into individual play length
        _atkWarmup = _currentAtk.GetWarmTime();
        _atkHitTime = _currentAtk.GetHitTime() - _currentAtk.GetWarmTime(); 
        _atkCooldown = _currentAtk.GetCoolTime() - _currentAtk.GetHitTime();

        _currentAtk.SetLayerMask(_hittableLayers);
    }
    private void ClearAtk()
    {
        //ensure atkCasting is deactivated
        if (_currentAtk != null)
            _currentAtk.OnHitsDetected -= RespondToAtksOnHitsDetectedEvent;
            

        _currentAtk = null;
        _currentAtkName = _emptyAtkName;

        _atkWarmup = 0;
        _atkHitTime = 0;
        _atkCooldown = 0;
    }
    private void RespondToAtksOnHitsDetectedEvent(HashSet<IIdentity> detectedIdentities) { OnHitsDetected?.Invoke(detectedIdentities);}
    private void StartAtkAnimation(){_coreAnimator.SetTrigger(_currentAtk.GetAtkAnimationParameterName());}




    //externals
    public void EnterAttack(string atkName)
    {
        StartAttackChain(atkName);
    }
    public void CancelAttack(){InterruptAttack();}
    public AtkState GetAtkState() { return _atkState; }
    public bool IsAtkKnown(string atkName) { return _knownAtks.ContainsKey(atkName); }


    //debug
    private void ListenForDebugCommands()
    {
        if (_cmdStartAtk)
        {
            _cmdStartAtk = false;
            EnterAttack(_paramAtkName);
        }

        if (_cmdInterruptAtk)
        {
            _cmdInterruptAtk = false;
            CancelAttack();
        }

        if (_cmdRebuildKnownAtks)
        {
            _cmdRebuildKnownAtks = false;
            ReadAtkCollectionForKnownAtks();
        }
    }

    private void LogAtkStart() { if (_isDebugActive && _debugDeclareEventPassage) Debug.Log("Atk Started"); }
    private void LogAtkInterrupt() { if (_isDebugActive && _debugDeclareEventPassage) Debug.Log("Atk Interrupted"); }
    private void LogWarmupEntered() { if (_isDebugActive && _debugDeclareEventPassage) Debug.Log("Atk Warming"); }
    private void LogHitStepEntered() { if (_isDebugActive && _debugDeclareEventPassage) Debug.Log("Atk HitStep Entered"); }
    private void LogCooldownEntered() { if (_isDebugActive && _debugDeclareEventPassage) Debug.Log("Atk Cooldown Entered"); }
    private void LogStandbyEntered() { if (_isDebugActive && _debugDeclareEventPassage) Debug.Log("Atk Standby Entered"); }
    private void LogHitsDetectedResponse(HashSet<IIdentity> detections) { if (_isDebugActive) Debug.Log("Detected Atk Event 'onHitsDetected'"); }

}
