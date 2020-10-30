using System;

namespace JoshGameLibrary20
{
	public class ScreenColor
	{
        public byte b;  /* blue */
        public byte g;  /* green */
        public byte r;  /* red */
        public byte t;  /* transparent */

        public override String ToString()
        {
            return "0x" + r.ToString("x") + ", 0x" + g.ToString("x") + ", 0x" + b.ToString("x") + ", 0x" + t.ToString("x");
        }

        public ScreenColor(byte rr, byte gg, byte bb, byte tt)
        {
            b = bb;
            g = gg;
            r = rr;
            t = tt;
        }

        public ScreenColor(int rr, int gg, int bb, int tt)
        {
            b = (byte)bb;
            g = (byte)gg;
            r = (byte)rr;
            t = (byte)tt;
        }

        public ScreenColor(String formattedString)
        {
            String[] data = formattedString.Split(',');
            if (data.Length == 4)
            {
                try
                {
                    r = (byte)(Int32.Parse(data[0]) & 0xFF);
                    g = (byte)(Int32.Parse(data[1]) & 0xFF);
                    b = (byte)(Int32.Parse(data[2]) & 0xFF);
                    t = (byte)(Int32.Parse(data[3]) & 0xFF);
                }
                catch (FormatException)
                {
                    b = 0;
                    g = 0;
                    r = 0;
                    t = 0;
                }
            }
            else
            {
                b = 0;
                g = 0;
                r = 0;
                t = 0;
            }
        }

        public ScreenColor()
        {
            b = 0;
            g = 0;
            r = 0;
            t = 0;
        }

        public ScreenPoint ToScreenPoint()
        {
            return new ScreenPoint(r, g, b, t, 0, 0, ScreenPoint.SO_Landscape);
        }
    }
}
