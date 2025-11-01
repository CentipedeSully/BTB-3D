using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camshaker : MonoBehaviour
{
    //Declarations
    [SerializeField] private bool _isShaking = false;
    [SerializeField] private float _repositionFrequency;
    [SerializeField] private float _shakeDuration;
    private float _currentTime;
    [SerializeField] private float _shakeMagnitude;



    //monobehaviours
    private void Awake()
    {
        CamshakeHelper.SetCamShaker(this);
    }

    private void Update()
    {
        if (_isShaking)
            ShakeCamera();
    }



    //internals
    private void ShakeCamera()
    {

        _currentTime += Time.deltaTime;

        if (_currentTime >= _shakeDuration)
            ResetCamShakeUtils();
    }

    private void RandomlyRepositionCamera()
    {
        float xPosition = Random.Range(-_shakeMagnitude, _shakeMagnitude);
        float yPosition = Random.Range(-_shakeMagnitude, _shakeMagnitude);

        transform.localPosition = new Vector3(xPosition, yPosition, transform.localPosition.z);
    }

    private void ResetCamShakeUtils()
    {
        //reset all utils
        _currentTime = 0;
        CancelInvoke(nameof(RandomlyRepositionCamera));
        _isShaking = false;

        //reset cam position to the local origin
        transform.localPosition = Vector3.zero;
    }


    //externals
    public void ShakeCam(float magnitude, float duration)
    {
        _shakeMagnitude = magnitude;
        _shakeDuration = duration;

        ResetCamShakeUtils();

        _isShaking = true;
        InvokeRepeating(nameof(RandomlyRepositionCamera), 0, _repositionFrequency);

    }
}
