using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicAtk : MonoBehaviour
{
    [Header("Animation Setup")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _atkClipName;
    [SerializeField] private string _atkParamName;
    [SerializeField] private string _atkSpeedName;
    [SerializeField] private float _normalizedWarmEnd;
    [SerializeField] private float _normalizedHitEnd;
    private float _normalizedCooldownEnd = 1;

    [Header("Atk Settings")]
    [SerializeField] private float _atkSpeed = 1;

    [Header("Watch States")]
    [SerializeField] private float _animDuration;
    [SerializeField] private float _warmExitTime;
    [SerializeField] private float _hitExitTime;
    [SerializeField] private float _coolExitTime;

    


    [Header("Debug")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private float _paramAtkSpeed = 1;
    [SerializeField] private bool _cmdSetAtkSpeed;
    [SerializeField] private bool _cmdRecalculateSyncTimes;

    private static List<AnimationClip> _clipsFoundDuringLookup = new();

    //monobehaviours
    private void Start()
    {
        SetAtkSpeed(_atkSpeed);
    }
    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();
    }


    //internals
    private void CalculateAtkAnimSyncTimes()
    {
        _animDuration = GetAnimationClip(_animator, _atkClipName).length / _atkSpeed;
        _warmExitTime = _animDuration * _normalizedWarmEnd / _atkSpeed;
        _hitExitTime = _animDuration * _normalizedHitEnd / _atkSpeed;
        _coolExitTime = _animDuration;
    }
    private void UpdateAtkSpeed()
    {
        _animator.SetFloat(_atkSpeedName, _atkSpeed);
        CalculateAtkAnimSyncTimes();
    }



    //externals
    public void SetAtkSpeed(float newValue)
    {
        _atkSpeed = newValue;
        UpdateAtkSpeed();
    }
    public float GetAtkSpeed() {  return _atkSpeed; }

    


    //debug
    private void ListenForDebugCommands()
    {
        if (_cmdSetAtkSpeed)
        {
            _cmdSetAtkSpeed = false;
            SetAtkSpeed(_paramAtkSpeed);
        }
        if (_cmdRecalculateSyncTimes)
        {
            _cmdRecalculateSyncTimes = false;
            CalculateAtkAnimSyncTimes();
        }
    }



    //other utilities
    public static AnimationClip GetAnimationClip(Animator animator, string clipName)
    {
        if (animator == null)
        {
            Debug.LogError("Attempted to lookup animationclip from null animator. Returning null");
            return null;
        }

        _clipsFoundDuringLookup.Clear();

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                _clipsFoundDuringLookup.Add(clip);
        }


        if (_clipsFoundDuringLookup.Count == 0)
        {
            Debug.LogWarning($"No animationClips found with name '{clipName}'. Returning null");
            return null;
        }

        if (_clipsFoundDuringLookup.Count > 1)
        {
            Debug.LogWarning($"Found multiple animationClips with name '{clipName}'. Returning the first found");
            return _clipsFoundDuringLookup[0];
        }

        return _clipsFoundDuringLookup[0];
    }

}
