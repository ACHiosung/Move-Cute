using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoveCute
{
    // Wheel input takes over immediately from drag inertia.
    public class BattleCardScrollRect : ScrollRect
    {
        public override void OnScroll(PointerEventData eventData)
        {
            StopMovement();
            base.OnScroll(eventData);
            StopMovement();
        }
    }
}
