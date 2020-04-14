using TaleWorlds.Core;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AutoTrader
{
    class AutoTraderMenuViewModel : ViewModel
    {
		private string _configMenuText;
		private string _doneText;
		private string _cancelText;
		private string _lockHintText;
		private string _buyText;
		private string _sellText;
		private string _defaultText;
		private string _infText;

		private string _presetsText;
		private string _presetsDefaultText;
		private string _presetsSellerText;
		private string _presetsGlobalText;
		private string _presetsLocalText;

		private string _headerBaseText;
		private string _headerCustomText;
		private string _headerSpecialText;
		private string _headerTradeText;

		private string _simpleAIText;
		private string _buyThresholdText;
		private string _sellThresholdText;
		private string _maxCapacityText;
		private string _keepGrainsText;
		private string _keepConsumablesText;
		private string _useInventorySpaceText;
		private string _keepWagesText;
		private string _searchRadiusText;
		private string _weaponTierText;
		private string _armorTierText;

		private string _sellSmithingText;
		private string _resupplyText;

		private string _tradeHorsesText;
		private string _tradeWeaponsText;
		private string _tradeArmorText;
		private string _tradeGoodsText;
		private string _tradeConsumablesText;
		private string _tradeLivestockText;

		private float _buyThresholdValue;
		private string _buyThresholdValueAsString;

		private float _sellThresholdValue;
		private string _sellThresholdValueAsString;

		private float _maxCapacityValue;
		private string _maxCapacityValueAsString;

		private float _keepGrainsValue;
		private string _keepGrainsValueAsString;

		private float _keepConsumablesValue;
		private string _keepConsumablesValueAsString;

		private float _useInventorySpaceValue;
		private string _useInventorySpaceValueAsString;

		private float _keepWagesValue;
		private string _keepWagesValueAsString;

		private float _searchRadiusValue;
		private string _searchRadiusValueAsString;

		private float _weaponTierValue;
		private string _weaponTierValueAsString;

		private float _armorTierValue;
		private string _armorTierValueAsString;

		private bool _simpleAIValue;

		private bool _sellSmithingValue;
		private bool _resupplyValue;

		private bool _buyHorsesValue;
		private bool _sellHorsesValue;
		private bool _buyWeaponsValue;
		private bool _sellWeaponsValue;
		private bool _buyArmorValue;
		private bool _sellArmorValue;
		private bool _buyGoodsValue;
		private bool _sellGoodsValue;
		private bool _buyConsumablesValue;
		private bool _sellConsumablesValue;
		private bool _buyLivestockValue;
		private bool _sellLivestockValue;

		private bool _isSearchRadiusActive;

		public AutoTraderMenuViewModel()
        {
			this._configMenuText = new TextObject("{=ATOptions}AutoTrader Configuration", null).ToString();
			this._doneText = new TextObject("{=ATDone}Done", null).ToString();
			this._cancelText = new TextObject("{=ATCancel}Cancel", null).ToString();
			this._lockHintText = new TextObject("{=ATLockHint}Note: Lock individual items in your inventory to keep them from being sold.", null).ToString();
			this._buyText = new TextObject("{=ATBuy}Buy", null).ToString();
			this._sellText = new TextObject("{=ATSell}Sell", null).ToString();
			this._defaultText = new TextObject("{=ATDefault}Default: ", null).ToString();
			this._infText = new TextObject("{=ATInf}Inf", null).ToString();

			this._presetsText = new TextObject("{=ATPresets}Presets", null).ToString();
			this._presetsDefaultText = new TextObject("{=ATPreDefault}Default", null).ToString();
			this._presetsSellerText = new TextObject("{=ATPreSeller}Loot Seller", null).ToString();
			this._presetsGlobalText = new TextObject("{=ATPreGlobal}Global Trade", null).ToString();
			this._presetsLocalText = new TextObject("{=ATPreLocal}Local Trade", null).ToString();

			this._headerBaseText = new TextObject("{=ATHeaderBase}Basic Options", null).ToString();
			this._headerCustomText = new TextObject("{=ATHeaderCustom}User Preferences", null).ToString();
			this._headerSpecialText = new TextObject("{=ATHeaderSpecial}Special Options", null).ToString();
			this._headerTradeText = new TextObject("{=ATHeaderTrade}Buy / Sell Options", null).ToString();

			this._simpleAIText = new TextObject("{=ATSimpleAI}Use simple AI: Buy at green, sell at red (adjustable with buy / sell thresholds)", null).ToString();

			this._buyThresholdText = new TextObject("{=ATBuyThreshold}Buy under % of the average price", null).ToString();
			this._sellThresholdText = new TextObject("{=ATSellThreshold}Sell above % of the average price", null).ToString();
			this._maxCapacityText = new TextObject("{=ATMaxCapacity}Maximum % of inventory filled by the same good", null).ToString();
			this._keepGrainsText = new TextObject("{=ATKeepGrains}Amount of grain to keep", null).ToString();
			this._keepConsumablesText = new TextObject("{=ATKeepConsumables}Amount of each other consumable to keep", null).ToString();
			this._useInventorySpaceText = new TextObject("{=ATUseInventorySpace}Total % of inventory space the mod may use", null).ToString();
			this._keepWagesText = new TextObject("{=ATKeepWages}Keep enough gold for X days of troop wages", null).ToString();
			this._searchRadiusText = new TextObject("{=ATSearchRadius}Average price search radius (advanced)", null).ToString();
			this._weaponTierText = new TextObject("{=ATWeaponTier}Sell weapons of tier X and below", null).ToString();
			this._armorTierText = new TextObject("{=ATArmorTier}Sell armor of tier X and below", null).ToString();

			this._sellSmithingText = new TextObject("{=ATSellSmithing}Sell charcoal and ingots", null).ToString();
			this._resupplyText = new TextObject("{=ATResupply}Resupply consumables to their 'keep' value", null).ToString();

			this._tradeHorsesText = new TextObject("{=ATTradeHorses}Trade Horses (buys only carry horses)", null).ToString();
			this._tradeWeaponsText = new TextObject("{=ATTradeWeapons}Trade Weapons (sells all unlocked)", null).ToString();
			this._tradeArmorText = new TextObject("{=ATTradeArmor}Trade Armor (sells all unlocked)", null).ToString();
			this._tradeGoodsText = new TextObject("{=ATTradeGoods}Trade Goods", null).ToString();
			this._tradeConsumablesText = new TextObject("{=ATTradeConsumables}Trade Consumables", null).ToString();
			this._tradeLivestockText = new TextObject("{=ATTradeLivestock}Trade Livestock", null).ToString();

			this._simpleAIValue = AutoTraderConfig.SimpleTradingAI;
			this._buyThresholdValue = AutoTraderConfig.BuyThresholdValue;
			this._sellThresholdValue = AutoTraderConfig.SellThresholdValue;
			this._maxCapacityValue = AutoTraderConfig.MaxCapacityValue;
			this._keepGrainsValue = AutoTraderConfig.KeepGrainsValue;
			this._keepConsumablesValue = AutoTraderConfig.KeepConsumablesValue;
			this._useInventorySpaceValue = AutoTraderConfig.UseInventorySpaceValue;
			this._keepWagesValue = AutoTraderConfig.KeepWagesValue;
			this._searchRadiusValue = AutoTraderConfig.SearchRadiusValue;
			this._weaponTierValue = AutoTraderConfig.WeaponTierValue;
			this._armorTierValue = AutoTraderConfig.ArmorTierValue;

			this._buyHorsesValue = AutoTraderConfig.BuyHorsesValue;
			this._buyWeaponsValue = AutoTraderConfig.BuyWeaponsValue;
			this._buyArmorValue = AutoTraderConfig.BuyArmorValue;
			this._buyGoodsValue = AutoTraderConfig.BuyGoodsValue;
			this._buyConsumablesValue = AutoTraderConfig.BuyConsumablesValue;
			this._buyLivestockValue = AutoTraderConfig.BuyLivestockValue;

			this._sellSmithingValue = AutoTraderConfig.SellSmithingValue;
			this._resupplyValue = AutoTraderConfig.ResupplyValue;

			this._sellHorsesValue = AutoTraderConfig.SellHorsesValue;
			this._sellWeaponsValue = AutoTraderConfig.SellWeaponsValue;
			this._sellArmorValue = AutoTraderConfig.SellArmorValue;
			this._sellGoodsValue = AutoTraderConfig.SellGoodsValue;
			this._sellConsumablesValue = AutoTraderConfig.SellConsumablesValue;
			this._sellLivestockValue = AutoTraderConfig.SellLivestockValue;

			this._isSearchRadiusActive = !this._simpleAIValue;

			this.RefreshValues();
		}

        public override void RefreshValues()
        {
            base.RefreshValues();
			this._buyThresholdValueAsString = this.BuyThresholdValue.ToString();
			this._sellThresholdValueAsString = this.SellThresholdValue.ToString();
			this._maxCapacityValueAsString = this.MaxCapacityValue.ToString();
			if (this._keepGrainsValue >= AutoTraderConfig.MaxKeepGrainsValue)
				this._keepGrainsValueAsString = new TextObject("{=ATAll}All", null).ToString();
			else
				this._keepGrainsValueAsString = this.KeepGrainsValue.ToString();
			this._keepConsumablesValueAsString = this.KeepConsumablesValue.ToString();
			this._useInventorySpaceValueAsString = this.UseInventorySpaceValue.ToString();
			this._keepWagesValueAsString = this.KeepWagesValue.ToString();
			if (this._searchRadiusValue > 999.0f)
				this._searchRadiusValueAsString = this.InfText;
			else
				this._searchRadiusValueAsString = this.SearchRadiusValue.ToString();
			this._weaponTierValueAsString = this.WeaponTierValue.ToString();
			this._armorTierValueAsString = this.ArmorTierValue.ToString();

			this._isSearchRadiusActive = !this.SimpleAIValue;
		}

		private void ExecuteDone()
		{
			AutoTraderConfig.SimpleTradingAI = this._simpleAIValue;
			AutoTraderConfig.BuyThresholdValue = (int)this.BuyThresholdValue;
			AutoTraderConfig.SellThresholdValue = (int)this.SellThresholdValue;
			AutoTraderConfig.MaxCapacityValue = (int)this.MaxCapacityValue;
			AutoTraderConfig.KeepGrainsValue = (int)this.KeepGrainsValue;
			AutoTraderConfig.KeepConsumablesValue = (int)this.KeepConsumablesValue;
			AutoTraderConfig.UseInventorySpaceValue = (int)this.UseInventorySpaceValue;
			AutoTraderConfig.KeepWagesValue = (int)this.KeepWagesValue;
			AutoTraderConfig.SearchRadiusValue = (int)this.SearchRadiusValue;
			AutoTraderConfig.WeaponTierValue = (int)this.WeaponTierValue;
			AutoTraderConfig.ArmorTierValue = (int)this.ArmorTierValue;

			AutoTraderConfig.BuyHorsesValue = this.BuyHorsesValue;
			AutoTraderConfig.BuyArmorValue = this.BuyArmorValue;
			AutoTraderConfig.BuyWeaponsValue = this.BuyWeaponsValue;
			AutoTraderConfig.BuyGoodsValue = this.BuyGoodsValue;
			AutoTraderConfig.BuyConsumablesValue = this.BuyConsumablesValue;
			AutoTraderConfig.BuyLivestockValue = this.BuyLivestockValue;

			AutoTraderConfig.SellSmithingValue = this.SellSmithingValue;
			AutoTraderConfig.ResupplyValue = this.ResupplyValue;

			AutoTraderConfig.SellHorsesValue = this.SellHorsesValue;
			AutoTraderConfig.SellArmorValue = this.SellArmorValue;
			AutoTraderConfig.SellWeaponsValue = this.SellWeaponsValue;
			AutoTraderConfig.SellGoodsValue = this.SellGoodsValue;
			AutoTraderConfig.SellConsumablesValue = this.SellConsumablesValue;
			AutoTraderConfig.SellLivestockValue = this.SellLivestockValue;

			AutoTraderConfig.Save();

			ScreenManager.PopScreen();
		}

		private void ExecuteCancel()
		{
			ScreenManager.PopScreen();
		}

		private void ExecutePresetDefault()
		{
			this.SimpleAIValue = false;

			this.BuyThresholdValue = 70.0f;
			this.SellThresholdValue = 95.0f;
			this.MaxCapacityValue = 15.0f;
			this.KeepGrainsValue = 10.0f;
			this.KeepConsumablesValue = 4.0f;
			this.UseInventorySpaceValue = 90.0f;
			this.KeepWagesValue = 3.0f;
			this.SearchRadiusValue = 300.0f;
			this.WeaponTierValue = 2.0f;
			this.ArmorTierValue = 2.0f;

			this.SellSmithingValue = false;
			this.ResupplyValue = false;
			this.BuyHorsesValue = true;
			this.SellHorsesValue = false;
			this.BuyWeaponsValue = false;
			this.SellWeaponsValue = true;
			this.BuyArmorValue = false;
			this.SellArmorValue = true;
			this.BuyGoodsValue = true;
			this.SellGoodsValue = true;
			this.BuyConsumablesValue = true;
			this.SellConsumablesValue = true;
			this.BuyLivestockValue = true;
			this.SellLivestockValue = true;

			RefreshValues();
		}

		private void ExecutePresetSeller()
		{
			this.WeaponTierValue = 3.0f;
			this.ArmorTierValue = 3.0f;
			this.SellSmithingValue = false;
			this.BuyHorsesValue = false;
			this.SellHorsesValue = true;
			this.BuyWeaponsValue = false;
			this.SellWeaponsValue = true;
			this.BuyArmorValue = false;
			this.SellArmorValue = true;
			this.BuyGoodsValue = false;
			this.SellGoodsValue = false;
			this.BuyConsumablesValue = false;
			this.SellConsumablesValue = false;
			this.BuyLivestockValue = false;
			this.SellLivestockValue = false;

			RefreshValues();
		}

		private void ExecutePresetGlobal()
		{
			this.SimpleAIValue = false;

			this.SearchRadiusValue = 1000.0f;

			this.BuyHorsesValue = true;
			this.BuyGoodsValue = true;
			this.SellGoodsValue = true;
			this.BuyConsumablesValue = true;
			this.SellConsumablesValue = true;

			RefreshValues();
		}

		private void ExecutePresetLocal()
		{
			this.SimpleAIValue = false;

			this.SearchRadiusValue = 300.0f;

			this.BuyHorsesValue = true;
			this.BuyGoodsValue = true;
			this.SellGoodsValue = true;
			this.BuyConsumablesValue = true;
			this.SellConsumablesValue = true;

			RefreshValues();
		}

		[DataSourceProperty]
		public string SimpleAIText
		{
			get
			{
				return this._simpleAIText;
			}
		}

		[DataSourceProperty]
		public string ResupplyText
		{
			get
			{
				return this._resupplyText;
			}
		}

		[DataSourceProperty]
		public string InfText
		{
			get
			{
				return this._infText;
			}
		}

		[DataSourceProperty]
		public string ConfigMenuText
		{
			get
			{
				return this._configMenuText;
			}
		}

		[DataSourceProperty]
		public string DoneText
		{
			get
			{
				return this._doneText;
			}
		}

		[DataSourceProperty]
		public string CancelText
		{
			get
			{
				return this._cancelText;
			}
		}

		[DataSourceProperty]
		public string LockHintText
		{
			get
			{
				return this._lockHintText;
			}
		}

		[DataSourceProperty]
		public string BuyText
		{
			get
			{
				return this._buyText;
			}
		}

		[DataSourceProperty]
		public string SellText
		{
			get
			{
				return this._sellText;
			}
		}

		[DataSourceProperty]
		public string DefaultText
		{
			get
			{
				return this._defaultText;
			}
		}

		[DataSourceProperty]
		public string PresetsText
		{
			get
			{
				return this._presetsText;
			}
		}

		[DataSourceProperty]
		public string PresetsDefaultText
		{
			get
			{
				return this._presetsDefaultText;
			}
		}

		[DataSourceProperty]
		public string PresetsSellerText
		{
			get
			{
				return this._presetsSellerText;
			}
		}

		[DataSourceProperty]
		public string PresetsGlobalText
		{
			get
			{
				return this._presetsGlobalText;
			}
		}

		[DataSourceProperty]
		public string PresetsLocalText
		{
			get
			{
				return this._presetsLocalText;
			}
		}

		[DataSourceProperty]
		public string HeaderBaseText
		{
			get
			{
				return this._headerBaseText;
			}
		}

		[DataSourceProperty]
		public string HeaderCustomText
		{
			get
			{
				return this._headerCustomText;
			}
		}

		[DataSourceProperty]
		public string HeaderSpecialText
		{
			get
			{
				return this._headerSpecialText;
			}
		}

		[DataSourceProperty]
		public string HeaderTradeText
		{
			get
			{
				return this._headerTradeText;
			}
		}

		[DataSourceProperty]
		public string BuyThresholdText
		{
			get
			{
				return this._buyThresholdText;
			}
		}

		[DataSourceProperty]
		public string SellThresholdText
		{
			get
			{
				return this._sellThresholdText;
			}
		}

		[DataSourceProperty]
		public string MaxCapacityText
		{
			get
			{
				return this._maxCapacityText;
			}
		}

		[DataSourceProperty]
		public string UseInventorySpaceText
		{
			get
			{
				return this._useInventorySpaceText;
			}
		}

		[DataSourceProperty]
		public string KeepWagesText
		{
			get
			{
				return this._keepWagesText;
			}
		}

		[DataSourceProperty]
		public string SearchRadiusText
		{
			get
			{
				return this._searchRadiusText;
			}
		}

		[DataSourceProperty]
		public string WeaponTierText
		{
			get
			{
				return this._weaponTierText;
			}
		}

		[DataSourceProperty]
		public string ArmorTierText
		{
			get
			{
				return this._armorTierText;
			}
		}

		[DataSourceProperty]
		public string SellSmithingText
		{
			get
			{
				return this._sellSmithingText;
			}
		}

		[DataSourceProperty]
		public string TradeHorsesText
		{
			get
			{
				return this._tradeHorsesText;
			}
		}

		[DataSourceProperty]
		public string TradeWeaponsText
		{
			get
			{
				return this._tradeWeaponsText;
			}
		}

		[DataSourceProperty]
		public string TradeArmorText
		{
			get
			{
				return this._tradeArmorText;
			}
		}

		[DataSourceProperty]
		public string TradeGoodsText
		{
			get
			{
				return this._tradeGoodsText;
			}
		}

		[DataSourceProperty]
		public string TradeConsumablesText
		{
			get
			{
				return this._tradeConsumablesText;
			}
		}

		[DataSourceProperty]
		public string TradeLivestockText
		{
			get
			{
				return this._tradeLivestockText;
			}
		}

		[DataSourceProperty]
		public bool IsSearchRadiusActive
		{
			get
			{
				return this._isSearchRadiusActive;
			}
			set
			{
				if (value != this._isSearchRadiusActive)
				{
					this._isSearchRadiusActive = value;
					base.OnPropertyChanged("IsSearchRadiusActive");
				}
			}
		}

		[DataSourceProperty]
		public bool SimpleAIValue
		{
			get
			{
				return this._simpleAIValue;
			}
			set
			{
				if (value != this._simpleAIValue)
				{
					this._simpleAIValue = value;
					this.IsSearchRadiusActive = !this._simpleAIValue;

					if (this._simpleAIValue)
					{
						this.SellThresholdValue = 125.0f;
						this.BuyThresholdValue = 80.0f;
					}
					else
					{
						this.SellThresholdValue = 95.0f;
						this.BuyThresholdValue = 70.0f;
					}

					base.OnPropertyChanged("SimpleAIValue");
				}
			}
		}

		[DataSourceProperty]
		public bool SellSmithingValue
		{
			get
			{
				return this._sellSmithingValue;
			}
			set
			{
				if (value != this._sellSmithingValue)
				{
					this._sellSmithingValue = value;
					base.OnPropertyChanged("SellSmithingValue");
				}
			}
		}

		[DataSourceProperty]
		public bool ResupplyValue
		{
			get
			{
				return this._resupplyValue;
			}
			set
			{
				if (value != this._resupplyValue)
				{
					this._resupplyValue = value;
					if (value)
						this.BuyConsumablesValue = true;
					base.OnPropertyChanged("ResupplyValue");
				}
			}
		}

		[DataSourceProperty]
		public bool BuyHorsesValue
		{
			get
			{
				return this._buyHorsesValue;
			}
			set
			{
				if (value != this._buyHorsesValue)
				{
					this._buyHorsesValue = value;
					base.OnPropertyChanged("BuyHorsesValue");
				}
			}
		}

		[DataSourceProperty]
		public bool BuyWeaponsValue
		{
			get
			{
				return this._buyWeaponsValue;
			}
			set
			{
				if (value != this._buyWeaponsValue)
				{
					this._buyWeaponsValue = value;
					base.OnPropertyChanged("BuyWeaponsValue");
				}
			}
		}

		[DataSourceProperty]
		public bool BuyArmorValue
		{
			get
			{
				return this._buyArmorValue;
			}
			set
			{
				if (value != this._buyArmorValue)
				{
					this._buyArmorValue = value;
					base.OnPropertyChanged("BuyArmorValue");
				}
			}
		}

		[DataSourceProperty]
		public bool BuyGoodsValue
		{
			get
			{
				return this._buyGoodsValue;
			}
			set
			{
				if (value != this._buyGoodsValue)
				{
					this._buyGoodsValue = value;
					base.OnPropertyChanged("BuyGoodsValue");
				}
			}
		}

		[DataSourceProperty]
		public bool BuyConsumablesValue
		{
			get
			{
				return this._buyConsumablesValue;
			}
			set
			{
				if (value != this._buyConsumablesValue)
				{
					this._buyConsumablesValue = value;
					if (!value)
						this.ResupplyValue = false;
					base.OnPropertyChanged("BuyConsumablesValue");
				}
			}
		}

		[DataSourceProperty]
		public bool BuyLivestockValue
		{
			get
			{
				return this._buyLivestockValue;
			}
			set
			{
				if (value != this._buyLivestockValue)
				{
					this._buyLivestockValue = value;
					base.OnPropertyChanged("BuyLivestockValue");
				}
			}
		}

		[DataSourceProperty]
		public bool SellHorsesValue
		{
			get
			{
				return this._sellHorsesValue;
			}
			set
			{
				if (value != this._sellHorsesValue)
				{
					this._sellHorsesValue = value;
					base.OnPropertyChanged("SellHorsesValue");
				}
			}
		}

		[DataSourceProperty]
		public bool SellWeaponsValue
		{
			get
			{
				return this._sellWeaponsValue;
			}
			set
			{
				if (value != this._sellWeaponsValue)
				{
					this._sellWeaponsValue = value;
					base.OnPropertyChanged("SellWeaponsValue");
				}
			}
		}

		[DataSourceProperty]
		public bool SellArmorValue
		{
			get
			{
				return this._sellArmorValue;
			}
			set
			{
				if (value != this._sellArmorValue)
				{
					this._sellArmorValue = value;
					base.OnPropertyChanged("SellArmorValue");
				}
			}
		}

		[DataSourceProperty]
		public bool SellGoodsValue
		{
			get
			{
				return this._sellGoodsValue;
			}
			set
			{
				if (value != this._sellGoodsValue)
				{
					this._sellGoodsValue = value;
					base.OnPropertyChanged("SellGoodsValue");
				}
			}
		}

		[DataSourceProperty]
		public bool SellConsumablesValue
		{
			get
			{
				return this._sellConsumablesValue;
			}
			set
			{
				if (value != this._sellConsumablesValue)
				{
					this._sellConsumablesValue = value;
					base.OnPropertyChanged("SellConsumablesValue");
				}
			}
		}

		[DataSourceProperty]
		public bool SellLivestockValue
		{
			get
			{
				return this._sellLivestockValue;
			}
			set
			{
				if (value != this._sellLivestockValue)
				{
					this._sellLivestockValue = value;
					base.OnPropertyChanged("SellLivestockValue");
				}
			}
		}

		[DataSourceProperty]
		public string KeepGrainsText
		{
			get
			{
				return this._keepGrainsText;
			}
		}

		[DataSourceProperty]
		public string KeepConsumablesText
		{
			get
			{
				return this._keepConsumablesText;
			}
		}


		[DataSourceProperty]
		public float BuyThresholdValue
		{
			get
			{
				return this._buyThresholdValue;
			}
			set
			{
				if (value != this._buyThresholdValue)
				{
					this._buyThresholdValue = value;
					this.BuyThresholdValueAsString = this.BuyThresholdValue.ToString();
					base.OnPropertyChanged("BuyThresholdValue");
				}
			}
		}

		[DataSourceProperty]
		public string BuyThresholdValueAsString
		{
			get
			{
				return this._buyThresholdValueAsString;
			}
			set
			{
				if (value != this._buyThresholdValueAsString)
				{
					this._buyThresholdValueAsString = value;
					base.OnPropertyChanged("BuyThresholdValueAsString");
				}
			}
		}

		[DataSourceProperty]
		public float SellThresholdValue
		{
			get
			{
				return this._sellThresholdValue;
			}
			set
			{
				if (value != this._sellThresholdValue)
				{
					this._sellThresholdValue = value;
					this.SellThresholdValueAsString = this.SellThresholdValue.ToString();
					base.OnPropertyChanged("SellThresholdValue");
				}
			}
		}

		[DataSourceProperty]
		public string SellThresholdValueAsString
		{
			get
			{
				return this._sellThresholdValueAsString;
			}
			set
			{
				if (value != this._sellThresholdValueAsString)
				{
					this._sellThresholdValueAsString = value;
					base.OnPropertyChanged("SellThresholdValueAsString");
				}
			}
		}

		[DataSourceProperty]
		public float MaxCapacityValue
		{
			get
			{
				return this._maxCapacityValue;
			}
			set
			{
				if (value != this._maxCapacityValue)
				{
					this._maxCapacityValue = value;
					this.MaxCapacityValueAsString = this.MaxCapacityValue.ToString();
					base.OnPropertyChanged("MaxCapacityValue");
				}
			}
		}

		[DataSourceProperty]
		public string MaxCapacityValueAsString
		{
			get
			{
				return this._maxCapacityValueAsString;
			}
			set
			{
				if (value != this._maxCapacityValueAsString)
				{
					this._maxCapacityValueAsString = value;
					base.OnPropertyChanged("MaxCapacityValueAsString");
				}
			}
		}

		[DataSourceProperty]
		public float KeepGrainsValue
		{
			get
			{
				return this._keepGrainsValue;
			}
			set
			{
				if (value != this._keepGrainsValue)
				{
					this._keepGrainsValue = value;
					if (this._keepGrainsValue >= AutoTraderConfig.MaxKeepGrainsValue)
						this.KeepGrainsValueAsString = new TextObject("{=ATAll}All", null).ToString();
					else
						this.KeepGrainsValueAsString = this.KeepGrainsValue.ToString();
					base.OnPropertyChanged("KeepGrainsValue");
				}
			}
		}

		[DataSourceProperty]
		public string KeepGrainsValueAsString
		{
			get
			{
				return this._keepGrainsValueAsString;
			}
			set
			{
				if (value != this._keepGrainsValueAsString)
				{
					this._keepGrainsValueAsString = value;
					base.OnPropertyChanged("KeepGrainsValueAsString");
				}
			}
		}

		[DataSourceProperty]
		public float KeepConsumablesValue
		{
			get
			{
				return this._keepConsumablesValue;
			}
			set
			{
				if (value != this._keepConsumablesValue)
				{
					this._keepConsumablesValue = value;
					this.KeepConsumablesValueAsString = this.KeepConsumablesValue.ToString();
					base.OnPropertyChanged("KeepConsumablesValue");
				}
			}
		}

		[DataSourceProperty]
		public string KeepConsumablesValueAsString
		{
			get
			{
				return this._keepConsumablesValueAsString;
			}
			set
			{
				if (value != this._keepConsumablesValueAsString)
				{
					this._keepConsumablesValueAsString = value;
					base.OnPropertyChanged("KeepConsumablesValueAsString");
				}
			}
		}

		[DataSourceProperty]
		public float UseInventorySpaceValue
		{
			get
			{
				return this._useInventorySpaceValue;
			}
			set
			{
				if (value != this._useInventorySpaceValue)
				{
					this._useInventorySpaceValue = value;
					this.UseInventorySpaceValueAsString = this.UseInventorySpaceValue.ToString();
					base.OnPropertyChanged("UseInventorySpaceValue");
				}
			}
		}

		[DataSourceProperty]
		public string UseInventorySpaceValueAsString
		{
			get
			{
				return this._useInventorySpaceValueAsString;
			}
			set
			{
				if (value != this._useInventorySpaceValueAsString)
				{
					this._useInventorySpaceValueAsString = value;
					base.OnPropertyChanged("UseInventorySpaceValueAsString");
				}
			}
		}

		[DataSourceProperty]
		public float KeepWagesValue
		{
			get
			{
				return this._keepWagesValue;
			}
			set
			{
				if (value != this._keepWagesValue)
				{
					this._keepWagesValue = value;
					this.KeepWagesValueAsString = this.KeepWagesValue.ToString();
					base.OnPropertyChanged("KeepWagesValue");
				}
			}
		}

		[DataSourceProperty]
		public string KeepWagesValueAsString
		{
			get
			{
				return this._keepWagesValueAsString;
			}
			set
			{
				if (value != this._keepWagesValueAsString)
				{
					this._keepWagesValueAsString = value;
					base.OnPropertyChanged("KeepWagesValueAsString");
				}
			}
		}

		[DataSourceProperty]
		public float SearchRadiusValue
		{
			get
			{
				return this._searchRadiusValue;
			}
			set
			{
				if (value != this._searchRadiusValue)
				{
					this._searchRadiusValue = value;
					if (this._searchRadiusValue > 999.0f)
						this.SearchRadiusValueAsString = this.InfText;
					else
						this.SearchRadiusValueAsString = this.SearchRadiusValue.ToString();
					base.OnPropertyChanged("SearchRadiusValue");
				}
			}
		}

		[DataSourceProperty]
		public string SearchRadiusValueAsString
		{
			get
			{
				return this._searchRadiusValueAsString;
			}
			set
			{
				if (value != this._searchRadiusValueAsString)
				{
					this._searchRadiusValueAsString = value;
					base.OnPropertyChanged("SearchRadiusValueAsString");
				}
			}
		}

		[DataSourceProperty]
		public float WeaponTierValue
		{
			get
			{
				return this._weaponTierValue;
			}
			set
			{
				if (value != this._weaponTierValue)
				{
					this._weaponTierValue = value;
					this.WeaponTierValueAsString = this.WeaponTierValue.ToString();
					base.OnPropertyChanged("WeaponTierValue");
				}
			}
		}

		[DataSourceProperty]
		public string WeaponTierValueAsString
		{
			get
			{
				return this._weaponTierValueAsString;
			}
			set
			{
				if (value != this._weaponTierValueAsString)
				{
					this._weaponTierValueAsString = value;
					base.OnPropertyChanged("WeaponTierValueAsString");
				}
			}
		}

		[DataSourceProperty]
		public float ArmorTierValue
		{
			get
			{
				return this._armorTierValue;
			}
			set
			{
				if (value != this._armorTierValue)
				{
					this._armorTierValue = value;
					this.ArmorTierValueAsString = this.ArmorTierValue.ToString();
					base.OnPropertyChanged("ArmorTierValue");
				}
			}
		}

		[DataSourceProperty]
		public string ArmorTierValueAsString
		{
			get
			{
				return this._armorTierValueAsString;
			}
			set
			{
				if (value != this._armorTierValueAsString)
				{
					this._armorTierValueAsString = value;
					base.OnPropertyChanged("ArmorTierValueAsString");
				}
			}
		}

	}
}
