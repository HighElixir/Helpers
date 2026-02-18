using System;
using UnityEngine;

namespace HighElixir.Timers.Unity
{
    [DefaultExecutionOrder(-100)]
    internal class GlobalTimerDriver : MonoBehaviour
    {
        private bool _updateRegister = false;
        private bool _fixedUpdateRegister = false;
        private bool _lateUpdateRegister = false;
        private bool _unscaledUpdateRegister = false;

        private void Update()
        {
            RegisterIfCreated(GlobalTimer.update, ref _updateRegister);
            RegisterIfCreated(GlobalTimer.unscaledUpdate, ref _unscaledUpdateRegister);

            UpdateTimer(GlobalTimer.update, Time.deltaTime);
            UpdateTimer(GlobalTimer.unscaledUpdate, Time.unscaledDeltaTime);
        }

        private void LateUpdate()
        {
            RegisterIfCreated(GlobalTimer.lateUpdate, ref _lateUpdateRegister);
            UpdateTimer(GlobalTimer.lateUpdate, Time.deltaTime);
        }

        private void FixedUpdate()
        {
            RegisterIfCreated(GlobalTimer.fixedUpdate, ref _fixedUpdateRegister);
            UpdateTimer(GlobalTimer.fixedUpdate, Time.fixedDeltaTime);
        }

        private void RegisterIfCreated(GlobalTimer.Wrapper wrapper, ref bool registered)
        {
            if (registered || !wrapper.IsCreated)
                return;

            wrapper.Instance.OnErrorAction(DebugOnEx);
            registered = true;
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
}
