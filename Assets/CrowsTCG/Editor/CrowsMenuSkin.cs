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
    /// Rebuilds the kit Menu scene's visual layer per Docs/Design/CROWS/mockups/crows_ui_shell.svg
    /// using Aria GUI's NATIVE fantasy art (velvet+gold buttons, textured backdrop, gold frames).
    /// RULES: colored Aria sprites stay white-tinted (dark multiply kills them); only Aria's
    /// WHITE tintable sprites (SimplePanel/SectionBig/Badge/PanelCaro) get palette tints.
    /// Idempotent: re-running deletes and rebuilds the CROWS layer.
    /// </summary>
    public static class CrowsMenuSkin
    {
        static readonly Color TINT_PANEL = Hex("#23282c");   // stone tint for white tintable sprites
        static readonly Color TINT_BAR = Hex("#2c3236");
        static readonly Color GOLD = Hex("#c9a84c");
        static readonly Color CREAM = Hex("#e8dcc0");        // label color on velvet buttons
        static readonly Color TEXT_DIM = Hex("#9aa4a8");

        const string ARIA = "Assets/Honeti/AriaGUI/";
        const string SCENE = "Assets/TcgEngine/Scenes/Menu/Menu.unity";

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

            // ---- Aria textured backdrop, first sibling ----
            var bg = MakeImage(canvas.transform, "CrowsBackground", Sprite(ARIA + "Textures/Misc/Background.png"), Color.white);
            Stretch(bg.rectTransform);
            bg.type = Image.Type.Simple;
            bg.transform.SetSiblingIndex(0);
            bg.raycastTarget = false;

            // ---- kit chrome off: hide with transparent CanvasGroup, NEVER SetActive(false) —
            // inactive objects skip Awake, their singletons never register, MainMenu NREs ----
            var kitTopBar = canvas.transform.Find("TopBar");
            if (kitTopBar != null) HideKeepAlive(kitTopBar.gameObject);

            // ---- home hub over PlayPanel ----
            if (playPanel != null)
            {
                foreach (Transform child in playPanel)
                {
                    if (child.name == "CrowsHome")
                        continue;
                    // UIPanel children manage their own visibility; hide only plain containers
                    if (child.GetComponent<TcgEngine.UI.UIPanel>() == null)
                        HideKeepAlive(child.gameObject);
                    else
                        child.gameObject.SetActive(true);
                }

                var home = new GameObject("CrowsHome", typeof(RectTransform));
                home.transform.SetParent(playPanel, false);
                Stretch(home.GetComponent<RectTransform>());
                var actions = home.AddComponent<CrowsMenuActions>();

                BuildTopbar(home.transform, actions);
                BuildHero(home.transform, actions);
                BuildNav(home.transform, actions);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("CROWS: menu skin applied + scene saved");
        }

        // ================= sections =================

        static void BuildTopbar(Transform parent, CrowsMenuActions actions)
        {
            // SectionBig is a WHITE tintable band with hairline edges — made for this
            var bar = MakeImage(parent, "Topbar", Sprite(ARIA + "Textures/Panels/SectionBig.png"), TINT_BAR);
            Anchor(bar.rectTransform, 0f, 1f, 1f, 1f, new Vector2(0, -72), new Vector2(0, -8));
            bar.type = Image.Type.Sliced;
            bar.raycastTarget = false;

            var title = MakeText(bar.transform, "Title", "CROWS", 34, GOLD, EconomicaBold());
            Anchor(title.rectTransform, 0f, 0f, 0f, 1f, new Vector2(28, 0), new Vector2(260, 0));
            title.alignment = TextAlignmentOptions.MidlineLeft;

            var settings = MakeAriaButton(bar.transform, "SettingsButton", "", "ButtonBrown");
            Anchor(((RectTransform)settings.transform), 1f, 0.5f, 1f, 0.5f, new Vector2(-72, -24), new Vector2(-20, 24));
            var gear = MakeImage(settings.transform, "Icon", Sprite(ARIA + "Textures/Icons/64/Settings.png"), CREAM);
            Anchor(gear.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(-14, -14), new Vector2(14, 14));
            gear.raycastTarget = false;
            Wire(settings, actions, "ShowSettings");
        }

        static void BuildHero(Transform parent, CrowsMenuActions actions)
        {
            // stone-tinted panel with a real gold frame over it — the Aria signature
            var hero = MakeImage(parent, "HeroPanel", Sprite(ARIA + "Textures/Panels/SimplePanel.png"), TINT_PANEL);
            Anchor(hero.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(-470, -180), new Vector2(470, 240));
            hero.type = Image.Type.Sliced;

            var frame = MakeImage(hero.transform, "GoldFrame", Sprite(ARIA + "Textures/Panels/FrameGold.png"), Color.white);
            Stretch(frame.rectTransform);
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;

            var hint = MakeText(hero.transform, "ArtHint", "[ key art ]", 18, TEXT_DIM, LatoRegular());
            Anchor(hint.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(-200, 30), new Vector2(200, 90));
            hint.alignment = TextAlignmentOptions.Center;

            var play = MakeAriaButton(hero.transform, "PlayButton", "PLAY", "ButtonGreen");
            Anchor(((RectTransform)play.transform), 0.5f, 0f, 0.5f, 0f, new Vector2(-150, 28), new Vector2(150, 104));
            Wire(play, actions, "PlaySolo");
        }

        static void BuildNav(Transform parent, CrowsMenuActions actions)
        {
            string[] names = { "DECKS", "COLLECTION", "TRIALS" };
            string[] methods = { "ShowCollection", "ShowCollection", "ShowAdventure" };
            for (int i = 0; i < 3; i++)
            {
                var b = MakeAriaButton(parent, "Nav" + names[i], names[i], "ButtonBrown");
                var rt = (RectTransform)b.transform;
                Anchor(rt, 0.5f, 0f, 0.5f, 0f, new Vector2(-470 + i * 325, 22), new Vector2(-160 + i * 325, 86));
                Wire(b, actions, methods[i]);
            }
        }

        // ================= helpers =================

        /// Aria velvet+gold button at native colors with Hover/Down sprite swaps.
        static Button MakeAriaButton(Transform parent, string name, string label, string baseName)
        {
            var img = MakeImage(parent, name, Sprite(ARIA + "Textures/Buttons/" + baseName + ".png"), Color.white);
            img.type = Image.Type.Sliced;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            var hover = Sprite(ARIA + "Textures/Buttons/" + baseName + "Hover.png");
            var down = Sprite(ARIA + "Textures/Buttons/" + baseName + "Down.png");
            if (hover != null || down != null)
            {
                btn.transition = Selectable.Transition.SpriteSwap;
                var state = btn.spriteState;
                state.highlightedSprite = hover;
                state.pressedSprite = down != null ? down : hover;
                btn.spriteState = state;
            }

            if (!string.IsNullOrEmpty(label))
            {
                var txt = MakeText(img.transform, "Label", label, 26, CREAM, EconomicaBold());
                Stretch(txt.rectTransform);
            }
            return btn;
        }

        /// Invisible + non-interactive but ACTIVE (Awake/singletons/logic keep running).
        static void HideKeepAlive(GameObject go)
        {
            go.SetActive(true); // undo any earlier SetActive-based hiding
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

        static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            if (font != null) tmp.font = font;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void Wire(Button b, CrowsMenuActions target, string method)
        {
            var action = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction), target, method);
            UnityEventTools.AddPersistentListener(b.onClick, action);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void Anchor(RectTransform rt, float ax, float ay, float bx, float by, Vector2 min, Vector2 max)
        {
            rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(bx, by);
            rt.offsetMin = min; rt.offsetMax = max;
        }

        static Sprite Sprite(string path) { return AssetDatabase.LoadAssetAtPath<Sprite>(path); }
        static TMP_FontAsset EconomicaBold() { return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ARIA + "Fonts/Economica-Bold SDF.asset"); }
        static TMP_FontAsset LatoRegular() { return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ARIA + "Fonts/Lato-Regular SDF.asset"); }

        static Color Hex(string hex)
        {
            Color c; ColorUtility.TryParseHtmlString(hex, out c); return c;
        }
    }
}
