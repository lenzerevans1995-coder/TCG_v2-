using UnityEngine;
using UnityEngine.UI;

namespace CCG
{
    /// <summary>
    /// Measures how far panel content must stay below the Heat nav header —
    /// the HOME/ADVENTURE/... bar is a strict header; nothing renders under it.
    /// </summary>
    public static class CCGNavUtil
    {
        public static float Clearance(RectTransform panel, float margin = 14f)
        {
            GameObject canvas = GameObject.Find("Canvas - Main Menu");
            if (canvas == null || panel == null)
                return 112f + margin;
            RectTransform nav = canvas.transform.Find("Main Content/Top Panel") as RectTransform;
            if (nav == null)
                return 112f + margin;

            Canvas.ForceUpdateCanvases();
            Vector3[] corners = new Vector3[4];
            nav.GetWorldCorners(corners); //corners[0] = bottom-left
            Vector3 local = panel.InverseTransformPoint(corners[0]);
            float clearance = panel.rect.yMax - local.y + margin;
            return Mathf.Clamp(clearance, 60f, 400f);
        }
    }

    /// <summary>
    /// Sizes this Image to COVER its parent rect while preserving the sprite's
    /// aspect (excess is cropped by the parent's mask) — CSS background-size:
    /// cover for UGUI. Used on Heat BoxButton background images so key art
    /// fills the tile without stretching.
    /// </summary>
    [ExecuteAlways]
    public class CCGCoverFit : MonoBehaviour
    {
        [Tooltip("Vertical crop anchor: 0.5 = centered, 1 = keep the TOP of the image (faces in portrait art)")]
        public float top_bias = 0.5f;

        private Image img;
        private RectTransform rt;

        void Awake()
        {
            img = GetComponent<Image>();
            rt = transform as RectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            if (img != null)
                img.preserveAspect = false;
        }

        void LateUpdate()
        {
            if (img == null || img.sprite == null)
                return;
            RectTransform parent = rt.parent as RectTransform;
            if (parent == null)
                return;

            float pw = parent.rect.width;
            float ph = parent.rect.height;
            if (pw < 1f || ph < 1f)
                return;

            float aspect = img.sprite.rect.width / img.sprite.rect.height;
            float w = pw;
            float h = w / aspect;
            if (h < ph)
            {
                h = ph;
                w = h * aspect;
            }
            Vector2 want = new Vector2(w, h);
            if ((rt.sizeDelta - want).sqrMagnitude > 1f)
                rt.sizeDelta = want;

            //vertical crop anchor: shift the oversized image down so its TOP
            //edge stays in frame when top_bias = 1 (0.5 = centered)
            float over_y = Mathf.Max(0f, h - ph);
            Vector2 want_pos = new Vector2(0f, (0.5f - top_bias) * over_y);
            if ((rt.anchoredPosition - want_pos).sqrMagnitude > 0.5f)
                rt.anchoredPosition = want_pos;
        }
    }
}
