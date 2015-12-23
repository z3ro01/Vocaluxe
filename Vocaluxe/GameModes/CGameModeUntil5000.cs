using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vocaluxe.GameModes
{
    public class CGameModeUntil5000 : CGameMode
    {
        public override bool IsPlayerFinished(int p, double points, double pointsGolden, double pointsLineBonus)
        {
            if (points >= 5000)
                return true;
            return false;
        }
    }
}
