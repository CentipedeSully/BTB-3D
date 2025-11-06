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
    [SerializeField] private AnimationController _animController;
    [SerializeField] private Rigidbody _throwBody;
    //[SerializeField] private LayerMask _groundLayers;

    [Header("Debug")]
    [SerializeField] private bool _isDebugActive = true;
    [SerializeField] private bool _cmdKnockOutUnit = false;
    [SerializeField] private bool _cmdReviveUnit = false;


    private NavMeshAgent _navMeshAgent;
    private Rigidbody _rigidbody;




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
        _navMeshAgent.enabled = false;
        _animController.GoRagdoll();

    }
    private void TransferControlToNavAgent()
    {
        if ( _navMeshAgent!= null && _rigidbody != null)
        {
            _navMeshAgent.enabled = true;
            _throwBody.isKinematic = true;
            _animController.StopRagdoll();
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
            _throwBody.AddForce(_calculatedThrowDirection, ForceMode.Impulse);
            _throwBody.AddTorque(_calculatedThrowDirection, ForceMode.Force);

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
