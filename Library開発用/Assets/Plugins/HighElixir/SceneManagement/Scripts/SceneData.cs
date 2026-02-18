using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using HighElixir.HESceneManager.Utils;

namespace HighElixir.HESceneManager
{
    public sealed class SceneData : IEquatable<SceneData>
    {
        public Scene Scene { get; internal set; } = Invalids.InvalidScene;
        public SceneInstance Instance { get; internal set; } = Invalids.InvalidSceneInstance;

        public bool IsPersisted { get; internal set; }
        public bool ShouldBeUnloaded { get; set; }

        // 互換レイヤーのキー（Registryはこれをキーにする）
        public string CompatID { get; internal set; } = "";

        // Addressablesでロードした時の「アドレス文字列」などを保持したい場合に使う
        public string AddressableName { get; internal set; } = "";

        // Scene.name を優先。Sceneが無効でも Instance.Scene が使えれば拾う
        public string SceneName
        {
            get
            {
                if (!Scene.IsInvalid()) return Scene.name;
                if (!Instance.IsInvalid()) return Instance.Scene.name;
                return "";
            }
        }

        // Registryが最後に使ったキー（Unregisterの安定化用）
        internal string RegistryKey { get; set; } = "";

        public SceneData(SceneInstance instance, bool isPersisted, string addressableName = "")
            : this(instance.Scene, isPersisted, addressableName)
        {
            Instance = instance;
        }

        public SceneData(Scene scene, bool isPersisted, string addressableName = "")
        {
            Scene = scene;
            IsPersisted = isPersisted;
            AddressableName = addressableName ?? "";
            ShouldBeUnloaded = false;

            EnsureCompatID(); // 取れる情報だけで仮ID生成 or Link
        }

        /// <summary>
        /// 持っている情報から CompatID を確定/更新する。
        /// - AddressableName と SceneName 両方あるなら Link で統合ID確定
        /// - 片方しかないなら その片側のID（仮）を使う
        /// </summary>
        internal string EnsureCompatID()
        {
            var ad = AddressableName;
            var sn = SceneName;

            if (!string.IsNullOrEmpty(ad) && !string.IsNullOrEmpty(sn))
            {
                CompatID = AddressablesCompat.Link(ad, sn);
                return CompatID;
            }

            if (string.IsNullOrEmpty(CompatID))
            {
                if (!string.IsNullOrEmpty(ad)) CompatID = AddressablesCompat.GetIDFromAddressable(ad);
                else if (!string.IsNullOrEmpty(sn)) CompatID = AddressablesCompat.GetIDFromSceneName(sn);
            }

            return CompatID;
        }

        /// <summary>
        /// 後からAddressableNameが判明した時などに呼ぶ。
        /// Linkにより CompatID が変わる可能性がある（= Registry側はReKeyが必要）
        /// </summary>
        internal void SetAddressableName(string addressableName)
        {
            AddressableName = addressableName ?? "";
            EnsureCompatID();
        }

        internal async UniTask<bool> ActivateAsync()
        {
            if (!Instance.IsInvalid())
            {
                var op = Instance.ActivateAsync();
                await op.ToUniTask();
                if (op.isDone)
                {
                    SceneManager.SetActiveScene(Instance.Scene);
                    return true;
                }
            }
            if (!Scene.IsInvalid())
            {
                SceneManager.SetActiveScene(Scene);
                return true;
            }
            return false;
        }

        internal async UniTask UnloadAsync()
        {
            if (!Instance.IsInvalid())
                await Addressables.UnloadSceneAsync(Instance);
            else if (!Scene.IsInvalid())
                await SceneManager.UnloadSceneAsync(Scene).ToUniTask();
        }

        public bool IsInvalid() => Instance.IsInvalid() && Scene.IsInvalid();

        public static SceneData InvalidData() => new(Invalids.InvalidScene, false);

        public override bool Equals(object obj) => Equals(obj as SceneData);

        public override int GetHashCode() => HashCode.Combine(Scene, Instance);

        public bool Equals(SceneData other)
        {
            return other != null &&
                   EqualityComparer<Scene>.Default.Equals(Scene, other.Scene) &&
                   EqualityComparer<SceneInstance>.Default.Equals(Instance, other.Instance) &&
                   IsPersisted == other.IsPersisted;
        }

        public static bool operator ==(SceneData left, SceneData right)
        {
            return EqualityComparer<SceneData>.Default.Equals(left, right);
        }

        public static bool operator !=(SceneData left, SceneData right)
        {
            return !(left == right);
        }
    }
}
