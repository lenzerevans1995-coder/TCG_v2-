using UnityEngine;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Zero-arg wiring targets for the CROWS home hub buttons
    /// (persistent UnityEvents can't bind methods with optional params).
    /// Also hides the hub while any kit overlay panel is open.
    /// </summary>
    public class CrowsMenuActions : MonoBehaviour
    {
        private CanvasGroup canvas_group;

        void Awake()
        {
            canvas_group = GetComponent<CanvasGroup>();
            if (canvas_group == null)
                canvas_group = gameObject.AddComponent<CanvasGroup>();
        }

        void Update()
        {
            bool overlay = IsVisible(CollectionPanel.Get())
                || IsVisible(SettingsPanel.Get())
                || IsVisible(AdventurePanel.Get())
                || IsVisible(PackPanel.Get())
                || IsVisible(StarterDeckPanel.Get());

            float alpha = overlay ? 0f : 1f;
            if (!Mathf.Approximately(canvas_group.alpha, alpha))
            {
                canvas_group.alpha = alpha;
                canvas_group.interactable = !overlay;
                canvas_group.blocksRaycasts = !overlay;
            }
        }

        private bool IsVisible(UIPanel panel)
        {
            return panel != null && panel.IsVisible();
        }

        public void PlaySolo()
        {
            MainMenu.Get().OnClickSolo();
        }

        public void ShowCollection()
        {
            CollectionPanel.Get().Show(false);
        }

        public void ShowAdventure()
        {
            MainMenu.Get().OnClickAdventure();
        }

        public void ShowSettings()
        {
            MainMenu.Get().OnClickSettings();
        }
    }
}
