using System.Collections.Generic;
using UnityEngine;


namespace dtsInventory
{

    [CreateAssetMenu]
    public class ItemData : ScriptableObject
    {
        [Header("Item Definition")]
        [SerializeField] private string _name;
        [SerializeField] private string _itemCode;
        [SerializeField] private List<Vector2Int> _spacialDefinition = new();
        [SerializeField] private Vector2Int _itemHandle;
        [SerializeField] private string _desc = "";
        [SerializeField] private Sprite _sprite;
        [SerializeField] private int _stackLimit = 1;
        [Header("Possible UI Options")]
        [SerializeField] private bool _isUsable;
        [SerializeField] private bool _isDiscardable;
        [SerializeField] private bool _isOrganizable;






        public string Name() { return _name; }
        public HashSet<(int, int)> SpacialDefinition()
        {
            //Convert the vector2Int types into tuples
            //tuples aren't serialized in the inspector, but they're faster to type on the keyboard XD
            HashSet<(int, int)> spacialDefTuples = new();

            foreach (Vector2Int index in _spacialDefinition)
                spacialDefTuples.Add((index.x, index.y));

            //Debug.Log($"Tracing Item Creation:\nSpacialDef size: {spacialDefTuples.Count}");
            return spacialDefTuples;
        }
        public (int, int) ItemHandle() { return (_itemHandle.x, _itemHandle.y); }
        public string Desc() { return _desc; }
        public Sprite Sprite() { return _sprite; }
        public HashSet<ContextOption> ContextualOptions()
        {
            HashSet<ContextOption> options = new();
            if (_isOrganizable)
                options.Add(ContextOption.OrganizeItem);
            if (_isUsable)
                options.Add(ContextOption.UseItem);
            if (_isDiscardable)
                options.Add(ContextOption.DiscardItem);

            return options;
        }
        public int StackLimit() { return _stackLimit; }
        public string ItemCode() { return _itemCode; }
    }
}

