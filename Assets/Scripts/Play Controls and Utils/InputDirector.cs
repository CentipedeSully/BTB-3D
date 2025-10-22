using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputDirector : MonoBehaviour
{
    [Header("Literal Inputs")]
    [SerializeField] private bool _escKey = false;
    [SerializeField] private bool _invKey = false;
    [SerializeField] private bool _lClick = false;
    [SerializeField] private bool _rClick = false;
    [SerializeField] private bool _mClick = false;
    [SerializeField] private bool _rotateLeftInput = false;
    [SerializeField] private bool _rotateRightInput = false;
    [SerializeField] private bool _zoomCamTowards= false;
    [SerializeField] private bool _zoomCamAway= false;






    private void Update()
    {
        ListenForInput();
        ManageUiKeyControls();
    }


    private void ListenForInput()
    {
        _escKey = Input.GetKeyDown(KeyCode.Escape);
        if (_escKey)
            Debug.Log("Escape Pressed");

        _invKey = Input.GetKeyUp(KeyCode.I);

        _lClick = Input.GetMouseButtonUp(0);
        _rClick = Input.GetMouseButtonUp(1);
        _mClick = Input.GetMouseButtonUp(2);

        //_rotateLeftInput = Input.Get
    }

    private void ManageUiKeyControls()
    {
        if (_escKey && UiTracker.GetHoveredWindow() != null)
        {
            IUiWindow uiWindow = UiTracker.GetHoveredWindow();
            uiWindow.CloseWindow();

        }
    }

}
