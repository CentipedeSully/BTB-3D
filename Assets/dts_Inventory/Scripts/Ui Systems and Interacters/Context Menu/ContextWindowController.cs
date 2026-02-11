using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;


namespace dtsInventory
{
    public enum ContextOption
    {
        None,
        OrganizeItem,
        UseItem,
        DiscardItem,
        TakeItem, //Implies the item will go to a home container (whether its opened or not)
        TransferItem  //implies the item will be tranferred to any currently-opened Container
    }

    public class ContextWindowController : MonoBehaviour
    {
        //Declarations
        [SerializeField] private GameObject _optionElementPrefab;
        [SerializeField] private RectTransform _pointerContainerTransform;
        private float _yPadding;
        private float _xPadding;
        private float _spacingBtwnOptions;
        private float _optionHeight;
        private RectTransform _rectTransform;
        private bool _isWindowOpen = false;
        private InvWindow _boundWindow;
        private List<Button> _currentButtons = new();

        public delegate void ContextWindowEvent(ContextOption selectedOption);
        public event ContextWindowEvent OnOptionSelected;





        //Monobehaviours
        private void Awake()
        {
            ContextWindowHelper.SetContextWindowController(this);

            _rectTransform = GetComponent<RectTransform>();

            if (_optionElementPrefab != null)
                _optionHeight = _optionElementPrefab.GetComponent<RectTransform>().sizeDelta.y;

            VerticalLayoutGroup layoutController = GetComponent<VerticalLayoutGroup>();
            if (layoutController != null)
            {
                _xPadding = layoutController.padding.left + layoutController.padding.right;
                _yPadding = layoutController.padding.top + layoutController.padding.bottom;
                _spacingBtwnOptions = layoutController.spacing;


            }

            gameObject.SetActive(false);


        }
        



        //Internals





        //Externals
        public void ShowOptionsWindow(Vector3 drawPosition, InvWindow boundWindow, HashSet<ContextOption> availableOptions)
        {
            if (availableOptions == null)
                return;
            if (availableOptions.Count < 1)
                return;

            


            int optionCount = 0;

            //show all matching context buttons, and hide all buttons that don't match the context
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                ContextualOptionDefinition context = child.GetComponent<ContextualOptionDefinition>();

                if (context != null)
                {
                    if (availableOptions.Contains(context.GetContextOption()))
                    {
                        child.gameObject.SetActive(true);
                        optionCount++;
                        _currentButtons.Add(child.GetComponent<Button>());
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }

            }

            if (optionCount > 0)
            {
                //reposition the window onto the pointer
                GetComponent<RectTransform>().localPosition = drawPosition;

                //set what this context window is bound to
                _boundWindow = boundWindow;

                //provide visual feedback to represent the menu being opened
                _boundWindow.DarkenGrid();

                _isWindowOpen = true;

                //resize the window to match the number of options
                float betwixtSpacing = _spacingBtwnOptions * (optionCount - 1);
                float height = _yPadding + _optionHeight * optionCount + _spacingBtwnOptions;
                float width = _xPadding + transform.GetChild(0).GetComponent<RectTransform>().sizeDelta.x; //make sure the child fits well
                _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, height);

                //show the window
                gameObject.SetActive(true);
            }
            else
                Debug.LogWarning("No contextual predefined contextual options were discovered. Ignoring 'ShowContextWindow' request");



        }
        public void HideOptionsWindow()
        {

            if (_isWindowOpen)
            {
                _isWindowOpen = false;
                _boundWindow.UndarkenGrid();
                _boundWindow = null;
                _currentButtons.Clear();
                EventSystem.current.SetSelectedGameObject(null);
                gameObject.SetActive(false);

            }

        }
        public void TriggerSelectionEventAndCloseWindow(ContextOption selectedOption)
        {
            if (_isWindowOpen)
            {
                OnOptionSelected.Invoke(selectedOption);
                HideOptionsWindow();
            }

        }
        public InvWindow CurrentBoundWindow() { return _boundWindow; }

        public bool IsWindowOpen() { return _isWindowOpen; }
        public void OffsetWindow(Vector2 offset)
        {
            _rectTransform.anchoredPosition += offset;
        }
        public bool IsAnyMenuOptionCurrentlyFocused()
        {
            
            foreach(Button button in _currentButtons)
            {
                if (EventSystem.current.currentSelectedGameObject == button.gameObject)
                    return true;
            }

            return false;
        }
        public void FocusOnFirstMenuOption()
        {
            if (_currentButtons.Count > 0)
                EventSystem.current.SetSelectedGameObject(_currentButtons[0].gameObject);
        }
    }

    public static class ContextWindowHelper
    {
        public static ContextWindowController _controller;

        public static void SetContextWindowController(ContextWindowController controller) { _controller = controller; }
        public static void ShowContextWindow(Vector3 drawPosition,InvWindow boundWindow, HashSet<ContextOption> optionsToShow) { _controller.ShowOptionsWindow(drawPosition,boundWindow,optionsToShow); }
        public static void HideContextWindow() { _controller.HideOptionsWindow(); }
        public static ContextWindowController GetContextWindowController() { return _controller; }
        public static bool IsContextWindowShowing() { return _controller.IsWindowOpen(); }
        public static bool CurrentlyBoundWindow() { return _controller.CurrentBoundWindow(); }
        public static void MoveWindow(Vector2 offset) { _controller.OffsetWindow(offset); }
        public static bool IsAnyMenuOptionCurrentlyFocused() { return _controller.IsAnyMenuOptionCurrentlyFocused(); }
        public static void FocusOnMenu() { _controller.FocusOnFirstMenuOption(); }


    }
}

