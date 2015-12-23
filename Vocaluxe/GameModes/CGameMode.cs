using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vocaluxe.GameModes
{
    public abstract class CGameMode
    {
        public virtual bool IsNotesVisible(int p)
        {
            return true;
        }
        public virtual bool IsPointsVisible(int p)
        {
            return true;
        }
        public virtual bool IsPlayerInformationVisible(int p)
        {
            return true;
        }
    }
}
