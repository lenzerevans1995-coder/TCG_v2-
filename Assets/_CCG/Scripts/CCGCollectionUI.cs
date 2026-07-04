using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using TcgEngine;
using Michsky.UI.Heat;
using TCard = TcgEngine.CardData; //asset pack defines a global CardData

namespace CCG
{
    /// <summary>Left/right click relay for grid cards.</summary>
    /// <summary>Per-deck equipment loadout (weapon, head, chest, arms, legs
    /// tids) saved as json in PlayerPrefs under CCGCollectionUI.LOADOUT_PREF +
    /// deck tid; CCGGameLogic.SpawnLoadouts reads it at match start.</summary>
    [System.Serializable]
    public class CCGLoadoutSave
    {
        public string[] tids = new string[5];
    }

    public class CCGCardClick : MonoBehaviour, IPointerClickHandler
    {
        public System.Action onLeft;
        public System.Action onRight;
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left && onLeft != null) onLeft();
            if (e.button == PointerEventData.InputButton.Right && onRight != null) onRight();
        }
    }

    /// <summary>
    /// COLLECTION area of the Heat menu — three screens per the approved mockups:
    /// BROWSE (filtered viewing), DECKS (management + active deck), and a
    /// dedicated DECK BUILDER (filter chips + grid with copy pips, hero slot,
    /// mana curve, +/- rows, save/discard). Built from Heat-themed primitives
    /// (UIManagerImage/Text drive all colors/fonts) + CCGCardFace cards.
    /// </summary>
    public class CCGCollectionUI : MonoBehaviour
    {
        public const string ACTIVE_DECK_PREF = "ccg_active_deck";
        private const float CARD_SCALE = 0.285f;

        private Transform panel_root;
        private RectTransform browse_root, decks_root, builder_root, zoom_root;
        private Button tab_browse, tab_decks;
        private bool built = false;

        //browse filters
        private string f_search = "";
        private int f_faction = 0;             //0 all, 1 san, 2 bul
        private int f_type = 0;                //0 all, 1 minion, 2 spell, 3 gear, 4 field
        private int f_rarity = 0;              //0 all then rarity ranks
        private readonly HashSet<int> f_costs = new HashSet<int>();
        private bool f_owned = false;
        private Transform browse_grid;
        private TextMeshProUGUI browse_footer;
        private TextMeshProUGUI btn_faction_txt, btn_type_txt, btn_rarity_txt, btn_owned_txt;

        //decks screen
        private Transform deck_tiles;
        private RectTransform deck_detail;
        private UserDeckData selected_deck;

        //builder
        private UserDeckData working;
        private string working_source_tid;
        private bool dirty = false;
        private int b_faction = 0; //derived from the hero now (hero locks the pool)
        private int b_type = 0;
        private readonly HashSet<int> b_costs = new HashSet<int>();
        private string b_search = "";
        private bool b_owned = false;
        private int b_equip_slot = -1;              //selected loadout slot (-1 = card pool)
        private string[] b_loadout = new string[5]; //weapon, head, chest, arms, legs (tids)
        private readonly List<Button> b_type_chips = new List<Button>();
        private readonly List<Button> b_cost_chips = new List<Button>();
        private Button b_owned_chip;
        private readonly List<Image> b_slot_arts = new List<Image>();
        private readonly List<Image> b_slot_frames = new List<Image>();
        private readonly List<TextMeshProUGUI> b_slot_labels = new List<TextMeshProUGUI>();
        private TextMeshProUGUI b_checkout;
        private float nav_clearance = 112f;
        private Transform b_grid;
        private Transform b_rows;
        private TMP_InputField b_name;
        private TextMeshProUGUI b_count;
        private TextMeshProUGUI b_validity;
        private TextMeshProUGUI b_typesum;
        private Image b_hero_art;
        private Image b_hero_cover;
        private TextMeshProUGUI b_hero_name;
        private readonly List<Image> b_curve_bars = new List<Image>();
        private TextMeshProUGUI b_save_label;

        private UserData UData { get { return Authenticator.Get().UserData; } }

        //================= draft + loadout persistence =================

        private const string DRAFT_PREF = "ccg_draft_decks";       //";"-joined tids
        public const string LOADOUT_PREF = "ccg_loadout_";         //+deck tid -> CCGLoadoutSave json
        private static readonly string[] SLOT_TRAITS = { "weapon", "head", "chest", "arms", "legs" };
        private static readonly string[] SLOT_NAMES = { "WEAPON", "HEAD", "CHEST", "ARMS", "LEGS" };

        public static bool IsDraftDeck(string tid)
        {
            return (";" + PlayerPrefs.GetString(DRAFT_PREF, "") + ";").Contains(";" + tid + ";");
        }

        public static void SetDraftDeck(string tid, bool draft)
        {
            List<string> list = new List<string>(PlayerPrefs.GetString(DRAFT_PREF, "").Split(';'));
            list.RemoveAll(x => x == tid || x.Length == 0);
            if (draft)
                list.Add(tid);
            PlayerPrefs.SetString(DRAFT_PREF, string.Join(";", list));
        }

        /// <summary>Copies of each card the deck uses beyond what's owned,
        /// grouped by rarity — the wildcard bill.</summary>
        private Dictionary<RarityData, int> ComputeDebt(UserDeckData deck)
        {
            Dictionary<RarityData, int> debt = new Dictionary<RarityData, int>();
            if (deck == null || deck.cards == null)
                return debt;
            foreach (UserCardData uc in deck.cards)
            {
                TCard cd = TCard.Get(uc.tid);
                if (cd == null || cd.rarity == null) continue;
                int owed = uc.quantity - UData.GetCardQuantity(cd, VariantData.GetDefault());
                if (owed <= 0) continue;
                debt[cd.rarity] = (debt.TryGetValue(cd.rarity, out int cur) ? cur : 0) + owed;
            }
            return debt;
        }

        private bool CanAfford(Dictionary<RarityData, int> debt)
        {
            foreach (var kv in debt)
                if (UData.GetPackQuantity(CCGForgeUI.WildcardTid(kv.Key)) < kv.Value)
                    return false;
            return true;
        }

        private int OwedCopies(TCard cd, int qty)
        {
            return Mathf.Max(0, qty - UData.GetCardQuantity(cd, VariantData.GetDefault()));
        }

        /// <summary>Masked COVER-fit art child: banner/tile images must CROP to
        /// fill their frame, never stretch the whole painting (user rule).
        /// bias 1 = crop from the very top; wide banners use ~0.85 so heads
        /// land in frame instead of the sky above them.</summary>
        private Image MakeCoverArt(Image host, float bias = 1f)
        {
            if (host.GetComponent<Mask>() == null)
                host.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            GameObject go = new GameObject("CoverArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(host.transform, false);
            rt.SetAsFirstSibling();
            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            CCGCoverFit fit = go.AddComponent<CCGCoverFit>();
            fit.top_bias = bias;
            return img;
        }

        void Update()
        {
            if (built || Authenticator.Get() == null || Authenticator.Get().UserData == null)
                return;
            Transform canvas_t = GameObject.Find("Canvas - Main Menu").transform;
            panel_root = canvas_t.Find("Main Content/Extras/Content/Panel Content");
            if (panel_root == null)
                return;
            built = true;
            BuildAll();
        }

        //================= shared UI helpers =================

        private RectTransform MakeRect(string n, Transform parent)
        {
            GameObject go = new GameObject(n, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static void Anchor(RectTransform rt, Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax)
        {
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.offsetMin = omin; rt.offsetMax = omax;
        }

        private Image MakePanel(string n, Transform parent, UIManagerImage.ColorType color, float alpha)
        {
            RectTransform rt = MakeRect(n, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            UIManagerImage mi = rt.gameObject.AddComponent<UIManagerImage>();
            mi.colorType = color;
            Color c = img.color; c.a = alpha; img.color = c;
            return img;
        }

        private TextMeshProUGUI MakeText(string n, Transform parent, string text, float size,
            UIManagerText.FontType font, UIManagerText.ColorType color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            RectTransform rt = MakeRect(n, parent);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            UIManagerText mt = rt.gameObject.AddComponent<UIManagerText>();
            mt.fontType = font;
            mt.colorType = color;
            return tmp;
        }

        private Button MakeButton(string label, Transform parent, UnityEngine.Events.UnityAction action, float font_size = 20,
            UIManagerImage.ColorType bg = UIManagerImage.ColorType.Secondary,
            UIManagerText.ColorType fg = UIManagerText.ColorType.Primary, float bg_alpha = 1f)
        {
            Image img = MakePanel("Btn_" + label, parent, bg, bg_alpha);
            Button btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (action != null)
                btn.onClick.AddListener(action);
            TextMeshProUGUI txt = MakeText("Label", img.transform, label, font_size, UIManagerText.FontType.Semibold, fg);
            Anchor(txt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }

        private TMP_InputField MakeInput(string n, Transform parent, string placeholder, System.Action<string> on_change)
        {
            Image bg = MakePanel(n, parent, UIManagerImage.ColorType.Background, 0.9f);
            RectTransform area = MakeRect("TextArea", bg.transform);
            Anchor(area, Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-12f, -4f));
            area.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI ph = MakeText("Placeholder", area, placeholder, 20, UIManagerText.FontType.Regular, UIManagerText.ColorType.Primary, TextAlignmentOptions.MidlineLeft);
            ph.color = new Color(ph.color.r, ph.color.g, ph.color.b, 0.35f);
            ph.DestroyThemeDriver(); //keep the faded alpha
            Anchor(ph.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI txt = MakeText("Text", area, "", 20, UIManagerText.FontType.Regular, UIManagerText.ColorType.Primary, TextAlignmentOptions.MidlineLeft);
            Anchor(txt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TMP_InputField input = bg.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = bg;
            input.textViewport = area;
            input.textComponent = txt;
            input.placeholder = ph;
            if (on_change != null)
                input.onValueChanged.AddListener((v) => on_change(v));
            return input;
        }

        //================= construction =================

        private void BuildAll()
        {
            Transform demo = panel_root.Find("Box Container");
            if (demo != null) demo.gameObject.SetActive(false);
            Transform old = panel_root.Find("CCG_Collection");
            if (old != null) old.gameObject.SetActive(false);
            Transform old2 = panel_root.Find("CCG_DeckEditor");
            if (old2 != null) old2.gameObject.SetActive(false);

            //(fullscreen background art is handled globally per-tab by CCGHeatMenu)

            //MAX SCREEN USE (user rule): Heat's Panel Content ships with huge
            //insets (75 sides, 175 bottom, 190 top) — strip them so the
            //collection is flush with the sub-tabs and runs to the bottom
            RectTransform prt = panel_root as RectTransform;
            prt.offsetMin = new Vector2(24f, 12f);
            prt.offsetMax = new Vector2(-24f, -8f);
            Canvas.ForceUpdateCanvases();

            //NAV RULE: the header bar is measured, not guessed — nothing under it
            float NAV_H = CCGNavUtil.Clearance(panel_root as RectTransform);
            nav_clearance = NAV_H;

            //sub-tab bar: very top-left, aligned with the filter bar's left edge,
            //on its own row ABOVE the filter bar. Pivot BEFORE offsets (setting it
            //after shifts the rect by half its width — the earlier misalignment).
            RectTransform tabbar = MakeRect("CCG_SubTabs", panel_root);
            tabbar.pivot = new Vector2(0f, 1f);
            Anchor(tabbar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -NAV_H - 40f), new Vector2(390f, -NAV_H));
            HorizontalLayoutGroup tl = tabbar.gameObject.AddComponent<HorizontalLayoutGroup>();
            tl.spacing = 8f;
            tl.childForceExpandWidth = true;
            tl.childForceExpandHeight = true;
            tab_browse = MakeButton("BROWSE", tabbar, () => ShowScreen(0), 22, UIManagerImage.ColorType.Accent, UIManagerText.ColorType.AccentMatch);
            tab_decks = MakeButton("DECKS", tabbar, () => ShowScreen(1), 22);

            //roots (below nav + sub-tabs)
            browse_root = MakeRect("CCG_Browse", panel_root);
            Anchor(browse_root, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -NAV_H - 48f));
            decks_root = MakeRect("CCG_Decks", panel_root);
            Anchor(decks_root, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -NAV_H - 48f));
            builder_root = MakeRect("CCG_Builder", panel_root);
            Anchor(builder_root, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(0f, -NAV_H));

            BuildBrowse();
            BuildDecks();
            BuildBuilder();

            //zoom overlay on top
            zoom_root = MakeRect("CCG_Zoom", panel_root);
            Anchor(zoom_root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image dim = MakePanel("Dim", zoom_root, UIManagerImage.ColorType.Background, 0.88f);
            Anchor(dim.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Button close = dim.gameObject.AddComponent<Button>();
            close.targetGraphic = dim;
            close.onClick.AddListener(() => zoom_root.gameObject.SetActive(false));
            zoom_root.gameObject.SetActive(false);

            ShowScreen(0);
        }

        private void ShowScreen(int index)
        {
            browse_root.gameObject.SetActive(index == 0);
            decks_root.gameObject.SetActive(index == 1);
            builder_root.gameObject.SetActive(index == 2);
            bool tabs_visible = index != 2;
            panel_root.Find("CCG_SubTabs").gameObject.SetActive(tabs_visible);

            SetTabColors(tab_browse, index == 0);
            SetTabColors(tab_decks, index == 1);

            if (index == 0) RefreshBrowseGrid();
            if (index == 1) RefreshDecks();
        }

        private void SetTabColors(Button tab, bool active)
        {
            UIManagerImage mi = tab.GetComponent<UIManagerImage>();
            mi.colorType = active ? UIManagerImage.ColorType.Accent : UIManagerImage.ColorType.Secondary;
            UIManagerText mt = tab.GetComponentInChildren<UIManagerText>();
            mt.colorType = active ? UIManagerText.ColorType.AccentMatch : UIManagerText.ColorType.Primary;
        }

        /// <summary>Right SLIDE-OUT drawer (user rule): big card + info + native
        /// actions — UNLOCK copies in browse, ADD/REMOVE in the deck builder.</summary>
        private void ShowZoom(TCard card)
        {
            bool was_open = zoom_root.gameObject.activeSelf;
            for (int i = zoom_root.childCount - 1; i >= 1; i--)
                Destroy(zoom_root.GetChild(i).gameObject);

            bool in_builder = builder_root != null && builder_root.gameObject.activeSelf && working != null;
            int owned_qty = UData.GetCardQuantity(card, VariantData.GetDefault());
            Color rc = card.rarity != null ? CCGForgeUI.RarityColor(card.rarity) : Color.white;
            string rname = card.rarity != null ? (card.rarity.title != "" ? card.rarity.title : card.rarity.id).ToUpper() : "";

            Image drawer = MakePanel("Drawer", zoom_root, UIManagerImage.ColorType.Background, 0.99f);
            RectTransform drt = drawer.rectTransform;
            //below the nav bar - the header row stays clear (nav rule)
            Anchor(drt, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-470f, 0f), new Vector2(0f, -nav_clearance));
            if (!was_open)
                StartCoroutine(SlideIn(drt)); //fresh open slides; refreshes stay put
            Image edge = MakePanel("Edge", drawer.transform, UIManagerImage.ColorType.Accent, 1f);
            Anchor(edge.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(3f, 0f));

            //close X
            Button xb = MakeButton("X", drawer.transform, () => zoom_root.gameObject.SetActive(false), 22, UIManagerImage.ColorType.Background);
            Anchor(xb.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-54f, -54f), new Vector2(-10f, -10f));

            //big card
            RectTransform host = MakeRect("Card", drawer.transform);
            Anchor(host, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(0f, -320f));
            CCGCardFace face = CCGCardFace.Create(host, 0.56f, true);
            face.Apply(card);

            //info block
            TextMeshProUGUI nm = MakeText("Name", drawer.transform, card.title.ToUpper(), 24, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary);
            Anchor(nm.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -636f), new Vector2(-16f, -596f));
            TextMeshProUGUI meta = MakeText("Meta", drawer.transform, BuildTypeLineFor(card), 16, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary);
            meta.DestroyThemeDriver();
            meta.color = rc;
            Anchor(meta.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -668f), new Vector2(-16f, -640f));
            TextMeshProUGUI own = MakeText("Own", drawer.transform, "OWNED " + owned_qty, 16, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary);
            Anchor(own.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -700f), new Vector2(-16f, -672f));

            if (in_builder)
            {
                //deck actions
                int in_deck = CountInWorking(card.id);
                int max = GameplayData.Get().deck_duplicate_max;
                TextMeshProUGUI dk = MakeText("InDeck", drawer.transform, "IN DECK  " + in_deck + " / " + max, 18, UIManagerText.FontType.Bold, UIManagerText.ColorType.Accent);
                Anchor(dk.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 150f), new Vector2(-16f, 186f));

                Button minus = MakeButton("- REMOVE", drawer.transform, () => { RemoveFromWorking(card.id); ShowZoom(card); }, 18, UIManagerImage.ColorType.Secondary, UIManagerText.ColorType.Negative);
                Anchor(minus.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 0f), new Vector2(16f, 84f), new Vector2(-6f, 140f));
                Button plus = MakeButton(owned_qty > in_deck ? "+ ADD" : "+ ADD (1 " + rname + " WC)", drawer.transform, () => { AddToWorking(card); ShowZoom(card); }, 18, UIManagerImage.ColorType.Accent, UIManagerText.ColorType.AccentMatch);
                Anchor(plus.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 0f), new Vector2(6f, 84f), new Vector2(-16f, 140f));

                if (owned_qty <= in_deck)
                {
                    TextMeshProUGUI note = MakeText("Note", drawer.transform, "Unowned copies are paid with wildcards on checkout.", 13, UIManagerText.FontType.Regular, UIManagerText.ColorType.Primary);
                    note.textWrappingMode = TextWrappingModes.Normal;
                    note.color = new Color(note.color.r, note.color.g, note.color.b, 0.6f);
                    Anchor(note.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 40f), new Vector2(-16f, 78f));
                }
            }
            else if (card.rarity != null)
            {
                //browse: unlock copies natively with wildcards
                string wc_tid = CCGForgeUI.WildcardTid(card.rarity);
                int wc_owned = UData.GetPackQuantity(wc_tid);
                if (wc_owned > 0)
                {
                    Button ub = MakeButton("UNLOCK COPY  (1 " + rname + " WC, have " + wc_owned + ")", drawer.transform, () => UnlockCard(card), 16, UIManagerImage.ColorType.Accent, UIManagerText.ColorType.AccentMatch);
                    Anchor(ub.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 96f), new Vector2(-16f, 152f));
                }
                else
                {
                    Image nb = MakePanel("NoWc", drawer.transform, UIManagerImage.ColorType.Secondary, 1f);
                    Anchor(nb.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 96f), new Vector2(-16f, 152f));
                    TextMeshProUGUI nt = MakeText("NT", nb.transform, "NO " + rname + " WILDCARDS - VISIT THE FORGE", 14, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary);
                    nt.color = new Color(nt.color.r, nt.color.g, nt.color.b, 0.6f);
                    Anchor(nt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }
                TextMeshProUGUI hint = MakeText("H", drawer.transform, rname + " WILDCARD - bought or earned in THE FORGE", 12, UIManagerText.FontType.Regular, UIManagerText.ColorType.Primary);
                hint.DestroyThemeDriver();
                hint.color = rc;
                Anchor(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 60f), new Vector2(-16f, 90f));
            }

            zoom_root.gameObject.SetActive(true);
            zoom_root.SetAsLastSibling();
        }

        private string BuildTypeLineFor(TCard c)
        {
            string type = c.type.ToString().ToUpper();
            if (c.type == CardType.Character) type = "MINION";
            string team = c.team != null ? c.team.id.ToUpper() : "";
            string rar = c.rarity != null ? (c.rarity.title != "" ? c.rarity.title : c.rarity.id).ToUpper() : "";
            return type + (team != "" ? "  ·  " + team : "") + (rar != "" ? "  ·  " + rar : "");
        }

        private System.Collections.IEnumerator SlideIn(RectTransform rt)
        {
            Vector2 to_min = rt.offsetMin, to_max = rt.offsetMax;
            Vector2 from_min = to_min + new Vector2(480f, 0f);
            Vector2 from_max = to_max + new Vector2(480f, 0f);
            float t = 0f;
            while (t < 1f && rt != null)
            {
                t += Time.unscaledDeltaTime / 0.16f;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                rt.offsetMin = Vector2.Lerp(from_min, to_min, e);
                rt.offsetMax = Vector2.Lerp(from_max, to_max, e);
                yield return null;
            }
        }

        private async void UnlockCard(TCard card)
        {
            string wc_tid = CCGForgeUI.WildcardTid(card.rarity);
            if (UData.GetPackQuantity(wc_tid) <= 0)
                return;
            UData.AddPack(wc_tid, -1);
            UData.AddCard(card.id, VariantData.GetDefault().id, 1);
            await Authenticator.Get().SaveUserData();
            RefreshBrowseGrid();
            if (builder_root.gameObject.activeSelf)
                RefreshBuilderGrid();
            ShowZoom(card); //drawer stays open, counts refresh in place
            CCGForgeUI forge = GetComponent<CCGForgeUI>();
            if (forge != null)
                forge.RefreshTiles();
        }

        //================= filtering =================

        private static readonly string[] FACTIONS = { "ALL", "SAN", "BUL" };
        private static readonly string[] TYPES = { "ALL", "MINION", "SPELL", "GEAR", "FIELD" };
        private static readonly string[] RARITIES = { "ALL", "COMMON", "RARE", "EPIC", "MYTHIC" };

        private bool PassesFilters(TCard c, int fac, int type, int rarity, HashSet<int> costs, string search, bool owned_only)
        {
            if (c.availability == CardAvailability.Unlisted || c.type == CardType.Hero)
                return false;

            string team = c.team != null ? c.team.id : "";
            if (fac == 1 && team != "sanguine") return false;
            if (fac == 2 && team != "bulwark") return false;

            if (type == 1 && c.type != CardType.Character) return false;
            if (type == 2 && c.type != CardType.Spell) return false;
            if (type == 3 && c.type != CardType.Equipment && !(c.type == CardType.Artifact && !c.HasTrait("field"))) return false;
            if (type == 4 && !(c.type == CardType.Artifact && c.HasTrait("field"))) return false;

            if (rarity > 0)
            {
                string rid = c.rarity != null ? c.rarity.id.ToLower() : "";
                string want = RARITIES[rarity].ToLower();
                if (!rid.Contains(want) && !(want == "mythic" && rid.Contains("legend")))
                    return false;
            }

            if (costs.Count > 0)
            {
                int bucket = Mathf.Min(c.mana, 6);
                if (!costs.Contains(bucket)) return false;
            }

            if (!string.IsNullOrEmpty(search) && !c.title.ToLower().Contains(search.ToLower()))
                return false;

            if (owned_only && UData.GetCardQuantity(c, VariantData.GetDefault()) <= 0)
                return false;

            return true;
        }

        private List<TCard> FilteredCards(int fac, int type, int rarity, HashSet<int> costs, string search, bool owned_only)
        {
            List<TCard> list = new List<TCard>();
            foreach (TCard c in TCard.GetAll())
                if (PassesFilters(c, fac, type, rarity, costs, search, owned_only))
                    list.Add(c);
            list.Sort((a, b) => {
                int t = string.Compare(a.team != null ? a.team.id : "", b.team != null ? b.team.id : "");
                if (t != 0) return t;
                if (a.mana != b.mana) return a.mana.CompareTo(b.mana);
                return string.Compare(a.title, b.title);
            });
            return list;
        }

        //================= BROWSE =================

        private void BuildBrowse()
        {
            //filter bar
            Image bar = MakePanel("FilterBar", browse_root, UIManagerImage.ColorType.Secondary, 0.85f);
            Anchor(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f), new Vector2(0f, 0f));
            HorizontalLayoutGroup hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 8f;
            hl.padding = new RectOffset(10, 10, 6, 6);
            hl.childForceExpandHeight = true;
            hl.childForceExpandWidth = false;
            hl.childControlWidth = true;

            TMP_InputField search = MakeInput("Search", bar.transform, "search cards...", (v) => { f_search = v; RefreshBrowseGrid(); });
            LayoutElement sle = search.gameObject.AddComponent<LayoutElement>();
            sle.preferredWidth = 260f;

            btn_faction_txt = MakeCycleChip(bar.transform, "FACTION: ALL", 190f, () => {
                f_faction = (f_faction + 1) % FACTIONS.Length;
                btn_faction_txt.text = "FACTION: " + FACTIONS[f_faction] + "  ▾";
                RefreshBrowseGrid();
            });
            btn_type_txt = MakeCycleChip(bar.transform, "TYPE: ALL", 170f, () => {
                f_type = (f_type + 1) % TYPES.Length;
                btn_type_txt.text = "TYPE: " + TYPES[f_type] + "  ▾";
                RefreshBrowseGrid();
            });
            btn_rarity_txt = MakeCycleChip(bar.transform, "RARITY: ALL", 190f, () => {
                f_rarity = (f_rarity + 1) % RARITIES.Length;
                btn_rarity_txt.text = "RARITY: " + RARITIES[f_rarity] + "  ▾";
                RefreshBrowseGrid();
            });

            for (int cost = 1; cost <= 6; cost++)
            {
                int cc = cost;
                Button chip = MakeButton(cost == 6 ? "6+" : cost.ToString(), bar.transform, null, 18, UIManagerImage.ColorType.Background);
                LayoutElement le = chip.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 36f;
                chip.onClick.AddListener(() => {
                    if (!f_costs.Remove(cc)) f_costs.Add(cc);
                    SetChipState(chip, f_costs.Contains(cc));
                    RefreshBrowseGrid();
                });
            }

            btn_owned_txt = MakeCycleChip(bar.transform, "OWNED: OFF", 160f, () => {
                f_owned = !f_owned;
                btn_owned_txt.text = f_owned ? "OWNED: ON" : "OWNED: OFF";
                RefreshBrowseGrid();
            });

            //backing behind the card grid — SAME dark scheme as the DECKS
            //section (user rule; the old light-grey primitive clashed)
            Image gb = MakePanel("GridBack", browse_root, UIManagerImage.ColorType.Secondary, 0.7f);
            Anchor(gb.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 28f), new Vector2(0f, -50f));
            gb.raycastTarget = false;

            //grid
            browse_grid = MakeScrollGrid(browse_root, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 30f), new Vector2(0f, -52f));

            //footer
            browse_footer = MakeText("Footer", browse_root, "", 18, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary);
            Anchor(browse_footer.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 2f), new Vector2(0f, 26f));
        }

        private TextMeshProUGUI MakeCycleChip(Transform parent, string label, float width, UnityEngine.Events.UnityAction action)
        {
            Button b = MakeButton(label + "  ▾", parent, action, 18, UIManagerImage.ColorType.Background);
            LayoutElement le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            return b.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void SetChipState(Button chip, bool on)
        {
            UIManagerImage mi = chip.GetComponent<UIManagerImage>();
            mi.colorType = on ? UIManagerImage.ColorType.Accent : UIManagerImage.ColorType.Background;
            UIManagerText mt = chip.GetComponentInChildren<UIManagerText>();
            mt.colorType = on ? UIManagerText.ColorType.AccentMatch : UIManagerText.ColorType.Primary;
        }

        private Transform MakeScrollGrid(Transform parent, Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax)
        {
            GameObject scroll_go = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
            RectTransform scroll_rt = scroll_go.GetComponent<RectTransform>();
            scroll_rt.SetParent(parent, false);
            Anchor(scroll_rt, amin, amax, omin, omax);
            ScrollRect scroll = scroll_go.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30f;
            RectTransform content = MakeRect("Content", scroll_rt);
            Anchor(content, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(618f * CARD_SCALE, 922f * CARD_SCALE + 10f);
            grid.spacing = new Vector2(14f, 14f);
            grid.padding = new RectOffset(6, 6, 6, 6);
            grid.childAlignment = TextAnchor.UpperCenter;
            ContentSizeFitter fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            return content;
        }

        private void RefreshBrowseGrid()
        {
            if (browse_grid == null) return;
            for (int i = browse_grid.childCount - 1; i >= 0; i--)
                Destroy(browse_grid.GetChild(i).gameObject);

            List<TCard> cards = FilteredCards(f_faction, f_type, f_rarity, f_costs, f_search, f_owned);
            int total = 0, owned = 0;
            foreach (TCard c in TCard.GetAll())
            {
                if (c.availability == CardAvailability.Unlisted || c.type == CardType.Hero) continue;
                total++;
                if (UData.GetCardQuantity(c, VariantData.GetDefault()) > 0) owned++;
            }

            foreach (TCard card in cards)
            {
                TCard c = card;
                int qty = UData.GetCardQuantity(c, VariantData.GetDefault());
                RectTransform cell = MakeRect("Card_" + c.id, browse_grid);
                CCGCardFace face = CCGCardFace.Create(cell, CARD_SCALE, true);
                face.Apply(c);
                if (qty <= 0)
                {
                    CanvasGroup cg = cell.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0.35f;
                }
                //owned badge
                Image badge = MakePanel("Qty", cell, qty > 0 ? UIManagerImage.ColorType.Accent : UIManagerImage.ColorType.Secondary, 1f);
                Anchor(badge.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-44f, 2f), new Vector2(-4f, 24f));
                TextMeshProUGUI bt = MakeText("N", badge.transform, "x" + qty, 15, UIManagerText.FontType.Bold,
                    qty > 0 ? UIManagerText.ColorType.AccentMatch : UIManagerText.ColorType.Primary);
                Anchor(bt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                Image hit = cell.gameObject.AddComponent<Image>();
                hit.color = new Color(0, 0, 0, 0);
                CCGCardClick click = cell.gameObject.AddComponent<CCGCardClick>();
                click.onLeft = () => ShowZoom(c);
                click.onRight = () => ShowZoom(c);
            }

            if (browse_footer != null)
                browse_footer.text = total + " CARDS · " + owned + " OWNED · SHOWING " + cards.Count;
        }

        //================= DECKS =================

        private void BuildDecks()
        {
            deck_tiles = MakeScrollGrid(decks_root, new Vector2(0f, 0f), new Vector2(0.6f, 1f), new Vector2(0f, 0f), new Vector2(-8f, 0f));
            GridLayoutGroup g = deck_tiles.GetComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(300f, 200f);
            g.spacing = new Vector2(16f, 16f);
            g.childAlignment = TextAnchor.UpperLeft; //first deck starts hard LEFT under the tabs (user rule)
            g.padding = new RectOffset(0, 0, 6, 6);

            deck_detail = MakeRect("Detail", decks_root);
            Anchor(deck_detail, new Vector2(0.6f, 0f), new Vector2(1f, 1f), new Vector2(8f, 0f), new Vector2(0f, 0f));
            Image bg = MakePanel("BG", deck_detail, UIManagerImage.ColorType.Secondary, 0.7f);
            Anchor(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void RefreshDecks()
        {
            if (deck_tiles == null) return;
            for (int i = deck_tiles.childCount - 1; i >= 0; i--)
                Destroy(deck_tiles.GetChild(i).gameObject);

            string active = PlayerPrefs.GetString(ACTIVE_DECK_PREF, "");
            List<UserDeckData> decks = new List<UserDeckData>(UData.decks != null ? UData.decks : new UserDeckData[0]);
            foreach (UserDeckData d in decks)
            {
                UserDeckData deck = d;
                RectTransform tile = MakeRect("Deck_" + deck.tid, deck_tiles);
                Image art = MakePanel("Art", tile, UIManagerImage.ColorType.Secondary, 1f);
                Anchor(art.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                TCard hero = TCard.Get(deck.hero != null ? deck.hero.tid : null);
                if (hero != null)
                {
                    art.DestroyThemeDriver();
                    art.color = new Color(0.04f, 0.06f, 0.1f, 1f);
                    Image cover = MakeCoverArt(art);
                    cover.sprite = hero.GetFullArt(VariantData.GetDefault());
                }
                Image label_bg = MakePanel("LabelBG", tile, UIManagerImage.ColorType.Background, 0.85f);
                Anchor(label_bg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 52f));
                TextMeshProUGUI name = MakeText("Name", label_bg.transform, deck.title.ToUpper(), 20, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary, TextAlignmentOptions.TopLeft);
                Anchor(name.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 24f), new Vector2(-10f, -4f));
                bool valid = deck.IsValid();
                bool is_draft = IsDraftDeck(deck.tid);
                TextMeshProUGUI status = MakeText("Status", label_bg.transform,
                    deck.GetQuantity() + " / " + GameplayData.Get().deck_size + (is_draft ? "  DRAFT" : (valid ? "  READY" : "  INCOMPLETE")),
                    16, UIManagerText.FontType.Medium, valid && !is_draft ? UIManagerText.ColorType.Accent : UIManagerText.ColorType.Negative, TextAlignmentOptions.BottomLeft);
                if (is_draft)
                    status.color = new Color(0.91f, 0.78f, 0.48f); //gold: unpaid cards
                Anchor(status.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 2f), new Vector2(-10f, -28f));

                if (is_draft)
                {
                    Image db = MakePanel("Draft", tile, UIManagerImage.ColorType.Background, 0.95f);
                    db.DestroyThemeDriver();
                    db.color = new Color(0.91f, 0.78f, 0.48f, 1f);
                    Anchor(db.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -26f), new Vector2(70f, -6f));
                    TextMeshProUGUI dt = MakeText("T", db.transform, "DRAFT", 12, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary);
                    dt.color = new Color(0.12f, 0.09f, 0.02f);
                    Anchor(dt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }

                if (deck.tid == active)
                {
                    Image ab = MakePanel("Active", tile, UIManagerImage.ColorType.Accent, 1f);
                    Anchor(ab.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-76f, -26f), new Vector2(-6f, -6f));
                    TextMeshProUGUI at = MakeText("T", ab.transform, "ACTIVE", 12, UIManagerText.FontType.Bold, UIManagerText.ColorType.AccentMatch);
                    Anchor(at.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }

                Button btn = tile.gameObject.AddComponent<Button>();
                btn.targetGraphic = art;
                btn.onClick.AddListener(() => { selected_deck = deck; RefreshDeckDetail(); });
            }

            //new deck tile
            RectTransform nt = MakeRect("NewDeck", deck_tiles);
            Image nbg = MakePanel("BG", nt, UIManagerImage.ColorType.Secondary, 0.6f);
            Anchor(nbg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI plus = MakeText("Plus", nt, "+\nNEW DECK", 26, UIManagerText.FontType.Bold, UIManagerText.ColorType.Accent);
            Anchor(plus.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Button nb = nt.gameObject.AddComponent<Button>();
            nb.targetGraphic = nbg;
            nb.onClick.AddListener(() => {
                working = new UserDeckData("deck_" + GameTool.GenerateRandomID(4, 7), "NEW DECK");
                working.cards = new UserCardData[0];
                working_source_tid = null;
                dirty = true;
                OpenBuilder();
            });

            if (selected_deck == null && decks.Count > 0)
                selected_deck = decks[0];
            RefreshDeckDetail();
        }

        private void RefreshDeckDetail()
        {
            for (int i = deck_detail.childCount - 1; i >= 1; i--)
                Destroy(deck_detail.GetChild(i).gameObject);
            if (selected_deck == null)
                return;
            UserDeckData deck = selected_deck;

            //hero banner
            Image banner = MakePanel("Banner", deck_detail, UIManagerImage.ColorType.Background, 1f);
            Anchor(banner.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -150f), new Vector2(-14f, -14f));
            TCard hero = TCard.Get(deck.hero != null ? deck.hero.tid : null);
            if (hero != null)
            {
                banner.DestroyThemeDriver();
                banner.color = new Color(0.04f, 0.06f, 0.1f, 1f);
                Image cover = MakeCoverArt(banner, 0.85f); //wide banner: slight downward bias
                cover.sprite = hero.GetFullArt(VariantData.GetDefault());
            }
            Image bl = MakePanel("BL", banner.transform, UIManagerImage.ColorType.Background, 0.8f);
            Anchor(bl.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 34f));
            TextMeshProUGUI bt = MakeText("N", bl.transform, deck.title.ToUpper() + (hero != null ? "   ·   " + hero.title.ToUpper() : ""), 17, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary, TextAlignmentOptions.MidlineLeft);
            Anchor(bt.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));

            //stats
            int minions = 0, spells = 0, gear = 0;
            foreach (UserCardData uc in deck.cards)
            {
                TCard cd = TCard.Get(uc.tid);
                if (cd == null) continue;
                if (cd.type == CardType.Character) minions += uc.quantity;
                else if (cd.type == CardType.Spell) spells += uc.quantity;
                else gear += uc.quantity;
            }
            TextMeshProUGUI stats = MakeText("Stats", deck_detail,
                "CARDS " + deck.GetQuantity() + "/" + GameplayData.Get().deck_size + "     MINIONS " + minions + "     SPELLS " + spells + "     OTHER " + gear,
                16, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary, TextAlignmentOptions.MidlineLeft);
            Anchor(stats.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -186f), new Vector2(-14f, -156f));

            //buttons
            RectTransform brow = MakeRect("Buttons", deck_detail);
            Anchor(brow, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 14f), new Vector2(-14f, 210f));
            VerticalLayoutGroup vl = brow.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 8f;
            vl.childForceExpandHeight = true;
            vl.childForceExpandWidth = true;

            MakeButton("EDIT DECK  →", brow, () => {
                working = CloneDeck(deck);
                working_source_tid = deck.tid;
                dirty = false;
                OpenBuilder();
            }, 20, UIManagerImage.ColorType.Accent, UIManagerText.ColorType.AccentMatch);
            MakeButton("DUPLICATE", brow, () => {
                UserDeckData copy = CloneDeck(deck);
                copy.tid = "deck_" + GameTool.GenerateRandomID(4, 7);
                copy.title = deck.title + " COPY";
                UpsertDeck(copy);
                SaveUser();
                RefreshDecks();
            }, 20);
            Button set_active = MakeButton(IsDraftDeck(deck.tid) ? "DRAFT — PAY IN EDITOR TO PLAY" : "SET AS ACTIVE DECK", brow, () => {
                if (IsDraftDeck(deck.tid))
                    return; //drafts hold unpaid cards: not playable
                PlayerPrefs.SetString(ACTIVE_DECK_PREF, deck.tid);
                RefreshDecks();
            }, 20);
            set_active.interactable = !IsDraftDeck(deck.tid);
            MakeButton("DELETE", brow, () => {
                List<UserDeckData> list = new List<UserDeckData>(UData.decks);
                list.RemoveAll(x => x.tid == deck.tid);
                UData.decks = list.ToArray();
                if (PlayerPrefs.GetString(ACTIVE_DECK_PREF, "") == deck.tid)
                    PlayerPrefs.DeleteKey(ACTIVE_DECK_PREF);
                selected_deck = null;
                SaveUser();
                RefreshDecks();
            }, 20, UIManagerImage.ColorType.Negative);
        }

        private UserDeckData CloneDeck(UserDeckData src)
        {
            UserDeckData d = new UserDeckData(src.tid, src.title);
            d.hero = src.hero;
            //merge duplicate entries (starter claims store each copy as its own
            //quantity-1 entry) so rows aggregate as "x2"
            Dictionary<string, UserCardData> merged = new Dictionary<string, UserCardData>();
            if (src.cards != null)
            {
                foreach (UserCardData uc in src.cards)
                {
                    UserCardData m;
                    if (merged.TryGetValue(uc.tid, out m))
                        m.quantity += uc.quantity;
                    else
                    {
                        m = new UserCardData();
                        m.tid = uc.tid; m.variant = uc.variant; m.quantity = uc.quantity;
                        merged[uc.tid] = m;
                    }
                }
            }
            d.cards = new List<UserCardData>(merged.Values).ToArray();
            return d;
        }

        private async void SaveUser()
        {
            await Authenticator.Get().SaveUserData();
        }

        /// <summary>Insert-or-replace by tid. Kit AddDeck always appends (and
        /// grants collection cards) — wrong for editing.</summary>
        private void UpsertDeck(UserDeckData deck)
        {
            List<UserDeckData> list = new List<UserDeckData>(UData.decks != null ? UData.decks : new UserDeckData[0]);
            int idx = list.FindIndex(d => d.tid == deck.tid);
            if (idx >= 0) list[idx] = deck;
            else list.Add(deck);
            UData.decks = list.ToArray();
        }

        //================= BUILDER =================

        private void BuildBuilder()
        {
            Image bg = MakePanel("BG", builder_root, UIManagerImage.ColorType.Background, 0.97f);
            Anchor(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            //header
            RectTransform header = MakeRect("Header", builder_root);
            Anchor(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -50f), new Vector2(-10f, -8f));
            Button back = MakeButton("←", header, TryCloseBuilder, 24);
            Anchor(back.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(48f, 0f));
            b_name = MakeInput("Name", header, "deck name", (v) => { if (working != null) { working.title = v; dirty = true; RefreshSaveState(); } });
            Anchor(b_name.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(56f, 0f), new Vector2(380f, 0f));
            b_count = MakeText("Count", header, "0 / 20", 26, UIManagerText.FontType.Bold, UIManagerText.ColorType.Accent, TextAlignmentOptions.MidlineLeft);
            Anchor(b_count.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(396f, 0f), new Vector2(520f, 0f));
            b_validity = MakeText("Validity", header, "", 15, UIManagerText.FontType.Medium, UIManagerText.ColorType.Negative, TextAlignmentOptions.MidlineLeft);
            Anchor(b_validity.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(530f, 0f), new Vector2(760f, 0f));
            Button discard = MakeButton("DISCARD", header, TryCloseBuilder, 18);
            Anchor(discard.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-260f, 0f), new Vector2(-140f, 0f));
            Button save = MakeButton("SAVE", header, SaveWorking, 20, UIManagerImage.ColorType.Accent, UIManagerText.ColorType.AccentMatch);
            Anchor(save.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-130f, 0f), new Vector2(0f, 0f));
            b_save_label = save.GetComponentInChildren<TextMeshProUGUI>();

            //filter chips row
            Image bar = MakePanel("Filters", builder_root, UIManagerImage.ColorType.Secondary, 0.85f);
            Anchor(bar.rectTransform, new Vector2(0f, 1f), new Vector2(0.66f, 1f), new Vector2(10f, -92f), new Vector2(-6f, -56f));
            HorizontalLayoutGroup hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 6f;
            hl.padding = new RectOffset(8, 8, 5, 5);
            hl.childForceExpandHeight = true;
            hl.childForceExpandWidth = false;
            hl.childControlWidth = true;

            //NO faction chips: the HERO locks the card pool to its faction
            b_type_chips.Clear();
            string[] types = { "ALL", "MINION", "SPELL", "GEAR", "FIELD" };
            for (int i = 0; i < types.Length; i++)
            {
                int ti = i;
                Button chip = MakeButton(types[i], bar.transform, null, 15, i == 0 ? UIManagerImage.ColorType.Accent : UIManagerImage.ColorType.Background,
                    i == 0 ? UIManagerText.ColorType.AccentMatch : UIManagerText.ColorType.Primary);
                LayoutElement le = chip.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = i == 0 ? 48f : 72f;
                b_type_chips.Add(chip);
                chip.onClick.AddListener(() => {
                    b_type = ti;
                    for (int k = 0; k < b_type_chips.Count; k++) SetChipState(b_type_chips[k], k == ti);
                    RefreshBuilderGrid();
                });
            }
            //cost chips (multi-select buckets, 4+ = 4..6)
            b_cost_chips.Clear();
            string[] cost_labels = { "1", "2", "3", "4+" };
            for (int i = 0; i < cost_labels.Length; i++)
            {
                int ci = i;
                Button chip = MakeButton(cost_labels[i], bar.transform, null, 15, UIManagerImage.ColorType.Background, UIManagerText.ColorType.Primary);
                LayoutElement le = chip.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 34f;
                b_cost_chips.Add(chip);
                chip.onClick.AddListener(() => {
                    //bucket 4+ covers 4,5,6 (PassesFilters clamps mana to 6)
                    int[] buckets = ci < 3 ? new[] { ci + 1 } : new[] { 4, 5, 6 };
                    bool on = b_costs.Contains(buckets[0]);
                    foreach (int b in buckets)
                        if (on) b_costs.Remove(b); else b_costs.Add(b);
                    SetChipState(chip, !on);
                    RefreshBuilderGrid();
                });
            }
            //owned toggle
            b_owned_chip = MakeButton("OWNED", bar.transform, null, 14, UIManagerImage.ColorType.Background, UIManagerText.ColorType.Primary);
            LayoutElement ole = b_owned_chip.gameObject.AddComponent<LayoutElement>();
            ole.preferredWidth = 70f;
            b_owned_chip.onClick.AddListener(() => {
                b_owned = !b_owned;
                SetChipState(b_owned_chip, b_owned);
                RefreshBuilderGrid();
            });
            TMP_InputField bs = MakeInput("Search", bar.transform, "search...", (v) => { b_search = v; RefreshBuilderGrid(); });
            LayoutElement ble = bs.gameObject.AddComponent<LayoutElement>();
            ble.preferredWidth = 150f;

            //grid
            b_grid = MakeScrollGrid(builder_root, new Vector2(0f, 0f), new Vector2(0.66f, 1f), new Vector2(10f, 10f), new Vector2(-6f, -96f));

            //deck rail
            Image rail = MakePanel("Rail", builder_root, UIManagerImage.ColorType.Secondary, 0.8f);
            Anchor(rail.rectTransform, new Vector2(0.66f, 0f), new Vector2(1f, 1f), new Vector2(4f, 10f), new Vector2(-10f, -56f));

            //hero slot
            b_hero_art = MakePanel("HeroArt", rail.transform, UIManagerImage.ColorType.Background, 1f);
            Anchor(b_hero_art.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -86f), new Vector2(-12f, -12f));
            b_hero_cover = MakeCoverArt(b_hero_art, 0.85f); //wide banner bias
            b_hero_cover.enabled = false;
            Image hb = MakePanel("HB", b_hero_art.transform, UIManagerImage.ColorType.Background, 0.8f);
            Anchor(hb.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 28f));
            b_hero_name = MakeText("HeroName", hb.transform, "PICK A HERO", 15, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary, TextAlignmentOptions.MidlineLeft);
            Anchor(b_hero_name.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-70f, 0f));
            Button hchange = MakeButton("CHANGE", b_hero_art.transform, CycleHero, 12, UIManagerImage.ColorType.Background);
            Anchor(hchange.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-66f, 3f), new Vector2(-4f, 25f));

            //EQUIPMENT loadout slots: weapon + 4 armor. Click a slot to filter
            //the grid to matching gear; click a gear card to socket it.
            b_slot_arts.Clear(); b_slot_frames.Clear(); b_slot_labels.Clear();
            RectTransform equip_row = MakeRect("Equip", rail.transform);
            Anchor(equip_row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -146f), new Vector2(-12f, -92f));
            HorizontalLayoutGroup el = equip_row.gameObject.AddComponent<HorizontalLayoutGroup>();
            el.spacing = 5f;
            el.childForceExpandWidth = true;
            el.childForceExpandHeight = true;
            for (int i = 0; i < 5; i++)
            {
                int si = i;
                Image frame = MakePanel("Slot" + i, equip_row, UIManagerImage.ColorType.Background, 0.9f);
                frame.DestroyThemeDriver();
                frame.color = new Color(0.07f, 0.1f, 0.16f, 1f);
                b_slot_frames.Add(frame);
                Image art = MakeCoverArt(frame);
                art.enabled = false;
                b_slot_arts.Add(art);
                TextMeshProUGUI lbl = MakeText("L", frame.transform, SLOT_NAMES[i], 9, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary, TextAlignmentOptions.Bottom);
                Anchor(lbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(-1f, -1f));
                b_slot_labels.Add(lbl);
                Button sb = frame.gameObject.AddComponent<Button>();
                sb.targetGraphic = frame;
                sb.onClick.AddListener(() => {
                    b_equip_slot = (b_equip_slot == si) ? -1 : si; //toggle
                    RefreshBuilderRail();
                    RefreshBuilderGrid();
                });
            }

            //curve
            RectTransform curve = MakeRect("Curve", rail.transform);
            Anchor(curve, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -198f), new Vector2(-12f, -152f));
            HorizontalLayoutGroup cl = curve.gameObject.AddComponent<HorizontalLayoutGroup>();
            cl.spacing = 5f;
            cl.childForceExpandWidth = true;
            cl.childForceExpandHeight = false;
            cl.childAlignment = TextAnchor.LowerCenter;
            cl.childControlHeight = true; //LayoutElement.preferredHeight drives bar height
            b_curve_bars.Clear();
            for (int i = 0; i < 7; i++)
            {
                Image bar_img = MakePanel("Bar" + i, curve, UIManagerImage.ColorType.Accent, 0.85f);
                LayoutElement le = bar_img.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 4f;
                b_curve_bars.Add(bar_img);
            }

            //rows scroll
            GameObject rs_go = new GameObject("Rows", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
            RectTransform rs_rt = rs_go.GetComponent<RectTransform>();
            rs_rt.SetParent(rail.transform, false);
            Anchor(rs_rt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(12f, 96f), new Vector2(-12f, -204f));
            ScrollRect rs = rs_go.GetComponent<ScrollRect>();
            rs.horizontal = false;
            rs.scrollSensitivity = 25f;
            RectTransform rc = MakeRect("Content", rs_rt);
            Anchor(rc, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            rc.pivot = new Vector2(0.5f, 1f);
            VerticalLayoutGroup rvl = rc.gameObject.AddComponent<VerticalLayoutGroup>();
            rvl.spacing = 4f;
            rvl.childForceExpandHeight = false;
            rvl.childForceExpandWidth = true;
            ContentSizeFitter rfit = rc.gameObject.AddComponent<ContentSizeFitter>();
            rfit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            rs.content = rc;
            b_rows = rc;

            //type summary
            b_typesum = MakeText("TypeSum", rail.transform, "", 13, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary, TextAlignmentOptions.MidlineLeft);
            Anchor(b_typesum.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 68f), new Vector2(-12f, 92f));

            //checkout summary: the wildcard bill for unowned copies
            b_checkout = MakeText("Checkout", rail.transform, "", 13, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary, TextAlignmentOptions.TopLeft);
            Anchor(b_checkout.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 8f), new Vector2(-12f, 64f));

            builder_root.gameObject.SetActive(false);
        }

        private int CountOffFaction()
        {
            TCard hero = TCard.Get(working != null && working.hero != null ? working.hero.tid : null);
            string hero_team = hero != null && hero.team != null ? hero.team.id : "";
            if (hero_team == "" || working == null || working.cards == null) return 0;
            int n = 0;
            foreach (UserCardData uc in working.cards)
            {
                TCard cd = TCard.Get(uc.tid);
                string t = cd != null && cd.team != null ? cd.team.id : "";
                if (t != "" && t != hero_team) n += uc.quantity;
            }
            return n;
        }

        private Color RarityColor2(RarityData r)
        {
            string rid = r != null ? r.id.ToLower() : "";
            foreach (var kv in RARITY_COLORS)
                if (rid.Contains(kv.Key)) return kv.Value;
            if (rid.Contains("legend")) return RARITY_COLORS["mythic"];
            return RARITY_COLORS["common"];
        }

        private void OpenBuilder()
        {
            ShowScreen(2);
            b_name.SetTextWithoutNotify(working.title); //don't trip the dirty flag
            b_type = 0; b_search = ""; b_owned = false; b_equip_slot = -1;
            b_costs.Clear();
            for (int k = 0; k < b_type_chips.Count; k++) SetChipState(b_type_chips[k], k == 0);
            foreach (Button cc in b_cost_chips) SetChipState(cc, false);
            if (b_owned_chip != null) SetChipState(b_owned_chip, false);

            //per-deck equipment loadout; first open prefills from the hero's
            //faction starter so decks never enter battle naked
            b_loadout = new string[5];
            string json = PlayerPrefs.GetString(LOADOUT_PREF + working.tid, "");
            if (!string.IsNullOrEmpty(json))
            {
                CCGLoadoutSave save = JsonUtility.FromJson<CCGLoadoutSave>(json);
                if (save != null && save.tids != null)
                    for (int i = 0; i < 5 && i < save.tids.Length; i++)
                        b_loadout[i] = save.tids[i];
            }
            else
            {
                PrefillLoadoutFromStarter();
            }

            RefreshBuilderGrid();
            RefreshBuilderRail();
            RefreshSaveState();
        }

        private void PrefillLoadoutFromStarter()
        {
            TCard hero = TCard.Get(working != null && working.hero != null ? working.hero.tid : null);
            string team = hero != null && hero.team != null ? hero.team.id : "";
            CCGLoadoutData starter = CCGLoadoutData.Get(team == "bulwark" ? "bul_starter" : "san_starter");
            if (starter == null) return;
            foreach (TcgEngine.CardData piece in starter.GetAllPieces())
            {
                if (piece == null) continue;
                for (int i = 0; i < 5; i++)
                    if (piece.HasTrait(SLOT_TRAITS[i]) && string.IsNullOrEmpty(b_loadout[i]))
                        b_loadout[i] = piece.id;
            }
        }

        private void TryCloseBuilder()
        {
            //simple discard: no modal (Heat modal wiring comes later); dirty state noted on SAVE
            working = null;
            ShowScreen(1);
        }

        private void SaveWorking()
        {
            if (working == null) return;
            if (string.IsNullOrWhiteSpace(working.title))
                working.title = "UNNAMED DECK";

            //CHECKOUT: unowned copies are paid with wildcards; if short, the
            //deck saves as a DRAFT instead (finish paying later)
            Dictionary<RarityData, int> debt = ComputeDebt(working);
            bool draft;
            if (debt.Count == 0)
            {
                draft = false;
            }
            else if (CanAfford(debt))
            {
                foreach (UserCardData uc in working.cards)
                {
                    TCard cd = TCard.Get(uc.tid);
                    if (cd == null) continue;
                    int owed = OwedCopies(cd, uc.quantity);
                    if (owed <= 0) continue;
                    UData.AddPack(CCGForgeUI.WildcardTid(cd.rarity), -owed);
                    UData.AddCard(cd.id, VariantData.GetDefault().id, owed);
                }
                draft = false;
                CCGForgeUI forge = GetComponent<CCGForgeUI>();
                if (forge != null)
                    forge.RefreshTiles();
            }
            else
            {
                draft = true;
            }
            SetDraftDeck(working.tid, draft);

            //equipment loadout rides with the deck
            CCGLoadoutSave lsave = new CCGLoadoutSave();
            lsave.tids = b_loadout;
            PlayerPrefs.SetString(LOADOUT_PREF + working.tid, JsonUtility.ToJson(lsave));

            UpsertDeck(working);
            if (!draft && PlayerPrefs.GetString(ACTIVE_DECK_PREF, "") == "" && working.IsValid())
                PlayerPrefs.SetString(ACTIVE_DECK_PREF, working.tid);
            if (draft && PlayerPrefs.GetString(ACTIVE_DECK_PREF, "") == working.tid)
                PlayerPrefs.DeleteKey(ACTIVE_DECK_PREF); //drafts can't stay active
            SaveUser();
            dirty = false;
            selected_deck = working;
            RefreshSaveState();
            ShowScreen(1);
        }

        private void RefreshSaveState()
        {
            if (b_save_label == null) return;
            Dictionary<RarityData, int> debt = ComputeDebt(working);
            string label;
            if (debt.Count == 0)
                label = "SAVE";
            else if (CanAfford(debt))
            {
                int n = 0; foreach (var kv in debt) n += kv.Value;
                label = "CHECKOUT " + n + " WC";
            }
            else
                label = "SAVE DRAFT";
            b_save_label.text = label + (dirty ? " *" : "");
        }

        private int CountInWorking(string tid)
        {
            if (working == null || working.cards == null) return 0;
            foreach (UserCardData uc in working.cards)
                if (uc.tid == tid) return uc.quantity;
            return 0;
        }

        private void AddToWorking(TCard card)
        {
            if (working == null) return;
            //HERO RULE: the deck may only hold the hero's faction (neutrals free)
            int fac = HeroFaction();
            string team = card.team != null ? card.team.id : "";
            if (fac == 1 && team != "" && team != "sanguine") return;
            if (fac == 2 && team != "" && team != "bulwark") return;
            int total = 0; UserCardData found = null;
            List<UserCardData> list = new List<UserCardData>(working.cards);
            foreach (UserCardData uc in list) { total += uc.quantity; if (uc.tid == card.id) found = uc; }
            if (total >= GameplayData.Get().deck_size) return;
            if (found != null && found.quantity >= GameplayData.Get().deck_duplicate_max) return;
            if (found != null) found.quantity++;
            else list.Add(new UserCardData(card, VariantData.GetDefault()));
            working.cards = list.ToArray();
            dirty = true;
            RefreshBuilderGrid();
            RefreshBuilderRail();
            RefreshSaveState();
        }

        private void RemoveFromWorking(string tid)
        {
            if (working == null) return;
            List<UserCardData> list = new List<UserCardData>(working.cards);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].tid == tid)
                {
                    list[i].quantity--;
                    if (list[i].quantity <= 0) list.RemoveAt(i);
                    break;
                }
            }
            working.cards = list.ToArray();
            dirty = true;
            RefreshBuilderGrid();
            RefreshBuilderRail();
            RefreshSaveState();
        }

        private void CycleHero()
        {
            if (working == null) return;
            List<TCard> heroes = new List<TCard>();
            foreach (TCard c in TCard.GetAll())
                if (c.type == CardType.Hero && c.availability != CardAvailability.Unlisted && !c.id.StartsWith("hero_palatine"))
                    heroes.Add(c);
            if (heroes.Count == 0) return;
            heroes.Sort((a, b) => a.id.CompareTo(b.id));
            int cur = -1;
            for (int i = 0; i < heroes.Count; i++)
                if (working.hero != null && heroes[i].id == working.hero.tid) cur = i;
            TCard next = heroes[(cur + 1) % heroes.Count];
            working.hero = new UserCardData(next, VariantData.GetDefault());
            dirty = true;
            RefreshBuilderRail();
            RefreshBuilderGrid(); //the hero locks the pool: re-filter + flag off-faction rows
            RefreshSaveState();
        }

        /// <summary>The hero's faction index for FilteredCards (0 = no filter).</summary>
        private int HeroFaction()
        {
            TCard hero = TCard.Get(working != null && working.hero != null ? working.hero.tid : null);
            string team = hero != null && hero.team != null ? hero.team.id : "";
            if (team == "sanguine") return 1;
            if (team == "bulwark") return 2;
            return 0;
        }

        private void RefreshBuilderGrid()
        {
            if (b_grid == null || working == null) return;
            for (int i = b_grid.childCount - 1; i >= 0; i--)
                Destroy(b_grid.GetChild(i).gameObject);

            //EQUIP SLOT MODE: grid shows only gear matching the selected slot
            if (b_equip_slot >= 0)
            {
                RefreshEquipGrid();
                return;
            }

            List<TCard> cards = FilteredCards(HeroFaction(), b_type, 0, b_costs, b_search, b_owned);
            foreach (TCard card in cards)
            {
                //loadout gear lives in the EQUIPMENT slots, not the deck list
                if (card.type == CardType.Equipment)
                    continue;
                TCard c = card;
                int in_deck = CountInWorking(c.id);
                int max = GameplayData.Get().deck_duplicate_max;
                int owned_qty = UData.GetCardQuantity(c, VariantData.GetDefault());
                RectTransform cell = MakeRect("Card_" + c.id, b_grid);
                CCGCardFace face = CCGCardFace.Create(cell, CARD_SCALE, true);
                face.Apply(c);
                if (in_deck >= max)
                {
                    CanvasGroup cg = cell.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0.5f;
                }
                else if (owned_qty <= 0)
                {
                    CanvasGroup cg = cell.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0.82f; //unowned: addable, pays with a wildcard on checkout
                }
                if (owned_qty < Mathf.Max(in_deck, 1))
                    MakeWildcardBadge(cell, c);
                //copy pips
                RectTransform pips = MakeRect("Pips", cell);
                Anchor(pips, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-30f, -2f), new Vector2(30f, 8f));
                HorizontalLayoutGroup pl = pips.gameObject.AddComponent<HorizontalLayoutGroup>();
                pl.spacing = 4f;
                pl.childForceExpandWidth = true;
                pl.childForceExpandHeight = true;
                for (int p = 0; p < max; p++)
                {
                    Image pip = MakePanel("P" + p, pips, p < in_deck ? UIManagerImage.ColorType.Accent : UIManagerImage.ColorType.Secondary, 1f);
                }
                Image hit = cell.gameObject.AddComponent<Image>();
                hit.color = new Color(0, 0, 0, 0);
                CCGCardClick click = cell.gameObject.AddComponent<CCGCardClick>();
                click.onLeft = () => AddToWorking(c);
                click.onRight = () => ShowZoom(c);
            }
        }

        private static readonly Dictionary<string, Color> RARITY_COLORS = new Dictionary<string, Color>
        {
            { "common", new Color(0.75f, 0.78f, 0.82f) },
            { "uncommon", new Color(0.42f, 0.85f, 0.45f) },
            { "rare", new Color(0.21f, 0.71f, 0.9f) },
            { "epic", new Color(0.55f, 0.36f, 0.96f) },
            { "mythic", new Color(0.91f, 0.78f, 0.48f) },
        };

        private Color RarityColor(TCard c)
        {
            string rid = c.rarity != null ? c.rarity.id.ToLower() : "";
            foreach (var kv in RARITY_COLORS)
                if (rid.Contains(kv.Key)) return kv.Value;
            if (rid.Contains("legend")) return RARITY_COLORS["mythic"];
            return RARITY_COLORS["common"];
        }

        private void MakeWildcardBadge(RectTransform cell, TCard c)
        {
            Color rc = RarityColor(c);
            Image badge = MakePanel("WC", cell, UIManagerImage.ColorType.Background, 0.92f);
            badge.DestroyThemeDriver();
            badge.color = new Color(0.03f, 0.05f, 0.08f, 0.92f);
            Anchor(badge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-64f, -24f), new Vector2(-4f, -4f));
            TextMeshProUGUI t = MakeText("T", badge.transform, "WC " + (c.rarity != null ? c.rarity.id.ToUpper() : ""), 10, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary);
            t.color = rc;
            Anchor(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        /// <summary>Grid in equip-slot mode: gear of the hero's faction (plus
        /// neutral) whose trait matches the selected slot; click to socket.</summary>
        private void RefreshEquipGrid()
        {
            string trait = SLOT_TRAITS[b_equip_slot];
            int fac = HeroFaction();
            foreach (TCard card in TCard.GetAll())
            {
                if (card.availability == CardAvailability.Unlisted) continue;
                if (card.type != CardType.Equipment || !card.HasTrait(trait)) continue;
                string team = card.team != null ? card.team.id : "";
                if (fac == 1 && team != "" && team != "sanguine") continue;
                if (fac == 2 && team != "" && team != "bulwark") continue;
                if (!string.IsNullOrEmpty(b_search) && !card.title.ToLower().Contains(b_search.ToLower())) continue;

                TCard c = card;
                bool socketed = b_loadout[b_equip_slot] == c.id;
                RectTransform cell = MakeRect("Gear_" + c.id, b_grid);
                CCGCardFace face = CCGCardFace.Create(cell, CARD_SCALE, true);
                face.Apply(c);
                if (socketed)
                {
                    Image sel = MakePanel("Sel", cell, UIManagerImage.ColorType.Accent, 0.9f);
                    Anchor(sel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f, -24f), new Vector2(88f, -4f));
                    TextMeshProUGUI st = MakeText("T", sel.transform, "EQUIPPED", 10, UIManagerText.FontType.Bold, UIManagerText.ColorType.AccentMatch);
                    Anchor(st.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }
                Image hit = cell.gameObject.AddComponent<Image>();
                hit.color = new Color(0, 0, 0, 0);
                CCGCardClick click = cell.gameObject.AddComponent<CCGCardClick>();
                click.onLeft = () => {
                    b_loadout[b_equip_slot] = socketed ? null : c.id; //toggle
                    dirty = true;
                    RefreshBuilderRail();
                    RefreshBuilderGrid();
                    RefreshSaveState();
                };
                click.onRight = () => ShowZoom(c);
            }
        }

        private void RefreshBuilderRail()
        {
            if (working == null) return;

            //hero (cover-cropped banner, never stretched)
            TCard hero = TCard.Get(working.hero != null ? working.hero.tid : null);
            if (hero != null)
            {
                b_hero_art.DestroyThemeDriver();
                b_hero_art.color = new Color(0.04f, 0.06f, 0.1f, 1f);
                b_hero_cover.sprite = hero.GetFullArt(VariantData.GetDefault());
                b_hero_cover.enabled = b_hero_cover.sprite != null;
                b_hero_name.text = hero.title.ToUpper();
            }
            else
            {
                b_hero_cover.enabled = false;
                b_hero_name.text = "PICK A HERO";
            }

            //count + validity
            int total = 0;
            int[] curve = new int[7];
            int minions = 0, spells = 0, gear = 0, fields = 0;
            foreach (UserCardData uc in working.cards)
            {
                total += uc.quantity;
                TCard cd = TCard.Get(uc.tid);
                if (cd == null) continue;
                curve[Mathf.Clamp(cd.mana - 1, 0, 6)] += uc.quantity;
                if (cd.type == CardType.Character) minions += uc.quantity;
                else if (cd.type == CardType.Spell) spells += uc.quantity;
                else if (cd.type == CardType.Artifact && cd.HasTrait("field")) fields += uc.quantity;
                else gear += uc.quantity;
            }
            int size = GameplayData.Get().deck_size;
            b_count.text = total + " / " + size;
            int off_faction = CountOffFaction();
            if (hero == null) b_validity.text = "! pick a hero";
            else if (off_faction > 0) b_validity.text = "! " + off_faction + " card" + (off_faction == 1 ? "" : "s") + " outside " + (hero.team != null ? hero.team.id.ToUpper() : "faction");
            else if (total < size) b_validity.text = "! add " + (size - total) + " more card" + ((size - total) == 1 ? "" : "s");
            else b_validity.text = "";
            b_typesum.text = "MINIONS " + minions + "   SPELLS " + spells + "   GEAR " + gear + "   FIELDS " + fields;

            //equipment slots
            for (int i = 0; i < 5; i++)
            {
                TCard piece = TCard.Get(b_loadout[i]);
                if (b_slot_arts.Count > i)
                {
                    b_slot_arts[i].sprite = piece != null ? (piece.GetBoardArt(VariantData.GetDefault()) != null ? piece.GetBoardArt(VariantData.GetDefault()) : piece.GetFullArt(VariantData.GetDefault())) : null;
                    b_slot_arts[i].enabled = b_slot_arts[i].sprite != null;
                    b_slot_labels[i].text = piece != null ? piece.title.ToUpper() : SLOT_NAMES[i] + " +";
                    bool selected = b_equip_slot == i;
                    b_slot_frames[i].color = selected ? new Color(0.208f, 0.835f, 0.949f, 0.55f) : new Color(0.07f, 0.1f, 0.16f, 1f);
                }
            }

            //checkout summary: the wildcard bill
            Dictionary<RarityData, int> debt = ComputeDebt(working);
            if (debt.Count == 0)
            {
                b_checkout.text = "";
            }
            else
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                bool affordable = true;
                int owed_total = 0;
                foreach (var kv in debt)
                {
                    int have = UData.GetPackQuantity(CCGForgeUI.WildcardTid(kv.Key));
                    bool ok = have >= kv.Value;
                    if (!ok) affordable = false;
                    owed_total += kv.Value;
                    Color rc = RarityColor2(kv.Key);
                    sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(rc)).Append(">WC ")
                      .Append(kv.Key.id.ToUpper()).Append(" x").Append(kv.Value)
                      .Append("</color>  have ").Append(have).Append(ok ? " OK" : " SHORT").Append("   ");
                }
                sb.Append("\n").Append(affordable
                    ? "CHECKOUT unlocks " + owed_total + " unowned cop" + (owed_total == 1 ? "y" : "ies") + " with wildcards."
                    : "Short on wildcards — saving keeps this deck as a DRAFT.");
                b_checkout.text = sb.ToString();
            }

            //curve bars
            int peak = 1;
            foreach (int v in curve) peak = Mathf.Max(peak, v);
            for (int i = 0; i < b_curve_bars.Count; i++)
            {
                LayoutElement le = b_curve_bars[i].GetComponent<LayoutElement>();
                le.preferredHeight = 4f + 46f * (curve[i] / (float)peak);
            }

            //rows
            for (int i = b_rows.childCount - 1; i >= 0; i--)
                Destroy(b_rows.GetChild(i).gameObject);
            List<UserCardData> sorted = new List<UserCardData>(working.cards);
            sorted.Sort((a, b) => {
                TCard ca = TCard.Get(a.tid); TCard cb = TCard.Get(b.tid);
                int m = (ca != null ? ca.mana : 0).CompareTo(cb != null ? cb.mana : 0);
                return m != 0 ? m : string.Compare(a.tid, b.tid);
            });
            TCard row_hero = TCard.Get(working.hero != null ? working.hero.tid : null);
            string hero_team = row_hero != null && row_hero.team != null ? row_hero.team.id : "";
            foreach (UserCardData uc in sorted)
            {
                UserCardData u = uc;
                TCard cd = TCard.Get(u.tid);
                Image row = MakePanel("Row_" + u.tid, b_rows, UIManagerImage.ColorType.Background, 0.9f);
                LayoutElement rle = row.gameObject.AddComponent<LayoutElement>();
                rle.preferredHeight = 38f;
                //OFF-FACTION after a hero change: flag red for removal
                string row_team = cd != null && cd.team != null ? cd.team.id : "";
                if (hero_team != "" && row_team != "" && row_team != hero_team)
                {
                    row.DestroyThemeDriver();
                    row.color = new Color(0.45f, 0.12f, 0.14f, 0.92f);
                }
                //unowned copies carry their wildcard price
                int owed = cd != null ? OwedCopies(cd, u.quantity) : 0;
                if (owed > 0 && cd != null)
                {
                    TextMeshProUGUI wc = MakeText("WC", row.transform, "WC" + owed, 13, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary);
                    wc.color = RarityColor(cd);
                    Anchor(wc.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-148f, 0f), new Vector2(-118f, 0f));
                }
                //art sliver (cover-cropped)
                if (cd != null && cd.GetBoardArt(VariantData.GetDefault()) != null)
                {
                    Image sliver = MakePanel("Art", row.transform, UIManagerImage.ColorType.Background, 1f);
                    sliver.DestroyThemeDriver();
                    sliver.color = new Color(0.04f, 0.06f, 0.1f, 1f);
                    Anchor(sliver.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(52f, 0f));
                    MakeCoverArt(sliver).sprite = cd.GetBoardArt(VariantData.GetDefault());
                }
                //mana gem
                TextMeshProUGUI gem = MakeText("Mana", row.transform, cd != null ? cd.mana.ToString() : "?", 16, UIManagerText.FontType.Bold, UIManagerText.ColorType.Accent);
                Anchor(gem.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(56f, 0f), new Vector2(80f, 0f));
                //name
                TextMeshProUGUI nm = MakeText("Name", row.transform, cd != null ? cd.title : u.tid, 15, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary, TextAlignmentOptions.MidlineLeft);
                Anchor(nm.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(84f, 0f), new Vector2(-118f, 0f));
                //qty
                TextMeshProUGUI q = MakeText("Qty", row.transform, "x" + u.quantity, 15, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary);
                Anchor(q.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-114f, 0f), new Vector2(-82f, 0f));
                //+ / -
                Button plus = MakeButton("+", row.transform, () => { if (cd != null) AddToWorking(cd); }, 18, UIManagerImage.ColorType.Secondary, UIManagerText.ColorType.Accent);
                Anchor(plus.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-76f, -14f), new Vector2(-46f, 14f));
                Button minus = MakeButton("−", row.transform, () => RemoveFromWorking(u.tid), 18, UIManagerImage.ColorType.Secondary, UIManagerText.ColorType.Negative);
                Anchor(minus.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, -14f), new Vector2(-10f, 14f));
            }
        }
    }
}
