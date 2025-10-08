using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu]
public class ItemData : ScriptableObject
{
    [SerializeField] private string _name;
    [SerializeField] private List<Vector2Int> _spacialDefinition = new();
    [SerializeField] private Vector2Int _itemHandle;
    [SerializeField] private string _desc = "";
    [SerializeField] private Sprite _sprite;





    public string Name() { return _name; }
    public List<(int, int)> SpacialDefinition() 
    {
        //Convert the vector2Int types into tuples
        //tuples aren't serialized in the inspector, but they're faster to type on the keyboard XD
        List<(int, int)> spacialDefTuples = new();

        foreach (Vector2Int index in _spacialDefinition)
            spacialDefTuples.Add((index.x,index.y));

        return spacialDefTuples;
    }
    public (int, int) ItemHandle() { return (_itemHandle.x, _itemHandle.y); }
    public string Desc() { return _desc; }
    public Sprite Sprite() { return _sprite; }
}
