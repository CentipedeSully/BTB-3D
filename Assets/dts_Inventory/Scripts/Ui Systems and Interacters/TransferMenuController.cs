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

        [SerializeField] private float _spacingBtwnOptions = 2;
        [SerializeField] private float _yPadding = 2;
        [SerializeField] private float _xPadding = 2;
        [SerializeField] private float _optionHeight;
        [SerializeField] private float _optionWidth;

        private ContextWindowController _contextController;
        private InvGrid _selectedInvGrid;
        private List<Button> _btnOptions = new();
        private List<InvWindow> _openContainers = new();
        private RectTransform _rectTransform;
        private GameObject _selectedOption;



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
            _activeOptionsContainer.gameObject.SetActive(true);
            _rectTransform = GetComponent<RectTransform>();
            
        }
        private void Start()
        {
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

            //ensure all options are recycled
            List<GameObject> activeOptions = new();

            //find all children that have options
            for (int i = 0; i < _activeOptionsContainer.childCount; i++)
            {
                if (_activeOptionsContainer.GetChild(i).GetComponent<TransferOptionDefinition>())
                    activeOptions.Add(_activeOptionsContainer.GetChild(i).gameObject);
                
            }

            //remove all of the options
            foreach (GameObject option in activeOptions)
            {
                option.SetActive(false);
                option.transform.SetParent(_unusedOptionsContainer);
            }

            activeOptions.Clear();


            //get the updated collection of opened containers
            _openContainers = InvManagerHelper.GetOpenedContainers();

            //rebuild the menu
            for (int i = 0; i < _openContainers.Count; i++)
            {
                //ignore the invGrid that is the donor
                if (InvManagerHelper.GetInvController().IsCurrentContextualInvGrid(_openContainers[i].GetItemGrid()))
                    continue;

                //create the new option for this container
                Button newOption = CreateNewOption(_openContainers[i]).GetComponent<Button>();

                //reparent the new option
                newOption.transform.SetParent(_activeOptionsContainer);
                newOption.gameObject.SetActive(true);

                //save the option
                _btnOptions.Add(newOption);
            }

            //resize the window to match the number of options
            float betwixtSpacing = _spacingBtwnOptions * (_btnOptions.Count - 1);
            float height = _yPadding + _optionHeight * _btnOptions.Count + _spacingBtwnOptions;
            float width = _xPadding + _btnOptions[0].GetComponent<RectTransform>().sizeDelta.x; //make sure the child fits well
            _rectTransform.sizeDelta = new Vector2(width, height);


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
        public void SaveSelectedOption(GameObject selectedBtnObject) { _selectedOption = selectedBtnObject; }
        public void SetContextMenuController(ContextWindowController controller) { _contextController = controller; }
        public void SetSelectedGrid(InvGrid invGrid) { _selectedInvGrid = invGrid; }
        public bool IsTransferMenuOpen() { return gameObject.activeSelf; }
        public RectTransform GetRectTransform() { return GetComponent<RectTransform>(); }
        public void ShowMenu(Vector3 position)
        {
            if (_containerOptionPrefab == null)
                return;

            if (!gameObject.activeSelf)
            {
                if (!InvManagerHelper.DoOpenedContainersExist())
                    return;

                BuildMenu();
                _rectTransform.position = position;
                gameObject.SetActive(true);

            }
                
        }
        public void OffsetMenu(Vector3 offset)
        {
            _rectTransform.localPosition += offset;
        }
        public void HideMenu()
        {
            if (gameObject.activeSelf)
            {
                if (ContextWindowHelper.IsMenuDarkened())
                    ContextWindowHelper.UndarkenContextmenu();
                gameObject.SetActive(false);
            }
        }
        
        public void SubmitSelection()
        {

            if (_selectedInvGrid == null || _contextController == null)
                return;

            Debug.Log("Transfer menu submitted the option selection!");
            _contextController.SaveTransferOption(_selectedOption.GetComponent<RectTransform>());
            _contextController.SpecifyAmount(ContextOption.TransferItem, _selectedInvGrid);
            _selectedOption = null;
            _selectedInvGrid = null;
        }

    }
}
