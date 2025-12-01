using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;




public enum UnitBehaviorState
{
    unset,
    KOed,
    Idle,
    Moving,
    Interacting
}


public enum InteractableType
{
    unset,
    Object,
    Unit
}

public interface IIdentity
{
    GameObject GetGameObject();
    string GetName();
    int GetID();
    InteractableType GetInteractableType();
    IInteractable GetInteractableInterface();
}

public interface IInteractable
{
    string GetName();
    GameObject GetGameObject();
    Vector3 GetPosition();
    InteractableType GetInteractableType();
    IIdentity GetIdentityInterface();
    

    
}

public interface IInteractionBehavior
{
    GameObject GetGameObject();
    string GetInteractionVerb();
    bool IsInteractionInProgress();
    void PerformInteraction();
    void InterruptInteraction();
    event Action OnInteractionCompleted;
    bool IsTargetInRange(IInteractable interactible);
}

public class UnitBehavior : MonoBehaviour , IIdentity, IInteractable
{
    //Declarations
    [Header("Identity Settings")]
    [SerializeField] private int _unitId;
    [SerializeField] private string _unitName;
    [SerializeField] private InteractableType _interactableType = InteractableType.Unit;

    [Header("Ai behaviour Settings")]
    [SerializeField] private string _currentInteractionVerb = "doing nothing";
    private string _defaultInteractionVerb = "[doing nothing]";
    [SerializeField] private IInteractionBehavior _currentPerformingInteraction = null;
    [SerializeField] private NavMeshAgent _navAgent;
    [SerializeField] private UnitBehaviorState _unitState;
    [SerializeField] private float _closeEnoughDistance;
    [SerializeField] private float _interactDistance;
    [SerializeField] private bool _isInteracting = false;
    private GameObject _targetGameObject;
    private IInteractable _targetInteractable;


    private AnimationController _animController;
    private IAttack _attack;
    private HealthBehavior _healthBehavior;
    private Vector3 _targetPosition;
    
    private KnockOutBehaviour _knockoutBehaviour;


    [Header("Debug")]
    [SerializeField] private bool _isDebugActive;
    [SerializeField] private bool _debugCancelOrder;



    public delegate void TargetingEvent(IInteractable newTarget);
    public event TargetingEvent OnTargetSet;
    public event TargetingEvent OnTargetCleared;

    public delegate void InteractEvent(IInteractable target);
    public event InteractEvent OnInteractionStarted;
    public event InteractEvent OnInteractionEnded;


    //Monobehaviours
    private void Awake()
    {
        _unitId = gameObject.GetInstanceID();
        _healthBehavior = GetComponent<HealthBehavior>();
        _healthBehavior.SetUnitID(_unitId);
        _attack = GetComponent<IAttack>();
        _attack.SetUnitID(_unitId);
        _animController = GetComponent<AnimationController>();
        _knockoutBehaviour = GetComponent<KnockOutBehaviour>();

        _currentInteractionVerb = _defaultInteractionVerb;
    }
    private void Start()
    {
        _unitState = UnitBehaviorState.Idle;
    }

    private void OnEnable()
    {
        _healthBehavior.OnKoed += EnterKOed;
        _healthBehavior.OnRevived += EndKOed;
        _healthBehavior.OnDamaged += _animController.TriggerDamageTaken;
        OnInteractionStarted += EnterInteractionWithTarget;
    }

    private void OnDisable()
    {
        _healthBehavior.OnKoed -= EnterKOed;
        _healthBehavior.OnRevived -= EndKOed;
        _healthBehavior.OnDamaged -= _animController.TriggerDamageTaken;
        OnInteractionStarted -= EnterInteractionWithTarget;
    }

    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();

        if (_unitState != UnitBehaviorState.KOed)
        {
            ExecuteBehavior();
            Animatebehavior();
        }
            
    }





    //Internals
    private void ClearCurrentOrder()
    {
        if (_unitState != UnitBehaviorState.Idle)
        {
            //perform any special-case cleanups
            switch (_unitState)
            {
                case UnitBehaviorState.Interacting:
                    _targetGameObject = null;
                    break;

                default:
                    break;
            }

            _navAgent.ResetPath();

            if (_unitState != UnitBehaviorState.KOed)
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


            case UnitBehaviorState.Interacting:

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
                    if (_targetInteractable != null)
                    {
                        if (!_isInteracting)
                        {
                            _isInteracting = true;
                            OnInteractionStarted?.Invoke(_targetInteractable);
                        }
                        
                        ///
                        /// let some other script handle the actual interaction process.
                        /// When it finishes (or an interruption occurs), it'll trigger this script's
                        /// 'OnInteractionEnded' event
                        
                    }

                    else
                    {
                        Debug.Log("Failed to interact. Canceling order");
                        ClearCurrentOrder();
                        break;
                    }
                }

                //keep approaching until we're close enough
                else
                    ApproachPosition(_targetInteractable.GetPosition());
                break;


            default:
                break;
        }
    }

    private void Animatebehavior()
    {
        if (_unitState == UnitBehaviorState.Idle)
        {
            _animController.SetMovement(false);
        }
        if (_unitState == UnitBehaviorState.Moving)
        {
            _animController.SetMovement(true);
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

            case UnitBehaviorState.Interacting:
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

    private void ClearCurrentTarget()
    {
        OnTargetCleared?.Invoke(_targetInteractable);
        _targetInteractable = null;
        _targetInteractable = null;
    }

    private void SetAsTarget(IInteractable newTargetInteractable)
    {
        _targetInteractable = newTargetInteractable;
        _targetGameObject = newTargetInteractable.GetGameObject();
        OnTargetSet?.Invoke(_targetInteractable);
    }

    private void EnterInteractionWithTarget(IInteractable targetInteractable)
    {
        if (_targetInteractable== null)
        {
            EndCurrentInteraction();
            return;
        }

        //change the interaction context based on what we're interacting with
        //attack units
        if (_targetInteractable.GetInteractableType() == InteractableType.Unit)
        {
            _currentPerformingInteraction = _attack.GetAsInteractBehavior();
            _currentInteractionVerb = _currentPerformingInteraction.GetInteractionVerb();

            //subscribe to the interaction's completion event
            //_currentPerformingInteraction.OnInteractionCompleted += ;

            LogCurrentInteractionDetails();
            _currentPerformingInteraction.PerformInteraction();
        }
    }

    private void RedetermineNextAction()
    {

    }

    private void ClearInteractionDetails(IInteractable targetInteractable)
    {
        if (_currentPerformingInteraction != null)
        {
            _currentPerformingInteraction = null;
            _currentInteractionVerb = _defaultInteractionVerb;
        }
    }


    //Externals
    public int GetUnitID() { return _unitId; }
    public void MoveToPosition(Vector3 point)
    {
        if (_unitState != UnitBehaviorState.Idle)
        {
            ClearCurrentOrder();
        }

        _unitState = UnitBehaviorState.Moving;
        ApproachPosition(point);
    }

    public void TargetNewInteractable(GameObject newTagetObject)
    {
        //make sure the gameobject isn't null
        if (_targetGameObject == null)
            return;

        //make sure an interactable component exists on the given gameObject
        IInteractable newTargetInteractable = newTagetObject.GetComponent<IInteractable>();
        if (newTargetInteractable == null)
            return;

        //update our targeting data if we currently have no target
        if (_targetInteractable == null)
            SetAsTarget(newTargetInteractable);

        //only update the target if it's different from our current one
        else if (_targetInteractable != newTargetInteractable)
        {
            ClearCurrentTarget();
            SetAsTarget(newTargetInteractable);
        }
    }

    public void EnterKOed()
    {
        if (_unitState != UnitBehaviorState.KOed)
        {
            ClearCurrentOrder();
            _unitState = UnitBehaviorState.KOed;
            _knockoutBehaviour.KnockOutUnit();
        }
    }
    public void EndKOed()
    {
        if (_unitState == UnitBehaviorState.KOed)
        {
            _knockoutBehaviour.ReviveUnit();
            _unitState = UnitBehaviorState.Idle;
        }
    }

    public UnitBehaviorState GetCurrentState(){return _unitState;}

    public bool IsInteracting() 
    {
        if (_currentPerformingInteraction == null)
            return false;
        else 
            return _currentPerformingInteraction.IsInteractionInProgress();
    }
    public void EndCurrentInteraction()
    {
        //ignore the command if we aren't interacting with anything
        if (!IsInteracting())
            return;

        _currentPerformingInteraction.InterruptInteraction();
        OnInteractionEnded?.Invoke(_targetInteractable);
    }
    public void LogCurrentInteractionDetails()
    {
        //only Log if debug is active
        if (!_isDebugActive)
            return;

        if (_isInteracting)
            Debug.Log($"{_unitName} is {_currentInteractionVerb} {_targetInteractable.GetName()}");
        else Debug.Log($"{_unitName} is {_defaultInteractionVerb}");
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

    public GameObject GetGameObject() {return gameObject;}

    public string GetName() {return _unitName;}

    public int GetID() {return _unitId;}

    public InteractableType GetInteractableType() { return _interactableType; }

    public Vector3 GetPosition() { return transform.position; }

    public IIdentity GetIdentityInterface() { return this; }

    public IInteractable GetInteractableInterface() { return this; }
}
