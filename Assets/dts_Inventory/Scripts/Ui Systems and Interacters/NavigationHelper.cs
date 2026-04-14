using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace dtsInventory
{
    public class NavigationHelper : MonoBehaviour
    {
        private GameObject _returnObject;
        private EventSystem _eventSystem;
        private IEnumerator _setNavCoroutine;
        private List<GameObject> _waitingNavTargets = new List<GameObject>();

        private void Awake()
        {
            NavHelper.SetNavHelper(this);
        }
        private IEnumerator SelectCurrentNavObject(GameObject navTarget)
        {
            //add the specified target to the waitList
            _waitingNavTargets.Add(navTarget);

            //wait for Unity's EventSystem NavTool to finish it's current action [of applicable]
            while (_eventSystem.alreadySelecting)
            {
                //Debug.LogWarning("Waiting [for 1 frame] for EventSystem to complete a preexisting 'setNav' command...");
                yield return new WaitForEndOfFrame();
            }

            //Get thru all the waiting navTargets over time
            while (_waitingNavTargets.Count > 0)
            {
                //skip all invalid wait targets and find the first that's valid
                bool validTargetDetected = false;
                int validTargetIndex = 0;
                for (int i = 0; i < _waitingNavTargets.Count; i++)
                {
                    if (_waitingNavTargets[i] != _eventSystem.currentSelectedGameObject)
                    {
                        navTarget = _waitingNavTargets[i];
                        validTargetDetected = true;
                        validTargetIndex = i;
                        break;
                    }
                }

                //clear the waitlist and end the coroutine if no valid targets were found
                if (!validTargetDetected)
                {
                    _waitingNavTargets.Clear();
                    /* Debug
                    Debug.Log("Ending NavTarget Coroutine: [No more valid navTargets waiting]\n Valid Targets Include:\n" +
                        $"GameObjects not already selected by the current EventSystem [{_eventSystem.name}]");
                    */
                    break;
                }

                //otherwise, clear all of the invalid waiting objects
                //then focus on the found valid target
                navTarget = _waitingNavTargets[validTargetIndex];
                /* Debug
                if (navTarget != null)
                    Debug.Log($"Valid navTarget found in waiting list: [{navTarget.name}].");
                else Debug.Log("Null navTarget found in waiting list. [Reading request as 'Set Target to nothing']");
                */

                while (validTargetIndex >= 0)
                {
                    /*Debug
                    if (navTarget != null)
                        Debug.Log($"Removing navTarget from waitList: [{_waitingNavTargets[validTargetIndex].name}]");
                    else Debug.Log("Removing the 'Null' request from the waitlist.");
                    */
                    _waitingNavTargets.RemoveAt(validTargetIndex);
                    validTargetIndex--;
                }
                
                _eventSystem.SetSelectedGameObject(navTarget);
                /*Debug
                if (navTarget != null)
                    Debug.Log($"New NavTarget Set: {navTarget.name}");
                else Debug.Log($"Deselected the previous NavTarget");
                */
                yield return new WaitForEndOfFrame();
            }

            _setNavCoroutine = null;

        }

        /// <summary>
        /// Sets (or Queues) targets to focus on [for navigation purposes] using the current event system. If multiple 
        /// targets are specified per frame, then one target is selected as a focus for each frame until all 
        /// targets have been iterated through. Targets that are already focused on are ignored. Duplicates are allowed in waiting list.
        /// </summary>
        /// <param name="newTarget">The new target to focus navigation on</param>
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


            //add the request to the list of waiting targets if the coroutine is already running
            if (_setNavCoroutine != null)
            {
                _waitingNavTargets.Add(newTarget);
                /*Debug
                if (newTarget != null)
                    Debug.Log($"Added nav Target to waiting list: {newTarget.name} [wait in progress]");
                else Debug.Log($"Added Deselection request to waitingList. [wait in progress] [null navTarget was detected as parameter]");
                */
            }

            //otherwise, start the retargeting enumerator
            else
            {
                /*Debug
                if (newTarget != null)
                    Debug.Log($"Added nav Target to waiting list: {newTarget.name} [first in line]");
                else Debug.Log($"Added Deselection request to waitingList. [first in line] [null navTarget was detected as parameter]");
                */
                _setNavCoroutine = SelectCurrentNavObject(newTarget);
                StartCoroutine(_setNavCoroutine);
            }
        }

        public bool IsSavedEventSystemNull() { return _eventSystem == null; }
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
            //Debug.Log("Nav waitlist cleared. Deselecting current navTarget...");
            SetCurrentNavObject(null);


        }
        public bool IsNavTargetingQueueInProgress() { return  _waitingNavTargets.Count > 0 && _setNavCoroutine != null;}
    }

    public static class NavHelper
    {
        private static NavigationHelper _controller;

        public static void SetNavHelper(NavigationHelper controller) { _controller = controller; }

        public static void SetCurrentNavObject(GameObject newTarget) { _controller.SetCurrentNavObject(newTarget); }
        public static bool IsSavedEventSystemNull() { return _controller.IsSavedEventSystemNull(); }
        public static bool IsCurrentEventSystemNull() { return _controller.IsCurrentEventSystemNull(); }
        public static void SaveCurrentEventSystem() { _controller.SaveCurrentEventSystem(); }
        public static void NavigateBackToReturnObject() { _controller.NavigateBackToReturnObject(); }
        public static void NavigateToIsolatedObject(GameObject target, GameObject returnObject) { _controller.NavigateToIsolatedObject(target, returnObject); }
        public static void ClearNav() { _controller.ClearNav(); }
        public static bool IsNavTargetingQueueInProgress() { return _controller.IsNavTargetingQueueInProgress(); }



    }
}
