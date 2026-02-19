using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiDarkener : MonoBehaviour
{
    [SerializeField] private Image _darkenEffectImage;
    [SerializeField] private float _darkenDuration;
    [SerializeField] private float _maxDarkness;
    private bool _isDarkenInProgress = false;
    private float _alpha;
    private float _currentDarkenTime;
    private float _targetDarknessValue;
    private float _startingDarknessValue;


    private void Awake()
    {

        _darkenEffectImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_isDarkenInProgress)
            UpdateDarkeningEffects();
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



    public bool IsDarkened()
    {
        // return true if the image is dark, or is currently darkening
        return (_darkenEffectImage.color.a == _maxDarkness) || (_isDarkenInProgress && _targetDarknessValue == _maxDarkness);
    }
    public void DarkenMenu()
    {
        //ignore command if the grid is already dark
        if (_darkenEffectImage.color.a == _maxDarkness)
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
}
