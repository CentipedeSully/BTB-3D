using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;



[Serializable]
public struct AnimPhysicsBodyStruct
{
    [Tooltip("The animation body that should directly influence the transform of the physical body. This may be influenced by other sources, and will influence the physics body.")]
    public Transform animationTransform;
    [Tooltip("The physics object that'll mimic the above animation object")]
    public Transform physicsTransform;
    public Rigidbody rb;
    [Tooltip("The transform that will follow the physics object. This transform should be setup as a source for an animation-rig's constraint.")]
    public Transform animationPositionSource;
    public Transform animationRotationSource;

}

public class BindPhysicsToAnimTransform : MonoBehaviour
{
    [SerializeField] private RagdollSwitch _ragdollSwitch;
    [SerializeField] private List<AnimPhysicsBodyStruct> _bodyParts = new List<AnimPhysicsBodyStruct>();
    [SerializeField] private float _collisionRecoveryTime = .5f;
    private float _currentRecoveryTime = 0;
    private bool _isRecovering = false;


    private void Start()
    {
    }

    private void Update()
    {
        if (!_ragdollSwitch.RagdollMode())
            BindPhysicsToAnimationParts();
    }


    //internals
    /// <summary>
    /// Used to make the physics (visual) body follow the invisible animation body
    /// </summary>
    private void BindPhysicsToAnimationParts()
    {
        foreach (AnimPhysicsBodyStruct part in _bodyParts)
        {
            part.physicsTransform.position = part.animationTransform.position;
            part.physicsTransform.rotation = part.animationTransform.rotation;
        }
    }

    /// <summary>
    /// Used to make the physics (visual) body influence the animation body
    /// </summary>
    private void BindAnimSourceToPhysics()
    {
        foreach (AnimPhysicsBodyStruct part in _bodyParts)
        {
            part.animationPositionSource.position = part.physicsTransform.position;
            part.animationRotationSource.rotation = part.physicsTransform.rotation;
        }
    }



}
