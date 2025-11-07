using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[Serializable]
public struct AnimPhysicsBodyStruct
{
    public Transform animationTransform;
    public Transform physicsTransform;

}

public class BindPhysicsToAnimTransform : MonoBehaviour
{
    [SerializeField] private RagdollSwitch _ragdollSwitch;
    [SerializeField] private List<AnimPhysicsBodyStruct> _bodyParts = new List<AnimPhysicsBodyStruct>();



    private void Update()
    {
        if (!_ragdollSwitch.RagdollMode())
            BindPhysicsToAnimationParts();
    }


    //internals
    private void BindPhysicsToAnimationParts()
    {
        foreach (AnimPhysicsBodyStruct part in _bodyParts)
        {
            part.physicsTransform.position = part.animationTransform.position;
            part.physicsTransform.rotation = part.animationTransform.rotation;
        }
    }




    //externals




}
