using HighElixir.UI.Countable;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace HighElixir.UI
{
    /// <summary>
    /// カウンタブルに追加できる。自動でpageの数で範囲を制限する
    /// </summary>
    public class ActivePage : MonoBehaviour
    {
        [SerializeField] private CountableSwitch _countable;
        [SerializeField] private bool _sendMessage = false;
        [SerializeField] private List<GameObject> _pages = new List<GameObject>();

        public void AddPage(GameObject page)
        {
            _pages.Add(page);
            _countable.Max = _pages.Count == 0 ? 1 : _pages.Count;
        }
        public void RemovePage(GameObject page)
        {
            if (_pages.Remove(page))
                _countable.Max = _pages.Count == 0 ? 1 : _pages.Count;
        }
        // Unity
        private void OnValidate()
        {
            _countable.Min = 1;
            _countable.Max = _pages.Count == 0 ? 1 : _pages.Count;
        }

        private void Awake()
        {
            _countable = GetComponent<CountableSwitch>();
            _countable.OnChanged.AddListener(s =>
            {
                var page = s;
                for (int i = 0; i < _pages.Count; i++)
                {
                    if (i + 1 == page)
                    {
                        if (_sendMessage)
                        {
                            ExecuteEvents.Execute<IPageReceiver>
                            (
                                target: _pages[i],
                                eventData: null,
                                functor: (reciever, eventData) => reciever.ActivePage()
                            );
                        }
                        else
                            _pages[i].SetActive(true);
                    }
                    else
                    {
                        if (_sendMessage)
                        {

                            ExecuteEvents.Execute<IPageReceiver>
                            (
                                target: _pages[i],
                                eventData: null,
                                functor: (reciever, eventData) => reciever.InactivePage()
                            );
                        }
                        else
                            _pages[i].SetActive(false);
                    }
                }
            });
            _countable.Value = 1;
        }
    }

    public interface IPageReceiver : IEventSystemHandler
    {
        void ActivePage();
        void InactivePage();
    }
}