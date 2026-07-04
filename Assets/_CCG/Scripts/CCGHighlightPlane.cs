using UnityEngine;

namespace CCG
{
    /// <summary>
    /// Board-card highlight driver: suppresses the kit's grey glow sprite and mirrors
    /// its alpha/color (hover + selection, ally/enemy tint) into the card-shaped
    /// UI glow built by CCGHexCardView. Class name kept for prefab compatibility.
    /// </summary>
    public class CCGHighlightPlane : MonoBehaviour
    {
        [Tooltip("Legacy field, unused")]
        public GameObject plane_model;
        [Tooltip("Legacy field, unused")]
        public Material highlight_material;
        [Tooltip("Glow strength multiplier over the kit glow alpha")]
        public float margin = 1.5f;

        private SpriteRenderer kit_glow;
        private CCGHexCardView view;

        void Start()
        {
            Transform glow_t = transform.Find("Glow");
            if (glow_t != null)
                kit_glow = glow_t.GetComponent<SpriteRenderer>();
            view = GetComponent<CCGHexCardView>();
        }

        void LateUpdate()
        {
            if (kit_glow == null)
                return;

            //Kit re-enables its grey glow sprite each refresh; keep it off permanently
            if (kit_glow.enabled)
                kit_glow.enabled = false;

            if (view != null)
                view.SetOutline(kit_glow.color.a * margin, kit_glow.color);
        }
    }
}
