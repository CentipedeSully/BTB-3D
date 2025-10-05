using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ItemRotation
{
    None, //0 or 360
    Once, //90
    Twice, //180
    Thrice, //270
}

public enum RotationDirection
{
    Clockwise,
    CounterClockwise
}


public class InventoryItem : MonoBehaviour
{
    //Declarations
    [SerializeField] ItemData _itemData;
    [SerializeField] Vector2Int _relativeOrigin = new Vector2Int(-1,-1);
    [SerializeField] List<(int,int)> _cellIndexes = new List<(int,int)> ();
    [SerializeField] private ItemRotation _rotation = ItemRotation.None;






    private void RotateIndexesClockwise()
    {
        List<(int,int)> newIndexes = new List<(int,int)> ();
        foreach ((int,int) index in _cellIndexes)
        {
            (int, int) newIndex = (index.Item2, -index.Item1);
            newIndexes.Add (newIndex);
        }

        _cellIndexes = newIndexes;
    }

    private void RotateIndexesCounterClockwise()
    {
        List<(int, int)> newIndexes = new List<(int, int)>();
        foreach ((int, int) index in _cellIndexes)
        {
            (int, int) newIndex = (-index.Item2, index.Item1);
            newIndexes.Add(newIndex);
        }

        _cellIndexes = newIndexes;

    }


    public ItemData ItemData() {  return _itemData; }
    public void SetItemData(ItemData newItemData) 
    {  
        _itemData = newItemData;

        //create the cell Occupancy. Changes when the item rotates
        _cellIndexes.Clear();
        for (int x = 0; x < _itemData.Width(); x++)
        {
            for (int y= 0; y < _itemData.Height(); y++)
            {
                _cellIndexes.Add((x, y));
            }
        }


    }

    public void SetRelativeOrigin(int gridX, int gridY)
    {
        _relativeOrigin.x = gridX;
        _relativeOrigin.y = gridY;
    }

    public Vector2Int GetOriginLocation() {  return _relativeOrigin; }

    public int Width()
    {
        switch (_rotation)
        {
            case ItemRotation.None:
                return _itemData.Width();

            case ItemRotation.Once:
                return _itemData.Height();

                case ItemRotation.Twice:
                return _itemData.Width();

            case ItemRotation.Thrice:
                return _itemData.Height();

            default:
                return _itemData.Width();
        }
    }

    public int Height()
    {
        switch (_rotation)
        {
            case ItemRotation.None:
                return _itemData.Height();

            case ItemRotation.Once:
                return _itemData.Width();

            case ItemRotation.Twice:
                return _itemData.Height();

            case ItemRotation.Thrice:
                return _itemData.Width();

            default:
                return _itemData.Height();
        }
    }

    public ItemRotation Rotation() {  return _rotation; }

    public void RotateItem(RotationDirection direction)
    {
        //default the direction to clockwise if given a werid value
        if (direction != RotationDirection.Clockwise && direction != RotationDirection.CounterClockwise)
            direction = RotationDirection.Clockwise;

        //change the indexes depending on the given direction
        if (direction == RotationDirection.Clockwise)
            RotateIndexesClockwise();
        else
        {
            RotateIndexesCounterClockwise();
        }


        //update the rotation state (used to determine sprite rotation)
        switch (_rotation)
        {
            case ItemRotation.None:
                if (direction == RotationDirection.Clockwise)
                    _rotation = ItemRotation.Once;
                else _rotation = ItemRotation.Thrice;
                return;

            case ItemRotation.Once:
                if (direction == RotationDirection.Clockwise)
                    _rotation = ItemRotation.Twice;
                else _rotation = ItemRotation.None;
                return;

            case ItemRotation.Twice:
                if (direction == RotationDirection.Clockwise)
                    _rotation = ItemRotation.Thrice;
                else _rotation = ItemRotation.Once;
                return;

            case ItemRotation.Thrice:
                if (direction == RotationDirection.Clockwise)
                    _rotation = ItemRotation.None;
                else _rotation = ItemRotation.Twice;
                return;

            default:
                return;
        }
    }

    public Quaternion RotationAngle()
    {
        switch (_rotation)
        {
            case ItemRotation.None:
                return Quaternion.Euler(0, 0, 0);

            case ItemRotation.Once:
                return Quaternion.Euler(0, 0, -90);

            case ItemRotation.Twice:
                return Quaternion.Euler(0, 0, -180);

            case ItemRotation.Thrice:
                return Quaternion.Euler(0, 0, -270);

            default:
                return Quaternion.Euler(0, 0, 0);
        }
    }

    public List<(int,int)> GetCellOccupancy() { return _cellIndexes; }

}
