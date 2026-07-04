using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TcgEngine;
using Michsky.UI.Heat;
using TCard = TcgEngine.CardData; //asset pack defines a global CardData

namespace CCG
{
    /// <summary>
    /// Removes the Heat theme-driver component so a custom color survives.
    /// Disabling isn't enough: UIManagerImage/Text re-apply manager colors in
    /// Awake, which fires when a hidden panel first activates — stomping any
    /// custom color set at build time. DestroyImmediate avoids that race.
    /// </summary>
    public static class CCGHeatThemeExtensions
    {
        public static void DestroyThemeDriver(this UnityEngine.UI.Image img)
        {
            var d = img.GetComponent<UIManagerImage>();
            if (d != null) Object.DestroyImmediate(d);
        }

        public static void DestroyThemeDriver(this TMPro.TextMeshProUGUI txt)
        {
            var d = txt.GetComponent<UIManagerText>();
            if (d != null) Object.DestroyImmediate(d);
        }
    }

    /// <summary>
    /// THE FORGE — LoR-style economy in the Heat PACKS panel. No packs:
    /// wildcards are INVENTORY ITEMS bought with coins (or earned later via
    /// vault/quests); actual card unlocks happen in the COLLECTION on unowned
    /// cards (CCGCollectionUI consumes the wildcards). Wildcards live in
    /// UserData.packs as "wc_&lt;rarity&gt;" so the kit save carries them.
    /// </summary>
    public class CCGForgeUI : MonoBehaviour
    {
        public static string WildcardTid(RarityData rarity) { return rarity != null ? "wc_" + rarity.id : null; }

        public static int WildcardPrice(RarityData rarity)
        {
            //five tiers by rank order
            int[] prices = { 100, 200, 400, 1000, 2400 };
            List<RarityData> all = SortedRarities();
            int idx = Mathf.Clamp(all.IndexOf(rarity), 0, prices.Length - 1);
            return prices[idx];
        }

        public static Color RarityColor(RarityData rarity)
        {
            Color[] colors = {
                new Color(1.000f, 0.259f, 0.318f), //common - red back
                new Color(0.000f, 0.725f, 0.333f), //uncommon - green back
                new Color(0.000f, 0.824f, 1.000f), //rare - blue back
                new Color(0.788f, 0.251f, 1.000f), //epic - purple back
                new Color(1.000f, 0.843f, 0.353f), //mythic - gold back
            };
            List<RarityData> all = SortedRarities();
            return colors[Mathf.Clamp(all.IndexOf(rarity), 0, colors.Length - 1)];
        }

        /// <summary>All five rarities by rank (five back styles — user rule).</summary>
        public static List<RarityData> SortedRarities()
        {
            List<RarityData> list = new List<RarityData>(RarityData.GetAll());
            list.Sort((a, b) => a.rank.CompareTo(b.rank));
            return list;
        }

        private Transform panel_root;
        private bool built = false;
        private Transform tile_row;
        private TextMeshProUGUI coins_txt;

        private UserData UData { get { return Authenticator.Get().UserData; } }

        void Update()
        {
            if (built || Authenticator.Get() == null || Authenticator.Get().UserData == null)
                return;
            Transform canvas_t = GameObject.Find("Canvas - Main Menu").transform;
            panel_root = canvas_t.Find("Main Content/Shop/Content/Panel Content");
            if (panel_root == null)
                return;
            built = true;
            Build();
        }

        //--- primitives (Heat-themed) ---
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
        private UnityEngine.UI.Button MakeButton(string label, Transform parent, UnityEngine.Events.UnityAction action, float font_size = 20,
            UIManagerImage.ColorType bg = UIManagerImage.ColorType.Secondary,
            UIManagerText.ColorType fg = UIManagerText.ColorType.Primary)
        {
            Image img = MakePanel("Btn_" + label, parent, bg, 1f);
            UnityEngine.UI.Button btn = img.gameObject.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            if (action != null)
                btn.onClick.AddListener(action);
            TextMeshProUGUI txt = MakeText("Label", img.transform, label, font_size, UIManagerText.FontType.Semibold, fg);
            Anchor(txt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
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
            tmp.text = text; tmp.fontSize = size; tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            UIManagerText mt = rt.gameObject.AddComponent<UIManagerText>();
            mt.fontType = font; mt.colorType = color;
            return tmp;
        }

        private void Build()
        {
            //hide Heat's demo shop content
            foreach (Transform child in panel_root)
                if (!child.name.StartsWith("CCG_"))
                    child.gameObject.SetActive(false);

            //NAV RULE: measured header clearance — nothing renders under the bar
            //(fullscreen background art is handled globally per-tab by CCGHeatMenu)
            RectTransform root = MakeRect("CCG_Forge", panel_root);
            Anchor(root, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -CCGNavUtil.Clearance(panel_root as RectTransform)));

            TextMeshProUGUI title = MakeText("Title", root, "THE FORGE", 34, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary, TextAlignmentOptions.TopLeft);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(8f, -46f), new Vector2(0f, -4f));
            TextMeshProUGUI sub = MakeText("Sub", root, "No packs, no gambling — buy or earn wildcards, unlock exact cards in your COLLECTION.", 16, UIManagerText.FontType.Regular, UIManagerText.ColorType.Primary, TextAlignmentOptions.TopLeft);
            sub.color = new Color(sub.color.r, sub.color.g, sub.color.b, 0.6f);
            Anchor(sub.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -72f), new Vector2(0f, -48f));

            //wildcard tiles
            RectTransform row = MakeRect("Tiles", root);
            Anchor(row, new Vector2(0f, 0.28f), new Vector2(0.68f, 1f), new Vector2(8f, 8f), new Vector2(-8f, -80f));
            HorizontalLayoutGroup hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 14f;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = true;
            tile_row = row;
            RefreshTiles();

            //vault (display prototype)
            Image vault = MakePanel("Vault", root, UIManagerImage.ColorType.Secondary, 0.8f);
            Anchor(vault.rectTransform, new Vector2(0.7f, 0.28f), new Vector2(1f, 1f), new Vector2(0f, 8f), new Vector2(-8f, -80f));
            TextMeshProUGUI vt = MakeText("VT", vault.transform, "WEEKLY VAULT", 20, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary, TextAlignmentOptions.TopLeft);
            Anchor(vt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -36f), new Vector2(-14f, -8f));
            TextMeshProUGUI vs = MakeText("VS", vault.transform,
                "Win matches to level the vault.\nIRON: 4 commons + 100 coins\nSTEEL: + 2 rares + rare wildcard\nMYTHIC: + epic wildcard\n\n(prototype — earning hooks land\nwith the quest system)",
                14, UIManagerText.FontType.Regular, UIManagerText.ColorType.Primary, TextAlignmentOptions.TopLeft);
            vs.textWrappingMode = TextWrappingModes.Normal;
            vs.color = new Color(vs.color.r, vs.color.g, vs.color.b, 0.75f);
            Anchor(vs.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 10f), new Vector2(-14f, -44f));

            //quest strip (display prototype)
            Image quest = MakePanel("Quest", root, UIManagerImage.ColorType.Secondary, 0.8f);
            Anchor(quest.rectTransform, new Vector2(0f, 0f), new Vector2(0.68f, 0.28f), new Vector2(8f, 8f), new Vector2(-8f, -8f));
            TextMeshProUGUI qt = MakeText("QT", quest.transform, "TODAY'S QUEST — Play 10 Sanguine cards   ·   REWARD: 150 coins + COMMON WILDCARD   (prototype)", 15, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary, TextAlignmentOptions.MidlineLeft);
            Anchor(qt.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, 0f));

            //CART (user rule): pick amounts on the tiles, then one checkout
            Image cart_panel = MakePanel("Cart", root, UIManagerImage.ColorType.Secondary, 0.9f);
            Anchor(cart_panel.rectTransform, new Vector2(0.7f, 0f), new Vector2(1f, 0.28f), new Vector2(0f, 8f), new Vector2(-8f, -8f));
            cart_txt = MakeText("CartTxt", cart_panel.transform, "", 15, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary, TextAlignmentOptions.TopLeft);
            cart_txt.textWrappingMode = TextWrappingModes.Normal;
            Anchor(cart_txt.rectTransform, new Vector2(0f, 0.4f), new Vector2(1f, 1f), new Vector2(14f, 0f), new Vector2(-14f, -10f));
            coins_txt = MakeText("Coins", cart_panel.transform, "", 16, UIManagerText.FontType.Bold, UIManagerText.ColorType.Accent, TextAlignmentOptions.MidlineLeft);
            Anchor(coins_txt.rectTransform, new Vector2(0f, 0f), new Vector2(0.45f, 0.4f), new Vector2(14f, 6f), new Vector2(0f, 0f));
            checkout_btn = MakeButton("CHECKOUT", cart_panel.transform, CheckoutCart, 16, UIManagerImage.ColorType.Accent, UIManagerText.ColorType.AccentMatch);
            Anchor(checkout_btn.GetComponent<RectTransform>(), new Vector2(0.46f, 0.04f), new Vector2(0.98f, 0.4f), Vector2.zero, Vector2.zero);
            checkout_label = checkout_btn.GetComponentInChildren<TextMeshProUGUI>();
            RefreshCoins();
            RefreshCart();
        }

        //---- cart ----
        private readonly Dictionary<string, int> cart = new Dictionary<string, int>();
        private TextMeshProUGUI cart_txt;
        private Button checkout_btn;
        private TextMeshProUGUI checkout_label;

        private int CartQty(RarityData r)
        {
            return cart.TryGetValue(r.id, out int n) ? n : 0;
        }

        private int CartTotal()
        {
            int total = 0;
            foreach (RarityData r in SortedRarities())
                total += CartQty(r) * WildcardPrice(r);
            return total;
        }

        private void RefreshCart()
        {
            if (cart_txt == null) return;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int items = 0;
            foreach (RarityData r in SortedRarities())
            {
                int n = CartQty(r);
                if (n <= 0) continue;
                items += n;
                Color rc = RarityColor(r);
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(rc)).Append(">")
                  .Append(n).Append("x ").Append((r.title != "" ? r.title : r.id).ToUpper()).Append("</color>   ");
            }
            int total = CartTotal();
            cart_txt.text = items == 0 ? "CART EMPTY - use + on a wildcard" : "CART:  " + sb.ToString();
            if (checkout_label != null)
                checkout_label.text = items == 0 ? "CHECKOUT" : "CHECKOUT - " + total + " COINS";
            if (checkout_btn != null)
                checkout_btn.interactable = items > 0 && UData != null && UData.coins >= total;
        }

        private async void CheckoutCart()
        {
            int total = CartTotal();
            if (total <= 0 || UData.coins < total)
                return;
            UData.coins -= total;
            foreach (RarityData r in SortedRarities())
            {
                int n = CartQty(r);
                if (n > 0)
                    UData.AddPack(WildcardTid(r), n);
            }
            cart.Clear();
            await Authenticator.Get().SaveUserData();
            RefreshTiles();
        }

        private void RefreshCoins()
        {
            if (coins_txt != null && UData != null)
                coins_txt.text = UData.coins + " COINS";
        }

        public void RefreshTiles()
        {
            if (tile_row == null) return;
            for (int i = tile_row.childCount - 1; i >= 0; i--)
                Destroy(tile_row.GetChild(i).gameObject);

            foreach (RarityData rarity in SortedRarities())
            {
                RarityData r = rarity;
                Color rc = RarityColor(r);
                int price = WildcardPrice(r);
                int owned = UData.GetPackQuantity(WildcardTid(r));

                Image tile = MakePanel("Tile_" + r.id, tile_row, UIManagerImage.ColorType.Secondary, 1f);
                //rarity colored frame
                Image frame = MakePanel("Frame", tile.transform, UIManagerImage.ColorType.Secondary, 1f);
                frame.DestroyThemeDriver();
                frame.color = rc;
                Anchor(frame.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Image fill = MakePanel("Fill", tile.transform, UIManagerImage.ColorType.Background, 1f);
                Anchor(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));

                //the rarity's actual card back (backs represent rarity — user rule)
                Sprite back = Resources.Load<Sprite>("GUI/gui_cardback_" + r.id);
                if (back != null)
                {
                    Image bi = MakePanel("Back", tile.transform, UIManagerImage.ColorType.Background, 1f);
                    bi.DestroyThemeDriver();
                    bi.sprite = back;
                    bi.color = Color.white;
                    bi.preserveAspect = true;
                    Anchor(bi.rectTransform, new Vector2(0.12f, 0.45f), new Vector2(0.88f, 0.95f), Vector2.zero, Vector2.zero);
                }
                else
                {
                    TextMeshProUGUI q = MakeText("Q", tile.transform, "?", 60, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary);
                    q.DestroyThemeDriver();
                    q.color = rc;
                    Anchor(q.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 0.95f), Vector2.zero, Vector2.zero);
                }

                TextMeshProUGUI nm = MakeText("Name", tile.transform, r.title != "" ? r.title.ToUpper() : r.id.ToUpper(), 20, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary);
                nm.DestroyThemeDriver();
                nm.color = rc;
                Anchor(nm.rectTransform, new Vector2(0f, 0.32f), new Vector2(1f, 0.44f), Vector2.zero, Vector2.zero);
                TextMeshProUGUI wl = MakeText("WL", tile.transform, "WILDCARD", 13, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary);
                wl.color = new Color(wl.color.r, wl.color.g, wl.color.b, 0.55f);
                Anchor(wl.rectTransform, new Vector2(0f, 0.24f), new Vector2(1f, 0.32f), Vector2.zero, Vector2.zero);

                //owned badge
                TextMeshProUGUI ob = MakeText("Owned", tile.transform, "OWN " + owned, 14, UIManagerText.FontType.Bold, UIManagerText.ColorType.Primary);
                ob.DestroyThemeDriver();
                ob.color = owned > 0 ? rc : new Color(0.35f, 0.4f, 0.47f);
                Anchor(ob.rectTransform, new Vector2(0f, 0.95f), new Vector2(1f, 1.0f), new Vector2(0f, -6f), new Vector2(0f, 0f));

                //price line + cart stepper (amounts checkout together, user rule)
                TextMeshProUGUI pt = MakeText("Price", tile.transform, price + " COINS EACH", 12, UIManagerText.FontType.Medium, UIManagerText.ColorType.Primary);
                pt.color = new Color(pt.color.r, pt.color.g, pt.color.b, 0.6f);
                Anchor(pt.rectTransform, new Vector2(0f, 0.19f), new Vector2(1f, 0.25f), Vector2.zero, Vector2.zero);

                int in_cart = CartQty(r);
                Button minus = MakeButton("-", tile.transform, () => {
                    cart[r.id] = Mathf.Max(0, CartQty(r) - 1);
                    RefreshTiles();
                }, 20, UIManagerImage.ColorType.Background, UIManagerText.ColorType.Negative);
                Anchor(minus.GetComponent<RectTransform>(), new Vector2(0.08f, 0.05f), new Vector2(0.3f, 0.18f), Vector2.zero, Vector2.zero);
                TextMeshProUGUI qty = MakeText("Qty", tile.transform, in_cart.ToString(), 20, UIManagerText.FontType.Bold, in_cart > 0 ? UIManagerText.ColorType.Accent : UIManagerText.ColorType.Primary);
                Anchor(qty.rectTransform, new Vector2(0.32f, 0.05f), new Vector2(0.68f, 0.18f), Vector2.zero, Vector2.zero);
                Button plus = MakeButton("+", tile.transform, () => {
                    cart[r.id] = CartQty(r) + 1;
                    RefreshTiles();
                }, 20, UIManagerImage.ColorType.Accent, UIManagerText.ColorType.AccentMatch);
                Anchor(plus.GetComponent<RectTransform>(), new Vector2(0.7f, 0.05f), new Vector2(0.92f, 0.18f), Vector2.zero, Vector2.zero);
            }
            RefreshCoins();
            RefreshCart();
        }
    }
}
