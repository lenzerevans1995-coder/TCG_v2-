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
    /// (user-approved). Kit panels keep their logic; PlayPanel gets the CROWS home hub on top.
    /// Idempotent: re-running deletes and rebuilds the CROWS layer.
    /// </summary>
    public static class CrowsMenuSkin
    {
        // CROWS stone palette (from the approved mockup)
        static readonly Color BG = Hex("#191d20");
        static readonly Color PANEL = Hex("#23282c");
        static readonly Color INK = Hex("#101416");
        static readonly Color STROKE = Hex("#3c4448");
        static readonly Color GOLD = Hex("#c9a84c");
        static readonly Color GREEN = Hex("#2f5c3a");
        static readonly Color GREEN_EDGE = Hex("#84c9a0");
        static readonly Color TEXT = Hex("#c8d0d4");
        static readonly Color TEXT_DIM = Hex("#8a9296");

        const string ARIA = "Assets/Honeti/AriaGUI/";
        const string SCENE = "Assets/TcgEngine/Scenes/Menu/Menu.unity";

        [MenuItem("CROWS/Skin Menu Scene")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(SCENE);
            var canvas = GameObject.Find("UICanvas");
            if (canvas == null) { Debug.LogError("CROWS: UICanvas not found"); return; }

            // ---- idempotency: clear previous runs ----
            Kill(canvas.transform, "CrowsBackground");
            var playPanel = canvas.transform.Find("PlayPanel");
            if (playPanel != null) Kill(playPanel, "CrowsHome");

            // ---- full-screen stone background, first sibling so everything draws over it ----
            var bg = MakeImage(canvas.transform, "CrowsBackground", null, BG);
            Stretch(bg.rectTransform);
            bg.transform.SetSiblingIndex(0);
            bg.raycastTarget = false;

            // ---- kit chrome off: our topbar replaces it ----
            var kitTopBar = canvas.transform.Find("TopBar");
            if (kitTopBar != null) kitTopBar.gameObject.SetActive(false);

            // ---- home hub over PlayPanel ----
            if (playPanel != null)
            {
                // hide kit's own PlayPanel chrome, keep the panel logic alive
                foreach (Transform child in playPanel)
                    if (child.name != "CrowsHome")
                        child.gameObject.SetActive(false);

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
            // flat stone plate + stroke (dark tints kill Aria's colored sprites — keep sprites near-white or none)
            var bar = MakePanel(parent, "Topbar");
            Anchor(bar.rectTransform, 0f, 1f, 1f, 1f, new Vector2(20, -64), new Vector2(-20, -12));

            var title = MakeText(bar.transform, "Title", "CROWS", 30, GOLD, EconomicaBold());
            Anchor(title.rectTransform, 0f, 0f, 0f, 1f, new Vector2(18, 0), new Vector2(220, 0));
            title.alignment = TextAlignmentOptions.MidlineLeft;

            var settings = MakeButton(bar.transform, "SettingsButton", "", 22, INK, TEXT_DIM, null);
            AddStroke(settings.image, STROKE);
            Anchor(((RectTransform)settings.transform), 1f, 0.5f, 1f, 0.5f, new Vector2(-56, -18), new Vector2(-16, 18));
            var gear = MakeImage(settings.transform, "Icon", Sprite(ARIA + "Textures/Icons/64/Settings.png"), TEXT_DIM);
            Anchor(gear.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(-12, -12), new Vector2(12, 12));
            gear.raycastTarget = false;
            Wire(settings, actions, "ShowSettings");
        }

        static void BuildHero(Transform parent, CrowsMenuActions actions)
        {
            var hero = MakePanel(parent, "HeroPanel");
            Anchor(hero.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(-460, -190), new Vector2(460, 230));

            var hint = MakeText(hero.transform, "ArtHint", "[ key art ]", 16, TEXT_DIM, LatoRegular());
            Anchor(hint.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(-200, 20), new Vector2(200, 80));
            hint.alignment = TextAlignmentOptions.Center;

            // Aria's green button keeps its own art — only a slight dim so it sits in the dark scene
            var play = MakeButton(hero.transform, "PlayButton", "PLAY", 34, new Color(0.9f, 0.9f, 0.9f, 1f), Color.white, Sprite(ARIA + "Textures/Buttons/ButtonGreen.png"));
            Anchor(((RectTransform)play.transform), 0.5f, 0f, 0.5f, 0f, new Vector2(-140, 26), new Vector2(140, 96));
            Wire(play, actions, "PlaySolo");
        }

        static void BuildNav(Transform parent, CrowsMenuActions actions)
        {
            string[] names = { "DECKS", "COLLECTION", "TRIALS" };
            string[] methods = { "ShowCollection", "ShowCollection", "ShowAdventure" };
            for (int i = 0; i < 3; i++)
            {
                var b = MakeButton(parent, "Nav" + names[i], names[i], 22, INK, TEXT, null);
                AddStroke(b.image, STROKE);
                var rt = (RectTransform)b.transform;
                Anchor(rt, 0.5f, 0f, 0.5f, 0f, new Vector2(-465 + i * 320, 20), new Vector2(-175 + i * 320, 70));
                Wire(b, actions, methods[i]);
            }
        }

        // ================= helpers =================

        static void Kill(Transform parent, string name)
        {
            var t = parent is RectTransform ? parent.Find(name) : parent.Find(name);
            if (t == null && parent.name == name) t = parent;
            var found = parent.Find(name);
            if (found != null) Object.DestroyImmediate(found.gameObject);
            else
            {
                var direct = GameObject.Find(name);
                if (direct != null && direct.transform.parent == parent) Object.DestroyImmediate(direct);
            }
        }

        static Image MakePanel(Transform parent, string name)
        {
            var img = MakeImage(parent, name, null, PANEL);
            AddStroke(img, STROKE);
            return img;
        }

        static void AddStroke(Image img, Color color)
        {
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(2, -2);
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

        static Button MakeButton(Transform parent, string name, string label, float size, Color bg, Color fg, Sprite sprite)
        {
            var img = MakeImage(parent, name, sprite, bg);
            img.type = Image.Type.Sliced;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = colors;
            var txt = MakeText(img.transform, "Label", label, size, fg, EconomicaBold());
            Stretch(txt.rectTransform);
            return btn;
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
