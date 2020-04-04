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

        private bool CanSell(int amount, ItemObject item, float priceFactor, int price, int merchantGold, int inventoryCapacity)
        {
            // Don't sell anything if price is too low
            if (priceFactor < 1.2f)
                return false;

            int amountToKeep = 0;
            if (item.IsFood)
            {
                if( item == DefaultItems.Grain)
                {
                    // Keep some grain as base food
                    amountToKeep = Math.Max(10, (int)(((float)inventoryCapacity / 10.0f) / DefaultItems.Grain.Weight));
                }
                else
                {
                    // Maintain a variety of food for morale
                    amountToKeep = 5;
                }
            }

            if (amountToKeep > amount)
                return false;

            // Make sure merchant has enough gold
            if (price > merchantGold)
                return false;

            return true;
        }

        private bool CanBuy(int amount, float weight, float priceFactor, int price, int availableGold, float availableCapacity)
        {
            // Don't buy anything if the price is too high
            if (priceFactor > 0.7f)
                return false;

            int amountToKeepInStock = 7;

            // If there is too few left in stock, we don't want to buy it to keep the city healthy and to prevent overpricing
            if (amount < amountToKeepInStock)
                return false;

            if (availableGold < price || availableCapacity < weight)
                return false;

            return true;
        }

        private void AutoTradeGoodsConsequence(MenuCallbackArgs args)
        {
            //Initialize Inventory
            InventoryManager.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.Town, 
                InventoryManager.InventoryCategoryType.Goods, null);
            InventoryLogic inventoryLogic = InventoryManager.MyInventoryLogic;

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

            if (tempGold < 1)
                return;

            // List for transfer commands
            List<KeyValuePair<ItemRosterElement, float>> itemPriceFactorBuyList = new List<KeyValuePair<ItemRosterElement, float>>();

            // Own items
            foreach (ItemRosterElement itemRosterElement in PartyBase.MainParty.ItemRoster)
            {
                if (itemRosterElement.EquipmentElement.Item.IsTradeGood || itemRosterElement.EquipmentElement.Item.IsFood)
                {
                    ItemCategory itemCategory = itemRosterElement.EquipmentElement.Item.GetItemCategory();
                    float priceFactor = Settlement.CurrentSettlement.Town.MarketData.GetPriceFactor(itemCategory, true);
                    int buyoutPrice = inventoryLogic.GetItemPrice(itemRosterElement, false);
                    int totalAmount = itemRosterElement.Amount;
                    bool canSell = CanSell(totalAmount, itemRosterElement.EquipmentElement.Item, priceFactor, buyoutPrice, tempMerchantGold, inventoryCapacity);

                    while (canSell)
                    {
                        // Update temp gold and capacity
                        tempGold +=  buyoutPrice;
                        tempMerchantGold -= buyoutPrice;
                        tempWeight -= itemRosterElement.EquipmentElement.Weight;

                        // Generate command
                        TransferCommand transferCommand = TransferCommand.Transfer(1, InventoryLogic.InventorySide.PlayerInventory, InventoryLogic.InventorySide.OtherInventory,
                            itemRosterElement, EquipmentIndex.None, EquipmentIndex.None, CharacterObject.PlayerCharacter, true);

                        inventoryLogic.AddTransferCommand(transferCommand);
                        
                        // Update with new prices
                        priceFactor = Settlement.CurrentSettlement.Town.MarketData.GetPriceFactor(itemCategory, true);
                        buyoutPrice = inventoryLogic.GetItemPrice(itemRosterElement, false);
                        totalAmount -= 1;
                        canSell = CanSell(totalAmount, itemRosterElement.EquipmentElement.Item, priceFactor, buyoutPrice, tempMerchantGold, inventoryCapacity);
                    }
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

            // Sort buy List to buy best offers first if gold or capacity is unsufficient
            itemPriceFactorBuyList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

            foreach ( var pair in itemPriceFactorBuyList)
            {
                ItemCategory itemCategory = pair.Key.EquipmentElement.Item.GetItemCategory();
                int buyoutPrice = inventoryLogic.GetItemPrice(pair.Key, true);
                int totalAmount = pair.Key.Amount;
                bool canBuy = CanBuy(totalAmount, pair.Key.EquipmentElement.Weight, pair.Value, buyoutPrice, tempGold, inventoryCapacity - tempWeight);

                while (canBuy)
                {
                    // Update temp gold and capacity
                    tempGold -= buyoutPrice;
                    tempWeight += pair.Key.EquipmentElement.Weight;

                    // Generate command
                    TransferCommand transferCommand = TransferCommand.Transfer(1, InventoryLogic.InventorySide.OtherInventory, InventoryLogic.InventorySide.PlayerInventory,
                        pair.Key, EquipmentIndex.None, EquipmentIndex.None, CharacterObject.PlayerCharacter, true);

                    inventoryLogic.AddTransferCommand(transferCommand);

                    // Update price info
                    float priceFactor = Settlement.CurrentSettlement.Town.MarketData.GetPriceFactor(itemCategory, false);
                    buyoutPrice = inventoryLogic.GetItemPrice(pair.Key, true);
                    totalAmount -= 1;
                    canBuy = CanBuy(totalAmount, pair.Key.EquipmentElement.Weight, priceFactor, buyoutPrice, tempGold, inventoryCapacity - tempWeight);
                }
            }
           
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
