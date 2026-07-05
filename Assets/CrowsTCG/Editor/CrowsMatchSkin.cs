using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace CrowsTCG.EditorTools
{
    /// <summary>
    /// De-neons the match (Game.unity) into the Heat/CROWS warm palette used by
    /// the menus: cyan/neon-blue accents -> gold, cool slate panels -> warm
    /// charcoal, enemy neon red/pink -> deep blood red. Covers UI Graphics,
    /// world SpriteRenderers (board/slots), the camera clear color, and the
    /// BoardCard prefab glow colors. Idempotent: re-running re-derives from
    /// current colors (warm colors map to themselves).
    /// </summary>
    public static class CrowsMatchSkin
    {
        const string SCENE = "Assets/TcgEngine/Scenes/Game/Game.unity";

        // Heat/CROWS anchors (match the menu retheme)
        static readonly Color GOLD = Hex("#c9a84c");
        static readonly Color PARCHMENT = Hex("#e8dcc0");
        static readonly Color BLOOD = Hex("#8e1f2c");

        [MenuItem("CROWS/Skin Match Scene")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(SCENE);
            int ui = 0, world = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var g in root.GetComponentsInChildren<Graphic>(true))
                {
                    if (Remap(g.color, out Color c)) { g.color = c; EditorUtility.SetDirty(g); ui++; }
                }
                foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (Remap(sr.color, out Color c)) { sr.color = c; EditorUtility.SetDirty(sr); world++; }
                }
                foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                {
                    if (Remap(cam.backgroundColor, out Color c)) { cam.backgroundColor = c; EditorUtility.SetDirty(cam); }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // gameplay prefabs: remap their graphics too (ability buttons etc. spawn from prefabs)
            foreach (var ppath in new[] {
                "Assets/TcgEngine/Prefabs/Gameplay/BoardCard.prefab",
                "Assets/TcgEngine/Prefabs/Gameplay/HandCard.prefab",
                "Assets/TcgEngine/Prefabs/GameUI.prefab" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ppath);
                if (prefab == null) continue;
                int n = 0;
                foreach (var g in prefab.GetComponentsInChildren<Graphic>(true))
                    if (Remap(g.color, out Color c)) { g.color = c; n++; }
                foreach (var sr in prefab.GetComponentsInChildren<SpriteRenderer>(true))
                    if (Remap(sr.color, out Color c)) { sr.color = c; n++; }
                var bc = prefab.GetComponent<TcgEngine.Client.BoardCard>();
                if (bc != null) { bc.glow_ally = GOLD; bc.glow_enemy = BLOOD; n++; }
                if (n > 0) EditorUtility.SetDirty(prefab);
                Debug.Log("CROWS: prefab " + ppath + " remapped " + n);
            }
            AssetDatabase.SaveAssets();

            // board backdrop texture is cool slate — warm it with a sepia tint
            var scene2 = EditorSceneManager.GetActiveScene();
            foreach (var root in scene2.GetRootGameObjects())
            {
                foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.gameObject.name.ToLower().Contains("background") || sr.gameObject.name.ToLower().Contains("board_bg") || (sr.transform.parent != null && sr.transform.parent.name == "GameBoard"))
                    {
                        Color cur = sr.color;
                        sr.color = new Color(cur.r * 0.92f, cur.g * 0.85f, cur.b * 0.68f, cur.a);
                        EditorUtility.SetDirty(sr);
                    }
                }
            }
            EditorSceneManager.MarkSceneDirty(scene2);
            EditorSceneManager.SaveScene(scene2);

            Debug.Log("CROWS: match de-neoned - " + ui + " ui graphics, " + world + " sprites remapped");
        }

        /// Map cool/neon colors into the warm palette; leave warm/neutral alone.
        static bool Remap(Color input, out Color output)
        {
            output = input;
            float a = input.a;
            Color.RGBToHSV(input, out float h, out float s, out float v);
            if (s < 0.06f && v > 0.02f)
            {
                // pure greys: warm them very slightly
                output = Color.Lerp(input, new Color(v, v * 0.96f, v * 0.88f, 1f), 0.5f);
                output.a = a;
                return Vector4.Distance(input, output) > 0.01f;
            }

            float deg = h * 360f;
            bool cool = deg > 150f && deg < 280f;   // cyan/blue/indigo = the neon family
            bool neonred = (deg >= 330f || deg < 15f) && s > 0.55f && v > 0.55f; // hot pink/red accents
            if (!cool && !neonred)
                return false;

            if (cool)
            {
                if (s > 0.45f && v > 0.5f)
                    output = Color.Lerp(GOLD, PARCHMENT, Mathf.InverseLerp(0.5f, 1f, v) * 0.5f); // accent -> gold
                else
                    output = Color.HSVToRGB(38f / 360f, Mathf.Min(s, 0.35f), v); // panel -> warm charcoal, keep value
            }
            else
            {
                output = Color.Lerp(BLOOD, Hex("#b04040"), Mathf.InverseLerp(0.55f, 1f, v) * 0.4f); // enemy -> blood
            }
            output.a = a;
            return true;
        }

        static Color Hex(string hex)
        {
            Color c; ColorUtility.TryParseHtmlString(hex, out c); return c;
        }
    }
}
