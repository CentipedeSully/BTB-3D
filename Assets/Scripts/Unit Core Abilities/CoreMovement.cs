using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;


public enum MoveSpeedLevel
{
    None,
    Basic,
    Fast
}

public class CoreMovement : MonoBehaviour
{
    //declarations
    [Header("General Movement Settings")]
    [Tooltip("How close is 'close enough' before a move order is considered fulilled")]
    [SerializeField] private float _stopRange = .25f;
    [SerializeField] private float _baseSpeed;
    private float _currentSpeed;
    [SerializeField] private float _basicMoveLevelMinSpeed;
    [SerializeField] private float _basicMoveLevelMaxSpeed;
    [SerializeField] private MoveSpeedLevel _moveLevel = MoveSpeedLevel.None;
    [SerializeField] private bool _pathCalculationInProgress = false;
    [SerializeField] private bool _isMoving = false;
    private NavMeshAgent _navAgent;
    private IEnumerator _pathingWaiter;
    


    [Header("Debug")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private Transform _paramMoveTransform;
    [SerializeField] private bool _cmdMoveToPosition = false;
    [SerializeField] private bool _toggleContinuousPathingToPosition = false;
    [SerializeField] private bool _cmdCancelMove = false;
    
    
    public Action OnMoveStarted;
    public Action OnMovePathingFailed;
    public Action OnMoveInterrupted;
    public Action OnMoveEnded;

    public Action<MoveSpeedLevel> OnMoveLevelUpdated;



    //monobehaviours
    private void Awake()
    {
        InitializeReferences();
        InitializeUtilities();
    }


    private void OnEnable()
    {
        OnMoveStarted += LogMoveStartedResponse;
        OnMovePathingFailed += LogMovePathingFailedResponse;
        OnMoveInterrupted += LogMoveInterruptedResponse;
        OnMoveEnded += LogMoveEndedResponse;
    }
    private void OnDisable()
    {
        OnMoveStarted -= LogMoveStartedResponse;
        OnMovePathingFailed -= LogMovePathingFailedResponse;
        OnMoveInterrupted -= LogMoveInterruptedResponse;
        OnMoveEnded -= LogMoveEndedResponse;
    }
    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();

        if (_isMoving)
        {
            WatchForMoveLevelChanges();
            WatchForMovementEnd();
        }
    }




    //internals
    private void InitializeReferences()
    {
        _navAgent = GetComponent<NavMeshAgent>();
    }
    private void InitializeUtilities()
    {
        _isMoving = false;
        SetBaseSpeed(_baseSpeed);
        SetStopRange(_stopRange);
    }
    private void WatchForMoveLevelChanges()
    {
        _currentSpeed = _navAgent.speed;
        if (_currentSpeed < _basicMoveLevelMinSpeed && _moveLevel != MoveSpeedLevel.None)
        {
            _moveLevel = MoveSpeedLevel.None;
            OnMoveLevelUpdated?.Invoke(_moveLevel);
        }

        if (_currentSpeed >= _basicMoveLevelMinSpeed && _currentSpeed <= _basicMoveLevelMaxSpeed && _moveLevel != MoveSpeedLevel.Basic)
        {
            _moveLevel = MoveSpeedLevel.Basic;
            OnMoveLevelUpdated?.Invoke(_moveLevel);
        }

        if (_navAgent.speed > _basicMoveLevelMaxSpeed && _moveLevel != MoveSpeedLevel.Fast)
        {
            _moveLevel = MoveSpeedLevel.Fast;
            OnMoveLevelUpdated?.Invoke(_moveLevel);
        }
            
    }
    private void WatchForMovementEnd()
    {
        //Signal the completion of the movement once we've reached our destination
        if (_navAgent.remainingDistance <= _navAgent.stoppingDistance + 0.1f)
        {
            
            StopMovement();
            OnMoveEnded?.Invoke();
            return;
        }

    }
    private void StopMovement()
    {
        _isMoving = false;
        _navAgent.isStopped = true;
        _navAgent.ResetPath();

        _moveLevel = MoveSpeedLevel.None;
        OnMoveLevelUpdated?.Invoke(_moveLevel);
    }

    private IEnumerator WaitForCalculatedPath()
    {
        while (_navAgent.pathPending)
        {
            yield return new WaitForEndOfFrame();
        }
        if (_navAgent.pathStatus == NavMeshPathStatus.PathComplete || _navAgent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            _pathCalculationInProgress = false;
            _pathingWaiter = null;

            _isMoving = true;
            OnMoveStarted?.Invoke();
        }
        else
        {
            Debug.Log("Invalid path calculation detected. Ignoring MoveCommand");
            _pathCalculationInProgress = false;
            _pathingWaiter = null;
        }

    }



    //externals
    public bool IsMoving() {  return _isMoving; }
    public void MoveToPosition(Vector3 newPosition)
    {
        _navAgent.destination = newPosition;

        if (!_isMoving)
        {
            _pathCalculationInProgress = true;

            //interrupt the current pathing if it's calculating
            if (_pathingWaiter!= null)
                StopCoroutine(_pathingWaiter);


            _pathingWaiter = WaitForCalculatedPath();
            StartCoroutine(_pathingWaiter);
        }
    }

    public void CancelMovement()
    {
        if (!_isMoving)
            return;

        StopMovement();
        OnMoveInterrupted?.Invoke();
    }

    public float GetBaseSpeed() {  return _baseSpeed; }
    public void SetBaseSpeed(float newBaseSpeed) { _baseSpeed = newBaseSpeed; _navAgent.speed = _baseSpeed; }
    public float GetStopRange() { return _stopRange; }
    public void SetStopRange(float newStopRange) { _stopRange = newStopRange; _navAgent.stoppingDistance = _stopRange; }




    //Debug
    private void ListenForDebugCommands()
    {
        if (_cmdMoveToPosition)
        {
            _cmdMoveToPosition = false;
            MoveToPosition(_paramMoveTransform.position);
        }
        if (_cmdCancelMove)
        {
            _cmdCancelMove = false;
            CancelMovement();
        }
        if (_toggleContinuousPathingToPosition)
        {
            MoveToPosition(_paramMoveTransform.position);
        }
    }

    private void LogMoveStartedResponse() { if (_isDebugActive) Debug.Log("Detected 'OnMoveStarted' Event"); }
    private void LogMoveInterruptedResponse() { if (_isDebugActive) Debug.Log("Detected 'OnMoveInterrupted' Event"); }
    private void LogMoveEndedResponse() { if (_isDebugActive) Debug.Log("Detected 'OnMoveEnded' Event"); }
    private void LogMovePathingFailedResponse() { if (_isDebugActive) Debug.Log("Detected 'OnMovePathingFailed' Event"); }



}
