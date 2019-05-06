using System.Drawing;

namespace AmbientWarClient
{
    internal class GangInfo
    {
        public string Identifier { get; set; }
        public Color Color { get; set; }

        public GangInfo(string identifier, Color color)
        {
            Identifier = identifier;
            Color = color;
        }
    }
}