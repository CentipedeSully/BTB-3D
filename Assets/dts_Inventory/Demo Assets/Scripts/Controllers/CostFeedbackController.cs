using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



namespace dtsInventory
{
    public class CostFeedbackController : MonoBehaviour
    {
        //Declarations
        [SerializeField] private Color _defaultColor;
        [SerializeField] private Color _notEnoughColor;
        [SerializeField] private Color _positiveColor;
        [SerializeField] private Image _image;
        [SerializeField] private bool _isRelevant = true;
        [SerializeField] private bool _isEnough = false;
        [SerializeField] private Text _amountText;




        //monobehaviours
        private void Awake()
        {
            SetIsEnough(_isEnough);
        }




        //internals





        //externals
        public void SetIsEnough(bool value)
        {
            _isEnough = value;

            if (!_isRelevant)
                return;

            if (_isEnough)
                _image.color = _positiveColor;
            else _image.color = _notEnoughColor;
        }
        public bool IsEnough() { return _isEnough; }
        public bool IsRelevant() { return _isRelevant; }
        public void SetIsRelevant(bool value)
        {
            if (!_isRelevant)
                _image.color = _defaultColor;
        }
        public void SetAmountText(string newValue)
        {
            _amountText.text = newValue;
        }




    }
}
