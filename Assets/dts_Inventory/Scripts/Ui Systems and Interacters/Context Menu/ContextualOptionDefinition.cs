using UnityEngine;
using UnityEngine.UI;

namespace dtsInventory
{
    public class ContextualOptionDefinition : MonoBehaviour
    {
        [SerializeField] private ContextOption _optionType = ContextOption.None;
        [SerializeField] private ContextWindowController _contextWindowController;

        public ContextOption GetContextOption() { return _optionType; }

        //refernced via unity event in the Contextual option prefab
        public void PerformSelectionOfThisOption() 
        {
            if (InvManagerHelper.IsInvSystemLocked())
                return;

            _contextWindowController.MarkOptionAsSelected(GetComponent<Button>()); 
            _contextWindowController.SpecifyAmount(_optionType); 
        }
    }
}

