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
        /*configuration*/
        private float _RequiredRatingEasy = 0.6f;
        private float _RequiredRatingNormal = 0.7f;
        private float _RequiredRatingHard = 0.8f;
        //dont kick players for the first x line.
        private float _Warmup = 2;
        //increase required rating to full at x percentage of current song
        private float _IncreaseToFull = 0.5f;
        //increase required rating randomly after reach required rating
        private float _IncreaseAfterFull = 0.01f;
        private int _IncreaseAfterFactor = 5;

        private IMenu _Screen;
        private CSong _Song;
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
            public int LastBeat = -1;
            public int FirstLine = -1;
            public float Rating = 1;
            public float RatingLimit = 0;
            public float RequiredRating = 0;
            public int LastLine = -1;
            public bool Lost = false;
        }

        private List<CGModeHLineIndicators> _Indicators = new List<CGModeHLineIndicators>();


        public CGameModeHoldTheLine() : base()
        {
            _Song = CBase.Game.GetSong();
            _Screen = CBase.Graphics.GetScreen(EScreen.Sing);
            var players = CBase.Game.GetPlayers();

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
                        P = 1, 
                        Lost = false });
                }
                catch
                {
                }

                CSongLine[] lines = _Song.Notes.GetVoice(players[i].VoiceNr).Lines;
                _Indicators[i].LastBeat = lines[lines.Count() - 1].EndBeat;

                switch (CBase.Profiles.GetDifficulty(players[i].ProfileID))
                {
                    case EGameDifficulty.TR_CONFIG_EASY:
                        _Indicators[i].RequiredRating = _RequiredRatingEasy;
                        break;
                    case EGameDifficulty.TR_CONFIG_NORMAL:
                        _Indicators[i].RequiredRating = _RequiredRatingNormal;
                        break;
                    case EGameDifficulty.TR_CONFIG_HARD:
                        _Indicators[i].RequiredRating = _RequiredRatingHard;
                        break;
                    default:
                        _Indicators[i].RequiredRating = _RequiredRatingNormal;
                        break;
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

        //calculate players rating
        public override void OnPointsUpdated(float time, float beat)
        {
            var players = CBase.Game.GetPlayers();
            for (int i = 0; i < CBase.Game.GetNumPlayer(); i++)
            {
                var maxScore = _GetMaxScore(i, beat);
                if (maxScore > 0 && beat <= _Indicators[i].LastBeat)
                {
                    var newRating = (float)(players[i].Points / maxScore);
                    //can be higher than 1 (linebonuses not calculated by _GetMaxScore, player can increase his line with linebonuses)
                    _Indicators[i].Rating = newRating.Clamp(0, 1);

                    if (_Indicators[i].FirstLine + _Warmup < players[i].CurrentLine) { 
                        if (beat <= 0)
                        {
                            _Indicators[i].RatingLimit = 0;
                        }
                        else if (beat < _Indicators[i].LastBeat * _IncreaseToFull)
                        {
                            _Indicators[i].RatingLimit = (beat / (_Indicators[i].LastBeat * _IncreaseToFull)) * _Indicators[i].RequiredRating;
                        }
                        else
                        {
                            _Indicators[i].RatingLimit = _Indicators[i].RequiredRating;
                            //make it harder
                            if (_Indicators[i].LastLine < players[i].CurrentLine)
                            {
                                _IncreaseRequiredRatings();
                                _Indicators[i].LastLine = players[i].CurrentLine;
                            }
                        }

                        if (_Indicators[i].Rating < _Indicators[i].RatingLimit)
                        {
                            _Indicators[i].Lost = true;
                            players[i].SongFinished = true;
                        
                            _Winner = _CheckWinner();
                            if (_Winner > -1)
                                break;
                        }
                    }

                    _Indicators[i].P = _Indicators[i].Rating;
                }
            }
        }

        public override void OnUpdate(float time)
        {
            return;
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
                        if (_Indicators[i].P - _Indicators[i].RatingLimit < 0.1f)
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

                        //current rating
                        SColorF pcolor;
                        var width = _Indicators[i].W * _Indicators[i].P;
                        if (_Indicators[i].Rating < 0.33)
                        {
                            pcolor = new SColorF(0.870f, 0.243f, 0.243f, alpha);
                        }
                        else if (_Indicators[i].Rating < 0.66)
                        {
                            pcolor = new SColorF(0.870f, 0.576f, 0.243f, alpha);
                        }
                        else if (_Indicators[i].Rating > 0.66)
                        {
                            pcolor = new SColorF(0.450f, 0.870f, 0.243f, alpha);
                        }
                        else
                        {
                            pcolor = new SColorF(0.870f, 0.576f, 0.243f, alpha);
                        }

                        var prect = new SRectF(_Indicators[i].X, _Indicators[i].Y+1, width,  _Indicators[i].H-2, -2.02f);
                        CBase.Drawing.DrawRect(pcolor, prect);

                        //current ratingLimit
                        CBase.Drawing.DrawRect(new SColorF(0f, 0f, 0f, 0.3f), new SRectF(_Indicators[i].X, _Indicators[i].Y, _Indicators[i].W * _Indicators[i].RatingLimit, _Indicators[i].H, -2.04f));

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

        private void _IncreaseRequiredRatings()
        {
            var rand = new Random();
            if (_IncreaseAfterFull > 0 && rand.Next(1, _IncreaseAfterFactor) == 1)
            {
                for (int i = 0; i < _Indicators.Count; i++)
                {
                    _Indicators[i].RequiredRating += _IncreaseAfterFull;
                }
            }
        }

        private double _GetMaxScore(int p, float beat)
        {
            var players = CBase.Game.GetPlayers();
            double points = 0;
            CSongLine[] lines = _Song.Notes.GetVoice(players[p].VoiceNr).Lines;
            for (int l = 0; l < lines.Count(); l++)
            {
                if (lines[l].Points > 0)
                {
                    if (_Indicators[p].FirstLine == -1)
                    {
                        _Indicators[p].FirstLine = l;
                    }
                    for (int n = 0; n < lines[l].Notes.Count(); n++)
                    {
                        if (lines[l].Notes[n].EndBeat <= beat)
                        {
                            points += (9000) * (double)lines[l].Notes[n].Points / _Song.Notes.GetVoice(players[p].VoiceNr).Points;
                        }
                        else
                        {
                            return points;
                        }
                    }
                }
            }
            return points;
        }

    }
}
