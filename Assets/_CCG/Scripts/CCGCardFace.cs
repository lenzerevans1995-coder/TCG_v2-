using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TcgEngine;
using CardUtilities;

namespace CCG
{
    /// <summary>
    /// THE universal card renderer, two layouts from one megapack style source:
    /// - FULL card (618x922): hand, mulligan, previews — frame, cloud art, badges,
    ///   rarity strip, class symbol, type line, rules text.
    /// - TOKEN (618x340, phase-style): board + zones — 9-sliced BG_Front tile
    ///   tinted by faction, cloud-masked art, name bar, swords/shield stat gems,
    ///   one-line keyword. Detail lives in the hover preview.
    /// </summary>
    public class CCGCardFace
    {
        public RectTransform root;
        public Image frame_bg;
        public Image art_mask;
        public Image art;
        public Image bg_text;
        public Image frame_border;
        public Image cost_icon;
        public Image pitch_icon;
        public Image atk_icon;
        public Image hp_icon;
        public Image class_icon;
        public Image[] class_icon_layers = new Image[3]; //stacked: symbol sprites have soft alpha, layering reads solid
        public Image[] rarity_icons = new Image[5];
        public TextMeshProUGUI title_txt;
        public TextMeshProUGUI desc_txt;
        public Text cost_txt;
        public Text attack_txt;
        public Text hp_txt;
        public Text pitch_txt;
        public Text type_txt;
        public Text keyword_txt;   //token: one-line keyword strip
        public Image block_icon;   //full face: BLOCK value plate (defense window)
        public Text block_txt;
        public bool show_cost = true;
        public bool token_mode = false;

        public const float TOKEN_W = 520f;
        public const float TOKEN_H = 480f;

        private float art_win_w = 512f;
        private float art_win_h = 816f;

        private static Sprite rarity_fill;
        private static Sprite rarity_slot;

        /// <summary>FULL card: CROWS pack frame (1800x2520 px scaled to 618x865 units),
        /// geometry from the approved composite spec (compose_card_preview.py).</summary>
        public static CCGCardFace Create(Transform parent, float scale, bool show_cost = true)
        {
            const float K = 618f / 1800f; //frame px -> ui units
            const float TOP = 865f * 0.5f;
            CCGCardFace f = new CCGCardFace();
            f.root = MakeRoot(parent, scale, new Vector2(618, 865));
            f.art_win_w = (1668 - 130) * K;
            f.art_win_h = (1417 - 133) * K;

            f.art_mask = Img("ArtMask", f.root, new Vector2(f.art_win_w, f.art_win_h), new Vector2(899 * K - 309f, TOP - 775 * K));
            f.art_mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            //backdrop: pack Artwork pieces are figures on TRANSPARENT ground — they
            //need a dark stage behind them or the window reads empty
            f.frame_bg = Img("ArtBackdrop", f.art_mask.rectTransform, new Vector2(f.art_win_w, f.art_win_h), Vector2.zero);
            f.frame_bg.color = new Color(0.09f, 0.10f, 0.11f, 1f);
            f.art = Img("Art", f.art_mask.rectTransform, new Vector2(f.art_win_w, f.art_win_h), Vector2.zero);
            f.frame_border = Img("FrontFrame", f.root, new Vector2(618, 865), Vector2.zero);

            //the frame's own black circles carry cost (top) + aspect gem (below)
            f.cost_txt = Txt("Cost", f.root, new Vector2(120, 100), new Vector2(267 * K - 309f, TOP - 266 * K), 64);
            for (int i = 0; i < 3; i++)
                f.class_icon_layers[i] = Img("ClassIcon" + i, f.root, new Vector2(62, 62), new Vector2(232 * K - 309f, TOP - 522 * K));
            f.class_icon = f.class_icon_layers[0];

            f.title_txt = Tmp("Title", f.root, new Vector2(500, 52), new Vector2(0f, TOP - 1520 * K), 34, FontStyles.Bold);
            f.desc_txt = Tmp("Desc", f.root, new Vector2(470, 170), new Vector2(0f, TOP - 1880 * K), 22, FontStyles.Normal);
            f.type_txt = Txt("TypeLine", f.root, new Vector2(300, 34), new Vector2(0f, TOP - 2380 * K), 19);

            //bottom badges: AC shield BL, attack dagger next to it, life heart BR
            f.block_icon = Img("ArmorIcon", f.root, new Vector2(113, 113), new Vector2(220 * K - 309f, TOP - 2325 * K));
            f.block_txt = Txt("Armor", f.root, new Vector2(110, 90), new Vector2(220 * K - 309f, TOP - 2350 * K), 46);
            f.atk_icon = Img("AtkIcon", f.root, new Vector2(103, 103), new Vector2(535 * K - 309f, TOP - 2330 * K));
            f.attack_txt = Txt("Atk", f.root, new Vector2(110, 90), new Vector2(545 * K - 309f, TOP - 2355 * K), 46);
            f.hp_icon = Img("HpIcon", f.root, new Vector2(113, 113), new Vector2(1580 * K - 309f, TOP - 2325 * K));
            f.hp_txt = Txt("HP", f.root, new Vector2(110, 90), new Vector2(1580 * K - 309f, TOP - 2350 * K), 46);
            foreach (Text t in new Text[] { f.attack_txt, f.hp_txt, f.block_txt })
                t.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3, -3);

            f.show_cost = show_cost;
            if (!show_cost)
                f.cost_txt.gameObject.SetActive(false);
            return f;
        }

        /// <summary>TOKEN composite (phase-style landscape tile, 618x340 units).</summary>
        public static CCGCardFace CreateToken(Transform parent, float scale)
        {
            CCGCardFace f = new CCGCardFace();
            f.token_mode = true;
            f.show_cost = false;
            f.root = MakeRoot(parent, scale, new Vector2(TOKEN_W, TOKEN_H));
            f.art_win_w = 496f;
            f.art_win_h = 384f;

            //ROUNDED frame (user rule: tokens have round, not hard, edges):
            //9-sliced rounded-rect shapes, faction-colored border + dark fill
            Sprite round_shape = GetRoundShape();
            f.frame_bg = Img("Border", f.root, new Vector2(TOKEN_W, TOKEN_H), Vector2.zero); //color = faction (Apply)
            SetRounded(f.frame_bg, round_shape, 0.55f);
            Image fill = Img("Fill", f.root, new Vector2(TOKEN_W - 12f, TOKEN_H - 12f), Vector2.zero);
            fill.color = new Color(0.06f, 0.08f, 0.12f, 1f);
            SetRounded(fill, round_shape, 0.57f);

            //Rounded art window: art COVER-fits, clipped to the rounded shape
            GameObject win = new GameObject("ArtWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            RectTransform win_rt = win.GetComponent<RectTransform>();
            win_rt.SetParent(f.root, false);
            win_rt.sizeDelta = new Vector2(f.art_win_w, f.art_win_h);
            win_rt.anchoredPosition = new Vector2(0f, -18f);
            Image win_img = win.GetComponent<Image>();
            SetRounded(win_img, round_shape, 0.7f);
            win.GetComponent<Mask>().showMaskGraphic = false;
            f.art = Img("Art", win_rt, new Vector2(f.art_win_w, f.art_win_h), Vector2.zero);

            //Name bar (rounded, nested inside the frame radius)
            Image bar = Img("NameBar", f.root, new Vector2(488, 58), new Vector2(0f, 203f));
            bar.color = new Color(0.03f, 0.05f, 0.09f, 0.94f);
            SetRounded(bar, round_shape, 0.9f);
            f.title_txt = Tmp("Title", f.root, new Vector2(410, 56), new Vector2(-28f, 203f), 33, FontStyles.Bold);
            f.title_txt.alignment = TextAlignmentOptions.Left;
            //Single line, long names end with "..." like other card games
            f.title_txt.textWrappingMode = TextWrappingModes.NoWrap;
            f.title_txt.overflowMode = TextOverflowModes.Ellipsis;

            //Class symbol in the name bar's right end
            for (int i = 0; i < 3; i++)
                f.class_icon_layers[i] = Img("ClassIcon" + i, f.root, new Vector2(52, 52), new Vector2(212f, 200f));
            f.class_icon = f.class_icon_layers[0];

            //Stat gems: swords (attack) bottom-left, shield (HP/durability) bottom-right —
            //LARGE so they read at board distance (user rule)
            f.atk_icon = Img("AtkIcon", f.root, new Vector2(122, 122), new Vector2(-186f, -180f));
            f.hp_icon = Img("HpIcon", f.root, new Vector2(128, 128), new Vector2(186f, -180f));
            f.attack_txt = Txt("Atk", f.root, new Vector2(150, 110), new Vector2(-186f, -134f), 84);
            f.hp_txt = Txt("HP", f.root, new Vector2(150, 110), new Vector2(186f, -134f), 84);
            f.attack_txt.gameObject.AddComponent<Outline>().effectDistance = new Vector2(4, -4);
            f.hp_txt.gameObject.AddComponent<Outline>().effectDistance = new Vector2(4, -4);

            //One-line keyword strip along the bottom, between the gems (cool
            //grey-white — the old amber read as "yellow text" on tokens)
            f.keyword_txt = Txt("Keyword", f.root, new Vector2(220, 44), new Vector2(0f, -206f), 27);
            f.keyword_txt.color = new Color(0.82f, 0.88f, 0.95f);

            return f;
        }

        /// <summary>Apply a card's data to whichever layout was built. Null-safe:
        /// token mode only builds a subset of the elements.</summary>
        private static Sprite crows_dagger, crows_heart, crows_shield;

        /// <summary>CROWS pack-frame skin: frame by aspect (champion trait wins),
        /// dagger/heart/shield badges, aspect gem in the frame circle.</summary>
        private void ApplyCrows(TcgEngine.CardData data)
        {
            if (crows_dagger == null)
            {
                crows_dagger = Resources.Load<Sprite>("Icons/24");
                crows_heart = Resources.Load<Sprite>("Icons/52");
                crows_shield = Resources.Load<Sprite>("Icons/57");
            }
            string frame_name = data.HasTrait("champion") ? "CHAMPION" : (data.team != null ? data.team.id.ToUpper() : "UNIVERSAL");
            Sprite fr = Resources.Load<Sprite>("Frames/" + frame_name);
            if (fr == null) fr = Resources.Load<Sprite>("Frames/UNIVERSAL");
            if (fr != null)
            {
                frame_border.sprite = fr;
                frame_border.color = Color.white;
            }
            if (atk_icon != null) atk_icon.sprite = crows_dagger;
            if (hp_icon != null) hp_icon.sprite = crows_heart;
            if (block_icon != null)
            {
                block_icon.sprite = crows_shield;
                block_icon.color = Color.white;
                bool show = data.IsCharacter();
                block_icon.gameObject.SetActive(show);
                block_txt.gameObject.SetActive(show);
                block_txt.text = data.GetStat("armor").ToString();
                block_txt.color = Color.white;
            }
            if (data.team != null && data.team.icon != null)
                foreach (Image layer in class_icon_layers)
                    if (layer != null) layer.sprite = data.team.icon;
            if (frame_bg != null && data.team != null) //art backdrop takes a whisper of aspect color
                frame_bg.color = Color.Lerp(new Color(0.07f, 0.075f, 0.08f, 1f), data.team.color, 0.15f);
        }

        public void Apply(TcgEngine.CardData data)
        {
            if (!token_mode)
                ApplyCrows(data);
            CardScriptable style = token_mode ? GetStyle(data) : null;
            if (style != null)
            {
                foreach (UICardImage el in style.UIElementsImage)
                {
                    switch (el.label)
                    {
                        case LabelImageCards.FrontBackground:
                            if (!token_mode) Set(frame_bg, el);
                            break;
                        //(token frames are primitive rects now; SpriteImage unused)
                        case LabelImageCards.Mask:
                            if (art_mask != null) art_mask.sprite = el.sprite;
                            break;
                        case LabelImageCards.FrontBackgroundText: Set(bg_text, el); break;
                        case LabelImageCards.FrontFrame: Set(frame_border, el); break;
                        case LabelImageCards.FrontIconValue1:
                            Set(cost_icon, el);
                            if (pitch_icon != null) pitch_icon.sprite = el.sprite;
                            break;
                        //SWAPPED on purpose: megapack Value2 is a shield, Value3 is swords.
                        case LabelImageCards.FrontIconValue2: Set(hp_icon, el); break;
                        case LabelImageCards.FrontIconValue3: Set(atk_icon, el); break;
                        case LabelImageCards.FrontDecorative:
                            foreach (Image layer in class_icon_layers)
                                if (layer != null) layer.sprite = el.sprite;
                            break;
                        case LabelImageCards.FrontRarity1: if (rarity_slot == null && el.sprite != null && el.sprite.name.Contains("Slot")) rarity_slot = el.sprite; break;
                        case LabelImageCards.FrontRarity2: if (rarity_fill == null && el.sprite != null) rarity_fill = el.sprite; break;
                    }
                }
                foreach (UICardText el in style.UIElementsText)
                {
                    if (el.label == LabelTextCards.TxtTitle && title_txt != null && !token_mode) title_txt.color = el.color;
                    if (el.label == LabelTextCards.TxtDescription && desc_txt != null) desc_txt.color = el.color;
                    if (el.label == LabelTextCards.TxtValue1 && cost_txt != null) cost_txt.color = el.color;
                }
            }
            LoadRaritySprites();

            //Token border: bright faction color (primitive rect)
            if (token_mode && data.team != null)
                frame_bg.color = Color.Lerp(data.team.color, Color.white, 0.25f);

            if (cost_icon != null && (!show_cost || data.type == CardType.Hero))
            {
                cost_icon.gameObject.SetActive(false);
                cost_txt.gameObject.SetActive(false);
            }

            art.sprite = data.art_full;
            if (art.sprite != null)
            {
                //COVER-fit everywhere (aspect preserved, mask crops overflow) —
                //the old token STRETCH visibly squashed portrait art
                float sa = art.sprite.rect.width / art.sprite.rect.height;
                float aw, ah;
                //pack Artwork = transparent-ground figures (no "_v" in name): CONTAIN so
                //the whole figure stands on the backdrop; generated squares COVER-fill
                bool figure = !token_mode && !art.sprite.name.Contains("_v");
                if (figure)
                {
                    if (sa > art_win_w / art_win_h) { aw = art_win_w * 0.94f; ah = aw / sa; }
                    else { ah = art_win_h * 0.94f; aw = ah * sa; }
                }
                else
                {
                    if (sa > art_win_w / art_win_h)
                    {
                        ah = art_win_h;
                        aw = ah * sa;
                    }
                    else
                    {
                        aw = art_win_w;
                        ah = aw / sa;
                    }
                    if (!token_mode)
                    {
                        //generated pieces carry their own drawn borders: overscan so those
                        //edges slide UNDER the frame (mask clips them) instead of showing
                        aw *= 1.10f;
                        ah *= 1.10f;
                    }
                }
                art.rectTransform.sizeDelta = new Vector2(aw, ah);
                //Tokens crop portrait art in a landscape window: bias toward the
                //TOP of the painting so faces stay in frame
                if (token_mode)
                    art.rectTransform.anchoredPosition = new Vector2(0f, -(ah - art_win_h) * 0.3f);
            }

            if (title_txt != null) title_txt.text = data.title;
            if (desc_txt != null) desc_txt.text = data.GetText();
            if (cost_txt != null) cost_txt.text = data.mana.ToString();

            //Weapons are PERMANENT (no durability): attack + swing cost only.
            bool is_weapon = data.IsEquipment() && data.HasTrait("weapon");
            bool has_stats = data.IsCharacter() || data.IsEquipment();
            if (atk_icon != null)
            {
                atk_icon.gameObject.SetActive(has_stats && data.attack > 0 || data.IsCharacter());
                attack_txt.gameObject.SetActive(atk_icon.gameObject.activeSelf);
                attack_txt.text = data.attack.ToString();
            }
            if (hp_icon != null)
            {
                hp_icon.gameObject.SetActive(has_stats && !is_weapon);
                hp_txt.gameObject.SetActive(has_stats && !is_weapon);
                hp_txt.text = data.hp.ToString();
            }

            //Pitch badge (full cards in hand only; never equipment/heroes)
            if (pitch_icon != null)
            {
                int pitch = GetPitchValue(data);
                bool show_pitch = pitch > 0 && !data.IsEquipment() && data.type != CardType.Hero;
                pitch_icon.gameObject.SetActive(show_pitch && pitch_icon.sprite != null);
                pitch_txt.gameObject.SetActive(show_pitch);
                if (show_pitch)
                {
                    pitch_icon.color = GetPitchColor(pitch);
                    pitch_txt.text = pitch.ToString();
                }
            }

            //Class symbol (solid via stacked layers)
            bool has_class = class_icon != null && class_icon.sprite != null && data.team != null;
            foreach (Image layer in class_icon_layers)
            {
                if (layer == null) continue;
                layer.gameObject.SetActive(has_class);
                if (has_class) //full cards: pack gem icons are already colored — keep them white
                    layer.color = token_mode ? Color.Lerp(data.team.color, Color.white, 0.85f) : Color.white;
            }

            //Rarity strip (full cards only)
            if (rarity_icons[0] != null)
            {
                int rank = data.rarity != null ? Mathf.Clamp(data.rarity.rank, 0, 5) : 0;
                for (int i = 0; i < 5; i++)
                {
                    Sprite s = i < rank ? rarity_fill : rarity_slot;
                    rarity_icons[i].sprite = s;
                    rarity_icons[i].gameObject.SetActive(s != null);
                }
            }

            //Type line (full cards only)
            if (type_txt != null)
            {
                type_txt.text = BuildTypeLine(data);
                type_txt.gameObject.SetActive(!string.IsNullOrEmpty(type_txt.text));
            }

            //Token keyword strip RETIRED (user rule): ability text on the token
            //is redundant — the hover preview explains abilities
            if (keyword_txt != null)
                keyword_txt.gameObject.SetActive(false);

            //BLOCK value (defense window): shown on full faces for blockable cards
            if (block_icon != null)
            {
                int block = CCGGameLogic.GetBlock(data);
                block_icon.gameObject.SetActive(block > 0);
                if (block_txt != null)
                {
                    block_txt.gameObject.SetActive(block > 0);
                    block_txt.text = block.ToString();
                }
            }
        }

        public void SetStats(int attack, int hp)
        {
            if (attack_txt != null) attack_txt.text = attack.ToString();
            if (hp_txt != null) hp_txt.text = hp.ToString();
        }

        /// <summary>Scale the stat badges — counters perspective shrink on the enemy side.</summary>
        public void SetStatScale(float s)
        {
            Vector3 scale = Vector3.one * s;
            if (atk_icon != null) atk_icon.rectTransform.localScale = scale;
            if (hp_icon != null) hp_icon.rectTransform.localScale = scale;
            if (attack_txt != null) attack_txt.rectTransform.localScale = scale;
            if (hp_txt != null) hp_txt.rectTransform.localScale = scale;
            if (cost_icon != null) cost_icon.rectTransform.localScale = scale;
            if (cost_txt != null) cost_txt.rectTransform.localScale = scale;
            if (pitch_icon != null) pitch_icon.rectTransform.localScale = scale;
            if (pitch_txt != null) pitch_txt.rectTransform.localScale = scale;
        }

        private static RectTransform MakeRoot(Transform parent, float scale, Vector2 size)
        {
            GameObject root_go = new GameObject("CardFace", typeof(RectTransform));
            RectTransform rt = root_go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one * scale;
            rt.sizeDelta = size;
            return rt;
        }

        private static void LoadRaritySprites()
        {
            if (rarity_fill != null && rarity_slot != null)
                return;
            foreach (string sn in new string[] { "ccg_style_03", "ccg_style_05" })
            {
                CardScriptable s = Resources.Load<CardScriptable>("CardStyles/" + sn);
                if (s == null) continue;
                foreach (UICardImage el in s.UIElementsImage)
                {
                    if (el.sprite == null) continue;
                    if (el.sprite.name.Contains("Rarity_Fill") && rarity_fill == null) rarity_fill = el.sprite;
                    if (el.sprite.name.Contains("Rarity_Slot") && rarity_slot == null) rarity_slot = el.sprite;
                }
            }
        }

        //FaB-style type banner: "SANGUINE | MINION", "BULWARK | WEAPON"...
        public static string BuildTypeLine(TcgEngine.CardData data)
        {
            string faction = data.team != null ? data.team.title.ToUpper() : "";
            string type;
            switch (data.type)
            {
                case CardType.Hero: type = "HERO"; break;
                case CardType.Character: type = "MINION"; break;
                case CardType.Spell: type = "SPELL"; break;
                case CardType.Artifact: type = data.HasTrait("field") ? "FIELD" : "ARTIFACT"; break;
                case CardType.Equipment:
                    type = "EQUIPMENT";
                    foreach (string slot in new string[] { "weapon", "head", "chest", "arms", "legs" })
                        if (data.HasTrait(slot)) { type = slot.ToUpper(); break; }
                    break;
                default: type = data.type.ToString().ToUpper(); break;
            }
            return string.IsNullOrEmpty(faction) ? type : faction + " | " + type;
        }

        public static int GetPitchValue(TcgEngine.CardData data)
        {
            if (data.stats == null) return 0;
            foreach (TraitStat s in data.stats)
                if (s.trait != null && s.trait.id == "pitch")
                    return s.value;
            return 0;
        }

        public static Color GetPitchColor(int pitch)
        {
            if (pitch >= 3) return new Color(0.25f, 0.55f, 0.95f);
            if (pitch == 2) return new Color(0.95f, 0.8f, 0.2f);
            return new Color(0.88f, 0.25f, 0.2f);
        }

        private static Sprite round_shape_cache;

        /// <summary>Rounded-rect 9-slice used by all board-token shapes.</summary>
        public static Sprite GetRoundShape()
        {
            if (round_shape_cache == null)
                round_shape_cache = Resources.Load<Sprite>("GUI/gui_semi");
            return round_shape_cache;
        }

        /// <summary>Make an Image a rounded 9-slice; ppu_mult scales the corner
        /// radius (smaller = rounder).</summary>
        public static void SetRounded(Image img, Sprite shape, float ppu_mult)
        {
            if (img == null || shape == null)
                return;
            img.sprite = shape;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ppu_mult;
        }

        /// <summary>In-match card BACKS are rarity/cosmetic driven, not faction:
        /// the DEFAULT back for both players is the RED style (user rule).</summary>
        public static CardScriptable GetBackStyle()
        {
            return Resources.Load<CardScriptable>("CardStyles/ccg_style_01");
        }

        public static CardScriptable GetStyle(TcgEngine.CardData data)
        {
            string style_name = "ccg_style_02";
            if (data != null && data.team != null)
            {
                if (data.team.id == "sanguine") style_name = "ccg_style_01";
                else if (data.team.id == "bulwark") style_name = "ccg_style_03";
            }
            return Resources.Load<CardScriptable>("CardStyles/" + style_name);
        }

        private static void Set(Image img, UICardImage el)
        {
            if (img == null) return;
            img.sprite = el.sprite;
            img.color = el.color;
            img.gameObject.SetActive(el.sprite != null);
        }

        private static Image Img(string name, Transform parent, Vector2 size, Vector2 pos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI Tmp(string name, Transform parent, Vector2 size, Vector2 pos, float fsize, FontStyles fstyle)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            //TMP DEFAULT font on purpose: both Rajdhani SDF routes (copied
            //assets, runtime-generated) rendered broken/invisible card text.
            //Legacy Text elements keep the Rajdhani ttf.
            TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = fsize;
            t.fontStyle = fstyle;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        private static Text Txt(string name, Transform parent, Vector2 size, Vector2 pos, int fsize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            Text t = go.GetComponent<Text>();
            var raj = Resources.Load<Font>("Fonts/Rajdhani-Bold");
            t.font = raj != null ? raj : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fsize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            go.AddComponent<Shadow>().effectDistance = new Vector2(3, -3);
            return t;
        }
    }
}
