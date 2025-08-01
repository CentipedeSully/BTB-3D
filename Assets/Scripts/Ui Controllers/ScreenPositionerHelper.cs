using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static  class ScreenPositionerHelper
{
    private static Camera _uiCamera;





    public static void SetUiCamera(Camera uiCam) {  _uiCamera = uiCam; }
    public static Vector3 ScreenPosition(RectTransform UiElement)
    {
        return _uiCamera.WorldToScreenPoint(UiElement.position,_uiCamera.stereoActiveEye);
    }

}
