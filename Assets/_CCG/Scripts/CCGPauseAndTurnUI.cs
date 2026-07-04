using UnityEngine;
using UnityEngine.UI;
using TcgEngine;
using TcgEngine.Client;

namespace CCG
{
    /// <summary>
    /// Two match-flow fixes:
    /// 1. PAUSE actually pauses — while the kit menu panel is open, Time.timeScale
    ///    is 0 (turn timer and AI freeze).
    /// 2. TURN BANNER — a big "YOUR TURN" / "ENEMY TURN" flash on every turn
    ///    change, plus a persistent tint on the turn box.
    /// </summary>
    public class CCGPauseAndTurnUI : MonoBehaviour
    {
        private GameObject menu_panel;
        private CanvasGroup menu_group;
        private TcgEngine.UI.UIPanel menu_ui;
        private Text banner_txt;
        private CanvasGroup banner_group;
        private float banner_timer = 0f;
        private int last_turn_player = -1;

        void Start()
        {
            BindMenuPanel();

            //Banner canvas
            GameObject cgo = new GameObject("CCG_TurnBanner", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            GameObject tgo = new GameObject("Banner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(CanvasGroup));
            RectTransform trt = tgo.GetComponent<RectTransform>();
            trt.SetParent(cgo.transform, false);
            trt.anchorMin = new Vector2(0.5f, 0.5f);
            trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(0f, 120f);
            trt.sizeDelta = new Vector2(1000, 140);
            banner_txt = tgo.GetComponent<Text>();
            banner_txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            banner_txt.fontSize = 92;
            banner_txt.fontStyle = FontStyle.Bold;
            banner_txt.alignment = TextAnchor.MiddleCenter;
            banner_txt.raycastTarget = false;
            tgo.AddComponent<Shadow>().effectDistance = new Vector2(4, -4);
            tgo.AddComponent<Outline>().effectDistance = new Vector2(2, -2);
            banner_group = tgo.GetComponent<CanvasGroup>();
            banner_group.alpha = 0f;
        }

        void OnDestroy()
        {
            Time.timeScale = 1f; //never leave the game frozen
        }

        /// <summary>MenuPanel starts INACTIVE — GameObject.Find misses it, so
        /// search including inactive and retry until bound.</summary>
        private void BindMenuPanel()
        {
            foreach (TcgEngine.UI.UIPanel p in Object.FindObjectsByType<TcgEngine.UI.UIPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (p.gameObject.name == "MenuPanel")
                {
                    menu_panel = p.gameObject;
                    menu_ui = p;
                    menu_group = p.GetComponent<CanvasGroup>();
                    return;
                }
            }
        }

        void Update()
        {
            if (menu_panel == null)
                BindMenuPanel();

            //--- Pause while the menu is open ---
            //Use the panel's TARGET state, not its animated alpha: the kit fades
            //panels with SCALED time, so at timeScale 0 the alpha never drops and
            //RESUME deadlocks the pause (alpha stays 1 -> stays paused forever).
            bool menu_open;
            if (menu_ui != null)
                menu_open = menu_ui.IsVisible() && menu_panel.activeInHierarchy;
            else if (menu_group != null)
                menu_open = menu_group.alpha > 0.5f && menu_panel.activeInHierarchy;
            else
                menu_open = false;
            float target_scale = menu_open ? 0f : 1f;
            if (!Mathf.Approximately(Time.timeScale, target_scale))
                Time.timeScale = target_scale;

            //--- Turn banner ---
            GameClient client = GameClient.Get();
            Game data = client != null ? client.GetGameData() : null;
            if (data == null || data.state != GameState.Play)
            {
                last_turn_player = -1;
                if (banner_group != null) banner_group.alpha = 0f;
                return;
            }

            if (data.current_player != last_turn_player)
            {
                last_turn_player = data.current_player;
                bool mine = data.current_player == client.GetPlayerID();
                banner_txt.text = mine ? "YOUR TURN" : "ENEMY TURN";
                banner_txt.color = mine ? new Color(0.35f, 1f, 0.9f) : new Color(1f, 0.4f, 0.35f);
                banner_timer = 2f;
            }

            if (banner_timer > 0f)
            {
                banner_timer -= Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(banner_timer / 0.5f);           //fade out last 0.5s
                float ain = Mathf.Clamp01((2f - banner_timer) / 0.25f); //fade in first 0.25s
                banner_group.alpha = Mathf.Min(a, ain);
            }
            else
            {
                banner_group.alpha = 0f;
            }
        }
    }
}
