using UnityEngine;
using TcgEngine;
using TcgEngine.Gameplay;

namespace CCG
{
    /// <summary>
    /// CCG rules layer on top of TcgEngine's GameLogic.
    /// Phase B.1: refill-to-hand-size draw (hero-defined, baseline 4).
    /// Instantiated from GameServer and AILogic (see // CCG-EDIT tags there).
    /// </summary>
    public class CCGGameLogic : GameLogic
    {
        //true on the AI's minimax prediction instances: the defense window
        //must never open inside a simulation (it would stall the search)
        private readonly bool predict_mode = false;

        public CCGGameLogic(bool is_ai) : base(is_ai) { predict_mode = is_ai; }
        public CCGGameLogic(Game game) : base(game) { }

        //---- DEFENSE WINDOW (FaB Phase 1, mockup defense_window.svg) ----
        //Attacks on YOUR HERO open a window: commit hand cards as blockers
        //(their BLOCK total soaks damage before armor); committed cards are
        //discarded, feeding the Blood Tithe cycle.

        private int pending_block = 0;              //committed block total
        private Player pending_block_target = null; //who the block protects
        private int pending_mode = 0;               //0 none, 1 hero swing, 2 minion attack
        private Card pending_attacker;
        private Card pending_weapon;
        private Player pending_ptarget;
        private int pending_damage;
        private bool pending_skip_cost;

        /// <summary>Block value: authored "block" stat, else derived —
        /// expensive cards block 3, the rest block 2. Heroes never block.</summary>
        public static int GetBlock(TcgEngine.CardData c)
        {
            if (c == null || c.type == CardType.Hero)
                return 0;
            int authored = c.GetStat("block");
            if (authored > 0)
                return authored;
            return c.mana >= 4 ? 3 : 2;
        }

        public bool HasPendingDefense { get { return pending_mode != 0; } }

        /// <summary>Open the window if the defender is the local human with
        /// hand cards; AI defenders auto-block heuristically instead.
        /// Returns true if the attack is now WAITING on the defender.</summary>
        protected bool TryOpenDefenseWindow(int mode, Card attacker, Card weapon, Player defender, int damage, bool skip_cost)
        {
            if (predict_mode || defender == null || damage <= 0)
                return false;
            Game data = GetGameData();
            if (data.state != GameState.Play || pending_mode != 0)
                return false;

            bool ai_defender = defender.is_ai || GameplayData.Get().ai_vs_ai;
            if (ai_defender || defender.cards_hand.Count == 0)
            {
                if (ai_defender)
                    AIAutoBlock(attacker, defender, damage);
                return false; //resolve immediately
            }

            pending_mode = mode;
            pending_attacker = attacker;
            pending_weapon = weapon;
            pending_ptarget = defender;
            pending_damage = damage;
            pending_skip_cost = skip_cost;
            pending_block = 0;
            pending_block_target = defender;

            //minion attacks COMMIT on declaration (FaB): exhaust now so the
            //window can never be exploited into a second action
            if (mode == 2 && attacker != null)
                attacker.exhausted = true;

            data.selector = SelectorType.Defense;
            data.selector_player_id = defender.player_id;
            data.selector_caster_uid = attacker != null ? attacker.uid : "";
            data.selector_ability_id = damage.ToString(); //remaining damage for the HUD
            RefreshData();
            return true;
        }

        /// <summary>AI defense heuristic: block lethal or heavy (4+) hits with
        /// its lowest-pitch cards, spending at most 2 and keeping 1 in hand.</summary>
        protected virtual void AIAutoBlock(Card attacker, Player defender, int damage)
        {
            bool lethal = damage >= defender.hp;
            if (damage < 4 && !lethal)
                return;
            pending_block_target = defender;
            int committed = 0;
            while (committed < 2 && pending_block < damage && defender.cards_hand.Count > 1)
            {
                Card worst = null;
                foreach (Card c in defender.cards_hand)
                {
                    if (GetBlock(c.CardData) <= 0)
                        continue;
                    if (worst == null || c.GetTraitValue(CCGKeys.TraitPitch) < worst.GetTraitValue(CCGKeys.TraitPitch))
                        worst = c;
                }
                if (worst == null)
                    break;
                pending_block += GetBlock(worst.CardData);
                DiscardCard(worst);
                committed++;
            }
            if (pending_block <= 0)
                pending_block_target = null;
        }

        public override void SelectCard(Card target)
        {
            Game data = GetGameData();
            if (data.selector == SelectorType.Defense && pending_mode != 0)
            {
                Player defender = pending_ptarget;
                if (target != null && defender != null && target.player_id == defender.player_id
                    && defender.GetHandCard(target.uid) != null && GetBlock(target.CardData) > 0)
                {
                    pending_block += GetBlock(target.CardData);
                    DiscardCard(target);
                    data.selector_ability_id = Mathf.Max(0, pending_damage - pending_block).ToString();
                    if (pending_block >= pending_damage || defender.cards_hand.Count == 0)
                        ResolveDefense(); //fully blocked or out of cards
                    else
                        RefreshData();
                }
                return;
            }
            base.SelectCard(target);
        }

        public override void CancelSelection()
        {
            Game data = GetGameData();
            if (data.selector == SelectorType.Defense && pending_mode != 0)
            {
                ResolveDefense();
                return;
            }
            base.CancelSelection();
        }

        public override void NextStep()
        {
            //turn timer ran out mid-window: resolve with whatever was committed
            if (pending_mode != 0)
                ResolveDefense();
            base.NextStep();
        }

        protected virtual void ResolveDefense()
        {
            Game data = GetGameData();
            int mode = pending_mode;
            Card attacker = pending_attacker;
            Card weapon = pending_weapon;
            Player defender = pending_ptarget;
            int damage = pending_damage;
            bool skip_cost = pending_skip_cost;
            pending_mode = 0;
            pending_attacker = null;
            pending_weapon = null;
            pending_ptarget = null;
            data.selector = SelectorType.None;
            data.selector_player_id = -1;
            data.selector_ability_id = "";

            if (mode == 1)
            {
                //hero swing tail (cost/exhaust already paid on declaration)
                DamagePlayer(attacker, defender, damage); //blocks consumed inside
                TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, weapon, (Card)null);
                UpdateOngoing();
                CheckForWinner();
                RefreshData();
            }
            else if (mode == 2)
            {
                //self-contained resolution (attacker already exhausted at
                //declaration): re-entering the kit's attack pipeline from a
                //deferred context can wedge its resolve queue
                DamagePlayer(attacker, defender, damage); //blocks consumed inside
                UpdateOngoing();
                CheckForWinner();
                RefreshData();
            }
        }

        public override void StartGame()
        {
            base.StartGame();
            SpawnLoadouts();
        }

        //Spawn each player's equipment loadout fully active at game start (v0.10 FaB model)
        protected virtual void SpawnLoadouts()
        {
            Game data = GetGameData();
            foreach (Player player in data.players)
            {
                if (player.cards_equip.Count > 0)
                    continue; //Already spawned (AI resimulation safety)

                //Deck-builder loadout (per-deck EQUIPMENT slots) takes
                //precedence over the static CCGLoadoutData assets
                string ljson = PlayerPrefs.GetString(CCGCollectionUI.LOADOUT_PREF + player.deck, "");
                if (!string.IsNullOrEmpty(ljson))
                {
                    CCGLoadoutSave lsave = JsonUtility.FromJson<CCGLoadoutSave>(ljson);
                    if (lsave != null && lsave.tids != null)
                    {
                        bool spawned_any = false;
                        foreach (string tid in lsave.tids)
                        {
                            TcgEngine.CardData piece = TcgEngine.CardData.Get(tid);
                            if (piece == null)
                                continue;
                            Card card = Card.Create(piece, VariantData.GetDefault(), player);
                            player.cards_equip.Add(card);
                            if (player.hero != null && piece.HasTrait("weapon"))
                                player.hero.equipped_uid = card.uid;
                            spawned_any = true;
                        }
                        if (spawned_any)
                            continue; //this player's gear is set
                    }
                }

                CCGLoadoutData loadout = CCGLoadoutData.Get(player.deck);

                //User decks carry random suffixes (san_starter_XXXX) and custom
                //decks have their own ids — fall back to prefix match, then to
                //the hero's faction loadout so every deck gets its gear.
                if (loadout == null && !string.IsNullOrEmpty(player.deck))
                {
                    foreach (CCGLoadoutData l in Resources.LoadAll<CCGLoadoutData>("Loadouts"))
                        if (player.deck.StartsWith(l.deck_id)) { loadout = l; break; }
                }
                if (loadout == null && player.hero != null && player.hero.CardData.team != null)
                {
                    string team = player.hero.CardData.team.id;
                    string fallback = team == "bulwark" ? "bul_starter" : "san_starter";
                    loadout = CCGLoadoutData.Get(fallback);
                }
                if (loadout == null)
                    continue;

                foreach (TcgEngine.CardData piece in loadout.GetAllPieces())
                {
                    if (piece == null)
                        continue;
                    Card card = Card.Create(piece, VariantData.GetDefault(), player);
                    player.cards_equip.Add(card);

                    //Link the weapon to the hero so ongoing equip abilities (attack bonus) apply
                    if (player.hero != null && piece.HasTrait("weapon"))
                        player.hero.equipped_uid = card.uid;
                }
            }
            RefreshData();
        }

        //Armor absorbs before hero Health (v0.4 rules): incoming player damage depletes
        //armor-trait equipment durability first, remainder hits the player.
        //Artifact hooks: Wardstone Pendant (spells deal 1 less to your hero) and
        //Gorecall Warhorn (your minions gain +1/+0 this round when your hero is hit).
        public override void DamagePlayer(Card attacker, Player target, int value)
        {
            //DEFENSE WINDOW blocks soak first (FaB Phase 1): committed blockers
            //reduce the attack before armor and Health
            if (pending_block > 0 && target == pending_block_target)
            {
                value = Mathf.Max(0, value - pending_block);
                pending_block = 0;
                pending_block_target = null;
            }

            //Wardstone Pendant: your hero takes 1 less damage from spells
            if (attacker != null && attacker.CardData.type == CardType.Spell && HasBoardCard(target, "bul_wardstone_pendant"))
                value = Mathf.Max(0, value - 1);

            int remain = value;
            for (int i = target.cards_equip.Count - 1; i >= 0 && remain > 0; i--)
            {
                Card piece = target.cards_equip[i];
                if (piece.CardData.HasTrait("armor") && piece.GetHP() > 0)
                {
                    int soak = Mathf.Min(remain, piece.GetHP());
                    remain -= soak;
                    DamageCard(piece, soak); //auto-discards at 0 durability
                }
            }

            if (remain > 0)
            {
                base.DamagePlayer(attacker, target, remain);

                //Gorecall Warhorn: when your hero takes damage, minions gain +1/+0 this round
                if (HasBoardCard(target, "san_gorecall_warhorn"))
                {
                    foreach (Card m in target.cards_board)
                        if (m.CardData.IsCharacter())
                            m.AddStatus(StatusType.AddAttack, 1, 1);
                    RefreshData();
                }
            }
            else
            {
                RefreshData();
            }
        }

        protected static bool HasBoardCard(Player player, string card_id)
        {
            foreach (Card c in player.cards_board)
                if (c.card_id == card_id)
                    return true;
            return false;
        }

        //Kit sweep discards any equipment without a BOARD bearer (GetBearerCard only
        //scans cards_board). CCG loadout pieces belong to the HERO (a portrait, not a
        //board card), so exempt them: they die only at 0 durability.
        protected override void UpdateOngoingKills()
        {
            Game data = GetGameData();
            foreach (Player player in data.players)
            {
                for (int i = player.cards_board.Count - 1; i >= 0; i--)
                {
                    if (i < player.cards_board.Count)
                    {
                        Card card = player.cards_board[i];
                        if (card.GetHP() <= 0)
                            DiscardCard(card);
                    }
                }
                for (int i = player.cards_equip.Count - 1; i >= 0; i--)
                {
                    if (i < player.cards_equip.Count)
                    {
                        Card card = player.cards_equip[i];
                        //Weapons and FIELDS are permanent (no durability); armor
                        //dies when its durability is depleted.
                        if (card.GetHP() <= 0 && !card.CardData.HasTrait("weapon") && !card.CardData.HasTrait("field"))
                            DiscardCard(card);
                        else if (!IsLoadoutPiece(card) && player.GetBearerCard(card) == null)
                            DiscardCard(card); //kit-style equip on a dead minion
                    }
                }
            }
            //NOT calling base: it would re-run the bearer sweep and discard the loadout
            for (int c = 0; c < cards_to_clear.Count; c++)
                cards_to_clear[c].Clear();
            cards_to_clear.Clear();
        }

        protected static bool IsLoadoutPiece(Card card)
        {
            TcgEngine.CardData d = card.CardData;
            return d.HasTrait("weapon") || d.HasTrait("head") || d.HasTrait("chest") || d.HasTrait("arms") || d.HasTrait("legs") || d.HasTrait("field");
        }

        //FIELD cards (v0.26): field-trait artifacts leave the minion grid on play
        //and live in cards_equip — displayed on the LAND slot, auras stay active.
        public override void PlayCard(Card card, Slot slot, bool skip_cost = false)
        {
            base.PlayCard(card, slot, skip_cost);

            if (card != null && card.CardData.HasTrait("field"))
            {
                Game data = GetGameData();
                Player owner = data.GetPlayer(card.player_id);
                if (owner != null && owner.IsOnBoard(card))
                {
                    //Replace an existing field (one land per player)
                    for (int i = owner.cards_equip.Count - 1; i >= 0; i--)
                        if (owner.cards_equip[i].CardData.HasTrait("field"))
                            DiscardCard(owner.cards_equip[i]);

                    owner.cards_board.Remove(card);
                    owner.cards_equip.Add(card);
                    card.slot = Slot.None;
                    RefreshData();
                }
            }
        }

        //---- HERO ATTACKS (v0.24, the FaB core loop) ----
        //The hero swings its equipped weapon: damage = weapon attack, cost = the
        //weapon's printed mana cost per swing, once per turn (hero exhausts).
        //Counter damage from a defending minion hits the PLAYER (armor soaks).

        protected bool IsHeroCard(Card c)
        {
            if (c == null) return false;
            Player p = GetGameData().GetPlayer(c.player_id);
            return p != null && p.hero != null && p.hero.uid == c.uid;
        }

        public bool CanHeroAttack(Card hero, out Card weapon)
        {
            weapon = null;
            Game data = GetGameData();
            Player p = data.GetPlayer(hero.player_id);
            if (p == null || data.current_player != hero.player_id)
                return false;
            if (hero.exhausted)
                return false;
            weapon = data.GetEquipCard(hero.equipped_uid);
            if (weapon == null || !weapon.CardData.HasTrait("weapon"))
                return false;
            if (p.mana < weapon.GetMana())
                return false;
            return weapon.GetAttack() > 0;
        }

        public override void AttackTarget(Card attacker, Card target, bool skip_cost = false)
        {
            if (IsHeroCard(attacker)) { HeroAttack(attacker, target, null, skip_cost); return; }
            base.AttackTarget(attacker, target, skip_cost);
        }

        public override void AttackPlayer(Card attacker, Player ptarget, bool skip_cost = false)
        {
            if (IsHeroCard(attacker)) { HeroAttack(attacker, null, ptarget, skip_cost); return; }
            //minion face attack: the defender may block (FaB Phase 1)
            if (attacker != null && TryOpenDefenseWindow(2, attacker, null, ptarget, attacker.GetAttack(), skip_cost))
                return; //resumes in ResolveDefense
            base.AttackPlayer(attacker, ptarget, skip_cost);
        }

        protected virtual void HeroAttack(Card hero, Card target, Player ptarget, bool skip_cost)
        {
            Game data = GetGameData();
            Card weapon;
            if (!CanHeroAttack(hero, out weapon))
                return;
            Player player = data.GetPlayer(hero.player_id);

            //Target legality (free targeting; Taunt and Stealth still apply)
            if (target != null)
            {
                if (target.player_id == hero.player_id || !data.IsOnBoard(target))
                    return;
                if (target.HasStatus(StatusType.Stealth) || target.HasStatus(StatusType.Protected))
                    return;
            }
            else if (ptarget != null)
            {
                if (ptarget.player_id == hero.player_id)
                    return;
                if (ptarget.HasStatus(StatusType.Protected))
                    return;
            }
            else return;

            //Pay the swing cost and exhaust the hero (one swing per turn)
            if (!skip_cost)
                player.mana = Mathf.Max(0, player.mana - weapon.GetMana());
            hero.exhausted = true;

            int dmg = weapon.GetAttack();
            TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, weapon, target);

            if (target != null)
            {
                DamageCard(hero, target, dmg);
                //Defender counters the attacking PLAYER; armor soaks first
                if (target.GetHP() > 0 && target.GetAttack() > 0)
                    DamagePlayer(target, player, target.GetAttack());
            }
            else
            {
                //hero swing at the enemy player: they may block (FaB Phase 1);
                //cost + exhaust are already paid — the swing is declared
                if (TryOpenDefenseWindow(1, hero, weapon, ptarget, dmg, skip_cost))
                    return; //tail resumes in ResolveDefense
                DamagePlayer(hero, ptarget, dmg);
            }

            TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, weapon, target);
            UpdateOngoing(); //sweep kills
            CheckForWinner();
            RefreshData();
        }

        public override void StartTurn()
        {
            //Base draws GameplayData.cards_per_turn (set to 0 in config; we do all drawing here)
            base.StartTurn();

            Game data = GetGameData();
            if (data.state == GameState.GameEnded)
                return;

            Player player = data.GetActivePlayer();

            //Loadout spawn: StartGame is not routed through this class in all modes,
            //so ensure the loadouts exist by the first turns (guarded against dupes).
            if (data.turn_count <= 1)
                SpawnLoadouts();

            //PITCH ECONOMY (FULL FaB, v0.23): no automatic mana pool, no pitch limit.
            //Floating mana starts at 0 each turn and comes ONLY from pitching cards;
            //hand economy is the real cap. Orb bar is a fixed 10-orb gauge.
            player.mana = 0;
            player.mana_max = 10;

            //BLOOD TITHE (balance, sim-driven): an EMPTY deck refills itself
            //from the discard pile (shuffled) for 2 Health. Before this, empty
            //decks made draws fail silently forever — no pitch fuel, no plays,
            //an unwinnable starve-out (user report; ai_vs_ai sims confirmed
            //~1.3 cards/turn net attrition on a 20-card deck). The Health cost
            //keeps a soft clock on grindy games instead of an infinite loop.
            if (player.cards_deck.Count == 0 && player.cards_discard.Count > 0)
            {
                foreach (Card dc in player.cards_discard)
                {
                    dc.Clear();
                    player.cards_deck.Add(dc);
                }
                player.cards_discard.Clear();
                ShuffleDeck(player.cards_deck);
                player.hp -= 2;
            }

            //Refill-up-to hand size (skip very first turn of the first player, matching kit draw rule)
            if (data.turn_count > 1 || player.player_id != data.first_player)
            {
                int hand_size = GetHandSize(player);
                int missing = hand_size - player.cards_hand.Count;
                if (missing > 0)
                    DrawCard(player, missing);
            }

            //AI cannot use the pitch UI: auto-pitch for it so it can play.
            //(ai_vs_ai balance sims: the "local" player is AI-driven but not
            //flagged is_ai — auto-pitch it too or it mana-starves)
            if (player.is_ai || GameplayData.Get().ai_vs_ai)
                AutoPitch(data, player);

            RefreshData();
        }

        //Simple AI pitch heuristic (FULL FaB: no limit): pitch the highest-value
        //cards for fuel, always keeping at least 2 cards in hand to play.
        protected virtual void AutoPitch(Game data, Player player)
        {
            while (player.cards_hand.Count > 2)
            {
                Card best = null;
                foreach (Card c in player.cards_hand)
                {
                    int pv = c.GetTraitValue(CCGKeys.TraitPitch);
                    if (pv <= 0)
                        continue;
                    if (best == null || pv > best.GetTraitValue(CCGKeys.TraitPitch))
                        best = c;
                }
                if (best == null)
                    break;

                player.mana += best.GetTraitValue(CCGKeys.TraitPitch);
                player.RemoveCardFromAllGroups(best);
                player.cards_deck.Add(best);
                best.Clear();
            }
        }

        //Hero card can override hand size with a "hand" trait stat; baseline 4
        public virtual int GetHandSize(Player player)
        {
            if (player.hero != null)
            {
                int custom = player.hero.GetTraitValue(CCGKeys.TraitHandSize);
                if (custom > 0)
                    return custom;
            }
            return CCGKeys.DefaultHandSize;
        }
    }
}
