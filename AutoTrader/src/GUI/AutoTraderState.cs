using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;

namespace AutoTrader.GUI
{
    class AutoTraderState : GameState
    {
		public IAutoTraderStateHandler Handler
		{
			get
			{
				return this._handler;
			}
			set
			{
				this._handler = value;
			}
		}

		private IAutoTraderStateHandler _handler;
	}
}
