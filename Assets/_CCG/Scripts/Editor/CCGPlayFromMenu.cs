using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CCG.EditorTools
{
    /// <summary>
    /// Pressing Play boots the full game flow (LoginMenu -> Menu -> match) no
    /// matter which scene is open in the editor.
    /// Toggle via menu: CCG / Play From Menu. Turn it off when iterating on the
    /// board scene directly.
    /// </summary>
    [InitializeOnLoad]
    public static class CCGPlayFromMenu
    {
        private const string PREF = "ccg_play_from_menu";
        private const string MENU = "CCG/Play From Menu";
        private const string SCENE = "Assets/TcgEngine/Scenes/Menu/LoginMenu.unity";

        static CCGPlayFromMenu()
        {
            EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            bool on = EditorPrefs.GetBool(PREF, true);
            Menu.SetChecked(MENU, on);
            EditorSceneManager.playModeStartScene = on
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE)
                : null;
        }

        [MenuItem(MENU)]
        private static void Toggle()
        {
            EditorPrefs.SetBool(PREF, !EditorPrefs.GetBool(PREF, true));
            Apply();
        }
    }
}
