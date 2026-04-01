using dtsInventory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dtsInventory
{
    public class InvWindowInitializer : MonoBehaviour
    {
        List<InvWindow> _windowsToInit = new();
        InvWindow _invWindow;


        private void Start()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                _invWindow = transform.GetChild(i).GetComponent<InvWindow>();

                if (_invWindow != null)
                    _windowsToInit.Add(_invWindow);
            }

            StartCoroutine(InitWindows());
        }


        private IEnumerator InitWindows()
        {
            foreach (InvWindow window in _windowsToInit)
                window.OpenWindow();

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            foreach (InvWindow window in _windowsToInit)
                window.CloseWindow();
        }
    }

}