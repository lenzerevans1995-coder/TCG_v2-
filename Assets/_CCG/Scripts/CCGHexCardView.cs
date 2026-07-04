using UnityEngine;
using UnityEngine.UI;
using TcgEngine;
using TcgEngine.Client;
using CardUtilities;

namespace CCG
{
    /// <summary>
    /// HEX-style full-card view for board and hand cards. Attach to the CCG_BoardCard /
    /// CCG_HandCard prefab roots (next to the kit's BoardCard/HandCard). Hides the kit's
    /// compact token visuals and hosts the UNIVERSAL card face (CCGCardFace) plus a
    /// two-layer card-shaped outline glow used for hover/targeting feedback.
    /// </summary>
    public class CCGHexCardView : MonoBehaviour
    {
        [Tooltip("Uniform scale of the card face inside CanvasUI (board cards). 0 = auto-fit host rect (hand cards)")]
        public float face_scale = 0.22f;

        private BoardCard board_card;
        private HandCard hand_card;
        private CCGCardFace face;
        private RectTransform face_host;
        private CanvasGroup face_group;
        private string built_card_id;
        private Color frame_border_color = Color.white;
        private Image outline_img;
        private Image outline_img_soft;

        /// <summary>Glow behind the face; alpha 0 hides it. Tokens are primitive
        /// rects so a sprite-less rect glow matches their shape exactly; full
        /// (hand) cards get the frame silhouette sprite in ApplyCard.</summary>
        public void SetOutline(float alpha, Color color)
        {
            float a = Mathf.Clamp01(alpha);
            if (outline_img != null)
                outline_img.color = new Color(color.r, color.g, color.b, a);
            if (outline_img_soft != null)
                outline_img_soft.color = new Color(color.r, color.g, color.b, a * 0.55f);
        }

        void Start()
        {
            board_card = GetComponent<BoardCard>();
            hand_card = GetComponent<HandCard>();
            BuildFace();
            HideKitToken();
        }

        private Card GetHostCard()
        {
            if (board_card != null) return board_card.GetCard();
            if (hand_card != null) return hand_card.GetCard();
            return null;
        }

        void LateUpdate()
        {
            if (face == null)
                return;

            //Kit CardUI repaints the hand Sprite/Glow every refresh; keep both invisible each frame
            if (hand_card != null)
            {
                Transform sprite = transform.Find("Sprite");
                if (sprite != null)
                {
                    Image simg = sprite.GetComponent<Image>();
                    if (simg != null && simg.color.a > 0f)
                        simg.color = new Color(1, 1, 1, 0);
                }
                Transform glow = transform.Find("Glow");
                if (glow != null && glow.gameObject.activeSelf)
                    glow.gameObject.SetActive(false); //grey hover square replaced by frame brighten

                //Hover feedback: card-shaped glow + brightened frame border
                bool focused = HandCard.GetFocus() == hand_card;
                if (outline_img != null)
                {
                    float target_a = focused ? 1f : 0f;
                    float a = Mathf.MoveTowards(outline_img.color.a, target_a, 6f * Time.deltaTime);
                    SetOutline(a, new Color(0.3f, 1f, 0.9f));
                }
                if (face.frame_border != null)
                {
                    Color target = focused ? new Color(0.5f, 1f, 0.9f, 1f) : frame_border_color;
                    face.frame_border.color = Color.Lerp(face.frame_border.color, target, 8f * Time.deltaTime);
                }

                //Focused card renders in FRONT of its neighbors
                if (focused && transform.GetSiblingIndex() != transform.parent.childCount - 1)
                    transform.SetAsLastSibling();

                //Slight tilt following the cursor over the card
                if (face_host != null)
                {
                    Quaternion target_rot = Quaternion.identity;
                    if (focused && UnityEngine.InputSystem.Mouse.current != null)
                    {
                        RectTransform rt = transform as RectTransform;
                        Vector2 local;
                        Vector2 mouse = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                        if (rt != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, mouse, Camera.main, out local))
                        {
                            float nx = Mathf.Clamp(local.x / (rt.rect.width * 0.5f), -1f, 1f);
                            float ny = Mathf.Clamp(local.y / (rt.rect.height * 0.5f), -1f, 1f);
                            target_rot = Quaternion.Euler(-ny * 6f, nx * 8f, 0f);
                        }
                    }
                    face_host.localRotation = Quaternion.Slerp(face_host.localRotation, target_rot, 10f * Time.deltaTime);
                }
            }

            Card card = GetHostCard();
            if (card == null)
                return;

            if (card.card_id != built_card_id)
                ApplyCard(card);

            //TAP indication (like Magic): exhausted board cards — just played
            //(summoning sickness) or already attacked — rotate sideways AND
            //grey out (user rule)
            if (board_card != null && face_host != null)
            {
                Quaternion tap = Quaternion.Euler(0f, 0f, card.exhausted ? -90f : 0f);
                face_host.localRotation = Quaternion.Slerp(face_host.localRotation, tap, 8f * Time.deltaTime);
                if (face_group == null)
                    face_group = face_host.gameObject.AddComponent<CanvasGroup>();
                float target_a = card.exhausted ? 0.55f : 1f;
                face_group.alpha = Mathf.MoveTowards(face_group.alpha, target_a, 3f * Time.deltaTime);
            }

            //Live stats with readability tints: damaged HP red, buffed stats green
            face.SetStats(card.GetAttack(), card.GetHP());
            if (face.hp_txt != null)
                face.hp_txt.color = card.damage > 0 ? new Color(1f, 0.35f, 0.3f) :
                    (card.GetHP() > card.CardData.hp ? new Color(0.45f, 1f, 0.45f) : Color.white);
            if (face.attack_txt != null)
                face.attack_txt.color = card.GetAttack() > card.CardData.attack ? new Color(0.45f, 1f, 0.45f) : Color.white;
        }

        private void HideKitToken()
        {
            if (board_card != null)
            {
                Transform card_sprite = transform.Find("Card");
                if (card_sprite != null) card_sprite.gameObject.SetActive(false);

                //Kit's black drop-shadow reads as a dark table cutout; the flat
                //card-slot frames (CCGEquipmentDisplay) replace that look
                Transform shadow = transform.Find("Shadow");
                if (shadow != null) shadow.gameObject.SetActive(false);

                Transform cui = transform.Find("CanvasUI");
                if (cui != null)
                {
                    //Equipment overlay hidden entirely: equipment never attaches to minions
                    //in CCG rules (it lives on the hero equipment row, Phase B.2).
                    //StatusPanel too — the ability tooltip under hovered tokens is
                    //redundant with the docked hover preview (user rule)
                    foreach (string n in new string[] { "Frame", "TeamIcon", "AttackIcon", "HPIcon", "ArmorIcon", "Equipment", "StatusPanel" })
                    {
                        Transform t = cui.Find(n);
                        if (t != null) t.gameObject.SetActive(false);
                    }
                }
            }

            if (hand_card != null)
            {
                //Keep "Sprite" active (its Image is the hover/drag raycast target) but invisible
                Transform sprite = transform.Find("Sprite");
                if (sprite != null)
                {
                    Image simg = sprite.GetComponent<Image>();
                    if (simg != null) simg.color = new Color(1, 1, 1, 0);
                }
                foreach (string n in new string[] { "Gradient 2", "Frame", "CostIcon", "AttackIcon", "HPIcon", "Cost", "Attack", "HP", "TeamIcon", "RarityIcon", "CardTitle", "CardText" })
                {
                    Transform t = transform.Find(n);
                    if (t != null) t.gameObject.SetActive(false);
                }
            }
        }

        private void BuildFace()
        {
            Transform parent = null;
            float scale = face_scale;

            if (board_card != null)
            {
                parent = transform.Find("CanvasUI");
            }
            else if (hand_card != null)
            {
                parent = transform;
                RectTransform rt = GetComponent<RectTransform>();
                if (rt != null && rt.rect.height > 1f)
                    scale = rt.rect.height / 922f;
            }
            if (parent == null)
                return;

            GameObject host = new GameObject("CCG_Face", typeof(RectTransform));
            RectTransform host_rt = host.GetComponent<RectTransform>();
            face_host = host_rt;
            host_rt.SetParent(parent, false);
            if (board_card != null)
                host_rt.SetAsFirstSibling();
            else
                host_rt.SetSiblingIndex(1); //above Glow, below interaction elements

            //Board cards are compact TOKENS (phase-style); hand cards are FULL faces
            float w = board_card != null ? CCGCardFace.TOKEN_W : 618f;
            float h = board_card != null ? CCGCardFace.TOKEN_H : 922f;

            //Outline: enlarged tinted copies of the silhouette, behind everything.
            outline_img_soft = MakeOutline("OutlineGlowSoft", host_rt, new Vector2(w * 1.16f, h * 1.18f), scale);
            outline_img = MakeOutline("OutlineGlow", host_rt, new Vector2(w * 1.09f, h * 1.1f), scale);

            face = board_card != null ? CCGCardFace.CreateToken(host_rt, scale) : CCGCardFace.Create(host_rt, scale, true);
        }

        private Image MakeOutline(string name, Transform parent, Vector2 size, float scale)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.localScale = Vector3.one * scale;
            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }

        private void ApplyCard(Card card)
        {
            built_card_id = card.card_id;
            TcgEngine.CardData data = card.CardData;
            face.Apply(data);

            //(top-down camera: both sides render at equal size, no stat boost needed)

            //Outline silhouette + resting frame color come from the applied style
            if (face.frame_bg != null && face.frame_bg.sprite != null)
            {
                bool sliced = face.frame_bg.sprite.border.sqrMagnitude > 0f;
                if (outline_img != null) { outline_img.sprite = face.frame_bg.sprite; if (sliced) outline_img.type = Image.Type.Sliced; }
                if (outline_img_soft != null) { outline_img_soft.sprite = face.frame_bg.sprite; if (sliced) outline_img_soft.type = Image.Type.Sliced; }
            }
            if (face.frame_border != null)
                frame_border_color = face.frame_border.color;
        }
    }
}
