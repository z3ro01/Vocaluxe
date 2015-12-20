#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocaluxeLib.Community
{
    public static class CPopupHelper
    {
        public static void Confirm(string Title, string Message, Action<SPopupGeneralEvent> callback = null, SPopupGeneral wndopts = new SPopupGeneral())
        {
            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            popup.SetDefaults();
            if (callback == null)
            {
                popup.AddEventHandler("onKeyReturn,onKeyEscape,onMouseLB", delegate(SPopupGeneralEvent eventData)
                {
                    if (eventData.Name.Equals("onKeyEscape")
                        || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonNo"))
                        || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonNo"))
                    )
                    {
                        CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                    }
                });
            }
            else { popup.AddEventHandler("onKeyReturn,onKeyEscape,onMouseLB", callback); }

           // SPopupGeneral data = new SPopupGeneral();
            if (wndopts.Size == EPopupGeneralSize.Small)
            {
                wndopts.Size = EPopupGeneralSize.Medium;
            }
            wndopts.Type = EPopupGeneralType.Confirm;

            if (wndopts.ButtonYesLabel == null)
                wndopts.ButtonYesLabel = CBase.Language.Translate("TR_COMMUNITY_BTNYES");
            if (wndopts.ButtonNoLabel == null)
                wndopts.ButtonNoLabel = CBase.Language.Translate("TR_COMMUNITY_BTNNO");

            wndopts.TextTitle   = Translate(Title);
            wndopts.TextMessage = Translate(Message, true);
            popup.SetDisplayData(wndopts);
            CBase.Graphics.ShowPopup(EPopupScreens.PopupGeneral);
        }

        public static void Alert(string Title, string Message, Action<SPopupGeneralEvent> callback = null)
        {
            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            popup.SetDefaults();
            if (callback == null)
            {
                popup.AddEventHandler("onKeyReturn,onKeyEscape,onMouseLB", delegate(SPopupGeneralEvent eventData)
                {
                    if (eventData.Name.Equals("onKeyEscape")
                        || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonOk"))
                        || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonOk"))
                    )
                    {
                        CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                    }
                });
            }
            else { popup.AddEventHandler("onKeyReturn,onKeyEscape,onMouseLB", callback); }

            SPopupGeneral data = new SPopupGeneral();
            data.Type = EPopupGeneralType.Alert;
            data.Size = EPopupGeneralSize.Medium;
            data.ButtonOkLabel = CBase.Language.Translate("TR_COMMUNITY_BTNOK");
            data.TextTitle = Translate(Title);
            data.TextMessage = Translate(Message,true);
            popup.SetDisplayData(data);

            CBase.Graphics.ShowPopup(EPopupScreens.PopupGeneral);
        }

        public static void Download(string Title, string Message, Action<SPopupGeneralEvent> callback = null)
        {
            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            popup.SetDefaults();
            if (callback != null) { popup.AddEventHandler("onKeyReturn,onKeyEscape,onMouseLB", callback); }
            SPopupGeneral data = new SPopupGeneral();
            data.Type = EPopupGeneralType.Loading;
            data.Size = EPopupGeneralSize.Medium;
            data.ButtonOkLabel = Translate("TR_COMMUNITY_BTNOK");
            data.TextTitle = Translate(Title);
            data.ProgressBar1Title = Translate(Message);
            data.ProgressBar1Visible = true;
            popup.SetDisplayData(data);
            CBase.Graphics.ShowPopup(EPopupScreens.PopupGeneral);
        }

        public static void Login(string Title, string Message, Action<SPopupGeneralEvent> callback)
        {
            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            popup.SetDefaults();
            popup.AddEventHandler("onKeyReturn,onKeyEscape,onMouseLB", callback);
            SPopupGeneral data = new SPopupGeneral();
            data.TextTitle = Translate(Title);
            data.TextMessage = Translate(Message,true);
            data.Type = EPopupGeneralType.Login;
            data.Size = EPopupGeneralSize.Medium;
            data.ButtonNoLabel = Translate("TR_COMMUNITY_BTNCANCEL");
            data.ButtonYesLabel = Translate("TR_COMMUNITY_BTNLOGIN");
            popup.SetDisplayData(data);
            CBase.Graphics.ShowPopup(EPopupScreens.PopupGeneral);
        }


        public static void Loading(string Title, string Message, Action<SPopupGeneralEvent> callback = null)
        {
            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            popup.SetDefaults();
            if (callback != null) { popup.AddEventHandler("onKeyReturn,onKeyEscape,onMouseLB", callback); }
            SPopupGeneral data = new SPopupGeneral();
            data.TextTitle = Translate(Title);
            data.TextMessage = Translate(Message);
            data.Type = EPopupGeneralType.Loading;
            data.Size = EPopupGeneralSize.Small;
            popup.SetDisplayData(data);
            CBase.Graphics.ShowPopup(EPopupScreens.PopupGeneral);
        }

        private static string Translate(string text, bool multiline = false)
        {
            String translated = text;
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^[A-Z0-9_]+$"))
            {
                translated = CBase.Language.Translate(text);
            }
            if (multiline)
                return translated.Replace("\\n", "\n");

            return translated;
        }
    }
}
