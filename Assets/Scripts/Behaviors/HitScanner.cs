using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitScanner : MonoBehaviour
{
    [SerializeField] private Transform _scanStart;
    [SerializeField] private Transform _scanEnd;
    [SerializeField] private float _scanRadius;
    [SerializeField] private bool _isHitScanningActive;
    private Collider[] _hitsDetected;
    private IAttackable _detectedAttackable;
    private IAttack _attack;
    private HashSet<int> _detectedAttackableIDs = new HashSet<int>();

    public delegate void HitsDetectedEvent(IAttackable detectedAttackable);
    public event HitsDetectedEvent OnAttackableDetected;

    private void Start()
    {
        _scanStart.gameObject.SetActive(false);
        _scanEnd.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_isHitScanningActive)
            ScanForHits();
    }

    private void ScanForHits()
    {
        _detectedAttackableIDs.Clear();
        _hitsDetected = Physics.OverlapCapsule(_scanStart.position, _scanEnd.position, _scanRadius,_attack.GetHittableLayers());
        

        foreach (Collider collider in _hitsDetected)
        {
            //cache the attackle behaviour for clarity
            _detectedAttackable = collider.GetComponent<IAttackable>();

            //raise the detection event if an attackable behaviour exists on the detected object
            //Don't raise the event if we're supposed to ignore this attackable
            if (_detectedAttackable != null)
            {
                
                int detectedId = _detectedAttackable.GetUnitID();

                //only raise event if this isn't on the ignore list
                if (!_attack.GetIgnoreList().Contains(detectedId))
                    OnAttackableDetected?.Invoke(_detectedAttackable);
            }
        }
            
    }


    public void ActivateHitScanner() 
    { 
        _isHitScanningActive = true;
        _scanStart.gameObject.SetActive(true);
        _scanEnd.gameObject.SetActive(true);
    }
    public void DeactivateHitScanner() 
    {
        _isHitScanningActive = false;
        _scanStart.gameObject.SetActive(false);
        _scanEnd.gameObject.SetActive(false);
    }

    public void SetAtkBehavior(IAttack atk)
    {
        _attack = atk;
    }
}
