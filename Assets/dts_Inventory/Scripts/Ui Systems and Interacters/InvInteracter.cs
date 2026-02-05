using mapPointer;
using System.Collections.Generic;
using System.Reflection;
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
        private RectTransform _defaultParentOfPointerContainer;
        [SerializeField] private Canvas _uiCanvas;
        [SerializeField] private GameObject _hoverGraphicPrefab;
        [SerializeField] private GameObject _directionalPointerHoverPrefab;
        [SerializeField] private InvGrid _homeInventoryGrid;
        private InvGrid _invGrid;

        [Header("Audio Settings")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _movementAudio;
        [SerializeField] private AudioClip _rotateAudio;
        [SerializeField] private AudioClip _selectionAudio;
        [SerializeField] private AudioClip _deselectionAudio;
        [SerializeField] private AudioClip _discardAudio;
        [SerializeField] private AudioClip _useAudio;

        [Header("Watch/Debug Values [Do Not Touch]")]
        [Tooltip("AutoDetects and updates based on the last detected input command. No need to touch this")]
        [SerializeField] private InputMode _inputMode = InputMode.Pointer;
        [SerializeField] private InvItem _heldItem;
        [SerializeField] private int _heldItemStackCount = 0;
        [SerializeField] CellInteract _hoveredCell;
        [SerializeField] Vector2Int _hoverIndex;
        [SerializeField] InvItem _itemInCell;


        private RectTransform _pointerRectTransform;
        private Vector2 _localPoint;
        
        (int, int) _hoveredCellIndex = (-1, -1);
        (int, int) _contextualItemPosition = (-1, -1);
        private InvGrid _contextualInvGrid;
        
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


        //Monobehaviours
        private void Awake()
        {
            _contextWindowController.gameObject.SetActive(true);

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

        private void OnEnable()
        {
            _contextWindowController.OnOptionSelected += ListenForValidContextualOption;
        }

        private void OnDisable()
        {
            _contextWindowController.OnOptionSelected -= ListenForValidContextualOption;
        }


        private void Update()
        {
            VisualizeHover();
        }


        //internals

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
                        HashSet<(int, int)> placementPositions = _invGrid.ConvertSpacialDefIntoGridIndexes(_hoveredCellIndex, _heldItem.GetSpacialDefinition(), _heldItem.ItemHandle());

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
                            RenderItemInfo(_heldItem.ItemData().Name(), _heldItem.ItemData().Desc());
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
                        foreach ((int, int) index in _invGrid.GetStackOccupancy(_hoveredCellIndex))
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
                        RenderItemInfo(hoveredItem.ItemData().Name(), hoveredItem.ItemData().Desc());
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

                    SetActiveItemGrid(_lastKnownGrid);
                    SetHoveredCell(_lastKnownGrid.GetCellObject((0,0)));
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
                    HashSet<(int, int)> placementPositions = _invGrid.ConvertSpacialDefIntoGridIndexes(_hoveredCellIndex, _heldItem.GetSpacialDefinition(), _heldItem.ItemHandle());

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
                        RenderItemInfo(_heldItem.ItemData().Name(), _heldItem.ItemData().Desc());
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
                    foreach ((int, int) index in _invGrid.GetStackOccupancy(_hoveredCellIndex))
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
                    RenderItemInfo(hoveredItem.ItemData().Name(), hoveredItem.ItemData().Desc());
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

                    //hide the graphic
                    currentHoverGraphic.SetActive(false);

                    //remove the graphic from the active list
                    _hoverTileGraphics.Remove(currentHoverGraphic);

                    //add the graphic to the inactive list
                    _unusedHoverTileGraphics.Add(currentHoverGraphic);
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
        private void RenderItemInfo(string newItemName, string newDesc)
        {
            if (_invGrid != null)
            {
                _invGrid.GetParentWindow().SetItemName(newItemName);
                _invGrid.GetParentWindow().SetItemDescription(newDesc);
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

                //reset the hover utils back to home origin
                SetActiveItemGrid(_homeInventoryGrid);
                SetHoveredCell(_homeInventoryGrid.GetCellObject((0, 0)));

                //bind the pointer container to this cell position
                BindPointerContainerToCellPosition(_lastKnownGrid);
            }
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
                    _invGrid.DeleteStack(_hoveredCellIndex);

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
                    _invGrid.DecreaseStack(_hoveredCellIndex, _heldItemStackCount);

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
                HashSet<(int, int)> placementArea = _invGrid.ConvertSpacialDefIntoGridIndexes(_hoveredCellIndex, _heldItem.GetSpacialDefinition(), _heldItem.ItemHandle());

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
                            _invGrid.CreateStack(_hoveredCellIndex, _heldItem, 1);
                            _heldItem = null;
                            _heldItemStackCount = 0;
                            UpdateHeldStackText();
                            return;
                        }

                        else
                        {
                            //create a new sprite for the new stack
                            InvItem newItemSprite = ItemCreatorHelper.CreateItem(_heldItem.ItemData(), _invGrid.CellSize().x, _invGrid.CellSize().y).GetComponent<InvItem>();

                            //make sure the new stack's rotation matches the held item
                            while (newItemSprite.Rotation() != _heldItem.Rotation())
                            {
                                newItemSprite.RotateItem(RotationDirection.Clockwise);
                            }

                            //create the new stack of 1
                            _invGrid.CreateStack(_hoveredCellIndex, newItemSprite, 1);

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
                                    _invGrid.IncreaseStack(index, 1);

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
                //save the item reference
                _heldItem = _invGrid.GetItemGraphicOnCell(_hoveredCellIndex);

                //save the amount of the item held
                _heldItemStackCount = _invGrid.GetStackValue(_hoveredCellIndex);

                //remove the item from the invGrid
                _invGrid.DeleteStack(_hoveredCellIndex);

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
                HashSet<(int, int)> placementArea = _invGrid.ConvertSpacialDefIntoGridIndexes(_hoveredCellIndex, _heldItem.GetSpacialDefinition(), _heldItem.ItemHandle());

                //make sure the entire item area is within the grid
                if (_invGrid.IsAreaWithinGrid(placementArea))
                {
                    int itemCount = _invGrid.CountUniqueStacksInArea(placementArea);

                    //if position is completely empty, place here
                    if (itemCount == 0)
                    {
                        PlayItemDropAudio();
                        _invGrid.CreateStack(_hoveredCellIndex, _heldItem, _heldItemStackCount);
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
                                        _invGrid.IncreaseStack(index, _heldItemStackCount);
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
                                        _invGrid.IncreaseStack(index, openCapacity);
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
                                    _invGrid.DeleteStack(index);

                                    //place the held item into the now-fully-open position
                                    _invGrid.CreateStack(_hoveredCellIndex, _heldItem, _heldItemStackCount);

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
                
                //open the context window
                ContextWindowHelper.ShowContextWindow(pointerPositionRelativeToCanvas, _invGrid.GetParentWindow() ,_invGrid.GetStackItemData(_hoveredCellIndex).ContextualOptions());

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
        private void ListenForValidContextualOption(ContextOption selectedOption)
        {
            //only respond to the contextual menu if we have an active item selected
            if (_contextualItemPosition != (-1, -1))
            {
                switch (selectedOption)
                {
                    case ContextOption.OrganizeItem:
                        RespondToOrganize();
                        return;

                    case ContextOption.UseItem:
                        RespondToUse();
                        return;

                    case ContextOption.DiscardItem:
                        RespondToDiscard();
                        return;

                    default:
                        return;

                }
            }

        }
        
        

        //menu Option Responses
        private void RespondToOrganize()
        {
            //exit if the contextual data expired somehow
            if (!IsContextualDataValid())
            {
                _contextualInvGrid = null;
                _contextualItemPosition = (-1, -1);
                return;
            }

            //save the item reference
            _heldItem = _contextualInvGrid.GetItemGraphicOnCell(_contextualItemPosition);

            //save the amount of the item held
            _heldItemStackCount = _contextualInvGrid.GetStackValue(_contextualItemPosition);

            //remove the item from the invGrid
            _contextualInvGrid.DeleteStack(_contextualItemPosition);

            BindHeldItemToPointerContainer(_contextualInvGrid);
            UpdateHeldStackText();

            PlayItemPickupAudio();

            //reset the selected position
            _contextualItemPosition = (-1, -1);
        }
        private void RespondToDiscard()
        {
            //exit if the contextual data expired somehow
            if (!IsContextualDataValid())
                return;

            //remove the item from inventory
            _contextualInvGrid.DeleteStack(_contextualItemPosition);

            PlayDiscardAudio();
        }
        private void RespondToUse()
        {
            //exit if the contextual data expired somehow
            if (!IsContextualDataValid())
                return;


            ItemData contextualItem = _contextualInvGrid.GetStackItemData(_contextualItemPosition);

            //use the item
            Debug.Log($"Used {contextualItem.name}!");

            //remove the item from inventory
            _contextualInvGrid.RemoveItem(_contextualItemPosition, 1);

            PlayUseAudio();

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

        //externals
        public void SetActiveItemGrid(InvGrid newGrid)
        {
            _invGrid = newGrid;

            //used if the pointer isn't on the grid, but we get directional input
            _lastKnownGrid = newGrid;
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


        public void RotateHeldItemClockwise()
        {
            if (ContextWindowHelper.IsContextWindowShowing())
                return;

            if (_heldItem != null)
            {
                _heldItem.RotateItem(RotationDirection.Clockwise);
                PlayRotateAudio();
            }
                
        }
        public void RotateHeldItemCounterClockwise()
        {
            if (ContextWindowHelper.IsContextWindowShowing())
                return;

            if (_heldItem != null )
            {
                _heldItem.RotateItem(RotationDirection.CounterClockwise);
                PlayRotateAudio();
            }
        }
        public void RespondToLeftClick()
        {
            if (ContextWindowHelper.IsContextWindowShowing())
            {
                RectTransform contextRectTransform = _contextWindowController.GetComponent<RectTransform>();

                //close the window if the click wasn't inside the menu
                if (!RectTransformUtility.RectangleContainsScreenPoint(contextRectTransform,_mousePosition))
                    ContextWindowHelper.HideContextWindow();

                return;
            }

            //only respond if we're hovering over a grid
            if (_invGrid != null)
            {
                //if we're not holding an item, pickup the full stack of items
                if (_heldItem == null)
                    PickupStackOnHoveredPosition();

                //else,place the full held itemStack on the grid
                else
                    DropStackOnHoveredPosition();
            }
        }
        public void RespondToRightClick()
        {
            //close the context menu of ANY rightClick is made
            if (ContextWindowHelper.IsContextWindowShowing())
            {
                ContextWindowHelper.HideContextWindow();
                return;
            }

            if (_invGrid != null)
            {
                //pick up half the stack if we're not holding anything
                if (_heldItem == null && _invGrid.IsCellOccupied(_hoveredCellIndex))
                    PickupHalfOfHoveredStack();

                //drop a single item if we're holding a stack, and there's a valid drop position underneath
                else if (_heldItem != null)
                    DropSingleItemOntoHoveredPosition();
            }
        }
        public void RespondToMiddleClick()
        {

            if (ContextWindowHelper.IsContextWindowShowing())
                return;

            if (_invGrid != null)
            {
                //only show if we're hovering over an item while not holding anything
                if (_invGrid.IsCellOccupied(_hoveredCellIndex) && _heldItem == null)
                    ShowContextMenuOnHoveredItem();
            }
        }
        public void RespondToLeftDirectionalCommand()
        {
            if (ContextWindowHelper.IsContextWindowShowing())
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
                        SetHoveredCell(_lastKnownGrid.GetCellObject((_lastKnownGrid.ContainerSize().x -1, _lastKnownHoveredIndex.Item2)));
                    }

                    //bind the pointer container to this cell position
                    BindPointerContainerToCellPosition(_lastKnownGrid);
                    
                }

                //else, check if our home grid is opened, and if so then reset our lastHovered util to that
                else if (_homeInventoryGrid != null)
                {
                    SetPointerToHomeGrid();
                }
            }
        }
        public void RespondToRightDirectionalCommand()
        {
            if (ContextWindowHelper.IsContextWindowShowing())
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
            }
        }
        public void RespondToUpDirectionalCommand()
        {
            if (ContextWindowHelper.IsContextWindowShowing())
            {
                if (!ContextWindowHelper.IsAnyMenuOptionCurrentlyFocused())
                {
                    ContextWindowHelper.FocusOnMenu();
                    return;
                }
                return;
            }

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
            }
        }
        public void RespondToDownDirectionalCommand()
        {
            if (ContextWindowHelper.IsContextWindowShowing())
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
            }
        }
        public void SetAlternateInputs(bool alternate1, bool alternate2, bool alternate3)
        {
            _altCmd = alternate1;
            _altCmd2 = alternate2;
            _altCmd3 = alternate3;
        }
        public void SetMousePosition(Vector2 position)
        {
            //update the mouse's latest position
            _mousePosition = position;

            //bind possible held object to position
            BindPointerContainerToMousePosition();
        }
        public void SetInputMode(InputMode newMode)
        {
            if (_inputMode != newMode)
            {
                _inputMode = newMode;
                //Debug.Log($"InputMode changed to [{_inputMode.ToString()}]");

                if (_inputMode == InputMode.Pointer)
                {
                    ClearHoveredCell();
                    ClearHoverTiles();
                    ClearDirectionalPointer();
                }
                else if (_inputMode == InputMode.Directional)
                {
                    
                    if (_invGrid == null)
                        SetPointerToHomeGrid();
                }
            }
        }
        public void OpenInventoryWindow()
        {
            if (_homeInventoryGrid != null)
            {
                //only open the window if it's closed
                if (!_homeInventoryGrid.GetParentWindow().IsWindowOpen())
                    _homeInventoryGrid.GetParentWindow().OpenWindow();
            }
            
        }
        public void CloseInventoryWindow()
        {
            if (_homeInventoryGrid != null)
            {
                //only close the window if its open
                if (_homeInventoryGrid.GetParentWindow().IsWindowOpen())
                    _homeInventoryGrid.GetParentWindow().CloseWindow();
            }
            
        }
        public void ToggleInventoryWindow()
        {
            if (_homeInventoryGrid.GetParentWindow().IsWindowOpen())
                CloseInventoryWindow();

            else 
                OpenInventoryWindow();
        }
        public void RespondToBackInput()
        {
            //close the context window if it's open
            if (ContextWindowHelper.IsContextWindowShowing())
            {
                HideContextMenu();
            }

            //else close whatever inventory we're in (if we aren't holding an item)
            else if (_invGrid != null)
            {
                if (_invGrid.GetParentWindow().gameObject.activeSelf && _heldItem == null)
                {
                    _invGrid.GetParentWindow().gameObject.SetActive(false);
                }
            }
        }
        public void RespondToConfirm()
        {
            if (ContextWindowHelper.IsContextWindowShowing())
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
                        DropStackOnHoveredPosition();
                }

                //if [default:left-shift] is held down...
                else if (_altCmd)
                {
                    //if we're not holding an item, pickup the full stack of items, bypassing the context menu
                    if (_heldItem == null)
                        PickupStackOnHoveredPosition();

                    //else,place a single stack on this position
                    else
                        DropSingleItemOntoHoveredPosition();
                }

                //if [default:left-ctrl] is held down...
                else if (_altCmd2)
                {
                    if (_heldItem == null)
                        PickupHalfOfHoveredStack();
                }

                
            }
        }


        
    }

    public static class InvManagerHelper
    {


        public static InvInteracter _invController;
        public static void SetInventoryController(InvInteracter invController) { _invController = invController; }
        public static InvInteracter GetInvController() { return _invController; }
        public static void SetActiveItemGrid(InvGrid newGrid) { _invController.SetActiveItemGrid(newGrid); }
        public static void LeaveGrid(InvGrid gridToLeave) { _invController.LeaveGrid(gridToLeave); }
        public static void SetHoveredCell(CellInteract cell) { _invController.SetHoveredCell(cell); }
        public static void ClearHoveredCell(CellInteract cell) { _invController.ClearHoveredCell(cell); }

    }
}
