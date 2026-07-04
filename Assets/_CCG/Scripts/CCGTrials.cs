using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TcgEngine;
using TcgEngine.Client;
using Michsky.UI.Heat;
using TCard = TcgEngine.CardData; //asset pack defines a global CardData

namespace CCG
{
    /// <summary>Serializable roguelite run state (PlayerPrefs JSON).</summary>
    [System.Serializable]
    public class CCGTrialRun
    {
        public string champion_id;
        public int trial = 1;
        public int lives = 3;
        public int max_lives = 3;
        public int seed;
        public int current = 0;            //node being resolved / last resolved
        public bool choosing = false;      //current resolved, pick a next node
        public bool draft_pending = false; //battle won, must draft before moving
        public List<int> cleared = new List<int>();
        public List<int> types = new List<int>();   //per node: 0 battle,1 elite,2 event,3 power,4 mend,5 boss
        public List<string> deck = new List<string>();  //card tids (run deck, duplicates allowed)
        public List<string> powers = new List<string>();

        public const string PREF = "ccg_trial_run";
        public const string RESULT_PREF = "ccg_trial_result"; //"", "win", "loss"
        public const string PENDING_PREF = "ccg_trial_battle"; //node index as string, "" = none

        public static CCGTrialRun Load()
        {
            string json = PlayerPrefs.GetString(PREF, "");
            if (string.IsNullOrEmpty(json))
                return null;
            try { return JsonUtility.FromJson<CCGTrialRun>(json); }
            catch { return null; }
        }

        public void Save() { PlayerPrefs.SetString(PREF, JsonUtility.ToJson(this)); }
        public static void Clear() { PlayerPrefs.DeleteKey(PREF); PlayerPrefs.DeleteKey(RESULT_PREF); PlayerPrefs.DeleteKey(PENDING_PREF); }
    }

    /// <summary>
    /// Global watcher: records trial battle results when a match ends, and
    /// redirects any load of the retired kit "Menu" scene to CCG_Menu.
    /// </summary>
    public static class CCGTrialWatcher
    {
        private static bool hooked_scene = false;
        private static bool hooked_game = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!hooked_scene)
            {
                hooked_scene = true;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            }
            TryHookGame();
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            //the kit Menu scene is retired: everything routes to the Heat menu
            if (scene.name == "Menu")
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("CCG_Menu");
                return;
            }
            hooked_game = false;
            TryHookGame();
        }

        private static void TryHookGame()
        {
            if (hooked_game)
                return;
            GameClient client = GameClient.Get();
            if (client == null)
                return;
            hooked_game = true;
            client.onGameEnd += (winner) =>
            {
                if (PlayerPrefs.GetString(CCGTrialRun.PENDING_PREF, "") != "")
                {
                    bool won = winner == client.GetPlayerID();
                    PlayerPrefs.SetString(CCGTrialRun.RESULT_PREF, won ? "win" : "loss");
                }
            };
        }
    }

    /// <summary>
    /// THE TRIALS — roguelite adventure in the Heat Chapters panel.
    /// Select screen (champion + trial) and the run MAP (9 branching nodes as
    /// glowing square tiles), battles launch real solo matches with the RUN
    /// deck, drafts grow it, 2 defeats end the run. Powers collected (inert in
    /// pass 1). Layout per approved mockups.
    /// </summary>
    public class CCGTrialsUI : MonoBehaviour
    {
        //fixed 9-node topology (start / 2 / 3 / 2 / boss)
        private static readonly Vector2[] NODE_POS = {
            new Vector2(0.07f, 0.50f),
            new Vector2(0.25f, 0.70f), new Vector2(0.25f, 0.30f),
            new Vector2(0.44f, 0.84f), new Vector2(0.44f, 0.50f), new Vector2(0.44f, 0.16f),
            new Vector2(0.63f, 0.67f), new Vector2(0.63f, 0.33f),
            new Vector2(0.84f, 0.50f),
        };
        private static readonly int[][] EDGES = {
            new int[]{1,2}, new int[]{3,4}, new int[]{4,5},
            new int[]{6}, new int[]{6,7}, new int[]{7},
            new int[]{8}, new int[]{8}, new int[]{},
        };
        private static readonly string[] TYPE_ICON = { "gui_ic_battle", "gui_ic_elite", "gui_ic_event", "gui_ic_power", "gui_ic_mend", "gui_ic_boss" };
        private static readonly string[] TYPE_NAME = { "BATTLE", "ELITE", "EVENT", "POWER", "MEND", "BOSS" };

        private static readonly string[][] POWERS = {
            new string[]{ "blood_surge", "BLOOD SURGE", "Start each battle with +1 mana crystal." },
            new string[]{ "iron_will", "IRON WILL", "Your hero starts with +3 HP." },
            new string[]{ "keen_edge", "KEEN EDGE", "Your first attack each turn deals +1." },
            new string[]{ "scavenger", "SCAVENGER", "+25 coins after every battle." },
            new string[]{ "deep_pockets", "DEEP POCKETS", "Draw an extra card in your opening hand." },
            new string[]{ "second_wind", "SECOND WIND", "The first time you'd lose a life, keep it." },
        };

        private Transform panel_root;
        private RectTransform select_root, map_root, overlay_root;
        private bool built = false;
        private CCGTrialRun run;
        private string sel_champion = "hero_sanguine";

        private UserData UData { get { return Authenticator.Get().UserData; } }

        void Update()
        {
            if (built || Authenticator.Get() == null || Authenticator.Get().UserData == null)
                return;
            Transform canvas_t = GameObject.Find("Canvas - Main Menu").transform;
            Transform chapters = canvas_t.Find("Main Content/Chapters Panel/Content");
            if (chapters == null)
                return;
            built = true;
            panel_root = chapters;
            Build();
        }

        //--- primitives ---
        private RectTransform MakeRect(string n, Transform parent)
        {
            GameObject go = new GameObject(n, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }
        private static void Anchor(RectTransform rt, Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax)
        {
            rt.anchorMin = amin; rt.anchorMax = amax; rt.offsetMin = omin; rt.offsetMax = omax;
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
        private Image MakeSprite(string n, Transform parent, string res, Color color)
        {
            RectTransform rt = MakeRect(n, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = Resources.Load<Sprite>("GUI/" + res);
            img.color = color;
            img.preserveAspect = true;
            return img;
        }
        private TextMeshProUGUI MakeText(string n, Transform parent, string text, float size,
            UIManagerText.FontType font, UIManagerText.ColorType color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            RectTransform rt = MakeRect(n, parent);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            UIManagerText mt = rt.gameObject.AddComponent<UIManagerText>();
            mt.fontType = font; mt.colorType = color;
            return tmp;
        }
        private TextMeshProUGUI MakeRawText(string n, Transform parent, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI t = MakeText(n, parent, text, size, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary, align);
            t.DestroyThemeDriver();
            t.color = color;
            return t;
        }
        private Button MakeButton(string label, Transform parent, UnityEngine.Events.UnityAction action, float font_size = 20,
            UIManagerImage.ColorType bg = UIManagerImage.ColorType.Secondary,
            UIManagerText.ColorType fg = UIManagerText.ColorType.Primary)
        {
            Image img = MakePanel("Btn_" + label, parent, bg, 1f);
            Button btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (action != null) btn.onClick.AddListener(action);
            TextMeshProUGUI txt = MakeText("Label", img.transform, label, font_size, UIManagerText.FontType.Semibold, fg);
            Anchor(txt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }

        //================= construction =================

        private void Build()
        {
            //hide Heat's chapter demo content
            foreach (Transform child in panel_root)
                if (!child.name.StartsWith("CCG_"))
                    child.gameObject.SetActive(false);

            //(fullscreen background art is handled globally per-tab by CCGHeatMenu)
            //NAV RULE: measured header clearance — nothing renders under the bar
            float NAV_H = CCGNavUtil.Clearance(panel_root as RectTransform);
            select_root = MakeRect("CCG_TrialSelect", panel_root);
            Anchor(select_root, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -NAV_H));
            map_root = MakeRect("CCG_TrialMap", panel_root);
            Anchor(map_root, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -NAV_H));
            overlay_root = MakeRect("CCG_TrialOverlay", panel_root);
            Anchor(overlay_root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            run = CCGTrialRun.Load();
            ConsumeBattleResult();
            Refresh();
        }

        private void Refresh()
        {
            bool active = run != null;
            select_root.gameObject.SetActive(!active);
            map_root.gameObject.SetActive(active);
            overlay_root.gameObject.SetActive(false);
            if (active) BuildMap();
            else BuildSelect();

            //battle won -> draft before anything else
            if (run != null && run.draft_pending)
                ShowDraft();
        }

        //================= battle result intake =================

        private void ConsumeBattleResult()
        {
            string result = PlayerPrefs.GetString(CCGTrialRun.RESULT_PREF, "");
            string pending = PlayerPrefs.GetString(CCGTrialRun.PENDING_PREF, "");
            if (run == null || result == "" || pending == "")
            {
                PlayerPrefs.DeleteKey(CCGTrialRun.RESULT_PREF);
                PlayerPrefs.DeleteKey(CCGTrialRun.PENDING_PREF);
                return;
            }

            PlayerPrefs.DeleteKey(CCGTrialRun.RESULT_PREF);
            PlayerPrefs.DeleteKey(CCGTrialRun.PENDING_PREF);

            if (result == "win")
            {
                int node = run.current;
                run.cleared.Add(node);
                if (run.types[node] == 5) //boss down: run complete
                {
                    int coins = 300 * run.trial;
                    UData.coins += coins;
                    UData.AddPack("wc_rare", 1);
                    SaveUser();
                    CCGTrialRun.Clear();
                    run = null;
                    return;
                }
                run.draft_pending = true;
                run.choosing = false;
                run.Save();
            }
            else //loss: lose a life, node stays current (retry)
            {
                run.lives--;
                if (run.lives <= 0)
                {
                    CCGTrialRun.Clear();
                    run = null;
                    return;
                }
                run.Save();
            }
        }

        //================= SELECT =================

        private void BuildSelect()
        {
            for (int i = select_root.childCount - 1; i >= 0; i--)
                Destroy(select_root.GetChild(i).gameObject);

            TextMeshProUGUI title = MakeText("Title", select_root, "THE TRIALS", 34, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary, TextAlignmentOptions.TopLeft);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(8f, -46f), new Vector2(0f, -4f));
            TextMeshProUGUI sub = MakeText("Sub", select_root, "Pick a champion. Fight a branching gauntlet. Draft cards and powers. Two defeats ends the run.", 15, UIManagerText.FontType.Regular, UIManagerText.ColorType.Primary, TextAlignmentOptions.TopLeft);
            sub.color = new Color(sub.color.r, sub.color.g, sub.color.b, 0.6f);
            Anchor(sub.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -70f), new Vector2(0f, -48f));

            //champions
            string[][] champs = {
                new string[]{ "hero_sanguine", "SANGUINE · lifesteal" },
                new string[]{ "hero_bulwark", "BULWARK · armor" },
            };
            for (int i = 0; i < champs.Length; i++)
            {
                string cid = champs[i][0];
                TCard hero = TCard.Get(cid);
                if (hero == null) continue;
                bool selected = sel_champion == cid;

                Image tile = MakePanel("Champ_" + cid, select_root, UIManagerImage.ColorType.Secondary, 1f);
                Anchor(tile.rectTransform, new Vector2(0.02f + i * 0.24f, 0.12f), new Vector2(0.24f + i * 0.24f, 0.82f), Vector2.zero, Vector2.zero);
                Image art = MakeSprite("Art", tile.transform, "", Color.white);
                art.sprite = hero.GetFullArt(VariantData.GetDefault());
                art.preserveAspect = false;
                Anchor(art.rectTransform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
                if (selected)
                {
                    Image sel = MakeSprite("Sel", tile.transform, "gui_sq_outline", new Color(0.208f, 0.835f, 0.949f));
                    sel.preserveAspect = false;
                    sel.type = Image.Type.Sliced;
                    Anchor(sel.rectTransform, Vector2.zero, Vector2.one, new Vector2(-4f, -4f), new Vector2(4f, 4f));
                }
                Image lb = MakePanel("LB", tile.transform, UIManagerImage.ColorType.Background, 0.85f);
                Anchor(lb.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 54f));
                TextMeshProUGUI nm = MakeText("N", lb.transform, hero.title.ToUpper(), 18, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary, TextAlignmentOptions.TopLeft);
                Anchor(nm.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 26f), new Vector2(-10f, -4f));
                TextMeshProUGUI ds = MakeText("D", lb.transform, champs[i][1], 13, UIManagerText.FontType.Medium, UIManagerText.ColorType.Accent, TextAlignmentOptions.BottomLeft);
                Anchor(ds.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, -28f));

                Button b = tile.gameObject.AddComponent<Button>();
                b.targetGraphic = tile;
                b.onClick.AddListener(() => { sel_champion = cid; BuildSelect(); });
            }

            //locked palatine
            Image locked = MakePanel("Champ_locked", select_root, UIManagerImage.ColorType.Secondary, 0.4f);
            Anchor(locked.rectTransform, new Vector2(0.50f, 0.12f), new Vector2(0.72f, 0.82f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI lk = MakeText("L", locked.transform, "PALATINE\nCOMING SOON", 18, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary);
            lk.textWrappingMode = TextWrappingModes.Normal;
            lk.color = new Color(lk.color.r, lk.color.g, lk.color.b, 0.4f);
            Anchor(lk.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            //setup rail
            Image rail = MakePanel("Rail", select_root, UIManagerImage.ColorType.Secondary, 0.8f);
            Anchor(rail.rectTransform, new Vector2(0.75f, 0.12f), new Vector2(1f, 0.82f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI rt2 = MakeText("T", rail.transform, "RUN SETUP", 20, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary, TextAlignmentOptions.TopLeft);
            Anchor(rt2.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -34f), new Vector2(-14f, -6f));
            TextMeshProUGUI rules = MakeText("R", rail.transform,
                "TRIAL I\n\n· 9 nodes, branching path\n· draft 1-of-3 after battles\n· powers last the whole run\n· 3 lives — 3 defeats ends it\n· boss: " + (300) + " coins + rare wildcard\n\nRun deck starts from your\nchampion's starter deck.",
                14, UIManagerText.FontType.Regular, UIManagerText.ColorType.Primary, TextAlignmentOptions.TopLeft);
            rules.textWrappingMode = TextWrappingModes.Normal;
            rules.color = new Color(rules.color.r, rules.color.g, rules.color.b, 0.8f);
            Anchor(rules.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 60f), new Vector2(-14f, -40f));
            Button start = MakeButton("START RUN  →", rail.transform, StartRun, 20, UIManagerImage.ColorType.Accent, UIManagerText.ColorType.AccentMatch);
            Anchor(start.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 12f), new Vector2(-14f, 54f));
        }

        private void StartRun()
        {
            run = new CCGTrialRun();
            run.champion_id = sel_champion;
            run.seed = Mathf.Abs(System.Environment.TickCount);
            System.Random rng = new System.Random(run.seed);

            //node types: 0 start battle, boss last; guarantee >=1 power & mend
            run.types = new List<int>();
            for (int i = 0; i < 9; i++) run.types.Add(0);
            run.types[8] = 5;
            int[] pool = { 0, 0, 0, 1, 2, 2, 3, 4 }; //nodes 1..7 pull from weighted pool
            List<int> bag = new List<int>(pool);
            for (int i = 1; i <= 7; i++)
            {
                int pick = rng.Next(bag.Count);
                run.types[i] = bag[pick];
                bag.RemoveAt(pick);
            }
            if (!run.types.Contains(3)) run.types[3] = 3;
            if (!run.types.Contains(4)) run.types[5] = 4;

            //run deck = champion starter deck
            string starter_id = sel_champion == "hero_sanguine" ? "san_starter" : "bul_starter";
            DeckData starter = DeckData.Get(starter_id);
            run.deck = new List<string>();
            if (starter != null)
                foreach (TCard c in starter.cards)
                    if (c != null) run.deck.Add(c.id);

            run.current = 0;
            run.choosing = false;
            run.Save();
            Refresh();
        }

        //================= MAP =================

        private void BuildMap()
        {
            for (int i = map_root.childCount - 1; i >= 0; i--)
                Destroy(map_root.GetChild(i).gameObject);

            //header
            TCard champ = TCard.Get(run.champion_id);
            TextMeshProUGUI hd = MakeText("HD", map_root, (champ != null ? champ.title.ToUpper() : "?") + " · TRIAL " + run.trial +
                "    LIVES " + new string('♥', Mathf.Max(0, run.lives)) + new string('·', Mathf.Max(0, run.max_lives - run.lives)) +
                "    DECK " + run.deck.Count +
                (run.powers.Count > 0 ? "    POWERS " + run.powers.Count : ""),
                17, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary, TextAlignmentOptions.MidlineLeft);
            Anchor(hd.rectTransform, new Vector2(0f, 1f), new Vector2(0.75f, 1f), new Vector2(8f, -36f), new Vector2(0f, -4f));
            Button abandon = MakeButton("ABANDON RUN", map_root, () => { CCGTrialRun.Clear(); run = null; Refresh(); }, 14, UIManagerImage.ColorType.Negative);
            Anchor(abandon.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-160f, -38f), new Vector2(0f, -6f));

            //map area
            RectTransform area = MakeRect("Area", map_root);
            Anchor(area, new Vector2(0f, 0.06f), new Vector2(1f, 1f), new Vector2(0f, 6f), new Vector2(0f, -44f));

            //connections need the REAL laid-out size of the area
            Canvas.ForceUpdateCanvases();
            Vector2 area_size = area.rect.size;
            if (area_size.x < 100f) area_size = new Vector2(1500f, 700f); //first-frame fallback

            for (int a = 0; a < EDGES.Length; a++)
            {
                foreach (int b in EDGES[a])
                {
                    bool traversed = run.cleared.Contains(a) && (run.cleared.Contains(b) || run.current == b);
                    MakeLink(area, area_size, NODE_POS[a], NODE_POS[b], traversed);
                }
            }

            //nodes
            for (int i = 0; i < 9; i++)
            {
                int node = i;
                int type = run.types[i];
                bool cleared = run.cleared.Contains(i);
                bool is_current = run.current == i && !run.choosing && !cleared;
                bool reachable = run.choosing && System.Array.IndexOf(EDGES[run.current], i) >= 0;

                Color col;
                if (cleared) col = new Color(0.208f, 0.835f, 0.949f, 0.35f);
                else if (is_current) col = type == 5 ? new Color(0.898f, 0.282f, 0.302f) : new Color(0.208f, 0.835f, 0.949f);
                else if (reachable) col = new Color(0.95f, 0.96f, 0.98f);
                else if (type == 5) col = new Color(0.898f, 0.282f, 0.302f, 0.55f);
                else col = new Color(0.35f, 0.42f, 0.5f, 0.45f);

                RectTransform tile = MakeRect("Node_" + i, area);
                tile.anchorMin = tile.anchorMax = NODE_POS[i];
                float s = type == 5 ? 92f : (is_current ? 84f : 66f);
                tile.sizeDelta = new Vector2(s, s);

                Image bg = MakeSprite("BG", tile, "gui_sq_glow", col);
                bg.preserveAspect = false;
                Anchor(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                if (is_current || reachable)
                {
                    Image outline = MakeSprite("OL", tile, "gui_sq_outline", col);
                    outline.preserveAspect = false;
                    outline.type = Image.Type.Sliced;
                    Anchor(outline.rectTransform, Vector2.zero, Vector2.one, new Vector2(-8f, -8f), new Vector2(8f, 8f));
                }
                Image icon = MakeSprite("Icon", tile, TYPE_ICON[type], Color.white);
                Anchor(icon.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 14f), new Vector2(-14f, -14f));

                if (is_current)
                {
                    TextMeshProUGUI lbl = MakeRawText("L", tile, TYPE_NAME[type] + " — TAP", 12, new Color(0.208f, 0.835f, 0.949f));
                    Anchor(lbl.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-80f, -24f), new Vector2(80f, -4f));
                }

                if (is_current || reachable)
                {
                    Button b = tile.gameObject.AddComponent<Button>();
                    b.targetGraphic = bg;
                    b.onClick.AddListener(() => OnNodeClicked(node));
                }
            }

            //legend
            TextMeshProUGUI legend = MakeText("Legend", map_root, "BATTLE · ELITE (better loot) · EVENT (choice) · POWER (pick 1 of 3) · MEND (+1 life) · BOSS", 12, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary, TextAlignmentOptions.MidlineLeft);
            legend.color = new Color(legend.color.r, legend.color.g, legend.color.b, 0.5f);
            Anchor(legend.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(8f, 0f), new Vector2(0f, 24f));
        }

        private void MakeLink(RectTransform area, Vector2 size, Vector2 a, Vector2 b, bool traversed)
        {
            RectTransform link = MakeRect("Link", area);
            Vector2 mid = (a + b) * 0.5f;
            link.anchorMin = link.anchorMax = mid;
            Vector2 d = new Vector2((b.x - a.x) * size.x, (b.y - a.y) * size.y);
            link.sizeDelta = new Vector2(d.magnitude - 70f, traversed ? 5f : 3f);
            link.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            Image img = link.gameObject.AddComponent<Image>();
            img.color = traversed ? new Color(0.208f, 0.835f, 0.949f, 0.9f) : new Color(0.3f, 0.36f, 0.44f, 0.55f);
            img.raycastTarget = false;
        }

        private void OnNodeClicked(int node)
        {
            if (run.choosing)
            {
                run.current = node;
                run.choosing = false;
                run.Save();
            }
            ResolveCurrent();
        }

        private void ResolveCurrent()
        {
            int type = run.types[run.current];
            switch (type)
            {
                case 0: case 1: case 5:
                    LaunchBattle(type);
                    break;
                case 2:
                    ShowEvent();
                    break;
                case 3:
                    ShowPowerDraft();
                    break;
                case 4:
                    run.lives = Mathf.Min(run.max_lives, run.lives + 1);
                    NodeDone();
                    break;
            }
        }

        private void NodeDone()
        {
            run.cleared.Add(run.current);
            run.choosing = true;
            run.Save();
            overlay_root.gameObject.SetActive(false);
            BuildMap();
        }

        //================= BATTLE =================

        private void LaunchBattle(int type)
        {
            //run deck (merged quantities)
            UserDeckData deck = new UserDeckData("trial_run", "Trial Run");
            TCard champ = TCard.Get(run.champion_id);
            deck.hero = new UserCardData(champ, VariantData.GetDefault());
            Dictionary<string, UserCardData> merged = new Dictionary<string, UserCardData>();
            foreach (string tid in run.deck)
            {
                if (merged.ContainsKey(tid)) merged[tid].quantity++;
                else { var uc = new UserCardData(); uc.tid = tid; uc.variant = VariantData.GetDefault().id; uc.quantity = 1; merged[tid] = uc; }
            }
            deck.cards = new List<UserCardData>(merged.Values).ToArray();

            DeckData[] ai_decks = GameplayData.Get().ai_decks;
            if (ai_decks == null || ai_decks.Length == 0) return;
            System.Random rng = new System.Random(run.seed + run.current);
            DeckData ai_deck = ai_decks[rng.Next(ai_decks.Length)];

            GameClient.player_settings.deck = deck;
            GameClient.ai_settings.deck = new UserDeckData(ai_deck);
            int ai_level = GameplayData.Get().ai_level + (type == 1 ? 2 : 0) + (type == 5 ? 3 : 0) + (run.trial - 1);
            GameClient.ai_settings.ai_level = Mathf.Clamp(ai_level, 1, 10);
            GameClient.game_settings.game_type = GameType.Solo;
            GameClient.game_settings.game_mode = GameMode.Casual;
            GameClient.game_settings.scene = GameplayData.Get().GetRandomArena();
            GameClient.game_settings.game_uid = GameTool.GenerateRandomID();

            PlayerPrefs.SetString(CCGTrialRun.PENDING_PREF, run.current.ToString());
            PlayerPrefs.Save();
            TcgEngine.TcgNetwork.Get().Disconnect();
            SceneNav.GoTo(GameClient.game_settings.GetScene());
        }

        //================= OVERLAYS =================

        private RectTransform MakeOverlay(string title)
        {
            for (int i = overlay_root.childCount - 1; i >= 0; i--)
                Destroy(overlay_root.GetChild(i).gameObject);
            overlay_root.gameObject.SetActive(true);
            overlay_root.SetAsLastSibling();
            Image dim = MakePanel("Dim", overlay_root, UIManagerImage.ColorType.Background, 0.92f);
            Anchor(dim.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI t = MakeText("Title", overlay_root, title, 26, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary);
            Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -70f), new Vector2(0f, -20f));
            return overlay_root;
        }

        private void ShowDraft()
        {
            RectTransform ov = MakeOverlay("VICTORY — DRAFT 1 OF 3");
            System.Random rng = new System.Random(run.seed + run.cleared.Count * 77);
            TCard champ = TCard.Get(run.champion_id);
            string team = champ != null && champ.team != null ? champ.team.id : "";

            List<TCard> pool = new List<TCard>();
            foreach (TCard c in TCard.GetAll())
                if (c.availability != CardAvailability.Unlisted && c.type != CardType.Hero
                    && c.team != null && c.team.id == team)
                    pool.Add(c);
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                TCard pick = pool[rng.Next(pool.Count)];
                pool.Remove(pick);
                RectTransform host = MakeRect("Pick" + i, ov);
                host.anchorMin = host.anchorMax = new Vector2(0.28f + i * 0.22f, 0.52f);
                CCGCardFace face = CCGCardFace.Create(host, 0.42f, true);
                face.Apply(pick);
                Image hit = host.gameObject.AddComponent<Image>();
                hit.color = new Color(0, 0, 0, 0);
                hit.rectTransform.sizeDelta = new Vector2(618f * 0.42f, 922f * 0.42f);
                Button b = host.gameObject.AddComponent<Button>();
                b.targetGraphic = hit;
                TCard chosen = pick;
                b.onClick.AddListener(() => {
                    run.deck.Add(chosen.id);
                    run.draft_pending = false;
                    run.choosing = true;
                    run.Save();
                    Refresh();
                });
            }
            Button skip = MakeButton("SKIP  (+25 COINS)", ov, () => {
                UData.coins += 25;
                SaveUser();
                run.draft_pending = false;
                run.choosing = true;
                run.Save();
                Refresh();
            }, 16);
            Anchor(skip.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-130f, 24f), new Vector2(130f, 64f));
        }

        private void ShowPowerDraft()
        {
            RectTransform ov = MakeOverlay("CHOOSE A POWER");
            System.Random rng = new System.Random(run.seed + run.current * 31);
            List<int> avail = new List<int>();
            for (int i = 0; i < POWERS.Length; i++)
                if (!run.powers.Contains(POWERS[i][0])) avail.Add(i);

            for (int i = 0; i < 3 && avail.Count > 0; i++)
            {
                int pi = avail[rng.Next(avail.Count)];
                avail.Remove(pi);
                string[] p = POWERS[pi];
                Image card = MakePanel("Power" + i, ov, UIManagerImage.ColorType.Secondary, 1f);
                Anchor(card.rectTransform, new Vector2(0.2f + i * 0.21f, 0.35f), new Vector2(0.38f + i * 0.21f, 0.7f), Vector2.zero, Vector2.zero);
                Image ic = MakeSprite("I", card.transform, "gui_ic_power", new Color(0.208f, 0.835f, 0.949f));
                Anchor(ic.rectTransform, new Vector2(0.35f, 0.62f), new Vector2(0.65f, 0.92f), Vector2.zero, Vector2.zero);
                TextMeshProUGUI nm = MakeText("N", card.transform, p[1], 18, UIManagerText.FontType.Bold, UIManagerText.ColorType.Accent);
                Anchor(nm.rectTransform, new Vector2(0f, 0.42f), new Vector2(1f, 0.58f), Vector2.zero, Vector2.zero);
                TextMeshProUGUI ds = MakeText("D", card.transform, p[2], 13, UIManagerText.FontType.Regular, UIManagerText.ColorType.Primary);
                ds.textWrappingMode = TextWrappingModes.Normal;
                ds.color = new Color(ds.color.r, ds.color.g, ds.color.b, 0.75f);
                Anchor(ds.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.4f), Vector2.zero, Vector2.zero);
                Button b = card.gameObject.AddComponent<Button>();
                b.targetGraphic = card;
                string pid = p[0];
                b.onClick.AddListener(() => {
                    run.powers.Add(pid);
                    NodeDone();
                });
            }
        }

        private void ShowEvent()
        {
            RectTransform ov = MakeOverlay("EVENT — A CROSSROADS OFFERING");
            TextMeshProUGUI story = MakeText("S", ov, "A hooded stranger offers a bargain at the waypoint shrine.", 17, UIManagerText.FontType.Regular, UIManagerText.ColorType.Primary);
            Anchor(story.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero);

            Button opt1 = MakeButton("TAKE THE COIN PURSE  (+75 COINS)", ov, () => {
                UData.coins += 75;
                SaveUser();
                NodeDone();
            }, 17);
            Anchor(opt1.GetComponent<RectTransform>(), new Vector2(0.3f, 0.42f), new Vector2(0.7f, 0.52f), Vector2.zero, Vector2.zero);
            Button opt2 = MakeButton(run.lives < run.max_lives ? "ACCEPT HEALING  (+1 LIFE)" : "MEDITATE  (NOTHING HAPPENS)", ov, () => {
                run.lives = Mathf.Min(run.max_lives, run.lives + 1);
                NodeDone();
            }, 17);
            Anchor(opt2.GetComponent<RectTransform>(), new Vector2(0.3f, 0.28f), new Vector2(0.7f, 0.38f), Vector2.zero, Vector2.zero);
        }

        private async void SaveUser()
        {
            await Authenticator.Get().SaveUserData();
        }
    }
}
