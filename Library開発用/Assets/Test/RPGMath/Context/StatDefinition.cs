using HighElixir.RPGMath.StatusManage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HighElixir.RPGMath.Context
{
    /// <summary>
    /// 汎用的に扱えるRPG向けのステータス定義コンテキスト。
    /// </summary>
    public sealed class StatDefinition<T>
        where T : IEquatable<T>
    {
        private readonly ConcurrentDictionary<T, Func<float, IStatusHandler<T>>> _handleDefinition = new();

        public void RegisterStatusHandler<TResult>(T key, params ParentRule<T>[] rules)
            where TResult : IStatusHandler<T>, new()
        {
            // 後勝ちで登録
            _handleDefinition[key] = (f) =>
            {
                var res = new TResult();
                res.Initialize(f, rules);
                return res;
            };
        }
        public void RegisterStatusHandler(T key, Type type, params ParentRule<T>[] rules)
        {
            if (type is null ||
                !typeof(IStatusHandler<T>).IsAssignableFrom(type))
            {
                throw new ArgumentException($"Type '{type}' is not IStatusHandler<{typeof(T).Name}>");
            }
            // 後勝ちで登録
            //_handleDefinition[key] = (f) =>
            //{
            //    dynamic x = Activator.CreateInstance(type);
                //x.Initialize(f, rules);
            //    return x;
            //};
        }

        public IStatusHandler<T> CreateStatusHandler(T key, float initialValue)
        {
            if (_handleDefinition.TryGetValue(key, out var func))
            {
                return func(initialValue);
            }
            throw new KeyNotFoundException($"StatusHandler for key '{key}' not found.");
        }
    }
}