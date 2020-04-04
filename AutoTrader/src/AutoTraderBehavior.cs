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

        private int CalculateAmountToSell(int amount, ItemObject item, float priceFactor, int price, int merchantGold, int inventoryCapacity)
        {
            // Don't sell anything if price is too low
            if (priceFactor < 1.2f)
                return 0;

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
                    amountToKeep = 7;
                }
            }

            if (amountToKeep > amount)
                return 0;

            int amountToSell = amount - amountToKeep;

            // Make sure merchant has enough gold
            if (amountToSell * price > merchantGold)
            {
                amountToSell = (int)Math.Floor((double)merchantGold / (double)price);
            }

            return amountToSell;
        }

        private int CalculateAmountToBuy(int amount, float weight, float priceFactor, int price, int availableGold, float availableCapacity)
        {
            // Don't buy anything if the price is too high
            if (priceFactor > 0.7f)
                return 0;

            int amountToKeepInStock = 7;

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
                    int amountToSell = CalculateAmountToSell(totalAmount, itemRosterElement.EquipmentElement.Item, priceFactor, buyoutPrice, tempMerchantGold, inventoryCapacity);

                    while (amountToSell > 0)
                    {
                        // sell a quarter of the amountToSell
                        int amount = 1; // Math.Max(1, (int)((float)amountToSell / 4.0f));

                        // Update temp gold and capacity
                        tempGold += amount * buyoutPrice;
                        tempMerchantGold -= amount * buyoutPrice;
                        tempWeight -= amount * itemRosterElement.GetRosterElementWeight();

                        // Generate command
                        TransferCommand transferCommand = TransferCommand.Transfer(amount, InventoryLogic.InventorySide.PlayerInventory, InventoryLogic.InventorySide.OtherInventory,
                            itemRosterElement, EquipmentIndex.None, EquipmentIndex.None, CharacterObject.PlayerCharacter, true);

                        inventoryLogic.AddTransferCommand(transferCommand);
                        
                        // Update with new prices
                        priceFactor = Settlement.CurrentSettlement.Town.MarketData.GetPriceFactor(itemCategory, true);
                        buyoutPrice = inventoryLogic.GetItemPrice(itemRosterElement, false);
                        totalAmount -= amount;
                        amountToSell = CalculateAmountToSell(totalAmount, itemRosterElement.EquipmentElement.Item, priceFactor, buyoutPrice, tempMerchantGold, inventoryCapacity);
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
                int amountToBuy = CalculateAmountToBuy(totalAmount, pair.Key.GetRosterElementWeight(), pair.Value, buyoutPrice, tempGold, inventoryCapacity - tempWeight);

                while (amountToBuy > 0)
                {
                    // buy only a quarter
                    int amount = 1;// Math.Max(1, (int)((float)amountToBuy / 4.0f));

                    // Update temp gold and capacity
                    tempGold -= amount * buyoutPrice;
                    tempWeight += amount * pair.Key.GetRosterElementWeight();

                    // Generate command
                    TransferCommand transferCommand = TransferCommand.Transfer(amount, InventoryLogic.InventorySide.OtherInventory, InventoryLogic.InventorySide.PlayerInventory,
                        pair.Key, EquipmentIndex.None, EquipmentIndex.None, CharacterObject.PlayerCharacter, true);

                    inventoryLogic.AddTransferCommand(transferCommand);

                    // Update price info
                    float priceFactor = Settlement.CurrentSettlement.Town.MarketData.GetPriceFactor(itemCategory, false);
                    buyoutPrice = inventoryLogic.GetItemPrice(pair.Key, true);
                    totalAmount -= amount;
                    amountToBuy = CalculateAmountToBuy(totalAmount, pair.Key.GetRosterElementWeight(), pair.Value, buyoutPrice, tempGold, inventoryCapacity - tempWeight);
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
