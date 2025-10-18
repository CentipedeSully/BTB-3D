using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputDirector : MonoBehaviour
{
    [SerializeField] private bool _escCommand = false;







    private void Update()
    {
        ListenForInput();
        ManageUiKeyControls();
    }


    private void ListenForInput()
    {
        _escCommand = Input.GetKeyDown(KeyCode.Escape);
        if (_escCommand)
            Debug.Log("Escape Pressed");
    }

    private void ManageUiKeyControls()
    {
        if (_escCommand && UiTracker.GetHoveredWindow() != null)
        {
            IUiWindow uiWindow = UiTracker.GetHoveredWindow();
            uiWindow.CloseWindow();

        }
    }

}
