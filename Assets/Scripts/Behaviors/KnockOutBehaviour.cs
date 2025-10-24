using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class KnockOutBehaviour : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool _isKnockedOut = false;
    
    [SerializeField] private Vector2 _yThrowForceRange = new(1,7);
    [SerializeField] private Vector2 _xzThrowForceRange = new(1, 7);
    [SerializeField] private Vector3 _calculatedThrowDirection;
    //[SerializeField] private LayerMask _groundLayers;

    [Header("Debug")]
    [SerializeField] private bool _isDebugActive = true;
    [SerializeField] private bool _cmdKnockOutUnit = false;
    [SerializeField] private bool _cmdReviveUnit = false;


    private NavMeshAgent _navMeshAgent;
    private Rigidbody _rigidbody;
    private UnitBehavior _unitBehavior;




    //monobehaviours
    private void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _rigidbody = GetComponent<Rigidbody>();

        TransferControlToNavAgent();
    }

    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();
    }






    //internals
    private void TransferControlToPhysics()
    {
        if (_navMeshAgent!= null && _rigidbody != null)
        {
            _navMeshAgent.enabled = false;
            _rigidbody.isKinematic = false;
        }
       
    }
    private void TransferControlToNavAgent()
    {
        if ( _navMeshAgent!= null && _rigidbody != null)
        {
            _navMeshAgent.enabled = true;
            _rigidbody.isKinematic = true;
        }
    }
    private static float RandomizeValue(float min, float max){return Random.Range(min, max);}
    private float RandomizeVerticalThrow() { return RandomizeValue(_yThrowForceRange.x, _yThrowForceRange.y); }
    private float RandomizeHorizontalThrow() { return RandomizeValue(_xzThrowForceRange.x, _xzThrowForceRange.y); }
    private Vector3 RandomizeThrowDirection() { return new Vector3(RandomizeValue(0, 1), 0, RandomizeValue(0, 1)).normalized; }





    //externals
    public void KnockOutUnit()
    {
        if (!_isKnockedOut)
        {
            _isKnockedOut = true;
            TransferControlToPhysics();

            //randomize the throw force and direction
            _calculatedThrowDirection = RandomizeThrowDirection() + new Vector3(RandomizeHorizontalThrow(), RandomizeVerticalThrow(), RandomizeHorizontalThrow());
            _rigidbody.AddForce(_calculatedThrowDirection, ForceMode.Impulse);
            _rigidbody.AddTorque(_calculatedThrowDirection, ForceMode.Force);

        }
    }
    public void ReviveUnit()
    {
        if (_isKnockedOut)
        {
            TransferControlToNavAgent();
            _isKnockedOut = false;
        }
    }

    //debug
    private void ListenForDebugCommands()
    {
        if (_cmdKnockOutUnit)
        {
            _cmdKnockOutUnit = false;
            KnockOutUnit();
            return;
        }

        if (_cmdReviveUnit)
        {
            _cmdReviveUnit = false;
            ReviveUnit();
            return;
        }
    }
}
