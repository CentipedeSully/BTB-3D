using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NavigationHelper :MonoBehaviour
{
    private GameObject _returnObject;
    private EventSystem _eventSystem;
    private IEnumerator _setNavCoroutine;
    private bool _wasNavSetThisFrame = false;
    private List<GameObject> _waitingNavTargets = new List<GameObject>();
    private bool _clearNav = false;

    private void Awake()
    {
        NavHelper.SetNavHelper(this);
    }
    private IEnumerator SelectCurrentNavObject(GameObject navTarget) 
    {
        _wasNavSetThisFrame = true;

        if (_clearNav)
        {
            Debug.Log("ClearNav flag detected. Setting nav target to nothing");
            _clearNav = false;
            _eventSystem.SetSelectedGameObject(null);
        }

        else
        {
            _eventSystem.SetSelectedGameObject(navTarget);
            Debug.Log($"Set new Nav Target: {navTarget.name}");
        }
        

        yield return new WaitForEndOfFrame();

        while (_waitingNavTargets.Count > 0)
        {
            int waitingNavTargets = _waitingNavTargets.Count;
            navTarget = _waitingNavTargets[waitingNavTargets - 1];
            _waitingNavTargets.RemoveAt(waitingNavTargets - 1);

            _eventSystem.SetSelectedGameObject(navTarget);
            Debug.Log($"Set new Nav Target: {navTarget.name}");
            yield return new WaitForEndOfFrame();
        }

        _wasNavSetThisFrame = false;
        _setNavCoroutine = null;
    
    }

    public void SetCurrentNavObject(GameObject newTarget)
    {
        //don't try to do anything if no ui event system exists
        if (_eventSystem == null && IsCurrentEventSystemNull())
        {
            Debug.LogWarning($"Failed to find an active eventSystem. Try manually saving an eventSystem to this " +
                $"utility before attemtping to use this utility again.");
            return;
        }

        //save the current event system 
        if (_eventSystem == null && !IsCurrentEventSystemNull())
            SaveCurrentEventSystem();

        if (newTarget == null && _clearNav == false)
        {
            Debug.LogWarning("Nav Target is null. ignoring request");
            return;
        }

        if (newTarget == _eventSystem.currentSelectedGameObject)
        {
            Debug.LogWarning($"NavTarget already selected. Ignoring 'SetCurrentNavTarget' request [{newTarget.name}]");
            return;
        }

        //add the next request to the list if we've already set the nav target this frame
        if (_wasNavSetThisFrame)
        {
            if (_clearNav)
            {
                _waitingNavTargets.Clear();
                Debug.Log($"All waiting nav Targets cleared.");
            }

            else
            {

                _waitingNavTargets.Add(newTarget);
                Debug.Log($"Added nav Target to waiting list: {newTarget.name}");
            }
            return;
        }

        //otherwise, start the retargeting enumerator
        else
        {
            _setNavCoroutine = SelectCurrentNavObject(newTarget);
            StartCoroutine(_setNavCoroutine);
        }
    }

    public bool IsSavedEventSystemNull() {  return _eventSystem == null; }
    public bool IsCurrentEventSystemNull() { return EventSystem.current == null; }
    public void SaveCurrentEventSystem() { _eventSystem = EventSystem.current; }

    public void NavigateBackToReturnObject() 
    {
        if (_returnObject == null)
        {
            Debug.LogError("Attempted to 'return' to a null navigation object.");
            return;
        }    

        SetCurrentNavObject(_returnObject);
    }
    public void NavigateToIsolatedObject(GameObject target, GameObject returnObject)
    {
        if (returnObject == null)
        {
            Debug.LogWarning("Ensure a returnTarget is passed before attempting to navigate to an isolated navOBject. ignoring request");
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("Nav Target parameter is null [during attempt to navigate to an isolated navObject]. ignoring request");
            return;
        }

        _returnObject = returnObject;
        SetCurrentNavObject(target);
    }
    public void ClearNav()
    {
        _waitingNavTargets.Clear();
        _clearNav = true;

        if (_setNavCoroutine == null)
        {
            SetCurrentNavObject(null);
        }

        
    }
}

public static class NavHelper
{
    private static NavigationHelper _controller;

    public static void SetNavHelper(NavigationHelper controller) {  _controller = controller; }

    public static void SetCurrentNavObject(GameObject newTarget) { _controller.SetCurrentNavObject(newTarget); }
    public static bool IsSavedEventSystemNull() { return _controller.IsSavedEventSystemNull(); }
    public static bool IsCurrentEventSystemNull() { return _controller.IsCurrentEventSystemNull(); }
    public static void SaveCurrentEventSystem() { _controller.SaveCurrentEventSystem(); }
    public static void NavigateBackToReturnObject() {  _controller.NavigateBackToReturnObject(); }
    public static void NavigateToIsolatedObject(GameObject target, GameObject returnObject) {  _controller.NavigateToIsolatedObject(target,returnObject); }
    public static void ClearNav() { _controller.ClearNav(); }



}
