using System.Collections.Generic;
using Font = CitizenFX.Core.UI.Font;
using CitizenFX.Core.UI;
using System.Drawing;
using static CitizenFX.Core.Native.API;
using System;

namespace NativeUI
{
    internal static class UIMenu
    {
        /// <summary>
        /// Returns the 1080pixels-based screen resolution while mantaining current aspect ratio.
        /// </summary>
        /// <returns></returns>
        public static SizeF GetScreenResolutionMaintainRatio()
        {
            int screenw = 0;
            int screenh = 0;
            GetActiveScreenResolution(ref screenw, ref screenh);
            const float height = 1080f;
            float ratio = (float)screenw / screenh;
            var width = height * ratio;

            return new SizeF(width, height);
        }

        /// <summary>
        /// Returns the safezone bounds in pixel, relative to the 1080pixel based system.
        /// </summary>
        /// <returns></returns>
        public static PointF GetSafezoneBounds()
        {
            float t = GetSafeZoneSize(); // Safezone size.
            double g = Math.Round(Convert.ToDouble(t), 2);
            g = (g * 100) - 90;
            g = 10 - g;

            const float hmp = 5.4f;
            int screenw = 0;
            int screenh = 0;
            GetActiveScreenResolution(ref screenw, ref screenh);
            float ratio = (float)screenw / screenh;
            float wmp = ratio * hmp;


            return new PointF((int)Math.Round(g * wmp), (int)Math.Round(g * hmp));
        }
    }

    public abstract class TimerBarBase
    {
        public string Label { get; set; }

        public TimerBarBase(string label)
        {
            Label = label;
        }

        public virtual void Draw(int interval)
        {
            SizeF res = UIMenu.GetScreenResolutionMaintainRatio();
            PointF safe = UIMenu.GetSafezoneBounds();
            new UIResText(Label, new PointF((int)res.Width - safe.X - 180, (int)res.Height - safe.Y - (30 + (4 * interval))), 0.3f, Color.FromArgb(255, 255, 255, 255), Font.ChaletLondon, UIResText.ScreenAlignment.Right).Draw();

            CitizenFX.Core.Native.API.DrawSprite("timerbars", "all_black_bg", ((int)res.Width - safe.X - 298) / res.Width + (300f / res.Width / 2), (res.Height - safe.Y - (40 + (4 * interval))) / res.Height + (37 / res.Height / 2), 300f / res.Width, 37f / res.Height, 0f, 255, 255, 255, 180);

            // TODO: just move them instead
            HideHudComponentThisFrame(7);
            HideHudComponentThisFrame(9);
            HideHudComponentThisFrame(6);
        }
    }

    public class TextTimerBar : TimerBarBase
    {
        public string Text { get; set; }

        public TextTimerBar(string label, string text) : base(label)
        {
            Text = text;
        }

        public override void Draw(int interval)
        {
            SizeF res = UIMenu.GetScreenResolutionMaintainRatio();
            PointF safe = UIMenu.GetSafezoneBounds();

            base.Draw(interval);
            new UIResText(Text, new PointF((int)res.Width - safe.X - 10, (int)res.Height - safe.Y - (42 + (4 * interval))), 0.5f, Color.FromArgb(-1), Font.ChaletLondon, UIResText.ScreenAlignment.Right).Draw();
        }
    }

    public class BarTimerBar : TimerBarBase
    {
        /// <summary>
        /// Bar percentage. Goes from 0 to 1.
        /// </summary>
        public float Percentage { get; set; }

        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }

        public BarTimerBar(string label) : base(label)
        {
            BackgroundColor = Color.FromArgb(-7667712);
            ForegroundColor = Color.FromArgb(-65536);
        }

        public override void Draw(int interval)
        {
            SizeF res = UIMenu.GetScreenResolutionMaintainRatio();
            PointF safe = UIMenu.GetSafezoneBounds();

            base.Draw(interval);

            var start = new PointF((int)res.Width - safe.X - 160, (int)res.Height - safe.Y - (28 + (4 * interval)));

            new UIResRectangle(start, new SizeF(150, 15), BackgroundColor).Draw();
            new UIResRectangle(start, new SizeF((int)(150 * Percentage), 15), ForegroundColor).Draw();
        }
    }

    public class TimerBarPool
    {
        private static List<TimerBarBase> _bars = new List<TimerBarBase>();

        public TimerBarPool()
        {
            _bars = new List<TimerBarBase>();
        }

        public List<TimerBarBase> ToList()
        {
            return _bars;
        }

        public void Add(TimerBarBase timer)
        {
            _bars.Add(timer);
        }

        public void Remove(TimerBarBase timer)
        {
            _bars.Remove(timer);
        }

        public void Draw()
        {
            for (int i = 0; i < _bars.Count; i++)
            {
                _bars[i].Draw(i * 10);
            }
        }
    }
}