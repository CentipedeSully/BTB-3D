using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
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
        [SerializeField] private Transform _buttonOptionsContainer;
        [SerializeField] private UiDarkener _uiDarkener;
        [SerializeField] private NumericalSelectorController _numericalSelector;
        [SerializeField] private TransferMenuController _transferMenuController;
        [Tooltip("Where to position the numerical selector when a context option is selected, relative to the selected button's position")]
        [SerializeField] private Vector2 _numberSelectorOffsetFromButton;
        [SerializeField] private Vector2 _transferMenuOffsetFromButton;
        
        private ContextOption _currentSelectedOption = ContextOption.None;
        private Button _selectedButton;
        private int _maximumInteractionAmount;
        private int _minimumInteractionAmount;
        private ItemData _itemData;
        private float _yPadding;
        private float _xPadding;
        private float _spacingBtwnOptions;
        private float _optionHeight;
        private RectTransform _rectTransform;
        private bool _isWindowOpen = false;
        private InvWindow _boundWindow;
        private List<Button> _currentButtons = new();
        private ContextualOptionDefinition _contextOptionDef;
        public delegate void ContextWindowEvent(ContextOption selectedOption, int selectedAmount);
        public event ContextWindowEvent OnOptionSelected;

        private Navigation _tempNavStruct;
        private InvGrid _specifiedContainer;
        private RectTransform _selectedTransferOption;






        //Monobehaviours
        private void Awake()
        {
            ContextWindowHelper.SetContextWindowController(this);
            CloseNumericalSelector();

            _rectTransform = GetComponent<RectTransform>();

            if (_optionElementPrefab != null)
                _optionHeight = _optionElementPrefab.GetComponent<RectTransform>().sizeDelta.y;

            VerticalLayoutGroup layoutController = _buttonOptionsContainer.GetComponent<VerticalLayoutGroup>();
            if (layoutController != null)
            {
                _xPadding = layoutController.padding.left + layoutController.padding.right;
                _yPadding = layoutController.padding.top + layoutController.padding.bottom;
                _spacingBtwnOptions = layoutController.spacing;


            }

            gameObject.SetActive(false);
            _transferMenuController.SetContextMenuController(this);
            

        }


        private void OnEnable()
        {
            if (_numericalSelector != null)
                SubToNumericalSelector();
        }
        private void OnDisable()
        {
            if (_numericalSelector != null)
                UnsubFromNumericalSelector();
        }


        //Internals
        private void SubToNumericalSelector()
        {
            _numericalSelector.OnNumberSubmitted += ConfirmNumercialSelection;
        }
        private void UnsubFromNumericalSelector()
        {
            _numericalSelector.OnNumberSubmitted -= ConfirmNumercialSelection;
        }

        private void RebuildButtonNavigations()
        {
            if (_currentButtons.Count == 0)
                return;

            if (_currentButtons.Count == 1)
            {
                //get the button's current nav data
                _tempNavStruct = _currentButtons[0].navigation;

                //tweak the data so this single button can only select itself
                _tempNavStruct.mode = Navigation.Mode.Explicit;
                _tempNavStruct.selectOnUp = _currentButtons[0];
                _tempNavStruct.selectOnDown = _currentButtons[0];
                _tempNavStruct.wrapAround = true;

                //update the button with the new nav data
                _currentButtons[0].navigation = _tempNavStruct;
            }

            else
            {
                for (int i =0; i < _currentButtons.Count; i++)
                {
                    //get the button's current nav data
                    _tempNavStruct = _currentButtons[i].navigation;


                    //tweak the data so this button navigates to it's neighbors
                    _tempNavStruct.mode = Navigation.Mode.Explicit;
                    
                    //navigate to the last element if this is the first element
                    if (i == 0)
                        _tempNavStruct.selectOnUp = _currentButtons[_currentButtons.Count - 1];

                    //otherwise, navigate to the previous element
                    else
                        _tempNavStruct.selectOnUp = _currentButtons[i-1];


                    //navigate to the first element if this is the last element
                    if (i == _currentButtons.Count - 1)
                        _tempNavStruct.selectOnDown = _currentButtons[0];

                    //otherwise, navigate to the next element
                    else
                        _tempNavStruct.selectOnDown = _currentButtons[i + 1];

                    _tempNavStruct.wrapAround = true;


                    //update the button with the new nav data
                    _currentButtons[i].navigation = _tempNavStruct;
                }
            }
        }










        //Externals
        public void MarkOptionAsSelected(Button option)
        {
            _selectedButton = option;

        }
        public void ShowOptionsWindow(Vector3 drawPosition, InvWindow boundWindow, HashSet<ContextOption> availableOptions,ItemData itemData,int minimumInteractionAmount,int maximumInteractionAmount)
        {
            if (availableOptions == null)
                return;
            if (availableOptions.Count < 1)
                return;

            


            int optionCount = 0;

            //show all matching context buttons, and hide all buttons that don't match the context
            for (int i = 0; i < _buttonOptionsContainer.childCount; i++)
            {
                Transform child = _buttonOptionsContainer.GetChild(i);
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

                //Rebuild each button's navData to reflect this context
                RebuildButtonNavigations();

                //show the window
                gameObject.SetActive(true);

                //Dont forget to save the interaction values of the context & item data
                _minimumInteractionAmount = minimumInteractionAmount;
                _maximumInteractionAmount = maximumInteractionAmount;
                _itemData = itemData;
            }
            else
                Debug.LogWarning("No contextual predefined contextual options were discovered. Ignoring 'ShowContextWindow' request");



        }
        public void ShowTranferWindow(Vector3 drawPosition, InvWindow boundWindow, int minimumInteractionAmount, int maximumInteractionAmount)
        {
            if (_transferMenuController == null)
                return;

            //_transferMenuController.ShowMenu(drawPosition)
        }
        public void HideOptionsWindow()
        {

            if (_isWindowOpen)
            {
                if (_numericalSelector.IsNumericalSelectorOpen())
                    CloseNumericalSelector();

                _isWindowOpen = false;
                _boundWindow.UndarkenGrid();
                _boundWindow = null;
                _currentButtons.Clear();
                EventSystem.current.SetSelectedGameObject(null);
                _uiDarkener.ForceImmediateUndarken();
                _transferMenuController.ForceImmediateUndarken();
                gameObject.SetActive(false);
                _specifiedContainer = null;
                _selectedButton = null;
            }

        }
        public void TriggerSelectionEventAndCloseWindow(ContextOption selectedOption,int selectedAmount)
        {
            if (_isWindowOpen)
            {
                _numericalSelector.HideNumericalSelector();
                _transferMenuController.HideMenu();
                HideOptionsWindow();
                Debug.Log($"Returning to invInteracter. Context: {selectedOption}, Amount:{selectedAmount}");


                OnOptionSelected.Invoke(selectedOption, selectedAmount);
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
            
            foreach(Button option in _currentButtons)
            {
                if (EventSystem.current.currentSelectedGameObject == option.gameObject)
                {
                    //Debug.Log($"Option {option.name} is selected");
                    return true;
                }
            }

            return false;
        }
        public bool IsNumercialSelectorOpen()
        {
            return _numericalSelector.IsNumericalSelectorOpen() ;
        }
        public void FocusOnFirstMenuOption()
        {
            if (_currentButtons.Count > 0)
            {
                EventSystem.current.SetSelectedGameObject(_currentButtons[0].gameObject);
            }
                
        }
        public void FocusOnLastMenuOption() 
        {
            if (_currentButtons.Count > 0)
            {
                EventSystem.current.SetSelectedGameObject(_currentButtons[_currentButtons.Count -1].gameObject);
            }
        
        }
        public void FocusOnLatestMenuOption()
        {
            if (_currentButtons.Count > 0)
            {
                if (_selectedButton.gameObject.activeSelf)
                    EventSystem.current.SetSelectedGameObject(_selectedButton.gameObject);
                else 
                    FocusOnFirstMenuOption();
            }
        }
        public void SaveTransferOption(RectTransform savedRectTransform) { _selectedTransferOption = savedRectTransform; }
        public void SetGridSelection(InvGrid transferGrid) { _specifiedContainer = transferGrid; }
        public void SpecifyAmount(ContextOption specifiedOption)
        {
            if (specifiedOption == ContextOption.None)
            {
                Debug.LogError("Attempted to specift amount for empty context");
                return;
            }
            //if the context is to tranfer, we need to also specify which container to transfer to before specifying the amount
            if (specifiedOption == ContextOption.TransferItem)
            {
                //if we haven't yet specified a container, show the tranfer menu
                if (_specifiedContainer == null)
                {
                    Button transferBtn = null;
                    foreach (Button btn in _currentButtons)
                    {
                        if (btn.GetComponent<ContextualOptionDefinition>().GetContextOption() == ContextOption.TransferItem)
                        {
                            transferBtn = btn;
                            break;
                        }
                    }

                    if (transferBtn == null)
                    {
                        Debug.LogError("The transfer button is missing from the button options. How did it get lost?" +
                            "Cant determine the tranfser menu's draw position without it.");
                        return;
                    }

                    Vector3 menuDrawPosition = transferBtn.GetComponent<RectTransform>().TransformPoint(Vector3.zero);
                    _transferMenuController.ShowMenu(menuDrawPosition);
                    _transferMenuController.OffsetMenu(_transferMenuOffsetFromButton);
                    _uiDarkener.DarkenMenu();
                    return;
                }

                //otherwise, the tranfer button option already told us the specified container.
                //we've also already received the selected button
                //continue as normal
            }

            //skip opening the numerical selector if there's only 1 item to act upon
            if (_maximumInteractionAmount == 1)
            {
                if (specifiedOption == ContextOption.TransferItem)
                {
                    InvManagerHelper.GetInvController().SetTransferReceiverContext(_specifiedContainer);
                    _specifiedContainer = null;
                }

                TriggerSelectionEventAndCloseWindow(specifiedOption, 1);
                Debug.Log("Transferring 1 item...");
                return;
            }

            //also, skip opening the numerical selector if we're 'using' an item stack, and bulkUse is disabled
            else if (specifiedOption == ContextOption.UseItem && !_itemData.IsBulkUseEnabled())
            {
                TriggerSelectionEventAndCloseWindow(specifiedOption, 1);
                return;
            }

            _numericalSelector.ShowNumericalSelector(_minimumInteractionAmount,_maximumInteractionAmount);
            _currentSelectedOption = specifiedOption;

            //if we're transferring items, then draw the numerical selector alongside the selected transferMenu button
            if (specifiedOption == ContextOption.TransferItem)
            {

                _numericalSelector.GetRectTransform().localPosition = Vector3.zero;
                _numericalSelector.GetRectTransform().position = _selectedTransferOption.GetComponent<RectTransform>().TransformPoint(_numberSelectorOffsetFromButton);
                InvManagerHelper.GetInvController().SetTransferReceiverContext(_specifiedContainer);
                _specifiedContainer = null;


                //darken the tranfer menu: if we reached this section of code, our transfer menu is already open 
                //and the context menu is already darkened. 
                _transferMenuController.DarkenTransferMenu();

            }

            //draw the numerical selector alongside the context button [visually]
            else
            {

                //get the rect transform of this matching button
                RectTransform rectTransform = _selectedButton.GetComponent<RectTransform>();

                //move the numerical selector to the button's position (include the offset)

                _numericalSelector.GetRectTransform().localPosition = Vector3.zero;
                _numericalSelector.GetRectTransform().position = rectTransform.TransformPoint(_numberSelectorOffsetFromButton);

                //darken the context menu
                _uiDarkener.DarkenMenu();
            }

            
        }
        public void CloseNumericalSelector()
        {
            _currentSelectedOption = ContextOption.None;
            _numericalSelector.HideNumericalSelector();

            //if the transfer menu is open, undarken that first
            if (ContextWindowHelper.IsTransferMenuOpen())
                UndarkenTransferMenu();
            else
                UndarkenContextMenu();
        }
        public void ConfirmNumercialSelection(int amount)
        {
            TriggerSelectionEventAndCloseWindow(_currentSelectedOption, amount);
        }
        public void ForceUnsubFromNumericalSelector()
        {
            if (_numericalSelector != null)
                UnsubFromNumericalSelector();
        }
        public RectTransform NumericalSelectorRectTransform() { return _numericalSelector.GetRectTransform(); }
        public void IncrementNumericalSelector(int amount)
        {
            //only increment once if we're at the maximum, regardless of the jump size
            if (_numericalSelector.GetNumber() == _numericalSelector.GetMax())
            {
                _numericalSelector.IncrementNumber();
                return;
            }

            int count = 0;

            //only keep incrementing if we havent reached the max
            while (count < amount && _numericalSelector.GetNumber() < _numericalSelector.GetMax())
            {
                _numericalSelector.IncrementNumber();
                count++;
            }
            
        }
        public void DecrementNumericalSelector(int amount)
        {
            //only decrement once if we're at the minimum, regardless of the jump size
            if (_numericalSelector.GetNumber() == _numericalSelector.GetMin())
            {
                _numericalSelector.DecrementNumber();
                return;
            }

            int count = 0;

            //only keep decrementing if we havent reached the min
            while (count < amount && _numericalSelector.GetNumber() > _numericalSelector.GetMin())
            {
                _numericalSelector.DecrementNumber();
                count++;
            }

        }
        public void PlayValueChangeAudioFeedback()
        {
            _numericalSelector.PlayValueChangeAudioFeedback();
        }
        public void SubmitCurrentNumericalSelection()
        {
            if (_numericalSelector.IsNumericalSelectorOpen())
                _numericalSelector.SubmitNumber();
        }
        public void SetPointerMode(bool newState) { _numericalSelector.TogglePointerMode(newState); }
        public bool PointerMode() { return _numericalSelector.IsInPointerMode(); }
        public RectTransform GetConfirmBtnRectTransform() { return _numericalSelector.GetConfirmBtnRectTransform(); }
        public RectTransform GetInputAreaRectTransform() { return _numericalSelector.GetTextNavAreaRectTransform(); }
        public RectTransform GetTransferMenuRectTransform() { return _transferMenuController.GetComponent<RectTransform>(); }
        public void FocusOnNumericalSelector()
        {
            _numericalSelector.FocusOnTextNavigationTarget();
        }
        public bool IsNumericalSelectorCurrentlyFocused()
        {
            return _numericalSelector.IsTextNavigationFocused();
        }
        public void ShowTransferMenu(Vector3 position)
        {
            _transferMenuController.ShowMenu(position);
        }
        public void HideTransferMenu()
        {
            _transferMenuController.HideMenu();
        }
        public bool IsTransferMenuOpen() { return _transferMenuController.IsTransferMenuOpen();}
        public bool IsDarkened() 
        {
            // return true if the image is dark, or is currently darkening
            return _uiDarkener.IsDarkened();
        }
        public void UndarkenContextMenu() { _uiDarkener.UndarkenMenu(); }
        public void UndarkenTransferMenu() { _transferMenuController.UndarkenTransferMenu(); }
    }

    public static class ContextWindowHelper
    {
        public static ContextWindowController _controller;
        

        public static void SetContextWindowController(ContextWindowController controller) { _controller = controller; }
        public static void ShowContextWindow(Vector3 drawPosition,InvWindow boundWindow, HashSet<ContextOption> optionsToShow,ItemData itemData, int minValue, int maxValue) 
        { 
            _controller.ShowOptionsWindow(drawPosition,boundWindow,optionsToShow,itemData,minValue,maxValue); 
        }
        public static void HideContextWindow() { _controller.HideOptionsWindow(); }
        public static void HideNumericalSelector() { _controller.CloseNumericalSelector(); }
        public static ContextWindowController GetContextWindowController() { return _controller; }
        public static bool IsContextWindowShowing() { return _controller.IsWindowOpen(); }
        public static bool IsNumericalSelectorWindowOpen() { return _controller.IsNumercialSelectorOpen(); }
        public static bool CurrentlyBoundWindow() { return _controller.CurrentBoundWindow(); }
        public static void MoveWindow(Vector2 offset) { _controller.OffsetWindow(offset); }
        public static bool IsAnyMenuOptionCurrentlyFocused() { return _controller.IsAnyMenuOptionCurrentlyFocused(); }
        public static void FocusOnMenu() { _controller.FocusOnFirstMenuOption(); }
        public static void FocusOnLatestMenuOption() { _controller.FocusOnLatestMenuOption(); }
        public static void ForceUnsubFromNumericalSelector() { _controller.ForceUnsubFromNumericalSelector(); }
        public static void IncrementNumericalSelector(int amount) { _controller.IncrementNumericalSelector(amount); _controller.PlayValueChangeAudioFeedback(); }
        public static void DecrementNumericalSelector(int amount) {_controller.DecrementNumericalSelector(amount); _controller.PlayValueChangeAudioFeedback(); }
        public static void SubmitCurrentNumber() { _controller.SubmitCurrentNumericalSelection(); }
        public static void SetPointerMode(bool newState) { _controller.SetPointerMode(newState); }
        public static bool IsPointerModeActive() { return _controller.PointerMode(); }
        public static void FocusOnNumericalSelector() { _controller.FocusOnNumericalSelector(); }
        public static bool IsNumericalSelectorCurrentlyFocused() { return _controller.IsNumericalSelectorCurrentlyFocused(); }
        public static bool IsTransferMenuOpen() { return _controller.IsTransferMenuOpen(); }
        public static bool IsMenuDarkened() { return _controller.IsDarkened(); }
        public static void UndarkenContextMenu() { _controller.UndarkenContextMenu(); }
        

    }
}

