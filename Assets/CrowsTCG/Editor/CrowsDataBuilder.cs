using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using TcgEngine;
using TcgCard = TcgEngine.CardData; // BulletGames ships a global-namespace CardData that shadows the kit's

namespace CrowsTCG.EditorTools
{
    /// <summary>
    /// Builds the CROWS v0 card set on the kit's ORIGINAL stat base (user ruling:
    /// mana = actions, attack, hp; armor rides as a TraitStat read by CrowsGameLogic).
    /// One card per batch subject (94): items = Equipment, champs/minions = Character.
    /// Art = generated batch piece when it exists, else a pack Artwork fallback.
    /// Also archives the v1 (_CCG) card/deck Resources out of the load path.
    /// Idempotent: re-running overwrites the generated assets in place.
    /// </summary>
    public static class CrowsDataBuilder
    {
        const string ROOT = "Assets/CrowsTCG/Resources";
        const string GEN = "Assets/CrowsTCG/Art/Generated";
        const string PACKART = "Assets/CrowsTCG/Art/Artwork";

        static readonly string[] ASPECTS = { "arcana", "boreal", "divine", "dusk", "gale", "heat", "land", "might", "occult", "prospect" };
        static readonly Dictionary<string, string> COLORS = new Dictionary<string, string> {
            {"arcana","#003eb4"},{"boreal","#00bd8f"},{"divine","#e8bf20"},{"dusk","#5f02b7"},
            {"gale","#8cc908"},{"heat","#d34600"},{"land","#00721b"},{"might","#9e052d"},
            {"occult","#ad01bc"},{"prospect","#b78131"},{"universal","#9aa4a8"}
        };
        // pack Artwork prefix per aspect for fallback art
        static readonly Dictionary<string, string> FALLBACK_PREFIX = new Dictionary<string, string> {
            {"arcana","A"},{"dusk","D"},{"heat","H"},{"land","L"},{"might","M"},
            {"boreal","A"},{"divine","L"},{"gale","A"},{"occult","D"},{"prospect","M"},{"universal","I"}
        };

        // subjects: category|aspect|name (mirrors gen_prompts.py order)
        static readonly string[] ITEMS = {
            "arcana|staff","arcana|spellbook","arcana|astrolabe",
            "boreal|iceaxe","boreal|trident","boreal|cloak",
            "divine|mace","divine|shield","divine|censer",
            "dusk|glaive","dusk|cloak","dusk|mirror",
            "gale|longbow","gale|daggers","gale|gauntlet",
            "heat|greatsword","heat|hammer","heat|lantern",
            "land|maul","land|spear","land|shield",
            "might|greatsword","might|banner","might|gauntlets",
            "occult|scythe","occult|grimoire","occult|candelabra",
            "prospect|crossbow","prospect|scales","prospect|pick",
            "universal|shortsword","universal|buckler","universal|backpack","universal|rope"
        };
        static readonly string[] CHAMPS = {
            "arcana|archivist","arcana|sorceress","boreal|seaqueen","boreal|harpooner",
            "divine|crusader","divine|oracle","dusk|assassin","dusk|witch",
            "gale|skydancer","gale|falconer","heat|forgemaster","heat|firedancer",
            "land|druid","land|warden","might|warchief","might|duelist",
            "occult|cultleader","occult|necromancer","prospect|prince","prospect|huntress"
        };
        static readonly string[] MINIONS = {
            "arcana|grimoire","arcana|owl","arcana|scribe","arcana|sprite",
            "boreal|crab","boreal|ghoul","boreal|gull","boreal|selkie",
            "divine|cherub","divine|pilgrim","divine|hound","divine|golem",
            "dusk|cat","dusk|moths","dusk|ghost","dusk|imp",
            "gale|kitehawk","gale|wisp","gale|thief","gale|stormcrow",
            "heat|cinderimp","heat|slaghound","heat|ashwraith","heat|beetle",
            "land|boar","land|golem","land|moleknight","land|treefolk",
            "might|brawler","might|wardog","might|shieldbearer","might|berserker",
            "occult|marionette","occult|skulls","occult|acolyte","occult|leech",
            "prospect|coingolem","prospect|mule","prospect|rats","prospect|appraiser"
        };

        [MenuItem("CROWS/Build v0 Card Set")]
        public static void Run()
        {
            ArchiveV1Data();
            EnsureFolders();

            var teams = BuildTeams();
            var armor = BuildTrait("armor", "Armor");
            var champion = BuildTrait("champion", "Champion");
            var rarities = LoadRarities();

            var cards = new Dictionary<string, TcgCard>();
            int fallbackCount = 0;

            // items -> Equipment (kit adds attack/hp to the bearer)
            int[][] itemStats = { new[]{2,0}, new[]{0,2}, new[]{1,1} }; // atk,hp by index-in-aspect
            for (int i = 0; i < ITEMS.Length; i++)
            {
                var parts = ITEMS[i].Split('|');
                int idx = i % 3;
                var stats = i < 30 ? itemStats[idx] : new[] { (i % 2), 1 };
                var card = MakeCard("item_" + parts[0] + "_" + parts[1], Title(parts[0], parts[1]), CardType.Equipment,
                    teams[parts[0]], rarities[idx == 0 ? "common" : "uncommon"],
                    1, stats[0], stats[1], 0, null, ref fallbackCount);
                cards[card.id] = card;
            }

            // champions -> Character + champion trait
            int[][] champStats = { new[]{4,7,2}, new[]{3,8,3} }; // atk,hp,armor
            for (int i = 0; i < CHAMPS.Length; i++)
            {
                var parts = CHAMPS[i].Split('|');
                var s = champStats[i % 2];
                var card = MakeCard("champ_" + parts[0] + "_" + parts[1], Title(parts[0], parts[1]), CardType.Character,
                    teams[parts[0]], rarities["mythic"], 3, s[0], s[1], s[2], champion, ref fallbackCount);
                card.traits = new TraitData[] { champion };
                EditorUtility.SetDirty(card);
                cards[card.id] = card;
            }

            // minions -> Character
            int[][] minionStats = { new[]{1,2,2,1}, new[]{1,1,3,2}, new[]{2,3,2,1}, new[]{2,2,4,2} }; // cost,atk,hp,armor
            string[] minionRarity = { "common", "common", "uncommon", "rare" };
            for (int i = 0; i < MINIONS.Length; i++)
            {
                var parts = MINIONS[i].Split('|');
                var s = minionStats[i % 4];
                var card = MakeCard("minion_" + parts[0] + "_" + parts[1], Title(parts[0], parts[1]), CardType.Character,
                    teams[parts[0]], rarities[minionRarity[i % 4]], s[0], s[1], s[2], s[3], null, ref fallbackCount);
                cards[card.id] = card;
            }

            // apply armor TraitStat wherever armor > 0 (stored while making)
            foreach (var kv in pendingArmor)
            {
                var card = cards[kv.Key];
                card.stats = new TraitStat[] { new TraitStat { trait = armor, value = kv.Value } };
                EditorUtility.SetDirty(card);
            }
            pendingArmor.Clear();

            // every ORIGINAL pack artwork piece is its own card (user ruling)
            int artworkCards = BuildArtworkCards(teams, rarities, armor, champion, cards);

            // starter decks (aspect pairs, LoR model): 2 champs + minions x2 + items
            var deck1 = MakeDeck("starter_occult_might", "Talons of Ruin", new string[] {
                "champ_occult_cultleader","champ_might_warchief",
                "minion_occult_marionette","minion_occult_marionette","minion_occult_skulls","minion_occult_skulls",
                "minion_occult_acolyte","minion_occult_acolyte","minion_occult_leech","minion_occult_leech",
                "minion_might_brawler","minion_might_brawler","minion_might_wardog","minion_might_wardog",
                "minion_might_berserker","minion_might_berserker",
                "item_occult_scythe","item_might_greatsword","item_universal_shortsword","item_universal_buckler" }, cards);
            var deck2 = MakeDeck("starter_prospect_boreal", "Gilded Tide", new string[] {
                "champ_prospect_prince","champ_boreal_seaqueen",
                "minion_prospect_coingolem","minion_prospect_coingolem","minion_prospect_mule","minion_prospect_mule",
                "minion_prospect_rats","minion_prospect_rats","minion_prospect_appraiser","minion_prospect_appraiser",
                "minion_boreal_crab","minion_boreal_crab","minion_boreal_ghoul","minion_boreal_ghoul",
                "minion_boreal_selkie","minion_boreal_selkie",
                "item_prospect_pick","item_boreal_iceaxe","item_universal_backpack","item_universal_rope" }, cards);

            // gameplay wiring: CROWS numbers (mana/turn handled by CrowsGameLogic)
            var gp = AssetDatabase.LoadAssetAtPath<GameplayData>("Assets/TcgEngine/Resources/GameplayData.asset");
            if (gp == null) { Debug.LogError("CROWS: GameplayData.asset not found"); return; }
            gp.hp_start = 30;           // THE ROOST
            gp.cards_start = 4;
            gp.cards_max = 12;
            gp.deck_size = 20;
            gp.deck_duplicate_max = 2;
            gp.mulligan = false;
            gp.free_decks = new DeckData[] { deck1, deck2 };
            gp.starter_decks = new DeckData[] { deck1, deck2 };
            gp.ai_decks = new DeckData[] { deck1, deck2 };
            gp.test_deck = deck1;
            gp.test_deck_ai = deck2;
            EditorUtility.SetDirty(gp);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CROWS: v0 set built - " + cards.Count + " cards (" + artworkCards + " from pack artwork), 2 starter decks, " + fallbackCount + " art fallbacks");
        }

        // ---------- original pack artwork -> cards ----------

        static readonly Dictionary<string, string[]> ADJ = new Dictionary<string, string[]> {
            {"arcana", new[]{"Whispering","Runebound","Astral","Inkstained","Forgotten"}},
            {"boreal", new[]{"Drowned","Frostbitten","Tidal","Barnacled","Pale"}},
            {"divine", new[]{"Radiant","Anointed","Gilded","Vigilant","Solemn"}},
            {"dusk", new[]{"Moonlit","Veiled","Silent","Waning","Shrouded"}},
            {"gale", new[]{"Windswept","Screaming","Feathered","Restless","Skyborne"}},
            {"heat", new[]{"Smoldering","Charred","Molten","Ashen","Blazing"}},
            {"land", new[]{"Rooted","Mossgrown","Ancient","Thorned","Verdant"}},
            {"might", new[]{"Bloodied","Ironclad","Scarred","Brutal","Warforged"}},
            {"occult", new[]{"Cursed","Hollow","Whispered","Blasphemous","Grinning"}},
            {"prospect", new[]{"Gilded","Hoarding","Glittering","Miserly","Jeweled"}},
            {"universal", new[]{"Wandering","Weathered","Trusty","Borrowed","Plain"}}
        };
        static readonly Dictionary<string, string[]> NOUN = new Dictionary<string, string[]> {
            {"arcana", new[]{"Tome","Scribe","Cipher","Archivist","Familiar"}},
            {"boreal", new[]{"Siren","Wreck","Leviathan","Tidecaller","Gull"}},
            {"divine", new[]{"Herald","Reliquary","Warden","Chorister","Lantern"}},
            {"dusk", new[]{"Prowler","Phantom","Lullaby","Crescent","Shade"}},
            {"gale", new[]{"Zephyr","Kestrel","Vagabond","Squall","Piper"}},
            {"heat", new[]{"Ember","Stoker","Pyre","Cinder","Furnace"}},
            {"land", new[]{"Warden","Sapling","Tusker","Grovekeeper","Burrower"}},
            {"might", new[]{"Veteran","Marauder","Banner","Duelist","Vanguard"}},
            {"occult", new[]{"Effigy","Congregant","Omen","Marionette","Litany"}},
            {"prospect", new[]{"Appraiser","Vault","Prospector","Tollkeeper","Magpie"}},
            {"universal", new[]{"Satchel","Blade","Charm","Provision","Tool"}}
        };
        static readonly string[] CHAMP_ASPECTS = { "arcana","boreal","divine","dusk","gale","heat","land","might","occult","prospect" };

        static int BuildArtworkCards(Dictionary<string, TeamData> teams, Dictionary<string, RarityData> rarities,
            TraitData armor, TraitData champion, Dictionary<string, TcgCard> cards)
        {
            var jobs = new List<KeyValuePair<string, string>>(); // path -> aspect ("" = derive)
            foreach (var g in AssetDatabase.FindAssets("t:Sprite", new[] { PACKART }))
                jobs.Add(new KeyValuePair<string, string>(AssetDatabase.GUIDToAssetPath(g), ""));
            var pairs = new Dictionary<string, string[]> {
                {"Assets/CrowsTCG/Art/Aspect_Arcana_Occult", new[]{"arcana","occult"}},
                {"Assets/CrowsTCG/Art/Aspect_Divine_Occult", new[]{"divine","occult"}},
                {"Assets/CrowsTCG/Art/Aspect_Prospection_Boreal", new[]{"prospect","boreal"}}
            };
            int count = 0, champIdx = 0;
            foreach (var job in jobs)
                count += ArtworkCard(job.Key, PrefixAspect(Path.GetFileNameWithoutExtension(job.Key)), teams, rarities, armor, champion, cards, ref champIdx);
            foreach (var kv in pairs)
            {
                int i = 0;
                foreach (var g in AssetDatabase.FindAssets("t:Sprite", new[] { kv.Key }))
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    count += ArtworkCard(p, kv.Value[i % 2], teams, rarities, armor, champion, cards, ref champIdx);
                    i++;
                }
            }
            return count;
        }

        static string PrefixAspect(string name)
        {
            switch (char.ToUpper(name[0]))
            {
                case 'A': return "arcana";
                case 'C': return "champion";
                case 'D': return "dusk";
                case 'H': return "heat";
                case 'I': return "universal";
                case 'L': return "land";
                case 'M': return "might";
                default: return "universal";
            }
        }

        static int ArtworkCard(string path, string aspect, Dictionary<string, TeamData> teams, Dictionary<string, RarityData> rarities,
            TraitData armor, TraitData champion, Dictionary<string, TcgCard> cards, ref int champIdx)
        {
            string fname = Path.GetFileNameWithoutExtension(path);
            string folder = Path.GetFileName(Path.GetDirectoryName(path));
            string id = ("art_" + folder + "_" + fname).ToLower().Replace(" ", "_").Replace("-", "_");
            if (cards.ContainsKey(id)) return 0;

            bool isChamp = aspect == "champion";
            if (isChamp) { aspect = CHAMP_ASPECTS[champIdx % CHAMP_ASPECTS.Length]; champIdx++; }
            bool isItem = aspect == "universal" && fname.ToUpper().StartsWith("I");

            int h = Mathf.Abs(id.GetHashCode());
            string title = ADJ[aspect][h % 5] + " " + NOUN[aspect][(h / 5) % 5];

            CardType type;
            RarityData rarity;
            int cost, atk, hp, ac;
            if (isChamp)
            {
                type = CardType.Character; rarity = rarities["mythic"];
                cost = 3; atk = 3 + h % 2; hp = 7 + h % 3; ac = 2 + h % 2;
            }
            else if (isItem)
            {
                type = CardType.Equipment; rarity = rarities[h % 2 == 0 ? "common" : "uncommon"];
                cost = 1; atk = h % 3; hp = (h / 3) % 3; ac = 0;
            }
            else if (h % 3 == 0)
            {
                type = CardType.Spell; rarity = rarities[h % 2 == 0 ? "uncommon" : "rare"];
                cost = 1 + h % 2; atk = 0; hp = 0; ac = 0;
            }
            else
            {
                type = CardType.Character; rarity = rarities[new[] { "common", "common", "uncommon", "rare" }[h % 4]];
                cost = 1 + h % 3; atk = 1 + h % 3; hp = 1 + (h / 7) % 4; ac = h % 3;
            }

            string apath = ROOT + "/Cards/" + id + ".asset";
            var card = AssetDatabase.LoadAssetAtPath<TcgCard>(apath);
            if (card == null)
            {
                card = ScriptableObject.CreateInstance<TcgCard>();
                AssetDatabase.CreateAsset(card, apath);
            }
            card.id = id;
            card.title = title;
            card.type = type;
            card.team = teams[aspect];
            card.rarity = rarity;
            card.mana = cost;
            card.attack = atk;
            card.hp = hp;
            card.availability = CardAvailability.Collectible;
            var art = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            card.art_full = art;
            card.art_board = art;
            if (isChamp) card.traits = new TraitData[] { champion };
            if (ac > 0) card.stats = new TraitStat[] { new TraitStat { trait = armor, value = ac } };
            EditorUtility.SetDirty(card);
            cards[id] = card;
            return 1;
        }

        // ---------- helpers ----------

        static Dictionary<string, int> pendingArmor = new Dictionary<string, int>();

        static void ArchiveV1Data()
        {
            // v1 cards/decks must stop loading (kit scans ALL Resources folders)
            if (AssetDatabase.IsValidFolder("Assets/_CCG/Resources/Cards"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_CCG/DataArchive"))
                    AssetDatabase.CreateFolder("Assets/_CCG", "DataArchive");
                foreach (var sub in new[] { "Cards", "Decks", "Heroes" })
                {
                    if (AssetDatabase.IsValidFolder("Assets/_CCG/Resources/" + sub))
                    {
                        string err = AssetDatabase.MoveAsset("Assets/_CCG/Resources/" + sub, "Assets/_CCG/DataArchive/" + sub);
                        if (!string.IsNullOrEmpty(err)) Debug.LogWarning("CROWS archive: " + err);
                    }
                }
            }
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/CrowsTCG/Resources"))
                AssetDatabase.CreateFolder("Assets/CrowsTCG", "Resources");
            foreach (var f in new[] { "Teams", "Traits", "Cards", "Decks" })
                if (!AssetDatabase.IsValidFolder(ROOT + "/" + f))
                    AssetDatabase.CreateFolder(ROOT, f);
        }

        static Dictionary<string, TeamData> BuildTeams()
        {
            var dict = new Dictionary<string, TeamData>();
            var all = new List<string>(ASPECTS) { "universal" };
            foreach (var a in all)
            {
                string path = ROOT + "/Teams/" + a + ".asset";
                var team = AssetDatabase.LoadAssetAtPath<TeamData>(path);
                if (team == null)
                {
                    team = ScriptableObject.CreateInstance<TeamData>();
                    AssetDatabase.CreateAsset(team, path);
                }
                team.id = a;
                team.title = char.ToUpper(a[0]) + a.Substring(1);
                Color c; ColorUtility.TryParseHtmlString(COLORS[a], out c);
                team.color = c;
                var icon = FindAspectIcon(a);
                if (icon != null) team.icon = icon;
                EditorUtility.SetDirty(team);
                dict[a] = team;
            }
            return dict;
        }

        static Sprite FindAspectIcon(string aspect)
        {
            // icon numbers from 05_Icon_Labels.csv
            var map = new Dictionary<string, string> {
                {"arcana","31"},{"gale","34"},{"prospect","40"},{"divine","42"},{"land","45"},
                {"dusk","46"},{"might","47"},{"occult","50"},{"heat","53"},{"boreal","54"},{"universal","13"}
            };
            return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CrowsTCG/Art/Icons/" + map[aspect] + ".png");
        }

        static TraitData BuildTrait(string id, string title)
        {
            string path = ROOT + "/Traits/" + id + ".asset";
            var t = AssetDatabase.LoadAssetAtPath<TraitData>(path);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<TraitData>();
                AssetDatabase.CreateAsset(t, path);
            }
            t.id = id;
            t.title = title;
            EditorUtility.SetDirty(t);
            return t;
        }

        static Dictionary<string, RarityData> LoadRarities()
        {
            var dict = new Dictionary<string, RarityData>();
            foreach (var r in Resources.LoadAll<RarityData>(""))
                dict[r.id.ToLower()] = r;
            // kit ids: common/uncommon/rare/mythic
            return dict;
        }

        static TcgCard MakeCard(string id, string title, CardType type, TeamData team, RarityData rarity,
            int cost, int attack, int hp, int armorVal, TraitData extraTrait, ref int fallbackCount)
        {
            string path = ROOT + "/Cards/" + id + ".asset";
            var card = AssetDatabase.LoadAssetAtPath<TcgCard>(path);
            if (card == null)
            {
                card = ScriptableObject.CreateInstance<TcgCard>();
                AssetDatabase.CreateAsset(card, path);
            }
            card.id = id;
            card.title = title;
            card.type = type;
            card.team = team;
            card.rarity = rarity;
            card.mana = cost;
            card.attack = attack;
            card.hp = hp;
            card.availability = CardAvailability.Collectible;

            var art = AssetDatabase.LoadAssetAtPath<Sprite>(GEN + "/" + id + "_v1_a.png");
            if (art == null) art = AssetDatabase.LoadAssetAtPath<Sprite>(GEN + "/" + id + "_v2_a.png");
            if (art == null) { art = FallbackArt(team.id, id); fallbackCount++; }
            card.art_full = art;
            card.art_board = art;

            if (armorVal > 0) pendingArmor[id] = armorVal;
            EditorUtility.SetDirty(card);
            return card;
        }

        static Sprite FallbackArt(string aspect, string id)
        {
            string prefix = FALLBACK_PREFIX.ContainsKey(aspect) ? FALLBACK_PREFIX[aspect] : "A";
            if (id.StartsWith("item_")) prefix = "I";
            if (id.StartsWith("champ_")) prefix = "C";
            // deterministic pick: hash id over available files
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { PACKART });
            var options = new List<string>();
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileName(p).StartsWith(prefix)) options.Add(p);
            }
            if (options.Count == 0) return null;
            int h = Mathf.Abs(id.GetHashCode()) % options.Count;
            return AssetDatabase.LoadAssetAtPath<Sprite>(options[h]);
        }

        static string Title(string aspect, string name)
        {
            string a = char.ToUpper(aspect[0]) + aspect.Substring(1);
            string n = char.ToUpper(name[0]) + name.Substring(1);
            return a + " " + n;
        }

        static DeckData MakeDeck(string id, string title, string[] cardIds, Dictionary<string, TcgCard> cards)
        {
            string path = ROOT + "/Decks/" + id + ".asset";
            var deck = AssetDatabase.LoadAssetAtPath<DeckData>(path);
            if (deck == null)
            {
                deck = ScriptableObject.CreateInstance<DeckData>();
                AssetDatabase.CreateAsset(deck, path);
            }
            deck.id = id;
            deck.title = title;
            deck.hero = null; // CROWS has no hero; champions are unit cards in the deck
            var list = new List<TcgCard>();
            foreach (var cid in cardIds)
            {
                if (cards.ContainsKey(cid)) list.Add(cards[cid]);
                else Debug.LogWarning("CROWS deck " + id + ": missing card " + cid);
            }
            deck.cards = list.ToArray();
            EditorUtility.SetDirty(deck);
            return deck;
        }
    }
}
