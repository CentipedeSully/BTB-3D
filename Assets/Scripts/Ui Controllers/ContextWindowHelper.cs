using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public static class ContextWindowHelper
{
    public static ContextWindowController _controller;

    public static void SetContextWindowController(ContextWindowController controller) {  _controller = controller;}
    public static void ShowContextWindow(HashSet<ContextOption> optionsToShow) { _controller.ShowOptionsWindow(optionsToShow); }
    public static void HideContextWindow() { _controller.HideOptionsWindow(); }
    public static ContextWindowController GetContextWindowController() { return _controller; }


}
