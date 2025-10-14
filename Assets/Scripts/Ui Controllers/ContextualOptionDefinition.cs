using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextualOptionDefinition : MonoBehaviour
{
    [SerializeField] private ContextOption _optionType = ContextOption.None;
    [SerializeField] private ContextWindowController _contextWindowController;

    public ContextOption GetContextOption() { return _optionType; }
    public void PerformSelectionOfThisOption() { _contextWindowController.TriggerSelectionEventAndCloseWindow(_optionType); }
}
