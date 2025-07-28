using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //Declarations
    [SerializeField] private ItemGrid _grid;




    //Monobehaviours
    private void Awake()
    {
        _grid = GetComponent<ItemGrid>();
    }


    //interface implementations
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_grid != null)
            InvControlHelper.SetActiveItemGrid(_grid);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_grid != null)
            InvControlHelper.LeaveGrid(_grid);
    }
}
