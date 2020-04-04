using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;

namespace AutoTrader
{
    public class AutoTraderSubModule : MBSubModuleBase
    {
		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			CampaignGameStarter gameInitializer = (CampaignGameStarter)gameStarterObject;

			this.AddBehaviors(gameInitializer);
		}

		private void AddBehaviors(CampaignGameStarter gameStarterObject)
		{
			gameStarterObject.AddBehavior(new TradeBehavior());
		}

	}
}
