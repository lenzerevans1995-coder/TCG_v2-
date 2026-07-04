using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using TcgEngine.Client;

namespace CrowsTCG.EditorTools
{
    /// <summary>
    /// Rebuilds the kit Menu scene's visual layer THE ARIA WAY (per AriaDemoScene):
    /// - navigation = horizontal bar of MainMenuItem toggle prefabs + ButtonIcon ends + Separator
    /// - content = Spacer | VerticalLayout | Spacer columns of instantiated Aria prefabs
    /// - labels come from the Prefabs/Texts prefabs, composed as children
    /// No hand-built imagery except the Aria Background. Idempotent.
    /// </summary>
    public static class CrowsMenuSkin
    {
        const string ARIA = "Assets/Honeti/AriaGUI/";
        const string SCENE = "Assets/TcgEngine/Scenes/Menu/Menu.unity";
        static readonly Color GOLD = new Color(0.788f, 0.659f, 0.298f, 1f);

        [MenuItem("CROWS/Skin Menu Scene")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(SCENE);
            var canvas = GameObject.Find("UICanvas");
            if (canvas == null) { Debug.LogError("CROWS: UICanvas not found"); return; }

            // ---- idempotency ----
            Kill(canvas.transform, "CrowsBackground");
            var playPanel = canvas.transform.Find("PlayPanel");
            if (playPanel != null) Kill(playPanel, "CrowsHome");

            // ---- Aria backdrop ----
            var bg = MakeImage(canvas.transform, "CrowsBackground", Sprite(ARIA + "Textures/Misc/Background.png"), Color.white);
            Stretch(bg.rectTransform);
            bg.transform.SetSiblingIndex(0);
            bg.raycastTarget = false;

            // ---- kit chrome hidden but alive (SetActive(false) breaks kit singletons) ----
            var kitTopBar = canvas.transform.Find("TopBar");
            if (kitTopBar != null) HideKeepAlive(kitTopBar.gameObject);

            if (playPanel != null)
            {
                foreach (Transform child in playPanel)
                {
                    if (child.name == "CrowsHome")
                        continue;
                    if (child.GetComponent<TcgEngine.UI.UIPanel>() == null)
                        HideKeepAlive(child.gameObject);
                    else
                        child.gameObject.SetActive(true);
                }

                var home = new GameObject("CrowsHome", typeof(RectTransform));
                home.transform.SetParent(playPanel, false);
                Stretch(home.GetComponent<RectTransform>());
                var actions = home.AddComponent<CrowsMenuActions>();

                BuildTabBar(home.transform, actions);
                BuildCenter(home.transform, actions);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("CROWS: menu skin applied (Aria prefab composition) + scene saved");
        }

        // ============ Aria-style top tab bar: MainMenuItem row + ButtonIcon ends + Separator ============

        static void BuildTabBar(Transform parent, CrowsMenuActions actions)
        {
            var bar = new GameObject("TabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ToggleGroup));
            bar.transform.SetParent(parent, false);
            var rt = (RectTransform)bar.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(0, -96); rt.offsetMax = new Vector2(0, 0);

            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 10;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            var group = bar.GetComponent<ToggleGroup>();

            // left end: crown = CROWS emblem (decorative ButtonIcon)
            var logo = InstantiatePrefab(ARIA + "Prefabs/Buttons/ButtonIcon.prefab", bar.transform, "Logo");
            SetIconImages(logo, ARIA + "Textures/Icons/64/Crown.png");
            SetText(logo, "", null); // clear the prefab's default shortcut label

            // tabs, exactly like AriaDemoScene UIMenu/MainMenu/Layout — native text labels
            AddTab(bar.transform, group, actions, "TabHome", "HOME", true);
            AddTab(bar.transform, group, actions, "TabDecks", "DECKS", false);
            AddTab(bar.transform, group, actions, "TabCollection", "COLLECTION", false);
            AddTab(bar.transform, group, actions, "TabTrials", "TRIALS", false);

            // right end: settings ButtonIcon
            var settings = InstantiatePrefab(ARIA + "Prefabs/Buttons/ButtonIcon.prefab", bar.transform, "SettingsButton");
            SetIconImages(settings, ARIA + "Textures/Icons/64/Settings.png");
            SetText(settings, "", null);
            var sbtn = settings.GetComponent<Button>();
            if (sbtn != null) Wire(sbtn.onClick, actions, "ShowSettings");

            // separator under the bar, Aria's own
            var sep = MakeImage(parent, "Separator", Sprite(ARIA + "Textures/Panels/Separator.png"), Color.white);
            var srt = sep.rectTransform;
            srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(40, -102); srt.offsetMax = new Vector2(-40, -96);
            sep.raycastTarget = false;
        }

        static void AddTab(Transform bar, ToggleGroup group, CrowsMenuActions actions, string method, string label, bool isOn)
        {
            var item = InstantiatePrefab(ARIA + "Prefabs/Buttons/MainMenuItem.prefab", bar, method);
            SetText(item, label, null); // use the prefab's own label
            var tmp = item.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.textWrappingMode = TextWrappingModes.NoWrap;
            var le = item.GetComponent<LayoutElement>();
            if (le == null) le = item.AddComponent<LayoutElement>();
            le.preferredWidth = 210; le.preferredHeight = 64;
            var toggle = item.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.group = group;
                toggle.SetIsOnWithoutNotify(isOn);
                var action = (UnityEngine.Events.UnityAction<bool>)System.Delegate.CreateDelegate(
                    typeof(UnityEngine.Events.UnityAction<bool>), actions, method);
                UnityEventTools.AddPersistentListener(toggle.onValueChanged, action);
            }
        }

        // ============ center content: Spacer | Layout | Spacer, prefab-composed ============

        static void BuildCenter(Transform parent, CrowsMenuActions actions)
        {
            var center = new GameObject("Center", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(CanvasGroup));
            center.transform.SetParent(parent, false);
            var crt = (RectTransform)center.transform;
            crt.anchorMin = new Vector2(0f, 0f); crt.anchorMax = new Vector2(1f, 1f);
            crt.offsetMin = new Vector2(0, 40); crt.offsetMax = new Vector2(0, -110);

            var hlg = center.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

            actions.center_group = center.GetComponent<CanvasGroup>();

            MakeSpacer(center.transform, 1f);
            var layout = new GameObject("Layout", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            layout.transform.SetParent(center.transform, false);
            layout.GetComponent<LayoutElement>().preferredWidth = 640;
            var vlg = layout.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 22;
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            MakeSpacer(center.transform, 1f);

            // title from Aria's text prefab, recolored gold only
            var title = InstantiatePrefab(ARIA + "Prefabs/Texts/Text (TMP)Title.prefab", layout.transform, "Title");
            SetText(title, "CROWS", GOLD);

            var tagline = InstantiatePrefab(ARIA + "Prefabs/Texts/Text (TMP)Body.prefab", layout.transform, "Tagline");
            SetText(tagline, "A trading card game of grim fortunes", null);

            // PLAY = Aria primary green button + Aria heading label composed in (the Aria way)
            var play = InstantiatePrefab(ARIA + "Prefabs/Buttons/ButtonPrimaryGreen.prefab", layout.transform, "PlayButton");
            var ple = play.GetComponent<LayoutElement>();
            if (ple == null) ple = play.AddComponent<LayoutElement>();
            ple.preferredHeight = 92; ple.preferredWidth = 320;
            SetText(play, "PLAY", null); // the prefab ships its own label
            var pbtn = play.GetComponent<Button>();
            if (pbtn != null) Wire(pbtn.onClick, actions, "PlaySolo");
        }

        // ================= helpers =================

        static GameObject InstantiatePrefab(string path, Transform parent, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogError("CROWS: prefab missing " + path); return new GameObject(name, typeof(RectTransform)); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            return go;
        }

        /// Swap a ButtonIcon's glyph: every child Image except the frame root and Selection glow.
        static void SetIconImages(GameObject item, string iconPath)
        {
            var icon = Sprite(iconPath);
            if (icon == null) { Debug.LogWarning("CROWS: icon missing " + iconPath); return; }
            foreach (var img in item.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject.name.Contains("Selection"))
                    continue;
                img.sprite = icon; // ButtonIcon is a single root Image = the glyph itself
                img.preserveAspect = true;
            }
        }

        static void SetText(GameObject go, string text, Color? color)
        {
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp == null)
            {
                // some prefabs (ButtonIcon shortcut badge) use legacy Text
                var legacy = go.GetComponentInChildren<Text>(true);
                if (legacy != null) legacy.text = text;
                return;
            }
            tmp.text = text;
            if (color.HasValue) tmp.color = color.Value;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        static void MakeSpacer(Transform parent, float flex)
        {
            var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().flexibleWidth = flex;
        }

        static void StretchChild(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void HideKeepAlive(GameObject go)
        {
            go.SetActive(true);
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        static void Kill(Transform parent, string name)
        {
            var found = parent.Find(name);
            if (found != null) Object.DestroyImmediate(found.gameObject);
        }

        static Image MakeImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        static void Wire(UnityEngine.Events.UnityEvent evt, CrowsMenuActions target, string method)
        {
            var action = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction), target, method);
            UnityEventTools.AddPersistentListener(evt, action);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static Sprite Sprite(string path) { return AssetDatabase.LoadAssetAtPath<Sprite>(path); }
    }
}
