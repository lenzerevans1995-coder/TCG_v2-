using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine;
using TcgEngine.Client;
using CardUtilities;

namespace CCG
{
    /// <summary>
    /// Draw animation: when a player draws, a card back flies from their DECK pile
    /// toward their hand (enemy draws fly toward the enemy hand at the top).
    /// </summary>
    public class CCGDrawFX : MonoBehaviour
    {
        public float fly_time = 0.45f;

        void Start()
        {
            GameClient client = GameClient.Get();
            if (client != null)
                client.onCardDraw += OnDraw;
        }

        void OnDestroy()
        {
            GameClient client = GameClient.Get();
            if (client != null)
                client.onCardDraw -= OnDraw;
        }

        private void OnDraw(int nb)
        {
            GameClient client = GameClient.Get();
            Game data = client != null ? client.GetGameData() : null;
            if (data == null || data.state != GameState.Play)
                return;
            Player drawer = data.GetActivePlayer();
            if (drawer == null)
                return;
            bool enemy = drawer.player_id != client.GetPlayerID();

            for (int i = 0; i < Mathf.Min(nb, 6); i++)
                StartCoroutine(Fly(drawer, enemy, i * 0.12f));
        }

        private IEnumerator Fly(Player drawer, bool enemy, float delay)
        {
            yield return new WaitForSeconds(delay);

            GameObject layout = GameObject.Find("CCG_BoardLayout");
            Transform side = layout != null ? layout.transform.Find(enemy ? "EnemySide" : "PlayerSide") : null;
            Transform deck_a = side != null ? side.Find("Deck") : null;
            if (deck_a == null)
                yield break;

            Vector3 from = deck_a.position + Vector3.up * 0.4f;
            Vector3 to = enemy ? new Vector3(0f, 0.6f, 4.8f) : new Vector3(0f, 0.6f, -4.8f);

            //Backs are cosmetic, not faction: default RED for everyone (user rule)
            CardScriptable style = CCGCardFace.GetBackStyle();
            if (style == null)
                yield break;

            GameObject go = new GameObject("DrawFX", typeof(RectTransform), typeof(Canvas));
            Canvas cv = go.GetComponent<Canvas>();
            cv.renderMode = RenderMode.WorldSpace;
            cv.sortingOrder = 10;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 200);
            rt.localScale = new Vector3(0.01f, 0.01f, 1f);

            GameObject face_go = new GameObject("Back", typeof(RectTransform));
            RectTransform face = face_go.GetComponent<RectTransform>();
            face.SetParent(go.transform, false);
            face.localScale = Vector3.one * 0.22f;
            face.sizeDelta = new Vector2(618, 922);

            var imgs = new System.Collections.Generic.List<Image>();
            foreach (UICardImage el in style.UIElementsImage)
            {
                Vector2 size = Vector2.zero; Vector2 epos = Vector2.zero; bool use = false;
                switch (el.label)
                {
                    case LabelImageCards.BackBackground: size = new Vector2(618, 922); use = true; break;
                    case LabelImageCards.BackFrame: size = new Vector2(512, 816); use = true; break;
                    case LabelImageCards.BackSymbol: size = new Vector2(369.64f, 369.64f); use = true; break;
                    case LabelImageCards.BackSymbol2: size = new Vector2(369.64f, 369.64f); epos = new Vector2(0, 27); use = true; break;
                }
                if (!use || el.sprite == null)
                    continue;
                GameObject igo = new GameObject(el.label.ToString(), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform irt = igo.GetComponent<RectTransform>();
                irt.SetParent(face, false);
                irt.sizeDelta = size;
                irt.anchoredPosition = epos;
                Image i2 = igo.GetComponent<Image>();
                i2.sprite = el.sprite;
                i2.color = el.color;
                i2.raycastTarget = false;
                imgs.Add(i2);
            }
            if (imgs.Count == 0)
            {
                Destroy(go);
                yield break;
            }

            //Top-down camera: the card stays flat for the whole flight
            Quaternion flat = Quaternion.Euler(90f, 0f, 0f);
            Quaternion facing = Quaternion.Euler(90f, 0f, 0f);

            float t = 0f;
            while (t < fly_time)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / fly_time);
                Vector3 p = Vector3.Lerp(from, to, k);
                p.y += Mathf.Sin(k * Mathf.PI) * 1.2f; //arc
                rt.position = p;
                rt.rotation = Quaternion.Slerp(flat, facing, k);
                float a = k > 0.85f ? (1f - k) / 0.15f : 1f;
                foreach (Image im in imgs)
                    im.color = new Color(im.color.r, im.color.g, im.color.b, a);
                yield return null;
            }
            Destroy(go);
        }
    }
}
