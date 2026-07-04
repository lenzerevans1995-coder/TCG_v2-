using UnityEngine;
using UnityEngine.UI;
using CardUtilities;

namespace CCG
{
    /// <summary>
    /// EDIT-MODE preview of the board template: renders the same dim card-frame
    /// silhouettes the game shows at runtime, at every CCG_BoardLayout anchor
    /// (player side Sanguine-red dress, enemy side Bulwark-blue), so the Scene
    /// view looks like the empty running board. Preview objects are never saved
    /// and are removed in play mode (the runtime display takes over).
    /// </summary>
    [ExecuteAlways]
    public class CCGBoardLayoutPreview : MonoBehaviour
    {
        public float card_scale = 0.22f;

        private Transform preview_root;
        private string built_hash = "";

        void Update()
        {
            if (Application.isPlaying)
            {
                if (preview_root != null)
                    DestroyImmediate(preview_root.gameObject);
                return;
            }

            //Rebuild when anchors move
            System.Text.StringBuilder hb = new System.Text.StringBuilder();
            foreach (Transform side in transform)
                foreach (Transform a in side)
                    hb.Append(a.position.x.ToString("F2")).Append(',').Append(a.position.z.ToString("F2")).Append(';');
            string hash = hb.ToString();
            if (hash == built_hash && preview_root != null)
                return;
            built_hash = hash;

            if (preview_root != null)
                DestroyImmediate(preview_root.gameObject);
            preview_root = new GameObject("CCG_LayoutPreview").transform;
            preview_root.gameObject.hideFlags = HideFlags.HideAndDontSave;

            foreach (Transform side in transform)
            {
                bool enemy = side.name.StartsWith("Enemy");
                CardScriptable style = Resources.Load<CardScriptable>("CardStyles/" + (enemy ? "ccg_style_03" : "ccg_style_01"));
                foreach (Transform a in side)
                    MakeFrame(style, a.position, a.name.ToUpper());
            }
        }

        void OnDisable()
        {
            if (preview_root != null)
                DestroyImmediate(preview_root.gameObject);
        }

        private void MakeFrame(CardScriptable style, Vector3 pos, string label)
        {
            GameObject go = new GameObject("Preview_" + label, typeof(RectTransform), typeof(Canvas));
            go.hideFlags = HideFlags.HideAndDontSave;
            Canvas cv = go.GetComponent<Canvas>();
            cv.renderMode = RenderMode.WorldSpace;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(preview_root, false);
            rt.sizeDelta = new Vector2(200, 200);
            rt.localScale = new Vector3(0.01f, 0.01f, 1f);
            rt.localPosition = pos + new Vector3(0f, 0.03f, 0f);
            rt.localRotation = Quaternion.Euler(90f, 0f, 0f);

            GameObject fgo = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform frt = fgo.GetComponent<RectTransform>();
            frt.SetParent(go.transform, false);
            frt.sizeDelta = new Vector2(618, 922);
            frt.localScale = Vector3.one * card_scale;
            Image img = fgo.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.16f);
            img.raycastTarget = false;
            if (style != null)
            {
                foreach (UICardImage el in style.UIElementsImage)
                    if (el.label == LabelImageCards.FrontBackground) { img.sprite = el.sprite; break; }
            }

            GameObject tgo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform trt = tgo.GetComponent<RectTransform>();
            trt.SetParent(frt, false);
            trt.sizeDelta = new Vector2(560, 100);
            trt.anchoredPosition = Vector2.zero;
            Text t = tgo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 64;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(1f, 1f, 1f, 0.5f);
            t.text = label;
            t.raycastTarget = false;
        }
    }
}
