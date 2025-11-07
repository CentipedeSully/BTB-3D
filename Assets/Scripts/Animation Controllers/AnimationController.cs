using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject _ragdollBody;
    [SerializeField] private GameObject _animationBody;
    [SerializeField] private RagdollSwitch _ragdollSwitch;

    [Header("Debugging")]
    [SerializeField] private bool _isDebugActive = false;


    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();
    }


    public void Ragdoll()
    {
        _ragdollSwitch.SetRagdollMode(true);
    }

    public void EndRagdoll()
    {
        _ragdollSwitch.SetRagdollMode(false);
    }

    public Vector3 GetBodyPosition()
    {
        return _ragdollSwitch.GetRagdollPosition();
    }




    private void ListenForDebugCommands()
    {

    }
}
