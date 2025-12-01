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

    string GetBehaviorVerb();
    GameObject GetGameObject();
    string GetBehaviorName();
    event Action OnBehaviorCompleted; //communicates to the brain when a behavior completed its goal
    event Action<IBehavior> OnBehaviorTriggered; //communicates to the brain when an external stimulus has occured
    event Action<string> OnVerbUpdated;
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


    public delegate void UnitBrainEvent();
    public event UnitBrainEvent OnBehaviorStarted;
    public event UnitBrainEvent OnBehaviorInterrupted;
    public event UnitBrainEvent OnBehaviorCompleted;
    public event UnitBrainEvent OnStunEntered;
    public event UnitBrainEvent OnStunExited;



    //monobehaviours
    private void Awake()
    {
        InitializeStates();
    }
    private void OnEnable()
    {
        SubToInternalEvents();
        ScanForBehaviorsOnGameObject();
    }
    private void OnDisable()
    {
        UnsubFromInternalEvents();
        ClearKnownBehaviors();
    }
    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();

        if (_state == UnitState.Stunned)
            TickStunDuration();
    }




    //internals
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
    }
    private void UnsubFromInternalEvents()
    {
        OnBehaviorStarted -= EnterPerformingBehaviorState;
        OnBehaviorCompleted -= EnterIdleState;
        OnBehaviorInterrupted -= EnterIdleState;
        OnStunEntered -= EnterStunnedState;
        OnStunExited -= EnterIdleState;
    }



    private void EnterIdleState(){ _state = UnitState.Idle;}
    private void EnterPerformingBehaviorState() { _state = UnitState.PerformingBehavior; }
    private void EnterStunnedState() { _state= UnitState.Stunned; }
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
            _knownBehaviors.Remove(behaviors[i]);
        }
    }



    /// <summary>
    /// Unsubs from the behavior's 'OnTriggeredEvent' and removes that behavior from the know behaviors list.
    /// If the requested behavior is driving this unit, then that behavior is also interrupted.
    /// </summary>
    /// <param name="behavior"></param>
    private void RemoveBehavior(IBehavior behavior)
    {
        //ignore if the behavior doesn't exist
        if (!_knownBehaviors.ContainsKey(behavior))
            return;

        //unusb from the behavior's trigger event
        behavior.OnBehaviorTriggered -= RespondToBehaviorTrigger;

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
    /// Adds the behavior to the knownBehaviors collection and subscribes to that behavior's 'OnTriggered' event.
    /// </summary>
    /// <param name="newBehavior"></param>
    private void AddBehavior(IBehavior newBehavior)
    {
        //only add new behaviors
        if (_knownBehaviors.ContainsKey(newBehavior))
            return;

        //add behavior to list
        _knownBehaviors.Add(newBehavior, newBehavior.GetBehaviorPriority());

        //begin watching the behavior's trigger event
        newBehavior.OnBehaviorTriggered += RespondToBehaviorTrigger;
    }



    /// <summary>
    /// Responds to behavior triggers. Changes the currentDrivingBehavior if the triggered behavior's priority is greater 
    /// than the currently driving behavior's priority. Otherwise the trigger is ignored.
    /// Runs ther triggered behavior if no other behavior is running (assuming the Unit itself is in a capable state to do so)
    /// </summary>
    /// <param name="triggeredBehavior"></param>
    private void RespondToBehaviorTrigger(IBehavior triggeredBehavior)
    {
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
