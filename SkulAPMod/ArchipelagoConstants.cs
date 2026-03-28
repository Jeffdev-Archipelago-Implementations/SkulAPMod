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

        // Other items
        public const long CastleRepair = 33;

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

        // Forest of Harmony (172–209)
        public const long ForestRoom1Cleared        = 172;
        public const long ForestRoom10Cleared       = 181;
        public const long ForestShopItem1           = 202;
        public const long ForestShopItem8           = 209;
        public const long ForestMiniBossDefeated    = 242;
        public const long ForestBossDefeated        = 243;

        // Grand Hall (182–217)
        public const long GrandHallRoom1Cleared     = 182;
        public const long GrandHallRoom10Cleared    = 191;
        public const long GrandHallShopItem1        = 210;
        public const long GrandHallShopItem8        = 217;
        public const long GrandHallMiniBossDefeated = 244;
        public const long GrandHallBossDefeated     = 245;

        // The Black Lab (192–225)
        public const long BlackLabRoom1Cleared      = 192;
        public const long BlackLabRoom10Cleared     = 201;
        public const long BlackLabShopItem1         = 218;
        public const long BlackLabShopItem8         = 225;
        public const long BlackLabMiniBossDefeated  = 246;
        public const long BlackLabBossDefeated      = 247;

        // Castle Repair (226–229)
        public const long CastleRepair1             = 226;
        public const long CastleRepair4             = 229;

        // Fortress of Fate (230–241, 248)
        public const long FortressRoom1Cleared      = 230;
        public const long FortressRoom4Cleared      = 233;
        public const long FortressShopItem1         = 234;
        public const long FortressShopItem8         = 241;
        public const long FortressBossDefeated      = 248;

        // NPC locations (249–252)
        public const long FoxNpcFreed         = 249;
        public const long OgreNpcFreed        = 250;
        public const long DruidNpcFreed       = 251;
        public const long KnightNpcFreed      = 252;

        // ========== WITCH BONUS LOCATION LOOKUPS ==========
        // skull: marrowImplant(0), fastDislocation(1), nutritionSupply(2), enhanceExoskeleton(3)
        public static readonly long[] SkullBonusLocations = { MarrowTransplant1, QuickDislocation1, NutritionSupply1, ExoskeletonReinforcement1 };
        // body:  strongBone(0), fractureImmunity(1), heavyFrame(2), reassemble(3)
        public static readonly long[] BodyBonusLocations  = { ThickBone1, FracturePrevention1, HeavyFrame1, Reassemble1 };
        // soul:  soulAcceleration(0), willOfAncestor(1), fatalMind(2), ancientAlchemy(3)
        public static readonly long[] SoulBonusLocations  = { SpiritAcceleration1, AncestralFortitude1, FatalMind1, AncientAlchemy1 };

        // Room cleared base location IDs, indexed by chapter (0=Forest, 1=GrandHall, 2=BlackLab, 3=Fortress)
        public static readonly long[] ChapterRoomBaseLocations = { ForestRoom1Cleared, GrandHallRoom1Cleared, BlackLabRoom1Cleared, FortressRoom1Cleared };

        // ========== GOAL CONSTANTS ==========


        // ========== OPTION CONSTANTS ==========

    }
}
