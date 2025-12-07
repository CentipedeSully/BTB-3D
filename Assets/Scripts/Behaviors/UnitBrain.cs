using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.CompilerServices;



public enum UnitType
{
    unset,
    Human,
    Insect
}

public enum Faction
{
    unset,
    Independent,
    Player,
    Pirate,
    Insect
}

public enum UnitState
{
    unset,
    Idle,
    PerformingBehavior,
    Stunned,
    KOed
}

public interface IBehavior
{
    /// <summary>
    /// Determines the importance level of the behavior. 
    /// Higher-Priority behaviors will interrupt lower-priority behaviors upon triggering.
    /// Ex: Lv2 behavior got triggered while Lv1 behavior is currently running. 
    /// Brain will cancel the current (Lv1) behavior and start the Lv2 behavior.
    /// The brain will not interrupt the Lv2 behavior if the Lv1 behavior gets triggered, due to the
    /// Lv2 behavior's higher priority Lv.
    /// </summary>
    /// <returns>returns an int. Bigger ints == higher priority.</returns>
    int GetBehaviorPriority();

    /// <summary>
    /// //Determines if the behavoir will autorun when idle
    /// </summary>
    /// <returns>True if the behavior should run if the brain isn't doing anything. False if the behavior must be explicitly be triggered</returns>
    bool IsBehaviourPassive();
    void SetPassiveState(bool newValue);
    string GetBehaviorVerb();
    GameObject GetGameObject();
    string GetBehaviorName();
    event Action OnBehaviorCompleted; //communicates to the brain when a behavior completed its goal
    event Action<IBehavior> OnBehaviorTriggered; //communicates to the brain when an external stimulus has occured
    event Action<string> OnVerbUpdated;
    event Action <IBehavior> OnPassiveStateUpdated;
    bool IsBehaviorDrivingBrain();
    void InterruptBehavior();
    void StartBehaviorAsDriver();
    IIdentity GetUnitIdentity();
    void SetUnitIdentity(IIdentity newIdentity);

}

public class UnitBrain : MonoBehaviour, IIdentity, IInteractable
{
    [Header("Unit Identity")]
    [SerializeField] private int _unitID;
    [SerializeField] private UnitType _unitType = UnitType.unset;
    [SerializeField] private string _unitName = "[unnamed unit]";
    [SerializeField] private Faction _faction = Faction.unset;

    [Header("Unit State")]
    [SerializeField] private UnitState _state = UnitState.unset;
    [SerializeField] private string _currentActionVerb = "";
    private string _defaultActionVerb = "[doing nothing]";
    [SerializeField] private bool _isStunnable = true;
    private float _stunDuration;
    private string _stunnedVerb = "stunned";
    [Tooltip("How long to wait when idle before autotriggering any passiveBehavior with the highest priority")]
    [SerializeField] private float _passiveBehaviorTriggerTime = 1;
    private float _currentPassiveTriggerTime = 0;
    [SerializeField]private bool _passiveBehaviorDetected = false;



    [Header("Debug")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private bool _cmdForceStunUnit = false;
    [SerializeField] private float _paramStunDuration = 4f;

    /// <summary>
    /// All currently-watched behaviors paired with their priority.
    /// </summary>
    private IBehavior _currentDrivingBehavior;
    private Dictionary<IBehavior,int> _knownBehaviors = new Dictionary<IBehavior,int>();
    private IBehavior[] _scannedBehaviors;
    private Dictionary<IBehavior,int> _passiveBehaviors = new Dictionary<IBehavior,int>();


    private CoreHealth _health;


    public delegate void UnitBrainEvent();
    public delegate void UnitBrainAutoTriggerEvent(IBehavior behavior);
    public event UnitBrainEvent OnBehaviorStarted;
    public event UnitBrainEvent OnBehaviorInterrupted;
    public event UnitBrainEvent OnBehaviorCompleted;
    public event UnitBrainEvent OnStunEntered;
    public event UnitBrainEvent OnStunExited;
    public event UnitBrainAutoTriggerEvent OnPassiveAutoTriggerTimerExpired;

    public UnitBrainEvent OnKOedEntered;
    public UnitBrainEvent OnKOedExited;



    //monobehaviours
    private void Awake()
    {
        InitializeReferences();
        InitializeStates();
    }
    private void OnEnable()
    {
        SubToInternalEvents();
        ScanForBehaviorsOnGameObject();
        SubToExternals();
    }
    private void OnDisable()
    {
        UnsubFromInternalEvents();
        ClearKnownBehaviors();
        UnsubFromExternals();
    }
    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();

        if (_state == UnitState.Stunned)
            TickStunDuration();

        if (_state == UnitState.Idle && _passiveBehaviorDetected)
            TickPassiveTriggerDuration();
    }




    //internals
    private void InitializeReferences()
    {
        _health = GetComponent<CoreHealth>();
    }
    private void InitializeStates()
    {
        _unitID = GetInstanceID();
        _state = UnitState.Idle;
        _currentActionVerb = _defaultActionVerb;
        _currentDrivingBehavior = null;
    }
    private void SubToInternalEvents()
    {
        OnBehaviorStarted += EnterPerformingBehaviorState;
        OnBehaviorCompleted += EnterIdleState;
        OnBehaviorInterrupted += EnterIdleState;
        OnStunEntered += EnterStunnedState;
        OnStunExited += EnterIdleState;
        OnPassiveAutoTriggerTimerExpired += RespondToPassiveAutoTriggerEvent;
    }
    private void UnsubFromInternalEvents()
    {
        OnBehaviorStarted -= EnterPerformingBehaviorState;
        OnBehaviorCompleted -= EnterIdleState;
        OnBehaviorInterrupted -= EnterIdleState;
        OnStunEntered -= EnterStunnedState;
        OnStunExited -= EnterIdleState;
        OnPassiveAutoTriggerTimerExpired -= RespondToPassiveAutoTriggerEvent;
    }
    private void SubToExternals()
    {
        _health.OnKoed += RespondToKOedEvent;
    }
    private void UnsubFromExternals()
    {
        _health.OnKoed -= RespondToKOedEvent;
    }



    private void EnterIdleState(){ _state = UnitState.Idle; ResetAutoStartPassiveBehaviorCountdown(); }
    private void EnterPerformingBehaviorState() { _state = UnitState.PerformingBehavior; }
    private void EnterStunnedState() { _state= UnitState.Stunned; ResetAutoStartPassiveBehaviorCountdown(); }
    private void EnterKOedState() {  _state= UnitState.KOed; }



    private void TickStunDuration()
    {
        _stunDuration -= Time.deltaTime;
        if (_stunDuration <= 0)
        {
            _stunDuration = 0;
            _currentActionVerb = _defaultActionVerb;
            OnStunExited?.Invoke();
        }
    }
    private void TickPassiveTriggerDuration()
    {
        _currentPassiveTriggerTime += Time.deltaTime;

        if (_currentPassiveTriggerTime >= _passiveBehaviorTriggerTime)
        {
            //find the passive behavior with the highest priority
            IBehavior highestPriorityBehavior = null;
            int highestPriority = int.MinValue;

            foreach (KeyValuePair<IBehavior,int> entry in _passiveBehaviors)
            {
                ResetAutoStartPassiveBehaviorCountdown();
                //default to the first entry found in this collection
                if (highestPriorityBehavior == null)
                {
                    highestPriorityBehavior = entry.Key;
                    highestPriority = entry.Value;
                }
                    
                //save this entry if it's higher than the last saved entry
                else if (entry.Value > highestPriority)
                {
                    highestPriority = entry.Value;
                    highestPriorityBehavior = entry.Key;
                }
            }

            //Log a warning if we somehow couldn't find a passive behavior
            //[even though this timer only runs if already held passive behavior]
            if (highestPriorityBehavior == null)
            {
                _passiveBehaviorDetected = false;
                Debug.LogWarning("PassiveBehavior List is empty, but the PassiveDetected flag is set. Clearing the passives detected flag. " +
                    "This shouldn't have happened. A passive behavior wasn't removed (or added) properly to the list. Did it become passive without the UnitBrain knowing?" +
                    " No passive behaviors will run, since the UnitBrain doesn't see any");
                return;
            }

            OnPassiveAutoTriggerTimerExpired?.Invoke(highestPriorityBehavior);

        }
    }
    private void ResetAutoStartPassiveBehaviorCountdown(){_currentPassiveTriggerTime = 0;}





    /// <summary>
    /// Locates all IBehaviors on this component's gameobject and adds them to the knownBehaviors Collection.
    /// Also subscribes to each behavior's 'OnTriggered'  event
    /// </summary>
    private void ScanForBehaviorsOnGameObject()
    {
        _scannedBehaviors = GetComponents<IBehavior>();

        foreach (IBehavior behavior in _scannedBehaviors)
            AddBehavior(behavior);
    }



    /// <summary>
    /// Removes and unsubs from all know behaviors. 
    /// Also interrupts the driving behavior if it exists in the knownBehaviors list (which should always be the case).
    /// </summary>
    private void ClearKnownBehaviors()
    {
        //convert the dictionary into an indexable, iterable dataCollection
        List<IBehavior> behaviors = new List<IBehavior>();
        foreach (IBehavior behavior in _knownBehaviors.Keys)
            behaviors.Add(behavior);

        //iteratively remove and unsub from each behavior
        for (int i = behaviors.Count -1; i > 0; i--) //Start from the back
        {
            //unsub and remove from the dictionary
            RemoveBehavior(behaviors[i]);

            //remove from this list
            behaviors.RemoveAt(i);
        }
    }



    /// <summary>
    /// Unsubs from the behavior's events and removes that behavior from the know behaviors list.
    /// If the requested behavior is driving this unit, then that behavior is also interrupted.
    /// </summary>
    /// <param name="behavior"></param>
    private void RemoveBehavior(IBehavior behavior)
    {
        //ignore if the behavior doesn't exist
        if (!_knownBehaviors.ContainsKey(behavior))
            return;

        //remove the behavior from the knownbehaviors Collection
        _knownBehaviors.Remove(behavior);

        //remove the behavior from the Passives Collection if it's passive
        if (behavior.IsBehaviourPassive())
        {
            _passiveBehaviors.Remove(behavior);

            //update the internal state. The passiveTimer mechanic won't execute if no passives exist.
            if (_passiveBehaviors.Count == 0)
                _passiveBehaviorDetected = false;
        }

        //unusb from the behavior's trigger and stateChange events
        behavior.OnBehaviorTriggered -= RespondToBehaviorTrigger;
        behavior.OnPassiveStateUpdated -= RespondToPassiveStateChanged;

        //if the behavior is currently driving
        if (_currentDrivingBehavior == behavior)
            InterruptCurrentDrivingBehavior();

    }



    /// <summary>
    /// Stops the current driving behavior and unsubscribes from that behavior's completion event. 
    /// Also raises the unit's 'OnBehaviorInterrupted' event.
    /// Clears the 'currentDrivingBehavior' utilities.
    /// Does NOT reset the Unit's state on it's own.
    /// </summary>
    private void InterruptCurrentDrivingBehavior()
    {
        if (_currentDrivingBehavior == null)
            return;

        //interrupt the current driving behavior
        _currentDrivingBehavior.InterruptBehavior();

        //clean the UnitBrain's currentDrivingBehavior state
        ClearCurrentDrivingBehaviorUtilities();

        OnBehaviorInterrupted?.Invoke();
    }



    /// <summary>
    /// Cleans the unit's currentDrivingBehavior state by unsubscribing from the current driver's completion and verbUpdate events
    /// and then clearing the currentDrivingBehavior reference. Also resets the current action verb.
    /// IS NOT RESPONSIBLE FOR INTERRUPTING BEHAVIORS!
    /// </summary>
    private void ClearCurrentDrivingBehaviorUtilities()
    {
        //unsub from the completion event
        _currentDrivingBehavior.OnBehaviorCompleted -= RespondToBehaviorCompletion;
        _currentDrivingBehavior.OnVerbUpdated -= RespondToVerbUpdate;

        //update the action verb
        _currentActionVerb = _defaultActionVerb;

        //clear the driving utilities
        _currentDrivingBehavior = null;
    }



    /// <summary>
    /// Sets the currentDrivingBehavior state to the provided behavior and then starts the behavior.
    /// subscribes to the behavior's 'OnCompleted' and 'onVerbUpdated' events. Also triggers the internal 'OnBehaviorStarted' event.
    /// </summary>
    /// <param name="behavior"></param>
    private void StartNewDrivingBehavior(IBehavior behavior)
    {
        _currentDrivingBehavior = behavior;
        _currentDrivingBehavior.OnBehaviorCompleted += RespondToBehaviorCompletion;
        _currentDrivingBehavior.OnVerbUpdated += RespondToVerbUpdate;

        OnBehaviorStarted?.Invoke();
        _currentDrivingBehavior.StartBehaviorAsDriver();
    }



    /// <summary>
    /// Adds the behavior to the knownBehaviors collection and subscribes to that behavior's events.
    /// </summary>
    /// <param name="newBehavior"></param>
    private void AddBehavior(IBehavior newBehavior)
    {
        //only add new behaviors
        if (_knownBehaviors.ContainsKey(newBehavior))
            return;

        //add behavior to list
        _knownBehaviors.Add(newBehavior, newBehavior.GetBehaviorPriority());

        //track if the behavior is passive
        if (newBehavior.IsBehaviourPassive())
        {
            _passiveBehaviors.Add(newBehavior, newBehavior.GetBehaviorPriority());
            _passiveBehaviorDetected = true;
        }
            

        //begin watching the behavior's onTriggered and StateChanged events
        newBehavior.OnBehaviorTriggered += RespondToBehaviorTrigger;
        newBehavior.OnPassiveStateUpdated += RespondToPassiveStateChanged;
    }



    /// <summary>
    /// Responds to behavior triggers. Changes the currentDrivingBehavior if the triggered behavior's priority is greater 
    /// than the currently driving behavior's priority. Otherwise the trigger is ignored.
    /// Runs ther triggered behavior if no other behavior is running (assuming the Unit itself is in a capable state to do so)
    /// </summary>
    /// <param name="triggeredBehavior"></param>
    private void RespondToBehaviorTrigger(IBehavior triggeredBehavior)
    {
        if (_isDebugActive)
            Debug.Log($"Responded to {triggeredBehavior.GetBehaviorName()}'s OnTriggered Event");
        
        if (_state == UnitState.Idle)
        {
            StartNewDrivingBehavior(triggeredBehavior);
            return;
        }

        else if (_state == UnitState.PerformingBehavior)
        {
            if (_currentDrivingBehavior.GetBehaviorPriority() < triggeredBehavior.GetBehaviorPriority())
            {
                InterruptCurrentDrivingBehavior();
                StartNewDrivingBehavior(triggeredBehavior);
            }
        }

    }



    /// <summary>
    /// Cleans the currentDrivingBehavior state and raises the internal OnbehaviorCompleted event.
    /// Also logs the completion event.
    /// </summary>
    private void RespondToBehaviorCompletion()
    {
        if (_isDebugActive)
            Debug.Log($"Completion Event Detected for behavior '{_currentDrivingBehavior.GetBehaviorName()}'." +
            $" Clearing the behavior from the driver's seat");

        ClearCurrentDrivingBehaviorUtilities();
        OnBehaviorCompleted?.Invoke();
    }



    /// <summary>
    /// Catches any verbage updates that's thrown by the currentDrivingBehavior. Used primarily for debugging purposes, currently
    /// </summary>
    /// <param name="newVerb"></param>
    private void RespondToVerbUpdate(string newVerb){_currentActionVerb = newVerb;}



    /// <summary>
    /// This responds to the internal timer that autoTriggers a passive behavior, assuming a
    /// passive behavior exists. Passive behaviors should only auto trigger when the unit is idling.
    /// </summary>
    /// <param name="selectedPassiveBehavior"></param>
    private void RespondToPassiveAutoTriggerEvent(IBehavior selectedPassiveBehavior)
    {
        if (_state == UnitState.Idle)
            StartNewDrivingBehavior(selectedPassiveBehavior);
    }



    /// <summary>
    /// Responds to any behavior that changes its isPassive state. Adjusts the UnitBrain's
    /// collections to reflect the changes. UnitBrain won't try to autoTrigger a passiveBehavior if none exist.
    /// </summary>
    /// <param name="changedBehavior"></param>
    private void RespondToPassiveStateChanged(IBehavior changedBehavior)
    {
        //add the behavior to the passiveBehaviors Collection if its passive and NOT already in the Collection
        if (changedBehavior.IsBehaviourPassive() && !_passiveBehaviors.ContainsKey(changedBehavior))
        {
            _passiveBehaviors.Add(changedBehavior,changedBehavior.GetBehaviorPriority());
            _passiveBehaviorDetected = true;
        }

        //remove the behavior from the passiveBehaviors Collecion if its not passive and IS SAVED AS a passive behavior
        else if (!changedBehavior.IsBehaviourPassive() && _passiveBehaviors.ContainsKey(changedBehavior))
        {
            _passiveBehaviors.Remove(changedBehavior);
            if (_passiveBehaviors.Count == 0)
                _passiveBehaviorDetected= false;
        }
    }

    private void RespondToKOedEvent()
    {
        InterruptCurrentDrivingBehavior();
        EnterKOedState();
        SetActionVerb("Is KOed");
        OnKOedEntered?.Invoke();
    }





    //externals
    public GameObject GetGameObject(){ return gameObject; }
    public int GetID() { return _unitID; }
    public IIdentity GetIdentityInterface() { return this; }
    public IInteractable GetInteractableInterface() { return this; }
    public InteractableType GetInteractableType() { return InteractableType.Unit; }
    public string GetName() {  return _unitName; }
    public Vector3 GetPosition() { return transform.position; }


    public UnitState GetState() { return _state; }
    public IBehavior GetCurrentDrivingBehavior() {  return _currentDrivingBehavior; }
    public string GetCurrentActionVerb() {  return _currentActionVerb; }
    public void SetActionVerb(string newVerb) {  _currentActionVerb = newVerb; }


    /// <summary>
    /// Stuns the unit if it's currently stunnable. Stunned units interrupt their current behavior 
    /// and ignore future behavior triggers until the stun state is exited.
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="reason">Optional additional verbage to describe how the stun occured</param>
    public void StunUnit(float duration, string reason = "")
    {
        //ignore stun calls if the unit is immune or KOed
        if (!_isStunnable || _state == UnitState.KOed)
            return;


        //set the duration
        _stunDuration = duration;

        //interrupt any currently driving behaviors
        InterruptCurrentDrivingBehavior();

        //set the reason
        if (reason == "")
            _currentActionVerb = _stunnedVerb + $" by Developer Powers for {duration} seconds";
        else
            _currentActionVerb = _stunnedVerb + $" {reason} for {duration} seconds";

        OnStunEntered?.Invoke();
    }


    public void AddNewBehvior(IBehavior newBehavior)
    {
        AddBehavior(newBehavior);
    }
    public void RemoveExistingBehavior(IBehavior behavior)
    {
        RemoveBehavior(behavior);
    }



    //Debug
    private void ListenForDebugCommands()
    {
        if (_cmdForceStunUnit)
        {
            _cmdForceStunUnit = false;
            StunUnit(_paramStunDuration);
        }
    }
}
