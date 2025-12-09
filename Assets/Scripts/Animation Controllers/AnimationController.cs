using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Animator _animator;
    [SerializeField] private float _onHitTriggerResetTimer = .1f;
    [SerializeField] private RagdollSwitch _ragdollSwitch;

    [Header("Debugging")]
    [SerializeField] private bool _isDebugActive = false;

    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();
    }

   public void SetMovement(bool value)
    {
        if (value == true)
            _animator.SetFloat("MoveSpeed", .2f);
        else _animator.SetFloat("MoveSpeed", 0);
    }

    public void TriggerDamageTaken()
    {
        _animator.SetTrigger("OnDamageTaken");
        Invoke(nameof(ResetOnHitTrigger), _onHitTriggerResetTimer);
        
    }

    private void ResetOnHitTrigger() { _animator.ResetTrigger("OnDamageTaken"); }


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
