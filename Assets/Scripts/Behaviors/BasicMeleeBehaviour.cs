using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicMeleeBehaviour : AbstractAttack
{
    [Header("Basic Melee Settings")]
    [SerializeField] private HitScanner _hitscanner;
    [SerializeField] private List<int> _unitsHit = new List<int>();





    protected override void SetupChildOnEnableUtils()
    {
        OnHitStepEntered += ClearOldHitData;
        OnHitStepEntered += _hitscanner.ActivateHitScanner;
        OnCooldownEntered += _hitscanner.DeactivateHitScanner;
        OnAtkInterrupted += _hitscanner.DeactivateHitScanner;
        _hitscanner.OnHitsDetected += SaveHitUnits;
    }

    protected override void SetupChildOnDisableUtils()
    {
        OnHitStepEntered -= ClearOldHitData;
        OnHitStepEntered -= _hitscanner.ActivateHitScanner;
        OnCooldownEntered -= _hitscanner.DeactivateHitScanner;
        OnAtkInterrupted -= _hitscanner.DeactivateHitScanner;
        _hitscanner.OnHitsDetected -= SaveHitUnits;
    }

    protected override void SetupChildStartUtils()
    {
        _hitscanner.SetAtkBehavior(this);
    }

    private void SaveHitUnits(HashSet<int> hitsDetected)
    {

        //create a copy of the data, don't copy the reference
        foreach (int hit in hitsDetected)
        {
            if (!_unitsHit.Contains(hit))
                _unitsHit.Add(hit);
        }
    }

    private void ClearOldHitData()
    {
        _unitsHit.Clear();
    }
}
