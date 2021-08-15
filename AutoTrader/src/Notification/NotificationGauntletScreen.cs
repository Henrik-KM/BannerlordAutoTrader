using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;

namespace AutoTrader.Notification
{
    class NotificationGauntletScreen : ScreenBase
	{
		private GauntletLayer _gauntletLayer;
		private AutoTraderNotificationViewModel _viewModel;

		public NotificationGauntletScreen()
		{
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();
			this._viewModel = new AutoTraderNotificationViewModel();

			// Add and configure layers
			this._gauntletLayer = new GauntletLayer(1, "GauntletLayer");
			this._gauntletLayer.LoadMovie("AutoTraderNotification", this._viewModel);
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
