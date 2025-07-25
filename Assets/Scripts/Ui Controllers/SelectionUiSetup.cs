using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionUiSetup : MonoBehaviour
{
    [SerializeField] private GameObject _selectionObject;
    [SerializeField] private Text _selectionText;



    private void Awake()
    {
        SelectionManager.SetUiSelectionObject(_selectionObject);
        SelectionManager.SetUiSelectionTextObject(_selectionText);

        if (_selectionObject != null)
            _selectionObject.SetActive(false);
    }
}
