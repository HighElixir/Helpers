using System;
using UnityEngine;

namespace HighElixir.Timers.Internal
{
#if UNITY_2017_1_OR_NEWER
    [DefaultExecutionOrder(-100)]
    internal class GlobalTimerDriver : MonoBehaviour
    {
        private bool _updateRegister = false;
        private bool _fixedUpdateRegister = false;
        private void Update()
        {
            if (!_updateRegister)
            {
                GlobalTimer.Update.OnErrorAction(DebugOnEx);
                _updateRegister = true;
            }
            UpdateTimer(GlobalTimer.update, Time.deltaTime);
        }
        private void FixedUpdate()
        {
            if (!_fixedUpdateRegister)
            {
                GlobalTimer.FixedUpdate.OnErrorAction(DebugOnEx);
                _fixedUpdateRegister = true;
            }
            UpdateTimer(GlobalTimer.fixedUpdate, Time.fixedDeltaTime);
        }
        private void UpdateTimer(GlobalTimer.Wrapper wrapper, float time)
        {
            if (wrapper.IsCreated)
                wrapper.Instance.Update(time);
        }
        private void DebugOnEx(Exception exception)
        {
            Debug.LogWarning(exception.ToString());
        }
    }
#else
    internal class GlobalTimerDriver
    {
        // No implementation needed for non-Unity environments
    }
#endif
}