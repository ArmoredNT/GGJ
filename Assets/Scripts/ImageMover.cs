using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ImageMover : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;

    void Start()
    {
        // Get necessary components
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Set the element as the topmost UI element
        transform.SetAsLastSibling();

        // Disable raycasting on the UI element during dragging
        //canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Move the UI element based on mouse input
        Vector2 mousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out mousePos);
        rectTransform.position = canvas.transform.TransformPoint(mousePos);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Enable raycasting on the UI element after dragging
        //canvasGroup.blocksRaycasts = true;
    }
}
