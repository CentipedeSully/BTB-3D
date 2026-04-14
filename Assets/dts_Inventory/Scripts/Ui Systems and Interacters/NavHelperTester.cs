using dtsInventory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavHelperTester : MonoBehaviour
{
    [SerializeField] List<GameObject> _selectablesToTest = new List<GameObject>();
    [SerializeField] private bool _startTest = false;


    private void Update()
    {
        if (_startTest)
        {
            Debug.Log("Beginning SetNavTarget stress test...");
            _startTest = false;
            for (int i = 0; i < _selectablesToTest.Count; i++)
            {
                NavHelper.SetCurrentNavObject( _selectablesToTest[i] );
                
            }
            for (int i = 0; i < _selectablesToTest.Count; i++)
            {
                NavHelper.SetCurrentNavObject(_selectablesToTest[i]);

            }
            Debug.Log($"Is queuing in progress: {NavHelper.IsNavTargetingQueueInProgress()}");
        }
    }
}
