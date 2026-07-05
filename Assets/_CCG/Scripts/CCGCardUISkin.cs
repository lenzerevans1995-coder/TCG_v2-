using UnityEngine;
using UnityEngine.UI;
using TcgEngine;
using TcgEngine.UI;
using CardUtilities;

namespace CCG
{
    /// <summary>
    /// Re-skins any kit CardUI (mulligan cards, zoom previews, spell-played FX) with
    /// the UNIVERSAL card face (CCGCardFace). Hides the kit's own card visuals and
    /// keeps them hidden (the kit repaints its Sprite every refresh).
    /// </summary>
    public class CCGCardUISkin : MonoBehaviour
    {
        private CardUI card_ui;
        private CCGCardFace face;
        private RectTransform face_root;
        private TcgEngine.CardData built;
        private float built_size = -1f;

        void Start()
        {
            card_ui = GetComponent<CardUI>();
            BuildFace();
            HideKit();
        }

        void LateUpdate()
        {
            if (card_ui == null || face == null)
                return;

            //Kit repaints its graphics every refresh (SetCard re-enables icons,
            //SetOpacity recolors) — re-hide every frame so they can never appear
            HideKit();
            FitFace();

            TcgEngine.CardData data = card_ui.GetCard();
            if (data != null && data != built)
            {
                built = data;
                face.Apply(data);
            }
        }

        /// <summary>
        /// Disables EVERY kit Graphic under this CardUI, whatever the prefab names
        /// its children (collection, deck builder, pack reveal, starter panel,
        /// mulligan, zoom all use different prefabs). Skips our own face and
        /// quantity badges (owned-count in the collection).
        /// </summary>
        private void HideKit()
        {
            foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
            {
                if (g == null || !g.enabled)
                    continue;
                if (g.name == "CCG_Hit") //the card's pointer surface — never disable
                    continue;
                if (face_root != null && g.transform.IsChildOf(face_root.parent))
                    continue;
                if (g.name.ToLowerInvariant().Contains("quantity"))
                    continue;
                g.enabled = false;
            }
        }

        /// <summary>
        /// Fit the face to the CardUI rect (layout groups size cards after Start,
        /// and grid cells vary per panel), preserving the 618x922 aspect.
        /// </summary>
        private void FitFace()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt == null || face_root == null)
                return;

            float w = rt.rect.width, h = rt.rect.height;
            if (w < 1f || h < 1f)
                return;

            float scale = Mathf.Min(w / 618f, h / 922f);
            if (Mathf.Abs(scale - built_size) > 0.001f)
            {
                built_size = scale;
                face_root.localScale = Vector3.one * scale;
            }
        }

        private void BuildFace()
        {
            RectTransform rt = GetComponent<RectTransform>();
            float scale = 0.5f;
            if (rt != null && rt.rect.width > 1f && rt.rect.height > 1f)
                scale = Mathf.Min(rt.rect.width / 618f, rt.rect.height / 922f);

            GameObject host = new GameObject("CCG_Face", typeof(RectTransform));
            RectTransform host_rt = host.GetComponent<RectTransform>();
            host_rt.SetParent(transform, false);
            host_rt.SetAsLastSibling();

            face = CCGCardFace.Create(host_rt, scale, true);
            face_root = host_rt.GetChild(0) as RectTransform;
            built_size = scale;

            //Invisible full-rect pointer surface: HideKit disables every kit
            //graphic (they repaint themselves), which also killed the card's
            //raycast target — hand/mulligan cards stopped receiving hover and
            //clicks. Alpha-0 images still raycast.
            if (transform.Find("CCG_Hit") == null)
            {
                GameObject hit_go = new GameObject("CCG_Hit", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform hit_rt = hit_go.GetComponent<RectTransform>();
                hit_rt.SetParent(transform, false);
                hit_rt.anchorMin = Vector2.zero;
                hit_rt.anchorMax = Vector2.one;
                hit_rt.offsetMin = Vector2.zero;
                hit_rt.offsetMax = Vector2.zero;
                Image hit = hit_go.GetComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                hit.raycastTarget = true;
            }
        }
    }

    /// <summary>
    /// Attaches CCGCardUISkin to every kit CardUI in EVERY scene (menus, deck
    /// builder, pack opening, collection, starter panel, mulligan, previews,
    /// spell FX). Spawned globally via CCGGlobalBootstrap; rescans fast to catch
    /// short-lived panels. NOWHERE in the game may an old kit card frame appear.
    /// </summary>
    public class CCGSkinBootstrap : MonoBehaviour
    {
        private static CCGSkinBootstrap instance;
        private float timer = 0f;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;

            //The editor's netcode transport never releases the UDP port between play
            //sessions; randomize per session so solo/test matches always bind.
            NetworkData ndata = NetworkData.Get();
            if (ndata != null)
                ndata.port = (ushort)Random.Range(20000, 60000);
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f)
                return;
            timer = 0.2f; //fast: spell-played FX cards live ~2s

            foreach (CardUI ui in Object.FindObjectsByType<CardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                //Board/hand prefabs own their visuals (CCGHexCardView tokens +
                //full faces). The generic skin's HideKit() was disabling the
                //HexView token images every frame — played cards were INVISIBLE
                //on the field.
                if (ui.GetComponentInParent<CCGHexCardView>(true) != null)
                {
                    CCGCardUISkin stale = ui.GetComponent<CCGCardUISkin>();
                    if (stale != null)
                        Destroy(stale);
                    continue;
                }
                if (ui.GetComponent<CCGCardUISkin>() == null)
                    ui.gameObject.AddComponent<CCGCardUISkin>();
            }

            foreach (TcgEngine.Client.PackCard pc in Object.FindObjectsByType<TcgEngine.Client.PackCard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (pc.GetComponent<CCGPackCardSkin>() == null)
                    pc.gameObject.AddComponent<CCGPackCardSkin>();
            }

            //CROWS battlefield tokens: dress every kit BoardCard with the pack
            //square frame + corner stats (world-space SpriteRenderers)
            foreach (TcgEngine.Client.BoardCard bc in Object.FindObjectsByType<TcgEngine.Client.BoardCard>(FindObjectsSortMode.None))
            {
                if (bc.GetComponent<CrowsBoardToken>() == null)
                    bc.gameObject.AddComponent<CrowsBoardToken>();
            }
        }
    }

    /// <summary>
    /// Companion for pack-reveal cards: scales the card back sprite to fully
    /// cover the card face, and hides the front canvas while the card is
    /// face-down (UI graphics render both sides, so the mirrored front would
    /// bleed through the back otherwise).
    /// </summary>
    public class CCGPackCardSkin : MonoBehaviour
    {
        private TcgEngine.Client.PackCard pack_card;
        private Canvas front_canvas;

        void Start()
        {
            pack_card = GetComponent<TcgEngine.Client.PackCard>();
            front_canvas = GetComponentInChildren<Canvas>(true);
        }

        void LateUpdate()
        {
            if (pack_card == null)
                return;

            //RARITY-COLORED back (user rule: backs represent rarity) — the moment
            //of anticipation before the flip
            SpriteRenderer back = pack_card.cardback;
            if (back != null && pack_card.GetCard() != null && pack_card.GetCard().rarity != null)
            {
                Sprite rb = Resources.Load<Sprite>("GUI/gui_cardback_" + pack_card.GetCard().rarity.id);
                if (rb != null && back.sprite != rb)
                    back.sprite = rb;
            }

            //Back sprite exactly covers the front card rect (kit prefab scale fits
            //the kit's old back sprite, not our baked 618x922 backs)
            if (back != null && back.sprite != null && pack_card.card_ui != null)
            {
                RectTransform rt = pack_card.card_ui.GetComponent<RectTransform>();
                Vector2 world = Vector2.Scale(rt.rect.size, rt.lossyScale);
                Vector2 sprite_size = back.sprite.rect.size / back.sprite.pixelsPerUnit;
                if (sprite_size.x > 0.01f && sprite_size.y > 0.01f)
                    back.transform.localScale = new Vector3(world.x / sprite_size.x, world.y / sprite_size.y, 1f);
            }

            //Show the front only when it faces the camera
            if (front_canvas != null)
            {
                float y = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, 0f));
                front_canvas.enabled = y < 90f;
            }
        }
    }

    /// <summary>
    /// Spawns the skin bootstrap once per play session so card re-skinning runs
    /// in ALL scenes (LoginMenu, Menu, OpenPack, game) — no scene setup required.
    /// </summary>
    public static class CCGGlobalBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            GameObject go = new GameObject("CCG_GlobalBootstrap");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<CCGSkinBootstrap>();
        }
    }
}
