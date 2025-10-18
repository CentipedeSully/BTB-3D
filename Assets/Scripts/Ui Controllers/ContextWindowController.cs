using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;


public enum ContextOption
{
    None,
    OrganizeItem,
    UseItem,
    DiscardItem
}

public class ContextWindowController : MonoBehaviour , IPointerExitHandler
{
    //Declarations
    [SerializeField] private GameObject _optionElementPrefab;
    [SerializeField] private RectTransform _pointerContainerTransform;
    private float _yPadding;
    private float _xPadding;
    private float _spacingBtwnOptions;
    private float _optionHeight;
    private RectTransform _rectTransform;
    private bool _isWindowOpen = false;


    public delegate void ContextWindowEvent(ContextOption selectedOption);
    public event ContextWindowEvent OnOptionSelected;





    //Monobehaviours
    private void Awake()
    {
        ContextWindowHelper.SetContextWindowController(this);
        
        _rectTransform = GetComponent<RectTransform>();

        if (_optionElementPrefab != null)
            _optionHeight = _optionElementPrefab.GetComponent<RectTransform>().sizeDelta.y;

        VerticalLayoutGroup layoutController = GetComponent<VerticalLayoutGroup>();
        if (layoutController != null)
        {
            _xPadding = layoutController.padding.left + layoutController.padding.right;
            _yPadding = layoutController.padding.top + layoutController.padding.bottom;
            _spacingBtwnOptions = layoutController.spacing;

            
        }
        

    }




    //Internals






    //Externals
    public void ShowOptionsWindow(HashSet<ContextOption> availableOptions)
    {
        if (availableOptions == null)
            return;
        if (availableOptions.Count < 1)
            return;

        //reposition the window onto the pointer
        GetComponent<RectTransform>().localPosition = _pointerContainerTransform.localPosition;

        int optionCount = 0;

        //show all matching context buttons, and hide all buttons that don't match the context
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            ContextualOptionDefinition context = child.GetComponent<ContextualOptionDefinition>();

            if (context != null)
            {
                if (availableOptions.Contains(context.GetContextOption()))
                {
                    child.gameObject.SetActive(true);
                    optionCount++;
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }

        }

        if (optionCount > 0)
        {
            _isWindowOpen = true;

            //resize the window to match the number of options
            float betwixtSpacing = _spacingBtwnOptions * (optionCount - 1);
            float height = _yPadding + _optionHeight * optionCount + _spacingBtwnOptions;
            float width = _xPadding + transform.GetChild(0).GetComponent<RectTransform>().sizeDelta.x; //make sure the child fits well
            _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, height);

            //show the window
            gameObject.SetActive(true);
        }
        else
            Debug.LogWarning("No contextual predefined contextual options were discovered. Ignoring 'ShowContextWindow' request");
        


    }
    public void HideOptionsWindow() 
    {

        if (_isWindowOpen)
        {
            gameObject.SetActive(false);
            _isWindowOpen = false;
        }
            
    }
    public void TriggerSelectionEventAndCloseWindow(ContextOption selectedOption)
    {
        if (_isWindowOpen)
        {
            HideOptionsWindow();
            OnOptionSelected.Invoke(selectedOption);
        }
        
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        HideOptionsWindow();
    }
}
