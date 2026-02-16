using dtsInventory;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private GameObject _textNavigationTarget;
    [SerializeField] private AudioClip _onValueChangedAudioClip;
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




    //internals
    private void RenderNumbertoDisplay()
    {
        _textDisplay.text = _number.ToString();
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
                EnterNumericalSelectionNavigation();
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
    public void SubmitNumber()
    {
        if (gameObject.activeSelf)
        {
            //Debug.Log($"submitting number [{_number}]");
            OnNumberSubmitted?.Invoke(_number);
        }
    }
    public bool IsNumericalSelectorOpen()
    {
        return gameObject.activeSelf;
    }

    public void EnterNumericalSelectionNavigation()
    {
        EventSystem.current.SetSelectedGameObject(_textNavigationTarget);
    }

    public bool IsLeftMostNavigationElementSelected()
    {
        return EventSystem.current.currentSelectedGameObject == _textNavigationTarget;
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

}
