using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InvControlHelper 
{

    public static InventoryController _invController;


    public static void SetInventoryController(InventoryController invController) { _invController = invController; }

    public static InventoryController GetInventoryController() { return _invController; }
    public static void SetActiveItemGrid(ItemGrid newGrid) { _invController.SetActiveItemGrid(newGrid); }
    public static void LeaveGrid(ItemGrid gridToLeave) { _invController.LeaveGrid(gridToLeave); }

}
