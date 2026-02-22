using dtsInventory;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Animations.Rigging;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace dtsInventory
{
    public class TransferMenuController : MonoBehaviour
    {
        //Declarations
        [SerializeField] private GameObject _containerOptionPrefab;
        [SerializeField] private Transform _activeOptionsContainer;
        [SerializeField] private Transform _unusedOptionsContainer;
        [SerializeField] private UiDarkener _uiDarkener;

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
        private GameObject _latestSelectedObject;



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
            ForceImmediateUndarken();
            gameObject.SetActive(false);
            
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

            //rebuild the navigation for the buttons
            BuildNavigationData();

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
        private void BuildNavigationData()
        {
            Navigation navData;

            for (int i = 0; i < _btnOptions.Count; i++)
            {
                navData = _btnOptions[i].navigation;

                navData.mode = Navigation.Mode.Explicit;
                navData.wrapAround = true;

                //wrap to the first element [when down is pressed] if this index is the last element
                if (i == _btnOptions.Count - 1)
                    navData.selectOnDown = _btnOptions[0];
                //otherwise just go the next element in the list [when down is pressed]
                else
                    navData.selectOnDown = _btnOptions[i + 1];


                //wrap to the last element [when up is pressed] if this index is the first element
                if (i == 0)
                    navData.selectOnUp = _btnOptions[_btnOptions.Count -1];

                //otherwise just go the previous element in the list [when up is pressed]
                else
                    navData.selectOnUp = _btnOptions[i - 1];


                //update the btn's nav data
                _btnOptions[i].navigation = navData;
            }
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
                    ContextWindowHelper.UndarkenContextMenu();
                _contextController.SetGridSelection(null);
                gameObject.SetActive(false);
                _uiDarkener.ForceImmediateUndarken();

                //focus navigation on the last known context menu option if 1) the menu is open and 2) we are in directional mode
                if (ContextWindowHelper.IsContextWindowShowing() && InvManagerHelper.GetInvController().GetInputMode() == InputMode.Directional)
                    ContextWindowHelper.FocusOnLatestMenuOption();
            }
        }
        
        public void SubmitSelection()
        {

            if (_selectedInvGrid == null || _contextController == null)
                return;

            Debug.Log($"Transfer menu submitted the option selection!");
            _contextController.SaveTransferOption(_selectedOption.GetComponent<RectTransform>());
            _contextController.SetGridSelection(_selectedInvGrid);
            _contextController.SpecifyAmount(ContextOption.TransferItem);
            _latestSelectedObject = _selectedOption;
            _selectedOption = null;
            _selectedInvGrid = null;
        }

        public bool IsDarkened()
        {
            // return true if the image is dark, or is currently darkening
            return _uiDarkener.IsDarkened();
        }
        public void UndarkenTransferMenu() { _uiDarkener.UndarkenMenu(); }
        public void DarkenTransferMenu() { _uiDarkener.DarkenMenu(); }
        public void ForceImmediateUndarken() { _uiDarkener.ForceImmediateUndarken(); }
        public void SetSelectionToFirstElement()
        {
            if (_btnOptions.Count > 0)
            {
                NavHelper.SetCurrentNavObject(_btnOptions[0].gameObject);
                _latestSelectedObject = _btnOptions[0].gameObject;
            }
        }
        public void SetSelectionToLatestElement()
        {
            if (_latestSelectedObject != null)
                NavHelper.SetCurrentNavObject(_latestSelectedObject);
            else 
                SetSelectionToFirstElement();
        }
    }
}
