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


    [Header("Watch Values (Don't Modify)")]
    [SerializeField] private Vector3 _mousePosition;
    [SerializeField] private List<GameObject> _detectedObjects;



    private bool _inputReady = true;

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




    //Internals
    private void ListenForInput()
    {
        //lCLick
        if (UnityEngine.Input.GetMouseButtonUp((int)MouseBtn.Left) && _inputReady)
        {
            //Debug.Log("LClick Detected");
            _inputReady = false;
            Invoke(nameof(ReadyInput), _inputCooldown);
            OnLClick?.Invoke();
        }

        //rClick
        if (UnityEngine.Input.GetMouseButtonUp((int)MouseBtn.Right) && _inputReady)
        {
            //Debug.Log("RClick Detected");
            _inputReady = false;
            Invoke(nameof(ReadyInput), _inputCooldown);
            OnRClick?.Invoke();
        }

        //mClick
        if (UnityEngine.Input.GetMouseButtonUp((int)MouseBtn.Middle) && _inputReady)
        {
            //Debug.Log("MClick Detected");
            _inputReady = false;
            Invoke(nameof(ReadyInput), _inputCooldown);
            OnMClick?.Invoke();
        }

            
    }

    private void ReadyInput() { _inputReady = true; }


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
        RaycastHit[] detectionResults = CaptureDetectionsOnPointer();

        //Is click location valid and is something selected?
        if (detectionResults.Length > 0 && SelectionManager.IsSelectionSet())
        {
            //cache for readability
            RaycastHit closestDetection = detectionResults[0];
            GameObject selection = SelectionManager.GetCurrentSelection();

            if (selection.CompareTag("Unit"))
            {
                //make sure the unit has a unitBehaviour
                UnitBehavior unitBehavior = selection.GetComponent<UnitBehavior>();
                if (unitBehavior != null)
                {
                    //determine the interaction context
                    switch (closestDetection.collider.tag)
                    {
                        case "Ground":
                            unitBehavior.MoveToPosition(closestDetection.point);
                            break;

                        case "Interactible":
                            GameObject interactibleObject = closestDetection.collider.gameObject;
                            InteractibleBehavior behaviour= interactibleObject.GetComponent<InteractibleBehavior>();

                            unitBehavior.InteractWithInteractible(behaviour);
                            break;

                        case "Unit":
                            break;

                        default:
                            break;
                    }
                }
            }
        }
    }


    private void DetectHoveredObjects()
    {
        _detectedObjects.Clear();

        RaycastHit[] detections =CastDetections();

        for (int i =0; i < detections.Length; i++)
        {
            //only add if not already included
            if (!_detectedObjects.Contains(detections[i].collider.gameObject))
                _detectedObjects.Add(detections[i].collider.gameObject);

        }

        if (detections.Length > 0)
        {
            GameObject closestDetection = detections[0].collider.gameObject;
            if (!closestDetection.CompareTag("Ground"))
            {
                _hoverText.text = closestDetection.name;
                _hoverObject.SetActive(true);
            }
            else _hoverObject.SetActive(false);
            
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
        return SortDetectionsClosestFirst( Physics.RaycastAll(_castRay, _castDistance, _layermask));
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




    //Externals
    public RaycastHit[] CaptureDetectionsOnPointer()
    {
        return CastDetections();
    }







}
