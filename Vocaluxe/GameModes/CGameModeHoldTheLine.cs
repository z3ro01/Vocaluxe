using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

namespace Vocaluxe.GameModes
{
    class CGameModeHoldTheLine : CGameMode
    {
        private IMenu _Screen;
        private CSong _Song;
        private float _RequiredPercentage = 0.75f;
        private int _Winner = -1;

        private class CGModeHLineIndicators
        {
            public int ID;
            public float X;
            public float Y;
            public float W;
            public float H;
            public float P = 0;
            public float AvatarW;
            public float AvatarH;
            public float AvatarY;
            public float AvatarX;
            public int LastLine = -1;
            public int LastCalculatedLine = -1;
            public double MaxPoints;
            public bool Lost = false;
        }

        private List<CGModeHLineIndicators> _Indicators = new List<CGModeHLineIndicators>();

        public CGameModeHoldTheLine() : base()
        {
            _Song = CBase.Game.GetSong();
            _Screen = CBase.Graphics.GetScreen(EScreen.Sing);
            for (int i = 0; i < CBase.Game.GetNumPlayer(); i++)
            {
                //get players avatar position (we draw the indicator here)
                IMenuElement element;
                string target = "StaticAvatarP" + (i + 1) + "N"+CBase.Game.GetNumPlayer();
                try
                {
                    element = _Screen.GetElement(EType.Static, target);
                    _Indicators.Add(new CGModeHLineIndicators { 
                        ID = i, 
                        X = element.Rect.X, 
                        Y = element.Rect.Y - 15, 
                        W = element.Rect.W, 
                        H = 15,
                        AvatarW = element.Rect.W, 
                        AvatarH = element.Rect.H,
                        AvatarX = element.Rect.X,
                        AvatarY = element.Rect.Y,
                        P = 0.5f, 
                        LastLine = -1, 
                        Lost = false });
                }
                catch
                {
                }
            }
        }

        public override bool IsNotesVisible(int p)
        {
            if (_Indicators[p].Lost)
                return false;
            return true;
        }

        public override bool IsPlayerInformationVisible(int p)
        {
            return true;
        }

        public override bool IsPointsVisible(int p)
        {
            if (_Indicators[p].Lost)
                return false;
            return true;
        }

        public override bool IsPlayerFinished(int p, double points, double pointsGolden, double pointsLineBonus)
        {
            if (_Winner > -1 && _Winner == p)
                return true;
            return false;
        }

        public override void OnUpdate(float time)
        {
            base.OnUpdate(time);
            if (_Winner > -1)
                return;

            int beat = (int)Math.Floor(CBase.Game.GetBeatFromTime(time, _Song.BPM, _Song.Gap));
            var players = CBase.Game.GetPlayers();
            for (int i = 0; i < CBase.Game.GetNumPlayer(); i++)
            {
                if (!_Indicators[i].Lost) {
                    CSongLine[] lines = _Song.Notes.GetVoice(players[i].VoiceNr).Lines;
                    var line = players[i].CurrentLine-1;
                    if (line < 0) { continue; }
                    if (lines[line].Points > 0 && _Indicators[i].LastLine < line)
                    {
                        //calculate the maximum available score per line (without line bonuses) 
                        double points = 0;
                        for (int n = 0; n < lines[line].Notes.Count(); n++)
                        {
                            points += (9000) * (double)lines[line].Notes[n].Points / _Song.Notes.GetVoice(players[i].VoiceNr).Points;
                        }
                        _Indicators[i].MaxPoints += points;
                        _Indicators[i].LastLine = line;
                    }

                    //set requiredPercentage by player difficulty
                    switch (CBase.Profiles.GetDifficulty(players[i].ProfileID))
                    {
                        case EGameDifficulty.TR_CONFIG_EASY:
                            _RequiredPercentage = 0.70f;
                            break;
                        case EGameDifficulty.TR_CONFIG_NORMAL:
                            _RequiredPercentage = 0.75f;
                            break;
                        case EGameDifficulty.TR_CONFIG_HARD:
                            _RequiredPercentage = 0.8f;
                            break;
                        default:
                            _RequiredPercentage = 0.75f;
                            break;
                    }

                    if (_Indicators[i].LastLine > -1) {

                        if (_Indicators[i].LastCalculatedLine != _Indicators[i].LastLine && lines[_Indicators[i].LastLine].LastNoteBeat < beat)
                        {
                            float _limit = ((float)_Indicators[i].MaxPoints * _RequiredPercentage);
                            float _rating = (float)players[i].Points / _limit;
                            if (_rating < 1)
                            {
                                _Indicators[i].P = 0;
                                _Indicators[i].Lost = true;
                                _Winner = _CheckWinner();
                                if (_Winner > -1)
                                    break;
                            }
                            else
                            {
                                float _newPercentage = (float)Math.Round(((float)players[i].Points - _limit) / ((float)_Indicators[i].MaxPoints-_limit), 2);
                                //can be higher than 1 (linebonuses!)
                                _Indicators[i].P = _newPercentage.Clamp(0, 1);
                            }
                            _Indicators[i].LastCalculatedLine = _Indicators[i].LastLine;
                        }
                    }
                }
            }
        }

        public override void OnDraw(float time)
        {
            base.OnDraw(time);

            if (_Indicators.Count > 0)
            {
                for (int i = 0; i < _Indicators.Count; i++)
                {
                    if (_Indicators[i].Lost != true) {
                        float alpha;
                        if (_Indicators[i].P < 0.3f)
                        {
                            alpha = (float)((Math.Cos(time * Math.PI * 2) + 1) / 2.0) / 2f + 0.5f;
                        }
                        else
                        {
                            alpha = 1;
                        }
                        //draw bar
                        var color = new SColorF(1, 1, 1, alpha);
                        var rect = new SRectF(_Indicators[i].X, _Indicators[i].Y, _Indicators[i].W, _Indicators[i].H, -2.01f);
                        CBase.Drawing.DrawRect(color, rect);

                        //current progress
                        SColorF pcolor;

                        var width = _Indicators[i].W * _Indicators[i].P;
                        if (_Indicators[i].P < 0.33)
                        {
                            pcolor = new SColorF(0.870f, 0.243f, 0.243f, alpha);
                        }
                        else if (_Indicators[i].P < 0.66)
                        {
                            pcolor = new SColorF(0.870f, 0.576f, 0.243f, alpha);
                        }
                        else if (_Indicators[i].P > 0.66)
                        {
                            pcolor = new SColorF(0.450f, 0.870f, 0.243f, alpha);
                        }
                        else
                        {
                            pcolor = new SColorF(0.870f, 0.576f, 0.243f, alpha);
                        }

                        var prect = new SRectF(_Indicators[i].X, _Indicators[i].Y+1, width,  _Indicators[i].H-2, -2.02f);
                        CBase.Drawing.DrawRect(pcolor, prect);

                        //sections
                        var sc = new SColorF(0.745f, 0.745f, 0.745f, alpha);
                        var bw = (_Indicators[i].W - 6) / 3;
                        CBase.Drawing.DrawRect(sc, new SRectF(_Indicators[i].X + (bw), _Indicators[i].Y, 3, _Indicators[i].H, -2.03f));
                        CBase.Drawing.DrawRect(sc, new SRectF(_Indicators[i].X + (bw*2) + 3, _Indicators[i].Y, 3, _Indicators[i].H, -2.03f));
                    }
                    //draw red X over avatar
                    else
                    {
                        var alpha = (float)((Math.Cos(time * Math.PI * 2) + 1) / 2.0) / 2f + 0.4f;
                        var red = new SColorF(0.743f, 0, 0, alpha);
                        double hyp = Math.Sqrt(Math.Pow((double)_Indicators[i].AvatarW, 2) + Math.Pow((double)_Indicators[i].AvatarH, 2));
                        double ang = Math.Sin(_Indicators[i].AvatarW / hyp);
                        var rectx = new SRectF(_Indicators[i].AvatarX + (_Indicators[i].AvatarW / 2) - 10, _Indicators[i].AvatarY - (((float)hyp - _Indicators[i].AvatarH) / 2), 20, (float)hyp, -2.01f);
                        rectx.Rotation = (float)-(ang * 180 / Math.PI);
                        CBase.Drawing.DrawRect(red, rectx);
                        rectx.Rotation = (float)(ang * 180 / Math.PI);
                        CBase.Drawing.DrawRect(red, rectx);
                    }
                }
            }
        }

        private int _CheckWinner()
        {
            int plnum = 0; 
            int winner = 0;
            for (int i = 0; i < _Indicators.Count(); i++)
            {
                if (!_Indicators[i].Lost) { 
                    plnum++;
                    winner = i;
                }
            }
            
            if (plnum == 1)
                return winner;
            return -1;
        }
    }
}
