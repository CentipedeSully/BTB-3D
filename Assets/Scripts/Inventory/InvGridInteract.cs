using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InvGridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //Declarations
    private InvGrid _grid;




    //Monobehaviours
    private void Awake()
    {
        _grid = GetComponent<InvGrid>();
    }


    //interface implementations
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_grid != null)
            InvManagerHelper.SetActiveItemGrid(_grid);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_grid != null)
            InvManagerHelper.LeaveGrid(_grid);
    }
}
