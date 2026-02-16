using dtsInventory;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NumericalSelectorController : MonoBehaviour
{
    //Declarations
    [SerializeField] private int _maxNumber = 9999;
    [SerializeField] private int _minNumber = 0;
    [SerializeField] private int _number = 0;
    [SerializeField] private Text _textDisplay;
    [SerializeField] private InputField _inputField;
    [SerializeField] private GameObject _textNavigationTarget;
    [SerializeField] private AudioClip _onValueChangedAudioClip;
    [SerializeField] private float _confirmInputDelay = .1f;
    [SerializeField] private Button _pointerConfirmBtn;
    bool _pointerMode = false;
    [SerializeField] private Animator _confirmBtnAnimator;
    private float _currentDelayCount = 0;
    private bool _confirmReady = false;
    private AudioSource _audioSource;

    public delegate void NumercialSelectionEvent(int submittedNumber);
    public event NumercialSelectionEvent OnNumberSubmitted;



    //monobehaviours
    private void Awake()
    {
        HideNumericalSelector();
    }

    private void OnDestroy()
    {
        ContextWindowHelper.ForceUnsubFromNumericalSelector();
    }

    private void OnEnable()
    {
        TogglePointerMode(_pointerMode);
    }

    private void Update()
    {
        TickDelay();
    }


    //internals
    private void RenderNumbertoDisplay()
    {
        _textDisplay.text = _number.ToString();
        _inputField.text = _number.ToString();
    }
    private void TickDelay()
    {
        if (!_confirmReady)
        {
            _currentDelayCount += Time.deltaTime;
            if (_currentDelayCount >= _confirmInputDelay)
            {
                _confirmReady = true;
                _currentDelayCount = 0;
            }
        }
    }






    //externals
    public void IncrementNumber()
    {
        _number++;

        if (_number > _maxNumber)
            _number = _minNumber;

        RenderNumbertoDisplay();
    }
    public void DecrementNumber()
    {
        _number--;

        if (_number < _minNumber)
            _number = _maxNumber;

        RenderNumbertoDisplay();
    }
    public void ResetNumber()
    {
        _number = _minNumber;
        RenderNumbertoDisplay();
    }
    public void SetNumber(int number)
    {
        _number = number;
        RenderNumbertoDisplay();
    }
    public int GetNumber() { return _number; }
    public int GetMax() {  return _maxNumber; }
    public int GetMin() { return _minNumber; }
    public void SetMax(int max) 
    { 
        _maxNumber = max; 
        
    }
    public void SetMin(int min)
    {
        _minNumber = min;
    }
    public void ShowNumericalSelector()
    {
        if (!gameObject.activeSelf)
        {
            ResetNumber();
            gameObject.SetActive(true);
            
            if (InvManagerHelper.GetInvController().GetInputMode() == InputMode.Directional)
            {
                EventSystem.current.SetSelectedGameObject(_textNavigationTarget);
            }
        }
        
    }
    public void ShowNumericalSelector(int startingNumber)
    {
        if (!gameObject.activeSelf)
        {
            ShowNumericalSelector();
            _number = startingNumber;
            RenderNumbertoDisplay();
        }
        
    }
    public void ShowNumericalSelector(int minValue,int maxValue)
    {
        if (!gameObject.activeSelf)
        {
            ShowNumericalSelector();
            SetMin(minValue);
            SetMax(maxValue);
            _number = _minNumber;
            RenderNumbertoDisplay();
        }

    }
    public void HideNumericalSelector()
    {
        ResetNumber();
        gameObject.SetActive(false);


        if (InvManagerHelper.GetInvController() != null && ContextWindowHelper.IsContextWindowShowing())
        {
            if (InvManagerHelper.GetInvController().GetInputMode() == InputMode.Directional)
            {
                ContextWindowHelper.FocusOnLatestMenuOption();
            }
        }
        
    }
    public RectTransform GetRectTransform() { return GetComponent<RectTransform>(); }
    public RectTransform GetConfirmBtnRectTransform() { return _confirmBtnAnimator.GetComponent<RectTransform>(); }
    public RectTransform GetTextNavAreaRectTransform() { return _textNavigationTarget.GetComponent<RectTransform>(); }
    public void SubmitNumber()
    {
        if (gameObject.activeSelf && _confirmReady)
        {
            //Debug.Log($"submitting number [{_number}]");
            _confirmReady = false;
            OnNumberSubmitted?.Invoke(_number);
        }
    }
    public bool IsNumericalSelectorOpen()
    {
        return gameObject.activeSelf;
    }

    public void PlayValueChangeAudioFeedback()
    {
        if (_audioSource == null)
            _audioSource = InvManagerHelper.GetInvInteracterAudiosource();

        if (_audioSource != null && _onValueChangedAudioClip != null)
        {
            _audioSource.clip = _onValueChangedAudioClip;
            _audioSource.Play();
        }
    }
    public void VerifyInputOnEnd()
    {
        int number = int.Parse(_textDisplay.text);
        //Debug.Log($"Here's the verified number: {number}");
        _number = Mathf.Clamp(number, _minNumber, _maxNumber);
        //Debug.Log($"Here's the saved [and clamped] number: {_number}");
        RenderNumbertoDisplay();
    }
    public void TogglePointerMode(bool newState)
    {
        
        _pointerMode = newState;
        Debug.Log($"pointerMode: {_pointerMode}");
        if (_pointerConfirmBtn == null || _confirmBtnAnimator == null)
            return;

        _pointerConfirmBtn.interactable = _pointerMode;
        _confirmBtnAnimator.SetBool("pointerMode", _pointerMode);
    }
    public bool IsInPointerMode() { return _pointerMode; }
    

}
