using UnityEngine;
using TcgEngine;

namespace CCG
{
    /// <summary>
    /// A deck's equipment loadout: 1 weapon + 4 typed armor pieces (any may be null).
    /// Loaded from Resources/Loadouts and matched to a deck by deck_id.
    /// All pieces enter play fully active on the equipment anchors at game start (v0.10 rules).
    /// </summary>
    [CreateAssetMenu(fileName = "loadout", menuName = "CCG/LoadoutData", order = 20)]
    public class CCGLoadoutData : ScriptableObject
    {
        public string deck_id;
        public TcgEngine.CardData weapon;
        public TcgEngine.CardData head;
        public TcgEngine.CardData chest;
        public TcgEngine.CardData arms;
        public TcgEngine.CardData legs;

        public static CCGLoadoutData Get(string deck_id)
        {
            foreach (CCGLoadoutData l in Resources.LoadAll<CCGLoadoutData>("Loadouts"))
            {
                if (l.deck_id == deck_id)
                    return l;
            }
            return null;
        }

        public TcgEngine.CardData[] GetAllPieces()
        {
            return new TcgEngine.CardData[] { weapon, head, chest, arms, legs };
        }
    }
}
