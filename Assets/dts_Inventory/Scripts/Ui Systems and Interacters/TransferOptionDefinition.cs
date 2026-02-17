using dtsInventory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



namespace dtsInventory
{
    public class TransferOptionDefinition : MonoBehaviour
    {
        [SerializeField] Text _buttonText;
        private InvGrid _invGridReference;
        private TransferMenuController _menuController;




        public void SetInvGridReference(InvGrid gridReference) { _invGridReference = gridReference; }
        public void SetTransferMenuController(TransferMenuController controller) { _menuController = controller; }
        public void SetButtonText(string newText) {  _buttonText.text = newText; }
        public void ConfirmThisSelection()
        {
            if (_menuController != null && _invGridReference != null)
            {
                _menuController.SubmitSelection(_invGridReference);
            }
        }
    }
}



