using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupCanvasHelper : MonoBehaviour
{
    private void Awake()
    {
        CanvasReferenceHelper.SetCanvase(GetComponent<Canvas>());
    }
}
