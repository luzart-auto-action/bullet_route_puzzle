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
            Debug.Log($"[UIManager] Awake() - {_panels.Count} panels");
            foreach (var panel in _panels)
            {
                if (panel != null)
                {
                    _panelMap[panel.PanelName] = panel;
                    panel.gameObject.SetActive(false);
                    Debug.Log($"[UIManager] Registered panel: '{panel.PanelName}'");
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

        /// <summary>
        /// Immediately deactivate all panels without animation.
        /// Used when transitioning between major states (Win->Loading, Fail->Loading).
        /// </summary>
        public void ForceHideAll()
        {
            foreach (var panel in _panelMap.Values)
            {
                if (panel == null) continue;
                DOTween.Kill(panel.transform);
                var cg = panel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    DOTween.Kill(cg);
                    cg.alpha = 0f;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }
                panel.gameObject.SetActive(false);
            }
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
