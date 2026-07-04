namespace CCG
{
    /// <summary>
    /// Shared constants for CCG custom rules (see Docs/Design/01_Core_Rule_Engine.md)
    /// </summary>
    public static class CCGKeys
    {
        public const string TraitPitch = "pitch";      //Pitch value on cards (1-3)
        public const string TraitHandSize = "hand";    //Optional hero trait overriding hand size

        public const string TraitPitchBonus = "pitch_bonus"; //Hero trait raising the pitch allowance

        public const int DefaultHandSize = 4;          //Refill-up-to each round (v0.7.3)
        public const int PitchPerTurn = 2;             //Base pitch allowance per round (modifiable via pitch_bonus)

        //Synthetic keys stored in Game.ability_played to track per-turn pitch count
        public static string PitchKey(int player_id, int index) { return "ccg_pitch_" + player_id + "_" + index; }
        public static string PitchKeyPrefix(int player_id) { return "ccg_pitch_" + player_id + "_"; }
    }
}
