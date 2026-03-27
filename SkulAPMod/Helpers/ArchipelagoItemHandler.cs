using Data;

namespace SkulAPMod
{
    public static class ArchipelagoItemHandler
    {
        public static void GrantItem(long itemId)
        {
            switch (itemId)
            {
                case ArchipelagoConstants.GoldItem:
                    GameData.Currency.gold.Earn(ArchipelagoConstants.GoldAmount);
                    break;

                case ArchipelagoConstants.DarkQuartzItem:
                    GameData.Currency.darkQuartz.Earn(ArchipelagoConstants.DarkQuartzAmount);
                    break;

                case ArchipelagoConstants.BoneItem:
                    GameData.Currency.bone.Earn(ArchipelagoConstants.BoneAmount);
                    break;
            }
        }
    }
}
