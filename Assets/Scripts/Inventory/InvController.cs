using mapPointer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvController : MonoBehaviour
{
    private InvGrid _invGrid;
    private InventoryItem _selectedItem;
    [SerializeField] private GameObject _pointerContainer;
    [SerializeField] private RectTransform _hoverEffect;
    [SerializeField] private Canvas _uiCanvas;
    [SerializeField] private Camera _uiCam;
    private RectTransform _pointerRectTransform;
    private Vector2 _localPoint;
    private Vector2Int _itemHandle;
    [SerializeField] CellInteract _hoveredCell;
    (int,int) _hoveredCellIndex = (-1,-1);
    [SerializeField] Vector2Int _hoverIndex;
    [SerializeField] InventoryItem _itemInCell;

    [Header("Debug Commands")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private bool _createItem;
    [SerializeField] private ItemData _specifiedItem;

    //Monobehaviours
    private void Awake()
    {
        ScreenPositionerHelper.SetUiCamera(_uiCam);
        InvControlHelper.SetInventoryController(this);
        _pointerRectTransform = _pointerContainer.GetComponent<RectTransform>();
    }

    private void Update()
    {

        if (_isDebugActive)
            ListenForDebugCommands();

        VisualizeHoverTile();
        RespondToInvClicks();
        BindPointerParentToMousePosition();
    }


    //internals
    private void VisualizeHoverTile()
    {
        if (_hoverEffect != null)
        {
            if (_hoveredCell == null)
            {
                _hoverEffect.gameObject.SetActive(false);
                _itemInCell = null;
            }
            else
            {
                _itemInCell = _invGrid.QueryItem(_hoverIndex.x, _hoverIndex.y);

                _hoverEffect.gameObject.SetActive(true);
                _hoverEffect.SetParent(_invGrid.GetComponent<RectTransform>().parent);

                _hoverEffect.localPosition = _hoveredCell.GetComponent<RectTransform>().localPosition;
                InventoryItem hoveredItem = _invGrid.QueryItem(_hoverIndex.x, _hoverIndex.y);
                float cellWidth = _invGrid.CellSize().x;
                float cellHeight = _invGrid.CellSize().y;

                //highlight the previewed position of the held item
                if (_selectedItem != null)
                {
                    //resize the sprite
                    Vector2 itemSpriteSize = new Vector2(_selectedItem.ItemData().Width() * cellWidth, _selectedItem.ItemData().Height() * cellHeight);
                    _hoverEffect.sizeDelta = itemSpriteSize;


                    //calculate the offset to the sprite's bottomLeft tile
                    Vector3 toBottomLeftTileCornerOffset = new();
                    toBottomLeftTileCornerOffset.x = _selectedItem.ItemData().Width() * cellWidth/ 2 - cellWidth / 2;
                    toBottomLeftTileCornerOffset.y = _selectedItem.ItemData().Height() * cellHeight/ 2 - cellHeight / 2;

                    //calculate the offset of the sprite's bottomLeftTile to the selected Handle's tile
                    Vector3 tileOffset = new();
                    tileOffset.x = _itemHandle.x * cellWidth ;
                    tileOffset.y = _itemHandle.y * cellHeight ;

                    _hoverEffect.localPosition += toBottomLeftTileCornerOffset - tileOffset;
                }

                //highlight the hovered item if no item is held
                else if (hoveredItem != null)
                {
                    //resize the sprite
                    Vector2 itemSpriteSize = new Vector2(hoveredItem.ItemData().Width() * cellWidth, hoveredItem.ItemData().Height() * cellHeight);
                    _hoverEffect.sizeDelta = itemSpriteSize;

                    //get item origin
                    Vector2Int itemOriginCell = hoveredItem.GetOriginLocation();

                    Vector2 toBottomLeftTileCornerOffset = new Vector2(cellWidth / 2, cellHeight / 2) * -1;
                    Vector2 originPosition = _invGrid.GetCellOnPosition((itemOriginCell.x, itemOriginCell.y)).GetComponent<RectTransform>().localPosition;

                    Vector2 spriteCenter = originPosition - toBottomLeftTileCornerOffset + itemSpriteSize / 2;
                    Vector2 indexOffByOneOffset = new Vector2(cellWidth, cellHeight);
                    _hoverEffect.localPosition = spriteCenter - indexOffByOneOffset;
                }

                else if (_selectedItem == null && hoveredItem == null)
                {
                    _hoverEffect.sizeDelta = new Vector2(cellWidth, cellHeight);
                }

            }
        }
    }
    private void RespondToInvClicks()
    {

        if (_invGrid != null && Input.GetMouseButtonDown((int)MouseBtn.Left))
        {
            //pickup item on grid if one exists
            if (_selectedItem == null)
            {
                if (_invGrid.QueryItem(_hoverIndex.x,_hoverIndex.y) != null)
                {
                    _selectedItem = _invGrid.TakeItem(_hoverIndex.x, _hoverIndex.y, out _itemHandle);
                    SetItemToMousePosition(_itemHandle);
                }
                
            }

            //place item on grid
            else
            {
                (int, int) itemHandleIntPair = (_itemHandle.x, _itemHandle.y);
                int itemWidth = _selectedItem.ItemData().Width();
                int itemHeight = _selectedItem.ItemData().Height();

                //only allow placement if the area is fully on the grid
                if (_invGrid.IsAreaOnGrid(itemWidth, itemHeight, _hoveredCellIndex, itemHandleIntPair))
                {
                    //place item if the area is unoccupied
                    if (_invGrid.IsAreaUnoccupied(itemWidth, itemHeight, _hoveredCellIndex, itemHandleIntPair))
                    {
                        _invGrid.PlaceItem(_selectedItem, _hoveredCellIndex, itemHandleIntPair);
                        _selectedItem = null;
                    }

                    //else swap items if only 1 item occupies the space
                    else
                    {
                        //check how many different items occupy the space
                        Dictionary<(int, int), InventoryItem> itemOccupancy = _invGrid.GetItemsInArea(itemWidth, itemHeight, _hoveredCellIndex, itemHandleIntPair);
                        List<InventoryItem> uniqueItems = new List<InventoryItem>();

                        foreach (KeyValuePair<(int, int), InventoryItem> entry in itemOccupancy)
                        {
                            if (!uniqueItems.Contains(entry.Value))
                                uniqueItems.Add(entry.Value);
                        }

                        //perform the swap
                        if (uniqueItems.Count == 1)
                        {
                            (InventoryItem, Vector2Int) swapResult = _invGrid.SwapItems(itemWidth, itemHeight, _hoveredCellIndex, itemHandleIntPair, _selectedItem);

                            if (swapResult.Item1 != null)
                            {
                                _selectedItem = swapResult.Item1;
                                _itemHandle = swapResult.Item2;
                                SetItemToMousePosition(_itemHandle);
                            }

                        }

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

    private void SetItemToMousePosition(float cellWidth, float cellHeight)
    {
        if (_selectedItem == null)
            return;

        //reparent the selected item to the pointerContainer. Visualize the pickup
        RectTransform itemRectTransform = _selectedItem.GetComponent<RectTransform>();
        itemRectTransform.SetParent(_pointerRectTransform);

        //offset the selected item onto it's origin tile (0,0)
        int itemWidth = _selectedItem.GetComponent<InventoryItem>().ItemData().Width();
        int itemHeight = _selectedItem.GetComponent<InventoryItem>().ItemData().Height();
        Vector2 originTilePosition = new();

        originTilePosition.x = itemWidth * cellWidth / 2 - cellWidth / 2;
        originTilePosition.y = itemHeight * cellHeight / 2 - cellHeight / 2;



        _itemHandle = new Vector2Int(0, 0);

        itemRectTransform.localPosition = originTilePosition;


        itemRectTransform.localScale = Vector2.one;
    }
    private void SetItemToMousePosition(Vector2Int itemHandle)
    {
        if (_selectedItem == null)
            return;

        float tileWidth = _invGrid.CellSize().x;
        float tileHeight = _invGrid.CellSize().y;

        //reparent the selected item to the pointerContainer. Visualize the pickup
        RectTransform itemRectTransform = _selectedItem.GetComponent<RectTransform>();
        itemRectTransform.SetParent(_pointerRectTransform);

        //offset the selected item onto it's origin tile (0,0)
        int itemWidth = _selectedItem.GetComponent<InventoryItem>().ItemData().Width();
        int itemHeight = _selectedItem.GetComponent<InventoryItem>().ItemData().Height();
        Vector2 originTilePosition = new();

        //pivot point is in the center of the UiObjects. Go to the bottomLeft corner
        originTilePosition.x = itemWidth * tileWidth / 2;
        originTilePosition.y = itemHeight * tileHeight / 2;

        //now go to the center of tile (0,0)
        originTilePosition.x -= tileWidth / 2;
        originTilePosition.y -= tileHeight / 2;


        //now go the itemHandle's position
        Vector2 handlePosition = new();
        handlePosition.x = -tileWidth * _itemHandle.x;
        handlePosition.y = -tileHeight * _itemHandle.y;

        itemRectTransform.localPosition = originTilePosition + handlePosition;
        itemRectTransform.localScale = Vector2.one;
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
        if (_createItem && _invGrid != null && _selectedItem == null)
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

            _selectedItem = item;

            SetItemToMousePosition(cellWidth, cellHeight);


        }
    }
}
