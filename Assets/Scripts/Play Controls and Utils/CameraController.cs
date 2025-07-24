using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    //Declarations
    [Header("Cam Control Settings")]
    [SerializeField] private Camera _mapCamera;
    [SerializeField] private float _camDepthSpeed = 2;
    [SerializeField] private float _camMoveSpeed = 5;
    [SerializeField] private float _camRotateSpeed = 90;
    [SerializeField] private Vector3 _relativeCamAxisX;
    [SerializeField] private Vector3 _relativeCamAxisZ;
    [SerializeField] private float _maxCamDistance = 20;
    [SerializeField] private float _minCamDistance = 4;

    [SerializeField] private bool _invertZoomControl = true;
    [SerializeField] private bool _invertRotationControl = false;

    private int _zoomInvertValue;
    private int _rotationInvertValue;
    private Vector3 _rotationAxis = Vector3.up;


    [Header("Input Detection")]
    [SerializeField] private Vector2 _zoomInput;
    [SerializeField] private Vector2 _positionInput;
    [SerializeField] private int _rotationInput;



    [Header("Cam Status")]
    [SerializeField] private bool _isCamControlEnabled = true;
    [SerializeField] private Vector3 _originalRotation;
    [SerializeField] private Vector3 _instanceHorizontalAxis;
    [SerializeField] private Vector3 _instanceVerticalAxis;
    [SerializeField] private Vector3 _zoomAxis;

    [SerializeField] private float _currentDistance;






    //Monobehaviours
    private void Awake()
    {
        InitializeUtils();
    }

    private void Update()
    {
        ReadInput();

        if (_isCamControlEnabled)
            ControlCamera();
    }


    //Internals
    private void InitializeUtils()
    {

        //save the original rotation
        _originalRotation = transform.localRotation.eulerAngles;

        //calculate or instance movement axis
        CalculateCameraMovementAxes();

    }

    private void CalculateCameraMovementAxes()
    {
        //initialize the current distance from the camera
        _currentDistance = Vector3.Distance(_mapCamera.transform.position, transform.position);

        //initialize the zoom axis
        _zoomAxis = transform.position - _mapCamera.transform.position;

        //calcualte x axis
        _instanceHorizontalAxis = transform.TransformDirection(_relativeCamAxisX);

        //calculate z axis
        _instanceVerticalAxis = transform.TransformDirection(_relativeCamAxisZ);
    }

    private void ReadInput()
    {
        //reset move inputs
        _positionInput = Vector2.zero;

        //read current move input
        if (Input.GetKey(KeyCode.A))
            _positionInput.x += -1;
        if (Input.GetKey(KeyCode.D))
            _positionInput.x += 1;
        if (Input.GetKey(KeyCode.S))
            _positionInput.y += -1;
        if (Input.GetKey(KeyCode.W))
            _positionInput.y += 1;


        //reset rotate input
        _rotationInput = 0;

        //read cam rotate input
        if (Input.GetKey(KeyCode.Q))
            _rotationInput += 1;
        if (Input.GetKey(KeyCode.E))
            _rotationInput += -1;


        //reset zoom input
        _zoomInput = Vector2.zero;

        //read zoom input
        _zoomInput = Input.mouseScrollDelta;
    
    }

    private void ControlCamera()
    {

        //invert zoom if necessary
        if (_invertZoomControl)
            _zoomInvertValue = -1;
        else 
            _zoomInvertValue = 1;

        _zoomInput.y = _zoomInput.y * _zoomInvertValue;


        //invert rotation if necessary
        if(_invertRotationControl)
            _rotationInvertValue = -1;
        else 
            _rotationInvertValue = 1;

        _rotationInput = _rotationInput * _rotationInvertValue;



        //update camera zoom
        if (_zoomInput.y != 0)
        {
            Vector3 zoomOffset = _camDepthSpeed * Mathf.Sign(_zoomInput.y) * Time.deltaTime * _zoomAxis.normalized;

            if (_currentDistance < _maxCamDistance && zoomOffset.y > 0 ||
                _currentDistance > _minCamDistance && zoomOffset.y < 0)
            {
                //zoom in/out the camera
                _mapCamera.transform.position += zoomOffset;

                //update the camera's distance
                _currentDistance = Vector3.Distance(_mapCamera.transform.position,transform.position);
            }
            
        }


        //update camera horizontal position
        if (_positionInput.x != 0)
        {
            Vector3 hMoveOffset = _camMoveSpeed * _positionInput.x * Time.deltaTime * _instanceHorizontalAxis.normalized;
            transform.localPosition = transform.localPosition + hMoveOffset;
        }
        

        //update camera vertical position
        if (_positionInput.y != 0)
        {
            Vector3 vOffset = _camMoveSpeed * Mathf.Sign(_positionInput.y) * Time.deltaTime * _instanceVerticalAxis.normalized;
            transform.localPosition = transform.localPosition + vOffset;
        }

        //update camera rotation
        if (_rotationInput != 0)
        {
            Vector3 angleOffset = _camRotateSpeed * Mathf.Sign(_rotationInput) * Time.deltaTime * _rotationAxis;
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles + angleOffset);

            //recalculate the movement angles to relate to the new rotation
            CalculateCameraMovementAxes();
        }

    }


    //Externals
    public void ResetCameraRotation()
    {
        _isCamControlEnabled = false;
        transform.localRotation = Quaternion.Euler(_originalRotation);

        CalculateCameraMovementAxes();
        _isCamControlEnabled = true;
    }





}
