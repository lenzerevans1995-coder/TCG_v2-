using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TcgEngine;
using TcgEngine.Client;
using TcgEngine.UI;

namespace CCG
{
    /// <summary>
    /// Docked match HUD (approved mockup match_hud.svg v5 — MTGO-style panes
    /// that never overlap):
    ///   DOCK A (left):  hero clusters — portrait, weapon, 4 armor chips (all
    ///                   with the equipped card's ART + hover full-card
    ///                   preview), HP bar, floating mana — enemy top, player
    ///                   bottom, plus the trial-powers strip in trial battles.
    ///   DOCK C (right): deck/discard pile tiles + TURN plate + END TURN
    ///                   (the kit's own button/texts reparented, cut-corner
    ///                   Shift chrome).
    ///   DOCK B stays the 3D board; CCGEquipmentDisplay draws only playable
    ///   cards there (minions + back row). The player hand keeps the kit FAN
    ///   (user call); the enemy hand-back fan is slightly shrunk and tucked
    ///   half off the top edge.
    /// NO OVALS (user rule): every pane/chip is a cut-corner or small-radius
    /// rounded rect; the only circles are mana/cost gems.
    /// All positions come from the 960x540 mockup grid, mapped to anchors.
    /// </summary>
    public class CCGMatchHUD : MonoBehaviour
    {
        private const float TW = 960f, TH = 540f; //mockup template space
        private const float CHIP_R = 2.4f;        //ppu mult: small corner radius, never a pill

        private static readonly Color FILL = new Color(0.098f, 0.088f, 0.070f, 0.97f);
        private static readonly Color PANEL = new Color(0.086f, 0.078f, 0.061f, 0.97f);
        private static readonly Color DARK = new Color(0.055f, 0.049f, 0.039f, 0.95f);
        private static readonly Color ACCENT = new Color(0.788f, 0.659f, 0.298f);
        private static readonly Color NEGATIVE = new Color(0.557f, 0.122f, 0.173f);
        private static readonly Color TEXTC = new Color(0.910f, 0.863f, 0.753f);
        private static readonly Color DIM = new Color(0.471f, 0.435f, 0.353f);

        private class EquipChip
        {
            public Image art;
            public Image stroke;
            public Text txt;
            public GameObject scrim;
            public string uid = "";
            public Card card;
        }

        private class Cluster
        {
            public Image portrait_art;
            public Image portrait_stroke;
            public Text name_txt;
            public EquipChip weapon = new EquipChip();
            public Text weapon_cost;
            public EquipChip[] armor = new EquipChip[4];
            public RectTransform hp_fill;
            public Text hp_txt;
            public Image[] orbs = new Image[10];
            public string hero_id = "";
            public Card hero_card;
        }

        private static readonly string[] armor_traits = { "head", "chest", "arms", "legs" };

        private RectTransform root;
        private Cluster me_cluster, opp_cluster;
        private Text[] pile_counts = new Text[4];      //edeck, ediscard, pdeck, pdiscard
        private Image[] pile_art = new Image[4];       //top card art / deck back
        private Image[,] pile_stack = new Image[4, 2]; //offset layers: the stacked-pile look
        private Image enemy_panel_stroke;              //drag-target highlight
        private Image enemy_panel_glow;
        private Font raj_regular, raj_semibold, raj_bold;
        private bool built = false;
        private bool enemy_hand_scaled = false;

        //hover preview: the kit's CardPreviewUI (big card + ability/keyword
        //help text) docked into the mockup's preview pane slot
        private RectTransform preview_panel;
        private RectTransform preview_slot;
        private float preview_scaled_w = -1f;
        private Card ui_hover_card;
        private bool over_my_portrait = false;
        private bool over_enemy_portrait = false;
        private static CCGMatchHUD instance;

        public static bool IsPointerOverMyPortrait { get { return instance != null && instance.over_my_portrait; } }
        public static bool IsPointerOverEnemyPortrait { get { return instance != null && instance.over_enemy_portrait; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            GameObject go = new GameObject("CCG_MatchHUD");
            DontDestroyOnLoad(go);
            go.AddComponent<CCGMatchHUD>();
        }

        void Update()
        {
            instance = this;
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!scene.Contains("Game"))
            {
                built = false;
                enemy_hand_scaled = false;
                ui_hover_card = null;
                over_my_portrait = false;
                over_enemy_portrait = false;
                root = null;
                browser_root = null;
                return;
            }

            if (!built)
                TryBuild();
            if (!built)
                return;

            Refresh();
            FitPreview();
            TuckEnemyHand();

            //right-click closes the pile browser (matches kit behavior)
            if (browser_root != null && browser_root.gameObject.activeSelf)
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null && mouse.rightButton.wasPressedThisFrame)
                    CloseBrowser();
            }
        }

        //=================== BUILD ===================

        private void TryBuild()
        {
            GameUI gui = Object.FindFirstObjectByType<GameUI>();
            if (gui == null || gui.game_canvas == null || gui.end_turn_button == null)
                return;

            raj_regular = Resources.Load<Font>("Fonts/Rajdhani-Regular");
            raj_semibold = Resources.Load<Font>("Fonts/Rajdhani-SemiBold");
            raj_bold = Resources.Load<Font>("Fonts/Rajdhani-Bold");

            GameObject rgo = new GameObject("CCG_HUD", typeof(RectTransform));
            root = rgo.GetComponent<RectTransform>();
            root.SetParent(gui.game_canvas.transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            opp_cluster = BuildCluster(true);
            me_cluster = BuildCluster(false);
            BuildTrialPowers();
            BuildDockC(gui);
            BuildPreview();
            BuildBrowser();
            BuildDefenseStrip();
            HideKitHud(gui);

            built = true;
        }

        /// <summary>Hero cluster panel — mockup coords: panel (8,24/384) 220x128.</summary>
        private Cluster BuildCluster(bool enemy)
        {
            Cluster cl = new Cluster();
            float py = enemy ? 24f : 384f;
            Color side = enemy ? NEGATIVE : ACCENT;

            RectTransform panel = Box("Cluster_" + (enemy ? "Enemy" : "Player"), 8, py, 220, 128);
            Image panel_fill = Img(Child(panel, "Fill"), "gui_cut_fill", PANEL, 0.5f);
            Image panel_stroke = Img(Child(panel, "Stroke"), "gui_cut_stroke3", side, 0.5f);
            if (enemy)
            {
                //The WHOLE enemy cluster is the face-attack drop target; it
                //highlights while an armed drag hovers it
                enemy_panel_stroke = panel_stroke;
                RectTransform glow_rt = Child(panel, "Glow");
                glow_rt.offsetMin = new Vector2(-6f, -6f);
                glow_rt.offsetMax = new Vector2(6f, 6f);
                enemy_panel_glow = Img(glow_rt, "gui_cut_glow3", new Color(0f, 0f, 0f, 0f), 0.5f);
                panel_fill.raycastTarget = true;
                //trigger on the raycastable Fill itself (clicks don't bubble)
                EventTrigger panel_trig = panel_fill.gameObject.AddComponent<EventTrigger>();
                AddTrigger(panel_trig, EventTriggerType.PointerEnter, () => over_enemy_portrait = true);
                AddTrigger(panel_trig, EventTriggerType.PointerExit, () => over_enemy_portrait = false);
                AddTrigger(panel_trig, EventTriggerType.PointerClick, () => CCGHeroAttackControls.UIAttackEnemyHero());
            }

            //Portrait: masked art + stroke + name band; click = arm (you) / take the hit (enemy)
            RectTransform port = Box("Portrait", 16, py + 8, 58, 56);
            Image port_bg = Img(port, "gui_semi", FILL, 1.6f);
            port_bg.raycastTarget = true;
            port.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            RectTransform art_rt = Child(port, "Art");
            cl.portrait_art = art_rt.gameObject.AddComponent<Image>();
            cl.portrait_art.raycastTarget = false;
            art_rt.gameObject.AddComponent<CCGCoverFit>();
            cl.portrait_stroke = Img(Box("PortraitStroke", 16, py + 8, 58, 56), "gui_semi_stroke2", side, 1.6f);
            //name kept INSIDE the portrait: dark band + truncation, no spill
            RectTransform nb = Box("HeroNameBand", 16, py + 49, 58, 15);
            Img(nb, "gui_square", new Color(0f, 0f, 0f, 0.55f), 1f);
            cl.name_txt = Txt(Box("HeroName", 18, py + 49, 54, 15), "", TEXTC, 11, true, TextAnchor.MiddleCenter);
            cl.name_txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            cl.name_txt.verticalOverflow = VerticalWrapMode.Truncate;
            //Your portrait: press to arm + DRAG (teal indication line, like a
            //minion attack); release on the portrait keeps click-click mode.
            //Enemy portrait: click (or drag-release onto it) to swing at them.
            EventTrigger ptrig = port.gameObject.AddComponent<EventTrigger>();
            if (enemy)
            {
                AddTrigger(ptrig, EventTriggerType.PointerClick, () => CCGHeroAttackControls.UIAttackEnemyHero());
                AddTrigger(ptrig, EventTriggerType.PointerEnter, () => over_enemy_portrait = true);
                AddTrigger(ptrig, EventTriggerType.PointerExit, () => over_enemy_portrait = false);
            }
            else
            {
                AddTrigger(ptrig, EventTriggerType.PointerDown, () => CCGHeroAttackControls.UIBeginDrag());
                AddTrigger(ptrig, EventTriggerType.PointerEnter, () => over_my_portrait = true);
                AddTrigger(ptrig, EventTriggerType.PointerExit, () => over_my_portrait = false);
            }
            HoverPreview(port.gameObject, () => cl.hero_card);

            //Weapon chip: equipped card ART + swing ATK / swing cost overlays
            RectTransform wchip = Box("Weapon", 80, py + 8, 44, 36);
            BuildEquipChip(cl.weapon, wchip, side);
            cl.weapon_cost = Txt(Box("WeaponCost", 80, py + 46, 44, 10), "", DIM, 8, false, TextAnchor.MiddleCenter);
            HoverPreview(wchip.gameObject, () => cl.weapon.card);

            //2x2 armor chips (Head/Chest over Arms/Legs) — equipped ART + HP left
            for (int i = 0; i < 4; i++)
            {
                float ax = 132 + (i % 2) * 24;
                float ay = py + 8 + (i / 2) * 24;
                RectTransform chip = Box("Armor" + i, ax, ay, 20, 20);
                cl.armor[i] = new EquipChip();
                BuildEquipChip(cl.armor[i], chip, side);
                int idx = i;
                HoverPreview(chip.gameObject, () => cl.armor[idx].card);
            }
            Txt(Box("ArmorLbl", 180, py + 8, 38, 10), "ARMOR", DIM, 8, false, TextAnchor.MiddleLeft);

            //HP bar — cut-corner Shift chrome, NOT a pill
            RectTransform bar = Box("HPBar", 16, py + 72, 198, 16);
            Img(bar, "gui_cut_fill", DARK, 0.8f);
            RectTransform fill = Child(bar, "Fill");
            Image fimg = fill.gameObject.AddComponent<Image>();
            fimg.sprite = S("gui_cut_fill");
            fimg.type = Image.Type.Sliced;
            fimg.pixelsPerUnitMultiplier = 0.8f;
            fimg.color = new Color(side.r, side.g, side.b, 0.85f);
            fimg.raycastTarget = false;
            cl.hp_fill = fill;
            cl.hp_txt = Txt(Box("HPTxt", 16, py + 72, 198, 16), "", new Color(0.05f, 0.08f, 0.13f), 12, true, TextAnchor.MiddleCenter);

            //Floating mana gems (circles are gems, not buttons)
            for (int i = 0; i < 10; i++)
            {
                RectTransform orb = Box("Orb" + i, 18 + i * 17, py + 96, 13, 13);
                Image oimg = Img(orb, "gui_circle", FILL, 1f);
                oimg.type = Image.Type.Simple;
                cl.orbs[i] = oimg;
            }
            Txt(Box("ManaLbl", 18 + 10 * 17, py + 96, 30, 13), "MANA", DIM, 8, false, TextAnchor.MiddleLeft);

            return cl;
        }

        /// <summary>Small equipment tile: masked card art + stat text over a dark scrim.</summary>
        private void BuildEquipChip(EquipChip chip, RectTransform rt, Color side)
        {
            Image bg = Img(rt, "gui_semi", FILL, CHIP_R);
            bg.raycastTarget = true;
            rt.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            RectTransform art_rt = Child(rt, "Art");
            chip.art = art_rt.gameObject.AddComponent<Image>();
            chip.art.raycastTarget = false;
            chip.art.enabled = false;
            art_rt.gameObject.AddComponent<CCGCoverFit>();
            //dark scrim behind the stat number so it reads over any art
            RectTransform scrim = Child(rt, "Scrim");
            scrim.anchorMin = Vector2.zero;
            scrim.anchorMax = new Vector2(1f, 0.48f);
            Image simg = scrim.gameObject.AddComponent<Image>();
            simg.color = new Color(0f, 0f, 0f, 0.55f);
            simg.raycastTarget = false;
            chip.scrim = scrim.gameObject;
            RectTransform txt_rt = Child(rt, "Txt");
            txt_rt.anchorMin = Vector2.zero;
            txt_rt.anchorMax = new Vector2(1f, 0.52f);
            chip.txt = txt_rt.gameObject.AddComponent<Text>();
            chip.txt.text = "-";
            StyleText(chip.txt, TEXTC, 11, true, TextAnchor.MiddleCenter);
            chip.stroke = Img(Child(rt, "Stroke"), "gui_semi_stroke2", side, CHIP_R);
        }

        /// <summary>Trial powers strip above the player cluster (trial battles only).</summary>
        private void BuildTrialPowers()
        {
            if (PlayerPrefs.GetString(CCGTrialRun.PENDING_PREF, "").Length == 0)
                return;
            CCGTrialRun run = CCGTrialRun.Load();
            if (run == null || run.powers.Count == 0)
                return;

            for (int i = 0; i < run.powers.Count && i < 6; i++)
            {
                RectTransform chip = Box("Power" + i, 8 + i * 22, 358, 18, 18);
                Img(Child(chip, "Fill"), "gui_semi", FILL, CHIP_R);
                Img(Child(chip, "Stroke"), "gui_semi_stroke2", ACCENT, CHIP_R);
                RectTransform icon = Box("PowerIcon" + i, 11 + i * 22, 361, 12, 12);
                Image iimg = Img(icon, "gui_ic_power", ACCENT, 1f);
                iimg.type = Image.Type.Simple;
                iimg.preserveAspect = true;
            }
            int n = Mathf.Min(run.powers.Count, 6);
            Txt(Box("PowerLbl", 8 + n * 22 + 4, 358, 80, 18), "TRIAL POWERS", DIM, 8, false, TextAnchor.MiddleLeft);
        }

        /// <summary>Right dock: pile tiles, TURN plate (kit texts reparented), END TURN (kit button reparented).</summary>
        private void BuildDockC(GameUI gui)
        {
            BuildPileTile(0, "ENEMY DECK", 852, 28, NEGATIVE, true, true);
            BuildPileTile(1, "DISCARD", 852, 124, DIM, true, false);
            BuildPileTile(2, "YOUR DECK", 852, 234, ACCENT, false, true);
            BuildPileTile(3, "DISCARD", 852, 330, DIM, false, false);

            //TURN plate
            RectTransform plate = Box("TurnPlate", 824, 424, 124, 36);
            Img(Child(plate, "Fill"), "gui_cut_fill", PANEL, 0.5f);
            Img(Child(plate, "Stroke"), "gui_cut_stroke3", ACCENT, 0.5f);
            if (gui.turn_count != null)
            {
                Reparent(gui.turn_count.rectTransform, Box("TurnSlot", 830, 428, 58, 28));
                StyleText(gui.turn_count, ACCENT, 13, true, TextAnchor.MiddleLeft);
            }
            if (gui.turn_timer != null)
            {
                RectTransform tslot = Box("TimerSlot", 892, 429, 52, 26);
                Img(tslot, "gui_cut_fill", DARK, 0.8f);
                Reparent(gui.turn_timer.rectTransform, tslot);
                StyleText(gui.turn_timer, TEXTC, 13, true, TextAnchor.MiddleCenter);
            }

            //END TURN — the kit button keeps its wiring (GameUI interactable logic)
            RectTransform et_slot = Box("EndTurnSlot", 824, 470, 124, 62);
            RectTransform et = gui.end_turn_button.GetComponent<RectTransform>();
            Reparent(et, et_slot);
            Image et_img = gui.end_turn_button.GetComponent<Image>();
            if (et_img == null)
                et_img = gui.end_turn_button.gameObject.AddComponent<Image>();
            et_img.sprite = S("gui_cut_fill");
            et_img.type = Image.Type.Sliced;
            et_img.pixelsPerUnitMultiplier = 0.5f;
            et_img.color = FILL;
            gui.end_turn_button.targetGraphic = et_img;
            //strip old kit decorations (rings, icons) except the label text
            for (int i = et.childCount - 1; i >= 0; i--)
            {
                Transform child = et.GetChild(i);
                if (child.GetComponent<Text>() == null)
                    child.gameObject.SetActive(false);
            }
            Img(Child(et, "CCG_Stroke"), "gui_cut_stroke3", ACCENT, 0.5f).transform.SetAsLastSibling();
            Img(Child(et, "CCG_Glow"), "gui_cut_glow3", new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.35f), 0.5f);
            foreach (Text t in et.GetComponentsInChildren<Text>(true))
            {
                t.text = "END TURN";
                StyleText(t, ACCENT, 20, true, TextAnchor.MiddleCenter);
                RectTransform trt = t.rectTransform;
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
            }
        }

        private void BuildPileTile(int index, string label, float x, float y, Color side, bool enemy, bool deck)
        {
            //No box/outline chrome (it buried the pile visual): the pile reads
            //as a free-standing CARD — rounded card silhouette with the back /
            //top-card art, grey stacked edges peeking out behind it.
            RectTransform tile = Box("Pile_" + label + (enemy ? "_E" : "_P"), x, y, 56, 72);
            Image bg = tile.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f); //invisible click surface
            bg.raycastTarget = true;

            //card-aspect insets inside the tile (618x922 in a 56x72 box)
            float card_w = 72f * (618f / 922f); //≈48.3
            float inset = (56f - card_w) * 0.5f;

            //stacked edges: rounded dark silhouettes offset up-right, revealed
            //as the pile grows
            for (int s = 1; s >= 0; s--)
            {
                RectTransform srt = Child(tile, "Stack" + s);
                float off = (s + 1) * 3.2f;
                srt.anchorMin = new Vector2(inset / 56f, 0f);
                srt.anchorMax = new Vector2(1f - inset / 56f, 1f);
                srt.offsetMin = new Vector2(off, off * 0.6f);
                srt.offsetMax = new Vector2(off, off);
                Image slay = Img(srt, "gui_semi", Color.black, 2.2f);
                float shade = 0.5f - s * 0.18f;
                slay.color = new Color(shade, shade, shade, 1f);
                slay.enabled = false;
                pile_stack[index, s] = slay;
            }

            //card body: rounded silhouette masks the cover-fit art
            RectTransform body = Child(tile, "CardBody");
            body.anchorMin = new Vector2(inset / 56f, 0f);
            body.anchorMax = new Vector2(1f - inset / 56f, 1f);
            Img(body, "gui_semi", DARK, 2.2f);
            body.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            //overscan: the baked back composite carries a transparent margin
            //that read as a black border — zoom past it so art fills the card
            RectTransform zoom = Child(body, "Zoom");
            zoom.anchorMin = new Vector2(-0.10f, -0.10f);
            zoom.anchorMax = new Vector2(1.10f, 1.10f);
            RectTransform art_rt = Child(zoom, "Art");
            Image art = art_rt.gameObject.AddComponent<Image>();
            art.raycastTarget = false;
            art_rt.gameObject.AddComponent<CCGCoverFit>();
            art.enabled = false;
            pile_art[index] = art;
            if (deck)
            {
                art.sprite = S("gui_cardback_common"); //baked RED back
                art.enabled = art.sprite != null;
            }
            //dark band behind the count so it reads over the art
            RectTransform scrim = Child(body, "Scrim");
            scrim.anchorMin = new Vector2(0f, 0.32f);
            scrim.anchorMax = new Vector2(1f, 0.68f);
            Image simg = scrim.gameObject.AddComponent<Image>();
            simg.color = new Color(0f, 0f, 0f, 0.5f);
            simg.raycastTarget = false;
            pile_counts[index] = Txt(Box("PileCount" + index, x, y + 21, 56, 30), "0", TEXTC, 20, true, TextAnchor.MiddleCenter);
            Txt(Box("PileLbl" + index, x - 8, y + 72, 72, 11), label, DIM, 8, false, TextAnchor.MiddleCenter);

            Button btn = tile.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => BrowsePile(enemy, deck));
        }

        private void BrowsePile(bool enemy, bool deck)
        {
            GameClient client = GameClient.Get();
            Game data = client != null ? client.GetGameData() : null;
            if (data == null)
                return;
            int pid = enemy ? client.GetOpponentPlayerID() : client.GetPlayerID();
            Player owner = data.GetPlayer(pid);
            if (owner == null)
                return;
            List<Card> cards = new List<Card>(deck ? owner.cards_deck : owner.cards_discard);
            cards.Sort((a, b) => a.CardData.title.CompareTo(b.CardData.title)); //never reveal deck order
            OpenBrowser(cards, (deck ? "DECK (" : "DISCARD (") + cards.Count + ")");
        }

        //=================== PILE BROWSER ===================
        //Own pane instead of the kit CardSelector: its card prefab's lifecycle
        //fought the skin system and cards rendered without title/rules text.
        //Faces here use CCGCardFace.Create directly — the exact path that
        //works on the board and in the hover preview — and hovering any card
        //shows it full size in the docked preview pane.

        private RectTransform browser_root;
        private Text browser_title;
        private RectTransform browser_content;

        private void BuildBrowser()
        {
            browser_root = Box("CCG_Browser", 446, 30, 506, 490);
            Image bg = Img(Child(browser_root, "Fill"), "gui_cut_fill", new Color(PANEL.r, PANEL.g, PANEL.b, 0.99f), 0.5f);
            bg.raycastTarget = true; //swallow clicks under the pane
            Img(Child(browser_root, "Stroke"), "gui_cut_stroke3", ACCENT, 0.5f);

            browser_title = Txt(Box("BrowserTitle", 462, 40, 340, 26), "", ACCENT, 18, true, TextAnchor.MiddleLeft);
            browser_title.transform.SetParent(browser_root, true);

            //close X
            RectTransform xr = Box("BrowserClose", 912, 38, 28, 28);
            xr.SetParent(browser_root, true);
            Image ximg = Img(xr, "gui_ic_close", TEXTC, 1f);
            ximg.type = Image.Type.Simple;
            ximg.preserveAspect = true;
            ximg.raycastTarget = true;
            Button xbtn = xr.gameObject.AddComponent<Button>();
            xbtn.targetGraphic = ximg;
            xbtn.onClick.AddListener(CloseBrowser);

            //scrollable grid
            RectTransform viewport = Box("BrowserViewport", 460, 74, 478, 434);
            viewport.SetParent(browser_root, true);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image vimg = viewport.gameObject.AddComponent<Image>();
            vimg.color = new Color(0f, 0f, 0f, 0.01f);
            vimg.raycastTarget = true;

            GameObject cgo = new GameObject("Content", typeof(RectTransform));
            browser_content = cgo.GetComponent<RectTransform>();
            browser_content.SetParent(viewport, false);
            browser_content.anchorMin = new Vector2(0f, 1f);
            browser_content.anchorMax = new Vector2(1f, 1f);
            browser_content.pivot = new Vector2(0.5f, 1f);
            browser_content.offsetMin = Vector2.zero;
            browser_content.offsetMax = Vector2.zero;
            GridLayoutGroup grid = cgo.AddComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.UpperCenter;
            ContentSizeFitter fitter = cgo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = browser_root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = browser_content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            browser_root.gameObject.SetActive(false);
        }

        private void OpenBrowser(List<Card> cards, string title)
        {
            if (browser_root == null)
                return;
            browser_root.gameObject.SetActive(true);
            browser_title.text = title;

            for (int i = browser_content.childCount - 1; i >= 0; i--)
                Destroy(browser_content.GetChild(i).gameObject);

            //cell size from the live viewport width: 4 columns of card-aspect cells
            Canvas.ForceUpdateCanvases();
            RectTransform viewport = browser_root.Find("BrowserViewport") as RectTransform;
            float vw = viewport.rect.width;
            GridLayoutGroup grid = browser_content.GetComponent<GridLayoutGroup>();
            float spacing = 14f;
            float cell_w = (vw - spacing * 4f) / 3f;
            float cell_h = cell_w * (922f / 618f);
            grid.cellSize = new Vector2(cell_w, cell_h);
            grid.spacing = new Vector2(spacing, spacing);
            grid.padding = new RectOffset((int)spacing, (int)spacing, (int)spacing, (int)spacing);

            foreach (Card card in cards)
            {
                GameObject cell = new GameObject("Card_" + card.card_id, typeof(RectTransform));
                RectTransform cell_rt = cell.GetComponent<RectTransform>();
                cell_rt.SetParent(browser_content, false);
                Image hit = cell.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                hit.raycastTarget = true;

                GameObject host = new GameObject("Face", typeof(RectTransform));
                RectTransform host_rt = host.GetComponent<RectTransform>();
                host_rt.SetParent(cell_rt, false);
                CCGCardFace face = CCGCardFace.Create(host_rt, cell_w / 618f, true);
                face.Apply(card.CardData);

                Card captured = card;
                HoverPreview(cell, () => captured);
            }
        }

        private void CloseBrowser()
        {
            if (browser_root != null)
                browser_root.gameObject.SetActive(false);
            ui_hover_card = null;
        }

        //=================== HOVER PREVIEW ===================

        /// <summary>Full-card preview pane docked at the board's left edge —
        /// how MTGO makes small tiles readable.</summary>
        /// <summary>
        /// Dock the kit's CardPreviewUI (big card + ability help text) into the
        /// mockup's preview pane — it was floating over the enemy cluster. It
        /// keeps its own show/hide logic; we feed it our chip hovers too.
        /// </summary>
        private void BuildPreview()
        {
            CardPreviewUI cp = Object.FindFirstObjectByType<CardPreviewUI>(FindObjectsInactive.Include);
            if (cp == null || cp.ui_panel == null)
                return;
            preview_panel = cp.ui_panel.GetComponent<RectTransform>();
            //the side ability/status list is redundant — the desc box below the
            //card already explains abilities (user rule). Only hide rows that
            //do NOT hold the card + desc themselves (CardRow does!)
            if (cp.side_rows != null)
            {
                foreach (RectTransform row in cp.side_rows)
                {
                    if (row == null)
                        continue;
                    bool holds_content = (cp.card_ui != null && cp.card_ui.transform.IsChildOf(row))
                        || (cp.desc != null && cp.desc.transform.IsChildOf(row));
                    if (!holds_content)
                        row.gameObject.SetActive(false);
                }
            }
            preview_slot = Box("PreviewSlot", 236, 60, 200, 420);
            preview_panel.SetParent(preview_slot, false);
            preview_panel.anchorMin = preview_panel.anchorMax = new Vector2(0f, 1f);
            preview_panel.pivot = new Vector2(0f, 1f);
            preview_panel.anchoredPosition = Vector2.zero;
            preview_scaled_w = -1f;
        }

        /// <summary>Scale the docked preview to fit its pane (sizes are only
        /// valid after canvas layout, so fit lazily).</summary>
        private void FitPreview()
        {
            if (preview_panel == null || preview_slot == null)
                return;
            float sw = preview_slot.rect.width;
            if (sw < 10f || Mathf.Abs(sw - preview_scaled_w) < 1f)
                return;
            float pw = preview_panel.rect.width;
            if (pw < 10f)
                return;
            preview_scaled_w = sw;
            //never shrink below 0.85: the desc/keyword box became an unreadable
            //black sliver at small scales (it may overflow the slot — fine,
            //it's a transient hover pane)
            preview_panel.localScale = Vector3.one * Mathf.Clamp(sw / pw, 0.85f, 1f);
        }

        public static Card GetUIHoverCard()
        {
            return instance != null ? instance.ui_hover_card : null;
        }

        private void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = type;
            entry.callback.AddListener((d) => { action(); });
            trigger.triggers.Add(entry);
        }

        private void HoverPreview(GameObject go, System.Func<Card> getter)
        {
            EventTrigger trigger = go.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = go.AddComponent<EventTrigger>();
            EventTrigger.Entry enter = new EventTrigger.Entry();
            enter.eventID = EventTriggerType.PointerEnter;
            enter.callback.AddListener((d) => { ui_hover_card = getter(); });
            trigger.triggers.Add(enter);
            EventTrigger.Entry exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener((d) => { ui_hover_card = null; });
            trigger.triggers.Add(exit);
        }

        //=================== DEFENSE WINDOW (FaB Phase 1) ===================

        private RectTransform defense_strip;
        private Text defense_txt;
        private Text defense_btn_txt;

        /// <summary>Docked strip above the hand (mockup defense_window.svg):
        /// incoming damage, remaining after blocks, RESOLVE/TAKE IT.</summary>
        private void BuildDefenseStrip()
        {
            defense_strip = Box("CCG_Defense", 236, 356, 576, 44);
            Img(Child(defense_strip, "Fill"), "gui_cut_fill", new Color(0.11f, 0.05f, 0.07f, 0.98f), 0.5f);
            Img(Child(defense_strip, "Stroke"), "gui_cut_stroke3", NEGATIVE, 0.5f);
            defense_txt = Txt(Box("DefTxt", 248, 360, 380, 36), "", TEXTC, 13, true, TextAnchor.MiddleLeft);
            defense_txt.transform.SetParent(defense_strip, true);

            RectTransform btn = Box("DefResolve", 690, 362, 112, 32);
            btn.SetParent(defense_strip, true);
            Image bimg = Img(btn, "gui_cut_fill", NEGATIVE, 0.8f);
            bimg.raycastTarget = true;
            defense_btn_txt = Txt(Box("DefBtnTxt", 690, 362, 112, 32), "RESOLVE", new Color(0.06f, 0.03f, 0.04f), 14, true, TextAnchor.MiddleCenter);
            defense_btn_txt.transform.SetParent(btn, true);
            Button b = btn.gameObject.AddComponent<Button>();
            b.targetGraphic = bimg;
            b.onClick.AddListener(() => {
                GameClient client = GameClient.Get();
                if (client != null)
                    client.CancelSelection(); //resolve with committed blocks
            });

            defense_strip.gameObject.SetActive(false);
        }

        private void RefreshDefense(Game data, GameClient client)
        {
            if (defense_strip == null)
                return;
            bool defending = data.selector == SelectorType.Defense
                && data.selector_player_id == client.GetPlayerID();
            if (defense_strip.gameObject.activeSelf != defending)
                defense_strip.gameObject.SetActive(defending);
            if (!defending)
                return;

            Card attacker = data.GetCard(data.selector_caster_uid);
            int total = attacker != null ? attacker.GetAttack() : 0;
            int remaining = 0;
            int.TryParse(data.selector_ability_id, out remaining);
            defense_txt.text = "INCOMING " + total + "  -  YOU TAKE " + remaining
                + "   (click hand cards to block)";
            defense_btn_txt.text = remaining >= total ? "TAKE IT" : "RESOLVE";
        }

        /// <summary>The kit HUD these docks replace.</summary>
        private void HideKitHud(GameUI gui)
        {
            Transform gc = gui.game_canvas.transform;
            foreach (string n in new[] { "TimerArea", "PlayerUI1", "PlayerUI2" })
            {
                Transform t = gc.Find(n);
                if (t != null)
                    t.gameObject.SetActive(false);
            }
        }

        //=================== REFRESH ===================

        private void Refresh()
        {
            GameClient client = GameClient.Get();
            Game data = client != null ? client.GetGameData() : null;
            if (data == null)
                return;
            Player me = data.GetPlayer(client.GetPlayerID());
            Player opp = data.GetOpponentPlayer(client.GetPlayerID());
            if (me == null || opp == null)
                return;

            RefreshCluster(me_cluster, data, me, false);
            RefreshCluster(opp_cluster, data, opp, true);
            RefreshDefense(data, client);

            RefreshPile(0, opp.cards_deck.Count, null);
            RefreshPile(1, opp.cards_discard.Count, opp);
            RefreshPile(2, me.cards_deck.Count, null);
            RefreshPile(3, me.cards_discard.Count, me);

            //Armed hero OR dragged minion over the enemy cluster: highlight it
            bool minion_drag = PlayerControls.Get() != null && PlayerControls.Get().GetSelected() != null;
            bool targeted = (CCGHeroAttackControls.IsArmedAny || minion_drag) && over_enemy_portrait;
            if (enemy_panel_stroke != null)
                enemy_panel_stroke.color = targeted
                    ? Color.Lerp(NEGATIVE, Color.white, Mathf.PingPong(Time.unscaledTime * 3f, 1f))
                    : NEGATIVE;
            if (enemy_panel_glow != null)
                enemy_panel_glow.color = new Color(NEGATIVE.r, NEGATIVE.g, NEGATIVE.b,
                    targeted ? 0.45f + 0.3f * Mathf.PingPong(Time.unscaledTime * 3f, 1f) : 0f);
        }

        /// <summary>Count text + top art (discards) + stacked-layer visibility.</summary>
        private void RefreshPile(int index, int count, Player discard_owner)
        {
            if (pile_counts[index] != null)
                pile_counts[index].text = count.ToString();

            if (discard_owner != null && pile_art[index] != null)
            {
                Sprite top = null;
                if (discard_owner.cards_discard.Count > 0)
                {
                    TcgEngine.CardData cd = discard_owner.cards_discard[discard_owner.cards_discard.Count - 1].CardData;
                    top = cd.art_board != null ? cd.art_board : cd.art_full;
                }
                if (pile_art[index].sprite != top)
                {
                    pile_art[index].sprite = top;
                    pile_art[index].enabled = top != null;
                }
            }

            //stacked edges appear as the pile grows
            if (pile_stack[index, 0] != null)
            {
                bool l0 = count >= 2;
                bool l1 = count >= 6;
                if (pile_stack[index, 0].enabled != l0) pile_stack[index, 0].enabled = l0;
                if (pile_stack[index, 1].enabled != l1) pile_stack[index, 1].enabled = l1;
            }
        }

        private void RefreshCluster(Cluster cl, Game data, Player p, bool enemy)
        {
            if (cl == null)
                return;
            Color side = enemy ? NEGATIVE : ACCENT;

            //Hero portrait + name (set once per hero); short name only — the
            //full title spilled out of the panel
            if (p.hero != null && p.hero.card_id != cl.hero_id)
            {
                cl.hero_id = p.hero.card_id;
                cl.portrait_art.sprite = p.hero.CardData.art_full;
                cl.portrait_art.enabled = cl.portrait_art.sprite != null;
                string title = p.hero.CardData.title;
                int cut = title.IndexOf(',');
                cl.name_txt.text = (cut > 0 ? title.Substring(0, cut) : title).ToUpper();
            }
            cl.hero_card = p.hero;

            //Armed swing highlight on YOUR portrait
            if (!enemy && cl.portrait_stroke != null)
            {
                bool armed = CCGHeroAttackControls.IsArmedAny;
                cl.portrait_stroke.color = armed
                    ? Color.Lerp(ACCENT, Color.white, Mathf.PingPong(Time.unscaledTime * 2f, 1f))
                    : side;
            }

            //Weapon: card art + swing stats
            Card weapon = FindPiece(p, "weapon");
            RefreshChip(cl.weapon, weapon, weapon != null ? "ATK " + weapon.GetAttack() : "-", side);
            cl.weapon_cost.text = weapon != null ? "COST " + weapon.GetMana() : "WEAPON";

            //Armor chips: card art + HP remaining
            for (int i = 0; i < 4; i++)
            {
                Card piece = FindPiece(p, armor_traits[i]);
                RefreshChip(cl.armor[i], piece, piece != null ? piece.GetHP().ToString() : "-", side);
            }

            //HP bar
            float frac = p.hp_max > 0 ? Mathf.Clamp01((float)p.hp / p.hp_max) : 0f;
            cl.hp_fill.anchorMin = Vector2.zero;
            cl.hp_fill.anchorMax = new Vector2(Mathf.Max(frac, 0.001f), 1f);
            cl.hp_fill.offsetMin = Vector2.zero;
            cl.hp_fill.offsetMax = Vector2.zero;
            cl.hp_txt.text = p.hp + " / " + p.hp_max;

            //Mana gems
            int shown = Mathf.Clamp(p.mana_max, 0, 10);
            for (int i = 0; i < 10; i++)
            {
                bool visible = i < shown;
                if (cl.orbs[i].gameObject.activeSelf != visible)
                    cl.orbs[i].gameObject.SetActive(visible);
                if (visible)
                    cl.orbs[i].color = i < p.mana ? side : new Color(FILL.r, FILL.g, FILL.b, 0.9f);
            }
        }

        private void RefreshChip(EquipChip chip, Card card, string stat, Color side)
        {
            string uid = card != null ? card.uid : "";
            if (uid != chip.uid)
            {
                chip.uid = uid;
                Sprite art = null;
                if (card != null)
                    art = card.CardData.art_board != null ? card.CardData.art_board : card.CardData.art_full;
                chip.art.sprite = art;
                chip.art.enabled = art != null;
            }
            chip.card = card;
            chip.txt.text = stat;
            chip.txt.color = card != null ? TEXTC : DIM;
            chip.scrim.SetActive(card != null);
            chip.stroke.color = card != null ? side : new Color(DIM.r, DIM.g, DIM.b, 0.5f);
        }

        private Card FindPiece(Player player, string trait)
        {
            foreach (Card c in player.cards_equip)
                if (c.CardData.HasTrait(trait))
                    return c;
            return null;
        }

        //=================== ENEMY HAND ===================

        /// <summary>
        /// Player hand keeps the kit's FAN (user call). The enemy hand-back fan
        /// shrinks slightly and tucks against the top edge with its upper half
        /// off-screen, like the kit default — plain scaling alone dragged it
        /// down onto the board (it shrinks toward the transform origin).
        /// </summary>
        private void TuckEnemyHand()
        {
            if (enemy_hand_scaled)
                return;
            GameObject eh = GameObject.Find("EnemyHand");
            if (eh == null)
                return;
            Transform ha = eh.transform.Find("HandArea");
            Vector3 anchor = ha != null ? ha.position : eh.transform.position;
            eh.transform.localScale = eh.transform.localScale * 0.85f;
            if (ha != null)
                eh.transform.position += anchor - ha.position; //shrink in place
            eh.transform.position += new Vector3(0f, 0f, 0.9f);  //tuck past the top edge
            enemy_hand_scaled = true;
        }

        //=================== HELPERS ===================

        /// <summary>Rect from mockup coords (x,y from top-left of the 960x540 template) → canvas anchors.</summary>
        private RectTransform Box(string name, float x, float y, float w, float h)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(root, false);
            rt.anchorMin = new Vector2(x / TW, 1f - (y + h) / TH);
            rt.anchorMax = new Vector2((x + w) / TW, 1f - y / TH);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private RectTransform Child(RectTransform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private Image Img(RectTransform rt, string sprite, Color color, float ppum = 1f)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = S(sprite);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ppum;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private Text Txt(RectTransform rt, string s, Color color, int size, bool bold, TextAnchor align)
        {
            Text t = rt.gameObject.AddComponent<Text>();
            t.text = s;
            StyleText(t, color, size, bold, align);
            return t;
        }

        private void StyleText(Text t, Color color, int size, bool bold, TextAnchor align)
        {
            t.font = bold ? raj_bold : raj_semibold;
            if (t.font == null)
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontStyle = FontStyle.Normal;
            t.color = color;
            t.alignment = align;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 4;
            t.resizeTextMaxSize = size * 2; //canvas ref-res is ~2x the mockup grid
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void Reparent(RectTransform rt, RectTransform slot)
        {
            rt.SetParent(slot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private Sprite S(string key) { return Resources.Load<Sprite>("GUI/" + key); }
    }
}
