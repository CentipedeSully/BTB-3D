using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace dtsInventory
{
    public class PlayerController : MonoBehaviour
    {
        //Declarations
        [Header("References")]
        [SerializeField] private MapPointer _mapPointer;


        [SerializeField] private bool _isMoving = false;
        [SerializeField] private GameObject _interactionTarget;
        private IInteractable _interactable;
        private NavMeshAgent _navAgent;
        private RaycastHit[] _raycastHits;
        float _distanceFromTarget = 0;




        //Monobehaviours
        private void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            _mapPointer.OnLClick += CaptureTargetOnClick;
        }

        private void OnDisable()
        {
            _mapPointer.OnLClick -= CaptureTargetOnClick;
        }

        private void Update()
        {
            if (_isMoving)
            {
                if (_interactionTarget != null)
                    EnterInteractionWhenInRange();

                WatchForMoveEnd();
            }
        }




        //internals
        private void CaptureTargetOnClick()
        {
            _raycastHits = _mapPointer.CaptureDetectionsOnPointer();


            if (_raycastHits.Length == 0)
                return;

            //the map pointer autoSorts the detections. Nearest first
            RaycastHit chosedDetection = _raycastHits[0];


            //update our interal interact state if the player clicked an interactible
            IInteractable interactible = chosedDetection.collider.GetComponent<IInteractable>();
            if (interactible != null)
            {
                _interactable = interactible;
                _interactionTarget = _interactable.GetGameObject();
            }
            else
                ClearInteractionTarget();

            MoveToPosition(chosedDetection.point);
        }
        private void EnterInteractionWhenInRange()
        {
            _distanceFromTarget = Mathf.Abs(Vector3.Distance(_interactionTarget.transform.position, transform.position));

            if (_distanceFromTarget <= _interactable.InteractDistance())
            {
                Debug.Log("interaction Triggered");
                _interactable.TriggerInteraction();
                ClearInteractionTarget();
                EndMovement();

            }
        }
        private void WatchForMoveEnd()
        {
            if (_navAgent.isStopped)
                EndMovement();
        }
        private void ClearInteractionTarget()
        {
            _interactable = null;
            _interactionTarget = null;
        }





        //externals
        public void MoveToPosition(Vector3 targetPosition)
        {
            _isMoving = true;
            _navAgent.SetDestination(targetPosition);
        }
        public void EndMovement()
        {
            _isMoving = false;
            _navAgent.isStopped = true;
            _navAgent.ResetPath();

        }
        public bool IsMoving() { return _isMoving; }




    }

}