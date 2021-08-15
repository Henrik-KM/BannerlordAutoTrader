using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;

namespace AutoTrader
{
    public static class AutoTraderLogic
    {
        public static bool IsTradingActive { get; set; }

        private static bool _isSimpleAI;
        private enum MerchantType { 
            Town, 
            Village, 
            Caravan }

        private static MerchantType _merchantType;
        public static bool IsBuying { get; set; }

        private static InventoryLogic _inventoryLogic;
        private static List<string> _locks;

        private static int _baseCapacity;
        private static int _availableInventoryCapacity;
        private static int _availablePlayerGold;
        private static int _availableMerchantGold;

        public static void PerformAutoTrade(bool isCaravan = false)
        {
            // Set trading state
            IsTradingActive = true;

            // Define the merchant type
            if (isCaravan)
            {
                _merchantType = MerchantType.Caravan;
                // Make sure its opened through conversation
                if (MobileParty.ConversationParty == null)
                    return;
            }
            else
                _merchantType = Settlement.CurrentSettlement.IsTown ? MerchantType.Town : MerchantType.Village;

            if (!InitializeInventory()) return;
            InitializeMembers();

            /*if (_merchantType == MerchantType.Town || _merchantType == MerchantType.Caravan)
            {
                Sell();
                Buy();
                BuyHorses();
                _inventoryLogic.RemoveZeroCounts();
            }
            else if(_merchantType == MerchantType.Village)
            {*/
            Buy();
            Sell();
            Buy();
            BuyHorses();
            //_inventoryLogic.RemoveZeroCounts();
            //}

            // Remove trading state
            IsTradingActive = false;
        }

        private static bool InitializeInventory()
        {
            if (_merchantType == MerchantType.Town)
            {
                InventoryManager.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.GetComponent<Town>(),
                    InventoryManager.InventoryCategoryType.None, null);
                return true;
            }
            else if (_merchantType == MerchantType.Village)
            {
                InventoryManager.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.Village, InventoryManager.InventoryCategoryType.None, null);
                return true;
            }
            else if (_merchantType == MerchantType.Caravan)
            {
                InventoryManager.OpenTradeWithCaravanOrAlleyParty(MobileParty.ConversationParty, InventoryManager.InventoryCategoryType.None);
                return true;
            }
            return false;
        }

        private static void InitializeMembers()
        {
            // Inventory logic
            _inventoryLogic = InventoryManager.InventoryLogic;
            _isSimpleAI = AutoTraderConfig.SimpleTradingAI;

            // Inventory capacity
            _baseCapacity = PartyBase.MainParty.MobileParty.InventoryCapacity;
            float currentWeight = PartyBase.MainParty.ItemRoster.TotalWeight;
            _availableInventoryCapacity = (int)((float)_baseCapacity * ((float)AutoTraderConfig.UseInventorySpaceValue / 100.0f));
            _availableInventoryCapacity -= (int)currentWeight;

            // Player gold
            int initialGold = PartyBase.MainParty.Owner.Gold;
            int troopWage = PartyBase.MainParty.MobileParty.TotalWage; // ToDo: whole daily 
            _availablePlayerGold = initialGold - (AutoTraderConfig.KeepWagesValue * troopWage);

            // Merchant gold
            switch (_merchantType)
            {
                case MerchantType.Town:
                    _availableMerchantGold = Settlement.CurrentSettlement.Town.Gold;
                    break;
                case MerchantType.Village:
                    _availableMerchantGold = Settlement.CurrentSettlement.Village.Gold;
                    break;
                case MerchantType.Caravan:
                    _availableMerchantGold = MobileParty.ConversationParty.PartyTradeGold;
                    break;
            }

            // Locks
            var locksEnumerable = Campaign.Current.GetCampaignBehavior<IInventoryLockTracker>().GetLocks();
            if (locksEnumerable != null)
                _locks = locksEnumerable.ToList<string>();
        }

        private static void Sell()
        {
            IsBuying = false;

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
            IsBuying = true;

            var itemBuyList = new List<KeyValuePair<ItemRosterElement, KeyValuePair<float, float>>>();

            // Get item roster
            ItemRoster merchantItemRoster;
            if (_merchantType == MerchantType.Caravan)
                merchantItemRoster = MobileParty.ConversationParty.ItemRoster;
            else
                merchantItemRoster = Settlement.CurrentSettlement.ItemRoster;

            // Loop through all items of merchant
            foreach (ItemRosterElement itemRosterElement in merchantItemRoster)
            {
                // Check if its filtered
                if (IsItemFiltered(itemRosterElement)) continue;

                float averagePrice = GetAveragePrice(itemRosterElement);
                int buyoutPrice = _inventoryLogic.GetItemPrice(itemRosterElement, IsBuying);
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

                // Buy items one by one
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
            if (!IsBuying && buyoutPrice > ((float)AutoTraderConfig.SellThresholdValue / 100.0f) * (float)truePrice)
            {
                return true;
            }
            else if (IsBuying && buyoutPrice < ((float)AutoTraderConfig.BuyThresholdValue / 100.0f) * (float)truePrice)
            {
                return true;
            }
            return false;
        }

        private static void ProcessTransaction(ItemRosterElement itemRosterElement, int buyoutPrice)
        {
            ItemObject itemObject = itemRosterElement.EquipmentElement.Item;

            // Update available gold
            _availablePlayerGold += IsBuying ? -buyoutPrice : buyoutPrice;
            _availableMerchantGold += IsBuying ? buyoutPrice : -buyoutPrice;

            // Generate command
            TransferCommand transferCommand = TransferCommand.Transfer(1, 
                IsBuying ? InventoryLogic.InventorySide.OtherInventory : InventoryLogic.InventorySide.PlayerInventory, 
                IsBuying ? InventoryLogic.InventorySide.PlayerInventory : InventoryLogic.InventorySide.OtherInventory, 
                itemRosterElement, EquipmentIndex.None, EquipmentIndex.None, CharacterObject.PlayerCharacter, true);

            // Update available weight
            _availableInventoryCapacity = (int)((float)PartyBase.MainParty.InventoryCapacity * (AutoTraderConfig.UseInventorySpaceValue / 100.0f)) - (int)PartyBase.MainParty.ItemRoster.TotalWeight;

            _inventoryLogic.AddTransferCommand(transferCommand);
        }

        private static void BuyHorses()
        {
            IsBuying = true;

            ItemRoster merchantItemRoster;

            // Get the item roster
            if (_merchantType == MerchantType.Caravan)
                merchantItemRoster = MobileParty.ConversationParty.ItemRoster;
            else
                merchantItemRoster = Settlement.CurrentSettlement.ItemRoster;

            // Loop through items
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
            buyoutPrice = _inventoryLogic.GetItemPrice(itemRosterElement, IsBuying);
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

            if (AutoTraderSpecialRules.CheckBuyGoodsCondition(itemObject))
            {
                if (AutoTraderSpecialRules.CheckBuyGoodsRules(itemObject))
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
            buyoutPrice = _inventoryLogic.GetItemPrice(itemRosterElement, IsBuying);

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
                if (itemObject.HorseComponent.IsPackAnimal && AutoTraderConfig.SellHorsesValue)
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

            // Special hardwood rule
            if (itemObject == DefaultItems.HardWood && AutoTraderConfig.ResupplyHardwoodValue)
            {
                // If hardwood needs to be resupplied dont sell
                if (AutoTraderSpecialRules.CheckBuyResupplyHardwoodRule())
                    return false;
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
            float count = 0.0f;

            foreach (Town town in Town.AllTowns)
            {
                bool isInRange = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, town.Settlement, (float)AutoTraderConfig.SearchRadiusValue, out actualDistance);
                if (AutoTraderConfig.UseWeightedValue // Consider weighted value
                    || AutoTraderConfig.SearchRadiusValue > 999 // Consider the maximum setting
                    || isInRange)
                {
                    if (AutoTraderConfig.UseWeightedValue)
                    {
                        // If its the current town, skip
                        if (!(_merchantType == MerchantType.Caravan) && Settlement.CurrentSettlement.IsTown && town == Settlement.CurrentSettlement.Town)
                            continue;
                        // Weight by distance
                        averagePrice += (float)town.MarketData.GetPrice(itemRosterElement.EquipmentElement.Item, PartyBase.MainParty.MobileParty, true) / actualDistance;
                        averagePrice += (float)town.MarketData.GetPrice(itemRosterElement.EquipmentElement.Item, PartyBase.MainParty.MobileParty, false) / actualDistance;
                        count += 2.0f / actualDistance;
                    }
                    else
                    {
                        averagePrice += town.MarketData.GetPrice(itemRosterElement.EquipmentElement.Item, PartyBase.MainParty.MobileParty, true);
                        averagePrice += town.MarketData.GetPrice(itemRosterElement.EquipmentElement.Item, PartyBase.MainParty.MobileParty, false);
                        count += 2.0f;
                    }
                }
            }
            if (itemRosterElement.EquipmentElement.Item.IsTradeGood)
            {
                foreach (Village village in Village.All)
                {
                    bool isInRange = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, village.Settlement, (float)AutoTraderConfig.SearchRadiusValue, out actualDistance);
                    if (AutoTraderConfig.UseWeightedValue // Consider weighted value
                        || AutoTraderConfig.SearchRadiusValue > 999 // Consider the maximum setting
                        || isInRange)
                    {
                        if (AutoTraderConfig.UseWeightedValue)
                        {
                            // If its the current town, skip
                            if (!(_merchantType == MerchantType.Caravan) && Settlement.CurrentSettlement.IsVillage && village == Settlement.CurrentSettlement.Village)
                                continue;
                            // Weight by distance
                            averagePrice += (float)village.MarketData.GetPrice(itemRosterElement.EquipmentElement.Item, PartyBase.MainParty.MobileParty, true, null) / actualDistance;
                            averagePrice += (float)village.MarketData.GetPrice(itemRosterElement.EquipmentElement.Item, PartyBase.MainParty.MobileParty, false, null) / actualDistance;
                            count += 2.0f / actualDistance;
                        }
                        else
                        {
                            averagePrice += village.MarketData.GetPrice(itemRosterElement.EquipmentElement.Item, PartyBase.MainParty.MobileParty, true, null);
                            averagePrice += village.MarketData.GetPrice(itemRosterElement.EquipmentElement.Item, PartyBase.MainParty.MobileParty, false, null);
                            count += 2.0f;
                        }
                    }
                }
            }

            if (count == 0.0f)
                return GetAveragePriceFallback(itemRosterElement);

            averagePrice /= count;

            return averagePrice > 0 ? averagePrice : GetAveragePriceFallback(itemRosterElement);
        }

        private static float GetAveragePriceFallback(ItemRosterElement itemRosterElement)
        {
            return (float)itemRosterElement.EquipmentElement.Item.Value;
        }

        private static bool IsItemLocked(ItemRosterElement itemRosterElement)
        {
            if (_locks != null)
            {
                var item_id = itemRosterElement.EquipmentElement.Item.StringId;
                if (itemRosterElement.EquipmentElement.ItemModifier != null)
                {
                    item_id += itemRosterElement.EquipmentElement.ItemModifier.StringId;
                }

                return _locks.Contains(item_id);
            }
            return false;
        }

        public static bool IsItemFiltered(ItemRosterElement itemRosterElement)
        {
            ItemObject itemObject = itemRosterElement.EquipmentElement.Item;

            // Filter by lock
            if (IsItemLocked(itemRosterElement))
                return true;

            // Exclude horses when buying for now
            if (AutoTraderHelpers.IsHorse(itemObject) && IsBuying)
                return true;

            // Filter by type
            if (!IsBuying && AutoTraderHelpers.IsSmithingMaterial(itemObject))
                return AutoTraderConfig.SellSmithingValue ? false : true;
            if (AutoTraderHelpers.IsHorse(itemObject)  && !(IsBuying ? AutoTraderConfig.BuyHorsesValue : AutoTraderConfig.SellHorsesValue))
                return true;
            if (AutoTraderHelpers.IsArmor(itemObject)  && !(IsBuying ? AutoTraderConfig.BuyArmorValue : AutoTraderConfig.SellArmorValue))
                return true;
            if (AutoTraderHelpers.IsWeapon(itemObject) && !(IsBuying ? AutoTraderConfig.BuyWeaponsValue : AutoTraderConfig.SellWeaponsValue))
                return true;
            if (AutoTraderHelpers.IsLivestock(itemObject) && !(IsBuying ? AutoTraderConfig.BuyLivestockValue : AutoTraderConfig.SellLivestockValue))
                return true;
            if (AutoTraderHelpers.IsTradeGood(itemObject) && !(IsBuying ? AutoTraderConfig.BuyGoodsValue : AutoTraderConfig.SellGoodsValue))
            {
                if (!AutoTraderHelpers.IsConsumable(itemObject))
                    return true;
            }
            if (AutoTraderHelpers.IsConsumable(itemObject) && !(IsBuying ? AutoTraderConfig.BuyConsumablesValue : AutoTraderConfig.SellConsumablesValue))
                return true;

            return false;
        }

    }
}
