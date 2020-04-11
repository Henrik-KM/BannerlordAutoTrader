using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;

namespace AutoTrader
{
    public class AutoTraderSubModule : MBSubModuleBase
    {
		protected override void OnSubModuleLoad()
		{
			base.OnSubModuleLoad();
			AutoTraderConfig.Initialize();
			Module.CurrentModule.AddInitialStateOption(new InitialStateOption("AutoTraderConfig", new TextObject("{=ATConfig}AutoTrader Options", null), 9998, delegate ()
			{
				ScreenManager.PushScreen(new AutoTradeMenuGauntletScreen());
			}, false));
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
