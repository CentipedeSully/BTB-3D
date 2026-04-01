using dtsInventory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnContainerEmpty : MonoBehaviour
{
    private ContainerController _containerController;




    private void Awake()
    {
        _containerController = GetComponent<ContainerController>();
    }



}
