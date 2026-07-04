using UnityEngine;
using UnityEngine.UI;
using CardUtilities;

namespace CCG
{
    /// <summary>
    /// Renders the megapack card BACK composite (BackBackground, BackFrame,
    /// BackSymbol, BackSymbol2 from a ccg_style) scaled to fit this element's rect.
    /// Attach to CCG_HandCardBack root; hides the kit's flat back sprite.
    /// </summary>
    public class CCGCardBack : MonoBehaviour
    {
        public string style_name = ""; //empty = auto: the OPPONENT's faction style

        void Start()
        {
            //Keep the kit Sprite for raycast but make it invisible
            Transform sprite = transform.Find("Sprite");
            if (sprite != null)
            {
                Image simg = sprite.GetComponent<Image>();
                if (simg != null) simg.color = new Color(1, 1, 1, 0);
            }

            //Backs are cosmetic, not faction: default RED for everyone (user rule)
            CardScriptable style = string.IsNullOrEmpty(style_name)
                ? CCGCardFace.GetBackStyle()
                : Resources.Load<CardScriptable>("CardStyles/" + style_name);
            if (style == null)
                return;

            RectTransform rt = GetComponent<RectTransform>();
            //0.62: the kit's original back sprite had large transparent padding, so a
            //full-rect composite reads oversized; shrink to match the kit's visual weight
            float scale = ((rt != null && rt.rect.height > 1f) ? rt.rect.height / 922f : 0.35f) * 0.62f;

            GameObject root = new GameObject("CCG_Back", typeof(RectTransform));
            RectTransform face = root.GetComponent<RectTransform>();
            face.SetParent(transform, false);
            face.SetSiblingIndex(0);
            face.localScale = Vector3.one * scale;
            face.sizeDelta = new Vector2(618, 922);

            foreach (UICardImage el in style.UIElementsImage)
            {
                Vector2 size = Vector2.zero; Vector2 pos = Vector2.zero; bool use = false;
                switch (el.label)
                {
                    case LabelImageCards.BackBackground: size = new Vector2(618, 922); use = true; break;
                    case LabelImageCards.BackFrame: size = new Vector2(512, 816); use = true; break;
                    case LabelImageCards.BackSymbol: size = new Vector2(369.64f, 369.64f); pos = new Vector2(0, 0); use = true; break;
                    case LabelImageCards.BackSymbol2: size = new Vector2(369.64f, 369.64f); pos = new Vector2(0, 27); use = true; break;
                }
                if (!use || el.sprite == null)
                    continue;

                GameObject go = new GameObject(el.label.ToString(), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform ert = go.GetComponent<RectTransform>();
                ert.SetParent(face, false);
                ert.sizeDelta = size;
                ert.anchoredPosition = pos;
                Image img = go.GetComponent<Image>();
                img.sprite = el.sprite;
                img.color = el.color;
                img.raycastTarget = false;
            }
        }
    }
}
