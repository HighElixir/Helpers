using UnityEngine;

namespace HighElixir.Timers.Internal
{
#if UNITY_2017_1_OR_NEWER
    [DefaultExecutionOrder(-100)]
    internal class GlobalTimerDriver : MonoBehaviour
    {
        private void Update()
            => UpdateTimer(GlobalTimer.update, Time.deltaTime);

        private void FixedUpdate()
            => UpdateTimer(GlobalTimer.fixedUpdate, Time.fixedDeltaTime);

        private void UpdateTimer(GlobalTimer.Wrapper wrapper, float time)
        {
            if (wrapper.IsCreated)
                wrapper.Instance.Update(time);
        }
    }
#else
    internal class GlobalTimerDriver
    {
        // No implementation needed for non-Unity environments
    }
#endif
}