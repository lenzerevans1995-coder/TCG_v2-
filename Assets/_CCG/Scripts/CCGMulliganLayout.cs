using System.Collections.Generic;
using UnityEngine;
using TcgEngine.UI;

namespace CCG
{
    /// <summary>
    /// Companion to the kit's MulliganSelector (attach to the same GameObject).
    /// The kit fills mulligan slots left-to-right but never hides unused ones and
    /// never centers the row. This hides empty slots and re-centers the filled ones,
    /// so any hand size (baseline 4) displays correctly.
    /// </summary>
    public class CCGMulliganLayout : MonoBehaviour
    {
        private MulliganSelector selector;
        private Vector3[] original_pos;
        private float center_x;
        private float spacing = 0f;

        void Awake()
        {
            selector = GetComponent<MulliganSelector>();
            if (selector == null || selector.cards == null || selector.cards.Length == 0)
                return;

            original_pos = new Vector3[selector.cards.Length];
            float sum = 0f;
            for (int i = 0; i < selector.cards.Length; i++)
            {
                original_pos[i] = selector.cards[i].transform.localPosition;
                sum += original_pos[i].x;
            }
            center_x = sum / selector.cards.Length;
            if (selector.cards.Length > 1)
                spacing = Mathf.Abs(original_pos[1].x - original_pos[0].x);
        }

        void LateUpdate()
        {
            if (selector == null || selector.cards == null || original_pos == null)
                return;

            //Collect filled slots
            List<CardMulligan> filled = new List<CardMulligan>();
            foreach (CardMulligan slot in selector.cards)
            {
                bool has_card = slot.GetCard() != null;
                if (slot.gameObject.activeSelf != has_card)
                    slot.gameObject.SetActive(has_card);
                if (has_card)
                    filled.Add(slot);
            }

            //Center the filled row around the original row center
            if (filled.Count > 0 && spacing > 0f)
            {
                float start = center_x - (filled.Count - 1) * 0.5f * spacing;
                for (int i = 0; i < filled.Count; i++)
                {
                    Vector3 pos = filled[i].transform.localPosition;
                    pos.x = start + i * spacing;
                    filled[i].transform.localPosition = pos;
                }
            }
        }
    }
}
