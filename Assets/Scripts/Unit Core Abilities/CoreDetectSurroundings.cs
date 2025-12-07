using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CoreDetectSurroundings : MonoBehaviour
{
    [Header("General Detection Settings")]
    [SerializeField] private float _detectionRadius;
    [SerializeField] private Vector3 _detectionOriginOffset = Vector3.zero;
    [SerializeField] private LayerMask _detectionLayers;
    [SerializeField] private Color _gizmoColor = Color.yellow;

    [Header("Watch States")]
    [SerializeField] private bool _isDetectionActive = false;
    [SerializeField] private int _selfID;
    [SerializeField] private List<int> _detectedIds = new List<int>();
    [SerializeField] private List<int> _ignoreIdsList = new List<int>();
    private HashSet<IIdentity> _uniqueDetectedIdentities = new();
    private HashSet<int> _uniqueDetectedIds = new();
    private Vector3 _castPosition;
    private Collider[] _detections;

    [Header("Debug Commands")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private int _paramId;
    [SerializeField] private bool _cmdIgnoreId = false;
    [SerializeField] private bool _cmdRemoveIgnoreId = false;
    private string _debugResponseLogString = "";


    //cached closest detection utils
    [SerializeField] int _closestID;
    IIdentity _closestIdentity;
    Transform _closestTransform;
    Transform _currentTransform;
    List<IIdentity> _identitiesList = new();

    //self detect utils
    private bool _detectSelfAttempted = false;
    private int _currentDetectionID;
    private IIdentity _currentDetectedIdentity;

    //events
    /// <summary>
    /// Provides the identity interface of the closest detected entity
    /// </summary>
    public Action<IIdentity> OnEntityDetected;
    /// <summary>
    /// Provides the identity interfaces of all detected entities
    /// </summary>
    public Action<HashSet<IIdentity>> OnEntitiesDetected;

    




    private void OnEnable()
    {
        OnEntityDetected += LogClosestEntityDetectionResponse;
        OnEntitiesDetected += LogEntitiesDetectedResponse;
    }

    private void OnDisable()
    {
        _detectSelfAttempted = false;
        OnEntityDetected -= LogClosestEntityDetectionResponse;
        OnEntitiesDetected -= LogEntitiesDetectedResponse;
    }

    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();

        if (_isDetectionActive)
            DetectSurroundings();
    }

    private void OnDrawGizmos()
    {
        if (_isDetectionActive)
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawSphere(_castPosition, .2f);
            Gizmos.DrawWireSphere(_castPosition, _detectionRadius);
        }
    }






    //internals
    private void DetectSelfId()
    {
        IIdentity selfIdentity = GetComponent<IIdentity>();
        if (selfIdentity != null)
        {
            _selfID = selfIdentity.GetID();
            _ignoreIdsList.Add(_selfID);
        }
        _detectSelfAttempted = true;
            
    }
    private void DetectSurroundings()
    {
        if (!_detectSelfAttempted)
            DetectSelfId();

        _castPosition = transform.position + _detectionOriginOffset;
        _detections = Physics.OverlapSphere(_castPosition,_detectionRadius,_detectionLayers);
        ParseDetectedColliders();
        SerializeDetectedIds();

        RaiseDetectionEventForClosestEntitiy();

        //raise detection event for All Entities
        if (_uniqueDetectedIdentities.Count > 0)
            OnEntitiesDetected?.Invoke(_uniqueDetectedIdentities);
    }
    private void ParseDetectedColliders()
    {
        _uniqueDetectedIds.Clear();
        _uniqueDetectedIdentities.Clear();
        foreach (Collider collider in _detections)
        {
            _currentDetectedIdentity = collider.GetComponent<IIdentity>();

            if (_currentDetectedIdentity != null)
            {
                _currentDetectionID = _currentDetectedIdentity.GetID();

                if (!_ignoreIdsList.Contains(_currentDetectionID))
                {
                    _uniqueDetectedIds.Add(_currentDetectionID);
                    _uniqueDetectedIdentities.Add(_currentDetectedIdentity);
                }
                    
            }
        }
    }
    private void SerializeDetectedIds()
    {
        _detectedIds.Clear();
        _detectedIds = _uniqueDetectedIds.ToList();
    }
    private void RaiseDetectionEventForClosestEntitiy()
    {
        _closestID = 0;
        _closestIdentity = null;
        if (_uniqueDetectedIdentities.Count == 0)
            return;

        if (_uniqueDetectedIdentities.Count == 1)
        {
            _closestID = _uniqueDetectedIdentities.First().GetID();
            OnEntityDetected?.Invoke(_uniqueDetectedIdentities.First());
            return;
        }

        _identitiesList = _uniqueDetectedIdentities.ToList();
        _currentTransform = null;
        _closestTransform = _identitiesList[0].GetGameObject().transform;

        for (int i = 1; i < _identitiesList.Count; i++)
        {
            _currentTransform = _identitiesList[i].GetGameObject().transform;
            _closestTransform = CalculateDistance.GetClosestTransform(_currentTransform, _closestTransform, _castPosition);
        }

        _closestIdentity = _closestTransform.GetComponent<IIdentity>();
        _closestID = _closestIdentity.GetID();
        OnEntityDetected?.Invoke(_closestIdentity);

    }




    //externals
    public void SetDetection(bool isDetectionActive) {_isDetectionActive = isDetectionActive;}
    public bool IsDetectionActive() {  return _isDetectionActive; }
    public void SetDetectionLayers(LayerMask newMask) {  _detectionLayers = newMask;}
    public LayerMask GetDetectionLayers() { return _detectionLayers; }
    public void IgnoreId(int newId)
    {
        if (!_ignoreIdsList.Contains(newId))
            _ignoreIdsList.Add(newId);
    }
    public void RemoveIgnoredId(int id)
    {
        if (id != _selfID)
            _ignoreIdsList.Remove(id);
    }
    public float GetDetectionDistance() { return _detectionRadius; }
    public void SetDetectionDistance(float newRadius) { _detectionRadius = newRadius; }



    //Debug
    private void ListenForDebugCommands()
    {
        if (_cmdIgnoreId)
        {
            _cmdIgnoreId = false;
            IgnoreId(_paramId);
        }
        if (_cmdRemoveIgnoreId)
        {
            _cmdRemoveIgnoreId = false;
            RemoveIgnoredId(_paramId);
        }
    }
    private void LogClosestEntityDetectionResponse(IIdentity identityOfClosest)
    {
        if (_isDebugActive)
            Debug.Log($"Detected 'OnEntityDetected' Event. Closest entity: {identityOfClosest.GetName()} [ID:{identityOfClosest.GetID()}]");
    }
    private void LogEntitiesDetectedResponse(HashSet<IIdentity> detectedIdentities)
    {
        if (_isDebugActive)
        {
            _debugResponseLogString = "";

            foreach (IIdentity identity in detectedIdentities)
                _debugResponseLogString += $"\n{identity.GetName()} [ID: {identity.GetID()}]";

            Debug.Log("Detected (plural) 'OnEntitiesDetected' Event. Entities detected" + _debugResponseLogString);
        }
    }

}
