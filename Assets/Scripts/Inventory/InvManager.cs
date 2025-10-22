using mapPointer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class InvManager : MonoBehaviour
{
    [SerializeField] private ContextWindowController _contextWindowController;
    [SerializeField] private Text _heldStackText;
    [SerializeField] private RectTransform _heldStackContainer;
    private InvGrid _invGrid;
    [SerializeField] private InventoryItem _heldItem;
    [SerializeField] private int _heldItemStackCount = 0;
    [SerializeField] private GameObject _pointerContainer;
    [SerializeField] private Canvas _uiCanvas;
    [SerializeField] private Camera _uiCam;
    private RectTransform _pointerRectTransform;
    private Vector2 _localPoint;
    [SerializeField] CellInteract _hoveredCell;
    (int,int) _hoveredCellIndex = (-1,-1);
    (int, int) _contextualItemPosition = (-1, -1);
    private InvGrid _contextualInvGrid;
    [SerializeField] Vector2Int _hoverIndex;
    [SerializeField] InventoryItem _itemInCell;
    HashSet<(int,int)> _itemCellOccupancy = new();
    private List<ContextOption> _itemContextualOptions = new();

    [SerializeField] private GameObject _hoverGraphicPrefab;
    [SerializeField] List<GameObject> _unusedHoverTileGraphics = new();
    [SerializeField] List<GameObject> _hoverTileGraphics = new();
    [SerializeField] private HashSet<(int, int)> _hoveredIndexes = new();
    [SerializeField] private HashSet<(int,int)> _lastFramesHoveredIndexes = new();

    private bool _lClick = false;
    private bool _rClick = false;
    private bool _mClick = false;
    

    //Monobehaviours
    private void Awake()
    {
        _contextWindowController.gameObject.SetActive(true);

        ScreenPositionerHelper.SetUiCamera(_uiCam);
        InvManagerHelper.SetInventoryController(this);
        _pointerRectTransform = _pointerContainer.GetComponent<RectTransform>();
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

        RespondToRotationCommands();
        VisualizeHover();
        RespondToInvClicks();
        BindPointerParentToMousePosition();
    }


    //internals
    private void RespondToRotationCommands()
    {
        if (_heldItem != null)
        {

            if (Input.GetKeyDown(KeyCode.Q))
            {
                //rotate the item internally
                _heldItem.RotateItem(RotationDirection.CounterClockwise);

            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                //rotate the item internally
                _heldItem.RotateItem(RotationDirection.Clockwise);
            }
        }
    }
    private void VisualizeHover()
    {
        //save the previous frame's hoverData 
        _lastFramesHoveredIndexes.Clear();
        foreach ((int, int) index in _hoveredIndexes)
            _lastFramesHoveredIndexes.Add(index);
        
        //reset the present frame's hover data
        _hoveredIndexes.Clear();


        if (_hoveredCell == null)
        {
            _itemInCell = null;
            ClearHoverTiles();
        }

        else
        {
            _itemInCell = _invGrid.GetItemGraphicOnCell(_hoverIndex.x, _hoverIndex.y);
            InventoryItem hoveredItem = _invGrid.GetItemGraphicOnCell(_hoverIndex.x, _hoverIndex.y);


            //highlight the previewed position of the held item
            if (_heldItem != null)
            {
                //Get the new potential placement positions
                HashSet<(int, int)> placementPositions = _invGrid.ConvertSpacialDefIntoGridIndexes(_hoveredCellIndex,_heldItem.GetSpacialDefinition(),_heldItem.ItemHandle());

                if (_invGrid.IsAreaWithinGrid(placementPositions))
                {
                    //Looks like our placement is valid here.
                    //Make our hoveredIndexes equal to our expected placement position
                    foreach ((int,int) index in placementPositions)
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
    private void RespondToInvClicks()
    {

        _lClick = Input.GetMouseButtonDown((int)MouseBtn.Left);
        _rClick = Input.GetMouseButtonDown((int)MouseBtn.Right);
        _mClick = Input.GetMouseButtonDown((int)MouseBtn.Middle);

        if (_invGrid != null)
        {
            //right click?
            // -> Pick up half the stack
            // -> Or drop a single item from the held stack
            if (_rClick)
            {
                //pick up half the stack if we're not holding anything
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
                        BindHeldItemToPointer();
                        UpdateHeldStackText();
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
                        InventoryItem pickedUpItem = ItemCreatorHelper.CreateItem(detectedItemData, _invGrid.CellSize().x, _invGrid.CellSize().y).GetComponent<InventoryItem>();
                        _heldItem = pickedUpItem;

                        //remove the heldAmount from the hovered stack
                        _invGrid.DecreaseStack(_hoveredCellIndex, _heldItemStackCount);

                        //update the 'holding item' feedback utils
                        BindHeldItemToPointer();
                        UpdateHeldStackText();
                        return;
                    }


                }

                //drop a single item if we're holding a stack, and there's a valid drop position underneath
                else if (_heldItem != null)
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
                                _invGrid.CreateStack(_hoveredCellIndex, _heldItem, 1);
                                _heldItem = null;
                                _heldItemStackCount = 0;
                                UpdateHeldStackText();
                                return;
                            }

                            else
                            {
                                //create a new sprite for the new stack
                                InventoryItem newItemSprite = ItemCreatorHelper.CreateItem(_heldItem.ItemData(), _invGrid.CellSize().x, _invGrid.CellSize().y).GetComponent<InventoryItem>();

                                //create the new stack of 1
                                _invGrid.CreateStack(_hoveredCellIndex, newItemSprite, 1);

                                //update our held count 
                                _heldItemStackCount -= 1;
                                UpdateHeldStackText();
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
                                        _invGrid.IncreaseStack(_hoveredCellIndex, 1);

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

            //leftClick
            // -> Pickup full stack
            // -> Place the held stack
            else if (_lClick)
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
                    BindHeldItemToPointer();
                    UpdateHeldStackText();
                    return;

                }

                //else,place the full held itemStack on the grid
                else
                {
                    HashSet<(int, int)> placementArea = _invGrid.ConvertSpacialDefIntoGridIndexes(_hoveredCellIndex, _heldItem.GetSpacialDefinition(), _heldItem.ItemHandle());

                    //make sure the entire item area is within the grid
                    if (_invGrid.IsAreaWithinGrid(placementArea))
                    {
                        int itemCount = _invGrid.CountUniqueStacksInArea(placementArea);

                        //if position is completely empty, place here
                        if (itemCount == 0)
                        {
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
                                            _invGrid.IncreaseStack(index, openCapacity);
                                            _heldItemStackCount -= openCapacity;

                                            UpdateHeldStackText();

                                            return;
                                        }

                                    }


                                    //otherwise, swap the stacks
                                    else
                                    {
                                        //save the found item's data
                                        InventoryItem newGraphic = _invGrid.GetItemGraphicOnCell(index);
                                        int stackSize = _invGrid.GetStackValue(index);

                                        //delete the currently-stored item
                                        _invGrid.DeleteStack(index);

                                        //place the held item into the now-fully-open position
                                        _invGrid.CreateStack(_hoveredCellIndex, _heldItem, _heldItemStackCount);

                                        //update our held itemData
                                        _heldItem = newGraphic;
                                        _heldItemStackCount = stackSize;

                                        BindHeldItemToPointer(_invGrid);
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

                        //Ignore clicks that hover over multiple items
                        /*
                        else
                        {
                            //if any compatible stack found, place there
                            foreach ((int,int) index in placementArea)
                            {
                                if (_invGrid.GetStackItemData(index) == _heldItem.ItemData())
                                {
                                    int stackValue = _invGrid.GetStackValue(index);
                                    int stackMaxCapacity = _invGrid.GetStackItemData(index).StackLimit();
                                    int openCapacity = stackMaxCapacity - stackValue;

                                    if (openCapacity > 0)
                                    {
                                        //place the remaining items here if the stack can take it
                                        if (_heldItemStackCount <= openCapacity)
                                        {
                                            _invGrid.IncreaseStack(index, _heldItemStackCount);
                                            ItemCreatorHelper.ReturnItemToCreator(_heldItem);
                                            _heldItem = null;
                                            _heldItemStackCount = 0;

                                            //update the heldStackText Ui
                                            _heldStackText.text = $"{_heldItemStackCount}";
                                            _heldStackText.gameObject.SetActive(false);
                                        }

                                        //otherwise, only place enough items to fill the stack here. Don't clear the held item yet, since we have some left.
                                        else
                                        {
                                            _invGrid.IncreaseStack(index, openCapacity);
                                            _heldItemStackCount -= openCapacity;

                                            //update the heldStackText Ui
                                            _heldStackText.text = $"{_heldItemStackCount}";
                                        }
                                    }
                                }
                            }



                        } */
                    }
                }
            }

            //middleClick
            // -> show context menue
            else if (_mClick)
            {
                if (_invGrid.IsCellOccupied(_hoveredCellIndex))
                {
                    //save the clicked index
                    _contextualItemPosition = _hoveredCellIndex;
                    _contextualInvGrid = _invGrid;

                    //open the context window
                    ContextWindowHelper.ShowContextWindow(_invGrid.GetStackItemData(_hoveredCellIndex).ContextualOptions());

                    return;
                }

            }
        }
    }


    private void BindPointerParentToMousePosition()
    {
        if (_pointerContainer != null)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _uiCanvas.GetComponent<RectTransform>(),
                Input.mousePosition,
                _uiCam,
                out _localPoint))
            {
                _pointerRectTransform.anchoredPosition = _localPoint;
            }
        }
    }
    private void BindHeldItemToPointer()
    {
        BindHeldItemToPointer(_invGrid);
    }
    private void BindHeldItemToPointer(InvGrid specificGrid)
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
    private void RenderItemInfo(string newItemName,string newDesc) 
    { 
        if (_invGrid != null)
        {
            _invGrid.GetParentWindow().SetItemName(newItemName);
            _invGrid.GetParentWindow().SetItemDescription(newDesc);
        }
            
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

        BindHeldItemToPointer(_contextualInvGrid);
        UpdateHeldStackText();
        

        //reset the selected position
        _contextualItemPosition = (-1, -1);
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

    private void RespondToDiscard()
    {
        //exit if the contextual data expired somehow
        if (!IsContextualDataValid())
            return;

        //remove the item from inventory
        _contextualInvGrid.DeleteStack(_contextualItemPosition);
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
        _contextualInvGrid.RemoveItem(_contextualItemPosition,1);

    }

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

    //externals
    public void SetActiveItemGrid(InvGrid newGrid)
    {
        _invGrid = newGrid;
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
        _hoveredCell = cell;
        _hoveredCellIndex = cell.Index();
        _hoverIndex.x = _hoveredCellIndex.Item1;
        _hoverIndex.y = _hoveredCellIndex.Item2;

        
    }
    public void ClearHoveredCell(CellInteract cell)
    {
        if (_hoveredCell == cell)
        {
            _hoveredCell = null;
            _hoveredCellIndex = (-1, -1);
            _hoverIndex.x = _hoveredCellIndex.Item1;
            _hoverIndex.y = _hoveredCellIndex.Item2;
        }
    }


}
