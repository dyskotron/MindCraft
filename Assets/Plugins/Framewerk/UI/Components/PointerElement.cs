using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Framewerk.UI.Components
{
    public class PointerElement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public class OnPointerChangeEvent : UnityEvent<bool, Vector2>
        {

        }

        public OnPointerChangeEvent OnPointerChanged
        {
            get { return _onPointerChanged; }
        }

        private readonly OnPointerChangeEvent _onPointerChanged = new OnPointerChangeEvent();

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            _onPointerChanged.Invoke(true, eventData.position);
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            _onPointerChanged.Invoke(false, eventData.position);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            
        }
    }
}