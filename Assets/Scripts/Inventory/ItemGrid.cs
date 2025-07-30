using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

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



    //Monobehaviours
    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        InitializeContainer(_containerSize.x,_containerSize.y);
    }




    //Internals
    private void InitializeContainer(int width, int height)
    {
        _invItems = new InventoryItem[width ,height];
        Vector2 size = new Vector2(width * _tileSizeWidth, height * _tileSizeHeight);

        _rectTransform.sizeDelta = size;

    }





    //Externals
    public Dictionary<(int,int),InventoryItem> GetItemsInArea(int width, int height, (int, int) clickedGridPosition, (int, int) itemHandle)
    {
        //calculate the item's expected (0,0) position on the grid
        int startingX = clickedGridPosition.Item1 - itemHandle.Item1;
        int startingY = clickedGridPosition.Item2 - itemHandle.Item2;

        Dictionary<(int,int),InventoryItem> foundOccupancy = new Dictionary<(int,int),InventoryItem>();
        (int, int) indexPair;

        //check each cell
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                indexPair = (startingX+i, startingY+j);

                if (IsCellOnGrid(indexPair))
                {
                    if (IsCellOccupied(indexPair))
                        foundOccupancy.Add(indexPair, QueryItem(indexPair.Item1, indexPair.Item2));

                }
            }
        }

        return foundOccupancy;

    }

    public (InventoryItem,Vector2Int newItemHandle) SwapItems(int width, int height, (int, int) clickedGridPosition, (int, int) itemHandle, InventoryItem newItem)
    {
        if (newItem == null)
            return (null, -Vector2Int.one);
        InventoryItem returnedItem = null;

        //take the preexisting item from the grid
        Dictionary<(int, int), InventoryItem> itemsFound = GetItemsInArea(width, height, clickedGridPosition, itemHandle);

        if (itemsFound == null)
        {
            PlaceItem(newItem, clickedGridPosition, itemHandle);
            return (null, -Vector2Int.one);
        }
        else 
        {
            //check how many different items occupy the space
            List<InventoryItem> uniqueItems = new List<InventoryItem>();

            foreach (KeyValuePair<(int, int), InventoryItem> entry in itemsFound)
            {
                if (!uniqueItems.Contains(entry.Value))
                    uniqueItems.Add(entry.Value);
            }

            //perform the swap
            if (uniqueItems.Count == 1)
            {
                //setup the take operation
                (int, int) arbitraryIndex = (-1,-1);
                foreach ((int,int) key in itemsFound.Keys)
                {
                    arbitraryIndex = key;
                    break;
                }

                Vector2Int newHandle = Vector2Int.zero;
                returnedItem = TakeItem(arbitraryIndex.Item1, arbitraryIndex.Item2, out newHandle);

                //place the new item at the clickedPosition
                PlaceItem(newItem,clickedGridPosition, itemHandle);

                //return the taken item
                return (returnedItem, newHandle);
            }

            else
            {
                Debug.LogWarning("Attempted to swap items, but more than one item found in grid area. aborting operation and returning null");
                return (null, -Vector2Int.one);
            }
        }
    }

    public bool IsAreaUnoccupied(int width, int height, (int, int) clickedGridPosition, (int, int) itemHandle)
    {
        if (GetItemsInArea(width, height, clickedGridPosition, itemHandle).Count == 0)
            return true;
        else return false;
    }

    public bool IsAreaOnGrid(int width, int height, (int, int) clickedGridPosition, (int, int) itemHandle)
    {
        //calculate the item's expected (0,0) position on the grid
        int startingX = clickedGridPosition.Item1 - itemHandle.Item1;
        int startingY = clickedGridPosition.Item2 - itemHandle.Item2;

        Dictionary<(int, int), InventoryItem> foundOccupancy = new Dictionary<(int, int), InventoryItem>();
        (int, int) indexPair;

        //check each cell
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                indexPair = (startingX + i, startingY + j);

                //return false if any index exists outside the grid
                if (!IsCellOnGrid(indexPair))
                {
                    return false;
                }
                    
            }
        }

        return true;
    }

    public bool IsCellOnGrid((int,int) cell)
    {
        if (cell.Item1 < 0 || cell.Item1 >= _containerSize.x || cell.Item2 < 0 || cell.Item2 >= _containerSize.y)
        {
            //Debug.Log($"INVALID Cell: {cell}");
            return false;
        }

        //Debug.Log($"Cell {cell} confirmed");
        return true;
    }

    public bool IsCellOccupied((int,int) cell)
    {
        bool isCellOccupied = _invItems[cell.Item1, cell.Item2] != null;
        //Debug.Log($"Cell {cell} Occupancy: {isCellOccupied}");
        return isCellOccupied;
    }

    public Vector2Int GetTileOnGrid(Vector2 mousePosition)
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

    public Vector2 GetPositionFromGridTile(int x, int y)
    {
        Vector2 tilePosition = new();

        tilePosition.x = x * _tileSizeWidth + _tileSizeWidth / 2;
        tilePosition.y = y * _tileSizeHeight + _tileSizeHeight / 2;

        return _rectTransform.anchoredPosition + tilePosition;
    }

    public void PlaceItem(InventoryItem item, (int,int)clickedPosition ,(int,int)itemHandle)
    {
        if (item == null)
            return;

        int itemWidth = item.ItemData().Width();
        int itemHeight = item.ItemData().Height();


        if (IsAreaUnoccupied(itemWidth, itemHeight, clickedPosition, itemHandle))
        {
            
            //calculate the item's expected (0,0) position on the grid
            int startingX = clickedPosition.Item1 - itemHandle.Item1;
            int startingY = clickedPosition.Item2 - itemHandle.Item2;

            //save where the item's bottomLeft-most tile exists on the grid
            item.SetRelativeOrigin(startingX, startingY);

            //populate each cell
            for (int i = 0; i < itemWidth; i++)
            {
                for (int j = 0; j < itemHeight; j++)
                {
                    //Debug.Log($"Setting {index} to {item.ItemData().Name()}");
                    _invItems[startingX + i, startingY + j] = item;
                }
            }

            //parent image to grid
            RectTransform itemRectTransform = item.GetComponent<RectTransform>();
            itemRectTransform.SetParent(_rectTransform);

            //get the difference btwn the mousePosition and the clicked tile's position
            Vector2 offsetTowardsCenter = GetPositionFromGridTile(clickedPosition.Item1,clickedPosition.Item2) - (Vector2)Input.mousePosition;

            //adjust the item by the offset
            item.GetComponent<RectTransform>().anchoredPosition += offsetTowardsCenter;

            itemRectTransform.localScale = Vector2.one;
        }        
    }

    public InventoryItem QueryItem(int x, int y)
    {
        if (IsCellOnGrid((x,y)))
            return _invItems[x,y];
        
        return null;
    }

    public InventoryItem TakeItem(int x, int y, out Vector2Int itemHandle)
    {
        InventoryItem querydItem = _invItems[x, y];

        //calculate the item's handle (local to itself)
        Vector2Int clickedPosition = new Vector2Int(x, y);

        itemHandle = clickedPosition - _invItems[x, y].GetOriginLocation();
        //Debug.Log($"Item Handle: {itemHandle}");
        List<(int,int)> validIndexes = new List<(int,int)> ();

        //free up all the cells this item is occupying
        for (int i =0; i < querydItem.ItemData().Width(); i++)
        {
            for (int j = 0; j < querydItem.ItemData().Height(); j++)
            {
                int xPos = querydItem.GetOriginLocation().x + i;
                int yPos = querydItem.GetOriginLocation().y + j;
                //Debug.Log($"Checking if Position {xPos},{yPos} is expected item");

                InventoryItem foundItem = QueryItem(xPos, yPos);

                //make sure the item at this position matches 
                if ( foundItem == querydItem)
                {
                    //save the index to be removed after all spaces have been checked
                    validIndexes.Add((xPos, yPos));
                    
                }
                else
                {
                    //Debug.LogError($"" +
                    //    $"Detected Item mismatch while taking item. " +
                    //    $"Expected item {querydItem.ItemData().Name()} on cell ({xPos},{yPos})," +
                    //    $" but found item {foundItem.ItemData().Name()} instead. Aborting take operation");
                    return null;
                }
            }
        }


        foreach ((int,int) index in validIndexes)
        {
            _invItems[index.Item1, index.Item2] = null;
            //Debug.Log($"Position {index.Item1},{index.Item2} Freed up");
        }
        

        return querydItem;

    }

    public float TileWidth() { return _tileSizeWidth; }
    public float TileHeight() { return _tileSizeHeight; }
    

}
