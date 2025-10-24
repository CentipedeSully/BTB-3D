using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HpBarManager : MonoBehaviour
{
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private HealthBehavior _healthBehaviour;


    private void OnEnable()
    {
        _healthBehaviour.OnHealthChanged += UpdateBar;
    }

    private void OnDisable()
    {
        _healthBehaviour.OnHealthChanged -= UpdateBar;
    }


    public void UpdateBar(int newValue)
    {
        if (_hpSlider == null)
            return;

        _hpSlider.maxValue = _healthBehaviour.GetMaxHp();
        _hpSlider.value = newValue;
    }
}
