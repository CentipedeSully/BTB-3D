using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class LookAtController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MultiAimConstraint _aimConstraint;
    [SerializeField] private Transform _lookAtTarget;

    [Header("Setting")]
    [SerializeField] private bool _isLookingAtATarget = false;


    //monobehaviours
    




    //internals



    //externals
    public void ToggleLookAt(bool newValue)
    {
        _isLookingAtATarget=newValue;

        if (_isLookingAtATarget)
            _aimConstraint.weight = 1;
        else _aimConstraint.weight = 0;
    }

    public void SetLookAtPosition(Vector3 newPosition)
    {
        _lookAtTarget.position = newPosition;
    }


    //Debug




}
