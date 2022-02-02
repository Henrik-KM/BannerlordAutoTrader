using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;

[assembly: InternalsVisibleTo("AutoTraderTests")]
namespace AutoTrader
{
    public static class AutoTraderLogic
    {
        public static bool IsTradingActive { get; set; }
        public static bool IsBuying { get; set; }

        private enum MerchantType { 
            Town, 
            Village, 
            Caravan }

        private static MerchantType _merchantType;

        private static InventoryLogic _inventoryLogic;
        private static List<string> _locks;

        private static int _availableInventoryCapacity;
        private static int _availablePlayerGold;
        private static int _availableMerchantGold;
        private static bool _isCaravan;

        private static List<string> _soldItems;
        private static List<string> _boughtItems;

        public static void PerformAutoTrade(bool isCaravan = false)
        {
            _isCaravan = isCaravan;

            // Set trading state
            IsTradingActive = true;
            try
            {
                InitMerchantType();

                if (!InitInventory())
                {
                    AutoTraderHelpers.PrintDebugMessage("Failed to initialize the inventory!");
                    return;
                }

                InitializeMembers();

                Restock();
                Buy();
                Sell();
                Buy();
                BuyHorses();
            } catch ( Exception e)
            {
                AutoTraderHelpers.PrintMessage("My Lord! Something terrible happened to our autotraders! The last we heard of them is:\n" + e.ToString());
            } finally
            {
                // Unset trading state
                IsTradingActive = false;
            }   
        }

        private static void InitializeMembers()
        {
            // Inventory logic
            _inventoryLogic = InventoryManager.InventoryLogic;

            // Player gold
            int initialGold = PartyBase.MainParty.Owner.Gold;
            int troopWage = PartyBase.MainParty.MobileParty.TotalWage; // ToDo: whole daily 
            _availablePlayerGold = initialGold - (AutoTraderConfig.KeepWagesValue * troopWage);

            _soldItems = new List<string>();
            _boughtItems = new List<string>();

            UpdateAvailableInventoryCapacity();
            InitMerchantGold();

            // Locks
            var locksEnumerable = Campaign.Current.GetCampaignBehavior<IViewDataTracker>().GetInventoryLocks();
            if (locksEnumerable != null)
                _locks = locksEnumerable.ToList<string>();
        }

        private static void InitMerchantType()
        {
            if (_isCaravan)
            {
                _merchantType = MerchantType.Caravan;

                // Make sure its opened through conversation
                if (MobileParty.ConversationParty == null)
                {
                    AutoTraderHelpers.PrintDebugMessage("Caravan trading but not through a conversation!");
                    return;
                }
            }
            else
                _merchantType = Settlement.CurrentSettlement.IsTown ? MerchantType.Town : MerchantType.Village;
        }

        private static void InitMerchantGold()
        {
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
        }

        private static bool InitInventory()
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

        private static void UpdateAvailableInventoryCapacity()
        {
            float currentWeight = PartyBase.MainParty.ItemRoster.TotalWeight;
            _availableInventoryCapacity = (int)((float)PartyBase.MainParty.InventoryCapacity * ((float)AutoTraderConfig.UseInventorySpaceValue / 100.0f));
            _availableInventoryCapacity -= (int)currentWeight;
            AutoTraderHelpers.PrintDebugMessage("Current weight: " + currentWeight.ToString() + ", available capacity: " + _availableInventoryCapacity.ToString());
        }

        private static void Sell()
        {
            IsBuying = false;

            // Loop through all items in inventory
            ItemRoster playerItemRoster = PartyBase.MainParty.ItemRoster;

            foreach (ItemRosterElement itemRosterElement in playerItemRoster.ToList())
            {
                // Check if its filtered
                if (IsItemFiltered(itemRosterElement)) continue;

                int amount = itemRosterElement.Amount;

                // TODO: Change on simple AI
                float averagePrice = GetAveragePrice(itemRosterElement);

                // Sell items one by one
                bool canSell = false;
                do
                {
                    int buyout_price = 0;
                    canSell = CanSell(itemRosterElement, averagePrice, 1, out buyout_price);
                    if (canSell)
                    {
                        // Update members
                        ProcessTransaction(itemRosterElement, buyout_price);
                        amount--;
                    }

                } while (canSell && amount > 0);
            }
        }

        private static void Restock()
        {
            IsBuying = true;

            if (!AutoTraderConfig.ResupplyValue)
                return;

            ItemRoster merchantItemRoster = GetMerchantItemRoster();

            // Loop through items
            foreach (ItemRosterElement itemRosterElement in merchantItemRoster)
            {
                ItemObject itemObject = itemRosterElement.EquipmentElement.Item;
                // Check if its filtered
                if (!AutoTraderHelpers.IsConsumable(itemObject)) continue;

                if (!AutoTraderSpecialRules.CheckBuyResupplyRule(itemObject)) continue;

                int amount = itemRosterElement.Amount;
                float averagePrice = GetAveragePrice(itemRosterElement);
                int buyoutPrice = 0;

                bool canBuy = false;
                bool needsResupply = false;
                do
                {
                    canBuy = CanBuy(itemRosterElement, averagePrice, 1, out buyoutPrice);
                    needsResupply = AutoTraderSpecialRules.CheckBuyResupplyRule(itemObject);
                    if (canBuy && needsResupply)
                    {
                        ProcessTransaction(itemRosterElement, buyoutPrice);
                        amount -= 1;
                    }
                } while (canBuy && needsResupply && amount > 0);
            }
        }

        private static void Buy()
        {
            IsBuying = true;

            var itemBuyList = new List<KeyValuePair<ItemRosterElement, KeyValuePair<float, float>>>();

            // Get item roster
            ItemRoster merchantItemRoster = GetMerchantItemRoster();

            // Loop through all items of merchant
            foreach (ItemRosterElement itemRosterElement in merchantItemRoster)
            {
                // Check if its filtered
                if (IsItemFiltered(itemRosterElement)) continue;

                IPlayerTradeBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IPlayerTradeBehavior>();
                int buyoutPrice = _inventoryLogic.GetItemPrice(itemRosterElement, IsBuying);
                float averagePrice = GetAveragePrice(itemRosterElement);
                float profit = campaignBehavior.GetProjectedProfit(itemRosterElement, buyoutPrice);
                if (profit == buyoutPrice)
                    profit = averagePrice - (float)buyoutPrice;
                // TODO no good place for average price

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
                    canBuy = CanBuy(itemRosterElement, averagePrice, 1, out buyout_price);

                    if (canBuy)
                    {
                        // Update members
                        ProcessTransaction(itemRosterElement, buyout_price);
                        amount--;
                    }

                } while (canBuy && amount > 0);
            }
        }

        private static void BuyHorses()
        {
            IsBuying = true;

            ItemRoster merchantItemRoster = GetMerchantItemRoster();

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
                    canBuy = CanBuy(itemRosterElement, averagePrice, 1, out buyoutPrice);
                    if (canBuy)
                    {
                        ProcessTransaction(itemRosterElement, buyoutPrice);
                        amount -= 1;
                    }
                } while (canBuy && amount > 0);
            }
        }

        internal static bool SimpleWorthCheck(int value, int buyoutPrice)
        {
            /// Checks if the value is below or above threshold 

            if (!IsBuying && buyoutPrice >= ((float)AutoTraderConfig.SellThresholdValue / 100.0f) * (float)value)
            {
                return true;
            }
            else if (IsBuying && buyoutPrice < ((float)AutoTraderConfig.BuyThresholdValue / 100.0f) * (float)value)
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

            // Mark the item
            string name = itemRosterElement.EquipmentElement.Item.GetName().ToString();
            if (IsBuying)
            {
                if (!_boughtItems.Exists(x => x == name))
                    _boughtItems.Add(name);
            } else
            {
                if (!_soldItems.Exists(x => x == name))
                    _soldItems.Add(name);
            }

            // Update available weight
            UpdateAvailableInventoryCapacity();

            _inventoryLogic.AddTransferCommand(transferCommand);
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
            if (AutoTraderConfig.SimpleTradingAI)
            {
                float averagePriceFactorItemCategory = _inventoryLogic.GetAveragePriceFactorItemCategory(itemRosterElement.EquipmentElement.Item.ItemCategory);
                Town town = Settlement.CurrentSettlement.IsVillage ? Settlement.CurrentSettlement.Village.Bound.Town : Settlement.CurrentSettlement.Town;
                if (averagePriceFactorItemCategory != -99.0)
                {
                    float price_factor = town.MarketData.GetPriceFactor(itemRosterElement.EquipmentElement.Item.ItemCategory, false);
                    if (price_factor > averagePriceFactorItemCategory * 0.95)
                        return false;
                } else {
                    AutoTraderHelpers.PrintDebugMessage("No average price found for " + itemRosterElement.ToString());
                    return false;
                }
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
            if (AutoTraderSpecialRules.CheckBuyMaxCapacityRule(itemObject, PartyBase.MainParty.InventoryCapacity))
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

                if (amountToKeep >= itemRosterElement.Amount)
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

            if (AutoTraderConfig.SimpleTradingAI)
            {
                IPlayerTradeBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IPlayerTradeBehavior>();
                if (campaignBehavior != null)
                {
                    int weighted_profit = buyoutPrice - campaignBehavior.GetProjectedProfit(itemRosterElement, buyoutPrice);
                    // Check case of no trade rumours
                    if(weighted_profit == 0)
                    {
                        float priceFactor = (float)buyoutPrice / averagePrice;
                        if (priceFactor <= 1.05f)
                            return false;
                    } else if (weighted_profit > buyoutPrice * 0.95)
                        return false;
                    
                } else
                {
                    AutoTraderHelpers.PrintDebugMessage("AutoTrader: Missing PlayerTradeBehavior!");
                    return false;
                }
                    
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

        private static ItemRoster GetMerchantItemRoster()
        {
            if (_merchantType == MerchantType.Caravan)
                return MobileParty.ConversationParty.ItemRoster;
            return Settlement.CurrentSettlement.ItemRoster;
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

            // Check if already bought / sold
            if (IsBuying)
            {
                if (_soldItems.Exists(x => x == itemRosterElement.EquipmentElement.Item.GetName().ToString()))
                    return true;
            } else
            {
                if (_boughtItems.Exists(x => x == itemRosterElement.EquipmentElement.Item.GetName().ToString()))
                    return true;
            }

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
