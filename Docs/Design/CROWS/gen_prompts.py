import csv

TAIL = "hand-drawn dark fantasy board game illustration, bold black ink outlines, muted desaturated colors, rough watercolor texture shading, grim storybook style, square composition, no text"
ITEM_BG = "displayed alone, centered on a weathered dark parchment background"
CHAR_BG = "centered on a plain dark stone background"

subjects = []

def item(name, desc):
    subjects.append((name, "a single %s, %s" % (desc, ITEM_BG)))

def champ(name, desc):
    subjects.append((name, "%s, three-quarter portrait, %s" % (desc, CHAR_BG)))

def minion(name, desc):
    subjects.append((name, "%s, full body, %s" % (desc, CHAR_BG)))

# ---------- EQUIPMENT / WEAPONS (3 per aspect + 4 universal) ----------
item("item_arcana_staff", "gnarled oak wizard staff crowned with a deep blue crystal wrapped in copper wire, faint indigo runes along the shaft")
item("item_arcana_spellbook", "thick leather-bound spellbook with glowing indigo runes and brass clasps")
item("item_arcana_astrolabe", "ornate brass astrolabe amulet with rotating rings and a blue gem core")
item("item_boreal_iceaxe", "jagged battle axe with a blade of translucent teal ice and a whale-bone handle")
item("item_boreal_trident", "barnacle-crusted coral trident dripping seawater, teal and deep sea green")
item("item_boreal_cloak", "heavy sealskin cloak with frost-rimed fur trim and a carved bone clasp")
item("item_divine_mace", "winged bronze mace with a radiant sunburst head, warm gold and amber")
item("item_divine_shield", "tall tower shield embossed with a golden sun emblem, worn gilded edges")
item("item_divine_censer", "gilded incense censer on a chain, thin trail of sacred smoke")
item("item_dusk_glaive", "curved glaive with a crescent-moon blade of pale violet steel")
item("item_dusk_cloak", "hooded cloak woven from living shadow, violet-pink inner lining")
item("item_dusk_mirror", "obsidian hand mirror with a crescent moon carved into the frame")
item("item_gale_longbow", "tall yew longbow with pale green feather fletching arrows in a quiver")
item("item_gale_daggers", "pair of curved wind daggers with swirling etched blades, pale green ribbons")
item("item_gale_gauntlet", "leather falconer gauntlet with storm-grey feathers stitched along the arm")
item("item_heat_greatsword", "greatsword with a charred black blade cracked with glowing embers, burnt orange heat haze")
item("item_heat_hammer", "blackened blacksmith war hammer with a glowing molten core")
item("item_heat_lantern", "iron ember lantern leaking sparks and orange firelight")
item("item_land_maul", "massive moss-covered stone maul with a knotted wooden haft")
item("item_land_spear", "wooden spear wrapped in living thorny vines with a flint head")
item("item_land_shield", "round shield of thick bark with green moss and mushrooms growing on it")
item("item_might_greatsword", "huge notched greatsword with a blood-red wrapped hilt, battle-scarred steel")
item("item_might_banner", "spiked war banner pole with a torn dark crimson flag")
item("item_might_gauntlets", "pair of heavy iron gauntlets with brutal riveted knuckles")
item("item_occult_scythe", "bone scythe with a haft made of fused vertebrae and a violet-tinged blade")
item("item_occult_grimoire", "cursed grimoire bound in chains, a single eye embedded in the cover, violet glow")
item("item_occult_candelabra", "ritual candelabra of twisted black iron holding dripping black candles with violet flames")
item("item_prospect_crossbow", "gilded crossbow inlaid with gold coins and brass fittings, umber wood")
item("item_prospect_scales", "merchant brass balance scales with a gem on one plate and a skull on the other")
item("item_prospect_pick", "miner pickaxe with a gem-studded head and a worn leather grip")
item("item_universal_shortsword", "plain iron shortsword with a simple leather-wrapped grip")
item("item_universal_buckler", "small round wooden buckler with an iron boss, dented and scratched")
item("item_universal_backpack", "traveler leather backpack with a bedroll, tin cup and rope straps")
item("item_universal_rope", "coil of rough hemp rope with an iron grappling hook")

# ---------- CHAMPIONS (2 per aspect) ----------
champ("champ_arcana_archivist", "elderly archivist wizard in deep blue robes with floating open tomes orbiting his head, long silver beard")
champ("champ_arcana_sorceress", "young sorceress with storm-lit eyes and glowing indigo rune tattoos on her arms, deep blue cloak")
champ("champ_boreal_seaqueen", "regal sea queen in whale-bone armor with a coral crown, teal cloak like a wave")
champ("champ_boreal_harpooner", "grizzled old harpooner with a frost-crusted beard, heavy sealskin coat, harpoon over shoulder")
champ("champ_divine_crusader", "radiant crusader knight in gilded plate armor with a faint sun halo behind the helm")
champ("champ_divine_oracle", "blind oracle priestess with a golden blindfold and flowing white and amber robes, hands raised")
champ("champ_dusk_assassin", "masked assassin in violet-black garb with a crescent moon marked on the porcelain mask, twin blades")
champ("champ_dusk_witch", "pale nightmare witch with long dark hair, holding a crescent-topped staff, violet-pink mist around her feet")
champ("champ_gale_skydancer", "agile sky dancer balancing on one foot with an enormous feathered cloak spread like wings, pale green")
champ("champ_gale_falconer", "weathered old storm falconer with a hooded hawk on his gauntlet, wind-torn grey cloak")
champ("champ_heat_forgemaster", "broad forge master with ember-cracked skin and a molten hammer, leather apron, orange glow")
champ("champ_heat_firedancer", "wild fire dancer mid-spin trailing ribbons of burnt orange flame, ash-streaked clothing")
champ("champ_land_druid", "antlered druid elder in bark-and-moss robes with a gnarled staff sprouting leaves, forest green")
champ("champ_land_warden", "stone-skinned mountain warden with granite plates for shoulders, earth brown furs")
champ("champ_might_warchief", "scarred war chief with a belt of trophy skulls and a massive axe across the shoulders, blood-red war paint")
champ("champ_might_duelist", "iron-clad duelist in dented full plate saluting with a longsword, dark crimson plume")
champ("champ_occult_cultleader", "horned cult leader in black robes wearing a bone mask, violet candle flames floating around him")
champ("champ_occult_necromancer", "gaunt plague necromancer with a raven-skull staff and tattered violet burial wraps")
champ("champ_prospect_prince", "gilded merchant prince in brocade robes weighing coins on small scales, brass rings on every finger")
champ("champ_prospect_huntress", "grizzled treasure huntress with a coil of rope, gem-studded eyepatch and a gilded crossbow on her back")

# ---------- MINIONS (4 per aspect) ----------
minion("minion_arcana_grimoire", "animated grimoire with fanged pages and a leather tongue, deep blue binding")
minion("minion_arcana_owl", "solemn owl with indigo runes carved into its feathers, perched on a floating book")
minion("minion_arcana_scribe", "nervous apprentice scribe buried under a stack of scrolls, ink-stained fingers")
minion("minion_arcana_sprite", "small crystal sprite made of floating blue shards with a glowing core")
minion("minion_boreal_crab", "armored frost crab with icicle-tipped claws and a barnacled shell, teal")
minion("minion_boreal_ghoul", "drowned sailor ghoul draped in kelp and rotted netting, pale teal skin")
minion("minion_boreal_gull", "aggressive ice-feathered gull with a fish skeleton in its beak")
minion("minion_boreal_selkie", "melancholy selkie half-wrapped in a seal hide, dripping seawater")
minion("minion_divine_cherub", "chipped alabaster cherub statue come to life, holding a tiny golden trumpet")
minion("minion_divine_pilgrim", "hooded pilgrim carrying a tall lantern staff, warm amber light")
minion("minion_divine_hound", "loyal hound with a glowing sun mark on its brow, golden collar")
minion("minion_divine_golem", "temple guardian golem of white stone with gold seams and a serene carved face")
minion("minion_dusk_cat", "sleek shadow cat with violet eyes, half-dissolving into darkness")
minion("minion_dusk_moths", "swarm of pale moths forming a ghostly face, violet-pink dust")
minion("minion_dusk_ghost", "small sleepwalking child ghost in a nightgown holding a candle, translucent")
minion("minion_dusk_imp", "grinning crescent-shaped imp hanging upside down, violet skin")
minion("minion_gale_kitehawk", "hawk made of folded paper and kite string, pale green tail ribbons")
minion("minion_gale_wisp", "swirling wind wisp with leaves and feathers caught in its vortex")
minion("minion_gale_thief", "nimble sky thief crouched on a weathervane with a stolen pouch, wind-blown scarf")
minion("minion_gale_stormcrow", "large storm crow with lightning-grey feathers and a fierce eye")
minion("minion_heat_cinderimp", "mischievous cinder imp juggling embers, burnt orange skin cracked like coals")
minion("minion_heat_slaghound", "molten slag hound dripping liquid metal, glowing orange seams")
minion("minion_heat_ashwraith", "hunched ash wraith crumbling and reforming, ember eyes")
minion("minion_heat_beetle", "furnace beetle with a smoking chimney horn and glowing carapace vents")
minion("minion_land_boar", "bristling thorn boar with a hide of brambles and moss, forest green")
minion("minion_land_golem", "squat moss golem with a small tree growing from its shoulder")
minion("minion_land_moleknight", "burrowing mole knight in tiny earthen armor with a shovel-sword")
minion("minion_land_treefolk", "young treefolk sapling with a knothole face and root feet")
minion("minion_might_brawler", "shirtless pit brawler with wrapped fists and a broken nose, blood-red sash")
minion("minion_might_wardog", "armored war dog with a spiked collar and chainmail barding")
minion("minion_might_shieldbearer", "stoic shield bearer nearly hidden behind a massive tower shield")
minion("minion_might_berserker", "howling berserker mid-charge with twin hand axes, dark crimson war paint")
minion("minion_occult_marionette", "bone marionette dangling from violet threads held by an unseen hand")
minion("minion_occult_skulls", "cluster of whispering skulls stacked in a pyramid, violet candle between them")
minion("minion_occult_acolyte", "kneeling cultist acolyte in black robes offering a bowl of violet flame")
minion("minion_occult_leech", "bloated leech horror with a lamprey mouth and too many eyes, violet-black")
minion("minion_prospect_coingolem", "small golem built entirely of stacked gold coins with gem eyes")
minion("minion_prospect_mule", "stubborn pack mule loaded with treasure chests and lanterns, umber")
minion("minion_prospect_rats", "swarm of tunnel rats carrying stolen coins and a tiny gem crown")
minion("minion_prospect_appraiser", "clockwork appraiser automaton with a magnifying lens eye and brass fingers")

lines = []
manifest = []
n = 0
for name, core in subjects:
    for v, extra in (("v1", ""), ("v2", ", alternate design with a different silhouette and pose")):
        n += 1
        prompt = "%s%s, %s" % (core, extra, TAIL)
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
