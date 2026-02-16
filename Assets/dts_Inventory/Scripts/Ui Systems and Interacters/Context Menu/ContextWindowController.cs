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
        [SerializeField] private Image _darkenEffectImage;
        [SerializeField] private float _darkenDuration;
        [SerializeField] private float _maxDarkness;
        [SerializeField] private NumericalSelectorController _numericalSelector;
        [Tooltip("Where to position the numerical selector when a context option is selected, relative to the selected button's position")]
        [SerializeField] private Vector2 _numberSelectorOffsetFromButton;
        
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
        private bool _isDarkenInProgress = false;
        private float _alpha;
        private float _currentDarkenTime;
        private float _targetDarknessValue;
        private float _startingDarknessValue;
        private Navigation _tempNavStruct;






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
            _darkenEffectImage.gameObject.SetActive(false);
            

        }

        private void Update()
        {
            if (_isDarkenInProgress)
                UpdateDarkeningEffects();
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
        private void UpdateDarkeningEffects()
        {
            if (_darkenEffectImage.color.a == _targetDarknessValue)
            {
                _isDarkenInProgress = false;
                _currentDarkenTime = 0;

                if (_targetDarknessValue == 0)
                {
                    _darkenEffectImage.gameObject.SetActive(false);
                }
            }
            else
            {
                _currentDarkenTime += Time.deltaTime;
                _alpha = Mathf.Lerp(_startingDarknessValue, _targetDarknessValue, _currentDarkenTime / _darkenDuration);
                _darkenEffectImage.color = new Color(_darkenEffectImage.color.r, _darkenEffectImage.color.g, _darkenEffectImage.color.b, _alpha);
            }
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




        public void DarkenMenu()
        {
            //ignore command if the grid is already dark
            if (_darkenEffectImage.color.a == _darkenDuration)
                return;

            //ignore command if we're already darkening the grid
            if (_isDarkenInProgress && _targetDarknessValue == _maxDarkness)
                return;

            //if we're currently UNDOING a previous darkening effect, then reverse direction
            if (_isDarkenInProgress && _targetDarknessValue == 0)
            {
                //reverse our progression point
                _currentDarkenTime = _darkenDuration - _currentDarkenTime;

                //ensure our start and end points are reversed
                _startingDarknessValue = 0;
                _targetDarknessValue = _maxDarkness;
            }

            //if we're starting
            else if (!_isDarkenInProgress)
            {
                _startingDarknessValue = 0;
                _targetDarknessValue = _maxDarkness;
                _isDarkenInProgress = true;
                _darkenEffectImage.gameObject.SetActive(true);
            }
        }
        public void UndarkenMenu()
        {
            //ignore command if the grid is not dark
            if (_darkenEffectImage.color.a == 0)
                return;

            //ignore command if we're already UNdarkening the grid
            if (_isDarkenInProgress && _targetDarknessValue == 0)
                return;

            //if we're currently darkening, then reverse direction
            if (_isDarkenInProgress && _targetDarknessValue == _maxDarkness)
            {
                //reverse our progression point
                _currentDarkenTime = _darkenDuration - _currentDarkenTime;

                //ensure our start and end points are reversed
                _startingDarknessValue = _maxDarkness;
                _targetDarknessValue = 0;
            }

            //if we're starting
            else if (!_isDarkenInProgress)
            {
                _startingDarknessValue = _maxDarkness;
                _targetDarknessValue = 0;
                _isDarkenInProgress = true;
                _currentDarkenTime = 0;
            }
        }
        public void ForceImmediateUndarken()
        {
            _isDarkenInProgress = false;
            _darkenEffectImage.color = new Color(_darkenEffectImage.color.r, _darkenEffectImage.color.g, _darkenEffectImage.color.b, 0);
            _currentDarkenTime = 0;
            _darkenEffectImage.gameObject.SetActive(false);
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
                ForceImmediateUndarken();
                gameObject.SetActive(false);
                
            }

        }
        public void TriggerSelectionEventAndCloseWindow(ContextOption selectedOption,int selectedAmount)
        {
            if (_isWindowOpen)
            {
                
                _numericalSelector.HideNumericalSelector();
                HideOptionsWindow();
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
        public void SpecifyAmount(ContextOption specifiedOption)
        {
            //skip opening the numerical selector if there's only 1 item to act upon
            if (_maximumInteractionAmount == 1)
            {
                Debug.Log("Triggering selection due to max == 1");
                TriggerSelectionEventAndCloseWindow(specifiedOption, 1);
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

            //get the rect transform of this matching button
            RectTransform rectTransform = _selectedButton.GetComponent<RectTransform>();

            //move the numerical selector to the button's position (include the offset)
            _numericalSelector.GetRectTransform().position = rectTransform.TransformPoint(_numberSelectorOffsetFromButton);

            //darken the context menu
            DarkenMenu();
        }
        public void CloseNumericalSelector()
        {
            _currentSelectedOption = ContextOption.None;
            _numericalSelector.HideNumericalSelector();
            UndarkenMenu();
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
        public void FocusOnNumericalSelector()
        {
            _numericalSelector.FocusOnTextNavigationTarget();
        }
        public bool IsNumericalSelectorCurrentlyFocused()
        {
            return _numericalSelector.IsTextNavigationFocused();
        }
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

    }
}

