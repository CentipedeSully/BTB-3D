using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemGrid : MonoBehaviour
{
    //Declarations
    const float _tileSizeWidth = 32;
    const float _tileSizeHeight = 32;
    RectTransform _rectTransform;
    Vector2 _positionOnGrid;
    float _rawPosX;
    float _rawPosY;
    Vector2Int _tilePosition;

    [SerializeField] private Vector2Int _containerSize;
    InventoryItem[,] _invItems;

    [Header("Debug Commands")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private bool _placeItem;
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private Vector2Int _placementPosition;


    //Monobehaviours
    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        InitializeContainer(_containerSize.x,_containerSize.y);
    }

    private void Update()
    {
        if (_isDebugActive)
            ListenForDebugCommands();
    }




    //Internals
    private void InitializeContainer(int width, int height)
    {
        _invItems = new InventoryItem[width ,height];
        Vector2 size = new Vector2(width * _tileSizeWidth, height * _tileSizeHeight);

        _rectTransform.sizeDelta = size;

    }






    //Externals
    public Vector2Int GetTileGridPosition(Vector2 mousePosition)
    {
        _positionOnGrid.x = mousePosition.x - _rectTransform.anchoredPosition.x;
        _positionOnGrid.y = mousePosition.y - _rectTransform.anchoredPosition.y;

        _rawPosX = _positionOnGrid.x / _tileSizeWidth;
        _rawPosY = _positionOnGrid.y / _tileSizeHeight;

        //fix cases where negative positions are off by 1
        if (_rawPosX < 0)
        {
            _rawPosX -= 1;
        }
        if (_rawPosY < 0)
        {
            _rawPosY -= 1;
        }

        _tilePosition.x = (int)(_rawPosX);
        _tilePosition.y = (int)(_rawPosY);

        return _tilePosition;
    }

    public void PlaceItem(InventoryItem item, int x, int y)
    {
        //parent image to grid
        RectTransform  itemRectTransform = item.GetComponent<RectTransform>();
        itemRectTransform.SetParent(_rectTransform);

        _invItems[x,y] = item;

        Vector2 position = new Vector2();

        //offset and center the item to it's necessary cell position
        position.x = x * _tileSizeWidth + _tileSizeWidth / 2;
        position.y = y * _tileSizeHeight + _tileSizeHeight / 2;

        itemRectTransform.localPosition = position;
        itemRectTransform.localScale = Vector2.one;
    }

    /// <summary>
    /// Returns the item at the grid position. Does NOT remove it.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public InventoryItem GetItem(int x, int y)
    {
        return _invItems[x,y];
    }

    public InventoryItem RemoveItem(int x, int y)
    {
        InventoryItem querydItem = _invItems[x, y];
        _invItems[x, y] = null;
        return querydItem;
    }

    //debug utils
    private void ListenForDebugCommands()
    {
        if (_placeItem && _itemPrefab != null)
        {
            _placeItem = false;
            GameObject newItemObject = Instantiate(_itemPrefab);
            InventoryItem item = newItemObject.GetComponent<InventoryItem>();

            PlaceItem(item, _placementPosition.x, _placementPosition.y);
        }
    }

}
