using dtsInventory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputsLockedVisualController : MonoBehaviour
{
    private UiDarkener _darkenController;
    private bool _isUiLocked = false;
    private bool _wasUiLockedBeforeThisFrame = false;

    private void Start()
    {
        _darkenController = GetComponent<UiDarkener>();
    }


    private void Update()
    {
        if (_darkenController != null)
            ControlDarkener();
    }

    private void ControlDarkener()
    {
        _wasUiLockedBeforeThisFrame = _isUiLocked;
        _isUiLocked = !InputFilter.AllowNonUiInput();

        if (_isUiLocked && _wasUiLockedBeforeThisFrame)
            return;
        else if (!_isUiLocked && !_wasUiLockedBeforeThisFrame)
            return;
        
        //darken the game if we just locked our inputs
        if (_isUiLocked && !_wasUiLockedBeforeThisFrame)
        {
            _darkenController.DarkenMenu();
        }

        //undarken the game if we just unlocked our inputs
        else if (!_isUiLocked && _wasUiLockedBeforeThisFrame)
        {
            _darkenController.UndarkenMenu();
        }
    }

}
