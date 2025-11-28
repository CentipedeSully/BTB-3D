using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicMeleeBehaviour : AbstractAttack
{
    [Header("Basic Melee Settings")]
    [SerializeField] private HitScanner _hitscanner;
    [SerializeField] private List<int> _unitsAttacked = new List<int>();
    private HashSet<int> _unitsDamaged = new HashSet<int>();





    protected override void SetupChildOnEnableUtils()
    {
        OnHitStepEntered += ResetHitData;
        OnHitStepEntered += _hitscanner.ActivateHitScanner;
        OnCooldownEntered += _hitscanner.DeactivateHitScanner;
        OnAtkInterrupted += _hitscanner.DeactivateHitScanner;
        _hitscanner.OnAttackableDetected += ProcessDetection;
    }

    protected override void SetupChildOnDisableUtils()
    {
        OnHitStepEntered -= ResetHitData;
        OnHitStepEntered -= _hitscanner.ActivateHitScanner;
        OnCooldownEntered -= _hitscanner.DeactivateHitScanner;
        OnAtkInterrupted -= _hitscanner.DeactivateHitScanner;
        _hitscanner.OnAttackableDetected -= ProcessDetection;
    }

    protected override void SetupChildStartUtils()
    {
        _hitscanner.SetAtkBehavior(this);
    }

    private void ProcessDetection(IAttackable attackableBehaviour)
    {

        //damage the attackable if it didn't get damaged yet
        if (!_unitsDamaged.Contains(attackableBehaviour.GetUnitID()))
        {
            attackableBehaviour.TakeDamage(_damage);

            //add this unit as already damaged to prevent multi-hits
            _unitsDamaged.Add(attackableBehaviour.GetUnitID());

            //also add the unit's ID to the inspector variable, for visibility
            _unitsAttacked.Add(attackableBehaviour.GetUnitID());
        }
    }

    private void ResetHitData()
    {
        _unitsAttacked.Clear();
        _unitsDamaged.Clear();
    }
}
