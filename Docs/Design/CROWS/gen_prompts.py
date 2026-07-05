import csv

# ---- CROWS card-art batch v2: gritty graphic-novel style (user direction 2026-07-04) ----
# Style reference prompt (user): shadowy hooded rogue archer... raw coarse gestural
# linework, heavy dry-brush ink, jagged edges, sponge-stippled background gradient,
# desaturated greys/blacks + one vibrant neon glow accent. Backgrounds NOT empty.

STYLE = ("cartoon graphic novel style, raw coarse gestural linework, "
         "heavy dry-brush ink strokes, jagged edges, deep analog texture, sponge-stippled "
         "background gradient, limited desaturated color palette of muted natural tones")
END = ("high contrast, square composition, no text, "
       "no border, no frame, full-bleed edge-to-edge artwork, "
       "make sure it is a hand drawn stylized cartoon")

# per-aspect neon glow accent (derived from frame palette, pushed to neon)
ACCENT = {
    "arcana":    "electric cobalt blue",
    "boreal":    "glacial teal",
    "divine":    "blazing golden yellow",
    "dusk":      "magenta and neon purple",
    "gale":      "acid neon green",
    "heat":      "searing ember orange",
    "land":      "luminous mossy green",
    "might":     "burning blood red",
    "occult":    "sickly neon violet",
    "prospect":  "molten amber gold",
    "universal": "cold pale white",
}

# per-aspect themed environment (backgrounds must match the theme, never empty)
ENV = {
    "arcana":    "in a ruined library with drifting torn pages and floating dust",
    "boreal":    "on a frozen shoreline under a black storm sky",
    "divine":    "in a crumbling cathedral pierced by shafts of light",
    "dusk":      "on moonlit fog-drowned rooftops",
    "gale":      "on a storm-torn cliff edge with wind-whipped debris",
    "heat":      "in a smoldering forge choked with ash and embers",
    "land":      "in a gnarled ancient forest of twisted roots",
    "might":     "on a war-torn battlefield of smoke and broken banners",
    "occult":    "in a candlelit ritual crypt strewn with bones",
    "prospect":  "in a torchlit mine vault spilling with hoarded treasure",
    "universal": "at an abandoned roadside camp swallowed by fog",
}

subjects = []

def aspect_of(name):
    return name.split("_")[1]

def item(name, desc):
    a = aspect_of(name)
    core = ("a single %s, resting %s, %s, high-contrast vibrant glowing %s light accents "
            "for the enchanted details" % (desc, ENV[a], STYLE, ACCENT[a]))
    subjects.append((name, core))

def champ(name, desc):
    a = aspect_of(name)
    core = ("%s, three-quarter portrait %s, %s, high-contrast vibrant glowing %s light "
            "accents for the eyes and magical effects" % (desc, ENV[a], STYLE, ACCENT[a]))
    subjects.append((name, core))

def minion(name, desc):
    a = aspect_of(name)
    core = ("%s, full body %s, %s, high-contrast vibrant glowing %s light accents for the "
            "eyes and magical effects" % (desc, ENV[a], STYLE, ACCENT[a]))
    subjects.append((name, core))

# ---------- EQUIPMENT / WEAPONS (3 per aspect + 4 universal) ----------
item("item_arcana_staff", "gnarled oak wizard staff crowned with a glowing crystal wrapped in copper wire, runes flaring along the shaft")
item("item_arcana_spellbook", "thick leather-bound spellbook with glowing runes and brass clasps, pages fluttering")
item("item_arcana_astrolabe", "ornate astrolabe amulet with rotating rings and a glowing gem core")
item("item_boreal_iceaxe", "jagged battle axe with a blade of translucent glowing ice and a whale-bone handle")
item("item_boreal_trident", "barnacle-crusted coral trident dripping glowing seawater")
item("item_boreal_cloak", "heavy sealskin cloak with frost-rimed fur trim and a carved bone clasp, frost glowing along the hem")
item("item_divine_mace", "winged bronze mace with a radiant glowing sunburst head")
item("item_divine_shield", "tall tower shield embossed with a glowing sun emblem, battered gilded edges")
item("item_divine_censer", "incense censer on a chain leaking a glowing trail of sacred smoke")
item("item_dusk_glaive", "curved glaive with a glowing crescent-moon blade")
item("item_dusk_cloak", "hooded cloak woven from living shadow with a glowing inner lining")
item("item_dusk_mirror", "obsidian hand mirror with a glowing crescent moon carved into the frame")
item("item_gale_longbow", "tall yew longbow with glowing fletched arrows in a quiver")
item("item_gale_daggers", "pair of curved wind daggers with glowing etched blades")
item("item_gale_gauntlet", "leather falconer gauntlet with storm-grey feathers and glowing talon marks")
item("item_heat_greatsword", "greatsword with a charred black blade cracked with glowing embers, heat haze rising")
item("item_heat_hammer", "blackened blacksmith war hammer with a glowing molten core")
item("item_heat_lantern", "iron ember lantern leaking sparks and glowing firelight")
item("item_land_maul", "massive moss-covered stone maul with a knotted wooden haft, glowing spores drifting off the moss")
item("item_land_spear", "wooden spear wrapped in living thorny vines with a flint head, glowing sap in the vine veins")
item("item_land_shield", "round shield of thick bark with glowing mushrooms growing on it")
item("item_might_greatsword", "huge notched greatsword rammed into the ground, glowing runes of war along the fuller")
item("item_might_banner", "spiked war banner pole with a torn flag, glowing war sigil burned into the cloth")
item("item_might_gauntlets", "pair of heavy iron gauntlets with brutal riveted knuckles, glowing seams between the plates")
item("item_occult_scythe", "bone scythe with a haft of fused vertebrae and a glowing-edged blade")
item("item_occult_grimoire", "cursed grimoire bound in chains with a single glowing eye embedded in the cover")
item("item_occult_candelabra", "ritual candelabra of twisted black iron holding dripping black candles with glowing flames")
item("item_prospect_crossbow", "gilded crossbow inlaid with coins and brass fittings, glowing gem sight")
item("item_prospect_scales", "brass balance scales with a glowing gem on one plate and a skull on the other")
item("item_prospect_pick", "miner pickaxe with a glowing gem-studded head and a worn leather grip")
item("item_universal_shortsword", "plain iron shortsword with a leather-wrapped grip, faint glowing edge")
item("item_universal_buckler", "small round wooden buckler with an iron boss, dented and scratched, faint glowing rim")
item("item_universal_backpack", "traveler leather backpack with a bedroll, tin cup and rope straps, a faint glowing trinket hanging from a strap")
item("item_universal_rope", "coil of rough hemp rope with an iron grappling hook, hook edge faintly glowing")

# ---------- CHAMPIONS (2 per aspect) ----------
champ("champ_arcana_archivist", "elderly archivist wizard in tattered robes with floating open tomes orbiting his head, long wild beard, glowing script swirling in the air")
champ("champ_arcana_sorceress", "young sorceress with glowing eyes and glowing rune tattoos coiling up her arms, torn cloak snapping")
champ("champ_boreal_seaqueen", "regal sea queen in whale-bone armor with a coral crown, cloak breaking like a wave, glowing frost crown")
champ("champ_boreal_harpooner", "grizzled old harpooner with a frost-crusted beard and heavy sealskin coat, glowing harpoon head over his shoulder")
champ("champ_divine_crusader", "battle-worn crusader knight in scarred plate armor with a glowing halo of light behind the helm")
champ("champ_divine_oracle", "blind oracle priestess with a glowing blindfold and tattered flowing robes, hands raised")
champ("champ_dusk_assassin", "shadowy masked assassin with a glowing crescent moon marked on the porcelain mask, twin blades trailing glowing energy")
champ("champ_dusk_witch", "pale nightmare witch with long wild hair holding a crescent-topped staff, glowing mist coiling around her feet")
champ("champ_gale_skydancer", "agile sky dancer balancing on one foot with an enormous feathered cloak spread like wings, glowing wind currents spiraling around her")
champ("champ_gale_falconer", "weathered old storm falconer with a hooded hawk on his gauntlet, wind-torn cloak, glowing storm eyes")
champ("champ_heat_forgemaster", "broad forge master with ember-cracked skin and a molten hammer, leather apron, glowing veins of fire")
champ("champ_heat_firedancer", "wild fire dancer mid-spin trailing ribbons of glowing flame, ash-streaked clothing")
champ("champ_land_druid", "antlered druid elder in bark-and-moss robes with a gnarled staff, glowing sap veins in the antlers")
champ("champ_land_warden", "stone-skinned mountain warden with granite plates for shoulders, glowing cracks running through the stone")
champ("champ_might_warchief", "scarred war chief with a belt of trophy skulls and a massive axe across the shoulders, glowing war paint")
champ("champ_might_duelist", "iron-clad duelist in dented full plate saluting with a longsword, glowing plume and visor slit")
champ("champ_occult_cultleader", "horned cult leader in black robes wearing a bone mask, glowing candle flames orbiting him")
champ("champ_occult_necromancer", "gaunt plague necromancer with a raven-skull staff and tattered burial wraps, glowing skull eyes")
champ("champ_prospect_prince", "gilded merchant prince in brocade robes weighing glowing coins on small scales, rings on every finger")
champ("champ_prospect_huntress", "grizzled treasure huntress with a coil of rope, glowing gem-studded eyepatch and a crossbow on her back")

# ---------- MINIONS (4 per aspect) ----------
minion("minion_arcana_grimoire", "animated grimoire with fanged pages and a leather tongue, glowing script leaking from its maw")
minion("minion_arcana_owl", "solemn owl with glowing runes carved into its feathers, perched on a floating book")
minion("minion_arcana_scribe", "nervous apprentice scribe buried under a stack of scrolls, glowing ink stains on his fingers")
minion("minion_arcana_sprite", "small crystal sprite made of floating shards with a glowing core")
minion("minion_boreal_crab", "armored frost crab with glowing icicle-tipped claws and a barnacled shell")
minion("minion_boreal_ghoul", "drowned sailor ghoul draped in kelp and rotted netting, glowing dead eyes")
minion("minion_boreal_gull", "aggressive ice-feathered gull with a fish skeleton in its beak, glowing frost breath")
minion("minion_boreal_selkie", "melancholy selkie half-wrapped in a seal hide, glowing droplets running off the fur")
minion("minion_divine_cherub", "chipped alabaster cherub statue come to life holding a tiny trumpet, glowing seams in the cracked stone")
minion("minion_divine_pilgrim", "hooded pilgrim carrying a tall lantern staff casting glowing light")
minion("minion_divine_hound", "loyal hound with a glowing sun mark on its brow")
minion("minion_divine_golem", "temple guardian golem of white stone with glowing seams and a serene carved face")
minion("minion_dusk_cat", "sleek shadow cat with glowing eyes, half-dissolving into darkness")
minion("minion_dusk_moths", "swarm of pale moths forming a ghostly face, glowing wing dust")
minion("minion_dusk_ghost", "small sleepwalking child ghost in a nightgown holding a candle, glowing translucent edges")
minion("minion_dusk_imp", "grinning crescent-shaped imp hanging upside down, glowing grin and eyes")
minion("minion_gale_kitehawk", "hawk made of folded paper and kite string, glowing tail ribbons snapping in the wind")
minion("minion_gale_wisp", "swirling wind wisp with leaves and feathers caught in its glowing vortex")
minion("minion_gale_thief", "nimble sky thief crouched on a weathervane with a stolen pouch, glowing scarf trailing")
minion("minion_gale_stormcrow", "large storm crow with lightning-grey feathers and a glowing fierce eye")
minion("minion_heat_cinderimp", "mischievous cinder imp juggling glowing embers, skin cracked like coals")
minion("minion_heat_slaghound", "molten slag hound dripping liquid metal, glowing seams and jaws")
minion("minion_heat_ashwraith", "hunched ash wraith crumbling and reforming, glowing ember eyes")
minion("minion_heat_beetle", "furnace beetle with a smoking chimney horn and glowing carapace vents")
minion("minion_land_boar", "bristling thorn boar with a hide of brambles and moss, glowing eyes and sap-dripping tusks")
minion("minion_land_golem", "squat moss golem with a small tree growing from its shoulder, glowing spores drifting around it")
minion("minion_land_moleknight", "burrowing mole knight in earthen armor with a shovel-sword, glowing lantern helm")
minion("minion_land_treefolk", "young treefolk sapling with a knothole face and root feet, glowing sap in the bark seams")
minion("minion_might_brawler", "shirtless pit brawler with wrapped fists and a broken nose, glowing fighting spirit around the knuckles")
minion("minion_might_wardog", "armored war dog with a spiked collar and chainmail barding, glowing eyes")
minion("minion_might_shieldbearer", "stoic shield bearer nearly hidden behind a massive tower shield, glowing sigil on the shield face")
minion("minion_might_berserker", "howling berserker mid-charge with twin hand axes, glowing war paint streaking his face")
minion("minion_occult_marionette", "bone marionette dangling from glowing threads held by an unseen hand")
minion("minion_occult_skulls", "cluster of whispering skulls stacked in a pyramid around a glowing candle")
minion("minion_occult_acolyte", "kneeling cultist acolyte in black robes offering a bowl of glowing flame")
minion("minion_occult_leech", "bloated leech horror with a lamprey mouth and too many glowing eyes")
minion("minion_prospect_coingolem", "small golem built entirely of stacked coins with glowing gem eyes")
minion("minion_prospect_mule", "stubborn pack mule loaded with treasure chests and a glowing lantern")
minion("minion_prospect_rats", "swarm of tunnel rats carrying stolen coins and a tiny glowing gem crown")
minion("minion_prospect_appraiser", "clockwork appraiser automaton with a glowing magnifying lens eye and brass fingers")

lines = []
manifest = []
n = 0
for name, core in subjects:
    for v, extra in (("v1", ""), ("v2", ", alternate design with a different silhouette and pose")):
        n += 1
        prompt = "%s%s, %s" % (core, extra, END)
        lines.append(prompt)
        manifest.append((n, "%s_%s" % (name, v), prompt))

txt_path = r"C:\UnityProjects\TradingCardGame\My project\Docs\Design\CROWS\03_Card_Art_Prompts_BATCH.txt"
with open(txt_path, "w", encoding="utf-8") as f:
    f.write("\n".join(lines) + "\n")

csv_path = r"C:\UnityProjects\TradingCardGame\My project\Docs\Design\CROWS\03_Card_Art_Prompts_manifest.csv"
with open(csv_path, "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f)
    w.writerow(["line", "name", "full_prompt"])
    w.writerows(manifest)

print("subjects=%d lines=%d" % (len(subjects), len(lines)))
