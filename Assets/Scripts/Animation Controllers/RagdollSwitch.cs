using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollSwitch : MonoBehaviour
{
    //declarations
    [Header("Settings")]
    [SerializeField] private bool _ragdollMode = false;
    [SerializeField] private List<Rigidbody> _rbs = new List<Rigidbody>();
    [SerializeField] private Dictionary<Rigidbody,(Vector3,Vector3)> _originalPositionsAndRotations = new();

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

            //update the dictionary if the rb is new
            if (!_originalPositionsAndRotations.ContainsKey(rb))
                _originalPositionsAndRotations.Add(rb, (pos, eulers));
            else //otherwise just replace the current transform as the new original
                _originalPositionsAndRotations[rb] = (pos, eulers);
            
        }
    }

    private void RemoveKinematicsFromRbs()
    {
        foreach (Rigidbody rb in _rbs)
            rb.isKinematic = false;
    }

    private void ResetRbsToOriginalPositions()
    {

        if (_originalPositionsAndRotations == null || _originalPositionsAndRotations.Count == 0)
            SaveOriginalTransforms();

        for (int i = 0; i < _rbs.Count; i++)
        {
            //get the corresponding data for each rb
            Rigidbody rb = _rbs[i];
            (Vector3,Vector3) posAndEuler = _originalPositionsAndRotations[rb];


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

    public Vector3 GetRagdollPosition()
    {
        return _rbs[0].position;
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
