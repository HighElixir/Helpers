using HighElixir.DataManagements.DataReader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HighElixir.DataManagements
{
    /// <summary>
    /// データ管理クラス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class DataRepository<T>
        where T : IDefinitionData, new()
    {
        private static readonly ConcurrentDictionary<string, T> _dataCache = new();
        private static readonly object _lock = new();
        private static HandlingDuplicateDataMode _handlingDuplicateDataMode;
        private static IDataReader _dataReader;
        #region 初期化
        private static bool _isInitialized = false;
        public static void Initialize(IDataReader dataReader, HandlingDuplicateDataMode handling)
        {
            lock (_lock)
            {
                if (_isInitialized)
                    throw new InvalidOperationException("DataManager is already initialized.");
                _dataReader = dataReader ?? throw new ArgumentNullException(nameof(IDataReader));
                _isInitialized = true;
                _handlingDuplicateDataMode = handling;
            }
        }
        public static void Reinitialize(IDataReader dataReader, HandlingDuplicateDataMode handling)
        {
            lock (_lock)
            {
                _dataReader = dataReader ?? throw new ArgumentNullException(nameof(dataReader));
                _handlingDuplicateDataMode = handling;
                _isInitialized = true;
            }
        }
        #endregion

        #region ロード処理
        public static async Task LoadFromFile(string path, IProgress<float> progress = null)
        {
            lock (_lock)
            {
                if (!_isInitialized)
                    throw new InvalidOperationException("DataManager is not initialized.");
            }
            try
            {
                // ロード処理
                var datas = await _dataReader.ReadDataAsync<List<T>>(path, progress);
                foreach (var data in datas)
                {
                    Register(data);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load data from file: {path}", ex);
            }
        }

        private static void Register(T data)
        {
            bool shouldAddFile = true;
            if (!_dataCache.TryAdd(data.DefName, data))
            {
                switch (_handlingDuplicateDataMode)
                {
                    case HandlingDuplicateDataMode.Ignore:
                        shouldAddFile = false;
                        break;
                    case HandlingDuplicateDataMode.Overwrite:
                        OverwriteData(data);
                        break;
                    case HandlingDuplicateDataMode.ThrowException:
                        shouldAddFile = false;
                        throw new InvalidOperationException($"Duplicate data found for key: {data.DefName}");
                }
            }
            if (shouldAddFile)
            {
                ReferenceResolver.CheckRequiredReferences(data);
            }
        }
        #endregion

        #region データ取り出し
        public static T GetData(string defName)
        {
            if (_dataCache.TryGetValue(defName, out var data))
            {
                return data;
            }
            throw new KeyNotFoundException($"Data with defName '{defName}' not found.");
        }

        public static bool TryGetData(string defName, out T data)
        {
            return _dataCache.TryGetValue(defName, out data);
        }

        public static IEnumerable<T> GetAllData() => _dataCache.Values;

        public static async Task<T> GetOrLoadAsync(string defName, string path, IProgress<float> progress)
        {
            if (TryGetData(defName, out var existing))
                return existing;

            Register(await _dataReader.ReadDataAsync<T>(path, progress));
            return GetData(defName);
        }
        #endregion

        #region レポジトリ操作
        public static void OverwriteData(T data)
        {
            if (_dataCache.TryGetValue(data.DefName, out var old))
                _dataCache.TryUpdate(data.DefName, data, old);
        }
        #endregion

        #region Dispose
        public static bool Disposed { get; private set; }
        public static void Dispose()
        {
            lock (_lock)
            {
                if (Disposed) return;
                Disposed = true;
            }
            _dataCache.Clear();
            _dataReader = null;
        }
        #endregion
    }

    public enum HandlingDuplicateDataMode
    {
        Ignore,
        Overwrite,
        ThrowException
    }

    public interface IDefinitionData
    {
        string DefName { get; }
    }
}