using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InvControlHelper 
{


    public static InvController _invController;
    public static void SetInventoryController(InvController invController) { _invController = invController; }
    public static InvController GetInvController() { return _invController; }
    public static void SetActiveItemGrid(InvGrid newGrid) { _invController.SetActiveItemGrid(newGrid); }
    public static void LeaveGrid(InvGrid gridToLeave) { _invController.LeaveGrid(gridToLeave); }
    public static void SetHoveredCell(CellInteract cell) { _invController.SetHoveredCell(cell); }
    public static void ClearHoveredCell(CellInteract cell) { _invController.ClearHoveredCell(cell); }

}
