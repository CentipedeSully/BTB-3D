using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;



public class CoreMovement : MonoBehaviour
{
    //declarations
    [Tooltip("How close is 'close enough' before a move order is considered fulilled")]
    [SerializeField] private float _closeEnoughRange = .25f;
    [SerializeField] private float _speed;
    [SerializeField] private bool _isMoving = false;


    private NavMeshAgent _navAgent;
    
    
    public Action OnMoveStarted;
    public Action OnMoveEnded;





    //monobehaviours
    private void Awake()
    {
        InitializeReferences();
        InitializeUtilities();
    }








    //internals
    private void InitializeReferences()
    {
        _navAgent = GetComponent<NavMeshAgent>();
    }

    private void InitializeUtilities()
    {
        _isMoving = false;
        _navAgent.speed = _speed;
        _navAgent.stoppingDistance = _closeEnoughRange;
    }







    //externals
    public bool IsMoving() {  return _isMoving; }
    public void MoveToPosition(Vector3 newPosition)
    {
        if (transform.position != newPosition)
        {
            _navAgent.SetDestination(newPosition);

            if (!_isMoving)
            {
                _isMoving = true;
                OnMoveStarted?.Invoke();
            }
        }
    }
    public void CancelMovement()
    {
        if (!_isMoving)
            return;

        _isMoving = false;
        _navAgent.isStopped = true;
        _navAgent.ResetPath();

        OnMoveEnded?.Invoke();
    }








}
