using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace ChartUtil
{
    public class ChartPointerDragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public UnityAction onBeginDrag;
        public UnityAction onEndDrag;

        bool m_dragging = false;
        public bool isDragging { get { return m_dragging; } }

        public void OnBeginDrag(PointerEventData eventData)
        {
            m_dragging = true;
            if (onBeginDrag != null) onBeginDrag.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {

        }

        public void OnEndDrag(PointerEventData eventData)
        {
            m_dragging = false;
            if (onEndDrag != null) onEndDrag.Invoke();
        }
    }
}