using UnityEngine;
using TcgEngine;
using TcgEngine.Gameplay;

namespace CCG
{
    /// <summary>
    /// Pitch (FULL FaB, v0.23): the caster (a card in hand) is sent to the BOTTOM of
    /// its owner's deck and the owner gains floating mana equal to the card's pitch
    /// value. NO limit — hand economy is the cap, exactly like Flesh and Blood.
    /// Floating mana clears at the start of the owner's next turn.
    /// Used by the ccg_pitch activated ability (castable from hand, own turn only).
    /// </summary>
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/CCGPitch", order = 50)]
    public class EffectPitch : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            int pitch = caster.GetTraitValue(CCGKeys.TraitPitch);
            if (pitch <= 0)
                return;

            Game data = logic.GetGameData();
            Player owner = data.GetPlayer(caster.player_id);

            //Gain floating mana (can exceed the orb display within the turn)
            owner.mana += pitch;

            //Send the pitched card face-down to the bottom of the deck (draws pop index 0)
            owner.RemoveCardFromAllGroups(caster);
            owner.cards_deck.Add(caster);
            caster.Clear();
        }
    }
}
