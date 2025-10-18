using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebugSpawnItem : MonoBehaviour
{
    [SerializeField] private bool _spawnItem;
    [SerializeField] private bool _removeItem;
    [SerializeField] private InvWindow _targetInv;
    [SerializeField] private ItemData _specificItem;
    [SerializeField] private int _amount;





    private void Update()
    {
        if (_targetInv != null)
            ListenForCommands();
    }


    private void ListenForCommands()
    {
        //ensure logical errors are corrected
        if (_amount < 1)
            _amount = 1;


        if (_spawnItem)
        {
            _spawnItem = false;
            
            int spawnCount = 0;
            while (spawnCount < _amount)
            {
                SpawnItemIntoSpecifiedInventory();
                spawnCount++;
            }
        }

        if (_removeItem)
        {
            _removeItem = false;

            int removalCount = 0;
            while (removalCount < _amount)
            {
                RemoveItemFromSpecifiedInventory();
                removalCount++;
            }
        }

    }

    private void SpawnItemIntoSpecifiedInventory()
    {
        InvGrid itemGrid = _targetInv.GetItemGrid();
        GameObject itemObj = null;
        InventoryItem item = null;

        if (_specificItem == null)
        {
            itemObj = ItemCreatorHelper.CreateRandomItem(itemGrid.CellSize().x, itemGrid.CellSize().y);
            item = itemObj.GetComponent<InventoryItem>();

        }
        else
        {
            itemObj = ItemCreatorHelper.CreateItem(_specificItem, itemGrid.CellSize().x, itemGrid.CellSize().y);
            item = itemObj.GetComponent<InventoryItem>();
        }

        (int, int) itemHandleOriginOnGrid = (-1, -1); //default to invalid states
        ItemRotation necessaryRotation = ItemRotation.None; //default to invalid states
        List<(int, int)> openPositions = itemGrid.FindSpaceForItem(item, out itemHandleOriginOnGrid, out necessaryRotation);

        if (itemHandleOriginOnGrid != (-1, -1))
        {
            while (item.Rotation() != necessaryRotation)
                item.RotateItem(RotationDirection.Clockwise);

            itemGrid.PositionItemIntoGridLogically(item, openPositions);
            itemGrid.PositionItemGraphicOntoGridVisually(itemHandleOriginOnGrid, item);
        }
    }
    private void RemoveItemFromSpecifiedInventory()
    {
        InvGrid itemGrid = _targetInv.GetItemGrid();
        Dictionary<InventoryItem, List<(int, int)>> itemDict = itemGrid.GetContainedItemsListCopy();

        if (itemDict.Count == 0)
            return;


        if (_specificItem == null)
        {

            //convert to a list for random selection (if necessary)
            List<KeyValuePair<InventoryItem, List<(int, int)>>> itemsList = itemDict.ToList();

            //if only one item exists, just remove it.
            if (itemsList.Count == 1)
            {
                itemGrid.RemoveItem(itemsList[0].Key);
                return;
            }

            InventoryItem randomlySelectedItem = itemsList[Random.Range(0, itemsList.Count() - 1)].Key;
            itemGrid.RemoveItem(randomlySelectedItem);
            return;

        }

        else
        {
            itemGrid.RemoveItem(_specificItem);
        }
    }



}
