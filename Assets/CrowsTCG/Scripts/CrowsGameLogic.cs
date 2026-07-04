using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.Gameplay
{
    /// <summary>
    /// CROWS TCG rules layer v0 (Docs/Design/CROWS/00_Pillars_and_Plan.md).
    /// Deterministic — no dice. Strike model default = ArmorGate (model B,
    /// recommended; ruling A/B still open — flip strike_model to compare in sims).
    /// Counter-damage is also armor-gated (symmetric); sims to validate.
    /// </summary>
    public class CrowsGameLogic : GameLogic
    {
        public enum StrikeModel { ArmorReduction = 0, ArmorGate = 1 }
        public static StrikeModel strike_model = StrikeModel.ArmorGate;

        public const int actions_per_turn = 3;   // CROWS 0.3: 3 actions per turn
        public const int draw_per_turn = 4;      // CROWS 0.5: draw 4 at turn start
        public const string armor_stat = "armor";        // AC, authored as TraitStat on CardData
        public const string consumed_trait = "consumed"; // flagged in discard, excluded from recycle

        public CrowsGameLogic(bool is_ai) : base(is_ai) { }
        public CrowsGameLogic(Game game) : base(game) { }

        public static int GetArmor(Card card)
        {
            return card != null ? card.GetTraitValue(armor_stat) : 0;
        }

        // The whole deterministic combat rule in one place.
        public static int GetStrikeDamage(int power, int armor)
        {
            if (strike_model == StrikeModel.ArmorGate)
                return power >= armor ? power : 0; // armor is a wall: clear it or nothing
            return Mathf.Max(power - armor, 0);    // armor chips damage
        }

        public override void StartTurn()
        {
            if (GameData.state == GameState.GameEnded)
                return;

            Game game_data = GameData;
            ClearTurnData();
            game_data.phase = GamePhase.StartTurn;
            RefreshData();
            onTurnStart?.Invoke();

            Player player = game_data.GetActivePlayer();
            player.history_list.Clear();

            // CROWS: draw 4 every turn (deck recycles, hand discards at end — no first-turn skip)
            DrawCard(player, draw_per_turn);

            // CROWS: flat 3 actions per turn, no ramp (kit mana = actions)
            player.mana_max = actions_per_turn;
            player.mana = actions_per_turn;

            LevelData level = game_data.settings.GetLevel();
            game_data.turn_timer = level != null ? level.turn_duration : GameplayData.Get().turn_duration;

            if (player.HasStatus(StatusType.Poisoned))
                player.hp -= player.GetStatusValue(StatusType.Poisoned);

            if (player.hero != null)
                player.hero.Refresh();

            for (int i = player.cards_board.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_board[i];
                if (!card.HasStatus(StatusType.Sleep))
                    card.Refresh();
                if (card.HasStatus(StatusType.Poisoned))
                    DamageCard(card, card.GetStatusValue(StatusType.Poisoned));
            }

            UpdateOngoing();

            TriggerPlayerCardsAbilityType(player, AbilityTrigger.StartOfTurn);
            TriggerPlayerSecrets(player, AbilityTrigger.StartOfTurn);

            ResolveQueue.AddCallback(StartMainPhase);
            ResolveQueue.ResolveAll(0.2f);
        }

        public override void EndTurn()
        {
            if (GameData.state == GameState.GameEnded)
                return;
            if (GameData.phase != GamePhase.Main)
                return;

            // CROWS 0.5: discard the whole hand at end of turn (before kit end-turn flow)
            Player player = GameData.GetActivePlayer();
            for (int i = player.cards_hand.Count - 1; i >= 0; i--)
                DiscardCard(player.cards_hand[i]);

            base.EndTurn();
        }

        public override void DrawCard(Player player, int nb = 1)
        {
            // CROWS 0.6: empty deck recycles the discard for free (consumed cards stay out)
            for (int i = 0; i < nb; i++)
            {
                if (player.cards_deck.Count == 0)
                    RecycleDiscard(player);

                if (player.cards_deck.Count > 0 && player.cards_hand.Count < GameplayData.Get().cards_max)
                {
                    Card card = player.cards_deck[0];
                    player.cards_deck.RemoveAt(0);
                    player.cards_hand.Add(card);
                }
            }

            onCardDrawn?.Invoke(nb);
        }

        public virtual void RecycleDiscard(Player player)
        {
            for (int i = player.cards_discard.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_discard[i];
                if (card.GetTraitValue(consumed_trait) == 0)
                {
                    player.cards_discard.RemoveAt(i);
                    player.cards_deck.Add(card);
                }
            }
            ShuffleDeck(player.cards_deck);
        }

        /// CROWS Consume: removed from the game. v0: stays in discard flagged
        /// consumed so it survives netcode serialization but never recycles.
        public virtual void ConsumeCard(Card card)
        {
            Player owner = GameData.GetPlayer(card.player_id);
            owner.RemoveCardFromAllGroups(card);
            owner.cards_discard.Add(card);
            card.SetTrait(consumed_trait, 1);
            RefreshData();
        }

        protected override void ResolveAttackHit(Card attacker, Card target, bool skip_cost)
        {
            // Deterministic strike: POWER vs ARMOR both ways (no dice, no crits)
            int datt1 = GetStrikeDamage(attacker.GetAttack(), GetArmor(target));
            int datt2 = GetStrikeDamage(target.GetAttack(), GetArmor(attacker));

            if (datt1 > 0)
                DamageCard(attacker, target, datt1);

            if (!attacker.HasStatus(StatusType.Intimidate) && datt2 > 0)
                DamageCard(target, attacker, datt2);

            if (!skip_cost)
                ExhaustBattle(attacker);

            UpdateOngoing();

            bool att_board = GameData.IsOnBoard(attacker);
            bool def_board = GameData.IsOnBoard(target);
            if (att_board)
                TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, attacker, target);
            if (def_board)
                TriggerCardAbilityType(AbilityTrigger.OnAfterDefend, target, attacker);
            if (att_board)
                TriggerSecrets(AbilityTrigger.OnAfterAttack, attacker);
            if (def_board)
                TriggerSecrets(AbilityTrigger.OnAfterDefend, target);

            onAttackEnd?.Invoke(attacker, target);
            RefreshData();
            CheckForWinner();

            ResolveQueue.ResolveAll(0.2f);
        }
    }
}
