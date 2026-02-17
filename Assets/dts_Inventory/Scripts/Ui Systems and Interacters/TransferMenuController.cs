using dtsInventory;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

namespace dtsInventory
{
    public class TransferMenuController : MonoBehaviour
    {
        //Declarations
        [SerializeField] private GameObject _containerOptionPrefab;
        [SerializeField] private Transform _activeOptionsContainer;
        [SerializeField] private Transform _unusedOptionsContainer;
        private List<Button> _btnOptions = new();
        private List<InvWindow> _openContainers = new();
        private InvInteracter _invInteracter;
        private InvGrid _selectedGrid;
        private int _transferAmount;



        //monobehaviours
        private void Awake()
        {
            if (_containerOptionPrefab == null)
                Debug.LogError("No optionPrefab is provided for the tranferOptionMenu. This will cause problems if you use the 'transfer' menu.");
            else
            {
                if (_containerOptionPrefab.GetComponent<Button>() == null)
                    Debug.LogError($"TransferMenuOption Prefab missing necessary 'button' component.");
                if (_containerOptionPrefab.GetComponent<TransferOptionDefinition>() == null)
                    Debug.LogError($"TransferMenuOption Prefab missing necessary 'TransferOptionDefinition' component.");
            }

            _unusedOptionsContainer.gameObject.SetActive(false);
            
        }
        private void Start()
        {
            _invInteracter = InvManagerHelper.GetInvController();
            HideMenu();
        }




        //Internals
        private void BuildMenu()
        {
            if (_containerOptionPrefab == null)
                return;

            //clear the previous menu's utilities
            _openContainers.Clear();
            _btnOptions.Clear();

            //ensure any options are recycled
            foreach(Button btn in _activeOptionsContainer)
            {
                btn.gameObject.SetActive(false);
                btn.transform.SetParent(_unusedOptionsContainer);
            }

            //get the updated collection of opened containers
            _openContainers = _invInteracter.GetOpenedContainers();

            //rebuild the menu
            for (int i = 0; i < _openContainers.Count; i++)
            {
                //ignore the invGrid that is the donor
                if (_invInteracter.IsCurrentContextualInvGrid(_openContainers[i].GetItemGrid()))
                    continue;

                //create the new option for this container
                Button newOption = CreateNewOption(_openContainers[i]).GetComponent<Button>();

                //reparent the new option
                newOption.transform.SetParent(_activeOptionsContainer);

                //save the option
                _btnOptions.Add(newOption);
            }
        }

        private GameObject CreateNewOption(InvWindow invWindow)
        {
            if (_containerOptionPrefab == null)
            {
                Debug.LogError("No prefab exists for creating transfer menu options");
                return null;
            }
            GameObject newBtnObject = null;
            TransferOptionDefinition optionDef = null;


            //look for an unused btn to recycle
            for (int i = 0; i < _unusedOptionsContainer.childCount; i++)
            {
                //ensure the found object has our necessary componenets
                optionDef = _unusedOptionsContainer.GetChild(i).GetComponent<TransferOptionDefinition>();
                if (optionDef == null)
                    continue;

                if (_unusedOptionsContainer.GetChild(i).GetComponent<Button>() == null)
                    continue;

                //rewrite this new button object
                optionDef.SetInvGridReference(invWindow.GetItemGrid());
                optionDef.SetTransferMenuController(this);
                optionDef.SetButtonText(invWindow.ContainerName());

                //save this object as our new button
                newBtnObject = optionDef.gameObject;
                break;
            }

            //create a new obect if we didn't find any suitable unused btns
            if (newBtnObject == null)
            {
                newBtnObject = Instantiate(_containerOptionPrefab);
                optionDef = newBtnObject.GetComponent<TransferOptionDefinition>();

                //rewrite this new button object
                optionDef.SetInvGridReference(invWindow.GetItemGrid());
                optionDef.SetTransferMenuController(this);
                optionDef.SetButtonText(invWindow.ContainerName());
            }

            //return this newly created btn object!
            return newBtnObject;

        }



        //Externals
        public bool IsTransferMenuOpen() { return gameObject.activeSelf; }
        public RectTransform GetRectTransform() { return GetComponent<RectTransform>(); }
        public void ShowMenu(int transferAmount, Vector3 offset)
        {
            if (_containerOptionPrefab == null)
                return;

            if (!gameObject.activeSelf)
            {
                if (!_invInteracter.DoOpenedContainersExist())
                    return;


                
                _transferAmount = transferAmount;
                BuildMenu();
                GetComponent<RectTransform>().position += offset;
                gameObject.SetActive(true);

            }
                
        }
        public void HideMenu()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }
        public void SubmitSelection(InvGrid invGrid)
        {
            _selectedGrid = invGrid;

        }
        public InvGrid GetSelectionDecision() { return _selectedGrid; }
        public void ClearSelectionDecision() { _selectedGrid = null; _transferAmount = 0; }
    }
}
