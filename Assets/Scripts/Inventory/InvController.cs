using mapPointer;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InvController : MonoBehaviour
{
    private InvGrid _invGrid;
    [SerializeField] private InventoryItem _selectedItem;
    [SerializeField] private GameObject _pointerContainer;
    [SerializeField] private Canvas _uiCanvas;
    [SerializeField] private Camera _uiCam;
    private RectTransform _pointerRectTransform;
    private Vector2 _localPoint;
    private Vector2Int _itemHandle;
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

            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                //rotate the item internally
                _selectedItem.RotateItem(RotationDirection.Clockwise);
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

                if (_invGrid.IsPlacementValid(placementPositions))
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
                }
                
            }

            //place item on grid
            else
            {
                List<(int, int)> placementArea = _invGrid.ConvertSpacialDefIntoGridIndexes(_hoveredCellIndex, _selectedItem.GetSpacialDefinition(), _selectedItem.ItemHandle());

                //only allow placement if the derived position is valid 
                if (_invGrid.IsPlacementValid(placementArea))
                {
                    _invGrid.PlaceItemWithinGrid(_selectedItem, placementArea);

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


        }
    }
}
