using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dtsInventory
{
    [CreateAssetMenu(fileName = "EconomySetting")]
    public class EconomySetting : ScriptableObject
    {
        [SerializeField] private ItemData _defaultCurrency;
        [SerializeField] private string _unit;

        public ItemData GetCurrencyItem() { return _defaultCurrency; }
        public string GetCurrencyUnit() { return _unit; }

    }
}
