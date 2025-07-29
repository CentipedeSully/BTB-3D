using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu]
public class ItemData : ScriptableObject
{
    [SerializeField] private string _name;
    [SerializeField] private int _width = 1;
    [SerializeField] private int _height = 1;
    [SerializeField] private string _desc = "";
    [SerializeField] private Sprite _sprite;





    public string Name() { return _name; }
    public int Width() { return _width; }
    public int Height() { return _height; }
    public string Desc() { return _desc; }
    public Sprite Sprite() { return _sprite; }
}
