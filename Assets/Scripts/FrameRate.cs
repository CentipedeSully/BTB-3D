using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameRate : MonoBehaviour
{
    [SerializeField] private static int _targetFramRate = 30;


    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = _targetFramRate;
    }

    public static int GetTargetFrameRate() { return _targetFramRate; }
}
