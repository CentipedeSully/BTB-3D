using mapPointer;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Compilation;
using UnityEngine;
using static UnityEditor.Progress;

public class InvManager : MonoBehaviour
{
    [SerializeField] private ContextWindowController _contextWindowController;
    private InvGrid _invGrid;
    [SerializeField] private InventoryItem _heldItem;
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

    [Header("Debug Commands")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private bool _createItem;
    [SerializeField] private ItemData _specifiedItem;
    [SerializeField] private bool _removeItem;
    [SerializeField] private int _quantity;
    [SerializeField] private string _itemName;
    

    //Monobehaviours
    private void Awake()
    {
        _contextWindowController.gameObject.SetActive(true);
        _contextWindowController.gameObject.SetActive(false);

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

        if (_isDebugActive)
            ListenForDebugCommands();

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
            _itemInCell = _invGrid.GetItemOnCell(_hoverIndex.x, _hoverIndex.y);
            InventoryItem hoveredItem = _invGrid.GetItemOnCell(_hoverIndex.x, _hoverIndex.y);


            //highlight the previewed position of the held item
            if (_heldItem != null)
            {
                //Get the new potential placement positions
                List<(int, int)> placementPositions = _invGrid.ConvertSpacialDefIntoGridIndexes(_hoveredCellIndex,_heldItem.GetSpacialDefinition(),_heldItem.ItemHandle());

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
                foreach ((int, int) index in _invGrid.GetItemOccupancy(hoveredItem))
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

        if (_invGrid != null && Input.GetMouseButtonDown((int)MouseBtn.Left))
        {
            //if we're not holding an item, open the context menu for the item on the specified cell
            if (_heldItem == null)
            {

                if (_invGrid.IsCellOccupied(_hoveredCellIndex))
                {
                    //save the clicked index
                    _contextualItemPosition = _hoveredCellIndex;
                    _contextualInvGrid = _invGrid;

                    //open the context window
                    ContextWindowHelper.ShowContextWindow(_invGrid.GetItemOnCell(_hoveredCellIndex).ContextualOptions());
                }

            }

            //else,place the held item on the grid
            else
            {
                List<(int, int)> placementArea = _invGrid.ConvertSpacialDefIntoGridIndexes(_hoveredCellIndex, _heldItem.GetSpacialDefinition(), _heldItem.ItemHandle());

                //First ensure the placement area is within the grid
                if (_invGrid.IsAreaWithinGrid(placementArea))
                {
                    int itemCount = _invGrid.CountItemsInArea(placementArea);

                    //swap items if ONE item is within the palcement area
                    if (itemCount == 1)
                    {
                        InventoryItem pickedUpItem = null;

                        //find any cell that holds the preexisting item
                        //(any of them will do. Any occupied cell here will be holding the same item)
                        //verified this with the previous itemCount
                        foreach ((int, int) index in placementArea)
                        {
                            pickedUpItem = _invGrid.GetItemOnCell(index);
                            if (pickedUpItem != null)
                                break;
                        }

                        //save the reference to the item and remove the currently-stashed item from the grid
                        _invGrid.RemoveItem(pickedUpItem);

                        //place the held item in the freed up space
                        _invGrid.PositionItemIntoGridLogically(_heldItem, placementArea);
                        _invGrid.PositionItemGraphicOntoGridVisually(_hoveredCellIndex, _heldItem);
                        _heldItem = null;

                        //make the picked-up item the new held item
                        _heldItem = pickedUpItem;
                        BindSelectedItemToPointer();

                    }

                    else if (itemCount == 0)
                    {
                        //place the held item into the open space and clear our held item reference
                        _invGrid.PositionItemIntoGridLogically(_heldItem, placementArea);
                        _invGrid.PositionItemGraphicOntoGridVisually(_hoveredCellIndex, _heldItem);
                        _heldItem = null;

                    }

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
    private void BindSelectedItemToPointer()
    {
        if (_heldItem != null)
        {
            //parent the item to the pointer
            _heldItem.GetComponent<RectTransform>().SetParent(_pointerContainer.transform, false);

            //reset the item's local position to zero
            RectTransform itemRectTransform = _heldItem.GetComponent<RectTransform>();
            itemRectTransform.localPosition = Vector3.zero;

            //ensure the sprite is of the appropriate size
            itemRectTransform.sizeDelta = new Vector2(_heldItem.Width() * _invGrid.CellSize().x, _heldItem.Height() * _invGrid.CellSize().y);
        }
        
    }
    private void BindSelectedItemToPointer(InvGrid specificGrid)
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
        _heldItem = _contextualInvGrid.GetItemOnCell(_contextualItemPosition);

        //remove the item from the invGrid
        _contextualInvGrid.RemoveItem(_heldItem);

        BindSelectedItemToPointer(_contextualInvGrid);

        //reset the selected position
        _contextualItemPosition = (-1, -1);
    }

    private void RespondToDiscard()
    {
        //exit if the contextual data expired somehow
        if (!IsContextualDataValid())
            return;

        //remove the item from inventory
        InventoryItem contextualItem = _contextualInvGrid.GetItemOnCell(_contextualItemPosition);
        _contextualInvGrid.RemoveItem(contextualItem);
    }

    private void RespondToUse()
    {
        //exit if the contextual data expired somehow
        if (!IsContextualDataValid())
            return;


        InventoryItem contextualItem = _contextualInvGrid.GetItemOnCell(_contextualItemPosition);

        //use the item
        Debug.Log($"Used {contextualItem.name}!");

        //remove the item from inventory
        _contextualInvGrid.RemoveItem(contextualItem);

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

        InventoryItem contextualItem = _contextualInvGrid.GetItemOnCell(_contextualItemPosition);
        if (contextualItem == null)
        {
            Debug.LogWarning($"Attempted to get an item at position ({_contextualItemPosition.Item1},{_contextualItemPosition.Item2}) within a Missing ItemGrid Reference. " +
                "The item must've moved unexpectedly, since the context wouldn't have appeared for a empty position");
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



    //debug utils
    private void ListenForDebugCommands()
    {
        if (_createItem && _invGrid != null && _heldItem == null)
        {
            
            GameObject newItemObject = null;
            float cellWidth = _invGrid.CellSize().x;
            float cellHeight = _invGrid.CellSize().y;

            if (_specifiedItem == null)
            {
                newItemObject = ItemCreatorHelper.CreateRandomItem(cellWidth, cellHeight);
            }
            else newItemObject = ItemCreatorHelper.CreateItem(_specifiedItem, cellWidth, cellHeight);

            InventoryItem item = newItemObject.GetComponent<InventoryItem>();


            //calculate the object's pivot position
            float xPivotPosition = cellWidth * item.ItemHandle().Item1 + cellWidth/2;
            float yPivotPosition = cellHeight * item.ItemHandle().Item2 + cellHeight/2;

            float normalizedPositionX = xPivotPosition / (item.Width() * cellWidth);
            float normalizedPositionY = yPivotPosition / (item.Height() * cellHeight);

            RectTransform itemRectTransform = item.GetComponent<RectTransform>();
            itemRectTransform.pivot = new Vector2(normalizedPositionX, normalizedPositionY);


            //find space on the current active grid
            List<(int, int)> foundSpace = null;
            (int, int) gridPosition = (-1, -1);
            ItemRotation necessaryRotation = ItemRotation.None;

            foundSpace = _invGrid.FindSpaceForItem(item, out gridPosition, out necessaryRotation);

            if (foundSpace != null)
            {
                //rotate the item until it matches the necessary rotation
                while (item.Rotation() != necessaryRotation)
                    item.RotateItem(RotationDirection.Clockwise);

                _invGrid.PositionItemIntoGridLogically(item, foundSpace);
                _invGrid.PositionItemGraphicOntoGridVisually(gridPosition, item);

                _createItem = false;
            }
            else
            {
                Debug.LogWarning($"Couldn't find space for item '{item.name}'.");
            }

        }
        if (_invGrid != null && _removeItem)
        {
            if (_invGrid.DoesItemAndQuantityExist(_itemName, _quantity))
            {

                int removalsPerformed = 0;

                //fix the test case where an invalid quantity is deliberately applied
                if (_quantity < 1)
                    _quantity = 1;

                while (removalsPerformed < _quantity)
                {
                    _invGrid.RemoveItem(_itemName);
                    removalsPerformed++;
                }

            }

            else
                Debug.LogWarning("Item or specified Quantity doesn't exist on grid. Ignoring Removal Request.");

            _removeItem = false;

        }
    }
}
