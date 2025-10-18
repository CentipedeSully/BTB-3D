using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public interface IUiWindow
{
    string UiName();
    bool IsWindowOpen();
    void OpenWindow();
    void CloseWindow();
    void TrackHoverEnter();
    void TrackHoverExit();
    void MoveWindowToFront();

}
public static class UiTracker
{
    private static IUiWindow _hoveredWindow = null;
    public static void SetHoveredWindow(IUiWindow hoveredWindow)
    {
        _hoveredWindow = hoveredWindow;

        if (_hoveredWindow != null)
            Debug.Log($"Hovering Window: {_hoveredWindow.UiName()}");
    }
    public static void ExitHoveredWindow(IUiWindow window)
    {
        if (window == _hoveredWindow)
        {
            if (_hoveredWindow != null)
                Debug.Log($"Exiting Window: {_hoveredWindow.UiName()}");

            _hoveredWindow = null;
        }
            
    }

    public static IUiWindow GetHoveredWindow() { return _hoveredWindow; }



}
