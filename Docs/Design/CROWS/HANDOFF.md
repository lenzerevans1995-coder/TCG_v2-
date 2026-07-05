# CROWS TCG — Session Handoff (2026-07-04 → resume with Opus 4.8)

Project: `C:\UnityProjects\TradingCardGame\My project` (Unity 6000.5.2f1, URP)
Repo: https://github.com/lenzerevans1995-coder/TCG_v2- — **push after every chunk, always.**
Unity MCP serves TWO editors: pin to `My project@...` with set_active_instance (the other is v1 "CCG").
v1 reference project: `C:\UnityProjects\TradingCardGame\CCG` (repo lenzerevans1995-coder/CCG, tag v1-prototype).

## WHAT WORKS RIGHT NOW (all verified live, all pushed)

- **Full player loop**: CCG_Menu (Heat launcher) → auto-login → auto-granted CROWS
  starter decks → SOLO → Game scene → live match with the user's own deck.
- **CROWS rules on the server** (CrowsGameLogic, constructed by GameServer + AILogic):
  Roost 30hp, flat 3 actions/turn (kit mana = actions, no ramp), draw 4 every turn,
  whole-hand discard at end of turn, deck recycle on empty (consumed-flagged cards
  excluded), armor-as-gate strikes BOTH directions (2 vs AC2 = full dmg; 1 vs AC2 =
  zero), counter damage, exhaust, Roost strikes, win check. Multi-turn ai_vs_ai clean.
- **227-card v0 set** (CrowsTCG/Resources): 94 batch subjects + 133 original pack
  artworks (incl. 60 pair-pack pieces). 11 aspect TeamData (frame palette colors +
  pack gem icons), armor/champion TraitData, 2 starter decks (Talons of Ruin =
  occult/might, Gilded Tide = prospect/boreal). v1 + kit demo data archived out of
  Resources (DataArchive folders — NOT deleted).
- **Collection/deck builder** (v1's CCGCollectionUI in the Heat menu): 227/227 cards
  wearing CROWS pack frames via the rebuilt CCGCardFace.
- **Board tokens**: CrowsBoardToken dresses kit BoardCards — square TOKEN_FRAME +
  corners TL atk dagger / TR aspect gem / BL AC shield / BR life heart, white
  numbers black outline, kit CanvasUI chips suppressed, slot-fitted (2.05 world units).
- **Theme**: menus AND match are warm Heat palette (gold #c9a84c accent, warm
  charcoal, parchment text, blood red #8e1f2c enemy) — all neon eradicated.

## THE USER'S LAST VERDICT — TOP PRIORITY ON RESUME

The match Roost panels (top-left/bottom-left) still use **v1's hero-panel template
(WEAPON/ARMOR sub-slots, MANA bar)** — "the shit template that isn't good for what
we want." REDESIGN THEM FOR CROWS: no hero, no weapon/armor slots. A CROWS Roost
panel wants: Roost HP (30, the big number), 3 action pips, consumed-pile count,
maybe active-champion portraits. The panels are built by CCGMatchHUD (search
"Cluster_Player" / the panel build code); mock in SVG first (user rule), get
sign-off, then rebuild that section of CCGMatchHUD.

## PENDING USER INPUT

- **Art curation**: `Generated/` holds 100+ NEW cartoon-style renders (user re-ruled
  style: "cartoon graphic novel / hand drawn stylized cartoon", muted natural tones,
  no borders — prompts v3 in gen_prompts.py). DO NOT touch them — the user picks
  variants. On "import the keepers": `python Docs/Design/CROWS/import_generated.py
  --apply` (handles 3 filename schemes, letter-suffixes duplicates), then menu
  CROWS/Build v0 Card Set (builder prefers highest letter = newest render).
- **Open rulings**: strike model A(reduction)/B(gate) — B is live default, one-line
  switch in CrowsGameLogic.strike_model; deck size 19+champ vs 20; champion play
  action cost. 9 gaps listed in 01_Rules_Digest.md.

## NEXT WORK QUEUE (after the Roost panel redo)

1. Card backs: wire back_common..back_mythic (Art/Backs) as the kit cardback/variant
   system; deck piles + enemy hand still use a placeholder back.
2. Human interaction pass: drag-to-play, drag-to-attack, hover previews in match —
   only ever driven via code this session.
3. Balance sims: GameplayData.ai_vs_ai=true, n=20 games, log lengths/winrates
   (v1 pattern: PlayerPrefs turn logging).
4. Abilities: 227 cards have stats but NO abilities/spell effects yet — spells
   currently do nothing. Kit AbilityData authoring next; boons/ailments map to kit
   StatusType (icon semantics in 05_Icon_Labels.csv).
5. Board art: generate 02_Board_Art_Prompts_v3 batch (gritty->cartoon retail pass
   may be needed to match style v3), assemble modular board per crows_board.svg.
6. Fonts: TMP SDFs for Ultra (titles) + PatrickHand (card body) — ttfs in
   Assets/CrowsTCG/Fonts; card faces currently use default TMP font.

## TOOLING (editor menu items + scripts)

- `CROWS/Build v0 Card Set` — CrowsDataBuilder: idempotent teams/traits/cards/decks
  + GameplayData wiring + sprite-import fixes. Re-run after any art import.
- `CROWS/Skin Menu Scene` — rebuilds kit Menu.unity visual layer (Aria experiment;
  superseded by the Heat menu but harmless).
- `CROWS/Skin Match Scene` — CrowsMatchSkin: hue-remaps cool/neon colors in
  Game.unity + gameplay prefabs to the warm palette. Idempotent.
- `Docs/Design/CROWS/import_generated.py [--apply]` — manifest-driven rename of
  Firefly downloads into Assets/CrowsTCG/Art/Generated.
- `Docs/Design/CROWS/gen_prompts.py` — 188-line art prompt batch (style v3) +
  manifest CSV. compose_card_preview.py = measured card/token layout spec.
- Docs: 00_Pillars, 01_Rules_Digest, 02_Board_Art_Prompts_v3, 04_Art_Catalog,
  05_Icon_Labels.csv, catalog/frame_palette.json, mockups/crows_board.svg +
  crows_ui_shell.svg, CHANGELOG.md (../CHANGELOG.md).

## HARD-WON SKILLS (violate these and you will burn hours)

**Unity editor via MCP**
1. `Application.runInBackground = true` FIRST thing in every play session — the
   unfocused editor throttles frames; Heat fades crawl, network handshakes stall.
2. Check `EditorApplication.isPaused` whenever the game "ignores" actions — the
   editor pause self-engages; client actions sent while paused die silently.
3. NEVER start play mode until compile fully settles (sleep ~7s after refresh) —
   stale-domain sessions have null statics (GameClient.Get()==null while the
   component exists). If statics are null: stop, compile, replay.
4. The 40s turn timer desyncs slow tool-driven play (server plays on without you,
   then ends the game). Use GameplayData.ai_vs_ai=true for anything longer than a
   few actions; restore to false after.
5. GameplayData.Get() is NULL outside play mode — editor scripts must
   AssetDatabase.LoadAssetAtPath the asset.
6. execute_code is C#6 (CodeDom): no local functions, no ?: on mixed types, no
   FindFirstObjectByType<T> on non-UnityEngine.Object types.

**This codebase**
7. BulletGames ships a GLOBAL-namespace `CardData` that shadows TcgEngine.CardData —
   alias `using TcgCard = TcgEngine.CardData;` (CS0576 if alias named CardData) or
   fully qualify.
8. Kit objects must NEVER be SetActive(false) to hide — their Awake never runs,
   singletons never register, MainMenu NREs. Hide with CanvasGroup alpha 0.
9. Heroless decks (CROWS!) crash TWO kit paths: UserDeckData.NetworkSerialize
   (fixed — null hero placeholder) and StarterDeckPanel grant (fixed). Grep for
   `deck.hero` before trusting any new kit path.
10. Board display = SpriteRenderers on BoardCard (kit chips in child "CanvasUI");
    the CardUI canvas on it is only the hover zoom. CCGSkinBootstrap attaches all
    per-object skins globally (CCGCardUISkin to CardUIs, CrowsBoardToken to
    BoardCards) and randomizes the netcode port per session.
11. Fresh PNGs import as NON-sprite in this project (3D template): builder runs
    EnsureSpriteImports on Generated; new art folders need textureType=Sprite,
    spriteImportMode=Single before Load<Sprite> works (Multiple with no slices =
    zero sprite objects, everything renders white/invisible).
12. The neon lived in: Heat/Shift UI Manager assets (menus — edit the .asset
    colors, enableDynamicUpdate propagates), CCGMatchHUD/CCGMatchUISkin palette
    constants (match HUD), kit prefabs + scene colors (CrowsMatchSkin remap).
13. Card face geometry is MEASURED, not designed by eye: frame 1800x2520, art
    window (130,133)-(1668,1417), cost disk center (267,266), gem (232,522) —
    integral-image disk detection, naive black-pixel bbox fails (border+vines are
    black too). Token frame crop (111,478)-(1688,2041), window +117,+82/+1460,+1416.
14. Generated art carries its own drawn borders → render 10% past cover-fit under
    the mask. Pack Artwork = transparent-ground figures → contain-fit at 94% on an
    aspect-tinted dark backdrop (frame_bg).
15. Heat BoxButtonManager re-applies its serialized buttonBackground on enable —
    set sprites on the COMPONENT, not the child Image. Aria: prefabs ship their own
    TMP labels (set text, don't add); ButtonIcon root Image IS the glyph.
16. Git: raw Firefly downloads stay OUT (`/Generated/` gitignored root-only —
    plain `Generated/` swallowed Assets/CrowsTCG/Art/Generated and silently
    dropped 67 art files from a commit). LFS pushes >200MB: run in background.
17. Deleting `%LOCALAPPDATA%Low/DefaultCompany/My project/crows.user` resets the
    test account (fresh accounts now auto-receive CROWS starters on login).

## ARCHITECTURE MAP (one line each)

- CrowsGameLogic.cs (Assets/CrowsTCG/Scripts) — rules layer; StrikeModel switch.
- CrowsDataBuilder.cs / CrowsMenuSkin.cs / CrowsMatchSkin.cs (CrowsTCG/Editor).
- CrowsBoardToken.cs, CCGCardFace.cs (card+token composer), CCGCardUISkin.cs
  (+CCGSkinBootstrap), CCGMatchHUD.cs (match HUD — REDO TARGET), CCGHeatMenu.cs
  (menu flow + starter grant), CCGCollectionUI.cs (deck builder) — Assets/_CCG/Scripts.
- Scenes: _CCG/Scenes/CCG_Menu.unity (THE menu), TcgEngine/Scenes/Game/Game.unity
  (THE match, 6 slots/side @2.3 spacing), kit Menu/LoginMenu (legacy, skinned).
- Data: CrowsTCG/Resources/{Cards,Decks,Teams,Traits,Frames,Icons}; art in
  CrowsTCG/Art/{Generated,Artwork,Backs,Tokens,Aspect_*}.
- Slot.x_max=6 (kit edit); StarterDeckPanel + UserData null-hero kit edits tagged
  `CROWS-EDIT`.
