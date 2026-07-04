# CROWS TCG — v2 Pillars & Build Plan

v1 (anime-style FaB/LoR hybrid) is archived at git tag `v1-prototype`.
v2 = **CROWS rules (01_Rules_Digest.md) presented as a Legends of
Runeterra-type game** (user direction 2026-07-04): clean full-screen 2D
arena, painterly full-art cards, LoR's readable board language — while
reusing v1's engine systems and pipeline.

## Pillars
1. **RPG combat, deterministic (NO DICE — user ruling 2026-07-04)** —
   CROWS' d20/dice constructs translate to fixed values so play is fully
   predictable like LoR. Strikes resolve by numbers, not rolls; the
   presentation moment stays (staged strike resolution center-board),
   the randomness goes.
2. **LoR-style arena, ONE UNIT ROW PER SIDE (user rulings 2026-07-04)** —
   two rows total, 6 uniform sockets each. **CHAMPIONS ARE CARDS, plural
   (user: LoR runs multiple champions)**: premium units you draw and
   play into the row beside minions — deck may include several champion
   cards (LoR-style cap, e.g. 6 total / 1 copy per name on board), and
   losing a champion is NOT losing the game. **The deck's identity is
   its TWO ASPECTS (LoR regions model)** — champions must belong to the
   deck's aspects. **Player life = THE ROOST** (crow-flavored nexus):
   per-side health orb at the board edge; units strike the Roost to win.
   SPELLS cast and resolve center-stage — no board presence. EQUIPMENT
   casts onto units and shows as ICONS on the card (champions hold 2 =
   the two-hands rule; minions 1 unless stated). Continuous/temporary
   cards occupy row sockets like LoR landmarks. Hand fan bottom, round
   PASS button right, action pips (3/turn) left.
3. **Two-Aspect identity** — deckbuilding = pick a champion, its two
   Aspects open the pool (plus universal Items). 10 Aspects, 45 pairs.
4. **Coherent premade ruleset first** — implement CROWS as written, expand
   only after the base loop is playable and sim-tested. (Candidate later
   variant, sims will judge: LoR-style alternating priority — players
   trade single actions instead of whole 3-action turns.)

## What v1 gives v2 (reuse map)
| v1 system                       | v2 fate |
|---------------------------------|---------|
| TcgEngine server-auth core      | KEEP — actions/turns/status effects host CROWS cleanly |
| Sim harness (ai_vs_ai + logging)| KEEP — balance CROWS from day one |
| Deck builder v2 (hero-locked pools, checkout, drafts, loadout slots) | KEEP — aspects replace factions; 2 equip slots replace 5 |
| Wildcard economy + Forge + cart | KEEP as-is |
| Trials roguelite                | KEEP (re-theme later) |
| Blood Tithe                     | REPLACED by CROWS rule 0.6 (free discard->deck recycle) |
| Pitch economy                   | REMOVED — CROWS uses 3 actions/turn |
| Defense window                  | PARKED — CROWS resolves via AC/saves; revisit as a reaction system later |
| FaB loadout (weapon+4 armor)    | REPLACED by 2 equip "hands" |
| Docked match HUD + card face composer + hover preview + browser | REBUILT visually, but all patterns/utilities (CoverFit, panes, drawer) reused |
| Kit status system (StatusType)  | KEEP — boons/ailments map onto it (stack values via status value/duration) |
| Kit secrets                     | KEEP — CROWS "Prepare" face-down cards |
| Art pipeline (prompt manifest -> CSV -> generate -> import)| KEEP — new style templates |

## Deterministic translation of CROWS dice (proposal, needs ruling)
- **Strike (was d20 vs AC)** — two candidate models:
  (A) *Armor as reduction*: damage = POWER - ARMOR (LoR/standard, always
      chips through with high power).
  (B) *Armor as gate* (RECOMMENDED — keeps CROWS' "AC = how hard to
      strike" identity): POWER < ARMOR = no effect; POWER >= ARMOR = full
      damage. Armor becomes a real defensive stat worth building.
- **dX damage/values** — printed fixed numbers on cards (design guide:
  d4=2, d6=3, d8=4, d10=5, d12=6, d20=10 when converting existing text).
- **Crit/fumble** — cut. (Nat-1 CONSUME becomes an explicit cost on
  specific cards: "Consume this card" as printed text.)
- **Saves** — become explicit conditions/costs ("unless the target spends
  1 action", "if the target is damaged", etc.) card by card.
- **Advantage/Disadvantage & roll-boons** (Blessed, Lucky, Hunting,
  Sharpened, Inspired, Empowered, Enraged, Smitten) — retranslated as
  flat damage/armor modifiers; ailment DICE STACKS (Bleeding d4, Burning
  d6, Poison d4) become fixed stack counts (2/3/2).
- Action-economy boons/ailments survive unchanged (Focused, Quickened,
  Dazed, Distress, Stunned, Entangled, Cursed, Sleeping, Trauma, Chilled,
  Hidden as strike-prevention).

## New in v2 (build order)
1. **Strike resolver** (server): deterministic POWER vs ARMOR resolution
   (model A or B above), consume zone, logged for sims.
2. **Action economy**: 3 actions/turn, draw 4, discard hand at end,
   deck recycle on empty.
3. **Strike/Save resolution**: AC on champions/minions; strike rolls; nat-1
   consume; CONSUMED zone.
4. **Board v2 (LoR-style, one row per side)**: full-screen 2D arena —
   6-socket unit row per side with the champion as an in-row unit
   (ornate center socket); equipment renders as icons on unit cards;
   spells resolve center-stage with no board slot; deck/discard/consumed
   rails at the edges; action pips left; round PASS button right. No 3D
   table (half of v1's match bugs were 3D-board specific). Mockup:
   mockups/crows_board.svg.
5. **Boons/Ailments**: implement as kit statuses; the 23 from the digest.
6. **Aspects**: 10 aspect tags; champion dual-aspect deck legality.
7. **Card set v0**: ~6 cards per aspect + 5 champions to start; block/pitch
   stats retired; new stat line = cost(actions)/AC/Life/dice effects.
8. **Art style bible**: 3 candidate templates -> user shoots the same 6
   cards in each -> winner becomes the set template. (OPEN: direction.)

## Art direction — DECIDED (2026-07-04)
The **CROWS asset pack's hand-drawn ink style** IS the game's style (user
ruling): bold black ink outlines, muted desaturated palette, rough
watercolor shading, weathered grey stone + dark green thorny vines, grim
storybook. Pack location (to be imported into the project):
`C:/UnityProjects/TradingCardGame/My project/Crows_TCG/` — 10 aspect card
fronts + champion frame, 4 card backs (crow/octagon/10-gem identity),
~73 base artworks, ~79 icons/tokens/counters (statuses, aspects,
numbers), plus aspect-pair artwork packs (Divine+Occult,
Prospection+Boreal, Arcana+Occult). User generates ADDITIONAL art in the
same style.

**Reusable style tail for ALL new CROWS art prompts** (baked into every
prompt, per the CSV export rule):
"hand-drawn dark fantasy board game illustration, bold black ink
outlines, muted desaturated colors, rough watercolor texture shading,
grim storybook style, no text"
(+ "weathered grey stone, dark green thorny vines" for board/frame
elements.)

Board art: NONE in the pack — generated MODULARLY (Hearthstone-style
swappable pieces): see mockups/crows_board.svg (module map M1–M6) and
02_Board_Art_Prompts.csv (11 prompts: base, playmat, rails, 4 corner
dioramas, 3 socket types, center emblem).

## Open decisions (user)
- Strike model: (A) armor-as-reduction vs (B) armor-as-gate (recommended).
- The 9 rules gaps in 01_Rules_Digest.md need rulings (dice-related ones
  now resolve via the deterministic translation).
