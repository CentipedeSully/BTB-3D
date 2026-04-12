using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
        [SerializeField] private bool _showRestockResults;
        private IEnumerator _wareRegenerator;
        


        [Header("Debug")]
        [SerializeField] private bool _isDebugActive = false;
        [SerializeField] private bool _cmdToggleUi = false;
        [SerializeField] private bool _cmdRerollStock;


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
        private IEnumerator RegenStockAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            RepopulateMerhcantContainer();
            _wareRegenerator = null;
        }
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

        private void RepopulateMerhcantContainer()
        {
            if (_merchantInvWindow == null)
            {
                Debug.LogWarning("Attempted to regenerate merchant wares when merchant isn't initialized yet. Open the merchant to generate its wares the first time");
                return;
            }
            //first, remove all items from the merchant's container
            InvGrid merchantGrid = _merchantInvWindow.GetItemGrid();
            int removalIterations = 0;
            while (merchantGrid.GetAllStacks().Count > 0 && removalIterations <=1000) //this shouldn't take 1000 iterations. This demo doesn't contain 1000 different itemDatas
            {
                ItemData chosenItem = merchantGrid.GetAllStacks().First().Value;
                merchantGrid.RemoveItem(chosenItem, merchantGrid.CountItem(chosenItem));
                removalIterations++;
            }

            if (removalIterations > 1000 && merchantGrid.GetAllStacks().Count > 0)
            {
                Debug.LogWarning($"Failsafe detected: averted an infinite while loop while attempting to remove all items from a merchant's container {merchantGrid.name}. Aborting operation.");
                return;
            }

            //now reroll the merchant's loot
            List<bool> lootRollResults = ContainerController.RollForLoot(_stockChancesList, _showRestockResults);

            //add all successfully-rolled items to the container
            for (int i = 0; i < _stockChancesList.Count; i++)
            {

                if (lootRollResults[i] == true)
                {
                    int amountToAdd = UnityEngine.Random.Range(_stockChancesList[i].minAmount, _stockChancesList[i].maxAmount + 1);

                    if (_showRestockResults)
                        Debug.Log($"Rolled Amount to add: {amountToAdd}");

                    _merchantInvWindow.GetItemGrid().AddItem(_stockChancesList[i].itemdata, amountToAdd);
                }

            }
        }


        //externals
        public void OpenMerchantUi()
        {

            if (_merchantInvWindow == null)
            {
                CreateNewMerchantUi();

                //Don't forget to Restock
                RegenerateWares();
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

        public void RegenerateWares()
        {
            //only attempt to regen if we aren't already regen'ing
            if (_wareRegenerator == null)
            {
                _wareRegenerator = RegenStockAtEndOfFrame();
                StartCoroutine( _wareRegenerator );
            }
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
            if (_cmdRerollStock)
            {
                _cmdRerollStock = false;
                RegenerateWares();
            }
        }

       
    }
}

