using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TcgEngine;
using TcgEngine.Client;
using Michsky.UI.Heat;
using TCard = TcgEngine.CardData; //asset pack defines a global CardData; unique alias avoids CS0576

namespace CCG
{
    /// <summary>
    /// Controller for the Heat-based menu scene (CCG_Menu). Wires the stock Heat
    /// prefabs to TcgEngine: auto-login (offline test mode), profile chip,
    /// deck cycling on the home deck box, SOLO/ADVENTURE/MULTIPLAYER tiles, and
    /// scene routing. NO TcgEngine kit UI is used here - Heat visuals only.
    /// </summary>
    public class CCGHeatMenu : MonoBehaviour
    {
        public static string selected_deck_tid;

        private BoxButtonManager solo_box;
        private BoxButtonManager adventure_box;
        private BoxButtonManager multi_box;
        private BoxButtonManager deck_box;
        private TextMeshProUGUI profile_txt;
        private PanelManager panels;
        private bool starting = false;
        private int deck_index = 0;

        IEnumerator Start()
        {
            //bind scene objects (stock Heat hierarchy)
            Transform canvas = GameObject.Find("Canvas - Main Menu").transform;
            Transform rows = canvas.Find("Main Content/Home Panel/Content/Box Container");
            solo_box = rows.Find("Row 1/Continue").GetComponent<BoxButtonManager>();
            adventure_box = rows.Find("Row 1/New Game").GetComponent<BoxButtonManager>();
            multi_box = rows.Find("Row 2/Load Game").GetComponent<BoxButtonManager>();
            deck_box = rows.Find("Row 3/Deck Box").GetComponent<BoxButtonManager>();
            //profile chip lives TOP-RIGHT now (user rule); old path kept as fallback
            Transform profile_t = canvas.Find("Main Content/Top Panel/Content/Profile/Text");
            if (profile_t == null)
                profile_t = canvas.Find("Main Content/Bottom Panel/Content/Profile/Text");
            profile_txt = profile_t != null ? profile_t.GetComponent<TextMeshProUGUI>() : null;
            panels = canvas.Find("Main Content").GetComponent<PanelManager>();

            solo_box.onClick.AddListener(StartSolo);
            adventure_box.onClick.AddListener(OpenAdventure);
            multi_box.onClick.AddListener(StartMultiplayer);
            deck_box.onClick.AddListener(CycleDeck);

            //Q/E/ESC hotkey hint strips gone from every page (user rule)
            foreach (Transform tr in canvas.GetComponentsInChildren<Transform>(true))
                if (tr.name == "Hotkeys")
                    tr.gameObject.SetActive(false);

            BuildWildcardChip(canvas);

            //offline test auto-login so decks/coins exist (local auth reports
            //connected even before login, so key off missing user data)
            Authenticator auth = Authenticator.Get();
            if (auth.UserData == null)
            {
                string user = PlayerPrefs.GetString("tcg_last_user", "Player");
                if (string.IsNullOrEmpty(user))
                    user = "Player";
                var task = auth.Login(user);
                while (!task.IsCompleted)
                    yield return null;
                var load = auth.LoadUserData(); //login only sets the username
                while (!load.IsCompleted)
                    yield return null;
            }

            //CROWS: fresh accounts get the starter decks immediately — the Heat menu
            //has no starter-selection popup, and SOLO with no deck hangs at Connecting
            GrantStartersIfNeeded();

            RefreshProfile();
            RefreshDeckBox();
        }

        private void GrantStartersIfNeeded()
        {
            UserData udata = Authenticator.Get().UserData;
            if (udata == null || (udata.decks != null && udata.decks.Length > 0))
                return;

            foreach (DeckData deck in GameplayData.Get().starter_decks)
            {
                if (deck == null) continue;
                UserDeckData udeck = new UserDeckData();
                udeck.tid = deck.id + "_" + GameTool.GenerateRandomID(4, 7);
                udeck.title = deck.title;
                udeck.hero = deck.hero != null ? new UserCardData(deck.hero, VariantData.GetDefault()) : null;
                List<UserCardData> cards = new List<UserCardData>();
                foreach (TcgEngine.CardData card in deck.cards)
                    cards.Add(new UserCardData(card, VariantData.GetDefault()));
                udeck.cards = cards.ToArray();
                udata.AddDeck(udeck);
                udata.AddReward(udeck.tid);
            }
            var save = Authenticator.Get().SaveUserData();
            Debug.Log("CROWS: granted " + GameplayData.Get().starter_decks.Length + " starter decks to fresh account " + udata.username);
        }

        private UserData UData { get { return Authenticator.Get().UserData; } }

        private float refresh_timer = 0f;
        private UnityEngine.UI.Image global_bg;
        private int last_bg_panel = -1;

        void Update()
        {
            UpdateGlobalBackground();

            //keep the deck box + coins in sync with edits made on other screens
            refresh_timer -= Time.deltaTime;
            if (refresh_timer > 0f || deck_box == null || UData == null)
                return;
            refresh_timer = 1f;
            RefreshProfile();
            RefreshDeckBox();
        }

        /// <summary>The FULLSCREEN background swaps with the active tab, so each
        /// screen's key art fills the whole screen (user rule).</summary>
        private void UpdateGlobalBackground()
        {
            if (panels == null)
                return;
            if (global_bg == null)
            {
                Transform bg = GameObject.Find("Canvas - Main Menu").transform.Find("Background");
                if (bg != null)
                    global_bg = bg.GetComponentInChildren<UnityEngine.UI.Image>(true);
                if (global_bg == null)
                    return;
            }
            if (panels.currentPanelIndex == last_bg_panel)
                return;
            last_bg_panel = panels.currentPanelIndex;

            string pname = panels.panels[panels.currentPanelIndex].panelName.ToLower();
            string sprite_name = "UI/bg_home";
            Color tint = new Color(0.72f, 0.72f, 0.78f, 1f);
            if (pname.Contains("chapter")) { sprite_name = "UI/bg_trials"; tint = new Color(0.42f, 0.42f, 0.5f, 1f); }
            else if (pname.Contains("shop")) { sprite_name = "UI/bg_forge"; tint = new Color(0.55f, 0.55f, 0.62f, 1f); }
            else if (pname.Contains("extra")) { sprite_name = "UI/bg_collection"; tint = new Color(0.5f, 0.5f, 0.58f, 1f); }

            Sprite s = Resources.Load<Sprite>(sprite_name);
            if (s != null)
            {
                global_bg.sprite = s;
                global_bg.color = tint;
            }
        }

        private TextMeshProUGUI wildcards_txt;

        /// <summary>Wildcard inventory chip beside the player chip (top-right,
        /// user rule) — per-rarity counts in rarity colors.</summary>
        private void BuildWildcardChip(Transform canvas)
        {
            Transform top = canvas.Find("Main Content/Top Panel/Content");
            if (top == null)
                return;
            GameObject go = new GameObject("CCG_Wildcards", typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(top, false);
            var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
            le.ignoreLayout = true;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-330f, -6f);
            rt.sizeDelta = new Vector2(360f, 40f);
            wildcards_txt = go.AddComponent<TextMeshProUGUI>();
            wildcards_txt.fontSize = 20;
            wildcards_txt.alignment = TextAlignmentOptions.MidlineRight;
            wildcards_txt.raycastTarget = false;
        }

        private void RefreshProfile()
        {
            if (UData == null)
                return;
            if (profile_txt != null)
                profile_txt.text = UData.username + "  |  " + UData.coins + " coins";
            if (wildcards_txt != null)
            {
                //per-rarity wildcard counts, rank order, rarity-colored
                List<RarityData> rarities = new List<RarityData>(RarityData.GetAll());
                rarities.Sort((a, b) => a.rank.CompareTo(b.rank));
                System.Text.StringBuilder sb = new System.Text.StringBuilder("<size=70%>WC</size>  ");
                foreach (RarityData r in rarities)
                {
                    int n = UData.GetPackQuantity(CCGForgeUI.WildcardTid(r));
                    Color c = CCGForgeUI.RarityColor(r);
                    sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(c)).Append(">")
                      .Append(n).Append("</color> ");
                }
                wildcards_txt.text = sb.ToString();
            }
        }

        //---- deck selection ----

        private List<UserDeckData> GetDecks()
        {
            List<UserDeckData> list = new List<UserDeckData>();
            if (UData != null && UData.decks != null)
                foreach (UserDeckData d in UData.decks)
                    if (!CCGCollectionUI.IsDraftDeck(d.tid)) //drafts hold unpaid cards: not playable
                        list.Add(d);
            return list;
        }

        public void CycleDeck()
        {
            List<UserDeckData> decks = GetDecks();
            if (decks.Count == 0)
                return;
            deck_index = (deck_index + 1) % decks.Count;
            //cycling on HOME also sets the active deck (synced with the DECKS screen)
            PlayerPrefs.SetString(CCGCollectionUI.ACTIVE_DECK_PREF, decks[deck_index].tid);
            RefreshDeckBox();
        }

        private void RefreshDeckBox()
        {
            List<UserDeckData> decks = GetDecks();
            if (decks.Count == 0)
            {
                deck_box.SetText("NO DECK");
                deck_box.SetDescription("Visit COLLECTION to build one");
                selected_deck_tid = null;
                return;
            }

            //prefer the active deck chosen on the DECKS screen
            string active = PlayerPrefs.GetString(CCGCollectionUI.ACTIVE_DECK_PREF, "");
            for (int i = 0; i < decks.Count; i++)
                if (decks[i].tid == active)
                    deck_index = i;

            deck_index = Mathf.Clamp(deck_index, 0, decks.Count - 1);
            UserDeckData deck = decks[deck_index];
            selected_deck_tid = deck.tid;
            deck_box.SetText(deck.title.ToUpper());
            deck_box.SetDescription(deck.GetQuantity() + " / " + GameplayData.Get().deck_size + "  |  tap to cycle");

            TCard hero = TCard.Get(deck.hero != null ? deck.hero.tid : null);
            if (hero != null && hero.art_full != null)
                deck_box.SetBackground(hero.GetFullArt(VariantData.GetDefault()));
        }

        private UserDeckData GetSelectedDeck()
        {
            foreach (UserDeckData d in GetDecks())
                if (d.tid == selected_deck_tid)
                    return d;
            List<UserDeckData> decks = GetDecks();
            return decks.Count > 0 ? decks[0] : null;
        }

        //---- play modes ----

        public void StartSolo()
        {
            UserDeckData deck = GetSelectedDeck();
            if (deck == null || !deck.IsValid() || starting)
                return;

            DeckData[] ai_decks = GameplayData.Get().ai_decks;
            if (ai_decks == null || ai_decks.Length == 0)
                return;
            DeckData ai_deck = ai_decks[Random.Range(0, ai_decks.Length)];

            GameClient.player_settings.deck = deck;
            GameClient.ai_settings.deck = new UserDeckData(ai_deck);
            GameClient.ai_settings.ai_level = GameplayData.Get().ai_level;
            GameClient.game_settings.game_type = GameType.Solo;
            GameClient.game_settings.game_mode = GameMode.Casual;
            GameClient.game_settings.scene = GameplayData.Get().GetRandomArena();
            GameClient.game_settings.game_uid = GameTool.GenerateRandomID();
            StartCoroutine(LoadGame());
        }

        public void StartMultiplayer()
        {
            UserDeckData deck = GetSelectedDeck();
            if (deck == null || !deck.IsValid() || starting)
                return;

            GameClient.game_settings.game_type = GameType.Multiplayer;
            GameClient.game_settings.game_mode = GameMode.Casual;
            GameClient.player_settings.deck = deck;
            GameClient.game_settings.scene = GameplayData.Get().GetRandomArena();
            GameClientMatchmaker.Get().StartMatchmaking("", GameClient.game_settings.nb_players);

            Transform pop = GameObject.Find("Canvas - Main Menu").transform
                .Find("Main Content/Bottom Panel/Content/Searching Games");
            if (pop != null)
                pop.gameObject.SetActive(true);
        }

        public void OpenAdventure()
        {
            if (panels == null)
                return;
            //panel display names are scene-configured; match the chapters panel
            foreach (PanelManager.PanelItem item in panels.panels)
            {
                if (item.panelName.ToLower().Contains("chapter"))
                {
                    panels.OpenPanel(item.panelName);
                    return;
                }
            }
        }

        private IEnumerator LoadGame()
        {
            starting = true;
            ShowBattleCover(); //FULLSCREEN cover: the menu must not stay visible behind the load (user rule)
            TcgEngine.TcgNetwork.Get().Disconnect();
            yield return new WaitForSeconds(0.3f);
            SceneNav.GoTo(GameClient.game_settings.GetScene());
        }

        /// <summary>Fullscreen VS/loading cover over everything while the match
        /// scene loads — bg_vs key art + a battle line. Dies with the menu scene.</summary>
        private void ShowBattleCover()
        {
            GameObject go = new GameObject("CCG_BattleCover", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            Canvas cv = go.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 30000;

            GameObject bgo = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            RectTransform brt = bgo.GetComponent<RectTransform>();
            brt.SetParent(go.transform, false);
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            UnityEngine.UI.Image bg = bgo.GetComponent<UnityEngine.UI.Image>();
            bg.color = Color.black;

            Sprite vs = Resources.Load<Sprite>("UI/bg_vs");
            if (vs != null)
            {
                GameObject ago = new GameObject("Art", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                RectTransform art = ago.GetComponent<RectTransform>();
                art.SetParent(brt, false);
                UnityEngine.UI.Image ai = ago.GetComponent<UnityEngine.UI.Image>();
                ai.sprite = vs;
                ai.color = new Color(0.85f, 0.85f, 0.9f, 1f);
                ago.AddComponent<CCGCoverFit>();
                bgo.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
            }

            GameObject tgo = new GameObject("Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform trt = tgo.GetComponent<RectTransform>();
            trt.SetParent(go.transform, false);
            trt.anchorMin = new Vector2(0f, 0.1f); trt.anchorMax = new Vector2(1f, 0.22f);
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            TextMeshProUGUI tt = tgo.GetComponent<TextMeshProUGUI>();
            tt.text = "ENTERING BATTLE";
            tt.fontSize = 44;
            tt.alignment = TextAlignmentOptions.Center;
            tt.color = new Color(0.208f, 0.835f, 0.949f);
        }
    }
}
