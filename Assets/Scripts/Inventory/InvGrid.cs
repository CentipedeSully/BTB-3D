using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InvGrid : MonoBehaviour
{
    [SerializeField] private Vector2Int _containerSize;
    [SerializeField] private Vector2 _cellSize;
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private RectTransform _spritesContainer;
    private GridLayoutGroup _layoutGroup;
    private RectTransform _rectTransform;
    private CellInteract[,] _cellObjects;
    private Dictionary<InventoryItem, List<(int, int)>> _containedItems = new(); //used for quick referencing any item in the grid
    private Dictionary<(int, int), InventoryItem> _cellOccupancy = new(); //used to quickly check if specific cells are occupied (& what occupies them)


    //monobehaviours
    private void Awake()
    {
        //Initialize our references and utilities
        _rectTransform = GetComponent<RectTransform>();
        _layoutGroup = GetComponent<GridLayoutGroup>();
        _cellObjects = new CellInteract[_containerSize.x, _containerSize.y];
        _layoutGroup.cellSize = _cellSize;


        //Resize the UiWindow.
        Vector2 dynamicSize = new();
        dynamicSize.x = _containerSize.x * _cellSize.x + _layoutGroup.padding.right + _layoutGroup.padding.left;
        dynamicSize.y = _containerSize.y * _cellSize.y + _layoutGroup.padding.bottom + _layoutGroup.padding.top;
        _rectTransform.sizeDelta = dynamicSize;

    }

    private void Start()
    {
        //be mindful of the creation order of the cells. GridLayout configured to create them row by row.
        //(0,0) starts at the bottom, similar to the traditional cortesian coord system
        for (int y = 0; y < _containerSize.y; y++)//columns get created after rows
        {
            for (int x = 0; x < _containerSize.x; x++)//rows get created first 
            {
                GameObject newCell = Instantiate(_cellPrefab, _rectTransform);
                CellInteract cellInteract = newCell.GetComponent<CellInteract>();
                cellInteract.SetGrid(this);
                cellInteract.SetIndex((x, y));
                _cellObjects[x, y] = cellInteract;
                _cellOccupancy.Add((x, y), null);
            }
        }
    }




    //internals



    /*
    public Dictionary<(int, int), InventoryItem> GetItemsInArea(int width, int height, (int, int) clickedGridPosition, (int, int) itemHandle)
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

                if (IsCellOnGrid(indexPair))
                {
                    if (IsCellOccupied(indexPair))
                        foundOccupancy.Add(indexPair, QueryItem(indexPair.Item1, indexPair.Item2));

                }
            }
        }

        return foundOccupancy;

    }

    public (InventoryItem, Vector2Int newItemHandle) SwapItems(int width, int height, (int, int) clickedGridPosition, (int, int) itemHandle, InventoryItem newItem)
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
                (int, int) arbitraryIndex = (-1, -1);
                foreach ((int, int) key in itemsFound.Keys)
                {
                    arbitraryIndex = key;
                    break;
                }

                Vector2Int newHandle = Vector2Int.zero;
                returnedItem = TakeItem(arbitraryIndex.Item1, arbitraryIndex.Item2, out newHandle);

                //place the new item at the clickedPosition
                PlaceItem(newItem, clickedGridPosition, itemHandle);

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

    public void PlaceItem(InventoryItem item, (int, int) clickedPosition, (int, int) itemHandle)
    {
        if (item == null)
            return;

        int itemWidth = item.Width();
        int itemHeight = item.Height();

        //Debug.Log($"Clicked Position: {clickedPosition}");
        Debug.Log($"Item Handle: {itemHandle}"); 
        if (IsAreaUnoccupied(itemWidth, itemHeight, clickedPosition, itemHandle))
        {

            //calculate the item's expected (0,0) position on the grid
            int startingX = clickedPosition.Item1 - itemHandle.Item1;
            int startingY = clickedPosition.Item2 - itemHandle.Item2;

            //save where the item's bottomLeft-most tile exists on the grid
            item.SetRelativeOrigin(startingX, startingY);

            _containedItems.Add(item, new());

            //populate each cell
            for (int i = 0; i < itemWidth; i++)
            {
                for (int j = 0; j < itemHeight; j++)
                {
                    int gridPosX = startingX + i;
                    int gridPosY = startingY + j;

                    _containedItems[item].Add((gridPosX, gridPosY));

                    //Debug.Log($"Setting {startingX + i},{startingY + j} to {item.ItemData().Name()}");
                    _items[gridPosX,gridPosY] = item;
                }
            }

            //parent image to the sprites Container
            RectTransform itemRectTransform = item.GetComponent<RectTransform>();
            itemRectTransform.SetParent(_spritesContainer);

            //offset the image to its origin
            itemRectTransform.localPosition = _cellObjects[startingX,startingY].GetComponent<RectTransform>().localPosition; //sprite currently centered on position

            Vector3 toBottomLeftTileCornerOffset = new();
            toBottomLeftTileCornerOffset.x = itemWidth * CellSize().x/ 2 - CellSize().x / 2;
            toBottomLeftTileCornerOffset.y = itemHeight * CellSize().y/ 2 - CellSize().y / 2;

            itemRectTransform.localPosition += toBottomLeftTileCornerOffset ;

            itemRectTransform.localScale = Vector2.one;
        }
    }

    public InventoryItem TakeItem(int x, int y, out Vector2Int itemHandle)
    {
        InventoryItem querydItem = _items[x, y];

        //calculate the item's handle (local to itself)
        Vector2Int clickedPosition = new Vector2Int(x, y);

        itemHandle = clickedPosition - _items[x, y].GetOriginLocation();
        Debug.Log($"Item Handle: {itemHandle}");
        List<(int, int)> validIndexes = new List<(int, int)>();

        //free up all the cells this item is occupying
        for (int i = 0; i < querydItem.Width(); i++)
        {
            for (int j = 0; j < querydItem.Height(); j++)
            {
                int xPos = querydItem.GetOriginLocation().x + i;
                int yPos = querydItem.GetOriginLocation().y + j;
                //Debug.Log($"Checking if Position {xPos},{yPos} is expected item");

                InventoryItem foundItem = QueryItem(xPos, yPos);

                //make sure the item at this position matches 
                if (foundItem == querydItem)
                {
                    //save the index to be removed after all spaces have been checked
                    validIndexes.Add((xPos, yPos));

                }
                else
                {
                    Debug.LogError($"" +
                        $"Detected Item mismatch while taking item. " +
                        $"Expected item {querydItem.ItemData().Name()} on cell ({xPos},{yPos})," +
                        $" but found item {foundItem.ItemData().Name()} instead. Aborting take operation");
                    return null;
                }
            }
        }


        foreach ((int, int) index in validIndexes)
        {
            _items[index.Item1, index.Item2] = null;
            //Debug.Log($"Position {index.Item1},{index.Item2} Freed up");
        }

        _containedItems.Remove(querydItem);

        return querydItem;

    }
    */



    //externals
    public Vector2 CellSize() { return _cellSize; }
    public Vector2Int ContainerSize() {  return _containerSize; }
    public bool IsCellOnGrid((int, int) cell)
    {
        if (cell.Item1 < 0 || cell.Item1 >= _containerSize.x || cell.Item2 < 0 || cell.Item2 >= _containerSize.y)
            return false;
        return true;
    }
    public bool IsCellOccupied((int, int) cell)
    {
        return _cellOccupancy[cell] != null;
    }
    public CellInteract GetCellObject((int, int) index)
    {
        if (!IsCellOnGrid(index))
            return null;

        return _cellObjects[index.Item1, index.Item2];

    }
    public InventoryItem GetItemOnCell((int, int) index)
    {
        if (IsCellOnGrid(index))
            return _cellOccupancy[index];

        return null;
    }
    public InventoryItem GetItemOnCell(int x, int y)
    {
        return GetItemOnCell((x, y));
    }
    public List<(int,int)> GetItemOccupancy( InventoryItem item)
    {
        if (item == null)
            return null;

        if (_containedItems.ContainsKey(item))
            return _containedItems[item];

        else return null;
    }

    /// <summary>
    /// Converts a series of indexes into gridPositions based on how the item is manually placed into the grid.
    /// Does not check if the returned grid positions are actually on the grid.
    /// </summary>
    /// <param name="selectedGridPosition">The literal clicked position on the grid</param>
    /// <param name="spacialDefinition">The objects size defined as indexes</param>
    /// <param name="itemHandle">The index within the provided spacial definition that should align with the selected grid position</param>
    /// <returns></returns>
    public List<(int, int)> ConvertSpacialDefIntoGridIndexes((int, int) selectedGridPosition, List<(int, int)> spacialDefinition, (int, int) itemHandle)
    {
        if (spacialDefinition == null)
            return null;

        if (spacialDefinition.Count < 1)
            return null;

        List<(int, int)> gridPositions = new();

        //convert the provided spacialDefinition into gridPositions.
        foreach ((int, int) index in spacialDefinition)
        {
            // grid index = selectedGridPosition + (currentIndex - itemHandle)       
            int gridX = selectedGridPosition.Item1 + (index.Item1 - itemHandle.Item1);
            int gridY = selectedGridPosition.Item2 + (index.Item2 - itemHandle.Item2);

            gridPositions.Add((gridX, gridY));

        }

        return gridPositions;
    }
    public bool IsPlacementValid(List<(int,int)> gridPositions)
    {
        if (gridPositions == null)
            return false;
        if (gridPositions.Count < 1)
            return false;

        bool areAllPositionsValid = true;
        string debugString = "Placement Check Results:\n";
        foreach ((int,int) index in gridPositions)
        {
            if (IsCellOnGrid(index))
            {
                if (IsCellOccupied(index))
                {
                    debugString += $"Position ({index.Item1},{index.Item2}) Invalid. Occupied by item '{_cellOccupancy[index].name}'\n";
                    areAllPositionsValid = false;
                }
            }
            else
            {
                debugString += $"Position ({index.Item1},{index.Item2}) Invalid. Not on grid\n";
                areAllPositionsValid = false;
            }
        }

        return areAllPositionsValid;

    }

    public void RemoveItem(InventoryItem specifiedItem)
    {
        if (specifiedItem == null)
            return;

        if (_containedItems.ContainsKey(specifiedItem))
        {
            string debugString = $"Cells Cleared:\n";
            //clear each cell that this item references 
            foreach ((int, int) index in _containedItems[specifiedItem])
            {
                _cellOccupancy[index] = null;
                debugString += $"({index.Item1},{index.Item2})\n";
            }

            Debug.Log(debugString);
            _containedItems.Remove(specifiedItem);
        }
    }
    public void PlaceItemWithinGrid(InventoryItem item, List<(int, int)> gridPositions)
    {
        if (item == null)
        {
            Debug.LogWarning("Attempted to place a null item into the grid; Ignoring request");
            return;
        }


        //make sure the position is valid and the item isn't already in the grid
        if (IsPlacementValid(gridPositions) && !_containedItems.ContainsKey(item))
        {
            List<(int, int)> itemIndexes = new();

            //update our cellOccupancy references
            foreach ((int, int) index in gridPositions)
            {
                _cellOccupancy[index] = item;

                itemIndexes.Add(index); //also creating a copy of the gridPositions collection 
            }

            //add the item to our container registry, along
            //with the newly created copy of the grid positions list
            _containedItems.Add(item, itemIndexes);

            return;
        }

        else if (_containedItems.ContainsKey(item))
            Debug.LogWarning($"Item '{item}' Already exists within the grid. Ignoring request");

        else
        {
            Debug.LogWarning("Placement position is invalid. If no previous data was given, then the" +
                " placement space in question didn't provide any positions to check (but also wasn't null). " +
                " This happens when providing a spacial definition (or item size) of zero size");
        }
    }


}
