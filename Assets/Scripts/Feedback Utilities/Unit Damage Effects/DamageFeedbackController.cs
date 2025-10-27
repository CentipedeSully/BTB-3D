using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageFeedbackController : MonoBehaviour
{
    //Declarations
    [Header("References")]
    [SerializeField] private Transform _bodyContainer;
    [SerializeField] private HealthBehavior _healthBehavior;

    [Header("Settings")]
    [SerializeField] private float _staggerDuration;
    private Vector3 _originalBodyScale;
    private Vector3 _originalBodyPosition;
    private Vector3 _currentStaggerScale;
    private Vector3 _currentStaggerPosition;
    private float _xScaleCurveValue;
    private float _yScaleCurveValue;
    private float _zScaleCurveValue;
    private float _xPositionCurveValue;
    private float _yPositionCurveValue;
    private float _zPositionCurveValue;

    [Header("Scale Animation On Stagger")]
    [SerializeField] private AnimationCurve _xScaleAnimCurve;
    [SerializeField] private AnimationCurve _yScaleAnimCurve;
    [SerializeField] private AnimationCurve _zScaleAnimCurve;

    [Header("Position Animation On Stagger")]
    [SerializeField] private AnimationCurve _xPositionAnimCurve;
    [SerializeField] private AnimationCurve _yPositionAnimCurve;
    [SerializeField] private AnimationCurve _zPositionAnimCurve;
    private bool _isStaggering = false;
    
    private float _currentTime;
    

    [Header("Debug")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private bool _cmdStaggerUnit = false;







    //Monobehaviours
    private void Awake()
    {
        _originalBodyScale = _bodyContainer.localScale;
        _originalBodyPosition = _bodyContainer.localPosition;
    }

    private void OnEnable()
    {
        _healthBehavior.OnDamaged += StaggerUnit;
    }

    private void OnDisable()
    {
        _healthBehavior.OnDamaged -= StaggerUnit;
    }

    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();

        if (_isStaggering)
            ApplyStaggerAnimationTransformation();
    }


    //internals
    private void ApplyStaggerAnimationTransformation()
    {
        //tick the time passed
        _currentTime += Time.deltaTime;
        
        //separately evaluate the different animation curves
        _xScaleCurveValue = _xScaleAnimCurve.Evaluate(_currentTime/_staggerDuration);
        _yScaleCurveValue = _yScaleAnimCurve.Evaluate(_currentTime/_staggerDuration);
        _zScaleCurveValue = _zScaleAnimCurve.Evaluate(_currentTime/_staggerDuration);

        _xPositionCurveValue = _xPositionAnimCurve.Evaluate(_currentTime/_staggerDuration);
        _yPositionCurveValue = _yPositionAnimCurve.Evaluate(_currentTime/_staggerDuration);
        _zPositionCurveValue = _zPositionAnimCurve.Evaluate(_currentTime/_staggerDuration);

        //build the current transformation vectors (for readability)
        _currentStaggerScale.x = _xScaleCurveValue;
        _currentStaggerScale.y = _yScaleCurveValue;
        _currentStaggerScale.z = _zScaleCurveValue;

        _currentStaggerPosition.x = _xPositionCurveValue;
        _currentStaggerPosition.y = _yPositionCurveValue;
        _currentStaggerPosition.z = _zPositionCurveValue;

        //apply this frames transformations
        _bodyContainer.localScale = _currentStaggerScale;
        _bodyContainer.localPosition = _currentStaggerPosition;
        

        if (_currentTime >= _staggerDuration)
        {
            _currentTime = 0;
            _isStaggering = false;
        }

    }


    //externals
    public void StaggerUnit()
    {
        _isStaggering = true;
        _currentTime = 0;

    }


    //Debug
    private void ListenForDebugCommands()
    {
        if (_cmdStaggerUnit)
        {
            _cmdStaggerUnit = false;
            StaggerUnit();
        }
    }
}
