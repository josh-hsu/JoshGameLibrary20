using System;

namespace JoshGameLibrary20
{
	public class ScreenCoord
	{
        public int x;
        public int y;
        public int orientation;

        public ScreenCoord(int xx, int yy, int oo)
        {
            x = xx;
            y = yy;
            orientation = oo;
        }

        public ScreenCoord(String formattedString)
        {
            String[] data = formattedString.Split(',');
            if (data.Length == 3)
            {
                String o = data[2];
                try
                {
                    x = Int32.Parse(data[0]);
                    y = Int32.Parse(data[1]);

                    if (o == "Portrait" || o == "P" || o ==  "p" || o == "0")
                    {
                        orientation = ScreenPoint.SO_Portrait;
                    }
                    else if (o == "Landscape" || o == "L" || o == "l" || o == "1")
                    {
                        orientation = ScreenPoint.SO_Landscape;
                    }
                    else
                    {
                        throw new FormatException("Orientation " + orientation + " not legal");
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("ScreenCoord parse error");
                    x = 0;
                    y = 0;
                    orientation = 0;
                }
            }
            else
            {
                x = 0;
                y = 0;
                orientation = 0;
            }
        }

        public ScreenCoord()
        {
            x = 0;
            y = 0;
            orientation = 0;
        }

        public override String ToString()
        {
            return "(" + x + ", " + y + ")";
        }

        public static ScreenCoord GetTwoPointCenter(ScreenCoord src, ScreenCoord dest)
        {

            if (src.orientation != dest.orientation)
            {
                return null;
            }

            return new ScreenCoord((src.x + dest.x) / 2, (src.y + dest.y) / 2, src.orientation);
        }

        public ScreenPoint ToScreenPoint()
        {
            return new ScreenPoint(0, 0, 0, 0, x, y, orientation);
        }
    }
}
