using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemCreator : MonoBehaviour
{
    [SerializeField] private GameObject _itemBasePrefab;
    [SerializeField] private List<ItemData> _itemsList = new();



    private void Awake()
    {
        ItemCreatorHelper.SetItemCreator(this);
    }

    public GameObject CreateItem(ItemData itemData, float tileWidth, float tileHeight)
    {
        if (_itemsList.Count == 0 || _itemBasePrefab == null)
            return null;

        GameObject itemObject = Instantiate(_itemBasePrefab);
        InventoryItem invItem = itemObject.GetComponent<InventoryItem>();
        RectTransform invRectTransform = itemObject.GetComponent<RectTransform>();

        invItem.SetItemData(itemData);
        itemObject.GetComponent<Image>().sprite = itemData.Sprite();

        invRectTransform.sizeDelta= new Vector2(invItem.Width(), invItem.Height());
        return itemObject;
    }

    public GameObject CreateRandomItem(float tileWidth, float tileHeight)
    {
        if (_itemsList.Count == 0)
            return null;

        int randomIndex = Random.Range(0, _itemsList.Count);
        return CreateItem(_itemsList[randomIndex], tileWidth, tileHeight);
    }
}
