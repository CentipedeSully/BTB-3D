using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class CalculateDistance
{
    private static Vector3 _t1Position = Vector3.zero;
    private static Vector3 _t2Position = Vector3.zero;
    private static float _sqrDistanceT1;
    private static float _sqrDistanceT2;

    private static Transform _currentClosest;

    public static Transform GetClosestTransform(Transform t1, Transform t2, Vector3 origin)
    {
        if (t1 == null || t2 == null)
        {
            Debug.LogError("Provided null Arguments for Distance Calculation. Returning null");
            return null;
        }

        _t1Position = t1.position;
        _t2Position = t2.position;

        _sqrDistanceT1 = (_t1Position - origin).sqrMagnitude;

        _sqrDistanceT2 = (_t2Position - origin).sqrMagnitude;

        if (_sqrDistanceT1 <= _sqrDistanceT2)
            return t1;
        else return t2;

        

    }

    public static Transform GetClosestTransform(List<Transform> transforms, Vector3 origin)
    {
        //ensure the list isn't null
        if (transforms == null)
        {
            Debug.LogError("Provided null Argument List for Distance Calculation. Returning null");
            return null;
        }

        //ensure the list isn't empty
        if (transforms.Count == 0)
        {
            Debug.LogError("Provided empty Argument List for Distance Calculation. Returning null");
            return null;
        }

        //ensure all transforms arent null
        foreach (Transform item in transforms)
        {
            if (item == null)
            {
                Debug.LogError("A null transform exsits within Argument List for Distance Calculation. Returning null");
                return null;
            }
        }

        //return the solo position
        if (transforms.Count == 1)
            return transforms[0];


        _currentClosest = transforms[0];

        for (int i = 1; i < transforms.Count; i++)
            _currentClosest = GetClosestTransform(_currentClosest, transforms[i], origin);

        return _currentClosest;
        
    }

}
