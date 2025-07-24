using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;




/// <summary>
/// Tracks the current player's inGame selection.
/// </summary>
public static class SelectionManager
{
    private static GameObject _currentSelection;


    public delegate void SelectionEvent(GameObject newSelection);
    public delegate void SelectionClearedEvent();

    
    public static event SelectionEvent OnSelectionChanged;
    public static event SelectionClearedEvent OnSelectionCleared;




    public static  void SetSelection(GameObject newSelection)
    {
        //autoClear if a null selection was set
        if (newSelection == null)
        {
            Debug.Log("Null selection detected. Clearing current Selection if present.");
            ClearSelection();
            return;
        }

        _currentSelection = newSelection;
        Debug.Log($"Set {_currentSelection} as new Selection");
        OnSelectionChanged?.Invoke(_currentSelection);

    }

    public static  void ClearSelection()
    {
        if (_currentSelection != null)
        {
            _currentSelection = null;

            Debug.Log("Selection Cleared");

            OnSelectionCleared?.Invoke();
        }
        
    }

    public static GameObject GetCurrentSelection() { return _currentSelection; }
    public static bool IsSelectionSet() { return _currentSelection != null; }




}
