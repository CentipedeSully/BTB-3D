using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollSwitch : MonoBehaviour
{
    //declarations
    [Header("Settings")]
    [SerializeField] private bool _ragdollMode = false;
    [SerializeField] private List<Rigidbody> _rbs = new List<Rigidbody>();
    [SerializeField] private List<(Vector3, Vector3)> _originalPositionsAndRotations = new List<(Vector3, Vector3)>();

    [Header("Debugging")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private bool _cmdToggleRagdoll = false;



    //monobehaviours
    private void Awake()
    {
        SaveOriginalTransforms();
    }

    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();
    }



    //internals
    private void SaveOriginalTransforms()
    {
        for (int i = 0;i < _rbs.Count; i++)
        {
            Rigidbody rb = _rbs[i];
            Vector3 pos = rb.transform.localPosition;
            Vector3 eulers = rb.transform.localEulerAngles;

            _originalPositionsAndRotations.Add((pos, eulers));
        }
    }

    private void RemoveKinematicsFromRbs()
    {
        foreach (Rigidbody rb in _rbs)
            rb.isKinematic = false;
    }

    private void ResetRbsToOriginalPositions()
    {
        for (int i = 0; i < _rbs.Count; i++)
        {
            //get the corresponding data for each rb
            Rigidbody rb = _rbs[i];
            (Vector3,Vector3) posAndEuler = _originalPositionsAndRotations[i];


            rb.isKinematic = true;
            rb.transform.localPosition = posAndEuler.Item1;
            rb.transform.eulerAngles= posAndEuler.Item2;
            
        }
    }





    //externals
    public void SetRagdollMode(bool newMode)
    {
        _ragdollMode = newMode;

        if (_ragdollMode)
            RemoveKinematicsFromRbs();
        else 
            ResetRbsToOriginalPositions();
    }



    //Debug
    private void ListenForDebugCommands()
    {
        if (_cmdToggleRagdoll)
        {
            _cmdToggleRagdoll = false;
            SetRagdollMode(!_ragdollMode);
        }
    }

}
