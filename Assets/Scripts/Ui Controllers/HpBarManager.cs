using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HpBarManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private HealthBehavior _healthBehaviour;
    [SerializeField] private Animator _hpBarAnimator;

    [Header("Settings")]
    [SerializeField] private float _transitionTime = .25f;

    private float _currentTime = 0;
    private bool _isTransitioning = false;
    private float _startingValue;
    private float _currentValue;
    private float _targetValue;


    //monobehaviours
    private void OnEnable()
    {
        _healthBehaviour.OnHealthChanged += UpdateBar;
    }

    private void OnDisable()
    {
        _healthBehaviour.OnHealthChanged -= UpdateBar;
    }

    private void Update()
    {
        if (_isTransitioning)
            LerpTransition();
    }


    //internals
    private void LerpTransition()
    {
        _currentTime += Time.deltaTime;

        _currentValue = Mathf.Lerp(_startingValue, _targetValue, _currentTime / _transitionTime);
        _hpSlider.value = _currentValue;
        UpdateHpVisibility();

        if (_currentTime >= _transitionTime)
        {
            _currentTime = 0;
            _isTransitioning = false;
        }
    }

    private void UpdateHpVisibility()
    {
        if (_hpBarAnimator == null)
            return;

        if (_currentValue == 0 || _currentValue >= _healthBehaviour.GetMaxHp())
            _hpBarAnimator.SetBool("isHpInBetween", false);
        else _hpBarAnimator.SetBool("isHpInBetween", true);
    }




    //externals
    public void UpdateBar(int newValue)
    {
        if (_hpSlider == null)
            return;

        //reset all lerp utils, in case we're in the middle of a lerp
        _currentTime = 0;
        _isTransitioning = true;
        _startingValue = _currentValue;
        _targetValue = newValue;

        //also, update the maxValue if it changed
        _hpSlider.maxValue = _healthBehaviour.GetMaxHp();
        
    }
}
