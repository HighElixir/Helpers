using HighElixir.RPGMath.Context;
using System;
using System.Collections.Generic;

namespace HighElixir.RPGMath.StatusManage
{
    /// <summary>
    /// ステータスを管理し、依存関係を解決しながら再計算を行うマネージャ。
    /// 方針：サイクル依存は全面禁止（検出したら例外）。
    /// </summary>
    public sealed class StatusManager<T>
        where T : IEquatable<T>
    {
        private readonly StatDefinition<T> _definition;
        private readonly Dictionary<T, IStatusHandler<T>> _statusHandlers = new();

        // 依存グラフ（存在する親のみで構築）
        // parent -> children
        private readonly Dictionary<T, HashSet<T>> _childrenByParent = new();
        // child -> parents
        private readonly Dictionary<T, HashSet<T>> _parentsByChild = new();

        // 変更起点（dirty）: ここから影響範囲を再計算する
        private readonly HashSet<T> _dirtyRoots = new();

        public StatusManager(StatDefinition<T> definition)
        {
            _definition = definition;
        }
        #region CRUD

        public void CreateStatus(T key, float initializeValue)
        {
            _statusHandlers[key] = _definition.CreateStatusHandler(key, initializeValue);
            _statusHandlers[key].StatusManager = this;
            RebuildGraphAndValidateNoCycles();

            // 新規作成は自身と子孫の整合性を取るため dirty にする
            MarkDirty(key);
        }

        public bool RemoveStatusHandler(T key)
        {
            // いなければ終了
            if (!_statusHandlers.ContainsKey(key))
                return false;

            List<T> childrenSnapshot = null;
            if (_childrenByParent.TryGetValue(key, out var children))
                childrenSnapshot = new List<T>(children);

            // 削除
            _statusHandlers.Remove(key);

            // dirty集合に消滅キーが残ると後で事故るので先に掃除
            _dirtyRoots.Remove(key);

            // グラフ再構築＆サイクル検証
            RebuildGraphAndValidateNoCycles();

            // 退避しておいた子（まだ存在するもの）を dirty にする
            if (childrenSnapshot != null)
            {
                foreach (var c in childrenSnapshot)
                    MarkDirty(c); // MarkDirty は子孫まで dirty にする
            }

            // 念押し：dirty集合に存在しないキーが紛れたら除去
            _dirtyRoots.RemoveWhere(k => !_statusHandlers.ContainsKey(k));

            return true;
        }

        public void ClearStatusHandlers()
        {
            _statusHandlers.Clear();
            _childrenByParent.Clear();
            _parentsByChild.Clear();
            _dirtyRoots.Clear();
        }

        public bool HasStatusHandler(T key) => _statusHandlers.ContainsKey(key);

        public bool TryGetStatusHandler(T key, out IStatusHandler<T> handler)
            => _statusHandlers.TryGetValue(key, out handler);

        public IStatusHandler<T> GetStatusHandler(T key)
        {
            if (_statusHandlers.TryGetValue(key, out var handler))
                return handler;

            throw new KeyNotFoundException($"StatusHandler for key '{key}' not found.");
        }

        #endregion

        #region Dirty / Notification

        /// <summary>
        /// 値変更が起きた（または起きうる）ステータスを通知する。
        /// 親が変われば子も再計算が必要なので、子孫までdirtyにする。
        /// </summary>
        public void MarkDirty(T key)
        {
            if (!_statusHandlers.ContainsKey(key))
                return;


            // 子孫展開しない。根だけ覚える（O(1)）
            _dirtyRoots.Add(key);
        }
        #endregion

        #region Recalculation

        /// <summary>
        /// dirty通知された範囲だけ、親→子の順で再計算する。
        /// サイクルは禁止なので、ここで詰まるなら設計ミスとして例外にする。
        /// </summary>
        public void RecalculateDirty()
        {
            if (_dirtyRoots.Count == 0) return;

            var affected = BuildAffectedClosure(_dirtyRoots);
            var order = TopologicalOrderWithin(affected);

            // 今回 RecalculateCurrent を実行したノード
            var recalculated = new HashSet<T>();

            foreach (var key in order)
            {
                if (!_statusHandlers.TryGetValue(key, out var handler))
                    continue;

                bool parentRecalculated = false;
                if (_parentsByChild.TryGetValue(key, out var parents))
                {
                    foreach (var p in parents)
                    {
                        if (recalculated.Contains(p))
                        {
                            parentRecalculated = true;
                            break;
                        }
                    }
                }

                // 再計算が必要な条件
                bool should =
                    _dirtyRoots.Contains(key) ||
                    handler.ShouldBeRecalculated ||
                    parentRecalculated;

                if (!should)
                    continue;

                handler.RecalculateCurrent();
                recalculated.Add(key);
            }

            _dirtyRoots.Clear();
        }

        /// <summary>
        /// 全ステータスを再計算（親→子）。分断グラフでも全ノードを処理する。
        /// </summary>
        public void RecalculateAll()
        {
            if (_statusHandlers.Count == 0) return;

            var all = new HashSet<T>(_statusHandlers.Keys);
            var order = TopologicalOrderWithin(all);

            foreach (var key in order)
                _statusHandlers[key].RecalculateCurrent();

            _dirtyRoots.Clear();
        }
        public void RecalculateParents(IStatusHandler<T> handler)
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            // handler -> key を逆引き（O(n)）
            var key = FindKeyByHandler(handler);

            // key の「祖先（親方向）」＋自分を集める
            var nodes = BuildAncestorsPlusSelf(key);
            nodes.Remove(key); // 自分自身は除外

            // 親→子の順に並べて再計算
            var order = TopologicalOrderWithin(nodes);

            foreach (var k in order)
                _statusHandlers[k].RecalculateCurrent();
        }
        public void RecalculateClosure(IStatusHandler<T> handler, bool includeDescendants)
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            var key = FindKeyByHandler(handler);

            var nodes = includeDescendants
                ? BuildAncestorsPlusSelfAndDescendants(key)
                : BuildAncestorsPlusSelf(key);

            var order = TopologicalOrderWithin(nodes);

            foreach (var k in order)
                _statusHandlers[k].RecalculateCurrent();
        }

        private HashSet<T> BuildAncestorsPlusSelfAndDescendants(T key)
        {
            var nodes = BuildAncestorsPlusSelf(key);

            var q = new Queue<T>(nodes); // 祖先＋自分を起点に子孫も拾う
            while (q.TryDequeue(out var cur))
            {
                if (_childrenByParent.TryGetValue(cur, out var children))
                {
                    foreach (var c in children)
                    {
                        if (!_statusHandlers.ContainsKey(c))
                            continue;

                        if (nodes.Add(c))
                            q.Enqueue(c);
                    }
                }
            }

            return nodes;
        }
        private HashSet<T> BuildAffectedClosure(HashSet<T> dirtyRoots)
        {
            var affected = new HashSet<T>();

            // まず dirtyRoots を起点にする
            var q = new Queue<T>();
            foreach (var d in dirtyRoots)
            {
                if (!_statusHandlers.ContainsKey(d)) continue;
                if (affected.Add(d)) q.Enqueue(d);
            }

            // 1) 子孫方向（parent -> children）に広げる
            while (q.TryDequeue(out var cur))
            {
                if (_childrenByParent.TryGetValue(cur, out var children))
                {
                    foreach (var c in children)
                    {
                        if (!_statusHandlers.ContainsKey(c)) continue;
                        if (affected.Add(c)) q.Enqueue(c);
                    }
                }
            }

            // 2) 祖先方向（child -> parents）にも広げる
            var q2 = new Queue<T>(affected);
            while (q2.TryDequeue(out var cur))
            {
                if (_parentsByChild.TryGetValue(cur, out var parents))
                {
                    foreach (var p in parents)
                    {
                        if (!_statusHandlers.ContainsKey(p)) continue;
                        if (affected.Add(p)) q2.Enqueue(p);
                    }
                }
            }

            return affected;
        }

        private List<T> TopologicalOrderWithin(HashSet<T> nodes)
        {
            // indegree = 親（nodes内）の数
            var indegree = new Dictionary<T, int>(nodes.Count);
            foreach (var n in nodes) indegree[n] = 0;

            foreach (var n in nodes)
            {
                if (_parentsByChild.TryGetValue(n, out var parents))
                {
                    foreach (var p in parents)
                    {
                        if (nodes.Contains(p))
                            indegree[n]++;
                    }
                }
            }

            var q = new Queue<T>();
            foreach (var kv in indegree)
                if (kv.Value == 0) q.Enqueue(kv.Key);

            var result = new List<T>(nodes.Count);

            while (q.TryDequeue(out var parent))
            {
                result.Add(parent);

                if (_childrenByParent.TryGetValue(parent, out var children))
                {
                    foreach (var child in children)
                    {
                        if (!nodes.Contains(child)) continue;

                        indegree[child]--;
                        if (indegree[child] == 0)
                            q.Enqueue(child);
                    }
                }
            }

            // サイクル禁止なので、ここで欠けるのは異常
            if (result.Count != nodes.Count)
                throw new InvalidOperationException("Cycle detected in status dependency graph (should have been prevented).");

            return result;
        }
        private T FindKeyByHandler(IStatusHandler<T> handler)
        {
            foreach (var kv in _statusHandlers)
            {
                if (ReferenceEquals(kv.Value, handler))
                    return kv.Key;
            }
            throw new KeyNotFoundException("The given handler is not managed by this StatusManager.");
        }

        private HashSet<T> BuildAncestorsPlusSelf(T key)
        {
            if (!_statusHandlers.ContainsKey(key))
                throw new KeyNotFoundException($"StatusHandler for key '{key}' not found.");

            var nodes = new HashSet<T> { key };
            var q = new Queue<T>();
            q.Enqueue(key);

            while (q.TryDequeue(out var cur))
            {
                if (!_parentsByChild.TryGetValue(cur, out var parents))
                    continue;

                foreach (var p in parents)
                {
                    if (!_statusHandlers.ContainsKey(p))
                        continue;

                    if (nodes.Add(p))
                        q.Enqueue(p);
                }
            }

            return nodes;
        }
        #endregion

        #region Graph Build + Cycle Detection

        private void RebuildGraphAndValidateNoCycles()
        {
            _childrenByParent.Clear();
            _parentsByChild.Clear();

            foreach (var childKey in _statusHandlers.Keys)
            {
                if (!_parentsByChild.ContainsKey(childKey))
                    _parentsByChild[childKey] = new HashSet<T>();
            }

            // ルールから辺を張る（親が存在するものだけ）
            foreach (var (childKey, handler) in _statusHandlers)
            {
                var rules = handler.ParentRules;
                for (int i = 0; i < rules.Count; i++)
                {
                    var parentKey = rules[i].Key;

                    // 親が存在しない場合は無視（仕様通り）
                    if (!_statusHandlers.ContainsKey(parentKey))
                        continue;

                    AddEdge(parentKey, childKey);
                }
            }

            ValidateNoCycles();
        }

        private void AddEdge(T parent, T child)
        {
            if (!_childrenByParent.TryGetValue(parent, out var children))
            {
                children = new HashSet<T>();
                _childrenByParent[parent] = children;
            }
            children.Add(child);

            if (!_parentsByChild.TryGetValue(child, out var parents))
            {
                parents = new HashSet<T>();
                _parentsByChild[child] = parents;
            }
            parents.Add(parent);
        }

        // DFS (white/gray/black) でサイクル検出
        private enum Mark : byte { White, Gray, Black }

        private void ValidateNoCycles()
        {
            var mark = new Dictionary<T, Mark>(_statusHandlers.Count);
            foreach (var key in _statusHandlers.Keys)
                mark[key] = Mark.White;

            foreach (var key in _statusHandlers.Keys)
            {
                if (mark[key] == Mark.White)
                {
                    if (HasCycleDfs(key, mark, new Stack<T>()))
                        throw new InvalidOperationException("Cycle dependency is not allowed in status rules.");
                }
            }
        }

        private bool HasCycleDfs(T node, Dictionary<T, Mark> mark, Stack<T> path)
        {
            mark[node] = Mark.Gray;
            path.Push(node);

            // node（子） -> parents を辿る（依存方向）
            if (_parentsByChild.TryGetValue(node, out var parents))
            {
                foreach (var p in parents)
                {
                    if (!_statusHandlers.ContainsKey(p)) continue;

                    if (mark[p] == Mark.Gray)
                        return true;

                    if (mark[p] == Mark.White)
                    {
                        if (HasCycleDfs(p, mark, path))
                            return true;
                    }
                }
            }

            path.Pop();
            mark[node] = Mark.Black;
            return false;
        }

        #endregion
    }
}