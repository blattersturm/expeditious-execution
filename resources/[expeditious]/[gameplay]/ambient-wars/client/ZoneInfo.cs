namespace AmbientWarClient
{
    internal class ZoneInfo
    {
        public float X1 { get; set; }
        public float X2 { get; set; }
        public float Y1 { get; set; }
        public float Y2 { get; set; }
        public string Owner { get; set; }

        public ZoneInfo(float x1, float y1, float x2, float y2, string owner)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            Owner = owner;
        }
    }
}