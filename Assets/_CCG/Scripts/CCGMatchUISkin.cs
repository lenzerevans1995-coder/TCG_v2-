using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CCG
{
    /// <summary>
    /// In-match UI reskin (user rule: no kit button visuals anywhere): END TURN,
    /// mulligan OK, pause RESUME/RESIGN, game-over CONTINUE, selector popups,
    /// close X's and arrows — all restyled to the CCG neon language (dark round
    /// fills + cyan stroke glow) using the staged Resources/GUI shapes.
    /// </summary>
    public class CCGMatchUISkin : MonoBehaviour
    {
        private static readonly Color FILL = new Color(0.063f, 0.094f, 0.149f, 0.97f);
        private static readonly Color ACCENT = new Color(0.208f, 0.835f, 0.949f);
        private static readonly Color NEGATIVE = new Color(0.898f, 0.282f, 0.302f);
        private static readonly Color TEXTC = new Color(0.949f, 0.961f, 0.976f);

        private float timer = 0f;
        private readonly HashSet<Button> done = new HashSet<Button>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            GameObject go = new GameObject("CCG_MatchUISkin");
            DontDestroyOnLoad(go);
            go.AddComponent<CCGMatchUISkin>();
        }

        void Update()
        {
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!scene.Contains("Game"))
            {
                done.Clear();
                fonted.Clear();
                selector_done = false;
                return;
            }
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f)
                return;
            timer = 0.5f;

            foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (done.Contains(b))
                    continue;
                done.Add(b);
                Skin(b);
            }

            SkinSelector();
            FontPass();
        }

        //(HUD chips retired — CCGMatchHUD draws the docked cluster/pile/turn
        //panes and hides the kit's TimerArea/PlayerUI groups.)

        //---- deck/discard browser (CardSelector): Shift-theme chrome ----
        private bool selector_done = false;

        private void SkinSelector()
        {
            if (selector_done)
                return;
            TcgEngine.UI.CardSelector sel = Object.FindFirstObjectByType<TcgEngine.UI.CardSelector>(FindObjectsInactive.Include);
            if (sel == null)
                return;
            selector_done = true;

            Transform scroll = sel.transform.Find("ScrollArea");
            if (scroll != null)
            {
                Image bg = scroll.GetComponent<Image>();
                if (bg != null)
                {
                    bg.sprite = S("gui_cut_fill");
                    bg.type = Image.Type.Sliced;
                    bg.pixelsPerUnitMultiplier = 0.5f;
                    bg.color = FILL;
                }
                AddStroke(scroll, "gui_cut_stroke3", ACCENT, 0f);
            }
            if (sel.title != null) sel.title.color = ACCENT;
            if (sel.subtitle != null)
            {
                //carries the selected card's rules text — full readability
                sel.subtitle.color = TEXTC;
                sel.subtitle.fontSize = Mathf.Max(sel.subtitle.fontSize, 28);
                sel.subtitle.resizeTextForBestFit = false;
            }
        }

        //---- font overhaul: every legacy Text in the match -> Rajdhani ----
        private Font raj_regular, raj_semibold, raj_bold;
        private readonly HashSet<Text> fonted = new HashSet<Text>();

        private void FontPass()
        {
            if (raj_bold == null)
            {
                raj_regular = Resources.Load<Font>("Fonts/Rajdhani-Regular");
                raj_semibold = Resources.Load<Font>("Fonts/Rajdhani-SemiBold");
                raj_bold = Resources.Load<Font>("Fonts/Rajdhani-Bold");
                if (raj_bold == null)
                    return;
            }
            foreach (Text t in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (fonted.Contains(t))
                    continue;
                fonted.Add(t);
                string fname = t.font != null ? t.font.name : "";
                bool bold = t.fontStyle == FontStyle.Bold || t.fontStyle == FontStyle.BoldAndItalic || fname.Contains("Bold");
                t.font = bold ? raj_bold : (fname.Contains("Medium") || fname.Contains("Semi") ? raj_semibold : raj_regular);
            }
        }

        private Sprite S(string key) { return Resources.Load<Sprite>("GUI/" + key); }

        private void Skin(Button b)
        {
            string n = b.gameObject.name;
            Image img = b.GetComponent<Image>();
            string sprite_name = img != null && img.sprite != null ? img.sprite.name : "";

            if (n == "NextTurn")
            {
                //END TURN is restyled + docked by CCGMatchHUD (cut-corner pane)
            }
            else if (sprite_name == "button_large" || sprite_name == "button_thin" || n.StartsWith("Choice"))
            {
                //ROUNDED RECTS, never pills/ovals (user rule)
                bool negative = n == "Resign" || n == "Quit";
                RoundFill(img, "gui_semi", FILL, 1f);
                AddStroke(b.transform, "gui_semi_stroke2", negative ? NEGATIVE : ACCENT, 0f);
                AddStroke(b.transform, "gui_semi_stroke4", new Color(ACCENT.r, ACCENT.g, ACCENT.b, negative ? 0f : 0.25f), 6f);
                TintTexts(b.transform, negative ? NEGATIVE : TEXTC);
            }
            else if (sprite_name == "exit" || n == "X")
            {
                if (img != null)
                {
                    img.sprite = S("gui_ic_close");
                    img.color = TEXTC;
                    img.preserveAspect = true;
                }
            }
            else if (sprite_name == "arrow")
            {
                if (img != null)
                    img.color = ACCENT;
            }
            else if (sprite_name == "menu")
            {
                if (img != null)
                    img.color = TEXTC;
            }
            else if (n == "Quit" && sprite_name == "")
            {
                //game-over CONTINUE (text-only button): give it a real button
                if (img == null)
                    img = b.gameObject.AddComponent<Image>();
                b.targetGraphic = img;
                RoundFill(img, "gui_semi", FILL, 1f);
                AddStroke(b.transform, "gui_semi_stroke2", ACCENT, 0f);
                TintTexts(b.transform, ACCENT);
            }
        }

        private void RoundFill(Image img, string shape, Color color, float alpha)
        {
            if (img == null)
                return;
            Sprite s = S(shape);
            if (s == null)
                return;
            img.sprite = s;
            img.type = shape == "gui_circle" ? Image.Type.Simple : Image.Type.Sliced;
            img.color = new Color(color.r, color.g, color.b, alpha);
        }

        private void AddStroke(Transform host, string shape, Color color, float grow)
        {
            string sname = "CCG_Stroke_" + shape + "_" + grow;
            if (host.Find(sname) != null)
                return;
            GameObject go = new GameObject(sname, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(host, false);
            rt.SetSiblingIndex(0);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-grow, -grow);
            rt.offsetMax = new Vector2(grow, grow);
            Image img = go.GetComponent<Image>();
            img.sprite = S(shape);
            img.type = shape.StartsWith("gui_circle") ? Image.Type.Simple : Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
        }

        private void TintTexts(Transform host, Color color)
        {
            foreach (Text t in host.GetComponentsInChildren<Text>(true))
                t.color = color;
            foreach (TMPro.TextMeshProUGUI t in host.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                t.color = color;
        }
    }
}
