using System;
using System.Collections.Generic;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.TicTacToe
{
    public class CPartyScreenTicTacToeGameModes : CMenuPartyGameModeSelection
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private new CPartyModeTicTacToe _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeTicTacToe)base._PartyMode;
        }

        public override void Back()
        {
            _PartyMode.Back();
        }

        public override void Next()
        {
            _PartyMode.GameData.GameModesAvailable.Clear();
            _PartyMode.GameData.GameModesAvailable.AddRange(_SelectedModes);
            _PartyMode.Next();
        }

        public override bool UpdateGame()
        {
            return base.UpdateGame();
        }
    }
}
