using UnityEngine;
using UnityEngine.Events;

namespace Xilo.UI
{
    // Mouse and controller ray can both click the same button in one frame (simulator);
    // accepts only the first click per frame so actions like Next never run twice.
    public static class ClickGuard
    {
        static int lastAcceptedFrame = -1;

        public static UnityAction Wrap(UnityAction action)
        {
            return () =>
            {
                if (Time.frameCount == lastAcceptedFrame)
                    return;
                lastAcceptedFrame = Time.frameCount;
                action();
            };
        }
    }
}
