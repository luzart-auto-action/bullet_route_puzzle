using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BulletRoute.Core;

namespace BulletRoute.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private List<UIPanel> _panels = new List<UIPanel>();

        private Dictionary<string, UIPanel> _panelMap = new Dictionary<string, UIPanel>();

        private void Awake()
        {
            foreach (var panel in _panels)
            {
                if (panel != null)
                {
                    _panelMap[panel.PanelName] = panel;
                    panel.gameObject.SetActive(false);
                }
            }
            ServiceLocator.Register(this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ShowPanelEvent>(OnShowPanel);
            EventBus.Subscribe<HidePanelEvent>(OnHidePanel);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ShowPanelEvent>(OnShowPanel);
            EventBus.Unsubscribe<HidePanelEvent>(OnHidePanel);
        }

        private void OnShowPanel(ShowPanelEvent evt)
        {
            ShowPanel(evt.PanelName);
        }

        private void OnHidePanel(HidePanelEvent evt)
        {
            HidePanel(evt.PanelName);
        }

        public void ShowPanel(string name)
        {
            if (_panelMap.TryGetValue(name, out var panel))
                panel.Show();
        }

        public void HidePanel(string name)
        {
            if (_panelMap.TryGetValue(name, out var panel))
                panel.Hide();
        }

        public void HideAll()
        {
            foreach (var panel in _panelMap.Values)
                panel.Hide();
        }

        public T GetPanel<T>(string name) where T : UIPanel
        {
            if (_panelMap.TryGetValue(name, out var panel))
                return panel as T;
            return null;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<UIManager>();
        }
    }
}
