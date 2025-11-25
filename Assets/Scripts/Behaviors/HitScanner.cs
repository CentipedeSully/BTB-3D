using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitScanner : MonoBehaviour
{
    [SerializeField] private Transform _scanStart;
    [SerializeField] private Transform _scanEnd;
    [SerializeField] private float _scanRadius;

    [SerializeField] private bool _isHitScanningActive;
    [SerializeField] private LayerMask _hittableLayers;
    [SerializeField] private List<int> _safeIds = new List<int>();
    private Collider[] _hitsDetected;
    private IAttackable _detectedAttackable;
    private List<int> _attackablesDetected = new();

    public delegate void HitsDetectedEvent(List<int> hits);
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
        _attackablesDetected.Clear();
        _hitsDetected = Physics.OverlapCapsule(_scanStart.position, _scanEnd.position, _scanRadius,_hittableLayers);
        

        foreach (Collider collider in _hitsDetected)
        {
            //add the detected attackable if it exists, and hasn't already been detected during this scan
            _detectedAttackable = collider.GetComponent<IAttackable>();
            if (_detectedAttackable != null)
            {
                
                int detectedId = _detectedAttackable.GetUnitID();

                //also filter out the safe and already-detected unit IDs
                if (!_attackablesDetected.Contains(detectedId) && !_safeIds.Contains(detectedId))
                    _attackablesDetected.Add(detectedId);
            }
        }

        if (_attackablesDetected.Count > 0)
            OnHitsDetected?.Invoke(_attackablesDetected);
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

    public void AddSafeUnitID(int id)
    {
        if (!_safeIds.Contains(id))
            _safeIds.Add(id);
    }
    public void RemoveSafeID(int id)
    {
        _safeIds.Remove(id);
    }
}
