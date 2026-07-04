using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine;
using TcgEngine.Client;
using CardUtilities;

namespace CCG
{
    /// <summary>
    /// Board zone display (docked-HUD layout, mockup match_hud.svg v5): the
    /// board holds only PLAYABLE cards — the kit's minion BoardSlots (parented
    /// under Minion_1..5 anchors of the "CCG_BoardLayout" template) and the
    /// BACK ROW (Land + artifact/relic cards on Land/BackSlot_1..3 anchors),
    /// all world cards lying flat at ONE uniform token scale. Hero, weapon,
    /// armor, deck and discard are screen-space panes drawn by CCGMatchHUD.
    /// </summary>
    public class CCGEquipmentDisplay : MonoBehaviour
    {
        [Tooltip("Uniform face scale for ALL world cards (matches CCG_BoardCard)")]
        public float card_scale = 0.22f;

        private Transform layout;
        private Transform player_root;
        private Transform enemy_root;
        private string state_hash = "";

        private static readonly string[] slot_traits = { "head", "chest", "arms", "legs" };

        void Start()
        {
            GameObject lgo = GameObject.Find("CCG_BoardLayout");
            layout = lgo != null ? lgo.transform : null;
            if (layout == null)
                Debug.LogWarning("CCG_BoardLayout template not found in scene; zone display disabled.");

            player_root = new GameObject("CCG_PlayerZones").transform;
            enemy_root = new GameObject("CCG_EnemyZones").transform;
            HideKitPiles();
        }

        //The kit draws flat deck/discard sprites + counters on the table; our
        //standing world-card piles replace them.
        private void HideKitPiles()
        {
            foreach (var deck in GameObject.FindObjectsByType<BoardDeck>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Transform sprite = deck.transform.Find("Deck");
                if (sprite != null) sprite.gameObject.SetActive(false);
            }
            foreach (var canvas in GameObject.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.renderMode != RenderMode.WorldSpace)
                    continue;
                foreach (Transform child in canvas.transform)
                {
                    string n = child.name;
                    if (n == "Deck" || n == "Discard" || n == "DeckValue" || n == "DiscardValue")
                        child.gameObject.SetActive(false);
                }
            }
        }

        private Transform Anchor(bool enemy, string name)
        {
            if (layout == null) return null;
            Transform side = layout.Find(enemy ? "EnemySide" : "PlayerSide");
            return side != null ? side.Find(name) : null;
        }

        void Update()
        {
            if (layout == null)
                return;
            GameClient client = GameClient.Get();
            if (client == null)
                return;
            Game data = client.GetGameData();
            bool playing = data != null && data.state == GameState.Play;
            if (player_root != null && player_root.gameObject.activeSelf != playing)
            {
                player_root.gameObject.SetActive(playing);
                enemy_root.gameObject.SetActive(playing);
            }
            if (!playing)
                return;

            int my_id = client.GetPlayerID();
            Player me = data.GetPlayer(my_id);
            Player opp = data.GetOpponentPlayer(my_id);
            if (me == null || opp == null)
                return;

            System.Text.StringBuilder hb = new System.Text.StringBuilder();
            AppendState(hb, data, me);
            AppendState(hb, data, opp);
            //Template edits (moved anchors) also trigger a rebuild
            foreach (Transform side in layout)
                foreach (Transform a in side)
                    hb.Append(a.position.x.ToString("F2")).Append(',').Append(a.position.z.ToString("F2")).Append(';');
            string hash = hb.ToString();
            if (hash == state_hash)
                return;
            state_hash = hash;

            Rebuild(data, me, player_root, false);
            Rebuild(data, opp, enemy_root, true);
        }

        private void AppendState(System.Text.StringBuilder sb, Game data, Player p)
        {
            sb.Append(p.hp).Append('|').Append(p.mana).Append('|');
            sb.Append(p.cards_deck.Count).Append('|').Append(p.cards_discard.Count).Append('|');
            if (p.cards_discard.Count > 0)
                sb.Append(p.cards_discard[p.cards_discard.Count - 1].uid).Append('|');
            foreach (Card c in p.cards_equip)
                sb.Append(c.uid).Append(':').Append(c.GetHP()).Append(';');
            foreach (Card c in p.cards_board)
                sb.Append(c.uid).Append('@').Append(c.slot.x).Append(';');
        }

        //Docked-HUD layout (mockup match_hud.svg v5): hero, weapon, armor and
        //the deck/discard piles are drawn by CCGMatchHUD in screen-space docks.
        //The board keeps only PLAYABLE cards: minions (kit BoardCards), and the
        //back row — LAND plus artifact/relic cards on the BackSlot anchors.
        private void Rebuild(Game data, Player player, Transform root, bool enemy)
        {
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            CardScriptable style = player.hero != null ? CCGCardFace.GetStyle(player.hero.CardData) : null;

            //--- Back row: land + artifacts/relics (equip cards that are not
            //    weapon/armor/field), same token size as the minion row ---
            Transform land_a = Anchor(enemy, "Land");
            if (land_a != null)
            {
                Card field = FindFieldCard(player);
                if (field != null)
                    MakeWorldCard(field.CardData, root, land_a.position, 0, field.GetHP(), false, false, out _);
                else
                    MakeEmptySlot(style, root, land_a.position, "LAND", 0.09f);
            }

            List<Card> artifacts = new List<Card>();
            foreach (Card c in player.cards_equip)
            {
                if (c.CardData.HasTrait("weapon") || c.CardData.HasTrait("field") || IsArmor(c))
                    continue;
                artifacts.Add(c);
            }
            for (int i = 1; i <= 3; i++)
            {
                Transform a = Anchor(enemy, "BackSlot_" + i);
                if (a == null) continue;
                if (i - 1 < artifacts.Count)
                {
                    Card art = artifacts[i - 1];
                    MakeWorldCard(art.CardData, root, a.position, art.GetAttack(), art.GetHP(), false, false, out _, art);
                }
                else
                {
                    MakeEmptySlot(style, root, a.position, "", 0.04f);
                }
            }

            //--- Minion row: dim card-slot frames at unoccupied template slots ---
            for (int x = 1; x <= 5; x++)
            {
                Transform a = Anchor(enemy, "Minion_" + x);
                if (a == null) continue;
                bool occupied = false;
                foreach (Card c in player.cards_board)
                    if (c.slot.x == x) { occupied = true; break; }
                if (!occupied)
                    MakeEmptySlot(style, root, a.position, "", 0.05f);
            }
        }

        private bool IsArmor(Card c)
        {
            foreach (string t in slot_traits)
                if (c.CardData.HasTrait(t))
                    return true;
            return false;
        }

        //Field cards live in cards_equip (moved there on play) — shown on LAND
        private Card FindFieldCard(Player player)
        {
            foreach (Card c in player.cards_equip)
                if (c.CardData.HasTrait("field"))
                    return c;
            return null;
        }

        //A world-space canvas lying FLAT on the table holding the universal card
        //face - identical presentation and size to the board minion cards.
        //Pass live_card to enable hover (outline glow + big card preview).
        private CCGCardFace MakeWorldCard(TcgEngine.CardData cdata, Transform root, Vector3 pos, int attack, int hp, bool show_attack, bool show_cost, out Canvas card_canvas, Card live_card = null)
        {
            GameObject go = new GameObject("WorldCard_" + cdata.id, typeof(RectTransform), typeof(Canvas));
            card_canvas = go.GetComponent<Canvas>();
            card_canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(root, false);
            rt.sizeDelta = new Vector2(200, 200);
            rt.localScale = new Vector3(0.01f, 0.01f, 1f);
            rt.localPosition = pos + new Vector3(0f, 0.05f, 0f); //lift above the table plane
            rt.localRotation = Quaternion.Euler(90f, 0f, 0f);    //flat on the board, like minions

            //Zone cards are compact TOKENS like the board minions
            CCGCardFace face = CCGCardFace.CreateToken(go.transform, card_scale);
            face.Apply(cdata);
            face.SetStats(attack, hp);
            //(top-down camera: both sides equal size)
            if (!show_attack && face.atk_icon != null)
            {
                face.atk_icon.gameObject.SetActive(false);
                face.attack_txt.gameObject.SetActive(false);
            }
            //Weapon swing cost shown in the keyword strip (tokens carry no cost gem)
            if (show_cost && face.keyword_txt != null)
            {
                string kw = face.keyword_txt.text;
                face.keyword_txt.text = ("Cost " + cdata.mana + (kw.Length > 0 ? " · " + kw : ""));
                face.keyword_txt.gameObject.SetActive(true);
            }

            if (live_card != null)
            {
                //Physics click/hover surface (input via CCGHeroAttackControls raycasts).
                //Layer 2 (Ignore Raycast): invisible to the kit's pointer raycasts.
                go.layer = 2;
                BoxCollider col = go.AddComponent<BoxCollider>();
                //FLAT collider: a 10-deep pillar let perspective rays clip the SIDE
                //of neighboring pillars (hover over HEAD hit CHEST; hand hover hit
                //hero/weapon). Near-board thickness keeps hits on the actual tile.
                col.size = new Vector3(CCGCardFace.TOKEN_W * card_scale, CCGCardFace.TOKEN_H * card_scale, 0.4f);
                go.AddComponent<CCGZoneCardHover>().Setup(live_card, face, card_canvas);
            }
            return face;
        }

        //Dim card-frame silhouette with an optional label, in the player's faction dress
        private void MakeEmptySlot(CardScriptable style, Transform root, Vector3 pos, string label, float alpha)
        {
            GameObject go = new GameObject("EmptySlot_" + label, typeof(RectTransform), typeof(Canvas));
            Canvas cv = go.GetComponent<Canvas>();
            cv.renderMode = RenderMode.WorldSpace;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(root, false);
            rt.sizeDelta = new Vector2(200, 200);
            rt.localScale = new Vector3(0.01f, 0.01f, 1f);
            rt.localPosition = pos + new Vector3(0f, 0.03f, 0f);
            rt.localRotation = Quaternion.Euler(90f, 0f, 0f);

            //Rounded dim slot: faint rounded border + darker rounded fill (matches tokens)
            GameObject fgo = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform frt = fgo.GetComponent<RectTransform>();
            frt.SetParent(go.transform, false);
            frt.sizeDelta = new Vector2(CCGCardFace.TOKEN_W, CCGCardFace.TOKEN_H);
            frt.localScale = Vector3.one * card_scale;
            Image img = fgo.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, alpha);
            img.raycastTarget = false;
            CCGCardFace.SetRounded(img, CCGCardFace.GetRoundShape(), 0.55f);
            GameObject igo2 = new GameObject("Inner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform irt2 = igo2.GetComponent<RectTransform>();
            irt2.SetParent(frt, false);
            irt2.sizeDelta = new Vector2(CCGCardFace.TOKEN_W - 10f, CCGCardFace.TOKEN_H - 10f);
            Image inner = igo2.GetComponent<Image>();
            inner.color = new Color(0.06f, 0.08f, 0.12f, alpha * 6f);
            inner.raycastTarget = false;
            CCGCardFace.SetRounded(inner, CCGCardFace.GetRoundShape(), 0.57f);

            if (!string.IsNullOrEmpty(label))
            {
                GameObject tgo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                RectTransform trt = tgo.GetComponent<RectTransform>();
                trt.SetParent(frt, false);
                trt.sizeDelta = new Vector2(500, 100);
                trt.anchoredPosition = Vector2.zero;
                Text t = tgo.GetComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 72;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = new Color(1f, 1f, 1f, 0.3f);
                t.text = label;
                t.raycastTarget = false;
            }
        }
    }
}
