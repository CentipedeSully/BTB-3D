using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;


namespace dtsInventory
{
    public class InvGrid : MonoBehaviour
    {
        [SerializeField] private Vector2Int _containerSize;
        [SerializeField] private Vector2 _cellSize;
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private RectTransform _spritesContainer;
        [SerializeField] private InvWindow _parentWindow;
        [SerializeField] private RectTransform _stackTextUiPrefab;
        [SerializeField] private RectTransform _activeStackTextsContainer;
        [SerializeField] private RectTransform _unusedStackTextsContainer;
        [SerializeField] private RectTransform _overlayContainer;

        [SerializeField] GridLayoutGroup _layoutGroup;
        private RectTransform _rectTransform;
        private CellInteract[,] _cellObjects;
        //private Dictionary<InventoryItem, List<(int, int)>> _containedItems = new(); //used for quick referencing any item in the grid
        //private Dictionary<(int, int), InventoryItem> _cellOccupancy = new(); //used to quickly check if specific cells are occupied (& what occupies them)

        /// <summary>
        /// The current size of each stack. The keys are the stack's occupied gridPositions.
        /// </summary>
        private Dictionary<HashSet<(int, int)>, int> _stackCapacities = new();
        /// <summary>
        /// The itemData values that belong to each stack. The keys are the stack's occupied gridPositions.
        /// </summary>
        private Dictionary<HashSet<(int, int)>, ItemData> _stackItemDatas = new();
        /// <summary>
        /// The item graphic that visually defines the stack in the inventory window. The keys are the stack's occupied gridPositions.
        /// </summary>
        private Dictionary<HashSet<(int, int)>, InvItem> _stackSpriteObjects = new();
        private Dictionary<HashSet<(int, int)>, Text> _stackTexts = new();

        private struct ItemPlacementData
        {
            public (int, int) gridPlacementPosition;
            public ItemRotation necessaryRotation;

            public ItemPlacementData((int,int) gridPosition, ItemRotation itemRotation)
            {
                gridPlacementPosition = gridPosition;
                necessaryRotation = itemRotation;
            }
        }


        //monobehaviours
        private void Awake()
        {
            //Initialize our references and utilities
            _rectTransform = GetComponent<RectTransform>();
            _layoutGroup = GetComponent<GridLayoutGroup>();
            _cellObjects = new CellInteract[_containerSize.x, _containerSize.y];
            _layoutGroup.cellSize = _cellSize;
            _unusedStackTextsContainer.gameObject.SetActive(false);

            //Resize the UiWindow.
            Vector2 dynamicSize = new();
            dynamicSize.x = _containerSize.x * _cellSize.x + _layoutGroup.padding.right + _layoutGroup.padding.left;
            dynamicSize.y = _containerSize.y * _cellSize.y + _layoutGroup.padding.bottom + _layoutGroup.padding.top;
            _rectTransform.sizeDelta = dynamicSize;
            _spritesContainer.sizeDelta = dynamicSize;
            _activeStackTextsContainer.sizeDelta = dynamicSize;
            _overlayContainer.sizeDelta = dynamicSize;

        }

        private void Start()
        {
            InitializeGrid();
        }




        //internals
        private void InitializeGrid()
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
                }
            }
        }
        private void SeparateItemFromGridGraphically(InvItem item)
        {
            Transform unusedItemsContainer = ItemCreatorHelper.GetUiItemsContainer();

            item.GetComponent<RectTransform>().SetParent(unusedItemsContainer, false);
        }

        /// <summary>
        /// Returns all positions that correspond to single stack of items.
        /// The returned indexes together form a key that links to either an itemCode 
        /// or an integer (the number of items in the stack). If nothing is returned
        /// then the position holds no stack of items. Never returns null.
        /// </summary>
        /// <param name="position">The grid position to check.
        /// Any Grid position may belong to only one item stack at a time.</param>
        /// <returns></returns>
        private HashSet<(int, int)> GetStackArea((int, int) position)
        {
            //look at all the saved stack positionSets
            foreach (HashSet<(int, int)> indexSet in _stackItemDatas.Keys)
            {
                if (indexSet.Contains(position))
                    return indexSet;
            }

            //return an empty dataCollection if the position doesn't exist among our saved stacks
            return new();

        }
        private void PositionItemGraphicOntoGridVisually((int, int) index, InvItem item)
        {
            //reparent the item onto the grid visually
            //Get the position of the hovered cell, local to the grid
            Vector3 parentCellPosition = GetCellObject(index).GetComponent<RectTransform>().localPosition;

            RectTransform itemRectTransform = item.GetComponent<RectTransform>();

            //parent the item to the grid's sprite container
            itemRectTransform.SetParent(_spritesContainer, false);
            itemRectTransform.localPosition = parentCellPosition;

            //ensure the sprite is of the appropriate size
            itemRectTransform.sizeDelta = new Vector2(item.Width() * _cellSize.x, item.Height() * _cellSize.y);

            itemRectTransform.gameObject.SetActive(true);

            //ensure the stackText is positioned appropriately
        }

        private void ToggleStackTextViaCurrentAmount(RectTransform uiText, int stackSize)
        {
            if (stackSize <= 1)
                uiText.gameObject.SetActive(false);

            else uiText.gameObject.SetActive(true);
        }
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
        public InvWindow GetParentWindow() { return _parentWindow; }
        public Vector2 CellSize() { return _cellSize; }
        public Vector2Int ContainerSize() { return _containerSize; }
        public bool IsCellOnGrid((int, int) cell)
        {
            if (cell.Item1 < 0 || cell.Item1 >= _containerSize.x || cell.Item2 < 0 || cell.Item2 >= _containerSize.y)
                return false;
            return true;
        }
        public bool IsCellOccupied((int, int) position)
        {
            //Debug.Log($"Checking 'IsCellOccupied Integrity. Provided Position: ({position.Item1},{position.Item2})\nFound Stack Area at position: " + GetStackArea(position).ToCommaSeparatedString());
            //Debug.Log($"Is Cell Occupied: {GetStackArea(position).Count > 0}");
            if (GetStackArea(position).Count > 0)
                return true;
            else return false;
        }
        public bool IsCellOccupied(int x, int y)
        {
            return IsCellOccupied((x, y));
        }
        public bool DoesItemGraphicAlreadyExistOnGrid(InvItem itemGraphic)
        {
            if (itemGraphic == null)
                return false;

            foreach (InvItem itemReference in _stackSpriteObjects.Values)
            {
                if (itemReference == itemGraphic)
                    return true;
            }

            return false;
        }
        public CellInteract GetCellObject((int, int) index)
        {
            if (!IsCellOnGrid(index))
                return null;

            return _cellObjects[index.Item1, index.Item2];

        }
        public ItemData GetStackItemData((int, int) index)
        {
            if (IsCellOnGrid(index))
                if (_stackItemDatas.ContainsKey(GetStackArea(index)))
                    return _stackItemDatas[GetStackArea(index)];

            return null;
        }
        public ItemData GetStackItemData(int x, int y)
        {
            return GetStackItemData((x, y));
        }
        public InvItem GetItemGraphicOnCell((int, int) index)
        {
            if (IsCellOnGrid(index))
            {
                if (_stackSpriteObjects.ContainsKey(GetStackArea(index)))
                    return _stackSpriteObjects[GetStackArea(index)];
            }

            return null;
        }
        public InvItem GetItemGraphicOnCell(int x, int y)
        {
            return GetItemGraphicOnCell((x, y));
        }
        public int GetStackValue((int, int) position)
        {
            HashSet<(int, int)> stackPosition = GetStackArea(position);
            if (stackPosition.Count > 0)
                return _stackCapacities[stackPosition];

            return 0;
        }
        public int GetStackValue(int x, int y)
        {
            return GetStackValue((x, y));
        }
        /// <summary>
        /// Returns all of the cells that the stack at the provided position is occupying.
        /// Returns an empty collection if no stack exists at the provided position.
        /// Nonexistent grid positions will also return an empty collection.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public HashSet<(int, int)> GetStackOccupancy((int, int) position)
        {
            return GetStackArea(position);
        }
        /// <summary>
        /// Returns all of the cells that the stack at the provided position is occupying.
        /// Returns an empty collection if no stack exists at the provided position.
        /// Nonexistent grid positions will also return an empty collection.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public HashSet<(int, int)> GetStackOccupancy(int x, int y)
        {
            return GetStackArea((x, y));
        }

        /// <summary>
        /// Converts a series of indexes into gridPositions based on how the item is manually placed into the grid.
        /// Does not check if the returned grid positions are actually on the grid.
        /// </summary>
        /// <param name="selectedGridPosition">The literal clicked position on the grid</param>
        /// <param name="spacialDefinition">The objects size defined as indexes</param>
        /// <param name="itemHandle">The index within the provided spacial definition that should align with the selected grid position</param>
        /// <returns></returns>
        public HashSet<(int, int)> ConvertSpacialDefIntoGridIndexes((int, int) selectedGridPosition, HashSet<(int, int)> spacialDefinition, (int, int) itemHandle)
        {
            if (spacialDefinition == null)
                return null;

            if (spacialDefinition.Count < 1)
                return null;

            HashSet<(int, int)> gridPositions = new();

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
        public int CountUniqueStacksInArea(HashSet<(int, int)> gridPositions)
        {
            if (gridPositions == null)
            {
                //Debug.Log("No gridPositions were given to check for Uniqueness. Returning 0, since none we technically found");
                return 0;
            }


            //Save each found stack definition into a set. For convenient uniqueness checking
            HashSet<HashSet<(int, int)>> uniqueStacks = new();
            string positionSets = "";
            string position = "";
            foreach ((int, int) index in gridPositions)
            {
                HashSet<(int, int)> stackArea = GetStackArea(index);
                if (stackArea.Count > 0)
                    uniqueStacks.Add(stackArea);

                position = "";

                foreach ((int, int) cell in stackArea)
                    position += $"({cell.Item1},{cell.Item2}), ";

                positionSets += $"{position}\n";

            }

            //Debug.Log($"Unique Stacks Detected: {uniqueStacks.Count}\nBreakdown:{positionSets}");
            return uniqueStacks.Count;

        }
        public bool IsAreaWithinGrid(HashSet<(int, int)> gridPositions)
        {
            if (gridPositions == null)
                return false;

            if (gridPositions.Count == 0)
                return false;

            foreach ((int, int) index in gridPositions)
            {
                if (!IsCellOnGrid(index))
                    return false;
            }

            return true;
        }
        public void IncreaseStack((int, int) position, int increment)
        {
            HashSet<(int, int)> stackArea = GetStackArea(position);
            if (stackArea.Count <= 0)
                return;

            //get the maximum stack value
            int maxCapacity = _stackItemDatas[stackArea].StackLimit();

            //make sure we don't overshoot the stack's limit
            _stackCapacities[stackArea] = Mathf.Min(_stackCapacities[stackArea] + increment, maxCapacity);

            //update the stack's text
            _stackTexts[stackArea].text = $"{_stackCapacities[stackArea]}";

            //hide or toggle the stack text based on the new amount
            ToggleStackTextViaCurrentAmount(_stackTexts[stackArea].GetComponent<RectTransform>(), _stackCapacities[stackArea]);
        }
        public void DecreaseStack((int, int) position, int decrement)
        {
            HashSet<(int, int)> stackArea = GetStackArea(position);
            if (stackArea.Count <= 0)
                return;

            _stackCapacities[stackArea] -= decrement;
            _stackTexts[stackArea].text = $"{_stackCapacities[stackArea]}";

            int newCapacity = _stackCapacities[stackArea];

            //show or hide the text depending on the stacksize
            ToggleStackTextViaCurrentAmount(_stackTexts[stackArea].GetComponent<RectTransform>(), newCapacity);

            //delete the stack if we've expended all the items
            if (newCapacity <= 0)
                DeleteStack(position);
        }
        public void DeleteStack((int, int) position)
        {
            HashSet<(int, int)> stackArea = GetStackArea(position);
            if (stackArea.Count <= 0)
                return;

            _stackCapacities.Remove(stackArea);
            _stackItemDatas.Remove(stackArea);
            InvItem itemGraphic = _stackSpriteObjects[stackArea];
            _stackSpriteObjects.Remove(stackArea);
            Text uiText = _stackTexts[stackArea];
            _stackTexts.Remove(stackArea);

            uiText.GetComponent<RectTransform>().SetParent(_unusedStackTextsContainer, false);
            ItemCreatorHelper.ReturnItemToCreator(itemGraphic);
        }
        public void CreateStack((int, int) position, InvItem item, int amount) //only items have the necessary rotation data to fit within a grid
        {
            if (item == null)
            {
                Debug.LogWarning($"Attempted to create a new item stack using a Null item. Ignoring request.");
                return;
            }

            if (DoesItemGraphicAlreadyExistOnGrid(item))
            {
                Debug.LogWarning($"Attempted to create a new item stack using an item graphic thats currently in use by another item stack ({item.name}). Ignoring request.");
                return;
            }

            if (!IsCellOnGrid(position))
            {
                Debug.LogWarning($"Attempted to create a new item stack ({item.name}) " +
                    $"onto an invalid grid position '({position.Item1},{position.Item2})'. Ignoring request.");
                return;
            }

            //calculate the item's expectedGridPosition
            HashSet<(int, int)> expectedGridOccupancy = ConvertSpacialDefIntoGridIndexes(position, item.GetSpacialDefinition(), item.ItemHandle());

            //check if all of the positions are within the grid
            if (!IsAreaWithinGrid(expectedGridOccupancy))
            {
                Debug.LogWarning($"Attempted to create a new item stack ({item.name}) to position ({position.Item1},{position.Item2}), " +
                    $"but item won't fit based on the item's spacial definition + itemHandle Combination. Ignoring request.\n" +
                    $"Requested Grid Occupancy:{StringifyPositions(expectedGridOccupancy)}");
                return;
            }


            string overlappedPositions = "";
            bool isAreaOccupied = false;
            //check if all expected positions are available
            foreach ((int, int) index in expectedGridOccupancy)
            {
                if (IsCellOccupied(index))
                {
                    isAreaOccupied = true;
                    overlappedPositions += $"({index.Item1},{index.Item2}): occupied by '{GetStackItemData(index).name}'\n";

                }

                if (isAreaOccupied)
                {
                    Debug.LogWarning($"Attempted to create a new item stack ({item.name}) onto position ({position.Item1},{position.Item2}), " +
                    $"but the item stack's placement overlaps other stacks. Ignoring request.\nDetected overlaps:\n{overlappedPositions}" +
                    $"\nRequested Positions:{StringifyPositions(expectedGridOccupancy)}");
                    return;
                }
            }

            int stackAmount = Mathf.Clamp(amount, 1, item.ItemData().StackLimit());

            //create a textUi to represent the item's stacksize
            RectTransform uiTextTransform = null;

            //either create a new text object, or reuse a discarded one as the new text object 
            if (_unusedStackTextsContainer.childCount == 0)
                uiTextTransform = Instantiate(_stackTextUiPrefab, _activeStackTextsContainer);
            else
            {
                uiTextTransform = _unusedStackTextsContainer.GetChild(0).GetComponent<RectTransform>();
                uiTextTransform.SetParent(_activeStackTextsContainer, false);
            }

            //set the text to match the stack's new value
            Text uiText = uiTextTransform.GetComponent<Text>();
            uiText.text = $"{stackAmount}";

            //position the text to the lowest, rightmost cell positions
            PositionUiTextOntoStack(uiTextTransform, expectedGridOccupancy);

            //show or hide the text depending on the stacksize
            ToggleStackTextViaCurrentAmount(uiTextTransform, stackAmount);


            //everything is good! create the stack (clamp the amount to legit values)
            _stackItemDatas.Add(expectedGridOccupancy, item.ItemData());
            _stackCapacities.Add(expectedGridOccupancy, stackAmount);
            _stackSpriteObjects.Add(expectedGridOccupancy, item);
            _stackTexts.Add(expectedGridOccupancy, uiText);

            PositionItemGraphicOntoGridVisually(position, item);

        }



        public void RemoveItem((int, int) position, int amount)
        {
            DecreaseStack(position, amount);
        }

        /// <summary>
        /// Removes the requested amount of items from the inventory. If not enough exist, then the command is ignored.
        /// </summary>
        /// <param name="itemCode"></param>
        /// <param name="amount"></param>
        public void RemoveItem(string itemCode, int amount)
        {
            int remainder = amount;
            int found = 0;
            Dictionary<HashSet<(int, int)>, int> _foundAmounts = new();

            //check each itemStack's itemData for a matching itemCode
            foreach (KeyValuePair<HashSet<(int, int)>, ItemData> entry in _stackItemDatas)
            {
                if (entry.Value.ItemCode().ToLower() == itemCode.ToLower())
                {
                    //if our amount total was found, remove them all
                    if (_stackCapacities[entry.Key] >= remainder)
                    {
                        //remove any remainder amount from this stack first
                        RemoveItem(entry.Key.First(), remainder);

                        //then remove all the recorded amounts from the previous stacks
                        foreach (KeyValuePair<HashSet<(int,int)>,int> stack in _foundAmounts)
                            RemoveItem(stack.Key.First(), stack.Value);

                        return;
                    }

                    //else, save the current stack, reduce the amount by the found stack's capacity, and continue looking for the remainder
                    else
                    {
                        found += _stackCapacities[entry.Key];
                        _foundAmounts[entry.Key] = _stackCapacities[entry.Key];
                        remainder -= _stackCapacities[entry.Key];
                    }
                }
            }

            //if we got down here, then we didn't find the full amount of items to fulfill the request. Raise a yellow alert. The User probably didn't check the item count beforehand.
            Debug.LogWarning($"Failed to find {amount} items of itemCode [{itemCode}]. Only found {found} of {amount} items.");
            return;
        }
        public void RemoveItem(ItemData itemData, int amount)
        {
            RemoveItem(itemData.ItemCode(), amount);
        }

        /// <summary>
        /// Attempts to find an open position to place an new specified item.
        /// Searches for an available stack to fill before creating a new stack within the grid.
        /// </summary>
        /// <param name="itemData"></param>
        public void AddItem(ItemData itemData, int amount)
        {
            //make sure the item is valid
            if (itemData == null)
            {
                Debug.LogWarning("Attempted to add a Null itemData to the grid. Ignoring request.");
                return;
            }

            if (amount <= 0)
            {
                Debug.LogWarning($"Attempted to add a 0 or fewer [{itemData.name}](s) to the grid. Ignoring request.");
                return;
            }

            //Before adding anything, check the invGrid's capacity.

            //create the utilities that'll track all checked spaces
            int totalSpacesFound = 0;

            
            Dictionary<HashSet<(int, int)>, int> availableStacks = new(); // Key:Value -> StackPositions:RemainingValue

            //first, find preexisting stacks that aren't yet full
            foreach (KeyValuePair<HashSet<(int,int)>,ItemData> stack in _stackItemDatas)
            {
                //look for each stack that BOTH 1) matches our itemCode AND 2) isn't yet full
                if (stack.Value.ItemCode() == itemData.ItemCode() && _stackCapacities[stack.Key] < itemData.StackLimit())
                {
                    int foundSpace = itemData.StackLimit() - _stackCapacities[stack.Key];

                    //just track this stack's remaining capacity
                    availableStacks[stack.Key] = foundSpace;

                    totalSpacesFound += foundSpace;

                    //break if we don't need to keep searching for space
                    if (totalSpacesFound >= amount)
                        break;
                }
            }

            int remainingAmount = amount;

            //if we found enough vacancies among unfinished stacks, then add the requested amount and return
            if (totalSpacesFound >= amount)
            {
                
                foreach (KeyValuePair<HashSet<(int, int)>, int> stack in availableStacks)
                {
                    //place either the remainingAmount, or the stack's remaining capacity. Whichever is smallest
                    int placement = Mathf.Min(remainingAmount, stack.Value);

                    //place either the remaining items, or the stacks remaining capacity
                    IncreaseStack(stack.Key.First(), placement);

                    //update the remainder. 
                    remainingAmount -= placement;

                    //we've Added the requested items into preexisting stacks
                    if (remainingAmount == 0)
                        return;

                }

                //Logically, this part of the code shouldnt ever be reached:
                //We've confirmed that we have enough space in the preexisting stacks.
                //Reaching here means we've failed to add enough items DESPITE having enough space.
                //The previous looping mechanism should be revisited. Make sure we're counting our actions properly.
                Debug.LogWarning($"Counting anomoly during command [Add {amount} {itemData.name}]. " +
                    $"Failed to find a home for {remainingAmount} {itemData.name}(s), despite having enough space within preexisting stacks." +
                    $"\nDiscarding the remaining {remainingAmount} items. Please double check the code's counting");
                return;
            }




            //otherwise, we need to find more space.

            //save the individual positions to build the reservation list
            HashSet<(int, int)> reservedPositions = new();

            //save exactly how each stack should be organized 
            Dictionary<HashSet<(int,int)>,ItemPlacementData> reservedStacks = new();

            int autoBreakCount = 1000;// if the while runs 1000 times, cut it off.
            int iteractionCount = 0;

            //keep finding space for more stacks if we haven't found enough spots [with and autoBrake for added security]
            while (totalSpacesFound < amount && iteractionCount < autoBreakCount)
            {
                
                //check for the next available space.
                HashSet<(int, int)> openAreaForNewStack = FindSpaceForStack(itemData,out (int,int) gridPlacementPosition,out ItemRotation necessaryRotation,reservedPositions);

                //if no positions were found, then we've run out of space [but still require more].
                //Deny the request to add the specified items.
                if (openAreaForNewStack.Count == 0)
                {
                    Debug.LogWarning($"Failed to find enough space for {amount} {itemData.name}(s). Ignoring request.");
                    return;
                }

                //otherwise, save our findings
                //track the stack's exact placement data
                reservedStacks[openAreaForNewStack] = new ItemPlacementData(gridPlacementPosition, necessaryRotation);
                
                //mark the positions as reserved
                foreach ((int,int) position in openAreaForNewStack)
                    reservedPositions.Add(position);

                //update our amount of spaces found by the item's max stack limit
                totalSpacesFound += itemData.StackLimit();

                //track how many iterations are passing
                iteractionCount++;

            }

            if (iteractionCount >= autoBreakCount)
            {
                Debug.LogWarning($"Cancelled the command to add {amount} {itemData.name}(s) due to not finding enough space within a reasonable amount of iterations [{autoBreakCount}]. Ignoring request.");
                return;
            }


            //NOW! AFTER ALL THAT WORK!
            //
            //WE FILL THE INVENTORY


            //top off all the preexisting stacks
            foreach (KeyValuePair<HashSet<(int,int)>,int> entry in availableStacks)
            {
                IncreaseStack(entry.Key.First(), entry.Value);
                remainingAmount -= entry.Value;
            }


            //now for each reserved stack, create the item and add it to the inventory
            foreach (KeyValuePair<HashSet<(int,int)>,ItemPlacementData> entry in reservedStacks)
            {
                //create the new Item
                InvItem newItem = ItemCreatorHelper.CreateItem(itemData, _cellSize.x, _cellSize.y).GetComponent<InvItem>();

                //rotate the item to the saved rotation
                switch (entry.Value.necessaryRotation)
                {
                    case ItemRotation.None:
                        break;
                    case ItemRotation.Once:
                        newItem.RotateItem(RotationDirection.Clockwise);
                        break;
                    case ItemRotation.Twice:
                        newItem.RotateItem(RotationDirection.Clockwise);
                        newItem.RotateItem(RotationDirection.Clockwise);
                        break;
                    case ItemRotation.Thrice:
                        newItem.RotateItem(RotationDirection.CounterClockwise);
                        break;
                }

                int placementAmount = Mathf.Min(remainingAmount, itemData.StackLimit());

                //create a new stack using the newly-created, rotated item.
                //Stack size should be the smallest of either the stackLimit OR the remaining amount to place
                CreateStack(entry.Value.gridPlacementPosition, newItem, placementAmount);
                remainingAmount -= placementAmount;
            }

            //We're Done!

            /* Old, incomplete solution
            int itemsAdded = 0;
            bool inventoryFull = false;
            bool stackIncrementPerformed = false;
            int totalIterations = 0; //just in case a failure occurs while attempting to creating a stack, break the while to prevent program 'ascension'
            while (itemsAdded < amount && !inventoryFull && totalIterations < amount)
            {
                totalIterations++;
                stackIncrementPerformed = false;
                //Try to increase a compatible stack that isnt full yet
                foreach (KeyValuePair<HashSet<(int, int)>, ItemData> entry in _stackItemDatas)
                {
                    //if the item is similar
                    if (itemData == entry.Value)
                    {
                        //if the stack isn't full yet
                        if (_stackCapacity[entry.Key] < itemData.StackLimit())
                        {
                            //increment the amount of items in this stack
                            IncreaseStack(entry.Key.First<(int, int)>(), 1);
                            stackIncrementPerformed = true;
                            break;
                        }
                    }
                }

                if (stackIncrementPerformed)
                {
                    itemsAdded++;
                    continue;
                }

                //no open stacks were found. Now try to create a new stack.
                InvItem newItem = ItemCreatorHelper.CreateItem(itemData, _cellSize.x, _cellSize.y).GetComponent<InvItem>();

                (int, int) openPosition = (-1, -1);
                ItemRotation necessaryRotation = ItemRotation.None;
                HashSet<(int, int)> openArea = FindSpaceForItem(newItem, out openPosition, out necessaryRotation);
                //Debug.Log($"FoundPositions: {openArea.ToCommaSeparatedString()}");

                if (openArea.Count == 0)
                {
                    inventoryFull = true;
                    Debug.LogWarning($"No open position was found for item. Inventory must be full, or the item may be too big.\nLast Index checked: {openPosition}");
                    return;
                }

                while (newItem.Rotation() != necessaryRotation)
                    newItem.RotateItem(RotationDirection.Clockwise);

                CreateStack(openPosition, newItem, 1);
                itemsAdded++;
            }
            */

        }

        

        public HashSet<(int, int)> FindSpaceForItem(InvItem item, out (int, int) gridPosition, out ItemRotation necessaryRotation)
        {
            //Default the out parameters to invalid states
            gridPosition = (-1, -1);
            necessaryRotation = ItemRotation.None;

            if (item == null)
                return new();
            if (item.GetSpacialDefinition() == null)
                return new();
            if (item.GetSpacialDefinition().Count < 1)
                return new();

            ItemRotation originalRotation = item.Rotation();
            HashSet<(int, int)> calculatedPositions = new();
            int width = _containerSize.x;
            int height = _containerSize.y;
            int rotationCount = 0;

            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {

                    //save time and skip cells that are directly occupied
                    if (IsCellOccupied((w, h)))
                    {
                        //Debug.Log($"Tracing 'FindSpaceForItem':\n Cell iteration: ({w},{h})\nStatus: SKIPPED [CELL OCCUPIED]");
                        continue;
                    }


                    //check if the item fits with its origin (itemHandle) centered on this cell
                    //check all rotations
                    while (rotationCount < 4)
                    {
                        calculatedPositions = ConvertSpacialDefIntoGridIndexes((w, h), item.GetSpacialDefinition(), item.ItemHandle());


                        if (CountUniqueStacksInArea(calculatedPositions) == 0 && IsAreaWithinGrid(calculatedPositions))
                        {

                            necessaryRotation = item.Rotation();
                            gridPosition = (w, h);

                            //revert the item's rotation. This function isn't meant to be changing anything beyond the out arguments.
                            while (item.Rotation() != originalRotation) //only 4 exist in a cycle
                                item.RotateItem(RotationDirection.Clockwise);

                            //Debug.Log($"Tracing 'FindSpaceForItem':\n Cell iteration: ({w},{h})\nStatus: success! Returning '{calculatedPositions.Count()}' calculatedPositions.\n DesiredRotation: {necessaryRotation}");
                            return calculatedPositions;
                        }


                        item.RotateItem(RotationDirection.Clockwise);
                        rotationCount++;
                    }

                    //Debug.Log($"Tracing 'FindSpaceForItem':\n Cell iteration: ({w},{h})\nStatus: NO SPACE FOUND");

                    //none found. Reset the rotationCount and move on to the next cell
                    rotationCount = 0;
                }
            }

            //None were found.
            //Debug.Log($"Tracing 'FindSpaceForItem':\n No Positions Found. Returning an empty collection...");
            return new();
        }
        public HashSet<(int,int)> FindSpaceForStack(ItemData itemData, out (int, int) gridPosition, out ItemRotation necessaryRotation, HashSet<(int,int)> excludedPositions = default)
        {
            gridPosition = (-1, -1);
            necessaryRotation = ItemRotation.None;

            //this parameter is optional. But make sure its not null
            if (excludedPositions == null)
                excludedPositions = new();

            if (itemData == null)
            {
                Debug.LogWarning("Attempted to find space for a stack with a NULL itemData. Returning an empty collection");
                return new();
            }

            
            //setup the iteration utilities
            int width = _containerSize.x;
            int height = _containerSize.y;
            int rotationCount = 0;

            HashSet<(int, int)> calculatedPositions = new();

            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    //Debug.Log($"Starting Iteration ({w},{h})");
                    //skip cells that're either directly occupied or are explicitly excluded
                    if (IsCellOccupied((w, h)) || excludedPositions.Contains((w,h)))
                    {
                        /*Log Skipped Iterations
                        if (IsCellOccupied((w, h)))
                            Debug.Log($"Tracing 'FindSpaceForStack':\n Cell iteration: ({w},{h})\nStatus: SKIPPED [CELL OCCUPIED]");
                        else 
                            Debug.Log($"Tracing 'FindSpaceForStack':\n Cell iteration: ({w},{h})\nStatus: SKIPPED [CELL MARKED AS EXCLUDED]");
                        */
                        continue;
                    }


                    //check if an itemData fits with its origin (itemHandle) centered on this cell
                    //check all rotations
                    while (rotationCount < 4)
                    {
                        
                        switch (rotationCount)
                        {
                            case 0:
                                necessaryRotation = ItemRotation.None;
                                break;
                            case 1:
                                necessaryRotation = ItemRotation.Once;
                                break;
                            case 2:
                                necessaryRotation = ItemRotation.Twice;
                                break;
                            case 3:
                                necessaryRotation = ItemRotation.Thrice;
                                break;
                        }

                        /*
                        Debug.Log($"Checking Rotation: {necessaryRotation.ToString()}\n" +
                            $"Rotated SpacialDef: {StringifyPositions(itemData.RotatedSpacialDef(necessaryRotation))}\n" +
                            $"Rotated ItemHandle: ({itemData.RotatedItemHandle(necessaryRotation).Item1},{itemData.RotatedItemHandle(necessaryRotation).Item2})");
                        */

                        //calculate the items expected ROTATED spacialData [without going through the trouble of actually creating an item]
                        calculatedPositions = ConvertSpacialDefIntoGridIndexes((w, h), itemData.RotatedSpacialDef(necessaryRotation), itemData.RotatedItemHandle(necessaryRotation));

                        //Debug.Log($"Is area within grid: {IsAreaWithinGrid(calculatedPositions)}\n Positions: {StringifyPositions(calculatedPositions)}");

                        //first, make sure the space is cleared and valid
                        if (CountUniqueStacksInArea(calculatedPositions) == 0 && IsAreaWithinGrid(calculatedPositions))
                        {
                            bool isExcludedSpaceDetected = false;

                            //next, make sure no no excluded spaces are within the calculated positions
                            foreach ((int,int) position in calculatedPositions)
                            {
                                if (excludedPositions.Contains(position))
                                {
                                    isExcludedSpaceDetected = true;
                                    break;
                                }
                            }

                            //if no excluded spaces were found within this set of positions, then return this set of positions as a valid placement area
                            if (!isExcludedSpaceDetected)
                            {
                                gridPosition = (w, h);

                                //Debug.Log($"Tracing 'FindSpaceForStack':\n Cell iteration: ({w},{h})\nStatus: success! Returning '{calculatedPositions.Count()}' calculatedPositions.\n DesiredRotation: {necessaryRotation}");
                                return calculatedPositions;
                            }

                            
                        }

                        rotationCount++;
                    }

                    //Debug.Log($"Tracing 'FindSpaceForStack':\n Cell iteration: ({w},{h})\nStatus: NO SPACE FOUND");

                    //none found. Reset the rotationCount and move on to the next cell
                    rotationCount = 0;
                }
            }

            //None were found.
            //Debug.Log($"Tracing 'FindSpaceForStack':\n No Positions Found. Returning an empty collection...");
            return new();
        }


        private void PositionUiTextOntoStack(RectTransform uiText, HashSet<(int, int)> stackPositions)
        {


            //the stack value needs to be on the rightmost, lowest cell value.
            //Find that cell index


            //calculate the item's the rightmost, lowest cell
            int xMaxPosition = 0;
            int yMinPosition = 0;

            //first find the lowest cell that exists
            bool firstIteration = true;

            foreach ((int, int) index in stackPositions)
            {
                if (firstIteration)
                {
                    yMinPosition = index.Item2;
                    firstIteration = false;
                }
                else
                {
                    if (index.Item2 < yMinPosition)
                        yMinPosition = index.Item2;
                }
            }

            //next find the rightmost cell that's also the lowest
            firstIteration = false;

            foreach ((int, int) index in stackPositions)
            {
                if (index.Item2 == yMinPosition)
                {
                    if (firstIteration)
                    {
                        xMaxPosition = index.Item1;
                        firstIteration = false;
                    }
                    else if (index.Item1 > xMaxPosition)
                        xMaxPosition = index.Item1;
                }

            }

            //get the found cell's position on the grid
            Vector2 bottomRightCellPosition = GetCellObject((xMaxPosition, yMinPosition)).GetComponent<RectTransform>().localPosition;

            //set the stackText's transform to that cell's position. Different parents, but both object should be the same size and in the same place
            uiText.localPosition = bottomRightCellPosition;

        }
        public RectTransform GetOverlayRectTransform() { return _overlayContainer; }

        public string StringifyPositions(HashSet<(int,int)> positions)
        {
            string log = "";
            foreach (var position in positions)
                log += position.ToString();
            return log;
        }
    }
}

