using UnityEngine;
using TMPro;
using TcgEngine;
using TcgEngine.Client;

namespace CCG
{
    /// <summary>
    /// CROWS battlefield token: dresses the kit BoardCard (world-space
    /// SpriteRenderers) with the pack square TOKEN_FRAME and corner stats —
    /// TL attack dagger, TR aspect gem, BL AC shield, BR life heart.
    /// Sizes are absolute world units fitted to the 2.3-unit slot spacing;
    /// the kit's own card sprite is rescaled to sit inside the frame window
    /// and its CanvasUI stat chips are suppressed (ours replace them).
    /// Attached by CCGSkinBootstrap to every BoardCard.
    /// </summary>
    public class CrowsBoardToken : MonoBehaviour
    {
        const float TOKEN_W = 2.05f;                 //frame width in world units (slot spacing 2.3)
        const float WINDOW_RATIO = 1577f / 1343f;    //frame width vs art window width
        const float FRAME_H_RATIO = 1563f / 1577f;   //frame height vs width

        private BoardCard bc;
        private SpriteRenderer art;
        private SpriteRenderer frame;
        private SpriteRenderer icon_atk, icon_gem, icon_ac, icon_hp;
        private TextMeshPro txt_atk, txt_ac, txt_hp;
        private bool built;

        private static Sprite s_frame, s_dagger, s_heart, s_shield;

        void Awake()
        {
            bc = GetComponent<BoardCard>();
        }

        void LateUpdate()
        {
            if (bc == null) return;
            Card card = bc.GetCard();
            if (card == null) return;
            if (!built) Build();
            if (!built) return;
            Sync(card);
        }

        private void Build()
        {
            art = bc.card_sprite;
            if (art == null || art.sprite == null) return;
            if (s_frame == null)
            {
                s_frame = Resources.Load<Sprite>("Frames/TOKEN_FRAME");
                s_dagger = Resources.Load<Sprite>("Icons/24");
                s_heart = Resources.Load<Sprite>("Icons/52");
                s_shield = Resources.Load<Sprite>("Icons/57");
            }
            if (s_frame == null) return;

            float w = TOKEN_W * 0.5f;
            float h = TOKEN_W * FRAME_H_RATIO * 0.5f;
            float inset = TOKEN_W * 0.11f;
            float icon_w = TOKEN_W * 0.17f;

            frame = MakeSprite("CrowsFrame", s_frame, TOKEN_W, Vector3.zero);
            icon_atk = MakeSprite("CrowsAtk", s_dagger, icon_w, new Vector3(-w + inset, h - inset, 0f));
            icon_gem = MakeSprite("CrowsGem", null, icon_w * 0.8f, new Vector3(w - inset, h - inset * 0.95f, 0f));
            icon_ac = MakeSprite("CrowsAC", s_shield, icon_w, new Vector3(-w + inset, -h + inset, 0f));
            icon_hp = MakeSprite("CrowsHP", s_heart, icon_w, new Vector3(w - inset, -h + inset, 0f));

            txt_atk = MakeText("CrowsAtkTxt", new Vector3(-w + inset * 1.1f, h - inset * 1.15f, 0f));
            txt_ac = MakeText("CrowsACTxt", new Vector3(-w + inset, -h + inset * 0.92f, 0f));
            txt_hp = MakeText("CrowsHPTxt", new Vector3(w - inset, -h + inset * 0.92f, 0f));

            TcgEngine.CardData data = bc.GetCardData();
            if (data != null && data.team != null && data.team.icon != null)
            {
                icon_gem.sprite = data.team.icon;
                float gs = (icon_w * 0.8f) / icon_gem.sprite.bounds.size.x;
                icon_gem.transform.localScale = new Vector3(gs, gs, 1f);
            }

            //our corner stats replace the kit's CanvasUI chips
            foreach (Canvas cv in GetComponentsInChildren<Canvas>(true))
            {
                if (cv.gameObject.name != "CanvasUI") continue;
                foreach (UnityEngine.UI.Graphic gr in cv.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                    gr.enabled = false;
            }

            built = true;
        }

        private SpriteRenderer MakeSprite(string name, Sprite sprite, float width, Vector3 lpos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = lpos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (sprite != null)
            {
                float s = width / sprite.bounds.size.x;
                go.transform.localScale = new Vector3(s, s, 1f);
            }
            sr.sortingLayerID = art != null ? art.sortingLayerID : 0;
            return sr;
        }

        private TextMeshPro MakeText(string name, Vector3 lpos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = lpos;
            go.transform.localScale = Vector3.one * 0.1f;
            TextMeshPro t = go.AddComponent<TextMeshPro>();
            t.fontSize = 24f;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;
            t.outlineWidth = 0.3f;
            t.outlineColor = new Color32(10, 8, 12, 255);
            t.rectTransform.sizeDelta = new Vector2(10f, 5f);
            t.fontStyle = FontStyles.Bold;
            return t;
        }

        private void Sync(Card card)
        {
            //fit the kit art (and its shadow/glow) inside the frame window
            float artTarget = (TOKEN_W / WINDOW_RATIO) * 1.03f;
            float cur = art.bounds.size.x;
            if (cur > 0.01f && Mathf.Abs(cur - artTarget) / artTarget > 0.02f)
            {
                float k = artTarget / cur;
                art.transform.localScale *= k;
                if (bc.card_shadow != null) bc.card_shadow.transform.localScale *= k;
                if (bc.card_glow != null) bc.card_glow.transform.localScale *= k;
            }

            //keep draw order glued to the kit's dynamic sorting
            int baseOrder = art.sortingOrder;
            frame.sortingOrder = baseOrder + 1;
            foreach (SpriteRenderer sr in new[] { icon_atk, icon_gem, icon_ac, icon_hp })
                if (sr != null) sr.sortingOrder = baseOrder + 2;
            foreach (TextMeshPro t in new[] { txt_atk, txt_ac, txt_hp })
                if (t != null && t.renderer != null) t.renderer.sortingOrder = baseOrder + 3;

            txt_atk.text = card.GetAttack().ToString();
            txt_hp.text = card.GetHP().ToString();
            txt_ac.text = card.GetTraitValue("armor").ToString();
        }
    }
}
