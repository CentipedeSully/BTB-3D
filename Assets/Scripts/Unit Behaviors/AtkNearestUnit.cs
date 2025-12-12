using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;





public class AtkNearestUnit : AbstractBehavior
{
    //declarations
    private CoreDetectSurroundings _detectionAbility;
    private CoreAtkDriver _atkDriver;
    private CoreMovement _moveAbility;

    [Header("Behavior Settings")]
    [SerializeField] private int _currentTargetedId;
    private IIdentity _currentTarget;
    private Transform _currentTargetedTransform;

    [SerializeField] private string _atkName;
    [SerializeField] private float _targetLostRange;
    [SerializeField] private float _detectionRange;
    [SerializeField] private LayerMask _detectionMask;
    [SerializeField] private List<int> _ignoredIds=new();
    private Dictionary<IIdentity,float> _detectedUnits = new();
    private float _cachedDistance;
    private KeyValuePair<IIdentity,float> _cachedClosest;
    private HashSet<IIdentity> _hitUnits = new();
    [SerializeField] private bool _isAttacking = false;





    //monobehaviors
    protected override void RunChildAwakeUtils()
    {
        _detectionAbility = GetCoreComponent<CoreDetectSurroundings>();
        _atkDriver = GetCoreComponent<CoreAtkDriver>();
        _moveAbility = GetCoreComponent<CoreMovement>();
    }
    protected override void RunChildEnableUtils()
    {
        _detectionAbility.OnEntitiesDetected += TrackDetections;
        _atkDriver.OnAtkStarted += RespondToAtkEntered;
        _atkDriver.OnAtkInterrupted += RespondToAtkExited;
        _atkDriver.OnCooldownEnded += RespondToAtkExited;
    }
    protected override void RunChildDisableUtils()
    {
        _detectionAbility.OnEntitiesDetected -= TrackDetections;
        _atkDriver.OnAtkStarted -= RespondToAtkEntered;
        _atkDriver.OnAtkInterrupted -= RespondToAtkExited;
        _atkDriver.OnCooldownEnded -= RespondToAtkExited;

    }
    protected override void RunChildStartUtils()
    {
        AddSelfToIgnoredList();
    }
    protected override void RunChildUpdateUtils()
    {
        //don't make any sudden behavioral changes if we're in the middle of an attack
        if (!_isAttacking)
        {
            //always watch for units [unless we've already targetted a unit]
            DetectForNearbyUnits();

            //approach and attack the target unit, or lose the unit if it's too far away
            if (_isDrivingUnit)
                AtkNearestEntity();
        }
    }





    //internals
    private void ClearTarget()
    {
        _currentTarget = null;
        _currentTargetedId = 0;
        _currentTargetedTransform = null;
    }
    private void SetTarget(IIdentity newTarget)
    {
        if (newTarget == null)
            return;

        _currentTarget = newTarget;
        _currentTargetedId = _currentTarget.GetID();
        _currentTargetedTransform = _currentTarget.GetGameObject().transform;
    }
    private void AddSelfToIgnoredList()
    {
        _ignoredIds.Add(_identity.GetID());
    }
    private void DetectForNearbyUnits()
    {
        //stop the detection utility [to save resources] if we already have a target
        if (_currentTarget != null)
        {
            if (_detectionAbility.IsDetectionActive())
                _detectionAbility.SetDetection(false);
            return;
        }

        //otherwise, ensure we're actively detecting units 
        if (!_detectionAbility.IsDetectionActive())
        {
            _detectionAbility.SetDetection(true);
            _detectionAbility.SetDetectionLayers(_detectionMask);
            _detectionAbility.SetDetectionDistance(_detectionRange);
            UpdateVerb("Watching for targets");
        }

    }
    private bool IsTargetBeyondLostRange()
    {
        if (_currentTargetedTransform == null)
        {
            if (_isDebugActive)
                Debug.Log("Cant determine if target is beyond lost range. No Target set. returning True [target is beyond range]");
            return true;
        }
        else
        {
            float distance = CalculateDistance.GetDistance(_coreObject.transform, _currentTargetedTransform);

            if (_isDebugActive)
                Debug.Log($"Is target '{_currentTarget.GetName()}' beyond lostRange ({_targetLostRange}): {distance > _targetLostRange}\n[Calculated distance: {distance}]");

            return distance > _targetLostRange;
        }
    }
    private IIdentity GetClosestDetection()
    {
        if (_detectedUnits.Count == 0)
        {
            Debug.LogWarning("Attempted to get the closest detection, but none exist. This shouldn't happen. " +
                "Did the internal detections list get cleared somehow? Or it didn't get initialized properly. returning null");
            return null;
        }

        //return the only detection, if we only have one
        if (_detectedUnits.Count == 1)
            return _detectedUnits.First().Key;

        //return the detection with the shortest distance from our coreObject
        
        bool firstIteration = true;
        foreach (KeyValuePair<IIdentity,float> entry in _detectedUnits)
        {
            if (firstIteration)
            {
                firstIteration = false;
                _cachedClosest = entry;
                continue;
            }

            if (_cachedClosest.Value > entry.Value)
                _cachedClosest = entry;
        }

        Debug.Log($"Closest Detection: {_cachedClosest.Key.GetGameObject().name}");
        return _cachedClosest.Key;
    }
    private void AtkNearestEntity()
    {
        //if we have a target, attack it
        if (_currentTarget != null)
        {
            if (!IsTargetStillValid())
            {
                ClearTarget();
                StopMoving();
                return;
            }

            //Lose the target if it's out of range
            if (IsTargetBeyondLostRange())
            {
                ClearTarget();
                StopMoving();
                return;
            }

            //else Atk Target if it's within atkRange
            if (_atkDriver.IsTargetInRange(_currentTarget, _atkName))
            {
                UpdateVerb($"Attacking Target ({_currentTarget.GetName()})");

                StopMoving();
                _atkDriver.EnterAttack(_atkName);
            }

            //else keep approaching the target
            else
            {
                UpdateVerb($"Pursuing Target ({_currentTarget.GetName()})");
                _moveAbility.MoveToPosition(_currentTargetedTransform.position);
            }

        }
    }
    private bool IsTargetStillValid()
    {
        if (_currentTargetedTransform == null)
            return false;

        if (_currentTarget == null)
            return false;

        if (_currentTarget.IsAttackable())
            return true;
        else return false;
    }
    private void StopMoving()
    {
        if (_moveAbility.IsMoving())
            _moveAbility.CancelMovement();
    }

    private void TrackDetections(HashSet<IIdentity> detections)
    {
        //Don't know how this case would happen, but ignore it if it happens
        if (detections.Count == 0)
            return;

        //only track detections if we have no current target
        if (_currentTarget == null)
        {
            _detectedUnits.Clear();

            foreach (IIdentity identity in detections)
            {
                //filter out Ignored entities and Unattackable entities
                if (!_ignoredIds.Contains(identity.GetID()) && identity.IsAttackable())
                {
                    //add the detection to the collection
                    _cachedDistance = CalculateDistance.GetDistance(_coreObject.transform, identity.GetGameObject().transform);
                    _detectedUnits.Add(identity, _cachedDistance);
                }
            }

            //stop here if no detections made it past the filters
            if (_detectedUnits.Count == 0)
                return;

            //Debug.Log($"TrackDetections Checkpoint! ({_detectedUnits.Count} detections tracked)");

            //trigger the 'OnTriggered' event if we aren't driving the Unit
            //this might get the unit's attention if it isn't driving a behavior with higher priority
            if (_isDrivingUnit == false)
            {
                //Note, all of our detections are still saved, at the moment
                //if the unitBrain chooses to Start this behavior as a driver,
                //then we'll reference this our detections to select a new target
                TriggerOnTriggeredEvent();
                return;
            }

            //We must already be driving, but need a new target
            //select the closest target from our current detections
            SetTarget(GetClosestDetection());

        }
        
    }
    protected override void StartBehavior()
    {
        base.StartBehavior();

        //this event 
        SetTarget(GetClosestDetection());
    }

    protected override void ResetBehavior()
    {
        base.ResetBehavior();
        ClearTarget();
        _isAttacking = false;
    }

    private void RespondToAtkEntered() { if (_isDrivingUnit) _isAttacking = true; }
    private void RespondToAtkExited() {  if (_isDrivingUnit) _isAttacking = false; }



    //externals




}
