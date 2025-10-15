using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AutoTrader.GUI
{
    class AutoTradeMainMenuGauntletScreen : ScreenBase
    {
		private GauntletLayer _gauntletLayer;
		private AutoTraderMenuViewModel _viewModel;

		public AutoTradeMainMenuGauntletScreen()
		{
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();
			this._viewModel = new AutoTraderMenuViewModel(null);

			// Add and configure layers
			this._gauntletLayer = new GauntletLayer(1, "GauntletLayer");
			this._gauntletLayer.LoadMovie("AutoTraderConfigScreen", this._viewModel);
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);

			base.AddLayer(this._gauntletLayer);
		}

		protected override void OnFinalize()
		{
			// Cleanup
			base.OnFinalize();
			base.RemoveLayer(this._gauntletLayer);

			this._gauntletLayer = null;
			this._viewModel = null;
		}
	}
}
