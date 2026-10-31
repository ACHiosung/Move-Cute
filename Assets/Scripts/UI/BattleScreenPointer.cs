using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MoveCute
{
    // Reusable pointer relay: artwork and click targets remain independent.
    public class BattleScreenPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public Action Enter, Exit, Hold, RightClick, LeftClick;
        public float holdSeconds = 0.55f;
        Coroutine holdRoutine;
        bool hovering;
        public void OnPointerEnter(PointerEventData e) { hovering = true; Enter?.Invoke(); }
        public void OnPointerExit(PointerEventData e) { StopHold(); hovering = false; Exit?.Invoke(); }
        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || Hold == null) return;
            StopHold();
            holdRoutine = StartCoroutine(WaitForHold());
        }
        public void OnPointerUp(PointerEventData e) => StopHold();
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Right) RightClick?.Invoke();
            if (e.button == PointerEventData.InputButton.Left) LeftClick?.Invoke();
        }
        IEnumerator WaitForHold()
        {
            yield return new WaitForSecondsRealtime(holdSeconds);
            holdRoutine = null;
            Hold?.Invoke();
        }
        void StopHold()
        {
            if (holdRoutine == null) return;
            StopCoroutine(holdRoutine);
            holdRoutine = null;
        }
        void OnDisable()
        {
            StopHold();
            if (!hovering) return;
            hovering = false;
            Exit?.Invoke();
        }
    }
}
