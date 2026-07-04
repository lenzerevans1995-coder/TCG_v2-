using UnityEngine;
using UnityEngine.UI;
using TcgEngine;

namespace CCG
{
    /// <summary>
    /// Hover/click surface for the loadout/zone world cards (hero, weapon, armor,
    /// discard top). Input is PHYSICS-based (BoxCollider + camera raycast driven by
    /// CCGHeroAttackControls) — no UI event system dependency. Provides the
    /// card-shaped outline glow and feeds the kit CardPreviewUI via GetHoverCard.
    /// </summary>
    public class CCGZoneCardHover : MonoBehaviour
    {
        private static Card hover_card;
        public static Card GetHoverCard() { return hover_card; }
        public static void SetHoverInstance(CCGZoneCardHover h)
        {
            hover_card = h != null ? h.card : null;
        }

        private Card card;
        private CCGCardFace face;
        private Image outline_img;
        private Image outline_img_soft;

        public Card GetCard() { return card; }
        public bool IsHovered() { return hover_card != null && card != null && hover_card.uid == card.uid; }

        public void Setup(Card live_card, CCGCardFace card_face, Canvas canvas)
        {
            card = live_card;
            face = card_face;

            //Two-layer card-shaped glow behind the face (silhouette = tile sprite)
            Vector2 dims = face.root.sizeDelta;
            outline_img_soft = MakeOutline("OutlineGlowSoft", new Vector2(dims.x * 1.16f, dims.y * 1.18f));
            outline_img = MakeOutline("OutlineGlow", new Vector2(dims.x * 1.09f, dims.y * 1.1f));
        }

        private Image MakeOutline(string name, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(face.root, false);
            rt.sizeDelta = size;
            rt.SetAsFirstSibling();
            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.sprite = face.frame_bg.sprite;
            if (img.sprite != null && img.sprite.border.sqrMagnitude > 0f)
                img.type = Image.Type.Sliced;
            img.color = new Color(0.3f, 1f, 0.9f, 0f);
            return img;
        }

        void LateUpdate()
        {
            if (outline_img == null)
                return;
            float target = IsHovered() ? 1f : 0f;
            //Armed hero pulses
            if (CCGHeroAttackControls.IsArmed(card))
                target = 0.65f + Mathf.PingPong(Time.time * 1.4f, 0.35f);
            float a = Mathf.MoveTowards(outline_img.color.a, target, 6f * Time.deltaTime);
            outline_img.color = new Color(0.3f, 1f, 0.9f, a);
            outline_img_soft.color = new Color(0.3f, 1f, 0.9f, a * 0.55f);
        }

        void OnDestroy()
        {
            if (hover_card != null && card != null && hover_card.uid == card.uid)
                hover_card = null;
        }
    }
}
