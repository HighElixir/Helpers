using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HighElixir.DataManagements
{
    /// <summary>
    /// データ間の参照（Def同士のリンク）を解決するためのユーティリティクラス。
    /// DataRepositoryでロードした定義データの中に、他のDefを参照するプロパティがある場合、
    /// その参照を後から自動的に解決する。
    /// </summary>
    public static class ReferenceResolver
    {
        private static readonly object _lock = new();

        #region Queueing
        private static readonly Queue<Action> _resolveQueue = new();

        /// <summary>
        /// 単一のDef参照解決タスクをキューに登録する。
        /// </summary>
        /// <typeparam name="T">参照対象のDef型</typeparam>
        /// <param name="target">参照先を保持するオブジェクト</param>
        /// <param name="property">リンク先を設定するプロパティ情報</param>
        /// <param name="defName">参照するDefの名前</param>
        public static void EnqueueResolution<T>(object target, PropertyInfo property, string defName)
            where T : IDefinitionData, new()
        {
            lock (_lock)
            {
                _resolveQueue.Enqueue(() => ResolveImmediate<T>(target, property, false, defName));
            }
        }

        /// <summary>
        /// 複数のDef参照解決タスクをキューに登録する。
        /// プロパティがIEnumerable型（List等）の場合に使用される。
        /// </summary>
        /// <typeparam name="T">参照対象のDef型</typeparam>
        /// <param name="target">参照先を保持するオブジェクト</param>
        /// <param name="property">リンク先を設定するプロパティ情報</param>
        /// <param name="defNames">参照するDef名の配列</param>
        public static void EnqueueResolution<T>(object target, PropertyInfo property, params string[] defNames)
            where T : IDefinitionData, new()
        {
            lock (_lock)
            {
                bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(property.PropertyType)
                                    && property.PropertyType != typeof(string);
                _resolveQueue.Enqueue(() => ResolveImmediate<T>(target, property, isEnumerable, defNames));
            }
        }

        /// <summary>
        /// 解決待ちキューをすべてクリアする。
        /// </summary>
        public static void ClearQueue()
        {
            lock (_lock)
            {
                _resolveQueue.Clear();
            }
        }
        #endregion

        #region Resolving
        /// <summary>
        /// 登録済みのすべての参照解決タスクを順次実行する。
        /// IEnumeratorを返すため、Unityのコルーチンでフレーム分散実行も可能。
        /// </summary>
        public static IEnumerator ResolveAll()
        {
            while (true)
            {
                Action action;
                lock (_lock)
                {
                    if (_resolveQueue.Count == 0) break;
                    action = _resolveQueue.Dequeue();
                }
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ReferenceResolver] Resolve failed: {ex.Message}");
                }
                yield return null;
            }
        }

        /// <summary>
        /// 1つの参照解決を即時に実行する内部処理。
        /// 参照対象が存在しない場合はログ出力してスキップする。
        /// </summary>
        private static void ResolveImmediate<T>(object target, PropertyInfo property, bool isEnumerable, params string[] defNames)
            where T : IDefinitionData, new()
        {
            List<T> refData = new();
            for (int i = 0; i < defNames.Length; i++)
            {
                if (string.IsNullOrEmpty(defNames[i]))
                {
                    if (isEnumerable)
                        continue;
                    else
                        return;
                }
                try
                {
                    refData.Add(DataRepository<T>.GetData(defNames[i]));
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine($"Referenced data '{defNames[i]}' not found for property '{property.Name}' on type '{target.GetType().Name}'.");
                }
                if (!isEnumerable)
                    break;
            }

            if (property != null && property.CanWrite)
            {
                if (isEnumerable)
                    property.SetValue(target, refData);
                else
                    property.SetValue(target, refData.FirstOrDefault());
            }
            else
            {
                Console.WriteLine($"Property '{property?.Name}' not writable on type '{target.GetType().Name}'.");
            }
        }
        #endregion

        #region Checking
        private static readonly Dictionary<Type, List<(PropertyInfo prop, RequiredReferenceAttribute attr)>> _cache = new();

        /// <summary>
        /// 指定したデータインスタンスに定義されたRequiredReference属性を走査し、
        /// 自動的に参照解決タスクを登録する。
        /// 一度スキャンした型はキャッシュされ、次回以降は高速に処理される。
        /// </summary>
        /// <param name="data">チェック対象のDefデータ</param>
        public static void CheckRequiredReferences(IDefinitionData data)
        {
            var type = data.GetType();
            if (_cache.TryGetValue(type, out var cachedMembers))
            {
                foreach (var item in cachedMembers)
                {
                    EnqueueInternal(item.attr, data, item.prop);
                }
            }
            else
            {
                var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                List<(PropertyInfo prop, RequiredReferenceAttribute attr)> toCache = new();

                foreach (var member in members)
                {
                    if (member is not PropertyInfo prop) continue;
                    var attr = member.GetCustomAttribute<RequiredReferenceAttribute>();
                    if (attr == null) continue;

                    EnqueueInternal(attr, data, prop);
                    toCache.Add((prop, attr));
                }
                _cache[type] = toCache;
            }
        }

        /// <summary>
        /// 属性情報に基づいて解決タスクを登録する内部メソッド。
        /// </summary>
        private static void EnqueueInternal(RequiredReferenceAttribute attr, IDefinitionData target, PropertyInfo property)
        {
            var type = target.GetType();
            var defField = type.GetField(attr.DefNameField,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (defField == null)
                throw new InvalidOperationException($"DefName field '{attr.DefNameField}' not found in '{type.Name}'.");

            var defValue = defField.GetValue(target);
            string[] defNames = defValue switch
            {
                string s => new[] { s },
                IEnumerable<string> list => list.ToArray(),
                _ => throw new InvalidOperationException(
                    $"Unsupported DefName field type: {defValue?.GetType().Name}")
            };

            var method = typeof(ReferenceResolver)
                .GetMethod(nameof(EnqueueResolution))
                .MakeGenericMethod(attr.TargetType);
            method.Invoke(null, new object[] { target, property, defNames });
        }
        #endregion

        #region Disposing
        /// <summary>
        /// キャッシュをクリアする。
        /// </summary>
        public static void ClearCache()
        {
            lock (_lock)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// キャッシュおよびキューをすべて破棄する。
        /// リソース解放やドメインリロード時に使用。
        /// </summary>
        public static void Dispose()
        {
            ClearCache();
            ClearQueue();
        }
        #endregion
    }
}
