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

    public delegate void HitsDetectedEvent(HashSet<int> hits);
    public event HitsDetectedEvent OnHitsDetected;

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
            //cache the attackle behaviour for clarit, if it exists
            _detectedAttackable = collider.GetComponent<IAttackable>();
            if (_detectedAttackable != null)
            {
                
                int detectedId = _detectedAttackable.GetUnitID();

                //only add the detected ID if it's not on the ignore list
                if (!_attack.GetIgnoreList().Contains(detectedId))
                    _detectedAttackableIDs.Add(detectedId);
            }
        }

        if (_detectedAttackableIDs.Count > 0)
            OnHitsDetected?.Invoke(_detectedAttackableIDs);
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
