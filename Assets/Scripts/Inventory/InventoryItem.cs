using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryItem : MonoBehaviour
{
    //Declarations
    [SerializeField] ItemData _itemData;
    [SerializeField] Vector2Int _relativeOrigin = new Vector2Int(-1,-1);



    public ItemData ItemData() {  return _itemData; }
    public void SetItemData(ItemData newItemData) {  _itemData = newItemData; }

    public void SetRelativeOrigin(int gridX, int gridY)
    {
        _relativeOrigin.x = gridX;
        _relativeOrigin.y = gridY;
    }

    public Vector2Int GetOriginLocation() {  return _relativeOrigin; }

}
