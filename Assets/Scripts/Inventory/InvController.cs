using mapPointer;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Progress;

public class InvController : MonoBehaviour
{
    private InvGrid _invGrid;
    [SerializeField] private InventoryItem _selectedItem;
    [SerializeField] private GameObject _pointerContainer;
    [SerializeField] private Canvas _uiCanvas;
    [SerializeField] private Camera _uiCam;
    private RectTransform _pointerRectTransform;
    private Vector2 _localPoint;
    [SerializeField] CellInteract _hoveredCell;
    (int,int) _hoveredCellIndex = (-1,-1);
    [SerializeField] Vector2Int _hoverIndex;
    [SerializeField] InventoryItem _itemInCell;
    List<(int,int)> _itemCellOccupancy = new List<(int,int)> ();

    [SerializeField] private GameObject _hoverGraphicPrefab;
    [SerializeField] List<GameObject> _unusedHoverTileGraphics = new();
    [SerializeField] List<GameObject> _hoverTileGraphics = new();
    [SerializeField] private List<(int,int)> _hoveredIndexes = new List<(int,int)> ();
    [SerializeField] private List<(int,int)> _lastFramesHoveredIndexes = new List<(int,int)> ();

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

        RespondToRotationCommands();
        VisualizeHoverTile();
        RespondToInvClicks();
        BindPointerParentToMousePosition();
    }


    //internals
    private void RespondToRotationCommands()
    {
        if (_selectedItem != null)
        {

            if (Input.GetKeyDown(KeyCode.Q))
            {
                //rotate the item internally
                _selectedItem.RotateItem(RotationDirection.CounterClockwise);
                _selectedItem.GetComponent<RectTransform>().rotation = _selectedItem.RotationAngle();

            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                //rotate the item internally
                _selectedItem.RotateItem(RotationDirection.Clockwise);
                _selectedItem.GetComponent<RectTransform>().rotation = _selectedItem.RotationAngle();
            }
        }
    }
    private void VisualizeHoverTile()
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

            //clear any active hover tiles, since no hovered cell is detected
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

        else
        {
            _itemInCell = _invGrid.GetItemOnCell(_hoverIndex.x, _hoverIndex.y);
            InventoryItem hoveredItem = _invGrid.GetItemOnCell(_hoverIndex.x, _hoverIndex.y);


            //highlight the previewed position of the held item
            if (_selectedItem != null)
            {
                //Get the new potential placement positions
                List<(int, int)> placementPositions = _invGrid.ConvertSpacialDefIntoGridIndexes(_hoveredCellIndex,_selectedItem.GetSpacialDefinition(),_selectedItem.ItemHandle());

                if (_invGrid.IsAreaWithinGrid(placementPositions))
                {
                    //Looks like our placement is valid here. Make our hoveredIndexes equal to our expected placement position
                    _hoveredIndexes = placementPositions;
                    
                    //check if the hover data is the same as last frame before we do anything else
                    bool allElementsExist = true;
                    foreach ((int, int) index in _lastFramesHoveredIndexes)
                    {
                        if (!_hoveredIndexes.Contains(index))
                        {
                            allElementsExist = false;
                            break;
                        }
                    }

                    //collections are similar if each element of the last frame exists in the current collection
                    //and both collections are the same length
                    if (allElementsExist && _lastFramesHoveredIndexes.Count == _hoveredIndexes.Count)
                        return; //Since the hover data is the same, nothing needs to be done here. Exit execution



                    //Otherwise, clear the previous hovered graphics, since our hover data changed
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

                    //now replace a tile graphic over each of the new hover positions
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
                
                //clear the hover data. Shouldn't show outdated hover data
                else 
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
                    


                //check if the hover data is the same
                bool allElementsExist = true;
                foreach ((int, int) index in _lastFramesHoveredIndexes)
                {
                    if (!_hoveredIndexes.Contains(index))
                    {
                        allElementsExist = false;
                        break;
                    }
                }

                //collections are similar if each element of the last frame exists in the current collection
                //and both collections are the same length
                if (allElementsExist && _lastFramesHoveredIndexes.Count == _hoveredIndexes.Count)
                    return;

                //clear the previous hovered graphics, since our hover data changed
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

                //now replace a tile graphic over each of the item's positions
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

            //highlight the cell position
            else if (_selectedItem == null && hoveredItem == null)
            {
                _hoveredIndexes.Add(_hoveredCellIndex);

                //clear all other hover graphics
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
                hoverGraphic.transform.SetParent(_invGrid.GetCellObject(_hoveredCellIndex).transform, false);

                hoverGraphic.transform.localScale = Vector3.one;
                hoverGraphic.transform.position = new Vector3(hoverGraphic.transform.position.x, hoverGraphic.transform.position.y, 1);

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
                if (_invGrid.IsCellOccupied(_hoveredCellIndex))
                {
                    //save the item reference
                    _selectedItem = _invGrid.GetItemOnCell(_hoveredCellIndex);
                    
                    //remove the item from the invGrid
                    _invGrid.RemoveItem(_selectedItem);

                    BindSelectedItemToPointer();
                }
                
            }

            //place item on grid
            else
            {
                List<(int, int)> placementArea = _invGrid.ConvertSpacialDefIntoGridIndexes(_hoveredCellIndex, _selectedItem.GetSpacialDefinition(), _selectedItem.ItemHandle());

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
                        foreach ((int,int) index in placementArea)
                        {
                            pickedUpItem = _invGrid.GetItemOnCell(index);
                            if (pickedUpItem != null)
                                break;
                        }

                        //save the reference to the item and remove the currently-stashed item from the grid
                        _invGrid.RemoveItem(pickedUpItem);

                        //place the held item in the freed up space
                        _invGrid.PlaceItemWithinGrid(_selectedItem, placementArea);
                        _invGrid.PlaceItemOntoGridVisually(_hoveredCellIndex, _selectedItem);
                        _selectedItem = null;

                        //make the picked-up item the new held item
                        _selectedItem = pickedUpItem;
                        BindSelectedItemToPointer();
                    }

                    else if (itemCount == 0)
                    {
                        //place the held item into the open space and clear our held item reference
                        _invGrid.PlaceItemWithinGrid(_selectedItem, placementArea);
                        _invGrid.PlaceItemOntoGridVisually(_hoveredCellIndex, _selectedItem);
                        _selectedItem = null;
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
        if (_selectedItem != null)
        {
            //parent the item to the pointer
            _selectedItem.GetComponent<RectTransform>().SetParent(_pointerContainer.transform, false);

            //reset the item's local position to zero
            RectTransform itemRectTransform = _selectedItem.GetComponent<RectTransform>();
            itemRectTransform.localPosition = Vector3.zero;

            //ensure the sprite is of the appropriate size
            itemRectTransform.sizeDelta = new Vector2(_selectedItem.Width() * _invGrid.CellSize().x, _selectedItem.Height() * _invGrid.CellSize().y);
        }
        
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

            //calculate the object's pivot position
            float xPivotPosition = cellWidth * _selectedItem.ItemHandle().Item1 + cellWidth/2;
            float yPivotPosition = cellHeight * _selectedItem.ItemHandle().Item2 + cellHeight/2;

            float normalizedPositionX = xPivotPosition / (_selectedItem.Width() * cellWidth);
            float normalizedPositionY = yPivotPosition / (_selectedItem.Height() * cellHeight);

            RectTransform itemRectTransform = _selectedItem.GetComponent<RectTransform>();
            itemRectTransform.pivot = new Vector2(normalizedPositionX, normalizedPositionY);

            BindSelectedItemToPointer();

        }
    }
}
