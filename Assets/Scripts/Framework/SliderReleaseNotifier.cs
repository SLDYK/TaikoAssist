using UnityEngine;
using UnityEngine.EventSystems;

namespace TaikoAssist
{
    public class SliderReleaseNotifier : MonoBehaviour, IPointerUpHandler, IEndDragHandler
    {
        [SerializeField] private Separator Separator;
        private int lastNotifyFrame = -1;

        public void OnPointerUp(PointerEventData eventData)
        {
            NotifyOwnerOncePerFrame();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            NotifyOwnerOncePerFrame();
        }

        private void NotifyOwnerOncePerFrame()
        {
            if (lastNotifyFrame == Time.frameCount)
            {
                return;
            }

            lastNotifyFrame = Time.frameCount;
            Separator.SnapSliderAndBlend();
        }
    }
}