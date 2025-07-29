using mapPointer;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    private ItemGrid _invGrid;
    private InventoryItem _selectedItem;
    [SerializeField] private GameObject _pointerContainer;
    [SerializeField] private Canvas _uiCanvas;
    [SerializeField] private Camera _uiCam;
    private RectTransform _pointerRectTransform;
    private Vector2 _localPoint;
    private Vector2Int _itemHandle;
    private Vector2 _mousePosition;
    private Vector2 _detectedTilePosition;
    private Vector2Int _hoverPositionOnGrid;

    [Header("Debug Commands")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private bool _createItem;
    [SerializeField] private ItemData _specifiedItem;

    //Monobehaviours
    private void Awake()
    {
        InvControlHelper.SetInventoryController(this);
        _pointerRectTransform = _pointerContainer.GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();

        RespondToInvClicks();
        BindPointerParentToMousePosition();
        WatchMousePosition();
    }


    //internals
    private void WatchMousePosition()
    {
        _mousePosition = Input.mousePosition;
        if (_invGrid != null)
        {
            _hoverPositionOnGrid = _invGrid.GetTileOnGrid(_mousePosition);
            _detectedTilePosition = _invGrid.GetPositionFromGridTile(_hoverPositionOnGrid.x, _hoverPositionOnGrid.y);
        }
            
    }
    private void RespondToInvClicks()
    {

        if (_invGrid != null && Input.GetMouseButtonDown((int)MouseBtn.Left))
        {
            Vector2Int clickPosition = _invGrid.GetTileOnGrid(Input.mousePosition);
            if (_selectedItem == null)
            {
                if (_invGrid.QueryItem(clickPosition.x,clickPosition.y) != null)
                {
                    _selectedItem = _invGrid.TakeItem(clickPosition.x, clickPosition.y, out _itemHandle);
                    SetItemToMousePosition(_itemHandle);
                }
            }
            else
            {
                Debug.Log($"Clicked position {clickPosition}");
                (int, int) handle = (_itemHandle.x, _itemHandle.y);
                if (_invGrid.PlaceItem(_selectedItem,(clickPosition.x,clickPosition.y), handle))
                {
                    _selectedItem = null;
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

    private void SetItemToMousePosition(float tileWidth, float tileHeight)
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

        originTilePosition.x = itemWidth * tileWidth / 2 - tileWidth/2;
        originTilePosition.y = itemHeight * tileHeight / 2- tileHeight / 2;
        


        _itemHandle = new Vector2Int(0, 0);

        itemRectTransform.localPosition = originTilePosition;


        itemRectTransform.localScale = Vector2.one;
    }
    private void SetItemToMousePosition(Vector2Int itemHandle)
    {
        if (_selectedItem == null)
            return;

        float tileWidth = _invGrid.TileWidth();
        float tileHeight = _invGrid.TileHeight();

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
        originTilePosition.y -= tileHeight/ 2;


        //now go the itemHandle's position
        Vector2 handlePosition = new();
        handlePosition.x = -tileWidth * _itemHandle.x;
        handlePosition.y = -tileHeight * _itemHandle.y;

        itemRectTransform.localPosition = originTilePosition + handlePosition;
        itemRectTransform.localScale = Vector2.one;
    }





    //externals
    public void SetActiveItemGrid(ItemGrid newGrid)
    {
        _invGrid = newGrid;

        //also 'SetItemToMousePosition' again, since sprite size depends on the active gridObject's tileSize
        SetItemToMousePosition(_itemHandle);
    }

    public void LeaveGrid(ItemGrid specificGrid)
    {
        if (specificGrid == _invGrid)
        {
            _invGrid = null;
        }
    }





    //debug utils
    private void ListenForDebugCommands()
    {
        if (_createItem && _invGrid != null && _selectedItem == null)
        {
            GameObject newItemObject = null;
            float tileWidth = _invGrid.TileWidth();
            float tileHeight = _invGrid.TileHeight();

            if (_specifiedItem == null)
            {
                newItemObject = ItemCreatorHelper.CreateRandomItem(tileWidth, tileHeight);
            }
            else newItemObject = ItemCreatorHelper.CreateItem(_specifiedItem, tileWidth, tileHeight);

            InventoryItem item = newItemObject.GetComponent<InventoryItem>();

            _selectedItem = item;

            SetItemToMousePosition(tileWidth, tileHeight);
            

        }
    }




}
