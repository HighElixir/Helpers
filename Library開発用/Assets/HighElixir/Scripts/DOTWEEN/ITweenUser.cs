using DG.Tweening;
using System;
using UnityEngine;

namespace HighElixir.Tweenworks
{
    // DoTweenの動作をインスペクターからカスタマイズ可能にするためのインターフェース
    public interface ITweenUser : IDisposable
    {
        Tween Invoke();
        void Bind(GameObject target);
    }
}