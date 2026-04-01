using dtsInventory;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dtsInventory
{
    public class ContainerUpgradeController : MonoBehaviour
    {
        //Declarations
        [Header("Upgrade Settings")]
        [Header("Lv 1")]
        [SerializeField] private List<ItemData> _requiredItemsForUpgradeLv1 = new List<ItemData>();
        [SerializeField] private List<int> _requiredAmountsForUpgradeLv1 = new List<int>();
        [SerializeField] private Vector2Int _gridDimensionsUpgradeLv1;

        [Header("Lv 2")]
        [SerializeField] private List<ItemData> _requiredItemsForUpgradeLv2 = new List<ItemData>();
        [SerializeField] private List<int> _requiredAmountsForUpgradeLv2 = new List<int>();
        [SerializeField] private Vector2Int _gridDimensionsUpgradeLv2;

        [Header("Lv 3")]
        [SerializeField] private List<ItemData> _requiredItemsForUpgradeLv3 = new List<ItemData>();
        [SerializeField] private List<int> _requiredAmountsForUpgradeLv3 = new List<int>();
        [SerializeField] private Vector2Int _gridDimensionsUpgradeLv3;

        [Header("Debug")]
        [SerializeField] private bool _isDebugActive = false;
        [SerializeField] private InvGrid _paramUpgradeTargetInv;
        [SerializeField] private InvGrid _paramPaySourceInv;
        [SerializeField] private bool _paramIgnoreCosts = false;
        [SerializeField] private bool _cmdUpgradeToLv1;
        [SerializeField] private bool _cmdUpgradeToLv2;
        [SerializeField] private bool _cmdUpgradeToLv3;

        private List<ItemData> _selectedItemList;
        private List<int> _selectedCostList;
        private Vector2Int _selectedUpgradeDimensions;




        //Monobehaviours
        private void Update()
        {
            if (_isDebugActive)
                ListenForDebugCommands();
        }





        //internals
        private bool DoesGridContainCost(List<ItemData> itemList, List<int> costList, InvGrid grid)
        {
            if (grid == null)
            {
                Debug.LogWarning("Cannot check if a null grid contains any items");
                return false;
            }

            if (itemList == null)
            {
                Debug.LogWarning("item list is null. Cannot check if the given grid contains a null list of items");
                return false;
            }

            if (costList == null)
            {
                Debug.LogWarning("amounts list is null. Cannot check if the given grid contains a list of items with null amounts");
                return false;
            }

            if (itemList.Count != costList.Count)
            {
                Debug.LogWarning("The item list and amount list are different sizes. Aborting the query due to list size mismatch.");
                return false;
            }

            //Merge both lists into a single dictionary
            //count duplicate item entries
            Dictionary<string,int> costsDict = new Dictionary<string,int>();

            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i] == null)
                {
                    Debug.LogWarning("Detected a null item. Skipping this item in the list");
                    continue;
                }

                if (costsDict.ContainsKey(itemList[i].ItemCode()))
                    costsDict[itemList[i].ItemCode()] += costList[i];
                else
                    costsDict.Add(itemList[i].ItemCode(), costList[i]);
            }


            Dictionary<HashSet<(int, int)>, int> queryResults = new();
            foreach(KeyValuePair<string,int> entry in costsDict)
            {
                if (!grid.DoesItemExist(ItemCreatorHelper.GetItemDataFromItemCode(entry.Key),entry.Value, out queryResults))
                {
                    Debug.Log($"Grid [{grid.name}] FAILED to contain {entry.Value} {ItemCreatorHelper.GetItemDataFromItemCode(entry.Key).name}(s). returning false");
                    return false;
                }       
            }

            return true;
        }
        private void UpgradeGrid(InvGrid paySource, InvGrid upgradeTarget, int desiredLevel, bool ignoreCost = false)
        {

            switch (desiredLevel)
            {
                case 1:
                    _selectedItemList = _requiredItemsForUpgradeLv1;
                    _selectedCostList = _requiredAmountsForUpgradeLv1;
                    _selectedUpgradeDimensions = _gridDimensionsUpgradeLv1;
                    break;


                case 2:
                    _selectedItemList = _requiredItemsForUpgradeLv2;
                    _selectedCostList = _requiredAmountsForUpgradeLv2;
                    _selectedUpgradeDimensions = _gridDimensionsUpgradeLv2;
                    break;


                case 3:
                    _selectedItemList = _requiredItemsForUpgradeLv3;
                    _selectedCostList = _requiredAmountsForUpgradeLv3;
                    _selectedUpgradeDimensions = _gridDimensionsUpgradeLv3;
                    break;


                default:

                    Debug.LogWarning($"Undefined upgrade Level detected: [{desiredLevel}]. Aborting upgrade.");
                    return;

            }

            if (upgradeTarget == null)
            {
                Debug.LogWarning("Target upgrade grid is null. Aborting upgrade.");
                return;
            }

            int xNeededExtentions = _selectedUpgradeDimensions.x - upgradeTarget.ContainerSize().x;
            int yNeededExtentions = _selectedUpgradeDimensions.y - upgradeTarget.ContainerSize().y;

            //Abort if the target invGrid container's size is >= the upgrade's dimesions
            //[This avoids paying for upgrades the grid already possesses]
            if (xNeededExtentions <= 0 && yNeededExtentions <= 0)
            {
                Debug.Log($"Target Container [{upgradeTarget.name}] size is GreaterThan or EqualTo this upgrade's dimensions. Aborting upgrade.");
                return;
            }

            if (!ignoreCost)
            {
                //Abort if the paying grid is null
                if (paySource == null)
                {
                    Debug.LogWarning("PaySource grid [to upgrade a grid] is null. Aborting upgrade.");
                    return;
                }

                //Abort if insufficient items exist in the paying grid.
                if (!DoesGridContainCost(_selectedItemList, _selectedCostList, paySource))
                {
                    Debug.Log($"grid [{paySource.name}] doesn't contain the reqired upgrade cost. Aborting upgrade.");
                    return;
                }




                //remove the cost from the paying grid
                for (int i = 0; i < _selectedItemList.Count; i++)
                    paySource.RemoveItem(_selectedItemList[i], _selectedCostList[i]);

            }

            //only upgrade what needs to be upgraded
            if (xNeededExtentions > 0)
                upgradeTarget.ExpandGrid(xNeededExtentions, 0);
            if (yNeededExtentions > 0)
                upgradeTarget.ExpandGrid(0, yNeededExtentions);


        }





        //Externals
        public void UpgradeGridToLv1(InvGrid paySource, InvGrid upgradeTarget, bool ignoreCost = false)
        {

            UpgradeGrid(paySource, upgradeTarget, 1, ignoreCost);
            return;
        }
        public void UpgradeGridToLv2(InvGrid paySource, InvGrid upgradeTarget, bool ignoreCost = false)
        {

            UpgradeGrid(paySource, upgradeTarget, 2, ignoreCost);
            return;
        }
        public void UpgradeGridToLv3(InvGrid paySource, InvGrid upgradeTarget, bool ignoreCost = false)
        {

            UpgradeGrid(paySource, upgradeTarget, 3, ignoreCost);
            return;
        }

        public Vector2Int Lv1Dimensions() { return _gridDimensionsUpgradeLv1; }
        public Vector2Int Lv2Dimensions() { return _gridDimensionsUpgradeLv2; }
        public Vector2Int Lv3Dimensions() { return _gridDimensionsUpgradeLv3; }



        //Debug
        private void ListenForDebugCommands()
        {
            if (_cmdUpgradeToLv1)
            {
                _cmdUpgradeToLv1 = false;
                UpgradeGridToLv1(_paramPaySourceInv, _paramUpgradeTargetInv, _paramIgnoreCosts);
            }

            if (_cmdUpgradeToLv2)
            {
                _cmdUpgradeToLv2 = false;
                UpgradeGridToLv2(_paramPaySourceInv, _paramUpgradeTargetInv, _paramIgnoreCosts);
            }

            if (_cmdUpgradeToLv3)
            {
                _cmdUpgradeToLv3 = false;
                UpgradeGridToLv3(_paramPaySourceInv, _paramUpgradeTargetInv, _paramIgnoreCosts);
            }
        }



    }
}

