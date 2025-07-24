using mapPointer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;


namespace mapPointer
{
    public enum MouseBtn
    {
        Left = 0, 
        Right = 1, 
        Middle = 2
    }
}

public class MapPointer : MonoBehaviour
{
    //Declarations
    [Header("Pointer Settings")]
    [SerializeField] private Camera _mapCamera;
    [SerializeField] private float _castDistance = 200;
    [SerializeField] private LayerMask _layermask;
    [SerializeField] private float _inputCooldown = .1f;
    [SerializeField] private Color _pointerColor;
    
    private Ray _castRay;

    [Header("Ui Object Settings")]
    [SerializeField] private GameObject _hoverObject;
    [SerializeField] private Text _hoverText;
    [SerializeField] private Vector3 _hoverTextOffset;


    [Header("Watch Values (Don't Modify)")]
    [SerializeField] private Vector3 _mousePosition;
    [SerializeField] private List<GameObject> _detectedObjects;



    private bool _inputReady = true;
    private bool _lClick;
    private bool _rClick;
    private bool _mClick;

    public delegate void OnClickEvent();
    public OnClickEvent OnLClick;
    public OnClickEvent OnRClick;
    public OnClickEvent OnMClick;

    //Monobehaviours

    private void OnEnable()
    {
        OnLClick += UpdateSelection;
        OnRClick += GiveOrder;
    }

    private void OnDisable()
    {
        OnLClick -= UpdateSelection;
        OnRClick -= GiveOrder;
    }

    private void Update()
    {
        ListenForInput();
        DetectHoveredObjects();
    }

    private void OnDrawGizmos()
    {
        DrawPointerGizmo();
    }




    //Internals
    private void ListenForInput()
    {
        //lCLick
        if (UnityEngine.Input.GetMouseButtonUp((int)MouseBtn.Left) && _inputReady)
        {
            _lClick = true;
            //Debug.Log("LClick Detected");
            _inputReady = false;
            Invoke(nameof(ReadyInput), _inputCooldown);
            OnLClick?.Invoke();
        }
        else _lClick = false;

        //rClick
        if (UnityEngine.Input.GetMouseButtonUp((int)MouseBtn.Right) && _inputReady)
        {
            _rClick = true;
            //Debug.Log("RClick Detected");
            _inputReady = false;
            Invoke(nameof(ReadyInput), _inputCooldown);
            OnRClick?.Invoke();
        }
        else _rClick = false;

        //mClick
        if (UnityEngine.Input.GetMouseButtonUp((int)MouseBtn.Middle) && _inputReady)
        {
            _mClick = true;
            //Debug.Log("MClick Detected");
            _inputReady = false;
            Invoke(nameof(ReadyInput), _inputCooldown);
            OnMClick?.Invoke();
        }
        else _lClick = false;

            
    }

    private void ReadyInput() { _inputReady = true; }

    private void UpdateHoverObjectPosition(RaycastHit detection)
    {
        _hoverObject.transform.position = detection.point + _hoverTextOffset;
    }

    private void UpdateSelection()
    {
        CaptureDetectionsOnPointer();

        if (_detectedObjects.Count > 0)
        {
            //cache for readability
            GameObject closestDetection = _detectedObjects[0];

            //Clear the current selection if the ground was clicked
            if (closestDetection.CompareTag("Ground"))
            {
                SelectionManager.ClearSelection();
            }

            //Set a new selection if a unit or interactible was clicked
            else if (closestDetection.CompareTag("Unit") || closestDetection.CompareTag("Interactible"))
            {
                SelectionManager.SetSelection(closestDetection);
            }
            
        }
    }

    private void GiveOrder()
    {
        CaptureDetectionsOnPointer();

        //Is click location valid and is something selected?
        if (_detectedObjects.Count > 0 && SelectionManager.IsSelectionSet())
        {
            //cache for readability
            GameObject closestDetection = _detectedObjects[0];
            GameObject selection = SelectionManager.GetCurrentSelection();

            if (selection.CompareTag("Unit"))
                Debug.Log("Order Given to unit");
        }
    }


    private void DetectHoveredObjects()
    {
        _detectedObjects.Clear();

        RaycastHit[] detections = SortDetectionsClosestFirst( CastDetections() );

        for (int i =0; i < detections.Length; i++)
        {
            //only add if not already included
            if (!_detectedObjects.Contains(detections[i].collider.gameObject))
                _detectedObjects.Add(detections[i].collider.gameObject);

        }

        if (detections.Length > 0)
        {
            _hoverObject.SetActive(true);
            UpdateHoverObjectPosition(detections[0]);
        }
        else
        {
            _hoverObject.SetActive(false);
        }

        

    }

    private void BuildCastRay()
    {
        _mousePosition = UnityEngine.Input.mousePosition;
        _castRay = _mapCamera.ScreenPointToRay(_mousePosition);
    }

    private RaycastHit[] CastDetections()
    {
        BuildCastRay();
        return Physics.RaycastAll(_castRay, _castDistance, _layermask);
    }

    private RaycastHit[] SortDetectionsClosestFirst(RaycastHit[] detections)
    {
        RaycastHit temp;

        if (detections.Length > 0)
        {
            for (int i = 0; i < detections.Length; i++)
            {
                for (int j = i + 1; j < detections.Length; j++)
                {
                    //swap the element's places if the current one is further than the next
                    if (detections[i].distance > detections[j].distance)
                    {
                        temp = detections[i];
                        detections[i] = detections[j];
                        detections[j] = temp;
                    }
                }
            }
        }

        return detections;
    }

    private void DrawPointerGizmo()
    {
        Gizmos.color = _pointerColor;
        Gizmos.DrawLine(_castRay.origin,_castRay.direction * _castDistance);
    }




    //Externals
    public RaycastHit[] CaptureDetectionsOnPointer()
    {
        return CastDetections();
    }







}
