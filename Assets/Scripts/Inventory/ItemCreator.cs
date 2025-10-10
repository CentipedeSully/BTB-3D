using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemCreator : MonoBehaviour
{
    [SerializeField] private GameObject _itemBasePrefab;
    [SerializeField] private Transform _itemContainer;
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
        itemObject.name = itemData.name;
        InventoryItem invItem = itemObject.GetComponent<InventoryItem>();
        RectTransform invRectTransform = itemObject.GetComponent<RectTransform>();

        invItem.SetItemData(itemData);
        itemObject.GetComponent<Image>().sprite = itemData.Sprite();

        invRectTransform.sizeDelta= new Vector2(invItem.Width(), invItem.Height());
        invRectTransform.SetParent(_itemContainer,false);
        return itemObject;
    }

    public GameObject CreateRandomItem(float tileWidth, float tileHeight)
    {
        if (_itemsList.Count == 0)
            return null;

        int randomIndex = Random.Range(0, _itemsList.Count);
        return CreateItem(_itemsList[randomIndex], tileWidth, tileHeight);
    }

    public HashSet<ItemData> GetItemList()
    {
        HashSet<ItemData> listCopy = new HashSet<ItemData>();

        foreach (ItemData item in listCopy)
            listCopy.Add(item);

        return listCopy;
    }

    public Transform GetItemContainer()
    {
        return _itemContainer;
    }
}
