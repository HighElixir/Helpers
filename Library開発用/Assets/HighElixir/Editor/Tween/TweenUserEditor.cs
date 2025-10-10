using Cysharp.Threading.Tasks;
using HighElixir.Tweenworks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HighElixir.Editors
{
    [CustomEditor(typeof(TweenHolderMono))]
    public class TweenUserEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var holder = (TweenHolderMono)target;

            var space = new VisualElement();
            space.style.height = 6;
            space.style.backgroundColor = Color.clear;

            // プロファイル一覧
            var label = new Label("🎬 Tween Profiles");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(label);

            root.Add(space);

            var profilerNames = holder.GetProfierName();
            if (profilerNames.Count == 0)
            {
                root.Add(new Label("（登録されたProfilerがありません）"));
            }
            else
            {
                var fold = new Foldout();
                var scroll = new Scroller();
                fold.Add(scroll);
                foreach (var name in profilerNames)
                {
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                    var nameLabel = new Label(name);
                    nameLabel.style.width = 150;

                    var playBtn = new Button(() =>
                    {
                        if (Application.isPlaying)
                        {
                            holder.Invoke(name, t => Debug.Log($"[{name}] Completed: {t}")).Forget();
                        }
                        else
                        {
                            Debug.LogWarning("再生はPlayモードでのみ有効です。");
                        }
                    })
                    {
                        text = "▶ Play"
                    };

                    var stopBtn = new Button(() =>
                    {
                        if (Application.isPlaying)
                            holder.Stop(name);
                    })
                    {
                        text = "⏹ Stop"
                    };

                    row.Add(nameLabel);
                    row.Add(playBtn);
                    row.Add(stopBtn);
                    scroll.Add(row);
                }
                root.Add(fold);
            }

            // 区切り線
            var line = new VisualElement();
            line.style.height = 2;
            line.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            root.Add(line);

            root.Add(space);

            // デフォルトのインスペクタを追加
            var defaultInspector = new IMGUIContainer(() => DrawDefaultInspector());
            root.Add(defaultInspector);

            return root;
        }
    }
}
