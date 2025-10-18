using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InvWindow _selfInvController;



    private void Update()
    {
        if (_selfInvController != null)
            ListenForInventoryCommands();
    }


    private void ListenForInventoryCommands()
    {

        //toggle the inventory screen if "i" is pressed
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (_selfInvController.IsWindowOpen())
                _selfInvController.CloseWindow();
            else _selfInvController.OpenWindow();
        }
           
    }


    public InvWindow GetInventoryWindow() {  return _selfInvController; }
}
