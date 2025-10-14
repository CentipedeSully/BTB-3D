using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CellInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //Declarations
    [SerializeField] private (int, int) _index;
    [SerializeField] private InvGrid _grid;
    [SerializeField] private InventoryItem _item;

    //Monobehaviours






    //Internals




    //Interface
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_grid != null)
        {
            InvManagerHelper.SetHoveredCell(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_grid != null)
        {
            InvManagerHelper.ClearHoveredCell(this);
        }
    }




    //Externals
    public (int,int) Index() { return _index; }
    public InvGrid Grid() { return _grid; }
    public InventoryItem Item() { return _item; }
    public void SetIndex((int, int)newIndex) { _index = newIndex; }
    public void SetGrid(InvGrid newGrid) {  _grid = newGrid; }
    public void SetItem(InventoryItem item) { _item = item; }


}
