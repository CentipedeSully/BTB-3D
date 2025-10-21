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

    }
    public static void ExitHoveredWindow(IUiWindow window)
    {
        if (window == _hoveredWindow)
        {
            _hoveredWindow = null;
        }
            
    }

    public static IUiWindow GetHoveredWindow() { return _hoveredWindow; }



}
