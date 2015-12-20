﻿#region license
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
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenTest : CMenu
    {
        private System.Timers.Timer progressTimer = new System.Timers.Timer(100);
        private SPopupGeneralProgress pb1 = new SPopupGeneralProgress();
        private SPopupGeneralProgress pb2 = new SPopupGeneralProgress();

        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        /*
        private int _TestMusic = -1;
*/

        public override void Init()
        {
            base.Init();
            const string test = "Ö ÄÜabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPGRSTUVWGXZ1234567890";
            var color = new SColorF(1, 0, 0, 1);
            var text = new CText(10, 50, 1, 32, 0, EAlignment.Left, EStyle.Normal, "Normal", color, "jÄNormal Text" + test, -1, 26, 1);
            _AddText(text);
            text = new CText(10, 90, 1, 32, 0, EAlignment.Left, EStyle.Bold, "Normal", color, "jÄBold Text" + test, -1, 26, 1);
            _AddText(text);
            text = new CText(10, 130, 1, 32, 0, EAlignment.Left, EStyle.Italic, "Normal", color, "jÄItalic Text" + test, -1, 26, 1);
            _AddText(text);
            text = new CText(10, 170, 1, 32, 0, EAlignment.Left, EStyle.Normal, "Outline", color, "jÄNormal Text" + test, -1, 50, 1);
            _AddText(text);
            text = new CText(10, 210, 1, 32, 0, EAlignment.Left, EStyle.Bold, "Outline", color, "jÄBold Text" + test, -1, 100, 1);
            _AddText(text);
            text = new CText(10, 250, 1, 32, 0, EAlignment.Left, EStyle.Italic, "Outline", color, "jÄItalic Text" + test, -1, 150, 1);
            _AddText(text);
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode)) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        CGraphics.FadeTo(EScreen.Main);
                        break;

                    case Keys.Enter:
                        CGraphics.FadeTo(EScreen.Main);
                        break;

                    case Keys.D1:
                    case Keys.NumPad1:
                        PopupTest("Confirm.Small");
                        break;

                    case Keys.D2:
                    case Keys.NumPad2:
                        PopupTest("Confirm.Medium");
                        break;

                    case Keys.D3:
                    case Keys.NumPad3:
                        PopupTest("Confirm.Big");
                        break;

                    case Keys.D4:
                    case Keys.NumPad4:
                        PopupTest("Alert.Small");
                        break;

                    case Keys.D5:
                    case Keys.NumPad5:
                        PopupTest("Alert.Medium");
                        break;

                    case Keys.D6:
                    case Keys.NumPad6:
                        PopupTest("Alert.Big");
                        break;
                    
                    case Keys.D7:
                    case Keys.NumPad7:
                        PopupTest("Loading.Small");
                        break;

                    case Keys.D8:
                    case Keys.NumPad8:
                        PopupProgressTest(false);
                        break;

                    case Keys.D9:
                    case Keys.NumPad9:
                        PopupLoginTest();
                        break;

                    case Keys.F:
                        //FadeAndPause();
                        break;

                    case Keys.S:
                        //PlayFile();
                        break;

                    case Keys.P:
                        //PauseFile();
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent)) {}

            if (mouseEvent.LB)
                CGraphics.FadeTo(EScreen.Main);

            if (mouseEvent.RB)
                CGraphics.FadeTo(EScreen.Main);
            return true;
        }

        public override bool UpdateGame()
        {
            return true;
        }

        public void PopupTest(string type)
        {
            //get popup screen
            var popup = CGraphics.GetPopup(EPopupScreens.PopupGeneral);

            //reset eventhandlers
            popup.SetDefaults();
            //add new eventhandlers
            popup.AddEventHandler("onKeyReturn,onKeyEscape", (Action<SPopupGeneralEvent>)PopupCallback);
            popup.AddEventHandler("onMouseRB,onMouseLB", (Action<SPopupGeneralEvent>)PopupCallback);

            //popup options
            SPopupGeneral data = new SPopupGeneral();
            data.TextTitle = type;

            if (type.Equals("Confirm.Small"))
            {
                data.Type = EPopupGeneralType.Confirm;
                data.Size = EPopupGeneralSize.Small;
                data.ButtonYesLabel = "YES";
                data.ButtonNoLabel = "NO";
                data.TextMessage = "Its a small confirm test.";
            }
            else if (type.Equals("Confirm.Medium"))
            {
                data.Type = EPopupGeneralType.Confirm;
                data.Size = EPopupGeneralSize.Medium;
                data.ButtonYesLabel = "YES";
                data.ButtonNoLabel = "NO";
                data.DefaultButton = "ButtonYes";
                data.TextMessage = "Multiline text support!\nYou can create newlines.\nOr automatic break mode: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aliquam a tristique justo. Mauris sollicitudin ex vitae nulla facilisis, ut pulvinar urna blandit. Fusce cursus fringilla odio, at lobortis felis tempor ut. Duis faucibus et lacus in mattis. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Nullam et suscipit ante. Quisque mattis nisi et felis pellentesque congue. Proin eu iaculis eros. Phasellus eu velit in arcu iaculis consequat. Quisque laoreet, nisl sed vehicula molestie, massa mauris mollis dui, eget lobortis metus sem quis nulla. Nam massa tortor, vulputate in hendrerit in, iaculis ut ipsum. Duis sit amet iaculis turpis. In feugiat mauris sed neque convallis pretium. Aenean id justo eget odio consectetur maximus. Vestibulum eget lectus consectetur ligula lacinia finibus. Andlastaverylongwordthatcannotbecutalongspacessocutitbylengthfinally";
            }
            else if (type.Equals("Confirm.Big"))
            {
                data.Type = EPopupGeneralType.Confirm;
                data.Size = EPopupGeneralSize.Big;
                data.ButtonYesLabel = "YES";
                data.ButtonNoLabel = "NO";
                data.DefaultButton = "ButtonNo";
                data.TextMessage = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aliquam a tristique justo. Mauris sollicitudin ex vitae nulla facilisis, ut pulvinar urna blandit. Fusce cursus fringilla odio, at lobortis felis tempor ut. Duis faucibus et lacus in mattis. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Nullam et suscipit ante. Quisque mattis nisi et felis pellentesque congue. Proin eu iaculis eros. Phasellus eu velit in arcu iaculis consequat. Quisque laoreet, nisl sed vehicula molestie, massa mauris mollis dui, eget lobortis metus sem quis nulla. Nam massa tortor, vulputate in hendrerit in, iaculis ut ipsum. Duis sit amet iaculis turpis. In feugiat mauris sed neque convallis pretium. Aenean id justo eget odio consectetur maximus. Vestibulum eget lectus consectetur ligula lacinia finibus.";
                data.TextMessage += "\n \nQuisque elementum dui et sodales ultrices. Sed quam urna, pretium imperdiet molestie sit amet, facilisis ac diam. Aliquam interdum metus a efficitur egestas. Vivamus accumsan nulla sit amet eros semper, nec auctor neque ullamcorper. Sed eleifend pharetra est ut condimentum. Sed dolor quam, rhoncus eleifend augue et, sollicitudin vehicula arcu. Sed mi mauris, malesuada id risus quis, dapibus volutpat sem. Nullam eget orci tellus. Mauris consectetur volutpat nibh. In quis laoreet nibh, in hendrerit ligula. Integer turpis leo, suscipit vel pellentesque et, pretium at enim. Vestibulum rhoncus ultrices libero, in aliquam nibh accumsan ornare.";
                data.TextMessage += "\n \nVestibulum tincidunt tortor ac tempor elementum. Donec nec urna ut odio consequat pretium. Suspendisse potenti. Maecenas faucibus mauris sed leo condimentum bibendum. Praesent diam dui, gravida a turpis sed, faucibus pellentesque velit. Integer cursus neque nec elit porta sollicitudin. Mauris molestie condimentum ipsum, eu rhoncus elit bibendum eget. Proin gravida eu nulla et faucibus. Vivamus id nulla sed ex ornare fermentum sit amet non nisi. Sed semper lorem turpis, elementum vulputate metus fringilla at. Proin finibus nec diam sit amet venenatis. Curabitur in justo vehicula, vestibulum neque in, accumsan erat. Mauris sit amet consectetur sapien. Ut eleifend aliquam purus varius consectetur.";

            }
            else if (type.Equals("Alert.Small"))
            {
                data.Type = EPopupGeneralType.Alert;
                data.Size = EPopupGeneralSize.Small;
                data.ButtonOkLabel = "YES";
                data.TextMessage = "Its a small alert test.";
            }
            else if (type.Equals("Alert.Medium"))
            {
                data.Type = EPopupGeneralType.Alert;
                data.Size = EPopupGeneralSize.Medium;
                data.ButtonOkLabel = "YES";
                data.TextMessage = "Its a medium alert popup,\nwith multiline text support.\nDo you see? :)";
            }
            else if (type.Equals("Alert.Big"))
            {
                data.Type = EPopupGeneralType.Alert;
                data.Size = EPopupGeneralSize.Big;
                data.ButtonOkLabel = "YES";
                data.TextMessage = "Its a big alert popup,\nwith multiline text support.\nDo you see? :)";
            }
            else if (type.Equals("Loading.Small"))
            {
                data.Type = EPopupGeneralType.Loading;
                data.Size = EPopupGeneralSize.Small;
                data.TextMessage = "Loading simple text.";
                data.ButtonNoLabel = "Cancel";
            }

            //set popup display data
            popup.SetDisplayData(data);
            //and show it
            CGraphics.ShowPopup(EPopupScreens.PopupGeneral);
        }

        public void PopupLoginTest()
        {
            //get popup screen
            var popup = CGraphics.GetPopup(EPopupScreens.PopupGeneral);

            //reset eventhandlers
            popup.SetDefaults();
            //add new eventhandlers
            popup.AddEventHandler("onKeyReturn,onKeyEscape,onMouseLB", (Action<SPopupGeneralEvent>)PopupLoginCallback);

            SPopupGeneral data = new SPopupGeneral();
            data.TextTitle   = "Login to somewhere";
            data.TextMessage = "";
           
            data.Type = EPopupGeneralType.Login;
            data.Size = EPopupGeneralSize.Medium;

            data.ButtonNoLabel = "Cancel";
            data.ButtonYesLabel = "Login!";
            popup.SetDisplayData(data);
            CGraphics.ShowPopup(EPopupScreens.PopupGeneral);
        }

        public void PopupLoginCallback(SPopupGeneralEvent eventData)
        {
            if (eventData.Name.Equals("onKeyEscape")
                || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonNo"))
                || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonNo"))
            )
            {
                CGraphics.HidePopup(EPopupScreens.PopupGeneral);
            }
            else if ((eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonYes"))
                || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonYes")))
            {
                var popup = CGraphics.GetPopup(EPopupScreens.PopupGeneral);
                var data = popup.GetDisplayData();

                data.TextMessage = data.Username + " / " + data.Password + " --> Failed to login...";
                popup.SetDisplayData(data);
                CGraphics.ShowPopup(EPopupScreens.PopupGeneral);
            }
        }


        public void PopupProgressTest(Boolean cont)
        {
            var popup = CGraphics.GetPopup(EPopupScreens.PopupGeneral);
            //reset eventhandlers
            popup.SetDefaults();
            //add new eventhandlers
            popup.AddEventHandler("onMouseRB,onKeyEscape", (Action<SPopupGeneralEvent>)PopupProgressCallback);
           
            SPopupGeneral data = new SPopupGeneral();
            data.TextTitle = "Loading sample";
            data.Type = EPopupGeneralType.Loading;
            data.Size = EPopupGeneralSize.Medium;
            data.ButtonNoLabel = "Cancel";
            data.ProgressBar1Visible = true;
            data.ProgressBar2Visible = true;

            popup.SetDisplayData(data);
            if (!cont) {
                pb1.Target = 1;
                pb1.Percentage = 0;
            }
            pb1.Title = "Downloading xyz - zyx ...";
            popup.SetProgressData(pb1);


            pb2.Target = 2;
            if (!cont)
            {
                pb2.Total = 10;
                pb2.Loaded = 0;
                pb2.Title = "Downloading 1/10";
            }
            else
            {
                pb2.Title = "Downloading "+pb2.Loaded+"/"+pb2.Total;
            }
            
            popup.SetProgressData(pb2);

            CGraphics.ShowPopup(EPopupScreens.PopupGeneral);

            progressTimer.Elapsed += (timerSender, timerEvent) => updateProgressBars(timerSender, timerEvent);
            progressTimer.Enabled = true;
            progressTimer.Start();

           
        }

        public void updateProgressBars(object source, System.Timers.ElapsedEventArgs e)
        {
            var popup = CGraphics.GetPopup(EPopupScreens.PopupGeneral);
            if (pb1.Percentage < 100) { 
                 pb1.Percentage++;
                 popup.SetProgressData(pb1);
            }
            else if (pb1.Percentage == 100)
            {
                if (pb2.Loaded < pb2.Total)
                {
                    pb2.Loaded++;
                    pb1.Percentage = 0;
                    pb1.Title = "Downloading xyz / "+pb2.Loaded;
                    popup.SetProgressData(pb1);
                    pb2.Title = "Downloading " + pb2.Loaded + "/" + pb2.Total;
                    popup.SetProgressData(pb2);
                }
                else
                {
                    progressTimer.Enabled = false;
                    progressTimer.Stop();
                    CGraphics.HidePopup(EPopupScreens.PopupGeneral);
                }
            }
        }

        public void PopupProgressCallback(SPopupGeneralEvent eventData)
        {
            if (eventData.Name.Equals("onKeyEscape") || eventData.Name.Equals("onMouseRB"))
            {
                //create new popup
                var popup = CGraphics.GetPopup(EPopupScreens.PopupGeneral);
                popup.SetDefaults();
                popup.AddEventHandler("onKeyReturn,onKeyEscape,onMouseLB", (Action<SPopupGeneralEvent>)PopupProgressOnCancel);
                SPopupGeneral data = new SPopupGeneral();
                data.TextTitle = "Confirm";
                data.Type = EPopupGeneralType.Confirm;
                data.Size = EPopupGeneralSize.Small;
                data.ButtonYesLabel = "YES";
                data.ButtonNoLabel = "NO";
                data.TextMessage = "Do you want to cancel?";

                popup.SetDisplayData(data);
                CGraphics.ShowPopup(EPopupScreens.PopupGeneral);
            }
        }

        public void PopupProgressOnCancel(SPopupGeneralEvent eventData)
        {
            if (eventData.Name.Equals("onKeyReturn") || eventData.Name.Equals("onMouseLB"))
            {
                if (eventData.Target != null)
                {
                    if (eventData.Target.Equals("ButtonYes") || eventData.Target.Equals("ButtonOk"))
                    {
                        progressTimer.Enabled = false;
                        progressTimer.Stop();
                        CGraphics.HidePopup(EPopupScreens.PopupGeneral);
                    }
                    else if (eventData.Target.Equals("ButtonNo"))
                    {
                        PopupProgressTest(true);
                    }
                }
            }
            else if (eventData.Name.Equals("onKeyEscape"))
            {
                PopupProgressTest(true);
            }
        }

        public void PopupCallback(SPopupGeneralEvent eventData)
        {
            if (eventData.Name.Equals("onKeyReturn") || eventData.Name.Equals("onMouseLB"))
            {
                if (eventData.Target != null)
                {
                    if (eventData.Target.Equals("ButtonYes") || eventData.Target.Equals("ButtonOk"))
                    {
                        var currPopup = CGraphics.GetPopup(EPopupScreens.PopupGeneral);

                        Console.WriteLine("You selected YES!");
                        CGraphics.HidePopup(EPopupScreens.PopupGeneral);
                    }
                    else if (eventData.Target.Equals("ButtonNo"))
                    {
                        CGraphics.HidePopup(EPopupScreens.PopupGeneral);
                    }
                }
            }
            else if (eventData.Name.Equals("onKeyEscape") || eventData.Name.Equals("onMouseRB"))
            {
                CGraphics.HidePopup(EPopupScreens.PopupGeneral);
            }
        }

        /*
                private void _PlayFile()
                {
                    if (_TestMusic == -1)
                        _TestMusic = CSound.Load(Path.Combine(Environment.CurrentDirectory, "Test.mp3"));

                    CSound.Play(_TestMusic);
                    CSound.Fade(_TestMusic, 100f, 2f);
                }


                private void _PauseFile()
                {
                    CSound.Pause(_TestMusic);
                }

                private void _FadeAndPause()
                {
                    CSound.FadeAndPause(_TestMusic, 0f, 2f);
                }*/
    }
}