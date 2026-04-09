namespace SkulAPMod
{
    public static class ArchipelagoConstants
    {
        // ========== ITEM IDs ==========

        // Witch mastery upgrades
        public const long MarrowTransplant         = 1;
        public const long ThickBone                = 2;
        public const long FatalMind                = 3;
        public const long QuickDislocation         = 4;
        public const long FracturePrevention       = 5;
        public const long AncestralFortitude       = 6;
        public const long NutritionSupply          = 7;
        public const long HeavyFrame               = 8;
        public const long SpiritAcceleration       = 9;
        public const long ExoskeletonReinforcement = 10;
        public const long Reassemble               = 11;
        public const long AncientAlchemy           = 12;

        // NPC items
        public const long FoxNpc         = 13;
        public const long OgreNpc        = 14;
        public const long DruidNpc       = 15;
        public const long DeathKnightNpc = 16;

        // Progressive items
        public const long ProgressiveStage       = 20;
        public const long ProgressiveSkullTree   = 21;
        public const long ProgressiveBoneTree    = 22;
        public const long ProgressiveSpiritTree  = 23;

        // Currency items
        public const long BoneItem       = 30;
        public const long DarkQuartzItem = 31;
        public const long GoldItem       = 32;

        // Traps
        public const long DeSkullTrap = 40;

        // ========== CURRENCY GRANT AMOUNTS ==========

        public const int BoneAmount       = 10;
        public const int DarkQuartzAmount = 100;
        public const int GoldAmount       = 200;

        // ========== LOCATION IDs ==========

        // Witch mastery locations (100–171)
        public const long MarrowTransplant1         = 100;
        public const long MarrowTransplant10        = 109;
        public const long ThickBone1                = 110;
        public const long ThickBone10               = 119;
        public const long FatalMind1                = 120;
        public const long FatalMind10               = 129;
        public const long QuickDislocation1         = 130;
        public const long QuickDislocation10        = 139;
        public const long FracturePrevention1       = 140;
        public const long FracturePrevention10      = 149;
        public const long AncestralFortitude1       = 150;
        public const long AncestralFortitude10      = 159;
        public const long NutritionSupply1          = 160;
        public const long NutritionSupply2          = 161;
        public const long HeavyFrame1               = 162;
        public const long HeavyFrame2               = 163;
        public const long SpiritAcceleration1       = 164;
        public const long SpiritAcceleration2       = 165;
        public const long ExoskeletonReinforcement1 = 166;
        public const long ExoskeletonReinforcement2 = 167;
        public const long Reassemble1               = 168;
        public const long Reassemble2               = 169;
        public const long AncientAlchemy1           = 170;
        public const long AncientAlchemy2           = 171;

        // Shop items (8 per area) (currently unused)
        public const long ForestShopItem1           = 196;
        public const long ForestShopItem8           = 203;
        public const long GrandHallShopItem1        = 204;
        public const long GrandHallShopItem8        = 211;
        public const long BlackLabShopItem1         = 212;
        public const long BlackLabShopItem8         = 219;
        public const long FortressShopItem1         = 228;
        public const long FortressShopItem8         = 235;

        // Castle Repair (4 upgrades)
        public const long CastleRepair1             = 220;

        // Boss and mini-boss defeats
        public const long ForestMiniBossDefeated    = 236;
        public const long ForestBossDefeated        = 237;
        public const long GrandHallMiniBossDefeated = 238;
        public const long GrandHallBossDefeated     = 239;
        public const long BlackLabMiniBossDefeated  = 240;
        public const long BlackLabBossDefeated      = 241;
        public const long FortressBossDefeated      = 242;

        // NPC locations (243–246)
        public const long FoxNpcFreed               = 243;
        public const long OgreNpcFreed              = 244;
        public const long DruidNpcFreed             = 245;
        public const long KnightNpcFreed            = 246;

        // Shrine (altar) breaks — 5 per chapter (247–266)
        public const long ForestShrine1Broken       = 247;
        public const long GrandHallShrine1Broken    = 252;
        public const long BlackLabShrine1Broken     = 257;
        public const long FortressShrine1Broken     = 262;
        
        // Room cleared (8 rooms per area)
        public const long ForestRoom1Cleared        = 500;
        public const long GrandHallRoom1Cleared     = 600;
        public const long BlackLabRoom1Cleared      = 700;
        public const long FortressRoom1Cleared      = 800;
        
        // ========== WITCH BONUS KEY → ITEM ID ==========
        public static readonly System.Collections.Generic.Dictionary<string, long> BonusKeyToItemId =
            new System.Collections.Generic.Dictionary<string, long>
            {
                ["witch/skull/0"] = MarrowTransplant,
                ["witch/skull/1"] = QuickDislocation,
                ["witch/skull/2"] = NutritionSupply,
                ["witch/skull/3"] = ExoskeletonReinforcement,
                ["witch/body/0"]  = ThickBone,
                ["witch/body/1"]  = FracturePrevention,
                ["witch/body/2"]  = HeavyFrame,
                ["witch/body/3"]  = Reassemble,
                ["witch/soul/0"]  = SpiritAcceleration,
                ["witch/soul/1"]  = AncestralFortitude,
                ["witch/soul/2"]  = FatalMind,
                ["witch/soul/3"]  = AncientAlchemy,
            };

        // ========== WITCH BONUS LOCATION LOOKUPS ==========
        // skull: marrowImplant(0), fastDislocation(1), nutritionSupply(2), enhanceExoskeleton(3)
        public static readonly long[] SkullBonusLocations = { MarrowTransplant1, QuickDislocation1, NutritionSupply1, ExoskeletonReinforcement1 };
        // body:  strongBone(0), fractureImmunity(1), heavyFrame(2), reassemble(3)
        public static readonly long[] BodyBonusLocations  = { ThickBone1, FracturePrevention1, HeavyFrame1, Reassemble1 };
        // soul:  soulAcceleration(0), willOfAncestor(1), fatalMind(2), ancientAlchemy(3)
        public static readonly long[] SoulBonusLocations  = { SpiritAcceleration1, AncestralFortitude1, FatalMind1, AncientAlchemy1 };

        // Room cleared base location IDs, indexed by chapter (0=Forest, 1=GrandHall, 2=BlackLab, 3=Fortress)
        public static readonly long[] ChapterRoomBaseLocations   = { ForestRoom1Cleared,    GrandHallRoom1Cleared,    BlackLabRoom1Cleared,    FortressRoom1Cleared };

        // Shrine break base location IDs, indexed by chapter
        public static readonly long[] ChapterShrineBaseLocations = { ForestShrine1Broken, GrandHallShrine1Broken, BlackLabShrine1Broken, FortressShrine1Broken };

        // ========== GOAL CONSTANTS ==========


        // ========== OPTION CONSTANTS ==========
        public const string QuartzMultOption    = "quartz_mult";
        public const string ReqRoomCountOption  = "req_room_count";
    }
}
