using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;
using TaleWorlds.InputSystem;
using AutoTrader.GUI;
using AutoTrader.Notification;
using HarmonyLib;

namespace AutoTrader
{
    public class AutoTraderSubModule : MBSubModuleBase
    {
		public AutoTraderSubModule()
		{
			//var harmony = new Harmony("eskalior.autotrader");
			//harmony.PatchAll(typeof(AutoTraderSubModule).Assembly);
		}

		protected override void OnSubModuleLoad()
		{
			base.OnSubModuleLoad();
			AutoTraderConfig.Initialize();

			if (AutoTraderConfig.ShowMenuEntry || (AutoTraderConfig.ShowNotification && AutoTraderConfig.ShowMenuEntry))
			{
				Module.CurrentModule.AddInitialStateOption(new InitialStateOption("AutoTraderConfig", new TextObject("{=ATConfig}AutoTrader Options", null), 9998, delegate ()
				{
					if(AutoTraderConfig.ShowNotification)
						ScreenManager.PushScreen(new NotificationGauntletScreen());
					else
						ScreenManager.PushScreen(new AutoTradeMainMenuGauntletScreen());
				}, () => (false, null)));
			}
		}

		private void OpenSettingsMenu()
		{
			Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<AutoTraderState>(), 0);
		}


		protected override void OnApplicationTick(float dt)
		{
			if(Game.Current != null && !AutoTraderConfig.DisableOptionsHotkey && !AutoTraderLogic.IsTradingActive)
			{
				if(Input.IsKeyDown(InputKey.LeftAlt) 
					&& Input.IsKeyDown(InputKey.A) 
					&& Game.Current.GameStateManager.ActiveState.GetType() == typeof(MapState)
					&& Game.Current.GameStateManager.ActiveState.IsMenuState == false
					&& Game.Current.GameStateManager.ActiveState.IsMission == false)
				{
					OpenSettingsMenu();
				}
			}
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			if (game.GameType is Campaign)
			{
				CampaignGameStarter gameInitializer = (CampaignGameStarter)gameStarterObject;
				this.AddBehaviors(gameInitializer);
			}
		}

		private void AddBehaviors(CampaignGameStarter gameStarterObject)
		{
			gameStarterObject.AddBehavior(new TradeBehavior());
		}

	}
}
