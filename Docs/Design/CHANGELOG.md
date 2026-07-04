# CROWS TCG — Changelog

## v2.2.0 — 2026-07-04 (gritty restyle + generation tooling)

- ART STYLE RE-RULED (user): gritty graphic novel — coarse gestural
  linework, dry-brush ink, jagged edges, sponge-stippled gradients,
  desaturated greys/blacks + ONE neon glow accent per aspect (dusk =
  magenta/neon purple per user reference); backgrounds always themed,
  never empty. Supersedes the ink-storybook tail for GENERATED art
  (pack frames/icons unchanged).
- 03_Card_Art_Prompts_BATCH.txt regenerated in the new style (188
  lines; verified: line 1 render matches the reference dead-on).
  Old-style downloads parked in Generated/_oldstyle.
- 02_Board_Art_Prompts_v3.csv (+ _BATCH.txt + manifest): 11 board
  modules restyled to match; Roost orb socket prompt added, ratios
  noted per module (base/playmat WIDE, rail TALL, rest SQUARE).
- import_generated.py: renames firefly_*_<line>_var<V>.png downloads
  against the manifest into Assets/CrowsTCG/Art/Generated (dry-run
  default, --apply to move). Verified on the first download.
- Fonts landed in Assets/CrowsTCG/Fonts: PatrickHand-Regular (OFL,
  card body) + Ultra-Regular (Apache, docx heading font / title
  fallback), licenses included.


Continues the process rules from v1 (repo lenzerevans1995-coder/CCG, tag
v1-prototype): commit+push every chunk; SVG mockups before layout code;
art prompts exported as CSV with the CROWS style tail.

## v2.1.0 — 2026-07-04 (art decode + generation pipeline)

- Full CROWS pack decode: 04_Art_Catalog.md (frames/backs/artwork/
  fonts), 05_Icon_Labels.csv (icon semantics via docx perceptual-hash
  matching — icons PRECEDE their rule text; 13 = blank token, user-
  confirmed), catalog/frame_palette.json (two colors per frame,
  CHAMPION + UNIVERSAL manually overridden).
- Generated: 24 aspect tokens (circle+square per frame, nested SVG
  sources in tokens_svg/) + 5 rarity backs recolored from CARD BACK
  VERSE 4 (common/uncommon/rare/epic/mythic duotone hue-shift).
- Card-art batch: 03_Card_Art_Prompts_BATCH.txt — 188 lines (94
  subjects x 2 variations: 34 equipment, 20 champions, 40 minions),
  one prompt per line for Firefly batch, square 1:1, CROWS style tail;
  manifest CSV maps line -> asset name for renaming downloads. Firefly
  downloads land in Generated/ (session watcher auto-processes).
- Fonts: no credits shipped with the pack (itch: clockworkraven).
  Body = handwritten print, closest free = Patrick Hand (OFL).
  Titles = distressed display (NOT Ultra), nearest-match TBD.
  Aria ships Economica + Lato TMP SDFs for menu chrome.
- Aria GUI (Assets/Honeti/AriaGUI) inventoried: sprite kit, no code —
  98 prefabs (buttons/panels/bars/texts), 1698 textures, 1 demo scene.
  UI shell mockup: mockups/crows_ui_shell.svg (HOME hub + deck builder
  + Aria->CROWS component map; match screen stays in crows_board.svg).

## v2.0.0 — 2026-07-04 (project bootstrap)

- Fresh project ("My project", Unity 6000.5.2f1, URP) chosen over the v1
  repo (user call): the CROWS art pack lives here and v1 stays untouched
  as a reference. Repo: lenzerevans1995-coder/My-project.
- CROWS art pack copied into Assets/CrowsTCG/Art (Frames = 10 aspect
  fronts + champion, Backs x4, Artwork x73, Icons x79, three aspect-pair
  artwork sets). 228 PNGs, LFS-tracked.
- TcgEngine kit copied from v1 WITH the pure-kit CCG-EDITs kept (starter
  panel guards, SelectorType.Defense enum, CardSelector tweaks) and all
  v1-specific references stripped (GameServer/AILogic point at stock
  GameLogic until CrowsGameLogic lands; CardPreviewUI/PlayerControls v1
  hooks removed). Plugins (outline) copied.
- Packages merged: netcode.gameobjects, 2d.sprite, postprocessing,
  unity-mcp (so the editor can be driven from the session).
- Docs/Design/CROWS migrated (pillars+plan, rules digest, board mockup,
  board art prompt CSVs).
- NEXT: open the project once to import; UI redo direction (CROWS
  ink-stone theme — approach decision pending); CrowsGameLogic (actions,
  strike model pending ruling A/B); board scene from crows_board.svg.
