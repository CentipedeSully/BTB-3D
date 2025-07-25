using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;




public enum UnitBehaviorState
{
    unset,
    Idle,
    Moving,
    AttemptingInteraction
}

public class UnitBehavior : MonoBehaviour
{
    //Declarations
    [SerializeField] private NavMeshAgent _navAgent;
    [SerializeField] private UnitBehaviorState _unitState;
    [SerializeField] private float _closeEnoughDistance;
    [SerializeField] private float _interactDistance;
    private Vector3 _targetPosition;
    private GameObject _targetGameObject;


    [Header("Debug")]
    [SerializeField] private bool _isDebugActive;
    [SerializeField] private bool _debugCancelOrder;



    //Monobehaviours
    private void Start()
    {
        _unitState = UnitBehaviorState.Idle;
    }

    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();

        ExecuteBehavior();
    }





    //Internals
    private void ClearCurrentOrder()
    {
        if (_unitState != UnitBehaviorState.Idle)
        {
            //perform any special-case cleanups
            switch (_unitState)
            {
                case UnitBehaviorState.AttemptingInteraction:
                    _targetGameObject = null;
                    break;

                default:
                    break;
            }

            _navAgent.ResetPath();
            _unitState = UnitBehaviorState.Idle;
        }
    }

    private void ExecuteBehavior()
    {
        switch (_unitState)
        {
            case UnitBehaviorState.Moving:
                if (IsCloseEnough())
                {
                    //Debug.Log($"Satisfied Move Order to {_targetPosition}");
                    ClearCurrentOrder();
                }
                
                break;


            case UnitBehaviorState.AttemptingInteraction:

                //end the order if the target vanished
                if (_targetGameObject == null)
                {
                    ClearCurrentOrder();
                    break;
                }

                //make sure we're close enough for the interaction
                if (IsCloseEnough())
                {
                    _navAgent.ResetPath();
                    InteractibleBehavior interactBehaviour = _targetGameObject.GetComponent<InteractibleBehavior>();

                    //final check to make sure the interactionBehavior actually exists
                    if (interactBehaviour != null)
                    {
                        interactBehaviour.Interact();
                        //Debug.Log($"Satisfied Interaction Order on {_targetGameObject.name}");
                        ClearCurrentOrder();
                    }

                    else
                    {
                        Debug.Log("Failed to interact. Canceling order");
                        ClearCurrentOrder();
                    }
                }
                break;


            default:
                break;
        }
    }

    private bool IsCloseEnough()
    {
        switch (_unitState)
        {
            case UnitBehaviorState.Moving:
                float sqrDistanceFromTarget = (_targetPosition - _navAgent.transform.position).sqrMagnitude;
                float sqrCloseEnoughDistance = _closeEnoughDistance * _closeEnoughDistance;

                if (sqrCloseEnoughDistance >= sqrDistanceFromTarget)
                {
                    return true;
                }
                else return false;

            case UnitBehaviorState.AttemptingInteraction:
                float sqrDistanceFromTarget2 = (_targetPosition - _navAgent.transform.position).sqrMagnitude;
                float sqrInteractDistance = _interactDistance * _interactDistance;

                if (sqrInteractDistance >= sqrDistanceFromTarget2)
                {
                    return true;
                }
                else return false;


            default:
                return true;
        }
        
    }

    private void ApproachPosition(Vector3 position)
    {
        _targetPosition = position;
        _navAgent.SetDestination(position);
    }


    //Externals
    public void MoveToPosition(Vector3 point)
    {
        if (_unitState != UnitBehaviorState.Idle)
        {
            ClearCurrentOrder();
        }

        _unitState = UnitBehaviorState.Moving;
        ApproachPosition(point);
    }

    public void InteractWithInteractible(InteractibleBehavior interactible)
    {
        if (interactible == null) return;

        if (_unitState != UnitBehaviorState.Idle)
        {
            ClearCurrentOrder();
        }

        _unitState = UnitBehaviorState.AttemptingInteraction;
        _targetGameObject = interactible.gameObject;
        ApproachPosition(interactible.transform.position);
    }





    //DEBUG
    private void ListenForDebugCommands()
    {
        if (_debugCancelOrder)
        {
            _debugCancelOrder = false;
            ClearCurrentOrder();
        }
    }




}
