using UnityEngine;
using TcgEngine;

namespace CCG
{
    /// <summary>
    /// Gates the ccg_pitch ability (FULL FaB, v0.23): NO pitch limit — a card can
    /// always be pitched on the owner's own turn if it has a pitch value. The real
    /// cost is hand economy, exactly like Flesh and Blood.
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CCGPitchLimit", order = 50)]
    public class ConditionPitchLimit : ConditionData
    {
        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            if (data.current_player != caster.player_id)
                return false; //Only on your own turn

            //Only from HAND: without this, board minions offered a blank kit
            //ability button (the blue rectangle) and could be pitched from play
            if (data.GetHandCard(caster.uid) == null)
                return false;

            return caster.GetTraitValue(CCGKeys.TraitPitch) > 0;
        }
    }
}
