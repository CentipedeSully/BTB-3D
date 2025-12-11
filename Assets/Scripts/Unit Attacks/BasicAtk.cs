using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
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
    [Tooltip("The name of the animation state in the Animator")]
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

    [Header("RangeCheck")]
    [Tooltip("If the target entity is detected in ANY of these casts [at least one], then the rangeCheck will return true")]
    [SerializeField] private List<AtkCast> _rangeCasts = new();
    [SerializeField] private IIdentity _targetIdentity;
    





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
    [SerializeField] private GameObject _paramTargetWithAnIIdentity;
    [SerializeField] private bool _cmdRangeCheckForTarget;
    [SerializeField] private bool _cmdForceRangeCast;
    [SerializeField] private Transform _paramDistanceTestTransform1;
    [SerializeField] private Transform _paramDistanceTestTransform2;
    [SerializeField] private bool _cmdTestHorizontalDistanceUtilityUsingTarget;
    [SerializeField] private bool _cmdTestVerticalDistanceUtilityUsingTarget;
    [SerializeField] private bool _cmdTestDistanceUtilityUsingTarget;

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
        {
            ListenForDebugCommands();
            if (_cmdForceRangeCast && _paramTargetWithAnIIdentity != null)
                IsEntityInRange(_paramTargetWithAnIIdentity.GetComponent<IIdentity>());
        }
            

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

        CheckAtkCastsForDetections(_atkCasts);

        if (_uniqueIdentitiesDetected.Count > 0)
        {
            if (_isDebugActive)
                SerializeIdsFromDetectedIdentities();

            OnHitsDetected?.Invoke(_uniqueIdentitiesDetected);
        }
            
    }
    private void CheckAtkCastsForDetections(List<AtkCast> atkCastList)
    {
        foreach (AtkCast atkcast in atkCastList)
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
                _detections = Physics.OverlapBox(atkcast._castOrigin.position, atkcast._rectSize / 2, Quaternion.identity, _detectableLayers);
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
    public bool IsEntityInRange(IIdentity entityIdentity)
    {
        if (entityIdentity == null)
        {
            if (_isDebugActive)
                Debug.LogWarning("Attempted to rangeCheck against a null target. Returning false");
            return false;
        }

        //clear the past frame's detection data
        _uniqueIdentitiesDetected.Clear();
        _detectedIdsSerialized.Clear();
        CheckAtkCastsForDetections(_rangeCasts);
        if (_uniqueIdentitiesDetected.Count == 0)
        {
            if (_isDebugActive)
                Debug.Log("target not detected. NOTHING detected in rangeCast");
            return false;
        }



        //if the entity is detected, return true
        foreach (IIdentity identity in _uniqueIdentitiesDetected)
        {
            if (identity.GetID() == entityIdentity.GetID())
            {
                if (_isDebugActive)
                    Debug.Log("Target Detected!");
                return true;
            }
                
        }

        //the entity wasn't found
        if (_isDebugActive)
            Debug.Log("Target not detected");
        return false;
    }
    public string GetAtkAnimationParameterName() { return _atkParamName; }


    public virtual void EnterWarmup() { if (_isDebugActive) Debug.Log("BasicAtk warmupEntered"); }
    public virtual void EnterHitStep() { if (_isDebugActive) Debug.Log("BasicAtk hitstepEntered"); _isCastingAtk = true; }
    public virtual void EnterCooldown() { if (_isDebugActive) Debug.Log("BasicAtk cooldownEntered"); _isCastingAtk = false; }
    public virtual void AtkCompleted() { if (_isDebugActive) Debug.Log("BasicAtk Completed"); }
    public virtual void AtkInterrupted() { if (_isDebugActive) Debug.Log("BasicAtk Interrupted"); _isCastingAtk = false; }


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
        if (_cmdRangeCheckForTarget)
        {
            _cmdRangeCheckForTarget = false;
            IsEntityInRange(_paramTargetWithAnIIdentity.GetComponent<IIdentity>());
        }
        if (_cmdTestDistanceUtilityUsingTarget)
        {
            _cmdTestDistanceUtilityUsingTarget = false;
            Debug.Log($"Distance from Target: {CalculateDistance.GetDistance(_paramDistanceTestTransform1, _paramDistanceTestTransform2)}");
        }
        if (_cmdTestHorizontalDistanceUtilityUsingTarget)
        {
            _cmdTestHorizontalDistanceUtilityUsingTarget= false;
            Debug.Log($"Horizontal Distance from Target: {CalculateDistance.GetHorizontalDistance(_paramDistanceTestTransform1, _paramDistanceTestTransform2)}");
        }
        if (_cmdTestVerticalDistanceUtilityUsingTarget)
        {
            _cmdTestVerticalDistanceUtilityUsingTarget = false;
            Debug.Log($"Vertical Distance from Target: {CalculateDistance.GetVerticalDistance(_paramDistanceTestTransform1, _paramDistanceTestTransform2)}");
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
