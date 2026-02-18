using System;
using Cysharp.Threading.Tasks;
using HighElixir.HESceneManager.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace HighElixir.HESceneManager.Samples
{
    /// <summary>
    /// uGUI Button でシーンを切り替える簡易サンプル。
    /// 使い方:
    /// - アクティブシーン内の GameObject に本コンポーネントをアタッチ
    /// - Inspector で Button 参照を指定（未指定なら同一 GameObject の Button を利用）
    /// - ロードしたい Addressable 名を設定
    /// - 実行中のアクティブシーンはコンストラクタでレジストリ登録される
    /// ボタン押下で LoadSceneAsync → ActivateAsync → 旧シーン UnloadAsync(disposeResource:true) の順に処理する。
    /// </summary>
    public sealed class ButtonSceneSwitcher : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private string _addressableName = "";

        private SceneService _sceneService;
        private bool _isSwitching;

        private void Awake()
        {
            // SceneService 生成時にアクティブシーンがレジストリへ登録される。
            _sceneService = new SceneService();
            _sceneService.GetOrRegisterActiveSceneData();
        }

        private void OnEnable()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button != null)
                _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }

        /// <summary>
        /// UnityEvent から直接呼びたい場合用の公開メソッド。
        /// </summary>
        public void SwitchScene() => OnClick();

        private async void OnClick()
        {
            if (_isSwitching) return;

            if (string.IsNullOrWhiteSpace(_addressableName))
            {
                Debug.LogWarning($"[{nameof(ButtonSceneSwitcher)}] Addressable 名が空だよ。設定してから押してくれ。");
                return;
            }

            _isSwitching = true;

            try
            {
                // 参照保持しておき、次シーンがアクティブ化されたら破棄する。
                var current = _sceneService.Registry.CurrentScene;
                if (current == null || current.IsInvalid())
                    current = _sceneService.GetOrRegisterActiveSceneData();

                var next = await _sceneService.LoadSceneAsync(_addressableName);
                if (next.IsInvalid())
                    return;

                await _sceneService.ActivateAsync(next);

                if (current != null && !current.IsInvalid() && !ReferenceEquals(current, next))
                {
                    await _sceneService.UnloadAsync(current, disposeResource: true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(ButtonSceneSwitcher)}] シーン切り替えに失敗: {ex.Message}");
            }
            finally
            {
                _isSwitching = false;
            }
        }
    }
}
