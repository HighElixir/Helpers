using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace HighElixir.DataManagements
{
    public interface IDataRepository
    {
        Type GenericType { get; }
    }
    /// <summary>
    /// データ管理クラス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DataRepository<T> : ScriptableObject, IDataRepository
    {
        public Type GenericType => typeof(T);
        public abstract T ReadByID(string id);
    }
}