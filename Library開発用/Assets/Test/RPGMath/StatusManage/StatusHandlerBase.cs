using System;
using System.Collections.Generic;
using System.Linq;

namespace HighElixir.RPGMath.StatusManage
{
    public class StatusHandlerBase<T> : IStatusHandler<T>
        where T : IEquatable<T>
    {
        private float _current;
        private List<ParentRule<T>> _parentRules = new();
        public float Base { get; protected set; }
        public float Current
        {
            get => _current;
            protected set => _current = value;
        }

        public float FixedAmount { get; protected set; } = float.NaN;

        // 一時ステータスの増減要素が追加できたときに使用する予定
        // フラグの管理は継承されたCurrent操作メソッドで行う想定
        public bool ShouldBeRecalculated { get; protected set; } = false;

        public StatusManager<T> StatusManager { get; set; }

        public IReadOnlyList<ParentRule<T>> ParentRules => _parentRules.AsReadOnly();
        
        public virtual void Initialize(float initialValue, params ParentRule<T>[] rules)
        {
            Base = initialValue;
            Current = initialValue;
            _parentRules = rules.ToList();
        }
        public virtual void RecalculateCurrent()
        {
            // 実装中
            var rules = ParentRules;

            if (rules.Count > 0)
            {
                StatusManager.RecalculateParents(this);
                foreach (var rule in rules)
                {
                    if (!StatusManager.TryGetStatusHandler(rule.Key, out var status))
                        continue;
                }
            }
            ShouldBeRecalculated = false;
        }
        public virtual void Increase(float value)
        {
            Current += value;
            ShouldBeRecalculated = true;
        }
        public virtual void Decrease(float value)
        {
            Current -= value;
            ShouldBeRecalculated = true;
        }
        public virtual void Set(float value)
        {
            Current = value;
            ShouldBeRecalculated = true;
        }
        public virtual void ForceSet(float value)
        {
            FixedAmount = value;
        }
        public virtual void ResetToBase()
        {
            Current = Base;
            ShouldBeRecalculated = true;
        }
        public virtual void UpdateBase(float newValue)
        {
            Base = newValue;
        }
    }
}