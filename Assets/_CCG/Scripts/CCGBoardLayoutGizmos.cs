using UnityEngine;

namespace CCG
{
    /// <summary>
    /// Editor visualization for the CCG_BoardLayout template: draws every zone
    /// anchor as a labeled card-sized outline in the Scene view so the layout
    /// can be seen and edited by hand. Player side red, enemy side blue.
    /// No effect at runtime.
    /// </summary>
    public class CCGBoardLayoutGizmos : MonoBehaviour
    {
        [Tooltip("Card footprint drawn per anchor (world units, matches card_scale 0.22)")]
        public Vector2 card_size = new Vector2(1.36f, 2.03f);

        void OnDrawGizmos()
        {
            foreach (Transform side in transform)
            {
                bool enemy = side.name.StartsWith("Enemy");
                Gizmos.color = enemy ? new Color(0.35f, 0.6f, 1f, 0.9f) : new Color(1f, 0.35f, 0.35f, 0.9f);
                foreach (Transform a in side)
                {
                    Vector3 p = a.position;
                    Gizmos.DrawWireCube(p + Vector3.up * 0.02f, new Vector3(card_size.x, 0.01f, card_size.y));
#if UNITY_EDITOR
                    UnityEditor.Handles.color = Gizmos.color;
                    UnityEditor.Handles.Label(p + new Vector3(-card_size.x * 0.45f, 0.05f, 0f), a.name);
#endif
                }
            }
        }
    }
}
