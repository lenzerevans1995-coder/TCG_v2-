using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine;
using TcgEngine.UI;

namespace CCG
{
    /// <summary>
    /// Theme tokens for the Clean & Minimalist reskin. Two palettes (dark/light)
    /// over the same GUI-pack white 9-slice shapes — same mechanism the pack's
    /// own Dark/Light prefabs use. Faction accents come from the card theme:
    /// Sanguine crimson, Bulwark cyan, neutral neon teal (the in-game glow).
    /// </summary>
    public static class CCGTheme
    {
        public static event System.Action onChanged;

        private const string PREF = "ccg_light_mode";
        private static int light_cache = -1;

        public static bool Light
        {
            get
            {
                if (light_cache < 0)
                    light_cache = PlayerPrefs.GetInt(PREF, 0);
                return light_cache == 1;
            }
            set
            {
                light_cache = value ? 1 : 0;
                PlayerPrefs.SetInt(PREF, light_cache);
                if (onChanged != null)
                    onChanged.Invoke();
            }
        }

        public static void Toggle() { Light = !Light; }

        private static Color C(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString(hex, out c);
            return c;
        }

        //---- palette tokens (dark : light)
        public static Color Panel        { get { return Light ? new Color(1f, 1f, 1f, 0.96f) : C("#171E2BF2"); } }
        public static Color Popup        { get { return Light ? C("#F4F7FA") : C("#1B2333"); } }
        public static Color PanelSoft    { get { return Light ? new Color(1f, 1f, 1f, 0.55f) : new Color(0.07f, 0.10f, 0.15f, 0.55f); } }
        //dark surfaces make the neon edges read as glowing tubes
        public static Color ButtonFill   { get { return Light ? Color.white : C("#101826"); } }
        public static Color TabActive    { get { return C("#0FA7C6"); } }
        public static Color TabInactive  { get { return Light ? C("#E2E8F0") : C("#1E2836"); } }
        public static Color Accent       { get { return C("#17C3DD"); } }
        public static Color AccentSan    { get { return C("#F13242"); } }
        public static Color AccentBul    { get { return C("#339AF0"); } }
        public static Color TextPrimary  { get { return Light ? C("#10141C") : C("#F2F5F9"); } }
        public static Color TextOnAccent { get { return Color.white; } }
        public static Color TextMuted    { get { return Light ? C("#5A6678") : C("#8B98AC"); } }
        public static Color IconTint     { get { return Light ? C("#3D4D65") : C("#C9D3DF"); } }
        public static Color Dim          { get { return new Color(0f, 0f, 0f, 0.65f); } }

        //---- shape sprites (GUI pack copies in Resources/GUI)
        private static Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static Sprite Shape(string key)
        {
            Sprite s;
            if (!sprites.TryGetValue(key, out s))
            {
                s = Resources.Load<Sprite>("GUI/" + key);
                sprites[key] = s;
            }
            return s;
        }

        public static Sprite BGSprite { get { return Shape(Light ? "gui_bg_light" : "gui_bg_dark"); } }
    }

    public enum CCGUIRole
    {
        None = 0,
        Background = 1,     //fullscreen wallpaper
        Panel = 2,          //bars, sidebars
        PanelOutline = 3,   //hollow frame
        Popup = 4,          //dialog boxes
        PanelSoft = 5,      //scrollview backgrounds
        Button = 10,        //standard button (accent glow)
        TabActive = 11,
        TabInactive = 12,
        Input = 13,
        Dim = 20,           //fullscreen dim overlay behind popups
        Icon = 21,          //keep sprite, tint only
        Accent = 22,        //filled accent element (lines, highlights)
        Hidden = 23,        //clean-minimal: no fill at all (scrollview backgrounds)
        TileSan = 30,       //big menu tile, Sanguine accent
        TileBul = 31,
        TileTeal = 32,
        TileBar = 33,       //label strip on tiles: dark in BOTH modes (white label text)
        SliderBG = 40,
        SliderFill = 41,
        SliderHandle = 42,
        ToggleBG = 43,
        Check = 44,         //checkmarks: pack check icon, accent
        Picture = 45,       //sprite swap, no tint (e.g. profile cardback -> our baked back)
    }

    /// <summary>
    /// Applies theme sprite+color to one Graphic and keeps it applied (kit
    /// scripts repaint sprites/colors at runtime, e.g. tab active swaps).
    /// Re-applies instantly on light/dark toggle.
    /// </summary>
    public class CCGUITheme : MonoBehaviour
    {
        public CCGUIRole role;
        public string custom_sprite; //overrides the role's default shape (icons, pictures)

        private Image img;
        private bool dirty = true;

        void Awake()
        {
            //Awake runs late for components added while inactive — Apply() also
            //lazy-fetches, this just wires the theme-change event
            if (img == null)
                img = GetComponent<Image>();
            CCGTheme.onChanged += MarkDirty;
            dirty = true;
        }

        void OnDestroy() { CCGTheme.onChanged -= MarkDirty; }
        void MarkDirty() { dirty = true; }

        void LateUpdate()
        {
            if (img == null)
                return;

            //Kit swapped the sprite back (tabs do this every refresh)? Re-map.
            if (dirty || !IsApplied())
                Apply();
        }

        private bool IsApplied()
        {
            if ((role == CCGUIRole.Icon && custom_sprite == null) || role == CCGUIRole.None)
                return true; //tint-only roles: kit doesn't fight over these
            string want = TargetSpriteName();
            return want == null || (img.sprite != null && img.sprite.name == want);
        }

        private string TargetSpriteName()
        {
            if (custom_sprite != null)
                return custom_sprite;
            switch (role)
            {
                case CCGUIRole.Background: return CCGTheme.Light ? "gui_bg_light" : "gui_bg_dark";
                case CCGUIRole.Panel: return "gui_square";
                case CCGUIRole.PanelOutline: return "gui_semi_stroke2";
                case CCGUIRole.Popup: return "gui_semi";
                case CCGUIRole.PanelSoft: return "gui_semi";
                case CCGUIRole.Button: return "gui_round";
                case CCGUIRole.TabActive: return "gui_semi";
                case CCGUIRole.TabInactive: return "gui_semi";
                case CCGUIRole.Input: return "gui_semi_stroke2";
                case CCGUIRole.Accent: return "gui_square";
                case CCGUIRole.TileSan: return "gui_semi";
                case CCGUIRole.TileBul: return "gui_semi";
                case CCGUIRole.TileTeal: return "gui_semi";
                case CCGUIRole.TileBar: return "gui_square";
                case CCGUIRole.SliderBG: return "gui_round";
                case CCGUIRole.SliderFill: return "gui_round";
                case CCGUIRole.SliderHandle: return "gui_circle";
                case CCGUIRole.ToggleBG: return "gui_square_stroke2";
                case CCGUIRole.Check: return "gui_ic_check";
                default: return null;
            }
        }

        public void Apply()
        {
            dirty = false;
            if (img == null)
                img = GetComponent<Image>();
            if (img == null)
                return;
            string sname = TargetSpriteName();
            if (sname != null)
            {
                Sprite s = CCGTheme.Shape(sname);
                if (s != null)
                {
                    img.sprite = s;
                    bool simple = role == CCGUIRole.Background || role == CCGUIRole.Icon ||
                        role == CCGUIRole.Check || role == CCGUIRole.Picture || role == CCGUIRole.SliderHandle;
                    img.type = simple ? Image.Type.Simple : Image.Type.Sliced;
                    if (role == CCGUIRole.Icon || role == CCGUIRole.Check)
                        img.preserveAspect = true;
                }
            }

            switch (role)
            {
                case CCGUIRole.Hidden: img.enabled = false; return;
                case CCGUIRole.Background: img.color = Color.white; break;
                case CCGUIRole.Panel: img.color = CCGTheme.Panel; break;
                case CCGUIRole.PanelOutline: img.color = CCGTheme.Accent; break;
                case CCGUIRole.Popup: img.color = CCGTheme.Popup; break;
                case CCGUIRole.PanelSoft: img.color = CCGTheme.PanelSoft; break;
                case CCGUIRole.Button: img.color = CCGTheme.ButtonFill; break;
                case CCGUIRole.TabActive: img.color = CCGTheme.TabActive; break;
                case CCGUIRole.TabInactive: img.color = CCGTheme.TabInactive; break;
                case CCGUIRole.Input: img.color = CCGTheme.TextMuted; break;
                //dim overlays: recolor RGB only — the kit fades their alpha
                //(BlackPanel-style faders own it)
                case CCGUIRole.Dim: img.color = new Color(0f, 0f, 0f, img.color.a); break;
                case CCGUIRole.Icon: img.color = CCGTheme.IconTint; break;
                case CCGUIRole.Accent: img.color = CCGTheme.Accent; break;
                case CCGUIRole.TileSan: img.color = Color.Lerp(CCGTheme.AccentSan, CCGTheme.Popup, 0.55f); break;
                case CCGUIRole.TileBul: img.color = Color.Lerp(CCGTheme.AccentBul, CCGTheme.Popup, 0.55f); break;
                case CCGUIRole.TileTeal: img.color = Color.Lerp(CCGTheme.Accent, CCGTheme.Popup, 0.55f); break;
                case CCGUIRole.TileBar: img.color = new Color(0.08f, 0.11f, 0.17f, 0.85f); break;
                case CCGUIRole.SliderBG: img.color = new Color(CCGTheme.TextMuted.r, CCGTheme.TextMuted.g, CCGTheme.TextMuted.b, 0.35f); break;
                case CCGUIRole.SliderFill: img.color = CCGTheme.Accent; break;
                case CCGUIRole.SliderHandle: img.color = Color.white; break;
                case CCGUIRole.ToggleBG: img.color = CCGTheme.TextMuted; break;
                case CCGUIRole.Check: img.color = CCGTheme.Accent; break;
                case CCGUIRole.Picture: img.color = Color.white; break;
            }
        }
    }

    /// <summary>
    /// NEON edge for interactive elements: a crisp saturated stroke plus two
    /// widening soft halos (the bloom), all 9-sliced to the element's shape.
    /// Reads as a glowing tube on dark surfaces.
    /// </summary>
    public class CCGUIGlow : MonoBehaviour
    {
        public Color glow_color;
        public string shape = "gui_round"; //stroke sprites must match the fill's corner radius
        private readonly System.Collections.Generic.List<Image> layers = new System.Collections.Generic.List<Image>();

        public static void Attach(RectTransform host, Color color, string shape_family)
        {
            if (host.Find("CCG_GlowEdge") != null)
                return;
            CCGUIGlow g = host.gameObject.AddComponent<CCGUIGlow>();
            g.glow_color = color;
            g.shape = shape_family;
            g.Build();
        }

        private Image MakeLayer(string name, string sprite_key, float grow, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.SetSiblingIndex(0);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-grow, -grow);
            rt.offsetMax = new Vector2(grow, grow);
            Image i = go.GetComponent<Image>();
            i.sprite = CCGTheme.Shape(sprite_key);
            i.type = Image.Type.Sliced;
            i.color = color;
            i.raycastTarget = false;
            return i;
        }

        private void Build()
        {
            layers.Add(MakeLayer("CCG_GlowHalo2", shape + "_stroke8", 12f, new Color(glow_color.r, glow_color.g, glow_color.b, 0.16f)));
            layers.Add(MakeLayer("CCG_GlowHalo1", shape + "_stroke6", 5f, new Color(glow_color.r, glow_color.g, glow_color.b, 0.40f)));
            layers.Add(MakeLayer("CCG_GlowEdge", shape + "_stroke2", 0f, new Color(glow_color.r, glow_color.g, glow_color.b, 1f)));
            //draw order: soft halos first, crisp edge on top (still under kit text,
            //which sits at higher sibling indices)
            for (int i = 0; i < layers.Count; i++)
                layers[i].transform.SetSiblingIndex(i);
        }

        public void SetColor(Color c)
        {
            glow_color = c;
            float[] alphas = new float[] { 0.16f, 0.40f, 1f };
            for (int i = 0; i < layers.Count && i < alphas.Length; i++)
                if (layers[i] != null)
                    layers[i].color = new Color(c.r, c.g, c.b, alphas[i]);
        }
    }

    /// <summary>
    /// Global menu reskin: walks every canvas in menu scenes, maps kit sprites
    /// to GUI-pack shapes + theme roles (deterministic table — unmapped sprite
    /// names are logged once so nothing is silently missed), recolors text, and
    /// injects the light/dark toggle into the settings panel.
    /// </summary>
    public class CCGMenuSkin : MonoBehaviour
    {
        private float timer = 0f;
        private static HashSet<string> unmapped_logged = new HashSet<string>();

        //kit sprite name -> role
        private static Dictionary<string, CCGUIRole> map = new Dictionary<string, CCGUIRole>()
        {
            { "bg_menu", CCGUIRole.Background },
            { "adventure_bg", CCGUIRole.Background },
            { "top_bar1", CCGUIRole.Panel },
            { "collection_panel_left", CCGUIRole.Panel },
            { "collection_panel_right", CCGUIRole.Panel },
            { "home_panel_deck_area", CCGUIRole.PanelOutline },
            { "box1", CCGUIRole.Popup },
            { "box2", CCGUIRole.Popup },
            { "box3", CCGUIRole.Popup },
            { "player_stats_box", CCGUIRole.Popup },
            { "player_bar_bottom", CCGUIRole.Panel },
            { "button_large", CCGUIRole.Button },
            { "button_thin", CCGUIRole.Button },
            { "button_square", CCGUIRole.Button },
            { "input_field_long", CCGUIRole.Input },
            { "InputFieldBackground", CCGUIRole.Input },
            { "tab_big_inactive", CCGUIRole.TabInactive },
            { "tab_big_active", CCGUIRole.TabActive },
            { "tab_small_inactive", CCGUIRole.TabInactive },
            { "tab_small_active", CCGUIRole.TabActive },
            { "leaderboard_line", CCGUIRole.Accent },
            { "avatar_frame1", CCGUIRole.PanelOutline },
            { "exit", CCGUIRole.Icon },
            { "settings", CCGUIRole.Icon },
            { "logout", CCGUIRole.Icon },
            { "arrow", CCGUIRole.Icon },
            { "win_vs", CCGUIRole.Icon },
            { "icon_accept", CCGUIRole.Check },
            { "logo", CCGUIRole.Hidden },
            { "logo_color", CCGUIRole.Hidden },
            { "play_horse_off", CCGUIRole.TileSan },
            { "play_horse_on", CCGUIRole.TileSan },
            { "play_turtle2_off", CCGUIRole.TileBul },
            { "play_turtle2_on", CCGUIRole.TileBul },
            { "play-turtle-off", CCGUIRole.TileBul },
            { "play-turtle-on", CCGUIRole.TileBul },
            { "play-turtle", CCGUIRole.Background },
            { "play-wolf-off", CCGUIRole.TileTeal },
            { "play-wolf-on", CCGUIRole.TileTeal },
            { "home_title_bar1", CCGUIRole.TileBar },
            { "home_title_bar2", CCGUIRole.TileBar },
            { "home_title_bar3", CCGUIRole.TileBar },
            { "home_title_bar4", CCGUIRole.TileBar },
            { "home_title_bar5", CCGUIRole.TileBar },
            { "home_title_bar6", CCGUIRole.TileBar },
            { "leaderboard_highlight", CCGUIRole.PanelSoft },
            { "cardback_silver", CCGUIRole.Picture },
        };

        //kit icon -> GUI pack icon (tinted by role)
        private static Dictionary<string, string> icon_swap = new Dictionary<string, string>()
        {
            { "settings", "gui_ic_settings" },
            { "exit", "gui_ic_close" },
            { "logout", "gui_ic_logout" },
            { "arrow", "gui_ic_arrow_left" },
            { "icon_accept", "gui_ic_check" },
            { "cardback_silver", "gui_cardback_san" },
        };

        private string last_scene = "";

        void Update()
        {
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (scene != last_scene)
            {
                last_scene = scene;
                play_layout_done = false;
                topbar_done = false;
            }

            timer -= Time.deltaTime;
            if (timer > 0f)
                return;
            timer = 0.4f;

            foreach (Image img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                TrySkin(img);

            foreach (Text txt in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                TrySkinText(txt);

            foreach (Slider sl in Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                SkinSlider(sl);

            foreach (Toggle tg in Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                SkinToggle(tg);

            InjectThemeToggle();
            FixPlayLayout();
            FixTopBar();
        }

        //---- measured top bar ----
        //Kit tabs are 320 wide on 261 spacing — the angled parallelogram art hid a
        //59px overlap per pair. Flat tabs need real gutters: 340 spacing (20px gap),
        //block right edge at +870 (56px canvas margin), clear of the avatar block.
        private bool topbar_done = false;

        private void FixTopBar()
        {
            if (topbar_done)
                return;
            GameObject bar = GameObject.Find("UICanvas/TopBar");
            if (bar == null)
                return;

            string[] tabs = new string[] { "TabPlay", "TabCollection", "TabPacks", "TabLeaderboard" };
            float[] xs = new float[] { 616f, 956f, 1296f, 1636f }; //anchored-x: canvas -310/30/370/710
            for (int i = 0; i < tabs.Length; i++)
            {
                RectTransform t = bar.transform.Find(tabs[i]) as RectTransform;
                if (t == null)
                    return; //scene not ready yet
                t.anchoredPosition = new Vector2(xs[i], t.anchoredPosition.y);
            }

            //name/coins rows overlapped by 14px (50-tall texts on 36 spacing)
            RectTransform pname = bar.transform.Find("PlayerName") as RectTransform;
            RectTransform credits = bar.transform.Find("Credits") as RectTransform;
            if (pname != null)
                pname.anchoredPosition = new Vector2(pname.anchoredPosition.x, 42f);
            if (credits != null)
                credits.anchoredPosition = new Vector2(credits.anchoredPosition.x, -10f);

            //leftover kit zone marker that overlaps the avatar block
            Transform tz = bar.transform.Find("TabZone");
            if (tz != null)
            {
                Image tzi = tz.GetComponent<Image>();
                if (tzi != null)
                    tzi.enabled = false;
            }

            topbar_done = true;
        }

        private static void GiveRole(Component c, CCGUIRole role)
        {
            if (c == null)
                return;
            Image i = c.GetComponent<Image>();
            if (i == null || i.GetComponent<CCGUITheme>() != null)
                return;
            CCGUITheme th = i.gameObject.AddComponent<CCGUITheme>();
            th.role = role;
            th.Apply();
        }

        private void SkinSlider(Slider sl)
        {
            if (IsCardVisual(sl.transform))
                return;
            GiveRole(sl.transform.Find("Background"), CCGUIRole.SliderBG);
            if (sl.fillRect != null) GiveRole(sl.fillRect, CCGUIRole.SliderFill);
            if (sl.handleRect != null) GiveRole(sl.handleRect, CCGUIRole.SliderHandle);
        }

        private void SkinToggle(Toggle tg)
        {
            if (IsCardVisual(tg.transform))
                return;
            if (tg.targetGraphic != null) GiveRole(tg.targetGraphic, CCGUIRole.ToggleBG);
            if (tg.graphic != null) GiveRole(tg.graphic, CCGUIRole.Check);
        }

        //---- measured PlayPanel layout ----
        //Kit tile positions relied on transparent art padding; with flat fills the
        //tiles collide. Explicit symmetric grid on the 1852x1080 canvas:
        //two 430x380 tiles (gutter 20), an 880x380 wide tile below (gutter 30),
        //all clear of the deck frame on the right.
        private bool play_layout_done = false;

        private static void Place(Transform t, float x, float y, float w, float h)
        {
            if (t == null)
                return;
            RectTransform rt = t as RectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            rt.localRotation = Quaternion.identity;
        }

        private static void BottomBar(Transform tile)
        {
            if (tile == null)
                return;
            foreach (Transform child in tile)
            {
                CCGUITheme th = child.GetComponent<CCGUITheme>();
                bool is_bar = th != null && th.role == CCGUIRole.TileBar;
                bool is_label = child.GetComponent<Text>() != null;
                if (!is_bar && !is_label)
                    continue;
                RectTransform rt = child as RectTransform;
                rt.localRotation = Quaternion.identity;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.offsetMin = new Vector2(0f, 0f);
                rt.offsetMax = new Vector2(0f, 90f);
                Text label = child.GetComponent<Text>();
                if (label != null)
                    label.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void FixPlayLayout()
        {
            if (play_layout_done)
                return;
            GameObject panel = GameObject.Find("UICanvas/PlayPanel");
            if (panel == null)
                return;

            Transform adventure = panel.transform.Find("PlayAdventure");
            Transform solo = panel.transform.Find("PlaySolo");
            Transform multi = panel.transform.Find("PlayMulti");
            Transform code = panel.transform.Find("PlayMultiCode");
            //only proceed once the tiles are themed (bars need their roles)
            if (adventure == null || adventure.GetComponent<CCGUITheme>() == null)
                return;

            //composition centered: tiles block spans -695..185, deck 215..714,
            //left/right screen margins ~220px each
            Place(adventure, -480f, 80f, 430f, 380f);
            Place(solo, -30f, 80f, 430f, 380f);
            Place(multi, -255f, -330f, 880f, 380f);
            Transform deck_frame = panel.transform.Find("UIFrame");
            if (deck_frame != null)
                Place(deck_frame, 465f, -50f, 499f, 599f);
            BottomBar(adventure);
            BottomBar(solo);
            BottomBar(multi);

            if (code != null)
            {
                Place(code, 465f, -420f, 260f, 44f); //under the deck frame, clear of all tiles
                Image ci = code.GetComponent<Image>();
                if (ci != null && ci.GetComponent<CCGUITheme>() == null)
                {
                    CCGUITheme th = ci.gameObject.AddComponent<CCGUITheme>();
                    th.role = CCGUIRole.Button;
                    th.Apply();
                    CCGUIGlow.Attach(ci.rectTransform, CCGTheme.Accent, "gui_round");
                }
                Text ctext = code.GetComponentInChildren<Text>();
                if (ctext != null)
                {
                    RectTransform trt = ctext.rectTransform;
                    trt.anchorMin = Vector2.zero;
                    trt.anchorMax = Vector2.one;
                    trt.offsetMin = Vector2.zero;
                    trt.offsetMax = Vector2.zero;
                    ctext.alignment = TextAnchor.MiddleCenter;
                }
            }

            play_layout_done = true;
        }

        private static bool IsCardVisual(Transform t)
        {
            //never touch card faces, pack cards, or anything under a CardUI
            //(include inactive parents — most menu panels start disabled)
            if (t.GetComponentInParent<CardUI>(true) != null)
                return true;
            Transform p = t;
            while (p != null)
            {
                if (p.name == "CCG_Face" || p.name == "CardFace" || p.GetComponent<TcgEngine.Client.PackCard>() != null)
                    return true;
                p = p.parent;
            }
            return false;
        }

        private void TrySkin(Image img)
        {
            if (img.GetComponent<CCGUITheme>() != null || img.GetComponent<CCGUIGlow>() != null)
                return;
            if (img.name.StartsWith("CCG_Glow"))
                return;
            //scene faders own their alpha and start invisible — never touch
            if (img.name == "BlackPanel" || img.name == "WhitePanel")
                return;
            if (IsCardVisual(img.transform))
                return;

            string sname = img.sprite != null ? img.sprite.name : "";
            CCGUIRole role = CCGUIRole.None;

            if (sname != "" && map.TryGetValue(sname, out role)) { }
            else if (sname == "Square" || sname == "UISprite" || sname == "Background")
            {
                //context rules for Unity built-ins: fullscreen dark rect = dim
                //overlay, scrollview "Background" = soft panel, small = button
                RectTransform rt = img.rectTransform;
                Canvas cv = img.canvas;
                if (cv != null && rt.rect.width >= cv.pixelRect.width / cv.scaleFactor * 0.9f && rt.rect.height >= 500f)
                    role = CCGUIRole.Dim;
                else if (sname == "Background" && img.GetComponent<UnityEngine.UI.ScrollRect>() != null)
                    role = CCGUIRole.Hidden; //scroll containers: no fill, clean
                else if (sname == "Background")
                    role = CCGUIRole.PanelSoft;
                else
                    return; //small Square/UISprite: leave (selection markers etc.)
            }
            else
            {
                if (sname != "" && !sname.StartsWith("gui_") && unmapped_logged.Add(sname))
                    Debug.Log("[CCGMenuSkin] unmapped sprite: " + sname + " (" + GetPath(img.transform) + ")");
                return;
            }

            CCGUITheme th = img.gameObject.AddComponent<CCGUITheme>();
            th.role = role;
            string swap;
            if (icon_swap.TryGetValue(sname, out swap))
                th.custom_sprite = swap;
            th.Apply();

            //neon edge on interactive things — stroke shape matches the fill shape
            if (role == CCGUIRole.Button)
                CCGUIGlow.Attach(img.rectTransform, CCGTheme.Accent, "gui_round");
            else if (role == CCGUIRole.TileSan)
                CCGUIGlow.Attach(img.rectTransform, CCGTheme.AccentSan, "gui_semi");
            else if (role == CCGUIRole.TileBul)
                CCGUIGlow.Attach(img.rectTransform, CCGTheme.AccentBul, "gui_semi");
            else if (role == CCGUIRole.TileTeal)
                CCGUIGlow.Attach(img.rectTransform, CCGTheme.Accent, "gui_semi");

            //buttons: color-tint transition instead of kit sprite swaps
            Button btn = img.GetComponent<Button>();
            if (btn != null && (role == CCGUIRole.Button || role == CCGUIRole.TileSan || role == CCGUIRole.TileBul || role == CCGUIRole.TileTeal || role == CCGUIRole.TabActive || role == CCGUIRole.TabInactive))
            {
                btn.transition = Selectable.Transition.ColorTint;
                ColorBlock cb = ColorBlock.defaultColorBlock;
                cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
                cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                btn.colors = cb;
                btn.targetGraphic = img;
            }
        }

        private void TrySkinText(Text txt)
        {
            if (txt.GetComponent<CCGUIText>() != null)
                return;
            if (IsCardVisual(txt.transform))
                return;

            CCGUIText t = txt.gameObject.AddComponent<CCGUIText>();
            //text sitting on an accent-filled control stays white
            CCGUITheme parent_theme = txt.GetComponentInParent<CCGUITheme>();
            t.on_accent = parent_theme != null &&
                (parent_theme.role == CCGUIRole.TabActive || parent_theme.role == CCGUIRole.TileSan ||
                 parent_theme.role == CCGUIRole.TileBul || parent_theme.role == CCGUIRole.TileTeal ||
                 parent_theme.role == CCGUIRole.TileBar);
            t.Apply();
        }

        private void InjectThemeToggle()
        {
            if (GameObject.Find("CCG_ThemeBtn") != null)
                return;
            SettingsPanel sp = Object.FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            if (sp == null)
                return;
            Transform ok = sp.transform.Find("Ok");
            if (ok == null)
                return;

            GameObject theme_go = Object.Instantiate(ok.gameObject, ok.parent);
            theme_go.name = "CCG_ThemeBtn";
            RectTransform trt = theme_go.GetComponent<RectTransform>();
            RectTransform ort = ok.GetComponent<RectTransform>();
            trt.anchoredPosition = ort.anchoredPosition + new Vector2(-(ort.rect.width + 30f), 0f);
            Button tbtn = theme_go.GetComponent<Button>();
            //fresh event instance: drops the OK button's PERSISTENT (serialized)
            //close-panel handler, which RemoveAllListeners does not
            tbtn.onClick = new Button.ButtonClickedEvent();
            tbtn.onClick.AddListener(delegate { CCGTheme.Toggle(); });
            Text label = theme_go.GetComponentInChildren<Text>();
            if (label != null)
                label.text = "THEME";
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }
    }

    /// <summary>Theme-aware text color, reapplied on toggle.</summary>
    public class CCGUIText : MonoBehaviour
    {
        public bool on_accent = false;
        private Text txt;

        void Awake()
        {
            txt = GetComponent<Text>();
            CCGTheme.onChanged += Apply;
        }

        void OnDestroy() { CCGTheme.onChanged -= Apply; }
        void OnEnable() { Apply(); }

        public void Apply()
        {
            if (txt == null)
                txt = GetComponent<Text>();
            if (txt == null)
                return;
            txt.color = on_accent ? CCGTheme.TextOnAccent : CCGTheme.TextPrimary;
        }
    }

    /// <summary>
    /// Spawns the menu skinner in menu scenes only (the match board has its own
    /// visual language; its HUD gets a dedicated pass).
    /// </summary>
    public static class CCGMenuSkinBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            GameObject go = new GameObject("CCG_MenuSkin");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<CCGMenuSkinRunner>();
        }
    }

    /// <summary>Enables the skinner only outside the game board scene.</summary>
    public class CCGMenuSkinRunner : MonoBehaviour
    {
        private CCGMenuSkin skin;

        void Update()
        {
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool menu_scene = scene == "LoginMenu" || scene == "Menu" || scene == "OpenPack";
            if (menu_scene && skin == null)
                skin = gameObject.AddComponent<CCGMenuSkin>();
            else if (!menu_scene && skin != null)
                Destroy(skin);
        }
    }
}
