using AutoTrader.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AutoTrader.Notification
{
    class AutoTraderNotificationViewModel : ViewModel
    {

        private string _greetingsText;
        private string _descriptionText;
        private string _shortcutText;
        private string _happyText;
        private string _showAgainText;
        private string _doneText;

        private bool _showAgainValue;

        public AutoTraderNotificationViewModel()
        {
            this._greetingsText = new TextObject("{=ATNGreet}Behold, traveler!", null).ToString();
            this._descriptionText = new TextObject("{=ATNDescr}The AutoTrader settings menu can also be opened ingame from the map screen by pressing", null).ToString();
            this._shortcutText = new TextObject("{=ATNShortcut}ALT + A", null).ToString();
            this._happyText = new TextObject("{=ATNHappy}You can disable this menu entry in the settings. Happy Autotrading!", null).ToString();
            this._showAgainText = new TextObject("{=ATNShowAgain}Don't show this notification again", null).ToString();
            this._doneText = new TextObject("{=ATNDone}Got it!", null).ToString();

            this.RefreshValues();
        }
        public override void RefreshValues()
        {
            this.ShowAgainValue = !AutoTraderConfig.ShowNotification;
            base.RefreshValues();
        }

        #region Callbacks

        private void ExecuteDone()
        {
            AutoTraderConfig.ShowNotification = !this._showAgainValue;

            AutoTraderConfig.Save();

            ScreenManager.PopScreen();
            ScreenManager.PushScreen(new AutoTradeMainMenuGauntletScreen());
        }

        #endregion

        #region GetSet

        [DataSourceProperty]
        public bool ShowAgainValue
        {
            get
            {
                return this._showAgainValue;
            }
            set
            {
                if (value != this._showAgainValue)
                {
                    this._showAgainValue = value;
                    base.OnPropertyChanged("ShowAgainValue");
                }
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
        public string ShowAgainText
        {
            get
            {
                return this._showAgainText;
            }
        }

        [DataSourceProperty]
        public string HappyText
        {
            get
            {
                return this._happyText;
            }
        }

        [DataSourceProperty]
        public string ShortcutText
        {
            get
            {
                return this._shortcutText;
            }
        }


        [DataSourceProperty]
        public string DescriptionText
        {
            get
            {
                return this._descriptionText;
            }
        }

        [DataSourceProperty]
        public string GreetingsText
        {
            get
            {
                return this._greetingsText;
            }
        }

        #endregion
    }
}
