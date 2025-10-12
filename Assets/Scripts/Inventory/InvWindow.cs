using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InvWindow : MonoBehaviour, IDragHandler
{
    [SerializeField] private RectTransform _headerRectTransform;
    [SerializeField] private RectTransform _descRectTransform;
    [SerializeField] private RectTransform _gridAreaRectTransform;
    [SerializeField] private RectTransform _actualGridRectTransform;
    [SerializeField] private RectTransform _spritesContainerTransform;
    private RectTransform _rectTransform;
    private Canvas _canvas;

    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        float gridWidth = _actualGridRectTransform.sizeDelta.x;
        float headerHeight = _headerRectTransform.sizeDelta.y;
        float descHeight = _descRectTransform.sizeDelta.y;
        float gridHeight = _actualGridRectTransform.sizeDelta.y;
        _gridAreaRectTransform.sizeDelta = new Vector2(gridWidth, gridHeight);
        _rectTransform.sizeDelta = new Vector2(gridWidth, headerHeight + descHeight + gridHeight);
        _spritesContainerTransform.localPosition = _actualGridRectTransform.localPosition;

        _canvas = CanvasReferenceHelper.GetCanvas();
    }

}
