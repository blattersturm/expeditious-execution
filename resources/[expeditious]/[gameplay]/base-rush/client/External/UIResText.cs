using System;
using System.Drawing;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core.UI;
using Font = CitizenFX.Core.UI.Font;

namespace NativeUI
{
    /// <summary>
    /// A Text object in the 1080 pixels height base system.
    /// </summary>
    public class UIResText : Text
    {
        public UIResText(string caption, PointF position, float scale) : base(caption, position, scale)
        {
            TextAlignment = ScreenAlignment.Left;
        }

        public UIResText(string caption, PointF position, float scale, Color color)
            : base(caption, position, scale, color)
        {
            TextAlignment = ScreenAlignment.Left;
        }

        public UIResText(string caption, PointF position, float scale, Color color, Font font, ScreenAlignment justify)
            : base(caption, position, scale, color, font, CitizenFX.Core.UI.Alignment.Left)
        {
            TextAlignment = justify;
        }


        public ScreenAlignment TextAlignment { get; set; }
        public bool DropShadow { get; set; } = false;
        public new bool Outline { get; set; } = false;

        /// <summary>
        /// Push a long string into the stack.
        /// </summary>
        /// <param name="str"></param>
        public static void AddLongString(string str)
        {
            var utf8ByteCount = System.Text.Encoding.UTF8.GetByteCount(str);

            if (utf8ByteCount == str.Length)
            {
                AddLongStringForAscii(str);
            }
            else
            {
                AddLongStringForUtf8(str);
            }
        }

        private static void AddLongStringForAscii(string input)
        {
            const int maxByteLengthPerString = 99;

            for (int i = 0; i < input.Length; i += maxByteLengthPerString)
            {
                string substr = (input.Substring(i, Math.Min(maxByteLengthPerString, input.Length - i)));
                AddTextComponentString(substr);
            }
        }

        private static void AddLongStringForUtf8(string input)
        {
            const int maxByteLengthPerString = 99;

            if (maxByteLengthPerString < 0)
            {
                throw new ArgumentOutOfRangeException("maxLengthPerString");
            }
            if (string.IsNullOrEmpty(input) || maxByteLengthPerString == 0)
            {
                return;
            }

            var enc = System.Text.Encoding.UTF8;

            var utf8ByteCount = enc.GetByteCount(input);
            if (utf8ByteCount < maxByteLengthPerString)
            {
                AddTextComponentString(input);
                return;
            }

            var startIndex = 0;

            for (int i = 0; i < input.Length; i++)
            {
                var length = i - startIndex;
                if (enc.GetByteCount(input.Substring(startIndex, length)) > maxByteLengthPerString)
                {
                    string substr = (input.Substring(startIndex, length - 1));
                    AddTextComponentString(substr);

                    i -= 1;
                    startIndex = (startIndex + length - 1);
                }
            }
            AddTextComponentString(input.Substring(startIndex, input.Length - startIndex));
        }

        public static float MeasureStringWidth(string str, Font font, float scale)
        {
            int screenw = Screen.Resolution.Width;
            int screenh = Screen.Resolution.Height;

            const float height = 1080f;
            float ratio = (float)screenw / screenh;
            float width = height * ratio;
            return MeasureStringWidthNoConvert(str, font, scale) * width;
        }

        public static float MeasureStringWidthNoConvert(string str, Font font, float scale)
        {
            BeginTextCommandWidth("STRING");
            AddLongString(str);
            return EndTextCommandGetWidth(true) * scale;
        }

        public SizeF WordWrap { get; set; }

        public override void Draw(SizeF offset)
        {
            int screenw = 0;
            int screenh = 0;
            GetActiveScreenResolution(ref screenw, ref screenh);
            const float height = 1080f;
            float ratio = (float)screenw / screenh;
            var width = height * ratio;

            float x = (Position.X) / width;
            float y = (Position.Y) / height;

            SetTextFont((int) Font);
            SetTextScale(1f, Scale);
            SetTextColour(Color.R, Color.G, Color.B, Color.A);
            if (DropShadow)
                SetTextDropShadow();
            if (Outline)
                SetTextOutline();
            switch (TextAlignment)
            {
                case ScreenAlignment.Centered:
                    SetTextCentre(true);
                    break;
                case ScreenAlignment.Right:
                    SetTextRightJustify(true);
                    SetTextWrap(0, x);
                    break;
            }

            if (WordWrap.Width != 0)
            {
                float xsize = (Position.X + WordWrap.Width) / width;
                SetTextWrap(x, xsize);
            }

            SetTextEntry("jamyfafi");
            AddLongString(Caption);

            DrawText(x, y);
        }

        //public static void Draw(string caption, int xPos, int yPos, Font font, float scale, UnknownColors color, Alignment alignment, bool dropShadow, bool outline, int wordWrap)
        //{
        //    int screenw = Screen.Resolution.Width;
        //    int screenh = Screen.Resolution.Height;
        //    const float height = 1080f;
        //    float ratio = (float)screenw / screenh;
        //    var width = height * ratio;

        //    float x = (xPos) / width;
        //    float y = (yPos) / height;

        //    Function.Call(Hash.SET_TEXT_FONT, (int)font);
        //    Function.Call(Hash.SET_TEXT_SCALE, 1.0f, scale);
        //    Function.Call(Hash.SET_TEXT_COLOUR, color.R, color.G, color.B, color.A);
        //    if (dropShadow)
        //        Function.Call(Hash.SET_TEXT_DROP_SHADOW);
        //    if (outline)
        //        Function.Call(Hash.SET_TEXT_OUTLINE);
        //    switch (alignment)
        //    {
        //        case Alignment.Centered:
        //            Function.Call(Hash.SET_TEXT_CENTRE, true);
        //            break;
        //        case Alignment.Right:
        //            Function.Call(Hash.SET_TEXT_RIGHT_JUSTIFY, true);
        //            Function.Call(Hash.SET_TEXT_WRAP, 0, x);
        //            break;
        //    }

        //    if (wordWrap != 0)
        //    {
        //        float xsize = (xPos + wordWrap) / width;
        //        Function.Call(Hash.SET_TEXT_WRAP, x, xsize);
        //    }

        //    Function.Call(Hash._SET_TEXT_ENTRY, "jamyfafi");
        //    AddLongString(caption);

        //    Function.Call(Hash._DRAW_TEXT, x, y);
        //}

        public enum ScreenAlignment
        {
            Left,
            Centered,
            Right,
        }
    }
}