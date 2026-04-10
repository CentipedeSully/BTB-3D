using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace dtsInventory
{
    public class MerchantController : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private GameObject _merchantInvWindowPrefab;
        private Canvas _canvas;
        private InvWindow _merchantInvWindow;

        [Header("Stock Settings")]
        [SerializeField] private float _interactRadius = 1.5f;
        [SerializeField] private List<LootRoll> _stockChancesList;
        [SerializeField] private bool _rerollStock;


        [Header("Debug")]
        [SerializeField] private bool _isDebugActive = false;
        [SerializeField] private bool _cmdToggleUi = false;


        //monobehaviours
        private void OnDrawGizmos()
        {
            /*
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, _interactRadius);
            */
        }
        private void Update()
        {
            if (_isDebugActive)
                ListenForDebugCommands();
        }

        private void OnDestroy()
        {
            if (_merchantInvWindow != null)
            {
                Destroy(_merchantInvWindow.gameObject);
            }
        }



        //internals
        private void CreateNewMerchantUi()
        {
            if (_merchantInvWindow == null)
            {
                _canvas = InvManagerHelper.GetUiCanvas();
                GameObject newUi = Instantiate(_merchantInvWindowPrefab);
                _merchantInvWindow = newUi.GetComponent<InvWindow>();
                _merchantInvWindow.RenameContainer(gameObject.name);
                _merchantInvWindow.GetItemGrid().SetAsMerchant(true);
                InvManagerHelper.TrackNewInvWindow(_merchantInvWindow);
                _merchantInvWindow.GetComponent<RectTransform>().position = _canvas.renderingDisplaySize/2;
                _merchantInvWindow.SetItemValueLabel("Price:");
                
            }
        }




        //externals
        public void OpenMerchantUi()
        {

            if (_merchantInvWindow == null)
            {
                CreateNewMerchantUi();

                //Don't forget to Restock
                //...
            }

            if (!_merchantInvWindow.IsWindowOpen())
                _merchantInvWindow.OpenWindow();

        }

        public void CloseMerchantUi()
        {
            if (_merchantInvWindow == null)
            {
                Debug.LogWarning($"Failed to Close MerchantUi: No merchantInvWindow exists for Merchant '{gameObject.name}'");
                return;
            }

            if (_merchantInvWindow.IsWindowOpen())
                _merchantInvWindow.CloseWindow();
        }

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public void TriggerInteraction()
        {
            OpenMerchantUi();
        }

        public void EndInteraction()
        {
            CloseMerchantUi();
        }

        public float InteractDistance()
        {
            return _interactRadius;
        }



        //Debug
        private void ListenForDebugCommands()
        {
            if (_cmdToggleUi)
            {
                _cmdToggleUi = false;

                if (_merchantInvWindow == null)
                    OpenMerchantUi();
                else if (_merchantInvWindow.IsWindowOpen())
                    CloseMerchantUi();
                else OpenMerchantUi();
                
            }
        }

       
    }
}

