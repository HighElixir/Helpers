using System;
using System.Threading.Tasks;

namespace HighElixir.HESceneManager
{
    public sealed class SceneScopeHandle : IAsyncDisposable
    {
        public enum HandleOnDispose
        {
            Unload,
            SetActive
        }
        private readonly SceneData _data;
        private readonly SceneService _service;
        private readonly HandleOnDispose _handle;
        private bool _disposed = false;

        public bool Canceled { get; private set; } = false;
        public SceneScopeHandle(SceneData data, SceneService service, HandleOnDispose handle = HandleOnDispose.Unload)
        {
            _data = data;
            _service = service;
            _handle = handle;
        }

        public void Cancel()
        {
            if (_disposed) return;
            Canceled = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            if (Canceled) return;
            if (_handle == HandleOnDispose.SetActive)
                await _service.ActivateAsync(_data);
            else
                await _service.UnloadAsync(_data);
        }
    }
}