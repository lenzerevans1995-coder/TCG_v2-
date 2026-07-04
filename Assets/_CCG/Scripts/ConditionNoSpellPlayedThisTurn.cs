using UnityEngine;
using TcgEngine;

namespace CCG
{
    /// <summary>
    /// Radiant Signet gate: true while the caster's owner has NOT played a spell
    /// yet this turn (Player.history_list is cleared at the start of each turn),
    /// so the ongoing "first spell costs 1 less" discount applies only once.
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CCGNoSpellPlayedThisTurn", order = 51)]
    public class ConditionNoSpellPlayedThisTurn : ConditionData
    {
        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            Player player = data.GetPlayer(caster.player_id);
            if (player == null)
                return false;
            foreach (ActionHistory item in player.history_list)
            {
                if (item.type == GameAction.PlayCard)
                {
                    TcgEngine.CardData cd = TcgEngine.CardData.Get(item.card_id);
                    if (cd != null && cd.type == CardType.Spell)
                        return false;
                }
            }
            return true;
        }
    }
}
