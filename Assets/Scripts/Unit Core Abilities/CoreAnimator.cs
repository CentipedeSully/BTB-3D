using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreAnimator : MonoBehaviour
{
    [SerializeField] private Animator _bodyAnimator;
    [SerializeField] private string _onDamagedParam = "OnDamageTaken";
    [SerializeField] private string _onMovementParam = "MoveSpeed";
    [SerializeField] private float _nonMovementValue = 0;
    [SerializeField] private float _basicMovementValue = .2f;
    [SerializeField] private float _fastMovementValue = 1f;
    private CoreHealth _healthAbility;
    private CoreMovement _movementAbility;



    //monobehaviours
    private void Awake()
    {
        _healthAbility = GetComponent<CoreHealth>();
        _movementAbility = GetComponent<CoreMovement>();
    }
    private void OnEnable()
    {
        _movementAbility.OnMoveLevelUpdated += UpdateMoveLevel;
        _healthAbility.OnDamaged += TriggerDamaged;
    }

    private void OnDisable()
    {
        _movementAbility.OnMoveLevelUpdated -= UpdateMoveLevel;

    }

    //internals
    private void UpdateMoveLevel(MoveSpeedLevel moveLvl) 
    {
        if (moveLvl == MoveSpeedLevel.None)
            SetFloat(_onMovementParam, _nonMovementValue);
        else if (moveLvl == MoveSpeedLevel.Basic)
            SetFloat(_onMovementParam, _basicMovementValue);
        else if (moveLvl == MoveSpeedLevel.Fast)
            SetFloat(_onMovementParam, _fastMovementValue);
    }
    private void TriggerDamaged(){ SetTrigger(_onDamagedParam); }


    //externals
    public void SetBool(string name,bool value) { _bodyAnimator.SetBool(name, value); }
    public void SetFloat(string name, float value) {  _bodyAnimator.SetFloat(name, value); }
    public void SetTrigger(string name) { _bodyAnimator.SetTrigger(name); }
}
