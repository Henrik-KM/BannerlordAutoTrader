using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace AutoTrader
{
    public static class AutoTraderSpecialRules
    {
        // Capacity
        public static bool CheckBuyMaxCapacityRule(ItemObject itemObject, int baseCapacity)
        {
            int itemIndex = PartyBase.MainParty.ItemRoster.FindIndexOfItem(itemObject);
            float stackWeightInRoster = 0f;
            if (itemIndex >= 0) //TEST
            {
                stackWeightInRoster = PartyBase.MainParty.ItemRoster.GetElementCopyAtIndex(itemIndex).GetRosterElementWeight();
            }
            if (stackWeightInRoster >= (float)baseCapacity * ((float)AutoTraderConfig.MaxCapacityValue / 100f))
            {
                return false;
            }
            return true;
        }

        // Cattle
        public static bool CheckBuyCattleCondition(ItemObject itemObject)
        {
            return AutoTraderConfig.BuyGoodsValue && itemObject.IsAnimal;
        }

        public static bool CheckBuyCattleRule()
        {
            return PartyBase.MainParty.NumberOfAllMembers >= PartyBase.MainParty.ItemRoster.NumberOfLivestockAnimals;
        }

        // Horses
        public static bool CheckBuyHorsesCondition(ItemObject itemObject)
        {
            return AutoTraderHelpers.IsHorse(itemObject);
        }

        public static bool CheckBuyHorsesRules(ItemObject itemObject, int buyoutPrice, int availablePlayerGold)
        {
            if (itemObject.HorseComponent.IsPackAnimal && AutoTraderConfig.BuyHorsesValue)
            {
                // Buy pack horses rule
                if ((int)((float)PartyBase.MainParty.NumberOfAllMembers / 2.0f) > PartyBase.MainParty.NumberOfPackAnimals && buyoutPrice * 3 < availablePlayerGold)
                    return true;
            }
            return false;
        }

        // Resupply
        public static bool CheckBuyConsumablesCondition(ItemObject itemObject)
        {
            return AutoTraderHelpers.IsConsumable(itemObject);
        }

        public static bool CheckBuyConsumablesRules(ItemObject itemObject)
        {
            if (AutoTraderConfig.ResupplyValue)
            {
                if (CheckBuyResupplyRule(itemObject))
                    return true;
            }
            return false;
        }

        private static bool CheckBuyResupplyRule(ItemObject itemObject)
        {
            // Find item stack in current inventory
            int amountInInventory = 0;
            int itemIndex = PartyBase.MainParty.ItemRoster.FindIndexOfItem(itemObject);
            if( itemIndex >= 0)
            {
                amountInInventory = PartyBase.MainParty.ItemRoster.GetElementCopyAtIndex(itemIndex).Amount;
            }

            // Resupply grain
            if (itemObject == DefaultItems.Grain)
            {
                if (amountInInventory < AutoTraderConfig.KeepGrainsValue)
                {
                    return true;
                }
                return false;
            }else
            {
                if(amountInInventory < AutoTraderConfig.KeepConsumablesValue)
                {
                    return true;
                }
                return false;
            }
        }

    }
}
