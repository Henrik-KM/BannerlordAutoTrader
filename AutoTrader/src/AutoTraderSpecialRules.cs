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
            // Checks if we have the item too often
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
                if ((int)((float)PartyBase.MainParty.NumberOfAllMembers) > PartyBase.MainParty.NumberOfPackAnimals && buyoutPrice * 2 < availablePlayerGold)
                    return true;

                // TODO: Add max herding setting
            }
            return false;
        }

        // Resupply hardwood
        public static bool CheckBuyGoodsCondition(ItemObject itemObject)
        {
            return AutoTraderHelpers.IsTradeGood(itemObject);
        }

        public static bool CheckBuyGoodsRules(ItemObject itemObject)
        {
            if (AutoTraderConfig.ResupplyHardwoodValue && itemObject == DefaultItems.HardWood)
            {
                if (CheckBuyResupplyHardwoodRule())
                    return true;
            }
            return false;
        }

        public static bool CheckBuyResupplyHardwoodRule()
        {
            // Find item stack in current inventory
            int amountInInventory = 0;
            int itemIndex = PartyBase.MainParty.ItemRoster.FindIndexOfItem(DefaultItems.HardWood);
            if (itemIndex >= 0)
            {
                amountInInventory = PartyBase.MainParty.ItemRoster.GetElementCopyAtIndex(itemIndex).Amount;
            }

            if (amountInInventory < AutoTraderConfig.KeepConsumablesMinValue)
            {
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

        /// <returns>
        /// True if the given item is below the restock value
        /// False if not
        /// </returns>
        public static bool CheckBuyResupplyRule(ItemObject itemObject)
        {
            // Find item stack in current inventory
            int amountInInventory = 0;
            int itemIndex = PartyBase.MainParty.ItemRoster.FindIndexOfItem(itemObject);
            if (itemIndex >= 0)
            {
                amountInInventory = PartyBase.MainParty.ItemRoster.GetElementCopyAtIndex(itemIndex).Amount;
            }

            // Resupply grain
            if (itemObject == DefaultItems.Grain)
            {
                if (amountInInventory < AutoTraderConfig.KeepGrainsMinValue)
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (amountInInventory < AutoTraderConfig.KeepConsumablesMinValue)
                {
                    return true;
                }
                return false;
            }
        }

    }
}
