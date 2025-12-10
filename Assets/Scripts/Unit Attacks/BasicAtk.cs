using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct AtkCast
{
    public Transform _castOrigin;
    public CastType _castType;
    public float _radius;
    public Vector3 _rectSize;
    public Transform _capsuleSecondOrigin;

}

public enum CastType
{
    unset,
    Sphere,
    Rect,
    Capsule
}

public class BasicAtk : MonoBehaviour, IAtk
{
    [Header("Animation Setup")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _atkClipName;
    [SerializeField] private string _atkParamName;
    [SerializeField] private string _atkSpeedName;
    [SerializeField] private float _normalizedWarmEnd;
    [SerializeField] private float _normalizedHitEnd;
    private float _normalizedCooldownEnd = 1;

    [Header("Atk Cast Settings")]
    [SerializeField] private LayerMask _detectableLayers;
    [SerializeField] private List<AtkCast> _atkCasts;
    private Collider[] _detections;
    private IIdentity _cachedIdentity;
    private HashSet<IIdentity> _uniqueIdentitiesDetected = new();
    [SerializeField] private bool _isCastingAtk = false;

    [Header("Atk Settings")]
    [SerializeField] private string _atkName = "Basic Atk";
    [SerializeField] private float _atkSpeed = 1;
    

    [Header("Watch States")]
    [SerializeField] private float _animDuration;
    [SerializeField] private float _warmExitTime;
    [SerializeField] private float _hitExitTime;
    [SerializeField] private float _coolExitTime;
    [SerializeField] private List<int> _detectedIdsSerialized = new();
    


    [Header("Debug")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private float _paramAtkSpeed = 1;
    [SerializeField] private bool _cmdSetAtkSpeed;
    [SerializeField] private bool _cmdRecalculateSyncTimes;

    private static List<AnimationClip> _clipsFoundDuringLookup = new();

    public event Action<HashSet<IIdentity>> OnHitsDetected;




    //monobehaviours
    private void Start()
    {
        SetAtkSpeed(_atkSpeed);
    }
    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();

        if (_isCastingAtk)
            CastAtk();
    }


    //internals
    private void CalculateAtkAnimSyncTimes()
    {
        _animDuration = GetAnimationClip(_animator, _atkClipName).length / _atkSpeed;
        _warmExitTime = _animDuration * _normalizedWarmEnd;
        _hitExitTime = _animDuration * _normalizedHitEnd;
        _coolExitTime = _animDuration;
        
    }
    private void UpdateAtkSpeed()
    {
        _animator.SetFloat(_atkSpeedName, _atkSpeed);
        CalculateAtkAnimSyncTimes();
    }
    private void CastAtk()
    {
        //clear the past frame's detection data
        _uniqueIdentitiesDetected.Clear();
        _detectedIdsSerialized.Clear();

        foreach (AtkCast atkcast in _atkCasts)
        {
            //sphere cast if the castType is a sphere
            if (atkcast._castType == CastType.Sphere)
            {
                _detections = Physics.OverlapSphere(atkcast._castOrigin.position, atkcast._radius, _detectableLayers);
                if (_detections.Length > 0)
                    ReadDetections();
                continue;
            }

            //rect cast if the castType is a rect 
            if (atkcast._castType == CastType.Rect)
            {
                _detections = Physics.OverlapBox(atkcast._castOrigin.position,atkcast._rectSize/2,Quaternion.identity,_detectableLayers);
                if (_detections.Length > 0)
                    ReadDetections();
                continue;
            }

            //capsule cast if the castType is a capsule
            if (atkcast._castType == CastType.Capsule)
            {
                _detections = Physics.OverlapCapsule(atkcast._castOrigin.position, atkcast._capsuleSecondOrigin.position, atkcast._radius, _detectableLayers);
                if (_detections.Length > 0)
                    ReadDetections();
                continue;
            }
        }

        if (_uniqueIdentitiesDetected.Count > 0)
        {
            if (_isDebugActive)
                SerializeIdsFromDetectedIdentities();

            OnHitsDetected?.Invoke(_uniqueIdentitiesDetected);
        }
            


    }
    private void ReadDetections()
    {
        foreach (Collider collider in _detections)
        {
            _cachedIdentity = collider.gameObject.GetComponent<IIdentity>();
            if (_cachedIdentity != null)
                _uniqueIdentitiesDetected.Add(_cachedIdentity);
        }

    }
    private void SerializeIdsFromDetectedIdentities()
    {
        foreach (IIdentity identity in _uniqueIdentitiesDetected)
            _detectedIdsSerialized.Add(identity.GetID());
    }



    //externals
    public void SetAtkSpeed(float newValue)
    {
        _atkSpeed = newValue;
        UpdateAtkSpeed();
    }
    public float GetAtkSpeed() {  return _atkSpeed; }
    public void CastAtk(bool newState) { _isCastingAtk = newState; }
    public bool IsCastingAtk() {  return _isCastingAtk; }
    public LayerMask GetLayerMask() { return _detectableLayers; }
    public void SetLayerMask(LayerMask newLayerMask) { _detectableLayers = newLayerMask; }
    public string GetAtkName() { return _atkName; }
    public void SetAtkName(string newName) {  _atkName = newName; }
    public GameObject GetGameObject() { return gameObject; }
    public float GetWarmTime() { return _warmExitTime; }
    public float GetHitTime() { return _hitExitTime; }
    public float GetCoolTime() { return _coolExitTime; }


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
