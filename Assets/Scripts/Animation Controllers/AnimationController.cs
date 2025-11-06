using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [SerializeField] private RagdollSwitch _ragdollSwitch;

    [Header("Debugging")]
    [SerializeField] private bool _isDebugActive = false;


    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();
    }


    public void GoRagdoll()
    {
        _ragdollSwitch.SetRagdollMode(true);
    }

    public void StopRagdoll()
    {
        _ragdollSwitch.SetRagdollMode(false);
    }





    private void ListenForDebugCommands()
    {

    }
}
