using mapPointer;
using System.Collections;
using System.Collections.Generic;
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

    //Monobehaviours
    private void Awake()
    {
        InvControlHelper.SetInventoryController(this);
        _pointerRectTransform = _pointerContainer.GetComponent<RectTransform>();
    }

    private void Start()
    {
        //_pointerRectTransform = _pointerContainer.GetComponent<RectTransform>();
    }

    private void Update()
    {
        RespondToInvClicks();
        BindPointerParentToMousePosition();
    }


    //internals
    private void RespondToInvClicks()
    {

        if (_invGrid != null && Input.GetMouseButtonDown((int)MouseBtn.Left))
        {
            Vector2Int clickPosition = _invGrid.GetTileGridPosition(Input.mousePosition);
            if (_selectedItem == null)
            {

                _selectedItem = _invGrid.RemoveItem(clickPosition.x,clickPosition.y);

                //reparent the selected item to the pointerContainer. Visualize the pickup
                RectTransform itemRectTransform = _selectedItem.GetComponent<RectTransform>();
                itemRectTransform.SetParent(_pointerRectTransform);
                itemRectTransform.localPosition = Vector2.zero;
                itemRectTransform.localScale = Vector2.one;
            }
            else
            {
                _invGrid.PlaceItem(_selectedItem,clickPosition.x,clickPosition.y);
                _selectedItem = null;

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
    public void SetActiveItemGrid(ItemGrid newGrid)
    {
        _invGrid = newGrid;
    }

    public void LeaveGrid(ItemGrid specificGrid)
    {
        if (specificGrid == _invGrid)
        {
            _invGrid = null;
        }
    }





}
