using UnityEngine;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Wiring targets for the CROWS home hub (Aria MainMenuItem tabs + buttons).
    /// Toggle tabs call the TabX(bool) methods; buttons call the zero-arg ones.
    /// Also hides the hub's center content while a kit overlay panel is open.
    /// </summary>
    public class CrowsMenuActions : MonoBehaviour
    {
        public CanvasGroup center_group; // assigned by the skin bootstrap (center content only — tabs stay)

        void Update()
        {
            if (center_group == null)
                return;

            bool overlay = IsVisible(CollectionPanel.Get())
                || IsVisible(SettingsPanel.Get())
                || IsVisible(AdventurePanel.Get())
                || IsVisible(PackPanel.Get())
                || IsVisible(StarterDeckPanel.Get());

            float alpha = overlay ? 0f : 1f;
            if (!Mathf.Approximately(center_group.alpha, alpha))
            {
                center_group.alpha = alpha;
                center_group.interactable = !overlay;
                center_group.blocksRaycasts = !overlay;
            }
        }

        private bool IsVisible(UIPanel panel)
        {
            return panel != null && panel.IsVisible();
        }

        private void HideOverlays()
        {
            if (CollectionPanel.Get() != null && CollectionPanel.Get().IsVisible()) CollectionPanel.Get().Hide(false);
            if (SettingsPanel.Get() != null && SettingsPanel.Get().IsVisible()) SettingsPanel.Get().Hide(false);
            if (AdventurePanel.Get() != null && AdventurePanel.Get().IsVisible()) AdventurePanel.Get().Hide(false);
            if (PackPanel.Get() != null && PackPanel.Get().IsVisible()) PackPanel.Get().Hide(false);
        }

        // ---- tab targets (Aria MainMenuItem = Toggle) ----

        public void TabHome(bool on)
        {
            if (on) HideOverlays();
        }

        public void TabDecks(bool on)
        {
            if (on) { HideOverlays(); CollectionPanel.Get().Show(false); }
        }

        public void TabCollection(bool on)
        {
            if (on) { HideOverlays(); CollectionPanel.Get().Show(false); }
        }

        public void TabTrials(bool on)
        {
            if (on) { HideOverlays(); MainMenu.Get().OnClickAdventure(); }
        }

        // ---- button targets ----

        public void PlaySolo()
        {
            MainMenu.Get().OnClickSolo();
        }

        public void ShowSettings()
        {
            MainMenu.Get().OnClickSettings();
        }
    }
}
