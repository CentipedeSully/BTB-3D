using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyForce : MonoBehaviour
{
    [SerializeField] Rigidbody _target;
    [SerializeField] Vector3 _direction;
    [SerializeField] float _magnitude;
    [SerializeField] bool _applyForce;





    private void Update()
    {
        if (_applyForce && _target != null)
        {
            _applyForce = false;
            _target.AddForce(_direction * _magnitude * Time.deltaTime, ForceMode.Impulse);
        }
    }




}
