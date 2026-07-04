# CROWS TCG — Art Catalog

Everything in `Assets/CrowsTCG/Art/`, decoded 2026-07-04. Contact sheets in
`catalog/` (sheet_frames / sheet_backs / sheet_artwork / sheet_icons /
sheet_generated + the three aspect-pair pack sheets). Icon semantics:
**05_Icon_Labels.csv**. Frame colors: **catalog/frame_palette.json**.

## Frames (12) — `Art/Frames/`
One front frame per aspect + CHAMPION + UNIVERSAL. Each frame carries a
two-color identity (primary / secondary), extracted to frame_palette.json:

| Frame | Primary | Secondary |
|---|---|---|
| ARCANA | #003eb4 | #020b60 |
| BOREAL | #00bd8f | #007470 |
| CHAMPION | #b9c6cc | #365059 (manual override — silver frame) |
| DIVINE | #e8bf20 | #a98700 |
| DUSK | #5f02b7 | #170661 |
| GALE | #8cc908 | #007a48 |
| HEAT | #d34600 | #862700 |
| LAND | #00721b | #003b1e |
| MIGHT | #9e052d | #53022a |
| OCCULT | #ad01bc | #3c015f |
| PROSPECT | #b78131 | #664315 |
| UNIVERSAL | #9aa4a8 | #17181b (manual override — grey frame) |

Card anatomy (from rules docx, `catalog/docx_image60.png`):
- Top-left: cost circle (black, white number) + aspect gem(s). Champions
  show TWO aspect icons and no cost.
- Top-right: `#XX` card ID.
- Name plate: **Ultra** font.
- Text box: rules text with INLINE ICONS (→ TMP sprite atlas later).
- Bottom-left shield = Armor Class; bottom-right heart = Life.
- Bottom plate: card type line.

## Backs (4 + 5 generated) — `Art/Backs/`
- `CARD BACK VERSE 1-3`: ornate crow/octagon/10-gem backs.
- `CARD BACK VERSE 4`: the most basic (three colors) — chosen as the
  recolor base (user ruling).
- Generated rarity recolors (duotone hue-shift of VERSE 4):
  `back_common.png` (original), `back_uncommon.png` (hue 120°),
  `back_rare.png` (205°), `back_epic.png` (275°), `back_mythic.png` (45°).
  Method lives in the session scripts; sat boost keeps darks readable.

## Artwork (73) — `Art/Artwork/`
Prefix = category: **A** = Arcana (14), **C** = Champions, **D** = Dusk,
**H** = Heat, **I** = Items, **L** = Land, **M** = Might. Plus aspect-pair
packs: `Aspect_Divine_Occult/`, `Aspect_Prospection_Boreal/`,
`Aspect_Arcana_Occult/`.

## Icons (79) — `Art/Icons/`
Numbered PNGs + spritesheet. Full label map in **05_Icon_Labels.csv**.
Decoding method: the rules docx embeds the same images; icons PRECEDE
their rule text in document flow (off-by-one caught by visual check of
the dice icons), matched by 16×16 perceptual average-hash, then verified
on a contact sheet. Highlights:
- Dice: 6=d4, 7=d6, 0=d8, 3=d10, 1=d12, 2=d20 (all cut by no-dice ruling,
  kept for reference), 27=reroll.
- Card types: 4=champion, 14=minion, 41=creature, 11=continuous,
  12=equipment (LOW confidence — verify).
- Stats: 19=armor(shield), 44=life(heart), 16=action pip.
- Zones/keywords: 17=consume, 10=discard, 8=search, 18=sacrifice,
  20=saving, 21=prepare, 22=temporary, 5=unique, 15=strike.
- 13 = **blank token** (grey/black circle, user-confirmed) — generic
  counter base.
- Aspects: 31=arcana 34=gale 40=prospection 42=divine 45=land 46=dusk
  47=might 50=occult 53=heat 54=boreal.
- Boons (83-107 range): sharpened, lucky, enraged, blessed, quickened,
  inspired, hunting, focused, empowered, hidden, advantage.
- Ailments: trauma, distress, burning, entangled, stunned, smitten,
  chilled, dazed, sleeping, cursed, poisoned, disadvantage, bleeding.

## Generated tokens (24) — `Art/Tokens/`
`token_{aspect}_{circle|square}.png`, one pair per frame identity, drawn
from each frame's two palette colors in the pack's ink-ring style. SVG
sources: `Docs/Design/CROWS/tokens_svg/` (nested groups: ink ring →
border ring → panel → inner detail).

## Fonts (investigated 2026-07-04)
No font credits anywhere: not on the itch.io page
(clockworkraven.itch.io/crows-tcg-card-game-template, by Caio /
Clockwork Raven Studios), not in the pack ("Special Note to the Dev.txt"
is just a thank-you), no ttf/otf shipped. Card text is baked into the
sample images, so both fonts had to be identified visually
(catalog/font_crop.png):
- **BODY (rules text)**: rounded handwritten print — single-story a,
  hooked descender on f, bouncy baseline. Closest free match:
  **Patrick Hand** (Google Fonts, OFL) → use as the card body font.
  (Alternates if it reads wrong in TMP: Delius, Schoolbell.)
- **TITLES (name/type plates)**: distressed spiky small-caps serif
  display. NOT Ultra (Ultra is the heavy slab listed in the docx
  fontTable for document headings). Nearest-match TBD when the card
  composer lands — candidates: Windlass, Pieces of Eight; or clean-slab
  fallback Ultra.
- docx fontTable (document itself, not the cards): Ultra, Montserrat
  Medium, Press Start 2P.
- Aria GUI ships **Economica** and **Lato** (both OFL, with TMP SDF
  assets) — usable for menu/HUD chrome out of the box.

## Board art
None in the pack — generated modularly (M1-M6), prompts in
`02_Board_Art_Prompts_v2.csv`. Card art batch prompts:
`03_Card_Art_Prompts_BATCH.txt` (+ manifest CSV for renaming downloads).
