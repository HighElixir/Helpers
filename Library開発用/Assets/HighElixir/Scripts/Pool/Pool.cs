using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HighElixir.Pools
{
    /// <summary>
    /// オブジェクトの再利用を行う汎用プールクラス。
    /// <br/>GC負荷を軽減し、高頻度生成オブジェクトの効率を最適化する。
    /// <br/>スレッドセーフで、Unity以外の環境でも利用可能。
    /// </summary>
    /// <typeparam name="T">プール対象の型</typeparam>
    public class Pool<T> : IDisposable
    {
        // --- 基本構成 ---
        private readonly Func<T> _createMethod;
        private readonly Action<T> _destroyMethod;
        private readonly ConcurrentQueue<T> _available = new();
        private readonly ConcurrentDictionary<T, byte> _inUse = new();

        private int _maxPoolSize;

        /// <summary>プールが破棄済みかどうか</summary>
        public bool Disposed { get; private set; } = false;

        /// <summary>初期化済みかどうか</summary>
        public bool Initialized { get; private set; } = false;

        /// <summary>必要に応じて自動でプールサイズを拡張するかどうか</summary>
        public bool AutoExpand { get; set; } = true;

        /// <summary>最大許容キャパシティ</summary>
        public int MaxCapacity { get; set; } = 10000;

        /// <summary>現在利用可能なオブジェクト数</summary>
        public int AvailableCount => _available.Count;

        /// <summary>現在使用中のオブジェクト数</summary>
        public int InUseCount => _inUse.Count;

        /// <summary>プール全体のオブジェクト数</summary>
        public int TotalCount => AvailableCount + InUseCount;

        // --- イベント群 ---
        /// <summary>Get直後に呼び出されるイベント</summary>
        public event Action<T> OnGetEvt;

        /// <summary>Release直後に呼び出されるイベント</summary>
        public event Action<T> OnReleaseEvt;

        /// <summary>GetAsDisposableで取得した際に呼ばれるイベント</summary>
        public event Action<PooledObject<T>> OnAcquiredPooledEvt;

        /// <summary>生成直後に呼び出されるイベント</summary>
        public event Action<T> OnCreateEvt;

        /// <summary>破棄直前に呼び出されるイベント</summary>
        public event Action<T> OnDestroyEvt;

        /// <summary>
        /// プールを初期化する。
        /// </summary>
        /// <param name="createMethod">オブジェクト生成メソッド</param>
        /// <param name="destroyMethod">オブジェクト破棄メソッド</param>
        /// <param name="maxPoolSize">初期プールサイズ</param>
        /// <param name="lazyInit">遅延初期化を行うか</param>
        public Pool(Func<T> createMethod, Action<T> destroyMethod, int maxPoolSize, bool lazyInit = false)
        {
            if (createMethod == null) throw new ArgumentNullException(nameof(createMethod));
            if (destroyMethod == null) throw new ArgumentNullException(nameof(destroyMethod));
            if (maxPoolSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxPoolSize));

            _createMethod = createMethod;
            _destroyMethod = destroyMethod;
            _maxPoolSize = maxPoolSize;

            if (!lazyInit)
                Initialize();
        }

        /// <summary>
        /// プールを初期化し、必要数のインスタンスを生成する。
        /// </summary>
        public void Initialize()
        {
            if (Initialized) return;
            Initialized = true;
            FillPool();
        }

        #region Get / Release

        /// <summary>
        /// プールからオブジェクトを取得する。
        /// <br/>足りない場合は新規生成される。
        /// </summary>
        public T Get()
        {
            if (Disposed) throw new ObjectDisposedException(nameof(Pool<T>));
            var obj = Get_Internal();
            OnGetEvt?.Invoke(obj);
            return obj;
        }

        /// <summary>
        /// IDisposableとしてプールオブジェクトを取得する。
        /// <br/>usingスコープで自動的にReleaseされる。
        /// </summary>
        public IPooledObject<T> GetAsDisposable()
        {
            if (Disposed) throw new ObjectDisposedException(nameof(Pool<T>));
            var obj = Get_Internal();
            var pooled = new PooledObject<T>(obj, () => Release(obj));
            OnAcquiredPooledEvt?.Invoke(pooled);
            return pooled;
        }

        /// <summary>
        /// 使用済みオブジェクトをプールへ返却する。
        /// <br/>プールサイズを超える場合は破棄される。
        /// </summary>
        public void Release(T obj)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(Pool<T>));
            if (obj == null) return;

            if (_inUse.Remove(obj, out _))
            {
                OnReleaseEvt?.Invoke(obj);
                if (_available.Count < _maxPoolSize)
                    _available.Enqueue(obj);
                else
                    DestroyObject(obj);
            }
            else
            {
                LogWarning($"{obj} はプール外のオブジェクトです");
            }
        }

        /// <summary>
        /// 内部取得処理。利用可能なオブジェクトを取得または新規生成する。
        /// </summary>
        private T Get_Internal()
        {
            if (!_available.TryDequeue(out T obj))
                obj = CreateInstance();

            if (!_inUse.TryAdd(obj, 0))
                LogWarning($"{obj} はすでに使用中です");

            return obj;
        }

        #endregion

        #region Settings

        /// <summary>
        /// プールサイズを設定し、余剰分を破棄・不足分を補充する。
        /// </summary>
        public void SetPoolSize(int poolSize)
        {
            _maxPoolSize = poolSize;
            var extra = _available.Count - _maxPoolSize;
            var need = _maxPoolSize - (_available.Count + _inUse.Count);

            if (extra > 0)
            {
                for (int i = 0; i < extra; i++)
                    if (_available.TryDequeue(out T g))
                        DestroyObject(g);
            }
            else if (need > 0)
            {
                for (int i = 0; i < need; i++)
                    CreateInstance(false);
            }
        }

        #endregion

        #region 生成・破棄

        /// <summary>
        /// すべてのオブジェクトを再生成する。
        /// </summary>
        public void ReCreateAll()
        {
            foreach (var obj in _available)
                DestroyObject(obj);
            _available.Clear();

            foreach (var obj in _inUse.Keys)
                DestroyObject(obj);
            _inUse.Clear();

            FillPool();
        }

        /// <summary>
        /// プールに必要数のオブジェクトを補充する。
        /// </summary>
        private void FillPool()
        {
            while (TotalCount < _maxPoolSize)
                CreateInstance();
        }

        /// <summary>
        /// 新しいオブジェクトを生成する。
        /// </summary>
        private T CreateInstance(bool enqueue = true)
        {
            if (TotalCount >= MaxCapacity)
                throw new InvalidOperationException($"[{nameof(T)}] MAX_CAPACITYを超過しました");

            var obj = _createMethod();

            if (AutoExpand && TotalCount > _maxPoolSize)
                _maxPoolSize = TotalCount;

            OnCreateEvt?.Invoke(obj);

            if (enqueue)
                _available.Enqueue(obj);

            return obj;
        }

        /// <summary>
        /// オブジェクトを破棄する。
        /// </summary>
        private void DestroyObject(T obj)
        {
            OnDestroyEvt?.Invoke(obj);
            _destroyMethod(obj);
        }

        #endregion

        /// <summary>
        /// プールを破棄し、すべてのオブジェクトを解放する。
        /// </summary>
        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;

            foreach (var obj in _available)
                DestroyObject(obj);

            foreach (var obj in _inUse.Keys)
                DestroyObject(obj);

            _available.Clear();
            _inUse.Clear();

            // イベントを解除
            OnGetEvt = null;
            OnReleaseEvt = null;
            OnAcquiredPooledEvt = null;
            OnCreateEvt = null;
            OnDestroyEvt = null;
        }

        /// <summary>
        /// 環境に応じて警告ログを出力する。
        /// </summary>
        private void LogWarning(string message)
        {
            Console.WriteLine("[Pool Warning] " + message);
        }
    }
}
