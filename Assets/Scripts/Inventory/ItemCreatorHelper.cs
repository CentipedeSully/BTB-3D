using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ItemCreatorHelper
{
    private static ItemCreator _creator;



    public static void SetItemCreator(ItemCreator creator) {  _creator = creator; }

    public static GameObject CreateItem(ItemData itemData, float tileWidth, float tileHeight) { return _creator.CreateItem(itemData, tileWidth, tileHeight);}
    public static GameObject CreateRandomItem(float tileWidth, float tileHeight) { return _creator.CreateRandomItem(tileWidth, tileHeight); }
    public static Transform GetUiItemsContainer() { return _creator.GetItemContainer(); }


}
