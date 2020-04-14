using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;

namespace AutoTrader
{
    public static class AutoTraderLogic
    {
        private static bool _isSimpleAI;
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
        private static bool _buyLivestock;
        private static bool _sellLivestock;

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
            _isSimpleAI = AutoTraderConfig.SimpleTradingAI;

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
            _buyLivestock = AutoTraderConfig.BuyLivestockValue;
            _sellLivestock = AutoTraderConfig.SellLivestockValue;
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

        private static bool SimpleWorthCheck(ItemRosterElement itemRosterElement, int buyoutPrice)
        {
            int truePrice = itemRosterElement.EquipmentElement.Item.Value;
            if (!_isBuying && buyoutPrice > ((float)AutoTraderConfig.SellThresholdValue / 100.0f) * (float)truePrice)
            {
                return true;
            }
            else if (_isBuying && buyoutPrice < ((float)AutoTraderConfig.BuyThresholdValue / 100.0f) * (float)truePrice)
            {
                return true;
            }
            return false;
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
                if (!AutoTraderHelpers.IsHorse(itemObject)) continue;

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
            ItemObject itemObject = itemRosterElement.EquipmentElement.Item;

            // Special Rules
            // Horses
            if (AutoTraderSpecialRules.CheckBuyHorsesCondition(itemObject))
            {
                if (AutoTraderSpecialRules.CheckBuyHorsesRules(itemObject, buyoutPrice, _availablePlayerGold))
                    return CheckBasicBuyRequirements(itemRosterElement, amount, buyoutPrice);
                return false; // buy no other horses
            }

            // Consumables
            if (AutoTraderSpecialRules.CheckBuyConsumablesCondition(itemObject))
            {
                if (AutoTraderSpecialRules.CheckBuyConsumablesRules(itemObject))
                    return CheckBasicBuyRequirements(itemRosterElement, amount, buyoutPrice);
            }

            // Price niveau
            if (_isSimpleAI)
            {
                if (!SimpleWorthCheck(itemRosterElement, buyoutPrice))
                    return false;
            }
            else
            {
                // Check threshold
                float priceFactor = (float)buyoutPrice / averagePrice;
                if (priceFactor > (float)AutoTraderConfig.BuyThresholdValue / 100.0f)
                    return false;
            }

            // Specials rules after price check
            // Check if we have enough cattle
            if (AutoTraderSpecialRules.CheckBuyCattleCondition(itemObject))
            {
                if (AutoTraderSpecialRules.CheckBuyCattleRule())
                    return CheckBasicBuyRequirements(itemRosterElement, amount, buyoutPrice);
                else return false; // Don't buy more cattle    
            }

            // Check weight
            if (AutoTraderSpecialRules.CheckBuyMaxCapacityRule(itemObject, _baseCapacity))
                return CheckBasicBuyRequirements(itemRosterElement, amount, buyoutPrice);
            else return false;
        }

        private static bool CanSell(ItemRosterElement itemRosterElement, float averagePrice, int amount, out int buyoutPrice)
        {
            // Retrieve price
            buyoutPrice = _inventoryLogic.GetItemPrice(itemRosterElement, _isBuying);

            // Sell all Armor and Weapons
            ItemObject itemObject = itemRosterElement.EquipmentElement.Item;
            if (AutoTraderHelpers.IsArmor(itemObject))
            {
                if (itemObject.Tier < (ItemObject.ItemTiers)AutoTraderConfig.ArmorTierValue)
                {
                    return CheckBasicSellRequirements(itemRosterElement, amount, buyoutPrice);
                }
                else return false;
            } else if (AutoTraderHelpers.IsWeapon(itemObject))
            {
                if (itemObject.Tier < (ItemObject.ItemTiers)AutoTraderConfig.WeaponTierValue)
                {
                    return CheckBasicSellRequirements(itemRosterElement, amount, buyoutPrice);
                }
                else return false;
            }
                
            // Special horse rule
            if (AutoTraderHelpers.IsHorse(itemObject))
            {
                if (itemObject.HorseComponent.IsPackAnimal && _sellHorses)
                {
                    return false;
                }
            }

            // Check amounts to keep
            if (AutoTraderHelpers.IsConsumable(itemObject))
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

            if (_isSimpleAI)
            {
                if (!SimpleWorthCheck(itemRosterElement, buyoutPrice))
                    return false;
            }
            else
            {
                // Check threshold
                float priceFactor = (float)buyoutPrice / averagePrice;
                if (priceFactor < (float)AutoTraderConfig.SellThresholdValue / 100.0f)
                    return false;
            }
            
            return CheckBasicSellRequirements(itemRosterElement, amount, buyoutPrice);
        }

        private static bool CheckBasicBuyRequirements(ItemRosterElement itemRosterElement, int amount, int price)
        {
            ItemObject itemObject = itemRosterElement.EquipmentElement.Item;

            if (price >= _availablePlayerGold)
                return false;

            if (amount <= 0)
                return false;

            if (!AutoTraderHelpers.IsHorse(itemObject) && itemObject.Weight > _availableInventoryCapacity)
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
            if (AutoTraderHelpers.IsHorse(itemObject) && _isBuying)
                return true;

            // Filter by type
            if (!_isBuying && AutoTraderHelpers.IsSmithingMaterial(itemObject))
                return _sellSmithing ? false : true;
            if (AutoTraderHelpers.IsHorse(itemObject)  && !(_isBuying ? _buyHorses : _sellHorses))
                return true;
            if (AutoTraderHelpers.IsArmor(itemObject)  && !(_isBuying ? _buyArmor : _sellArmor))
                return true;
            if (AutoTraderHelpers.IsWeapon(itemObject) && !(_isBuying ? _buyWeapons : _sellWeapons))
                return true;
            if (AutoTraderHelpers.IsLivestock(itemObject) && !(_isBuying ? _buyLivestock : _sellLivestock))
                return true;
            if (AutoTraderHelpers.IsTradeGood(itemObject) && !(_isBuying ? _buyGoods : _sellGoods))
            {
                if (!AutoTraderHelpers.IsConsumable(itemObject))
                    return true;
            }
            if (AutoTraderHelpers.IsConsumable(itemObject) && !(_isBuying ? _buyConsumables : _sellConsumables))
                return true;

            return false;
        }

    }
}
