using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dtsInventory
{
    public class UpgradeBenchController : MonoBehaviour, IInteractable
    {
        
        [SerializeField] private ExpandUiWindowController _upgradeUiWindowController;
        [SerializeField] private float _interactDistance;

        public void EndInteraction()
        {
            _upgradeUiWindowController.CloseWindow();
        }

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public float InteractDistance()
        {
            return _interactDistance;
        }

        public void TriggerInteraction()
        {
            _upgradeUiWindowController.OpenWindow();
        }
    }
}


