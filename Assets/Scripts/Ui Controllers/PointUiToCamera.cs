using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointUiToCamera : MonoBehaviour
{
    [SerializeField] private Camera _mainCam;




    private void Start()
    {
        _mainCam = ScreenPositionerHelper.GetMainCamera();
    }

    private void LateUpdate()
    {
        if (_mainCam != null)
            PointUiToMainCamera();
    }


    private void PointUiToMainCamera()
    {
        transform.LookAt(transform.position + _mainCam.transform.forward);
    }
}
