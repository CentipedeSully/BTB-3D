
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace dtsInventory
{
    public enum InputMode
    {
        Pointer,
        Directional
    }
    

    public class InvInteracter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ContextWindowController _contextWindowController;
        [SerializeField] private Text _heldStackText;
        [SerializeField] private RectTransform _heldStackContainer;
        [SerializeField] private GameObject _pointerContainer;
        [Tooltip("Used to preserve the pointerContainer when in directional mode while no grid exists.")]
        [SerializeField] private Transform _homePointerContainer;
        [SerializeField] private RectTransform _inputLockedUiFeedback;
        [SerializeField] private Canvas _uiCanvas;
        [SerializeField] private GraphicRaycaster _graphicRaycaster;
        [SerializeField] private GameObject _hoverGraphicPrefab;
        [SerializeField] private GameObject _directionalPointerHoverPrefab;
        [SerializeField] private InvGrid _homeInventoryGrid;
        [Tooltip("Where all Inventory Windows should exist. " +
            "Any Spawned invWindows will be a child of this transform.")]
        [SerializeField] private Transform _inventoryWindowsContainer;
        private HashSet<InvWindow> _knownInvWindows = new();
        [SerializeField]private List<InvWindow> _openedInvWindows = new();
        [SerializeField]private List<InvWindow> _openedMerchants = new();
        private RectTransform _defaultParentOfPointerContainer;
        private InvGrid _invGrid;
        private InvWindow _currentHoveredWindow;

        [SerializeField] private List<RectTransform> _containerRelatedUiControls = new();


        [Header("Input Settings")]
        [Tooltip("Completely stops ALL inputs (pointer commands and directional hotkeys) from affecting any Inv-related system." +
            " Useful if you need to ignore all inventory-related inputs for an unspecified amount of time")]
        [SerializeField] private bool _lockInvSystem = false;
        [Tooltip("How sensitive should scrolling the mousewheel feel")]
        [SerializeField] private float _scrollMultiplier = 1;

        [Header("Audio Settings")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _movementAudio;
        [SerializeField] private AudioClip _rotateAudio;
        [SerializeField] private AudioClip _selectionAudio;
        [SerializeField] private AudioClip _deselectionAudio;
        [SerializeField] private AudioClip _discardAudio;
        [SerializeField] private AudioClip _useAudio;
        [SerializeField] private AudioClip _openedAudio;
        [SerializeField] private AudioClip _closedAudio;

        [Header("Watch/Debug Values [Do Not Touch]")]
        [Tooltip("AutoDetects and updates based on the last detected input command. No need to touch this")]
        [SerializeField] private InputMode _inputMode = InputMode.Pointer;
        [SerializeField] private InvItem _heldItem;
        [SerializeField] private int _heldItemStackCount = 0;
        [SerializeField] CellInteract _hoveredCell;
        [SerializeField] Vector2Int _hoverIndex;
        [SerializeField] InvItem _itemInCell;
        [SerializeField] private GameObject _currentWindow;
        


        private RectTransform _pointerRectTransform;
        private Vector2 _localPoint;
        
        (int, int) _hoveredCellIndex = (-1, -1);
        (int, int) _contextualItemPosition = (-1, -1);
        private InvGrid _contextualInvGrid;
        private InvGrid _contextualGridReceiver;
        
        HashSet<(int, int)> _itemCellOccupancy = new();
        private List<ContextOption> _itemContextualOptions = new();

        
        List<GameObject> _unusedHoverTileGraphics = new();
        List<GameObject> _hoverTileGraphics = new();
        private HashSet<(int, int)> _hoveredIndexes = new();
        private HashSet<(int, int)> _lastFramesHoveredIndexes = new();
        private GameObject _pointerHoverTileObject;
        private RectTransform _pointerHoverTileRectTransform;

        (int, int) _lastKnownHoveredIndex;
        InvGrid _lastKnownGrid;

        private bool _altCmd;
        private bool _altCmd2;
        private bool _altCmd3;

        private Vector2 _mousePosition;
        private float _scrollDelta;
        private bool _isInventoryInteractionsLocked = false;
        private bool _ignoreConfirmUntilDelayExpires = false;
        private IEnumerator _resetIgnoreConfirmFlagCoroutine;
        private float _ignoreDelay = .05f;
        private GameObject _selectedObjectBeforeLockdown;
        


        //Monobehaviours
        private void Awake()
        {

            InvManagerHelper.SetInventoryController(this);
            _pointerRectTransform = _pointerContainer.GetComponent<RectTransform>();
            _defaultParentOfPointerContainer = _pointerContainer.transform.parent.GetComponent<RectTransform>();
            //Debug.Log($"detected parent of pointer container: {_defaultParentOfPointerContainer.name}");
            
            //create our directional pointer hover utility.
            //This aids the player with seeing where the directional pointer is on the grid
            if (_pointerHoverTileObject == null && _directionalPointerHoverPrefab != null)
            {
                //create and store our hover util on the pointer container.
                //This will be where we store it when not in use.
                _pointerHoverTileObject = Instantiate(_directionalPointerHoverPrefab, _pointerContainer.transform);
                _pointerHoverTileRectTransform = _pointerHoverTileObject.GetComponent<RectTransform>();

                //hide the utility
                _pointerHoverTileObject.SetActive(false);

            }
            
        }
        private void Start()
        {
            ContextWindowHelper.SetPointerMode(true);

        }


        private void OnEnable()
        {
            _contextWindowController.OnOptionSelected += ListenForValidContextualOption;

            if (_inventoryWindowsContainer == null)
            {
                Debug.LogWarning("Please Ensure a parent container is specified for all InventoryWindow Ui. " +
                    "Window creation/management will encounter unexpected results otherwise.");
            }

            //Begin tracking preexisting inventories along with their open/closed status
            ReadUiContainerForPreexistingInventoryWindows();

            //sub to all currently-known windows
            foreach (InvWindow window in _knownInvWindows)
                SubscribeToWindow(window);

        }

        private void OnDisable()
        {
            _contextWindowController.OnOptionSelected -= ListenForValidContextualOption;

            //reset any currently-firing coruoutines/ time-based utilities
            if (_ignoreConfirmUntilDelayExpires)
            {
                _ignoreConfirmUntilDelayExpires = false;
                if (_resetIgnoreConfirmFlagCoroutine != null)
                {
                    StopCoroutine(_resetIgnoreConfirmFlagCoroutine);
                    _resetIgnoreConfirmFlagCoroutine = null;
                }

            }

            //Stop tracking all currently-known windows
            while (_knownInvWindows.Count > 0)
            {
                InvWindow window = _knownInvWindows.First();
                UntrackInvWindow(window);
            }
        }



        private void Update()
        {
            if (!_lockInvSystem)
                VisualizeHover();
        }


        //internals

        //window management utilitites
        private void ReadUiContainerForPreexistingInventoryWindows()
        {
            for (int i = 0; i < _inventoryWindowsContainer.childCount; i++)
            {
                InvWindow childInvWindow = _inventoryWindowsContainer.GetChild(i).GetComponent<InvWindow>();

                if (childInvWindow == null)
                    continue;

                TrackInvWindow(childInvWindow);
            }
        }
        public void TrackInvWindow(InvWindow window)
        {
            if (window == null)
                return;

            if (!IsWindowBeingTracked(window))
            {
                //add this window to our awareness
                _knownInvWindows.Add(window);

                //also track whether or not the found window is opened
                if (window.IsWindowOpen())
                {
                    UpdateWindowAsOpened(window);

                }
                
                //subscribe to the window's open/close events
                SubscribeToWindow(window);

                /*
                Debug.Log($"Window [{window.name}] now being tracked.\n" +
                    $"Is Window open?: {window.IsWindowOpen()}\n" +
                    $"Is Window in the opened container?: {_openedInvWindows.Contains(window)}");
                */
            }
        }
        public void UntrackInvWindow(InvWindow window)
        {
            if (window == null)
                return;

            if (_knownInvWindows.Contains(window))
            {
                //remove this window from our awareness
                _knownInvWindows.Remove(window);

                //also ensure the window is removed from our 'opened' memory
                UpdateWindowAsClosed(window);

                //unsub from the window's open/close events
                UnsubFromWindow(window);
                
            }
        }
        public bool IsWindowBeingTracked(InvWindow window)
        {
            return _knownInvWindows.Contains(window);
        }
        public void ParentWindowToContainer(InvWindow window)
        {
            if (window == null) return;

            window.transform.SetParent(_inventoryWindowsContainer);

        }

        public void SetWindowToFront(InvWindow invWindowUi)
        {
            if (_openedInvWindows.Contains(invWindowUi))
            {
                invWindowUi.transform.SetAsLastSibling();
            }
                
        }
        public void FocusOnWindowForDirectionalInput(InvWindow currentWindow)
        {
            if (currentWindow != null)
            {
                //double check if the grid got properly initialized
                if (currentWindow.GetItemGrid().ContainerSize().y < 1 || currentWindow.GetItemGrid().ContainerSize().x < 1)
                {
                    Debug.LogWarning($"InvGrid '{currentWindow.GetItemGrid().name}' has an invalid containerSize [{currentWindow.GetItemGrid().ContainerSize().x},{currentWindow.GetItemGrid().ContainerSize().y}]\n" +
                        $"Ignoring 'FocusOnWindow' command");
                    return;
                }

                //set this window's invGrid as the latest active grid
                SetActiveItemGrid(currentWindow.GetItemGrid());

                //set the hovered cell to the grid's origin
                SetHoveredCell(currentWindow.GetItemGrid().GetCellObject((0, 0)));

                BindPointerContainerToCellPosition(_invGrid);

                SetWindowToFront(currentWindow);

            }

        }
        public void FocusOnWindowForPointerInput(InvWindow currentWindow)
        {
            if (currentWindow == null)
                return;

            ClearHoveredCell();
            SetActiveItemGrid(currentWindow.GetItemGrid());
            
            SetWindowToFront(currentWindow);
        }
        public void FocusOnNextOpenedWindow()
        {
            //ignore command if too few opened windows exist
            if (_openedInvWindows.Count < 2)
                return;

            //default if we aren't on a grid
            if (_invGrid == null)
            {
                //default to the first window we're aware of
                FocusOnWindowForDirectionalInput(_openedInvWindows[0]);
                return;
            }

            //also default if our curent grid is closed (if our hover isn't updated for some reason)
            else if (!_invGrid.GetParentWindow().IsWindowOpen())
            {
                //default to the first window we're aware of
                FocusOnWindowForDirectionalInput(_openedInvWindows[0]);
                return;
            }

            //get our current window index
            int currentWindowIndex = _openedInvWindows.IndexOf(_invGrid.GetParentWindow());

            //wrap to the first window if we're at the end of the list
            if (currentWindowIndex == _openedInvWindows.Count -1)
            {
                FocusOnWindowForDirectionalInput(_openedInvWindows[0]);
                return;
            }

            //otherwise just focus on the next window in the list
            else
            {
                FocusOnWindowForDirectionalInput(_openedInvWindows[currentWindowIndex + 1]);
                return;
            }
        }
        public void FocusOnPreviousOpenedWindow()
        {
            //ignore command if too few opened windows exist
            if (_openedInvWindows.Count < 2)
                return;

            //default if we aren't on a grid
            if (_invGrid == null)
            {
                //default to the first window we're aware of
                FocusOnWindowForDirectionalInput(_openedInvWindows[0]);
                return;
            }

            //also default if our curent grid is closed (if our hover isn't updated for some reason)
            else if (!_invGrid.GetParentWindow().IsWindowOpen())
            {
                //default to the first window we're aware of
                FocusOnWindowForDirectionalInput(_openedInvWindows[0]);
                return;
            }

            //get our current window index
            int currentWindowIndex = _openedInvWindows.IndexOf(_invGrid.GetParentWindow());

            //wrap to the last window if we're at the beginning of the list
            if (currentWindowIndex == 0)
            {
                FocusOnWindowForDirectionalInput(_openedInvWindows[_openedInvWindows.Count -1]);
                return;
            }

            //otherwise just focus on the next window in the list
            else
            {
                FocusOnWindowForDirectionalInput(_openedInvWindows[currentWindowIndex - 1]);
                return;
            }
        }
        private void SubscribeToWindow(InvWindow window)
        {
            window.OnWindowOpened += UpdateWindowAsOpened;
            window.OnWindowClosed += UpdateWindowAsClosed;
            window.OnWindowDestoryed += RespondToWindowDestroyed;
            //Debug.Log($"subscribed to '{window.name}. Currently subscribed to {_knownInvWindows.Count} windows'");
        }
        private void UnsubFromWindow(InvWindow window)
        {
            window.OnWindowOpened -= UpdateWindowAsOpened;
            window.OnWindowClosed -= UpdateWindowAsClosed;
            window.OnWindowDestoryed -= RespondToWindowDestroyed;
            //Debug.Log($"Unsubbed from '{window.name}'. Currently subscribed to {_knownInvWindows.Count} windows'");
        }
        private void RespondToWindowDestroyed(InvWindow window)
        {
            UntrackInvWindow(window);

            //reclaim any resources that're currently focused on this window
            if (_invGrid = window.GetItemGrid())
            {
                ClearHoverTiles();

                if (_inputMode == InputMode.Directional)
                {
                    if (_openedInvWindows.Count > 1)
                        FocusOnNextOpenedWindow();
                    else
                    {
                        ReturnPointerContainerToHomeTransform();
                        ClearDirectionalPointer();
                    }
                }
                
            }

        }
        
        public void UpdateWindowAsOpened(InvWindow window)
        {
            if (!_openedInvWindows.Contains(window))
            {
                _openedInvWindows.Add(window);
                
                //also track if a merchant's ui was opened, for contextOption reasons
                if (window.GetItemGrid().IsMerchant())
                    _openedMerchants.Add(window);

                InputFilter.DisallowNonUiInput(window.gameObject);

                if (window.GetItemGrid() != _homeInventoryGrid)
                    PlayContainerOpenedAudio();

                ShowContainerRelatedUiControls();
            }
        }
        public void UpdateWindowAsClosed(InvWindow window)
        {
            if (_openedInvWindows.Contains(window))
            {
                _openedInvWindows.Remove(window);

                //also track if a merchant's ui was closed, for contextOption reasons
                if (window.GetItemGrid().IsMerchant())
                    _openedMerchants.Remove(window);

                InputFilter.AllowNonUiInput(window.gameObject);

                if (window.GetItemGrid() != _homeInventoryGrid)
                    PlayContainerClosedAudio();

                if (_openedInvWindows.Count == 0)
                    HideContainerRelatedUicontrols();
            }
        }


        //visual updating/rendering utilities
        private void VisualizeHover()
        {

            //only visualize hover effects when the context window isn't open
            if (ContextWindowHelper.IsContextWindowShowing())
                return;

            //save the previous frame's hoverData 
            _lastFramesHoveredIndexes.Clear();
            foreach ((int, int) index in _hoveredIndexes)
                _lastFramesHoveredIndexes.Add(index);

            //reset the present frame's hover data
            _hoveredIndexes.Clear();

            //if the pointer is the input mode, the update the visuals based on the pointer's data
            if (_inputMode == InputMode.Pointer)
            {
                //if we're not hovering over anything, stop here
                if (_hoveredCell == null)
                {
                    _itemInCell = null;
                    ClearHoverTiles();
                }

                else
                {
                    _itemInCell = _invGrid.GetItemGraphicOnCell(_hoverIndex.x, _hoverIndex.y);
                    InvItem hoveredItem = _invGrid.GetItemGraphicOnCell(_hoverIndex.x, _hoverIndex.y);


                    //highlight the previewed position of the held item
                    if (_heldItem != null)
                    {
                        //Get the new potential placement positions
                        HashSet<(int, int)> placementPositions = _invGrid.ConvertSpacialDefIntoGridArea(_hoveredCellIndex, _heldItem.GetSpacialDefinition(), _heldItem.ItemHandle());

                        if (_invGrid.IsAreaWithinGrid(placementPositions))
                        {
                            //Looks like our placement is valid here.
                            //Make our hoveredIndexes equal to our expected placement position
                            foreach ((int, int) index in placementPositions)
                                _hoveredIndexes.Add(index);

                            //Don't change anything if the hover data didn't change
                            if (!DidHoverDataChange())
                                return;

                            //clear and rerender the updated hover tiles
                            ClearHoverTiles();
                            RenderHoverTiles();
                            string currencyUnit = ItemCreatorHelper.GetEconomySetting().GetCurrencyUnit();

                            string itemValue;

                            //ensure no value is displayed if the item has no value
                            if (_heldItem.ItemData().ItemValue() <= 0)
                            {
                                itemValue = "---";
                                currencyUnit = "";
                            }
                            else itemValue = _heldItem.ItemData().ItemValue().ToString();


                            if (!_invGrid.IsMerchant())
                            {
                                

                                RenderItemInfo(_heldItem.ItemData().Name(), _heldItem.ItemData().Desc(), itemValue, "Value:", currencyUnit);
                            }
                            else
                            {
                                //we need to show the merchan't offer for the item(s) in question
                                int merchantOffer = ItemData.CalculatePrice(_heldItem.ItemData(), 1, _invGrid.GetSellingPriceMultiplier());
                                string offerString;

                                if (merchantOffer <= 0)
                                    offerString = "---";
                                else offerString = merchantOffer.ToString();

                                RenderItemInfo(_heldItem.ItemData().Name(), _heldItem.ItemData().Desc(), offerString, "Offer:", currencyUnit);
                            }
                            
                        }

                        //just clear the hover tiles, if any exist
                        else
                            ClearHoverTiles();
                    }

                    //highlight the hovered item if no item is held
                    else if (hoveredItem != null)
                    {
                        //Get the item's occupancy as the new frame's hovered data
                        string indexesString = "";
                        foreach ((int, int) index in _invGrid.GetStackArea(_hoveredCellIndex))
                        {
                            _hoveredIndexes.Add(index);
                            indexesString += index.ToString() + "\n";
                        }
                        //Debug.Log("Cell Indexes:\n"+indexesString);


                        //Don't change anything if the hover data didn't change
                        if (!DidHoverDataChange())
                            return;

                        //clear and rerender the updated hover tiles
                        ClearHoverTiles();
                        RenderHoverTiles();
                        string currencyUnit = ItemCreatorHelper.GetEconomySetting().GetCurrencyUnit();

                        string itemValue;
                        //ensure no value is displayed if the item has no value
                        if (hoveredItem.ItemData().ItemValue() <= 0)
                        {
                            itemValue = "---";
                            currencyUnit = "";
                        }
                        else itemValue = hoveredItem.ItemData().ItemValue().ToString();


                        if (!_invGrid.IsMerchant())
                        {
                            
                            RenderItemInfo(hoveredItem.ItemData().Name(), hoveredItem.ItemData().Desc(),itemValue, "Value:", currencyUnit);
                        }
                        else
                        {
                            //the merchant should be showing the item's individual unit price
                            int merchantPrice = ItemData.CalculatePrice(hoveredItem.ItemData(), 1, _invGrid.GetBuyingPriceMultiplier());
                            string priceString;
                            if (merchantPrice <= 0)
                                priceString = "---";
                            else priceString = merchantPrice.ToString();
                            RenderItemInfo(hoveredItem.ItemData().Name(), hoveredItem.ItemData().Desc(), priceString, "Price:", currencyUnit);
                        }
                    }

                    //highlight the cell position
                    else if (_heldItem == null && hoveredItem == null)
                    {
                        _hoveredIndexes.Add(_hoveredCellIndex);

                        //clear and rerender the updated hover tiles
                        ClearHoverTiles();
                        RenderHoverTiles();

                    }

                }
            }

            //otherwise, the directionalInput should always be hovering over a gridspace
            else if (_inputMode == InputMode.Directional)
            {
                //if an inventory grid doesnt exist atm, then attempt to default to the last know grid's origin
                if (_invGrid == null)
                {
                    //we don't have a history of the last hovered grid
                    //(it typically defaults to the home grid, so that doesnt exist either)
                    //So stop trying to update the visuals
                    if (_lastKnownGrid == null)
                        return;

                    //our last know grid isn't set up properly. stop trying to update the visuals
                    if (_lastKnownGrid.ContainerSize().x <= 0 || _lastKnownGrid.ContainerSize().y <= 0)
                        return;

                    FocusOnWindowForDirectionalInput(_lastKnownGrid.GetParentWindow());
                    //SetActiveItemGrid(_lastKnownGrid);
                    //SetHoveredCell(_lastKnownGrid.GetCellObject((0,0)));
                }

                //in directional mode, always render the directional-hover pointer
                RenderDirectionalPointer();


                //now proceed with visualizing the hover data
                _itemInCell = _invGrid.GetItemGraphicOnCell(_hoverIndex.x, _hoverIndex.y);
                InvItem hoveredItem = _invGrid.GetItemGraphicOnCell(_hoverIndex.x, _hoverIndex.y);


                //highlight the previewed position of the held item
                if (_heldItem != null)
                {
                    //Get the new potential placement positions
                    HashSet<(int, int)> placementPositions = _invGrid.ConvertSpacialDefIntoGridArea(_hoveredCellIndex, _heldItem.GetSpacialDefinition(), _heldItem.ItemHandle());

                    if (_invGrid.IsAreaWithinGrid(placementPositions))
                    {
                        //Looks like our placement is valid here.
                        //Make our hoveredIndexes equal to our expected placement position
                        foreach ((int, int) index in placementPositions)
                            _hoveredIndexes.Add(index);

                        //Don't change anything if the hover data didn't change
                        if (!DidHoverDataChange())
                            return;

                        //clear and rerender the updated hover tiles
                        ClearHoverTiles();
                        RenderHoverTiles();
                        string currencyUnit = ItemCreatorHelper.GetEconomySetting().GetCurrencyUnit();

                        string itemValue;
                        //ensure no value is displayed if the item has no value
                        if (_heldItem.ItemData().ItemValue() <= 0)
                        {
                            itemValue = "---";
                            currencyUnit = "";
                        }
                        else itemValue = _heldItem.ItemData().ItemValue().ToString();


                        if (!_invGrid.IsMerchant())
                        {
                            
                            RenderItemInfo(_heldItem.ItemData().Name(), _heldItem.ItemData().Desc(), itemValue , "Value:", currencyUnit);
                        }
                        else
                        {
                            int merchantOffer = ItemData.CalculatePrice(_heldItem.ItemData(), 1, _invGrid.GetSellingPriceMultiplier());
                            string offerString;

                            if (merchantOffer <= 0)
                                offerString = "---";
                            else offerString = merchantOffer.ToString();
                            RenderItemInfo(_heldItem.ItemData().Name(), _heldItem.ItemData().Desc(), offerString, "Offer:", currencyUnit);
                        }
                    }

                    //just clear the hover tiles, if any exist
                    else
                        ClearHoverTiles();
                }

                //highlight the hovered item if no item is held
                else if (hoveredItem != null)
                {
                    //Get the item's occupancy as the new frame's hovered data
                    string indexesString = "";
                    foreach ((int, int) index in _invGrid.GetStackArea(_hoveredCellIndex))
                    {
                        _hoveredIndexes.Add(index);
                        indexesString += index.ToString() + "\n";
                    }
                    //Debug.Log("Cell Indexes:\n"+indexesString);


                    //Don't change anything if the hover data didn't change
                    if (!DidHoverDataChange())
                        return;

                    //clear and rerender the updated hover tiles
                    ClearHoverTiles();
                    RenderHoverTiles();
                    string currencyUnit = ItemCreatorHelper.GetEconomySetting().GetCurrencyUnit();

                    string itemValue;
                    //ensure no value is displayed if the item has no value
                    if (hoveredItem.ItemData().ItemValue() <= 0)
                    {
                        itemValue = "---";
                        currencyUnit = "";
                    }
                    else itemValue = hoveredItem.ItemData().ItemValue().ToString();


                    if (!_invGrid.IsMerchant())
                    {
                        RenderItemInfo(hoveredItem.ItemData().Name(), hoveredItem.ItemData().Desc(), itemValue, "Value:",currencyUnit);
                    }
                    else
                    {
                        int merchantPrice = ItemData.CalculatePrice(hoveredItem.ItemData(), 1, _invGrid.GetBuyingPriceMultiplier());
                        string priceString;
                        if (merchantPrice <= 0)
                            priceString = "---";
                        else priceString = merchantPrice.ToString();
                        RenderItemInfo(hoveredItem.ItemData().Name(), hoveredItem.ItemData().Desc(), priceString, "Price:", currencyUnit);
                    }
                }

                //highlight the cell position only. 
                else if (_heldItem == null && hoveredItem == null)
                {
                    //clear the previous hover tiles
                    ClearHoverTiles();
                }
            }
        }
        private void ClearHoverTiles()
        {
            if (_hoverTileGraphics.Count > 0)
            {
                for (int i = _hoverTileGraphics.Count - 1; i >= 0; i--)
                {
                    GameObject currentHoverGraphic = _hoverTileGraphics[i];

                    if (currentHoverGraphic == null)
                    {
                        //remove the graphic from the active list
                        _hoverTileGraphics.Remove(currentHoverGraphic);
                        continue;

                    }

                    //hide the graphic
                    else
                    {
                        currentHoverGraphic.SetActive(false);

                        //remove the graphic from the active list
                        _hoverTileGraphics.Remove(currentHoverGraphic);

                        //add the graphic to the inactive list
                        _unusedHoverTileGraphics.Add(currentHoverGraphic);

                        //recollect our tiles
                        currentHoverGraphic.transform.SetParent(this.transform);

                    }
                        
                }
            }
        }
        private bool DidHoverDataChange()
        {
            //check if all indexes from last frame are present in the current frame's detection
            foreach ((int, int) index in _lastFramesHoveredIndexes)
            {
                if (!_hoveredIndexes.Contains(index))
                    return false;
            }

            //check if there are any additional indexes this frame
            if (_lastFramesHoveredIndexes.Count == _hoveredIndexes.Count)
                return true;

            return false;

        }
        private void RenderHoverTiles()
        {
            if (_invGrid == null)
                return;

            else if (_hoveredIndexes == null)
                return;
            else if (_hoveredIndexes.Count == 0)
                return;

            if (!_invGrid.IsAreaWithinGrid(_hoveredIndexes))
                return;

            /*
            for (int i = _unusedHoverTileGraphics.Count-1; i > -1; i--)
            {
                if (_unusedHoverTileGraphics[i] == null)
                    _unusedHoverTileGraphics.Remove(_unusedHoverTileGraphics[i]);
            }*/

            foreach ((int, int) index in _hoveredIndexes)
            {
                //create a new hover tile if we're out of tiles
                if (_unusedHoverTileGraphics.Count == 0)
                {
                    GameObject newHoverGraphic = Instantiate(_hoverGraphicPrefab, this.transform);
                    newHoverGraphic.SetActive(false);
                    _unusedHoverTileGraphics.Add(newHoverGraphic);
                }

                //pick a new graphic from the unused graphics
                GameObject hoverGraphic = _unusedHoverTileGraphics[_unusedHoverTileGraphics.Count - 1];
                _unusedHoverTileGraphics.Remove(hoverGraphic);
                _hoverTileGraphics.Add(hoverGraphic);
                hoverGraphic.SetActive(true);

                //reposition the graphic onto the current cell position
                hoverGraphic.transform.SetParent(_invGrid.GetCellObject(index).transform, false);

                hoverGraphic.transform.localScale = Vector3.one;
                hoverGraphic.transform.position = new Vector3(hoverGraphic.transform.position.x, hoverGraphic.transform.position.y, 1);
            }
        }
        private void RenderDirectionalPointer()
        {
            if (_invGrid != null && _pointerHoverTileObject != null)
            {
                if (!_invGrid.IsCellOnGrid(_hoveredCellIndex))
                {
                    ClearDirectionalPointer();
                    return;
                }

                //make the graphic visible
                _pointerHoverTileObject.SetActive(true);

                //set the graphic to the hovered cell in question
                _invGrid.OverlayObjectOntoGridVisually(_hoveredCellIndex, _pointerHoverTileRectTransform);

                //ensure the graphic is of appropriate size
                _pointerHoverTileRectTransform.localScale = Vector3.one;
                _pointerHoverTileRectTransform.sizeDelta = new Vector2(_invGrid.CellSize().x, _invGrid.CellSize().y);

                //ensure the pointer is drawn over everything else
                _pointerHoverTileObject.transform.SetAsLastSibling();

                /* Doesnt work due to not being draw over the item sprites
                _pointerHoverTileObject.transform.SetParent(_invGrid.GetCellObject(_hoveredCellIndex).transform, false);

                _pointerHoverTileObject.transform.localScale = Vector3.one;
                _pointerHoverTileObject.transform.position = new Vector3(_pointerHoverTileObject.transform.position.x, _pointerHoverTileObject.transform.position.y, 2);
                */
            }
        }
        private void ClearDirectionalPointer()
        {
            if (_pointerContainer != null && _pointerHoverTileObject != null)
            {
                _pointerHoverTileObject.SetActive(false);
                _pointerHoverTileObject.transform.SetParent(_pointerContainer.transform);
                _pointerContainer.transform.position = Vector3.zero;
            }
        }
        private void RenderItemInfo(string newItemName, string newDesc, string newValue, string newLabel,string currencyUnit)
        {
            if (_invGrid != null)
            {
                _invGrid.GetParentWindow().SetItemName(newItemName);
                _invGrid.GetParentWindow().SetItemDescription(newDesc);
                _invGrid.GetParentWindow().SetItemValue(newValue + currencyUnit);
                _invGrid.GetParentWindow().SetItemValueLabel(newLabel);
            }

        }
        private void UpdateHeldStackText()
        {
            //update the heldStackText Ui
            _heldStackText.text = $"{_heldItemStackCount}";

            if (_heldItemStackCount > 1)
                _heldStackContainer.gameObject.SetActive(true);
            else _heldStackContainer.gameObject.SetActive(false);

            //ensure the text is visible over the item graphic
            _heldStackContainer.GetComponent<RectTransform>().SetAsLastSibling();
        }
        private void BindPointerContainerToMousePosition()
        {
            if (_pointerContainer != null)
            {
                
                if (_pointerRectTransform.parent != _defaultParentOfPointerContainer)
                    _pointerRectTransform.SetParent(_defaultParentOfPointerContainer,false);

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _uiCanvas.GetComponent<RectTransform>(),
                    _mousePosition,
                    null,
                    out _localPoint))
                {
                    _pointerRectTransform.anchoredPosition = _localPoint;
                }
            }
        }
        private void BindPointerContainerToCellPosition(InvGrid grid)
        {
            if (_pointerContainer != null && grid != null)
            {
                grid.OverlayObjectOntoGridVisually(_lastKnownHoveredIndex,_pointerContainer.GetComponent<RectTransform>(), false);
            }
        }
        private void BindHeldItemToPointerContainer()
        {
            BindHeldItemToPointerContainer(_invGrid);
        }
        private void BindHeldItemToPointerContainer(InvGrid specificGrid)
        {
            if (_heldItem != null)
            {
                //parent the item to the pointer
                _heldItem.GetComponent<RectTransform>().SetParent(_pointerContainer.transform, false);

                //reset the item's local position to zero
                RectTransform itemRectTransform = _heldItem.GetComponent<RectTransform>();
                itemRectTransform.localPosition = Vector3.zero;

                //ensure the sprite is of the appropriate size
                itemRectTransform.sizeDelta = new Vector2(_heldItem.Width() * specificGrid.CellSize().x, _heldItem.Height() * specificGrid.CellSize().y);

                itemRectTransform.gameObject.SetActive(true);
            }

        }
        private void SetPointerToHomeGrid()
        {
            if (_homeInventoryGrid == null)
                return;

            //only continue if our home grid window is actually opened
            if (_homeInventoryGrid.GetParentWindow().IsWindowOpen())
            {
                //ignore this call if no width OR HEIGHT dimension exists
                if (_homeInventoryGrid.ContainerSize().x <= 0 || _homeInventoryGrid.ContainerSize().y <= 0)
                    return;

                //Focus on our home (inv)windows
                FocusOnWindowForDirectionalInput(_homeInventoryGrid.GetParentWindow());
            }

            //if our home grid is closed, but any other grid is opened, just go to a valid grid instead
            else if (!_homeInventoryGrid.GetParentWindow().IsWindowOpen() && _openedInvWindows.Count > 0)
            {
                for (int i = 0; i < _openedInvWindows.Count; i++)
                {
                    //focus on the first valid opened window we find
                    if (_openedInvWindows[i].GetItemGrid().ContainerSize().x > 0 && _openedInvWindows[i].GetItemGrid().ContainerSize().y > 0)
                    {
                        FocusOnWindowForDirectionalInput(_openedInvWindows[i]);
                        return;
                    }
                }
            }
        }
        private void ReturnPointerContainerToHomeTransform()
        {
            if (_pointerContainer != null && _homePointerContainer != null)
            {
                _pointerContainer.GetComponent<RectTransform>().SetParent(_homePointerContainer);
                //_homePointerContainer.gameObject.SetActive(false);
            }
        }
        private void ShowContainerRelatedUiControls()
        {
            foreach (RectTransform rt in _containerRelatedUiControls)
                rt.gameObject.SetActive(true);
        }
        private void HideContainerRelatedUicontrols()
        {
            foreach (RectTransform rt in _containerRelatedUiControls)
                rt.gameObject.SetActive(false);
        }


        //inventory manipulation actions
        private void PickupHalfOfHoveredStack()
        {
            //Only pickup half if we aren't already holding an item (assuming an item is on the hovered cell position)
            if (_heldItem == null && _invGrid.IsCellOccupied(_hoveredCellIndex))
            {
                ItemData detectedItemData = _invGrid.GetStackItemData(_hoveredCellIndex);


                int stackAmount = _invGrid.GetStackValue(_hoveredCellIndex);

                //if the stack is only 1 item, just pick it up
                if (stackAmount == 1)
                {
                    //update the held item utils
                    _heldItemStackCount = stackAmount;
                    _heldItem = _invGrid.GetItemGraphicOnCell(_hoveredCellIndex);

                    //remove the stack from the inventory
                    _invGrid.RemoveItem(_hoveredCellIndex,1);
                    //Debug.LogWarning($"Interacter: is Grid Empty: {_invGrid.IsEmpty()}\nRemaining stacks in grid: {_invGrid.GetAllStacks().Count}");

                    //update the 'holding item' feedback utils
                    BindHeldItemToPointerContainer();
                    UpdateHeldStackText();
                    PlayItemPickupAudio();
                    return;
                }

                //else pick up half the stack (or half + 1 if the stackAmount is odd)
                //make sure to create a new sprite for the new itemStack
                else
                {
                    //pick over half if the amount is uneven
                    if (stackAmount % 2 == 1)
                        _heldItemStackCount = (stackAmount / 2) + 1;
                    else
                        _heldItemStackCount = stackAmount / 2;

                    //create a new sprite for the held item
                    InvItem pickedUpItem = ItemCreatorHelper.CreateItem(detectedItemData, _invGrid.CellSize().x, _invGrid.CellSize().y).GetComponent<InvItem>();
                    _heldItem = pickedUpItem;

                    //remove the heldAmount from the hovered stack
                    _invGrid.RemoveItem(_hoveredCellIndex, _heldItemStackCount);

                    //update the 'holding item' feedback utils
                    BindHeldItemToPointerContainer();
                    UpdateHeldStackText();
                    PlayItemPickupAudio();
                    return;
                }


            }
        }
        private void DropSingleItemOntoHoveredPosition()
        {
            if (_heldItem != null)
            {
                HashSet<(int, int)> placementArea = _invGrid.ConvertSpacialDefIntoGridArea(_hoveredCellIndex, _heldItem.GetSpacialDefinition(), _heldItem.ItemHandle());

                //make sure the entire item area is within the grid
                if (_invGrid.IsAreaWithinGrid(placementArea))
                {
                    int itemCount = _invGrid.CountUniqueStacksInArea(placementArea);

                    //if position is completely empty, place 1 item from the stack here
                    if (itemCount == 0)
                    {
                        //drop the held item if we're only holding a stack of 1
                        if (_heldItemStackCount == 1)
                        {
                            PlayItemDropAudio();
                            //Debug.Log($"Given placement position: ({_hoveredCellIndex.Item1},{_hoveredCellIndex.Item2})");
                            _invGrid.AddItem(_heldItem.ItemData(), 1, _hoveredCellIndex, _heldItem.Rotation());
                            ItemCreatorHelper.ReturnItemToCreator(_heldItem);
                            _heldItem = null;
                            _heldItemStackCount = 0;
                            UpdateHeldStackText();
                            return;
                        }

                        else
                        {
                            //Debug.Log($"Given placement position: ({_hoveredCellIndex.Item1},{_hoveredCellIndex.Item2})");

                            //add the new item to the hovered position
                            _invGrid.AddItem(_heldItem.ItemData(),1,_hoveredCellIndex, _heldItem.Rotation());

                            //update our held count 
                            _heldItemStackCount -= 1;
                            UpdateHeldStackText();
                            PlayItemDropAudio();
                            return;
                        }

                    }

                    //if only one stack found, top off the stack if compatible and available
                    else if (itemCount == 1)
                    {
                        foreach ((int, int) index in placementArea)
                        {
                            //find the first cell that our detected stack is occupying
                            if (_invGrid.IsCellOccupied(index))
                            {
                                //if the stack is compatible and not yet full, top it off
                                if (_invGrid.GetStackItemData(index).ItemCode() == _heldItem.ItemData().ItemCode() && _invGrid.GetStackValue(index) < _heldItem.ItemData().StackLimit())
                                {
                                    //increase the compatible stack by 1
                                    _invGrid.AddItem(_heldItem.ItemData(),1,index, _heldItem.Rotation());

                                    PlayItemDropAudio();

                                    //clear the held item settings if we're only holding a stack of 1
                                    if (_heldItemStackCount == 1)
                                    {
                                        //clear our held item utils
                                        ItemCreatorHelper.ReturnItemToCreator(_heldItem);
                                        _heldItem = null;
                                        _heldItemStackCount = 0;
                                    }

                                    //otherwise just decrement our held stack value
                                    else
                                        _heldItemStackCount--;

                                    UpdateHeldStackText();
                                    return;

                                }

                                //else, the found stack is either Not compatible, or its full. Ignore this case
                                else return;
                            }
                        }

                        //if we've made it here, there's been a lookup error.
                        //We know an item exists where we've looked, but we've failed to find it.
                        //throw a red error
                        Debug.LogError($"Error during rClick item placement via InvManager: couldn't find the inventory stack that should definitely exist." +
                            $" There's probably an error with itemStack lookup or item stack creation within the InvGrid, OR the InvManager isn't" +
                            $" looking where it should be (mixed up indexes? Wrong parameter?).");

                    }
                }
            }
        }
        private void PickupStackOnHoveredPosition()
        {
            //if we're not holding an item, pickup the full stack of items
            if (_heldItem == null)
            {
                //Debug.Log("Pickup stack Called");
                //save the item reference
                _heldItem = _invGrid.GetItemGraphicOnCell(_hoveredCellIndex);

                //save the amount of the item held
                _heldItemStackCount = _invGrid.GetStackValue(_hoveredCellIndex);

                //remove the item from the invGrid
                _invGrid.RemoveItem(_hoveredCellIndex,_heldItemStackCount);
                //Debug.LogWarning($"Interacter: is Grid Empty via 'InvGrid.IsEmpty()': {_invGrid.IsEmpty()}\nCounted stacks in grid: {_invGrid.GetAllStacks().Count}");

                //update the "held item" feedback utils
                BindHeldItemToPointerContainer();
                UpdateHeldStackText();

                PlayItemPickupAudio();
                return;

            }
        }
        private void DropStackOnHoveredPosition()
        {
            if (_heldItem != null)
            {
                HashSet<(int, int)> placementArea = _invGrid.ConvertSpacialDefIntoGridArea(_hoveredCellIndex, _heldItem.GetSpacialDefinition(), _heldItem.ItemHandle());
                //Debug.Log($"Drop Stack Called. Placement area: {_invGrid.StringifyPositions(placementArea)}");
                //make sure the entire item area is within the grid
                if (_invGrid.IsAreaWithinGrid(placementArea))
                {
                    int itemCount = _invGrid.CountUniqueStacksInArea(placementArea);

                    //if position is completely empty, place here
                    if (itemCount == 0)
                    {
                        PlayItemDropAudio();
                        _invGrid.AddItem(_heldItem.ItemData(),_heldItemStackCount, _hoveredCellIndex, _heldItem.Rotation());
                        ItemCreatorHelper.ReturnItemToCreator(_heldItem);
                        _heldItem = null;
                        _heldItemStackCount = 0;

                        UpdateHeldStackText();
                        
                        return;
                    }

                    //if only one stack found, top off the stack if compatible and available, or swap stacks otherwise
                    else if (itemCount == 1)
                    {
                        foreach ((int, int) index in placementArea)
                        {
                            //find the first cell that our detected stack is occupying
                            if (_invGrid.IsCellOccupied(index))
                            {
                                //if the stack is compatible and not yet full, top it off
                                if (_invGrid.GetStackItemData(index).ItemCode() == _heldItem.ItemData().ItemCode() && _invGrid.GetStackValue(index) < _heldItem.ItemData().StackLimit())
                                {
                                    int stackValue = _invGrid.GetStackValue(index);
                                    int stackMaxCapacity = _invGrid.GetStackItemData(index).StackLimit();
                                    int openCapacity = stackMaxCapacity - stackValue; //openCapacity will always be above zero if we've made it this far

                                    //place all held items here if the stack can take it
                                    if (_heldItemStackCount <= openCapacity)
                                    {
                                        PlayItemDropAudio();
                                        _invGrid.AddItem(_heldItem.ItemData(), _heldItemStackCount, index, _heldItem.Rotation());
                                        ItemCreatorHelper.ReturnItemToCreator(_heldItem);
                                        _heldItem = null;
                                        _heldItemStackCount = 0;

                                        UpdateHeldStackText();

                                        return;
                                    }

                                    //otherwise, only place enough items to fill the stack here. Don't clear the held item yet, since we have some left.
                                    else
                                    {
                                        PlayItemDropAudio();
                                        _invGrid.AddItem(_heldItem.ItemData(), openCapacity, index, _heldItem.Rotation());
                                        _heldItemStackCount -= openCapacity;

                                        UpdateHeldStackText();

                                        return;
                                    }

                                }


                                //otherwise, swap the stacks
                                else
                                {
                                    PlayItemDropAudio();

                                    //save the found item's data
                                    InvItem newGraphic = _invGrid.GetItemGraphicOnCell(index);
                                    int stackSize = _invGrid.GetStackValue(index);

                                    //delete the currently-stored item
                                    _invGrid.RemoveItem(index,stackSize);

                                    //place the held item into the now-fully-open position
                                    _invGrid.AddItem(_heldItem.ItemData(), _heldItemStackCount, _hoveredCellIndex, _heldItem.Rotation());

                                    //return the old item to the creator, since we'll be holding the new item next
                                    ItemCreatorHelper.ReturnItemToCreator(_heldItem);

                                    //update our held itemData
                                    _heldItem = newGraphic;
                                    _heldItemStackCount = stackSize;

                                    BindHeldItemToPointerContainer(_invGrid);
                                    UpdateHeldStackText();

                                    return;
                                }
                            }
                        }

                        /* If here was reached, then no stacks were found.
                         * This shouldn't ever happen, since we SUPPOSEDLY found exactly
                         * 1 preexisting stack before we entered this block. 
                         * Raise a red error. Something's wrong with our stack lookUp/creation
                         */
                        Debug.LogError($"Error during itemSwaping via InvManager: couldn't find the inventory stack that should definitely exist." +
                            $" There's probably an error with itemStack lookup or item stack creation within the InvGrid, OR the InvManager isn't" +
                            $" looking where it should be (mixed up indexes? Wrong parameter?).");

                    }
                }
            }
        }
        private void TakeStackOnHoveredPosition()
        {
            if (_heldItem == null && _invGrid != _homeInventoryGrid)
            {
                TransferItems(_invGrid, _homeInventoryGrid, _hoveredCellIndex, _invGrid.GetStackValue(_hoveredCellIndex));
            }
        }
        private void TransferStackOnHoveredPosition()
        {
            if (_heldItem ==null && _invGrid != null)
            {
                if (_openedInvWindows.Count == 2)
                {
                    //allow quick transfer if no merchant inventories are open
                    if (_openedMerchants.Count == 0)
                    {

                        InvGrid receiver;

                        //make sure the OTHER inventory is on the receiving end of the transfer
                        if (_openedInvWindows[0].GetItemGrid() != _invGrid)
                            receiver = _openedInvWindows[0].GetItemGrid();
                        else receiver = _openedInvWindows[1].GetItemGrid();

                        TransferItems(_invGrid, receiver, _hoveredCellIndex, _invGrid.GetStackValue(_hoveredCellIndex));
                    }

                    //QuickSell if the OTHER window is the merchant
                    //[assuming the items in question are sellable]
                    else if (_openedMerchants.Count == 1 && !_invGrid.IsMerchant())
                    {
                        ItemData hoveredItemData = _invGrid.GetStackItemData(_hoveredCellIndex);
                        if (hoveredItemData == null)
                            return;
                        if (hoveredItemData.IsSellable())
                        {
                            //save the item's data before we remove it from the grid, in case the transaction fails
                            InvItem itemBeingSold = _invGrid.GetItemGraphicOnCell(_hoveredCellIndex);
                            ItemRotation lastRotation = itemBeingSold.Rotation();
                            int itemStackSize = _invGrid.GetStackValue(_hoveredCellIndex);

                            //remove the stack, and suppress the removal event
                            _invGrid.RemoveItem(_hoveredCellIndex, itemStackSize,true); 

                            if (!SellItemToMerchant(hoveredItemData, itemStackSize, _openedMerchants[0].GetItemGrid(), _homeInventoryGrid))
                            {
                                //add the item back to the grid if the sale failed
                                _invGrid.AddItem(hoveredItemData,itemStackSize, _hoveredCellIndex, lastRotation, true);
                            }
                            
                        }
                    }
                }
                
            }
        }
        private void TransferItems(InvGrid donor, InvGrid receiver, (int,int) cellPosition,int amount)
        {
            if (receiver == null || donor == null)
                return;

            if (!donor.IsCellOnGrid(cellPosition))
                return;

            if (!donor.IsCellOccupied(cellPosition))
                return;

            ItemData item = donor.GetStackItemData(cellPosition);

            int amountTaken = 0;

            while (amountTaken < amount)
            {
                //keep taking items if we BOTH 1) have space for it and 2) more items remain at the specified position
                if (receiver.DoesSpaceExist(item, 1, null) && donor.GetStackValue(cellPosition) >= 1)
                {
                    receiver.AddItem(item, 1);
                    donor.RemoveItem(cellPosition, 1);
                    amountTaken++;
                }

                //otherwise, we ran out of space (or items to take). End the operation
                else return;
            }
        }
        private void ShowContextMenuOnHoveredItem()
        {
            //only show if we're hovering over an item while not holding anything
            if (_invGrid.IsCellOccupied(_hoveredCellIndex) && _heldItem == null)
            {
                //save the clicked index
                _contextualItemPosition = _hoveredCellIndex;
                _contextualInvGrid = _invGrid;

                //calculate the window's position (should be wherever our pointer container is)
                Vector3 pointerPositionRelativeToCanvas = _uiCanvas.transform.InverseTransformPoint(_pointerRectTransform.position);

                //Infer container-based context options
                HashSet<ContextOption> containerContextOptions =  InferContainerSpecificContextOptions();

                //Ignore the item's context options if the inventory belongs to a merchant
                //(You shouldn't be reorganizing & consuming the merchant's possessions!)
                if (_contextualInvGrid.IsMerchant())
                {
                    //Debug.Log("Merchant Detected. Ignoring Item's base context options");
                }

                //merge the item-in-question's context options with the container's
                else
                {
                    containerContextOptions.UnionWith(_invGrid.GetStackItemData(_hoveredCellIndex).ContextualOptions());
                }

                //also remove the sell option from showing up if no merchant's inventory is opened
                if (_openedMerchants.Count <= 0)
                    containerContextOptions.Remove(ContextOption.SellItem);
                

                int hoveredStackSize = _invGrid.GetStackValue(_hoveredCellIndex);

                //open the context window
                ContextWindowHelper.ShowContextWindow(pointerPositionRelativeToCanvas, _invGrid.GetParentWindow() ,containerContextOptions,_invGrid.GetStackItemData(_hoveredCellIndex),1,hoveredStackSize);

                PlaySelectionAudio();
                return;
            }
        }
        private void HideContextMenu()
        {
            if (ContextWindowHelper.IsContextWindowShowing())
            {
                ContextWindowHelper.HideContextWindow();
                PlayDeselectionAudio();
            }
        }
        private HashSet<ContextOption> InferContainerSpecificContextOptions()
        {
            
            HashSet<ContextOption> contextOptions = new HashSet<ContextOption>();

            //regardless of what's open, only show the buy command if the current grid is a merchant grid
            if (_invGrid.IsMerchant())
            {
                contextOptions.Add(ContextOption.BuyItem);
            }

            //is only one container open (the current one) [and it isn't our home container]
            else if (_openedInvWindows.Count == 1 && _invGrid != _homeInventoryGrid)
            {
                contextOptions.Add(ContextOption.TakeItem);
            }

            //otherwise (if many containers are open), Take a closer look at our current situation...
            else if (_openedInvWindows.Count > 1)
            {
                //if any of the other grids are a merchant's grid, show Sell (assuming selling is enabled in this inventory)
                if (_openedMerchants.Count() > 0 && _invGrid.CanSellFromThisInventory())
                    contextOptions.Add(ContextOption.SellItem);

                //if only 2 containers are open,
                //then ONLY show either take OR transfer. NOT BOTH
                if (_openedInvWindows.Count == 2)
                {
                    //if neither are merchant inv's, then show take or transfer, depending on the context
                    if (_openedMerchants.Count() == 0)
                    {
                        //Take if we aren't in our home inventory
                        if (_invGrid != _homeInventoryGrid)
                            contextOptions.Add(ContextOption.TakeItem);

                        //Transfer if we're in our home inv
                        else
                            contextOptions.Add(ContextOption.TransferItem);
                    }

                    //otherwise, the other inv is definitely a merchant.
                    //It's ok to show the Take option if this inv isn't a home inv
                    else
                    {
                        if (_invGrid != _homeInventoryGrid)
                            contextOptions.Add(ContextOption.TakeItem);
                    }
                    

                }

                //otherwise, many containers are open indeed
                //in this case we need to specify when we should show either or both contexts
                else
                {
                    //only show the Take option if we ARE NOT in our home inventory [already assuming its not a merchant]
                    if (_invGrid != _homeInventoryGrid)
                        contextOptions.Add(ContextOption.TakeItem);


                    //Transfer should only show up on the following cases
                    //1) we're in a home Ui and there exists ANOTHER nonMerchant ui [only nonMerhcants are tranfer targets]
                    //2) we're NOT in a home ui and there exists AT LEAST 2 OTHER NonMerhcant Uis
                    // ----> [home shouldn't be a merchant, but Take already resolves that case. Another additional nonMerchant must be also be open]
                    int nonMerchants = GetOpenedNonMerchantContainers().Count();
                    if (_invGrid == _homeInventoryGrid && nonMerchants > 1) // ensure home[current] + another nonMerchant[transferTarget] is opened 
                    {
                        contextOptions.Add(ContextOption.TransferItem);
                    }
                    else if (_invGrid != _homeInventoryGrid && nonMerchants > 2)// ensure nonMerchant[current] + home[Take] + another nonMerchant[TranferTarget] is open
                    {
                        contextOptions.Add(ContextOption.TransferItem);
                    }
                    
                }
            }

            return contextOptions;
        }

        /// <summary>
        /// Checks if the merchant and seller both have enough space for the sold items & payment items (respectively), 
        /// then adds the respecive items to both containers. 
        /// THIS METHOD DOES NOT CHECK IF THE SELLER POSSESSES THE ITEMS BEING SOLD, NOR DOES THIS METHOD REMOVE THE SOLD ITEMS FROM THE SELLER. 
        /// The items in question should be removed from the seller and held off grid before calling this method 
        /// (to free up space in the container for the payment), and then deleted if the transaction is successful. 
        /// Otherwise the items should be returned to the seller upon sell failure. Ensure the add/remove item events are suppressed during this middle-interaction.
        /// Upon success, this method does not suppress neither event of adding the sold item to the merchant, nor adding the payment items to the payment receiver.
        /// </summary>
        /// <param name="item">the item being sold</param>
        /// <param name="amount">how many is being sold</param>
        /// <param name="merchant">the receiver of the items. Must be a merchant</param>
        /// <param name="paymentReceiver">what container will receive payment for the items</param>
        /// <returns>True if the transaction is successful. False otherwise.</returns>
        private bool SellItemToMerchant(ItemData item, int amount, InvGrid merchant, InvGrid paymentReceiver)
        {
            //build the query to check if the merchant has space in their inv
            List<InvGrid.ItemQuery> querys = new List<InvGrid.ItemQuery>();
            InvGrid.ItemQuery itemTask = new InvGrid.ItemQuery(item,amount);
            querys.Add(itemTask);

            //check if the merchant has space
            if (merchant.FindSpaceForItems(querys) == null)
            {
                Debug.Log($"Merchant doesn't have space in their inv to buy {amount} '{item.name}'(s) ");
                return false;
            }

            //check if the payment receiver has space for the currency
            int paymentQuantity = ItemData.CalculatePrice(item, amount, merchant.GetSellingPriceMultiplier());
            ItemData currencyItem = ItemCreatorHelper.GetEconomySetting().GetCurrencyItem();

            querys.Clear();
            itemTask = new InvGrid.ItemQuery(currencyItem, paymentQuantity);
            querys.Add(itemTask);

            if (paymentReceiver.FindSpaceForItems(querys) == null)
            {
                Debug.Log($"Container '{paymentReceiver.name}' doesn't have space in their inv for the payment of {paymentQuantity} '{currencyItem}'(s) ");
                return false;
            }

            //give the merchant the items (assume the merchant has enough of the currency)
            merchant.AddItem(item, amount);

            //give the seller the payment
            paymentReceiver.AddItem(currencyItem, paymentQuantity);

            //play the currency audio to give some 'sell' feedback
            PlayItemDropAudio(currencyItem);

            return true;

            //make sure to remove the sold item(s) from whatever container they belonged to BEFORE attempting this method. 
            //This method doesn't remove any items from any containers, due to it's general-case nature
            //for example: the sold items could be held by the invInteracter (and not in any grid)
        }

        //context-menu-related
        private bool IsContextualDataValid()
        {
            //check if our Inventory-Grid-in-context still exists
            if (_contextualInvGrid == null)
            {
                Debug.LogWarning($"Attempted to organize an item at position ({_contextualItemPosition.Item1},{_contextualItemPosition.Item2}) within a Missing ItemGrid Reference. " +
                    "Somehow an inventory got deleted while the context menu was still showing for that invGrid. Ignoring organize command");
                return false;
            }

            //check if the cell still exists on the grid
            if (!_contextualInvGrid.IsCellOnGrid(_contextualItemPosition))
            {
                Debug.LogWarning($"Attempted to organize an item at position ({_contextualItemPosition.Item1},{_contextualItemPosition.Item2}), but the position no longer exists. " +
                    "Somehow an inventory got deleted while the context menu was still showing for that invGrid. Ignoring organize command");
                return false;
            }

            ItemData contextualItem = _contextualInvGrid.GetStackItemData(_contextualItemPosition);
            if (contextualItem == null)
            {
                Debug.LogWarning($"Attempted to get an itemData at position ({_contextualItemPosition.Item1},{_contextualItemPosition.Item2}), which is empty. " +
                    "The item stack must've moved unexpectedly, since the context wouldn't have appeared for a empty position");
                return false;
            }

            return true;
        }
        private void ListenForValidContextualOption(ContextOption selectedOption, int amount)
        {
            //only respond to the contextual menu if we have an active item selected
            if (_contextualItemPosition != (-1, -1))
            {
                switch (selectedOption)
                {
                    case ContextOption.OrganizeItem:
                        RespondToOrganize(amount);
                        SetInternalInventoryInteractionLock(false);

                        //Do this in case the input button [for UiButton events]
                        //is the same input as what's bound to the confirm response
                        //This fixes "Double-Reading" the Specified input
                        IgnoreOtherConfirmCommandsUntilEndOfFrame();
                        return;

                    case ContextOption.UseItem:
                        RespondToUse(amount);
                        SetInternalInventoryInteractionLock(false);
                        IgnoreOtherConfirmCommandsUntilEndOfFrame();
                        return;

                    case ContextOption.DiscardItem:
                        RespondToDiscard(amount);
                        SetInternalInventoryInteractionLock(false);
                        IgnoreOtherConfirmCommandsUntilEndOfFrame();
                        return;


                    case ContextOption.TakeItem:
                        RespondToTake(amount);
                        SetInternalInventoryInteractionLock(false);
                        IgnoreOtherConfirmCommandsUntilEndOfFrame();
                        return;

                    case ContextOption.TransferItem:
                        RespondToTranfer(amount);
                        SetInternalInventoryInteractionLock(false);
                        IgnoreOtherConfirmCommandsUntilEndOfFrame();
                        return;

                    case ContextOption.BuyItem:
                        RespondToBuy(amount);
                        SetInternalInventoryInteractionLock(false);
                        IgnoreOtherConfirmCommandsUntilEndOfFrame();
                        return;

                    case ContextOption.SellItem:
                        RespondToSell(amount);
                        SetInternalInventoryInteractionLock(false);
                        IgnoreOtherConfirmCommandsUntilEndOfFrame();
                        return;

                    default:
                        return;

                }
            }

        }
        
        

        //menu Option Responses
        private void RespondToOrganize(int amount)
        {
            //exit if the contextual data expired somehow
            if (!IsContextualDataValid())
            {
                Debug.LogWarning("Organize Response INVALID");
                _contextualInvGrid = null;
                _contextualItemPosition = (-1, -1);
                return;
            }

            //Debug.Log($"Organize Response Valid on position {_contextualItemPosition} [hovered Cell: {_hoveredCell}]. Organize Clicked.");

            //pickup all items if our amount is the entire stack
            if (amount == _contextualInvGrid.GetStackValue(_contextualItemPosition))
            {
                //save the item reference
                _heldItem = _contextualInvGrid.GetItemGraphicOnCell(_contextualItemPosition);

                //save the amount of the item held
                _heldItemStackCount = _contextualInvGrid.GetStackValue(_contextualItemPosition);

                //remove the item from the invGrid
                _contextualInvGrid.RemoveItem(_contextualItemPosition,amount);
            }
            else
            {
                //create a new sprite to hold [it's the same item as what's on the grid]
                _heldItem = ItemCreatorHelper.CreateItem(_contextualInvGrid.GetStackItemData(_contextualItemPosition), _contextualInvGrid.CellSize().x, _contextualInvGrid.CellSize().y).GetComponent<InvItem>();

                //save the amount of the held item
                _heldItemStackCount = amount;

                //remove the specified amount from the grid
                _contextualInvGrid.RemoveItem(_contextualItemPosition, amount);
            }

            BindHeldItemToPointerContainer(_contextualInvGrid);
            UpdateHeldStackText();

            PlayItemPickupAudio();

            //reset the selected position
            _contextualItemPosition = (-1, -1);

        }
        private void RespondToDiscard(int amount)
        {
            //exit if the contextual data expired somehow
            if (!IsContextualDataValid())
                return;

            _contextualInvGrid.RemoveItem(_contextualItemPosition, amount);
            PlayDiscardAudio();
        }
        private void RespondToUse(int amount)
        {
            //exit if the contextual data expired somehow
            if (!IsContextualDataValid())
                return;


            ItemData contextualItem = _contextualInvGrid.GetStackItemData(_contextualItemPosition);

            //use the item
            Debug.Log($"Used {contextualItem.name}!");

            //remove the items from inventory
            _contextualInvGrid.RemoveItem(_contextualItemPosition, amount);

            PlayUseAudio();

        }
        private void RespondToTake(int amount)
        {
            //exit if the contextual data expired somehow
            if (!IsContextualDataValid())
                return;

            TransferItems(_contextualInvGrid,_homeInventoryGrid,_contextualItemPosition,amount);

            PlayItemDropAudio(_contextualInvGrid.GetStackItemData(_contextualItemPosition));
        }
        private void RespondToTranfer(int amount)
        {
            if (!IsContextualDataValid())
                return;

            TransferItems(_contextualInvGrid, _contextualGridReceiver, _contextualItemPosition, amount);
            PlayItemDropAudio(_contextualInvGrid.GetStackItemData(_contextualItemPosition));

        }
        private void RespondToBuy(int amount)
        {
            if (!IsContextualDataValid())
                return;

            //Debug.Log($"Attempting to buy {amount} {_contextualInvGrid.GetStackItemData(_contextualItemPosition).Name()}(s)...");

            //get the payment item
            ItemData currencyItemData = ItemCreatorHelper.GetEconomySetting().GetCurrencyItem();

            //get the item that's being bought
            ItemData buyingItem = _contextualInvGrid.GetStackItemData(_contextualItemPosition);

            //get the cost of the items in question
            int cost = ItemData.CalculatePrice(buyingItem, amount, _contextualInvGrid.GetBuyingPriceMultiplier());
            //Debug.Log($"What you should be paying: {cost}");

            //Does removing the requested items work? This operation fails if not enough are found
            if (!_homeInventoryGrid.RemoveItem(currencyItemData, cost))
            {
                int heldGold = _homeInventoryGrid.CountItem(currencyItemData);
                Debug.LogWarning($"Container {_homeInventoryGrid.name} has insufficient {currencyItemData.name}(s) [{heldGold}/{cost}]");
                return;
            }

            //Transfer bought items from merchant to home inventory
            TransferItems(_contextualInvGrid, _homeInventoryGrid, _contextualItemPosition, amount);

            //play item drop audio
            PlayItemDropAudio(_contextualInvGrid.GetStackItemData(_contextualItemPosition));


        }
        private void RespondToSell(int amount)
        {
            if (!IsContextualDataValid())
                return;

            //Debug.Log($"Attempting to sell {amount} {_contextualInvGrid.GetStackItemData(_contextualItemPosition).Name()}(s)...");

            ItemData soldItem = _contextualInvGrid.GetStackItemData(_contextualItemPosition);
            ItemRotation lastRotation = _contextualInvGrid.GetItemGraphicOnCell(_contextualItemPosition).Rotation();

            //remove the stated amount from the contextual grid (to open up space for currency)
            _contextualInvGrid.RemoveItem(_contextualItemPosition, amount,true); //[suppress the event, if the transaction fails, we'll return it like nothing happened]

            //if successful, this adds the sold item(s) to the merchant and adds currency to the homegrid.
            //if it fails, then one of the containers doesn't have enough space to accept the items(s)/currency
            if (!SellItemToMerchant(soldItem, amount, _contextualGridReceiver, _homeInventoryGrid))
            {
                //put the items back as if nothing happened
                _contextualInvGrid.AddItem(soldItem,amount,_contextualItemPosition,lastRotation,true);

                Debug.LogWarning($"Failed to sell {amount} {soldItem.name}(s) to merchant {_contextualGridReceiver.name}.");
                return;
            }

            //The "sell item" method plays the sell audio, adds the sold items to the merchant, and adds the currency to the payment receiver
            //we're done here!
        }

        


        //audio 
        private void PlayMovementAudio()
        {
            if(_audioSource != null && _movementAudio != null)
            {
                _audioSource.clip = _movementAudio;
                _audioSource.Play();
            }
        }
        private void PlaySelectionAudio()
        {
            if (_audioSource != null && _selectionAudio != null)
            {
                _audioSource.clip = _selectionAudio;
                _audioSource.Play();
            }
        }
        private void PlayDeselectionAudio()
        {
            if (_audioSource != null && _deselectionAudio != null)
            {
                _audioSource.clip = _deselectionAudio;
                _audioSource.Play();
            }
        }
        private void PlayDiscardAudio()
        {
            if (_audioSource != null && _discardAudio != null)
            {
                _audioSource.clip = _discardAudio;
                _audioSource.Play();
            }
        }
        private void PlayUseAudio()
        {
            if (_audioSource != null && _useAudio != null)
            {
                _audioSource.clip = _useAudio;
                _audioSource.Play();
            }
        }
        private void PlayRotateAudio()
        {
            if (_audioSource != null && _rotateAudio != null)
            {
                _audioSource.clip = _rotateAudio;
                _audioSource.Play();
            }
        }
        private void PlayItemPickupAudio()
        {
            if (_audioSource != null && _heldItem != null)
            {
                if (_heldItem.ItemData().OnPickupAudioClip() != null)
                {
                    _audioSource.clip = _heldItem.ItemData().OnPickupAudioClip();
                    _audioSource.Play();
                }
            }
        }
        private void PlayItemDropAudio()
        {
            if (_audioSource != null && _heldItem != null)
            {
                if (_heldItem.ItemData().OnDropAudioClip() != null)
                {
                    _audioSource.clip = _heldItem.ItemData().OnDropAudioClip();
                    _audioSource.Play();
                }
            }
        }
        private void PlayItemDropAudio(ItemData itemData)
        {
            if (_audioSource != null && itemData != null)
            {
                if (itemData.OnDropAudioClip()!= null)
                {
                    _audioSource.clip = itemData.OnDropAudioClip();
                    _audioSource.Play();
                }
            }
        }
        private void PlayContainerOpenedAudio()
        {
            if (_audioSource != null)
            {
                _audioSource.clip = _openedAudio;
                _audioSource.Play();
            }
        }
        private void PlayContainerClosedAudio()
        {
            if (_audioSource != null)
            {
                _audioSource.clip = _closedAudio;
                _audioSource.Play();
            }
        }


        //externals
        public void SetActiveItemGrid(InvGrid newGrid)
        {
            _invGrid = newGrid;

            //used if the pointer isn't on the grid, but we get directional input
            _lastKnownGrid = newGrid;

            if (newGrid != null)
            {
                _currentWindow = newGrid.GetParentWindow().gameObject;
            }

        }
        public void LeaveGrid(InvGrid specificGrid)
        {
            if (specificGrid == _invGrid)
            {
                _invGrid = null;
            }
        }
        public void SetHoveredCell(CellInteract cell)
        {
            //update hover utilities
            _hoveredCell = cell;
            _hoveredCellIndex = cell.Index();
            _hoverIndex.x = _hoveredCellIndex.Item1;
            _hoverIndex.y = _hoveredCellIndex.Item2;

            //used if the pointer isn't on the grid, but we get directional input
            _lastKnownHoveredIndex = _hoveredCellIndex;

            //only visualize hover effects when the context window isn't open
            if (ContextWindowHelper.IsContextWindowShowing())
                return;
            PlayMovementAudio();
        }
        public void ClearHoveredCell(CellInteract cell)
        {
            if (_hoveredCell == cell)
                ClearHoveredCell();
        }
        private void ClearHoveredCell()
        {
            _hoveredCell = null;
            _hoveredCellIndex = (-1, -1);
            _hoverIndex.x = _hoveredCellIndex.Item1;
            _hoverIndex.y = _hoveredCellIndex.Item2;
        }




        public void RespondToChangeInputFieldCommand()
        {
            if (_lockInvSystem)
                return;

            if (ContextWindowHelper.IsNumericalSelectorWindowOpen())
            {
                ContextWindowHelper.ActivateNumericalSelectorInputEditing();
                return;
            }

            if (_invGrid != null)
            {
                _invGrid.GetParentWindow().ActivateInputFieldEditing();
                return;
            }
        }
        public void RespondToJumpWindowCommand()
        {
            if (_lockInvSystem)
                return;

            if (_isInventoryInteractionsLocked)
                return;

            //jump to the next opened window if [default:left-shift] is held down...
            if (_altCmd)
                FocusOnNextOpenedWindow();

            //otherwise, jump to the previous opened window
            else
                FocusOnPreviousOpenedWindow();

        }
        public void RotateHeldItemClockwise()
        {
            if (_lockInvSystem)
                return;

            if (_isInventoryInteractionsLocked)
                return;

            if (_heldItem != null)
            {
                _heldItem.RotateItem(RotationDirection.Clockwise);
                PlayRotateAudio();
            }
                
        }
        public void RotateHeldItemCounterClockwise()
        {
            if (_lockInvSystem)
                return;

            if (_isInventoryInteractionsLocked)
                return;

            if (_heldItem != null )
            {
                _heldItem.RotateItem(RotationDirection.CounterClockwise);
                PlayRotateAudio();
            }
        }
        public void RespondToLeftClick()
        {
            if (_lockInvSystem)
                return;

            //close the numerical selector if a click was detected outside of it (and the click wasn't in the confirm btn)
            if (ContextWindowHelper.IsNumericalSelectorWindowOpen())
            {
                RectTransform numericalRectTransform = _contextWindowController.NumericalSelectorRectTransform();

                //this just checks within the context menu AND the confirmBtn for the mouse
                if (!RectTransformUtility.RectangleContainsScreenPoint(numericalRectTransform, _mousePosition)
                    && !RectTransformUtility.RectangleContainsScreenPoint(_contextWindowController.GetConfirmBtnRectTransform(),_mousePosition))
                    ContextWindowHelper.HideNumericalSelector();

                return;
            }

            //close the transferMenu if the click was outside of the menu
            if (ContextWindowHelper.IsTransferMenuOpen())
            {
                RectTransform transferMenuRectTransform = _contextWindowController.GetTransferMenuRectTransform();

                if (!RectTransformUtility.RectangleContainsScreenPoint(transferMenuRectTransform, _mousePosition))
                    _contextWindowController.HideTransferMenu();

                return;
            }

            //close the window if the click wasn't inside the menu
            if (ContextWindowHelper.IsContextWindowShowing())
            {
                RectTransform contextRectTransform = _contextWindowController.GetComponent<RectTransform>();
                
                if (!RectTransformUtility.RectangleContainsScreenPoint(contextRectTransform,_mousePosition))
                    ContextWindowHelper.HideContextWindow();

                return;
            }

            if (_isInventoryInteractionsLocked)
                return;

            //only respond if we're hovering over a grid
            if (_invGrid != null)
            {
                //allow normal manipulation of the container's contents if the container isn't a merchant
                if (!_invGrid.IsMerchant())
                {
                    //if we're not holding an item..
                    if (_heldItem == null)
                    {
                        //if we're holding the alt command, perform quick transfer based on the uiContext
                        if (_altCmd)
                        {
                            //quick-take the stack if we aren't in a home inventory
                            if (_invGrid != _homeInventoryGrid)
                                TakeStackOnHoveredPosition();
                            else
                            {
                                TransferStackOnHoveredPosition();
                            }
                                
                        }

                        //else pickup the full stack of items
                        else
                            PickupStackOnHoveredPosition();
                    }


                    //else,place the full held itemStack on the grid
                    else
                        DropStackOnHoveredPosition();
                }

                //otherwise, we're in a merchant's container. Let's behave ourselves accordingly
                else
                {
                    //simply show the context menu if we're holding no item
                    if (_heldItem == null)
                        ShowContextMenuOnHoveredItem();

                    //otherwise, the player is trying to sell an item
                    //help the player sell the item (assuming it's sellable)
                    else if (_heldItem.ItemData().IsSellable())
                    {
                        //attempt the transaction
                        if (SellItemToMerchant(_heldItem.ItemData(),_heldItemStackCount,_invGrid,_homeInventoryGrid))
                        {
                            //clear the held item, since all of them sold
                            ItemCreatorHelper.ReturnItemToCreator(_heldItem);
                            _heldItem = null;
                            _heldItemStackCount = 0;
                            UpdateHeldStackText();
                            return;

                        }

                        //Either the merchant can't accept the full stack, or the user's home inv is full. Ignore this request
                        else
                        {
                            Debug.LogWarning($"Attempted to sell {_heldItemStackCount} '{_heldItem.name}'(s), " +
                                $"but either the merchant container has insufficient space, or the player's home container has insufficient space for the payment. Ignoring sell request.");
                        }
                    }
                }
            }
        }
        public void RespondToRightClick()
        {
            if (_lockInvSystem)
                return;

            if (ContextWindowHelper.IsNumericalSelectorWindowOpen())
            {
                ContextWindowHelper.HideNumericalSelector();
                return;
            }

            if (ContextWindowHelper.IsTransferMenuOpen())
            {
                _contextWindowController.HideTransferMenu();
                return;
            }

            //close the context menu if ANY rightClick is made
            if (ContextWindowHelper.IsContextWindowShowing())
            {
                ContextWindowHelper.HideContextWindow();
                return;
            }

            if (_isInventoryInteractionsLocked)
                return;

            if (_invGrid != null)
            {
                //Allow quick-manipulations if we aren't within a merchant's inventory
                if (!_invGrid.IsMerchant())
                {
                    //pick up half the stack if we're not holding anything
                    if (_heldItem == null && _invGrid.IsCellOccupied(_hoveredCellIndex))
                        PickupHalfOfHoveredStack();

                    //drop a single item if we're holding a stack, and there's a valid drop position underneath
                    else if (_heldItem != null)
                        DropSingleItemOntoHoveredPosition();
                }

                else
                {
                    //otherwise, if we're holding an item already, sell a single item [assuming it's sellable]
                    if (_heldItem != null )
                    {
                        if (_heldItem.ItemData().IsSellable())
                        {
                            //attempt the transaction
                            if (SellItemToMerchant(_heldItem.ItemData(), 1, _invGrid, _homeInventoryGrid))
                            {
                                _heldItemStackCount--;
                                if (_heldItemStackCount == 0)
                                {
                                    //clear the held item, since all of them sold
                                    ItemCreatorHelper.ReturnItemToCreator(_heldItem);
                                    _heldItem = null;
                                }

                                UpdateHeldStackText();
                                return;

                            }

                            //Either the merchant can't accept the full stack, or the user's home inv is full. Ignore this request
                            else
                            {
                                Debug.LogWarning($"Attempted to sell {1} '{_heldItem.name}'(s), " +
                                    $"but either the merchant container has insufficient space, or the player's home container has insufficient space for the payment. Ignoring sell request.");
                            }
                        }
                    }

                    //otherwise, just open the context menu
                    else
                    {
                        ShowContextMenuOnHoveredItem();
                    }

                }
                
            }
        }
        public void RespondToMiddleClick()
        {
            if (_lockInvSystem)
                return;

            if (_isInventoryInteractionsLocked)
                return;

            if (_invGrid != null)
            {
                //only show if we're hovering over an item while not holding anything
                if (_invGrid.IsCellOccupied(_hoveredCellIndex) && _heldItem == null)
                    ShowContextMenuOnHoveredItem();
            }
        }
        public void RespondToScroll()
        {
            if (_lockInvSystem)
                return;

            if (ContextWindowHelper.IsNumericalSelectorWindowOpen())
            {
                //if scrolling within the input field
                if (RectTransformUtility.RectangleContainsScreenPoint(_contextWindowController.GetInputAreaRectTransform(), _mousePosition))
                {
                    int modifier = (int)(_scrollDelta);

                    //Debug.Log($"Detected Scroll Magnitude: {modifier}");
                    if (modifier > 0)
                        ContextWindowHelper.IncrementNumericalSelector(modifier);
                    else if (modifier < 0)
                        ContextWindowHelper.DecrementNumericalSelector(-modifier);

                    return;
                }
            }
        }
        public void RespondToLeftDirectionalCommand()
        {
            if (_lockInvSystem)
                return;

            if (ContextWindowHelper.IsNumericalSelectorWindowOpen())
                return;

            if (_contextWindowController.IsTransferMenuOpen())
                return;

            if (ContextWindowHelper.IsContextWindowShowing())
                return;

            if (_isInventoryInteractionsLocked)
                return;

            if (_lastKnownGrid != null)
            {
                //Only respond if the last know grid's window is opened
                if (_lastKnownGrid.GetParentWindow().IsWindowOpen())
                {
                    //ignore this call if no width dimension exists
                    if (_lastKnownGrid.ContainerSize().x <= 0)
                        return;


                    //check if we can go left on the grid,
                    //from our last known hover position
                    if (_lastKnownHoveredIndex.Item1 > 0)
                    {
                        //go to the next left x position
                        SetHoveredCell(_lastKnownGrid.GetCellObject((_lastKnownHoveredIndex.Item1 - 1, _lastKnownHoveredIndex.Item2)));
                    }

                    //wrap around to the other side of the grid 
                    else
                    {
                        //go to the opposite end of the grid
                        SetHoveredCell(_lastKnownGrid.GetCellObject((_lastKnownGrid.ContainerSize().x - 1, _lastKnownHoveredIndex.Item2)));
                    }

                    //bind the pointer container to this cell position
                    BindPointerContainerToCellPosition(_lastKnownGrid);

                }
                //else, check if our home grid is opened, and if so then reset our lastHovered util to that
                else if (_homeInventoryGrid != null)
                {
                    SetPointerToHomeGrid();
                }
                else if (_openedInvWindows.Count > 0)
                {
                    FocusOnNextOpenedWindow();
                }
            }

            //else, check if our home grid is opened, and if so then reset our lastHovered util to that
            else if (_homeInventoryGrid != null)
            {
                SetPointerToHomeGrid();
            }

            else if (_openedInvWindows.Count > 0)
            {
                FocusOnNextOpenedWindow();
            }
        }
        public void RespondToRightDirectionalCommand()
        {
            if (_lockInvSystem)
                return;

            if (ContextWindowHelper.IsNumericalSelectorWindowOpen())
                return;

            if (_contextWindowController.IsTransferMenuOpen())
                return;

            if (ContextWindowHelper.IsContextWindowShowing())
                return;

            if (_isInventoryInteractionsLocked)
                return;

            if (_lastKnownGrid != null)
            {
                //Only respond if the last know grid's window is opened
                if (_lastKnownGrid.GetParentWindow().IsWindowOpen())
                {
                    //ignore this call if no width dimension exists
                    if (_lastKnownGrid.ContainerSize().x <= 0)
                        return;


                    //check if we can go right on the grid,
                    //from our last known hover position
                    if (_lastKnownHoveredIndex.Item1 < _lastKnownGrid.ContainerSize().x -1)
                    {
                        //go to the next right x position
                        SetHoveredCell(_lastKnownGrid.GetCellObject((_lastKnownHoveredIndex.Item1 + 1, _lastKnownHoveredIndex.Item2)));
                    }

                    //wrap around to the other side of the grid 
                    else
                    {
                        //go to the opposite end of the grid
                        SetHoveredCell(_lastKnownGrid.GetCellObject((0, _lastKnownHoveredIndex.Item2)));
                    }

                    //bind the pointer container to this cell position
                    BindPointerContainerToCellPosition(_lastKnownGrid);
                }

                //else, check if our home grid is opened, and if so then reset our lastHovered util to that
                else if (_homeInventoryGrid != null)
                {
                    SetPointerToHomeGrid();
                }

                else if (_openedInvWindows.Count > 0)
                {
                    FocusOnNextOpenedWindow();
                }
            }
            //else, check if our home grid is opened, and if so then reset our lastHovered util to that
            else if (_homeInventoryGrid != null)
            {
                SetPointerToHomeGrid();
            }

            else if (_openedInvWindows.Count > 0)
            {
                FocusOnNextOpenedWindow();
            }
        }
        public void RespondToUpDirectionalCommand()
        {
            if (_lockInvSystem)
                return;

            if (ContextWindowHelper.IsNumericalSelectorWindowOpen())
            {
                if (!ContextWindowHelper.IsNumericalSelectorCurrentlyFocused())
                    ContextWindowHelper.FocusOnNumericalSelector();

                if (_altCmd && _altCmd2)
                    ContextWindowHelper.IncrementNumericalSelector(100);
                else if (_altCmd || _altCmd2)
                    ContextWindowHelper.IncrementNumericalSelector(10);
                else
                    ContextWindowHelper.IncrementNumericalSelector(1);

                return;
            }

            if (_contextWindowController.IsTransferMenuOpen())
            {

                return;
            }

            if (ContextWindowHelper.IsContextWindowShowing())
            {
                
                if (!ContextWindowHelper.IsAnyMenuOptionCurrentlyFocused())
                {
                    ContextWindowHelper.FocusOnMenu();
                    Debug.Log($"No menu option was selected. Now {EventSystem.current.currentSelectedGameObject} is selected.");
                    return;
                }
                return;
            }

            if (_isInventoryInteractionsLocked)
                return;

            if (_lastKnownGrid != null)
            {
                //Only respond if the last know grid's window is opened
                if (_lastKnownGrid.GetParentWindow().IsWindowOpen())
                {
                    //ignore this call if no height dimension exists
                    if (_lastKnownGrid.ContainerSize().y <= 0)
                        return;


                    //check if we can go up on the grid,
                    //from our last known hover position
                    if (_lastKnownHoveredIndex.Item2 < _lastKnownGrid.ContainerSize().y -1)
                    {
                        //go to the next upper y position
                        SetHoveredCell(_lastKnownGrid.GetCellObject((_lastKnownHoveredIndex.Item1, _lastKnownHoveredIndex.Item2 + 1)));
                    }

                    //wrap around to the other side of the grid 
                    else
                    {
                        //go to the opposite end of the grid
                        SetHoveredCell(_lastKnownGrid.GetCellObject((_lastKnownHoveredIndex.Item1, 0)));
                    }

                    //bind the pointer container to this cell position
                    BindPointerContainerToCellPosition(_lastKnownGrid);
                }

                //else, check if our home grid is opened, and if so then reset our lastHovered util to that
                else if (_homeInventoryGrid != null)
                {
                    SetPointerToHomeGrid();
                }

                else if (_openedInvWindows.Count > 0)
                {
                    FocusOnNextOpenedWindow();
                }

            }
            //else, check if our home grid is opened, and if so then reset our lastHovered util to that
            else if (_homeInventoryGrid != null)
            {
                SetPointerToHomeGrid();
            }

            else if (_openedInvWindows.Count > 0)
            {
                FocusOnNextOpenedWindow();
            }
        }
        public void RespondToDownDirectionalCommand()
        {
            if (_lockInvSystem)
                return;

            if (ContextWindowHelper.IsNumericalSelectorWindowOpen())
            {
                if (!ContextWindowHelper.IsNumericalSelectorCurrentlyFocused())
                    ContextWindowHelper.FocusOnNumericalSelector();

                if (_altCmd && _altCmd2)
                    ContextWindowHelper.DecrementNumericalSelector(100);
                else if (_altCmd || _altCmd2)
                    ContextWindowHelper.DecrementNumericalSelector(10);
                else
                    ContextWindowHelper.DecrementNumericalSelector(1);

                return;
            }

            if (_contextWindowController.IsTransferMenuOpen())
            {

                return;
            }

            if (ContextWindowHelper.IsContextWindowShowing())
            {
                if (!ContextWindowHelper.IsAnyMenuOptionCurrentlyFocused())
                {
                    ContextWindowHelper.FocusOnMenu();
                    return;
                }
                return;
            }

            if (_isInventoryInteractionsLocked)
                return;

            if (_lastKnownGrid != null)
            {
                //Only respond if the last know grid's window is opened
                if (_lastKnownGrid.GetParentWindow().IsWindowOpen())
                {
                    //ignore this call if no height dimension exists
                    if (_lastKnownGrid.ContainerSize().y <= 0)
                        return;


                    //check if we can go down on the grid,
                    //from our last known hover position
                    if (_lastKnownHoveredIndex.Item2 > 0)
                    {
                        //go to the next upper y position
                        SetHoveredCell(_lastKnownGrid.GetCellObject((_lastKnownHoveredIndex.Item1, _lastKnownHoveredIndex.Item2 -1)));
                    }

                    //wrap around to the other side of the grid 
                    else
                    {
                        //go to the opposite end of the grid
                        SetHoveredCell(_lastKnownGrid.GetCellObject((_lastKnownHoveredIndex.Item1, _lastKnownGrid.ContainerSize().y -1)));
                    }

                    //bind the pointer container to this cell position
                    BindPointerContainerToCellPosition(_lastKnownGrid);
                }

                //else, check if our home grid is opened, and if so then reset our lastHovered util to that
                else if (_homeInventoryGrid != null)
                {
                    SetPointerToHomeGrid();
                }

                else if (_openedInvWindows.Count > 0)
                {
                    FocusOnNextOpenedWindow();
                }
            }
            //else, check if our home grid is opened, and if so then reset our lastHovered util to that
            else if (_homeInventoryGrid != null)
            {
                SetPointerToHomeGrid();
            }

            else if (_openedInvWindows.Count > 0)
            {
                FocusOnNextOpenedWindow();
            }
        }
        public void SetAlternateInputs(bool alternate1, bool alternate2, bool alternate3)
        {
            if (_lockInvSystem)
                return;

            _altCmd = alternate1;
            _altCmd2 = alternate2;
            _altCmd3 = alternate3;
        }
        public void SetMousePosition(Vector2 position)
        {
            if (_lockInvSystem)
                return;

            //update the mouse's latest position
            _mousePosition = position;

            //bind possible held object to position
            BindPointerContainerToMousePosition();
        }
        public void SetInputMode(InputMode newMode)
        {
            if (_lockInvSystem)
                return;

            if (_inputMode != newMode)
            {
                if (ContextWindowHelper.IsContextWindowShowing())
                {
                    //IF We're switching from directional to pointer mode with the context menu open
                    if (_inputMode == InputMode.Directional && newMode == InputMode.Pointer)
                    {
                        //deselect the current navigation element
                        EventSystem.current.SetSelectedGameObject(null);
                    }
                }

                _inputMode = newMode;
                //Debug.Log($"InputMode changed to [{_inputMode.ToString()}]");

                if (_inputMode == InputMode.Pointer)
                {
                    ClearHoveredCell();
                    ClearHoverTiles();
                    ClearDirectionalPointer();

                    if (!ContextWindowHelper.IsContextWindowShowing())
                        ReEnterGridOnPointerLocation();

                    ContextWindowHelper.SetPointerMode(true);
                    _graphicRaycaster.enabled = true;
                }

                else if (_inputMode == InputMode.Directional)
                {
                    
                    if (_invGrid == null)
                        SetPointerToHomeGrid();

                    ContextWindowHelper.SetPointerMode(false);
                    _graphicRaycaster.enabled = false;
                }

                
            }
        }
        public void SetScrollInput(Vector2 scrollDelta)
        {
            if (_lockInvSystem)
                return;

            //Debug.Log($"Scroll Read in from InputReader: {scrollDelta.y}");
            _scrollDelta = scrollDelta.y * _scrollMultiplier;
            RespondToScroll();
        }
        public InputMode GetInputMode() { return _inputMode; }
        public void OpenInventoryWindow()
        {
            if (_lockInvSystem)
                return;

            if (_homeInventoryGrid != null)
            {
                //only open the window if it's closed
                if (!_homeInventoryGrid.GetParentWindow().IsWindowOpen())
                    _homeInventoryGrid.GetParentWindow().OpenWindow();
            }
            
        }
        public void CloseInventoryWindow()
        {
            if (_lockInvSystem)
                return;

            if (_homeInventoryGrid != null)
            {
                //only close the window if its open
                if (_homeInventoryGrid.GetParentWindow().IsWindowOpen())
                    _homeInventoryGrid.GetParentWindow().CloseWindow();
            }
            
        }
        public void ToggleInventoryWindow()
        {
            if (_lockInvSystem)
                return;

            if (_homeInventoryGrid.GetParentWindow().IsWindowOpen())
                CloseInventoryWindow();

            else 
                OpenInventoryWindow();
        }
        public void RespondToBackInput()
        {
            if (_lockInvSystem)
                return;

            //back out of the numerical selector if it's open
            if (ContextWindowHelper.IsNumericalSelectorWindowOpen())
            {
                ContextWindowHelper.HideNumericalSelector();
                return;
            }

            if (ContextWindowHelper.IsTransferMenuOpen())
            {
                ContextWindowHelper.HideTransferMenu();
                return;
            }

            //close the context window if it's open
            if (ContextWindowHelper.IsContextWindowShowing())
            {
                HideContextMenu();
                return;
            }

            //else close whatever inventory we're in (if we aren't holding an item)
            if (_invGrid != null)
            {
                if (_invGrid.GetParentWindow().gameObject.activeSelf && _heldItem == null)
                {
                    Debug.Log("Closing Window [currentSelected]");
                    _invGrid.GetParentWindow().CloseWindow();
                    return;
                }
            }

            Debug.Log("Back Clicked. Reached Final close case [case: are any windows opened that can be closed?]");
            //else if any inv window is still opened, close it
            if (_openedInvWindows.Count > 0)
            {
                Debug.Log("Closing Window [Last In Line]");
                _openedInvWindows.Last().CloseWindow();
                return;
            }
        }
        public void RespondToConfirm()
        {
            if (_lockInvSystem)
                return;

            if (_ignoreConfirmUntilDelayExpires)
                return;

            if (ContextWindowHelper.IsNumericalSelectorWindowOpen())
            {
                Debug.Log("Confirm Detected. Submitting number");
                ContextWindowHelper.SubmitCurrentNumber();
                return;
            }
                

            if (ContextWindowHelper.IsContextWindowShowing())
                return;

            if (_isInventoryInteractionsLocked)
                return;


            //only respond if we're hovering over a grid
            if (_invGrid != null)
            {
                //dont talk to closed windows!
                if (!_invGrid.GetParentWindow().IsWindowOpen())
                    return;

                //if no alternate buttons are held down, then show the context menu
                if (!_altCmd && !_altCmd2 && !_altCmd3)
                {
                    if (_heldItem == null)
                        ShowContextMenuOnHoveredItem(); //only shows if not hovering over something
                    else
                    {
                        if (!_invGrid.IsMerchant())
                            DropStackOnHoveredPosition();
                        else
                        {
                            if (_heldItem.ItemData().IsSellable())
                            {
                                //attempt to sell whatever we're holding
                                if (SellItemToMerchant(_heldItem.ItemData(), _heldItemStackCount, _invGrid, _homeInventoryGrid))
                                {
                                    //sell successful. Update the heldItem utilities
                                    ItemCreatorHelper.ReturnItemToCreator(_heldItem);
                                    _heldItem = null;
                                    _heldItemStackCount = 0;
                                    UpdateHeldStackText();
                                }

                                //otherwise, do nothing since something's preventing us from selling this stuff
                            }
                        }
                    }

                }

                //if [default:left-shift] is held down...
                else if (_altCmd)
                {
                    //if we're not holding an item, pickup the full stack of items, bypassing the context menu
                    //[assuming we aren't messing with a merchant's possessions]
                    if (_heldItem == null)
                    {
                        
                        if (!_invGrid.IsMerchant())
                            PickupStackOnHoveredPosition();

                        //This is a merchant we're messing with. Just show the context window
                        else
                            ShowContextMenuOnHoveredItem();
                    }

                    //else,place a single stack on this position
                    else
                    {
                        if (!_invGrid.IsMerchant())
                            DropSingleItemOntoHoveredPosition();
                        else
                        {
                            if (_heldItem.ItemData().IsSellable())
                            {
                                //attempt to sell a single thing at a time
                                if (SellItemToMerchant(_heldItem.ItemData(), 1, _invGrid, _homeInventoryGrid))
                                {
                                    //sell successful. Update the heldItem utilities
                                    _heldItemStackCount--;

                                    if (_heldItemStackCount == 0)
                                    {
                                        ItemCreatorHelper.ReturnItemToCreator(_heldItem);
                                        _heldItem = null;

                                    }

                                    UpdateHeldStackText();
                                }

                                //otherwise, do nothing since something's preventing us from selling this stuff
                            }

                        }
                    }
                }

                //if [default:left-ctrl] is held down...
                else if (_altCmd2)
                {
                    if (!_invGrid.IsMerchant())
                    {
                        if (_heldItem == null)
                            PickupHalfOfHoveredStack();
                    }
                    else
                    {
                        if (_heldItem == null)
                            ShowContextMenuOnHoveredItem();
                    }
                    
                }

                
            }
        }
        private void SetInternalInventoryInteractionLock(bool newState)
        {
            _isInventoryInteractionsLocked = newState;

            /*
            if (_isInteractionsLocked)
                Debug.Log("Interactions Locked");
            else
                Debug.Log("UNLOCKED Interactions");
            */
            
        }
        public void IgnoreOtherConfirmCommandsUntilEndOfFrame()
        {
            _ignoreConfirmUntilDelayExpires = true;
            //only trigger the coroutine if one isn't yet active
            if (_resetIgnoreConfirmFlagCoroutine == null)
            {
                _resetIgnoreConfirmFlagCoroutine = ResetIgnoreConfirmFlagAfterDelay();
                StartCoroutine(_resetIgnoreConfirmFlagCoroutine);
            }
        }
        private IEnumerator ResetIgnoreConfirmFlagAfterDelay()
        {
            yield return new WaitForSeconds(_ignoreDelay);
            _ignoreConfirmUntilDelayExpires = false;
            _resetIgnoreConfirmFlagCoroutine = null;
        }
        private void ReEnterGridOnPointerLocation()
        {
            //Debug.Log($"Attempting to re-enter any window on the pointer's position...");
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = _mousePosition
            };

            EventSystem.current.RaycastAll(eventData, raycastResults);

            foreach(RaycastResult result in raycastResults)
            {
                //Debug.Log($"Parsing detection: {result.gameObject.name}");
                InvWindow invWindow = result.gameObject.GetComponent<InvWindow>();

                if (invWindow == null)
                {
                    //Debug.Log($"No invWindow found on this gameObject.");
                    continue;
                }
                else
                {
                    //Debug.Log($"Success [{invWindow.name}]. Focusing on this window");
                    FocusOnWindowForPointerInput(invWindow);
                    VisualizeHover();
                    return;
                }

                
            }

            //Debug.Log($"No invWindow found to re-enter");
        }
        public bool DoOpenedContainersExist() { return _openedInvWindows.Count > 0; }
        public List<InvWindow> GetOpenedContainers()
        {
            if (!DoOpenedContainersExist())
                return new();

            List<InvWindow> returnList = new();

            for (int i= 0; i < _openedInvWindows.Count; i++)
            {
                returnList.Add(_openedInvWindows[i]);
            }

            return returnList;

        }
        public bool IsCurrentInvGrid(InvGrid invGrid)
        {
            return invGrid == _invGrid;
        }
        public bool IsCurrentContextualInvGrid(InvGrid invGrid)
        {
            return invGrid == _contextualInvGrid;
        }
        public void SetTransferReceiverContext(InvGrid receiver)
        {
            _contextualGridReceiver = receiver;
        }
        public bool IsInvSystemLocked() { return _lockInvSystem; }
        public void SetInvSystemLock(bool newState) 
        { 
            _lockInvSystem = newState;
        }
        public Transform GetParentUiTransformForContainers() { return _inventoryWindowsContainer; }
        public bool IsPointerPointContainedWithinInvUi()
        {
            if (_inputMode == InputMode.Pointer)
            {
                //return true if any invWindow is currently hovered
                if (_currentHoveredWindow != null)
                    return true;

                //return true if the pointer is in any subUi menu system
                if (ContextWindowHelper.IsNumericalSelectorWindowOpen())
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(_contextWindowController.NumericalSelectorRectTransform(), _mousePosition))
                        return true;
                }

                if (ContextWindowHelper.IsContextWindowShowing())
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(_contextWindowController.GetComponent<RectTransform>(), _mousePosition))
                        return true;
                }

                if (ContextWindowHelper.IsTransferMenuOpen())
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(_contextWindowController.GetTransferMenuRectTransform(), _mousePosition))
                        return true;
                }

                return false;
                
            }

            return false;
            
        }
        public void SetCurrentHoveredInvWindow(InvWindow invWindow)
        {
            _currentHoveredWindow = invWindow;
            //Debug.Log("Hovered Inv Ui Set");
        }
        public void ClearInvWindowFromHovered(InvWindow previousInWindow)
        {
            if (previousInWindow == _currentHoveredWindow)
            {
                _currentHoveredWindow = null;
                //Debug.Log("Cleared hovered Inv Ui ");
            }
        }
        public Vector3 GetMousePosition()
        {
            return _mousePosition;
        }
        public Canvas GetCanvas() { return _uiCanvas; }
        public List<InvWindow> GetOpenedMerchantContainers() 
        { 
            //we're providing a new collection in case something wants to modify the reference
            List<InvWindow> merchants = new List<InvWindow>();
            
            for (int i = 0; i < _openedMerchants.Count; i++)
                merchants.Add(_openedMerchants[i]);
            return merchants; 
        }
        public List<InvWindow> GetOpenedNonMerchantContainers()
        {
            List<InvWindow> nonMerchants = new();
            for (int i = 0; i < _openedInvWindows.Count; i++)
            {
                if (!_openedInvWindows[i].GetItemGrid().IsMerchant())
                    nonMerchants.Add(_openedInvWindows[i]);
            }

            return nonMerchants;
        }
        public InvGrid GetContextualInvGrid() { return _contextualInvGrid; }

    }

    public static class InvManagerHelper
    {


        public static InvInteracter _invController;
        public static void SetInventoryController(InvInteracter invController) { _invController = invController; }
        public static InvInteracter GetInvController() { return _invController; }
        public static void SetActiveItemGrid(InvGrid newGrid) 
        { 
            _invController.SetActiveItemGrid(newGrid);
        }
        public static void BringWindowToFront(InvWindow window) {_invController.SetWindowToFront(window);}
        public static void LeaveGrid(InvGrid gridToLeave) { _invController.LeaveGrid(gridToLeave); }
        public static void SetHoveredCell(CellInteract cell) { _invController.SetHoveredCell(cell);  }
        public static void ClearHoveredCell(CellInteract cell) { _invController.ClearHoveredCell(cell); }
        public static void TrackNewInvWindow(InvWindow window) { if (!_invController.IsWindowBeingTracked(window)) _invController.TrackInvWindow(window); }
        public static void UnTrackInvWindow(InvWindow window) { _invController.UntrackInvWindow(window); }
        public static void ParentInvWindowToInventoryUisContainer(InvWindow window) { _invController.ParentWindowToContainer(window); }

        public static bool DoOpenedContainersExist() { return _invController.DoOpenedContainersExist(); }
        public static List<InvWindow> GetOpenedContainers() { return _invController.GetOpenedContainers(); }
        public static AudioSource GetInvInteracterAudiosource() { return _invController.GetComponent<AudioSource>(); }
        public static void SetInvSystemLock(bool newState) { _invController.SetInvSystemLock(newState); }
        public static bool IsInvSystemLocked() { return _invController.IsInvSystemLocked(); }
        public static void IgnoreOtherConfirmCommandsUntilEndOfFrame() { _invController.IgnoreOtherConfirmCommandsUntilEndOfFrame();}
        public static Transform GetParentUiTransformForContainers() { return _invController.GetParentUiTransformForContainers(); }
        public static void SetCurrentHoveredInvWindow(InvWindow hoveredWindow) { _invController.SetCurrentHoveredInvWindow(hoveredWindow); }
        public static void ClearHoveredInvWindow(InvWindow previousHoveredWindow) { _invController.ClearInvWindowFromHovered(previousHoveredWindow); }
        public static bool IsPointerWithinUiRect() { return _invController.IsPointerPointContainedWithinInvUi(); }
        public static Vector2 GetMousePosition() { return _invController.GetMousePosition(); }
        public static Canvas GetUiCanvas() { return _invController.GetCanvas(); }
        public static List<InvWindow> GetOpenedMerchantContainers() { return _invController.GetOpenedMerchantContainers(); }
        public static List<InvWindow> GetOpenedNonMerchantContainers() { return _invController.GetOpenedNonMerchantContainers(); }
        public static InvGrid GetContextualInvGrid() { return _invController.GetContextualInvGrid();}
    }
}
