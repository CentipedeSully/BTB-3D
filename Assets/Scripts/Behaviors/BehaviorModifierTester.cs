using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviorModifierTester : MonoBehaviour
{
    [SerializeField] UnitBrain _unitBrain;
    [SerializeField] private testbehavior _targetTestBehavior;
    private IBehavior _targetBehavior;
    [SerializeField] private bool _cmdAddBehavior;
    [SerializeField] private bool _cmdRemoveBehavior;






    private void Update()
    {
        if (_cmdAddBehavior)
        {
            _cmdAddBehavior = false;
            _targetBehavior = _targetTestBehavior;
            _unitBrain.AddNewBehvior(_targetBehavior);
        }

        if (_cmdRemoveBehavior)
        {
            _cmdRemoveBehavior = false;
            _targetBehavior = _targetTestBehavior;
            _unitBrain.RemoveExistingBehavior(_targetBehavior);
        }
    }
}
