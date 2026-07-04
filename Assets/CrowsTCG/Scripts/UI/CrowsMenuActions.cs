using UnityEngine;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Zero-arg wiring targets for the CROWS home hub buttons
    /// (persistent UnityEvents can't bind methods with optional params).
    /// </summary>
    public class CrowsMenuActions : MonoBehaviour
    {
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
