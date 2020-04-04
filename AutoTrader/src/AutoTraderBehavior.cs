using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;

namespace AutoTrader
{
    class TradeBehavior : CampaignBehaviorBase
    {

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            if (campaignGameStarter != null)
                this.AddDialogAndGameMenus(campaignGameStarter);
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            if (campaignGameStarter != null)
                this.AddDialogAndGameMenus(campaignGameStarter);
        }

        private bool AutoTradeGoodsCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return true;
        }

        private int CalculateAmountToSell(ItemRosterElement itemRosterElement, float priceFactor, int price, int merchantGold, int inventoryCapacity)
        {
            // Don't sell anything if price is too low
            if (priceFactor < 1.2f)
                return 0;

            int amountToKeep = 0;
            if (itemRosterElement.EquipmentElement.Item.IsFood)
            {
                if( itemRosterElement.EquipmentElement.Item == DefaultItems.Grain)
                {
                    // Keep some grain as base food
                    amountToKeep = Math.Max(10, (int)(((float)inventoryCapacity / 10.0f) / DefaultItems.Grain.Weight));
                }
                else
                {
                    // Maintain a variety of food for morale
                    amountToKeep = 7;
                }
            }

            if (amountToKeep > itemRosterElement.Amount)
                return 0;

            int amountToSell = itemRosterElement.Amount - amountToKeep;

            // Make sure merchant has enough gold
            if (amountToSell * price > merchantGold)
            {
                amountToSell = (int)Math.Floor((double)merchantGold / (double)price);
            }

            return amountToSell;
        }

        private int CalculateAmountToBuy(ItemRosterElement itemRosterElement, float priceFactor, int price, int availableGold, float availableCapacity)
        {
            // Don't buy anything if the price is too high
            if (priceFactor > 0.7f)
                return 0;

            int amountToKeepInStock = 7;
            int amount = itemRosterElement.Amount;
            float weight = itemRosterElement.GetRosterElementWeight();

            // If there is too few left in stock, we don't want to buy it to keep the city healthy and to prevent overpricing
            if (amount < amountToKeepInStock)
                return 0;

            // If we have enough gold and capacity buy as much as we can
            if (price * amount < availableGold && weight * amount <= availableCapacity)
                return amount - amountToKeepInStock;

            // Calculate how much we can buy
            int amountToBuyGold = (int)Math.Floor((double)availableGold / (double)price);
            int amountToBuyWeight = (int)Math.Floor((double)(availableCapacity / weight));
            int amountToBuy = Math.Min(amountToBuyGold, amountToBuyWeight);

            return Math.Min(amount - amountToKeepInStock, amountToBuy);
        }

        private void AutoTradeGoodsConsequence(MenuCallbackArgs args)
        {
            //Initialize Inventory
            InventoryManager.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.Town, 
                InventoryManager.InventoryCategoryType.Goods, null);
            InventoryLogic inventoryLogic = InventoryManager.MyInventoryLogic;
            InventoryLogic inventoryLogicBase = new InventoryLogic(Campaign.Current, Settlement.CurrentSettlement.Town.Owner);
            inventoryLogicBase.Initialize(Settlement.CurrentSettlement.ItemRoster, PartyBase.MainParty.ItemRoster, PartyBase.MainParty.MemberRoster,
                true, true, CharacterObject.PlayerCharacter, InventoryManager.InventoryCategoryType.Goods, Settlement.CurrentSettlement.Town.MarketData, true, null);

            // Initialize player info
            int troopWage = PartyBase.MainParty.MobileParty.GetTotalWage();
            int remainingFoodPercentage = PartyBase.MainParty.RemainingFoodPercentage;
            float totalWeight = PartyBase.MainParty.ItemRoster.TotalWeight;
            int inventoryCapacity = PartyBase.MainParty.MobileParty.InventoryCapacity;
            int initialGold = PartyBase.MainParty.Owner.Gold;
            int merchantGold = Settlement.CurrentSettlement.Town.Gold;

            // Make sure to have enough gold for 2 days of wages left (ToDo: 2 days of daily income)
            int tempGold = initialGold - (2 * troopWage);
            int tempMerchantGold = merchantGold;
            float tempWeight = totalWeight;

            // List for transfer commands
            List<TransferCommand> transferCommands = new List<TransferCommand>();
            List<KeyValuePair<ItemRosterElement, float>> itemPriceFactorBuyList = new List<KeyValuePair<ItemRosterElement, float>>();
            List<KeyValuePair<ItemRosterElement, float>> itemPriceFactorSellList = new List<KeyValuePair<ItemRosterElement, float>>();

            // Own items
            foreach (ItemRosterElement itemRosterElement in PartyBase.MainParty.ItemRoster)
            {
                if (itemRosterElement.EquipmentElement.Item.IsTradeGood || itemRosterElement.EquipmentElement.Item.IsFood)
                {
                    ItemCategory itemCategory = itemRosterElement.EquipmentElement.Item.GetItemCategory();
                    float priceFactor = Settlement.CurrentSettlement.Town.MarketData.GetPriceFactor(itemCategory, true);

                    itemPriceFactorSellList.Add(new KeyValuePair<ItemRosterElement, float>(itemRosterElement, priceFactor));
                }
            }

            // Merchand items
            foreach (ItemRosterElement itemRosterElement in Settlement.CurrentSettlement.ItemRoster)
            {
                if (itemRosterElement.EquipmentElement.Item.IsTradeGood || itemRosterElement.EquipmentElement.Item.IsFood)
                {
                    ItemCategory itemCategory = itemRosterElement.EquipmentElement.Item.GetItemCategory();
                    float priceFactor = Settlement.CurrentSettlement.Town.MarketData.GetPriceFactor(itemCategory, false);

                    itemPriceFactorBuyList.Add(new KeyValuePair<ItemRosterElement, float>(itemRosterElement, priceFactor));
                }
            }

            // Sell first to increade available gold and capacity
            foreach (var pair in itemPriceFactorSellList)
            {
                int buyoutPrice = inventoryLogic.GetItemPrice(pair.Key, false);
                int amountToSell = CalculateAmountToSell(pair.Key, pair.Value, buyoutPrice, tempMerchantGold, inventoryCapacity);

                if (amountToSell > 0)
                {
                    // Update temp gold and capacity
                    tempGold += amountToSell * buyoutPrice;
                    tempMerchantGold -= amountToSell * buyoutPrice;
                    tempWeight -= amountToSell * pair.Key.GetRosterElementWeight();

                    // Generate command
                    TransferCommand transferCommand = TransferCommand.Transfer(amountToSell, InventoryLogic.InventorySide.PlayerInventory, InventoryLogic.InventorySide.OtherInventory, 
                        pair.Key, EquipmentIndex.None, EquipmentIndex.None, CharacterObject.PlayerCharacter, true);

                    transferCommands.Add(transferCommand);
                }
            }

            // Sort buy List to buy best offers first if gold or capacity is unsufficient
            itemPriceFactorBuyList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

            foreach ( var pair in itemPriceFactorBuyList)
            {
                int buyoutPrice = inventoryLogic.GetItemPrice(pair.Key, true);
                int amountToBuy = CalculateAmountToBuy(pair.Key, pair.Value, buyoutPrice, tempGold, inventoryCapacity - tempWeight);

                if (amountToBuy > 0)
                {
                    // Update temp gold and capacity
                    tempGold -= amountToBuy * buyoutPrice;
                    tempWeight += amountToBuy * pair.Key.GetRosterElementWeight();

                    // Generate command
                    TransferCommand transferCommand = TransferCommand.Transfer(amountToBuy, InventoryLogic.InventorySide.OtherInventory, InventoryLogic.InventorySide.PlayerInventory,
                        pair.Key, EquipmentIndex.None, EquipmentIndex.None, CharacterObject.PlayerCharacter, true);

                    transferCommands.Add(transferCommand);
                }
            }
            
            // Process Commands
            inventoryLogic.AddTransferCommands(transferCommands);
        }

        private void AddDialogAndGameMenus(CampaignGameStarter campaignGameStarter)
        {
            if (campaignGameStarter != null)
                campaignGameStarter.AddGameMenuOption("town", "trade", "{=VN4ctHIU}Automatically trade goods",
                    new GameMenuOption.OnConditionDelegate(this.AutoTradeGoodsCondition),
                    new GameMenuOption.OnConsequenceDelegate(this.AutoTradeGoodsConsequence), false, 7, false);
        }

    }
}
