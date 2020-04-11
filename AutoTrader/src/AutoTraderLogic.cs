using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;

namespace AutoTrader
{
    public static class AutoTraderLogic
    {
        private static bool _isTown;
        private static bool _isBuying;

        private static InventoryLogic _inventoryLogic;
        private static IEnumerable<ItemRosterElement> _locks;

        private static int _baseCapacity;
        private static int _availableInventoryCapacity;
        private static int _availablePlayerGold;
        private static int _availableMerchantGold;

        private static bool _sellSmithing;
        private static bool _buyHorses;
        private static bool _sellHorses;
        private static bool _buyArmor;
        private static bool _sellArmor;
        private static bool _buyWeapons;
        private static bool _sellWeapons;
        private static bool _buyConsumables;
        private static bool _sellConsumables;
        private static bool _buyGoods;
        private static bool _sellGoods;

        public static void PerformAutoTrade()
        {
            _isTown = Settlement.CurrentSettlement.IsTown ? true : false;
            if (!InitializeInventory()) return;
            InitializeMembers();

            if (_isTown)
            {
                Sell();
                Buy();
                BuyHorses();
                _inventoryLogic.RemoveZeroCounts();
            }
            else
            {
                Buy();
                Sell();
                Buy();
                BuyHorses();
                _inventoryLogic.RemoveZeroCounts();
            }
        }

        private static bool InitializeInventory()
        {
            if (_isTown)
            {
                InventoryManager.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.GetComponent<Town>(),
                    InventoryManager.InventoryCategoryType.None, null);
                return true;
            }
            else if (Settlement.CurrentSettlement.IsVillage)
            {
                InventoryManager.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.Village, InventoryManager.InventoryCategoryType.None, null);
                return true;
            }
            return false;
        }

        private static void InitializeMembers()
        {
            // Inventory logic
            _inventoryLogic = InventoryManager.MyInventoryLogic;

            // Inventory capacity
            _baseCapacity = PartyBase.MainParty.MobileParty.InventoryCapacity;
            float currentWeight = PartyBase.MainParty.ItemRoster.TotalWeight;
            _availableInventoryCapacity = (int)((float)_baseCapacity * ((float)AutoTraderConfig.UseInventorySpaceValue / 100.0f));
            _availableInventoryCapacity -= (int)currentWeight;

            // Player gold
            int initialGold = PartyBase.MainParty.Owner.Gold;
            int troopWage = PartyBase.MainParty.MobileParty.GetTotalWage(); // ToDo: whole daily 
            _availablePlayerGold = initialGold - (AutoTraderConfig.KeepWagesValue * troopWage);

            // Merchant gold
            _availableMerchantGold = _isTown ? Settlement.CurrentSettlement.Town.Gold : Settlement.CurrentSettlement.Village.Gold;

            // Locks
            _locks = Campaign.Current.GetCampaignBehavior<InventoryLockTracker>().GetLocks();

            // Read from config
            _sellSmithing = AutoTraderConfig.SellSmithingValue;
            _buyHorses = AutoTraderConfig.BuyHorsesValue;
            _sellHorses = AutoTraderConfig.SellHorsesValue;
            _buyArmor = AutoTraderConfig.BuyArmorValue;
            _sellArmor = AutoTraderConfig.SellArmorValue;
            _buyWeapons = AutoTraderConfig.BuyWeaponsValue;
            _sellWeapons = AutoTraderConfig.SellWeaponsValue;
            _buyConsumables = AutoTraderConfig.BuyConsumablesValue;
            _sellConsumables = AutoTraderConfig.SellConsumablesValue;
            _buyGoods = AutoTraderConfig.BuyGoodsValue;
            _sellGoods = AutoTraderConfig.SellGoodsValue;
    }

        private static void Sell()
        {
            _isBuying = false;

            // Loop through all items in inventory
            ItemRoster playerItemRoster = PartyBase.MainParty.ItemRoster;
            foreach (ItemRosterElement itemRosterElement in playerItemRoster)
            {
                // Check if its filtered
                if (IsItemFiltered(itemRosterElement)) continue;

                int amount = itemRosterElement.Amount;
                float averagePrice = GetAveragePrice(itemRosterElement);

                // Sell items one by one
                bool canSell = false;
                do
                {
                    int buyout_price = 0;
                    canSell = CanSell(itemRosterElement, averagePrice, amount, out buyout_price);

                    if (canSell)
                    {
                        // Update members
                        ProcessTransaction(itemRosterElement, buyout_price);
                        amount--;
                    }

                } while (canSell);
            }
        }

        private static void Buy()
        {
            _isBuying = true;

            var itemBuyList = new List<KeyValuePair<ItemRosterElement, KeyValuePair<float, float>>>();

            // Loop through all items of merchant
            ItemRoster merchantItemRoster = Settlement.CurrentSettlement.ItemRoster;
            foreach (ItemRosterElement itemRosterElement in merchantItemRoster)
            {
                // Check if its filtered
                if (IsItemFiltered(itemRosterElement)) continue;

                float averagePrice = GetAveragePrice(itemRosterElement);
                int buyoutPrice = _inventoryLogic.GetItemPrice(itemRosterElement, _isBuying);
                float profit = averagePrice - (float)buyoutPrice;

                itemBuyList.Add(new KeyValuePair<ItemRosterElement, KeyValuePair<float, float>> (
                    itemRosterElement, new KeyValuePair<float, float>(averagePrice, profit)));
            }

            // Sort the list
            itemBuyList.Sort((pair1, pair2) => pair2.Value.Value.CompareTo(pair1.Value.Value));

            // Buy items in order of profit
            foreach (var element in itemBuyList)
            {
                ItemRosterElement itemRosterElement = element.Key;
                float averagePrice = element.Value.Key;
                int amount = itemRosterElement.Amount;

                // Sell items one by one
                bool canBuy = false;
                do
                {
                    int buyout_price = 0;
                    canBuy = CanBuy(itemRosterElement, averagePrice, amount, out buyout_price);

                    if (canBuy)
                    {
                        // Update members
                        ProcessTransaction(itemRosterElement, buyout_price);
                        amount--;
                    }

                } while (canBuy);
            }
        }

        private static void ProcessTransaction(ItemRosterElement itemRosterElement, int buyoutPrice)
        {
            ItemObject itemObject = itemRosterElement.EquipmentElement.Item;

            // Update available gold
            _availablePlayerGold += _isBuying ? -buyoutPrice : buyoutPrice;
            _availableMerchantGold += _isBuying ? buyoutPrice : -buyoutPrice;

            // Generate command
            TransferCommand transferCommand = TransferCommand.Transfer(1, 
                _isBuying ? InventoryLogic.InventorySide.OtherInventory : InventoryLogic.InventorySide.PlayerInventory, 
                _isBuying ? InventoryLogic.InventorySide.PlayerInventory : InventoryLogic.InventorySide.OtherInventory, 
                itemRosterElement, EquipmentIndex.None, EquipmentIndex.None, CharacterObject.PlayerCharacter, true);

            // Update available weight
            _availableInventoryCapacity = (int)((float)PartyBase.MainParty.InventoryCapacity * (AutoTraderConfig.UseInventorySpaceValue / 100.0f)) - (int)PartyBase.MainParty.ItemRoster.TotalWeight;

            _inventoryLogic.AddTransferCommand(transferCommand);
        }

        private static void BuyHorses()
        {
            _isBuying = true;

            ItemRoster merchantItemRoster = Settlement.CurrentSettlement.ItemRoster;
            foreach (ItemRosterElement itemRosterElement in merchantItemRoster)
            {
                ItemObject itemObject = itemRosterElement.EquipmentElement.Item;
                // Check if its filtered
                if (!IsHorse(itemObject)) continue;

                int amount = itemRosterElement.Amount;
                float averagePrice = GetAveragePrice(itemRosterElement);
                int buyoutPrice = 0;

                bool canBuy = false;
                do
                {
                    canBuy = CanBuy(itemRosterElement, averagePrice, amount, out buyoutPrice);
                    if (canBuy)
                    {
                        ProcessTransaction(itemRosterElement, buyoutPrice);
                        amount -= 1;
                    } 
                } while (canBuy);
            }
        }

        private static bool CanBuy(ItemRosterElement itemRosterElement, float averagePrice, int amount, out int buyoutPrice)
        {
            // Retrieve price
            buyoutPrice = _inventoryLogic.GetItemPrice(itemRosterElement, _isBuying);
            float priceFactor = (float)buyoutPrice / averagePrice;

            // Buy only carry horses
            ItemObject itemObject = itemRosterElement.EquipmentElement.Item;
            if (IsHorse(itemObject))
            {
                if (itemObject.HorseComponent.IsPackAnimal && _buyHorses)
                {
                    if ((int)((float)PartyBase.MainParty.NumberOfAllMembers / 2.0f) > PartyBase.MainParty.NumberOfPackAnimals && buyoutPrice * 3 < _availablePlayerGold)
                    {
                        return CheckBasicBuyRequirements(itemRosterElement, amount, buyoutPrice);
                    }
                        
                    else
                        return false;
                }
                else return false;
            }

            // Check threshold
            if (priceFactor > (float)AutoTraderConfig.BuyThresholdValue / 100.0f)
                return false;

            // Check if we have enough cattle
            if (itemObject.IsAnimal)
            {
                // Don't buy more livestock than we have party members
                if (PartyBase.MainParty.NumberOfAllMembers < PartyBase.MainParty.ItemRoster.NumberOfLivestockAnimals)
                    return false;
                else
                    return CheckBasicBuyRequirements(itemRosterElement, amount, buyoutPrice);
            }

            // Check weight
            int itemIndex = PartyBase.MainParty.ItemRoster.FindIndexOfItem(itemObject);
            float stackWeightInRoster = 0f;
            if (itemIndex > 0)
            {
                stackWeightInRoster = PartyBase.MainParty.ItemRoster.GetElementCopyAtIndex(itemIndex).GetRosterElementWeight();
            }
            if (stackWeightInRoster >= (float)_baseCapacity * ((float)AutoTraderConfig.MaxCapacityValue / 100f))
            {
                return false;
            }

            return CheckBasicBuyRequirements(itemRosterElement, amount, buyoutPrice);
        }

        private static bool CanSell(ItemRosterElement itemRosterElement, float averagePrice, int amount, out int buyoutPrice)
        {
            // Retrieve price
            buyoutPrice = _inventoryLogic.GetItemPrice(itemRosterElement, _isBuying);
            float priceFactor = (float)buyoutPrice / averagePrice;

            // Sell all Armor and Weapons
            ItemObject itemObject = itemRosterElement.EquipmentElement.Item;
            if (IsArmor(itemObject) || IsWeapon(itemObject))
                return CheckBasicSellRequirements(itemRosterElement, amount, buyoutPrice);

            if (IsHorse(itemObject))
            {
                if (itemObject.HorseComponent.IsPackAnimal && _sellHorses)
                {
                    return false;
                }
            }

            // Check amounts to keep
            if (IsConsumable(itemObject))
            {
                int amountToKeep = itemObject == DefaultItems.Grain ?
                    AutoTraderConfig.KeepGrainsValue : AutoTraderConfig.KeepConsumablesValue;

                // Consider max setting
                if (itemObject == DefaultItems.Grain && amountToKeep > 199)
                    return false;

                if (amountToKeep >= amount)
                {
                    return false;
                }
                    
            }

            // Check threshold
            if (priceFactor < (float)AutoTraderConfig.SellThresholdValue / 100.0f)
                return false;

            return CheckBasicSellRequirements(itemRosterElement, amount, buyoutPrice);
        }

        private static bool CheckBasicBuyRequirements(ItemRosterElement itemRosterElement, int amount, int price)
        {
            ItemObject itemObject = itemRosterElement.EquipmentElement.Item;

            if (price >= _availablePlayerGold)
                return false;

            if (amount <= 0)
                return false;

            if (!IsHorse(itemObject) && itemObject.Weight > _availableInventoryCapacity)
                return false;

            return true;
        }

        private static bool CheckBasicSellRequirements(ItemRosterElement itemRosterElement, int amount, int price)
        {
            if (price >= _availableMerchantGold)
                return false;

            if (amount <= 0)
                return false;

            return true;
        }

        private static float GetAveragePrice(ItemRosterElement itemRosterElement)
        {
            float averagePrice = 0;
            float actualDistance = 0;
            int count = 0;

            foreach (Town town in Town.All)
            {
                if (AutoTraderConfig.SearchRadiusValue > 999 // Consider the maximum setting
                    || Campaign.Current.Models.MapDistanceModel.GetDistance(Settlement.CurrentSettlement, town.Settlement, (float)AutoTraderConfig.SearchRadiusValue, out actualDistance))
                {
                    averagePrice += town.MarketData.GetPrice(itemRosterElement, PartyBase.MainParty.MobileParty, true);
                    averagePrice += town.MarketData.GetPrice(itemRosterElement, PartyBase.MainParty.MobileParty, false);
                    count += 2;
                }

            }
            if (itemRosterElement.EquipmentElement.Item.IsTradeGood)
            {
                foreach (Village village in Village.All)
                {
                    if (AutoTraderConfig.SearchRadiusValue > 999 // Consider the maximum setting
                        || Campaign.Current.Models.MapDistanceModel.GetDistance(Settlement.CurrentSettlement, village.Settlement, (float)AutoTraderConfig.SearchRadiusValue, out actualDistance))
                    {
                        averagePrice += village.MarketData.GetPrice(itemRosterElement.EquipmentElement.Item, PartyBase.MainParty.MobileParty, true, null);
                        averagePrice += village.MarketData.GetPrice(itemRosterElement.EquipmentElement.Item, PartyBase.MainParty.MobileParty, false, null);
                        count += 2;
                    }
                }
            }

            if (count == 0)
                return GetAveragePriceFallback(itemRosterElement);

            averagePrice /= count;

            return averagePrice > 0 ? averagePrice : GetAveragePriceFallback(itemRosterElement);
        }

        private static float GetAveragePriceFallback(ItemRosterElement itemRosterElement)
        {
            return (float)itemRosterElement.EquipmentElement.Item.Value;
        }

        private static bool IsItemFiltered(ItemRosterElement itemRosterElement)
        {
            ItemObject itemObject = itemRosterElement.EquipmentElement.Item;

            // Filter by lock
            if (_locks.Contains(itemRosterElement))
                return true;

            // Exclude horses when buying for now
            if (IsHorse(itemObject) && _isBuying)
                return true;

            // Filter by type
            if (IsSmithingMaterial(itemObject))
                return _sellSmithing ? false : true;
            if (IsHorse(itemObject)  && !(_isBuying ? _buyHorses : _sellHorses))
                return true;
            if (IsArmor(itemObject)  && !(_isBuying ? _buyArmor : _sellArmor))
                return true;
            if (IsWeapon(itemObject) && !(_isBuying ? _buyWeapons : _sellWeapons))
                return true;
            if (IsTradeGood(itemObject) && !(_isBuying ? _buyGoods : _sellGoods))
            {
                if (!IsConsumable(itemObject))
                    return true;
            }
            if (IsConsumable(itemObject) && !(_isBuying ? _buyConsumables : _sellConsumables))
                return true;

            return false;
        }

        private static void PrintMessage(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage(message));
        }

        //--- Type checks ---//

        private static bool IsArmor(ItemObject itemObject)
        {
            if (itemObject.ItemType == ItemObject.ItemTypeEnum.HeadArmor
                || itemObject.ItemType == ItemObject.ItemTypeEnum.BodyArmor
                || itemObject.ItemType == ItemObject.ItemTypeEnum.LegArmor
                || itemObject.ItemType == ItemObject.ItemTypeEnum.HandArmor
                || itemObject.ItemType == ItemObject.ItemTypeEnum.ChestArmor
                || itemObject.ItemType == ItemObject.ItemTypeEnum.Cape
                || itemObject.ItemType == ItemObject.ItemTypeEnum.HorseHarness)
                return true;
            return false;
        }

        private static bool IsWeapon(ItemObject itemObject)
        {
            if (itemObject.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon
                || itemObject.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon
                || itemObject.ItemType == ItemObject.ItemTypeEnum.Polearm
                || itemObject.ItemType == ItemObject.ItemTypeEnum.Arrows
                || itemObject.ItemType == ItemObject.ItemTypeEnum.Bolts
                || itemObject.ItemType == ItemObject.ItemTypeEnum.Shield
                || itemObject.ItemType == ItemObject.ItemTypeEnum.Bow
                || itemObject.ItemType == ItemObject.ItemTypeEnum.Crossbow
                || itemObject.ItemType == ItemObject.ItemTypeEnum.Thrown
                || itemObject.ItemType == ItemObject.ItemTypeEnum.Pistol
                || itemObject.ItemType == ItemObject.ItemTypeEnum.Musket
                || itemObject.ItemType == ItemObject.ItemTypeEnum.Bullets)
                return true;
            return false;
        }

        private static bool IsHorse(ItemObject itemObject)
        {
            if (itemObject.ItemType == ItemObject.ItemTypeEnum.Horse)
                return true;
            return false;
        }

        private static bool IsTradeGood(ItemObject itemObject)
        {
            if (itemObject.ItemType == ItemObject.ItemTypeEnum.Goods
                || itemObject.ItemType == ItemObject.ItemTypeEnum.Animal)
                return true;
            return false;
        }

        private static bool IsConsumable(ItemObject itemObject)
        {
            if (itemObject.IsFood)
                return true;
            return false;
        }

        private static bool IsSmithingMaterial(ItemObject itemObject)
        {
            if (itemObject == DefaultItems.Charcoal
                || itemObject == DefaultItems.IronIngot1
                || itemObject == DefaultItems.IronIngot2
                || itemObject == DefaultItems.IronIngot3
                || itemObject == DefaultItems.IronIngot4
                || itemObject == DefaultItems.IronIngot5
                || itemObject == DefaultItems.IronIngot6)
                return true;
            return false;
        }

    }
}
