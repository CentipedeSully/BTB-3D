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
    public bool IsAreaAvailable(int width, int height, (int,int) clickedGridPosition, (int,int) itemHandle)
    {
        Debug.Log($"Area Avvailability Check: givenHandle -> {itemHandle}");
        //autofail if item too big
        if (width > _containerSize.x || height > _containerSize.y)
            return false;

        //calculate the item's expected (0,0) position on the grid
        //the itemHandle is the item's tile that's bound to the mouse
        //the desired position is where the player clicked on the grid (while holding an item)
        int startingX = clickedGridPosition.Item1 - itemHandle.Item1;
        int startingY = clickedGridPosition.Item2 - itemHandle.Item2;

        //check each cell
        for (int i = 0; i < width; i++)
        {
            for (int j= 0; j < height; j++)
            {
                Debug.Log($"Checking Cell {startingX + i},{startingY + j}");
                //check each cell that's within the area of the item
                if (!IsCellSpaceAvailable(startingX + i, startingY + j))
                {
                    (int, int) index = (startingX + i, startingY + j);
                    string reason = "";
                    if ( index.Item1 < 0 || index.Item1 >= _containerSize.x)
                    {
                        reason += $"\nx position {index.Item1} out of bounds";
                    }

                    if (index.Item2 < 0 || index.Item2 >= _containerSize.y)
                    {
                        reason += $"\ny position {index.Item1} out of bounds";
                    }
                    
                    //log outOfBounds cells detected
                    if (reason != "")
                    {
                        Debug.Log($"Cell {index} is unavailable because {reason}");
                        return false;
                    }

                    InventoryItem occupier = QueryItem(index.Item1, index.Item2);
                    reason += $"\nCell {index} already occupied by {occupier.ItemData().Name()}";
                    Debug.Log($"Cell {index} is unavailable because {reason}");
                    return false;

                }
            }
        }

        //the are appears available. All cells
        return true;
    }

    public bool IsCellSpaceAvailable(int x, int y)
    {
        //position invalid if off the grid
        if (x < 0 || x >= _containerSize.x)
            return false;
        if (y < 0 || y >= _containerSize.y)
            return false;

        //position invalid if space is occupied
        if (QueryItem(x, y) != null)
            return false;

        return true;
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

    public bool PlaceItem(InventoryItem item, (int,int)clickedPosition ,(int,int)itemHandle)
    {
        if (item == null)
            return false;

        int itemWidth = item.ItemData().Width();
        int itemHeight = item.ItemData().Height();


        if (IsAreaAvailable(itemWidth, itemHeight, clickedPosition, itemHandle))
        {
            

            //place item on each overlaping gridcell
            //calculate the item's expected (0,0) position on the grid
            //the itemHandle is the item's tile that's bound to the mouse
            //the clicked position is where the player clicked on the grid (while holding an item)
            int startingX = clickedPosition.Item1 - itemHandle.Item1;
            int startingY = clickedPosition.Item2 - itemHandle.Item2;

            //used to calculate where the object lives in the grid
            //for easier retrieval
            item.SetRelativeOrigin(startingX, startingY);

            //populate each cell
            for (int i = 0; i < itemWidth; i++)
            {
                for (int j = 0; j < itemHeight; j++)
                {
                    (int, int) index = (startingX + i, startingY + j);
                    Debug.Log($"Setting {index} to {item.ItemData().Name()}");
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

            return true;
        }

        return false;

        
    }

    public InventoryItem QueryItem(int x, int y)
    {
        
        return _invItems[x,y];
    }

    //tailor This ------ vvv  (Check ALL cells first before clearing the positions. If one cell check is bad, don't change ANY)
    public InventoryItem TakeItem(int x, int y, out Vector2Int itemHandle)
    {
        InventoryItem querydItem = _invItems[x, y];

        //calculate the item's handle (local to itself)
        Vector2Int clickedPosition = new Vector2Int(x, y);

        itemHandle = clickedPosition - _invItems[x, y].GetOriginLocation();
        Debug.Log($"Item Handle: {itemHandle}");

        //free up all the cells this item is occupying
        for (int i =0; i < querydItem.ItemData().Width(); i++)
        {
            for (int j = 0; j < querydItem.ItemData().Height(); j++)
            {
                int xPos = querydItem.GetOriginLocation().x + i;
                int yPos = querydItem.GetOriginLocation().y + j;
                Debug.Log($"Attempting to clear Position {xPos},{yPos}");

                InventoryItem foundItem = QueryItem(xPos, yPos);

                //make sure the item at this position matches 
                if ( foundItem == querydItem)
                { 
                    _invItems[xPos, yPos] = null;
                    Debug.Log($"Position {xPos},{yPos} Freed up");
                }
                else
                {
                    Debug.LogError($"Detected Item mismatch. Expected item {querydItem.ItemData().Name()}, but found item {foundItem.ItemData().Name()} instead.");
                }
            }
        }

        return querydItem;

    }

    public float TileWidth() { return _tileSizeWidth; }
    public float TileHeight() { return _tileSizeHeight; }
    

}
