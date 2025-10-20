using System;
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
        private static readonly Dictionary<string, T> _dataCache = new();
        private static readonly object _lock = new();
        private static HandlingDuplicateDataMode _handlingDuplicateDataMode;

        #region 初期化
        private static bool _isInitialized = false;
        public static void Initialize(Func<string, Task<T>> loader, HandlingDuplicateDataMode handling)
        {
            lock (_lock)
            {
                if (_isInitialized)
                    throw new InvalidOperationException("DataManager is already initialized.");
                _loader = loader ?? throw new ArgumentNullException(nameof(loader));
                _isInitialized = true;
                _handlingDuplicateDataMode = handling;
            }
        }
        public static void Reinitialize(Func<string, Task<T>> loader, HandlingDuplicateDataMode handling)
        {
            lock (_lock)
            {
                _loader = loader ?? throw new ArgumentNullException(nameof(loader));
                _handlingDuplicateDataMode = handling;
                _isInitialized = true;
            }
        }
        #endregion

        #region ロード処理
        private static readonly HashSet<string> _loadedFiles = new();
        private static Func<string, Task<T>> _loader;
        public static async Task LoadFromFile(string path)
        {
            lock (_lock)
            {
                if (!_isInitialized)
                    throw new InvalidOperationException("DataManager is not initialized.");
            }
            try
            {
                // ロード処理
                var data = await _loader(path);
                lock (_lock)
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
                        _loadedFiles.Add(path);
                        ReferenceResolver.CheckRequiredReferences(data);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load data from file: {path}", ex);
            }
        }

        public static async Task LoadFromFolder(string folderPath)
        {
            var filePaths = System.IO.Directory.GetFiles(folderPath);
            List<Task> loadTasks = new();
            foreach (var filePath in filePaths)
            {
                loadTasks.Add(LoadFromFile(filePath));
            }
            await Task.WhenAll(loadTasks);
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
        public static IEnumerable<T> GetAllData()
        {
            lock (_lock)
                return _dataCache.Values.ToList();
        }
        public static async Task<T> GetOrLoadAsync(string defName, string path)
        {
            if (TryGetData(defName, out var existing))
                return existing;

            await LoadFromFile(path);
            return GetData(defName);
        }
        #endregion

        #region レポジトリ操作
        public static void ClearCache()
        {
            lock (_lock)
            {
                _dataCache.Clear();
                _loadedFiles.Clear();
            }
        }
        public static async Task ReloadAll()
        {
            lock (_lock)
            {
                if (!_isInitialized)
                    throw new InvalidOperationException("DataManager is not initialized.");
            }

            var filesToReload = new List<string>(_loadedFiles);
            ClearCache();

            List<Task> loadTasks = new();
            foreach (var filePath in filesToReload)
            {
                loadTasks.Add(LoadFromFile(filePath));
            }
            await Task.WhenAll(loadTasks);
        }

        public static void OverwriteData(T data)
        {
            lock (_lock)
            {
                _dataCache[data.DefName] = data;
            }
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