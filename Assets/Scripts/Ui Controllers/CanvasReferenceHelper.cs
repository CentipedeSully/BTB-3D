using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CanvasReferenceHelper
{

    private static Canvas _canvas;
    public static void SetCanvase(Canvas newCanvas) { _canvas = newCanvas; }
    public static Canvas GetCanvas() { return _canvas; }
}
