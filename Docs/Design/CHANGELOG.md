# CROWS TCG — Changelog

Continues the process rules from v1 (repo lenzerevans1995-coder/CCG, tag
v1-prototype): commit+push every chunk; SVG mockups before layout code;
art prompts exported as CSV with the CROWS style tail.

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
