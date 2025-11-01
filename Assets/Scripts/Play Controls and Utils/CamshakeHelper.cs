using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CamshakeHelper 
{
    static private Camshaker _camshaker;

    public static void SetCamShaker(Camshaker shaker) { _camshaker = shaker; }

    public static void ShakeCamera(float magnitude, float duration) { _camshaker.ShakeCam(magnitude,duration); }
}
