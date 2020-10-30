using System;

namespace JoshGameLibrary20
{
	public class ScreenPoint
	{
        private const String TAG = "LibGame";
        public const int SO_Portrait = 0;
        public const int SO_Landscape = 1;
        public ScreenCoord coord;
        public ScreenColor color;

        public ScreenPoint(int r, int g, int b, int t, int x, int y, int orientation)
        {
            coord = new ScreenCoord(x, y, orientation);
            color = new ScreenColor((byte)(r & 0xFF), (byte)(g & 0xFF), (byte)(b & 0xFF), (byte)(t & 0xFF));
        }

        public ScreenPoint(ScreenCoord screenCoord, ScreenColor screenColor)
        {
            coord = screenCoord;
            color = screenColor;
        }

        /*
         * ScreenPoint can now work with PointSelectionActivity for easily adding point (added in 1.33)
         *
         * 1. Example 8-digits formatted string: 4-Aj5Gr0
         *    first byte: 4-Aj is for coordination (up to 2400x2400)
         *    second byte: 5Gr0 is for color (FF000000 ~ FFFFFFFF)
         *
         *    char to int table
         *    |-------------------------------------------------------------------|
         *    |Decimal   Value |  0 - 9  |  10 - 35 | 36 - 61 | 62 | 63 | 64 | 65 |
         *    |--------------------------------------------------------------------
         *    |Character Value |  0 - 9  |  A  - Z  | a  - z  | +  |  - |  * |  / |
         *    |-------------------------------------------------------------------|
         *
         * 2. Example XML formatted string: 236,236,235,0xff,832,74,Landscape
         *    There is no space in string and separated by 6 commas
         *    There are 7 data, first 6 are numbers, last one is string
         *    First 4 data: 236,236,235,0xff are color
         *    Last 3 data: 832,74,Landscape are coordination
         */
        public ScreenPoint(String formattedString)
        {
            int[] parsedArray = new int[8];
            int unit = 66;

            if (formattedString == null)
            {
                coord = null;
                color = null;
            }
            else if (formattedString.Length == 8)
            { //8-digits format
                for (int i = 0; i < formattedString.Length; i++)
                {
                    char targetChar = formattedString[i];
                    int parsedInt = ParseFormattedChar(targetChar);
                    parsedArray[i] = parsedInt;
                }

                int coordX = parsedArray[0] * unit + parsedArray[1];
                int coordY = parsedArray[2] * unit + parsedArray[3];
                coord = new ScreenCoord(coordX, coordY, SO_Portrait); //in this case, we only use portrait orientation

                int rawColor = parsedArray[4] * unit * unit * unit + parsedArray[5] * unit * unit + parsedArray[6] * unit + parsedArray[7];
                int colorR = (rawColor >> 16) & 0xff;
                int colorG = (rawColor >> 8) & 0xff;
                int colorB = rawColor & 0xff;
                color = new ScreenColor(colorR, colorG, colorB, 0xFF); //we force transparent value to 0xff
            }
            else
            { //XML format
                String[] data = formattedString.Split(',');
                if (data.Length == 7)
                {
                    int r, g, b, t, x, y, o;
                    String orientation = data[6];
                    try
                    {
                        r = Int32.Parse(data[0]);
                        g = Int32.Parse(data[1]);
                        b = Int32.Parse(data[2]);
                        t = Int32.Parse(data[3]);
                        x = Int32.Parse(data[4]);
                        y = Int32.Parse(data[5]);

                        if (orientation == "Portrait" || orientation == "P" || orientation == "p" || orientation == "0")
                        {
                            o = ScreenPoint.SO_Portrait;
                        }
                        else if (orientation == "Landscape" || orientation == "L" || orientation == "l" || orientation == "1")
                        {
                            o = ScreenPoint.SO_Landscape;
                        }
                        else
                        {
                            throw new FormatException("Orientation " + orientation + " not legal");
                        }

                        coord = new ScreenCoord(x, y, o);
                        color = new ScreenColor((byte)(r & 0xFF), (byte)(g & 0xFF), (byte)(b & 0xFF), (byte)(t & 0xFF));
                    }
                    catch (FormatException)
                    {
                        coord = null;
                        color = null;
                    }
                }
                else
                {
                    coord = null;
                    color = null;
                }
            }
        }

        public ScreenPoint()
        {
            coord = new ScreenCoord(0, 0, 0);
            color = new ScreenColor((byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00);
        }

        public override String ToString()
        {
            return "ScreenPoint (" + coord.x + "," + coord.y + ") color "
                    + "0x" + color.r.ToString("x") + ", 0x" + color.g.ToString("x") 
                    + ", 0x" + color.b.ToString("x") + ", 0x" + color.t.ToString("x");
        }

        public int GetColor()
        {
            return ((color.t & 0xff) << 24 | (color.r & 0xff) << 16 | (color.g & 0xff) << 8 | (color.b & 0xff));
        }

        public String GetFormattedString()
        {
            int unit = 66;

            char coordX1 = GenFormattedChar((int)(coord.x / unit));
            char coordX2 = GenFormattedChar((int)(coord.x % unit));
            char coordY1 = GenFormattedChar((int)(coord.y / unit));
            char coordY2 = GenFormattedChar((int)(coord.y % unit));

            int rawColor = (color.r & 0xff) << 16 | (color.g & 0xff) << 8 | (color.b & 0xff);
            char color1 = GenFormattedChar((int)(rawColor % unit));
            rawColor = rawColor / unit;
            char color2 = GenFormattedChar((int)(rawColor % unit));
            rawColor = rawColor / unit;
            char color3 = GenFormattedChar((int)(rawColor % unit));
            char color4 = GenFormattedChar((int)(rawColor / unit));

            String coordString = "" + coordX1 + coordX2 + coordY1 + coordY2;
            String colorString = "" + color4 + color3 + color2 + color1;

            return coordString + colorString;
        }

        private char GenFormattedChar(int value)
        {
            int charBaseN = 48; //this is ASCII for number 0
            int charBaseU = 65; //this is ASCII for letter A
            int charBaseL = 97; //this is ASCII for letter B

            if (value >= 0 && value <= 9)
            {
                return (char)(charBaseN + value);
            }
            else if (value >= 10 && value <= 35)
            {
                return (char)(charBaseU + value - 10);
            }
            else if (value >= 36 && value <= 61)
            {
                return (char)(charBaseL + value - 36);
            }
            else if (value >= 62 && value <= 65)
            {
                switch (value)
                {
                    case 62:
                        return '+';
                    case 63:
                        return '-';
                    case 64:
                        return '*';
                    case 65:
                        return '/';
                }
            }

            return ' ';
        }

        private int ParseFormattedChar(char value)
        {
            int charBaseN = 48; //this is ASCII for number 0
            int charBaseU = 65; //this is ASCII for letter A
            int charBaseL = 97; //this is ASCII for letter B

            int castValue = (int)value;
            if (castValue >= charBaseN && castValue < charBaseN + 10)
            { //target is a number
                return castValue - charBaseN;
            }
            else if (castValue >= charBaseU && castValue < charBaseU + 26)
            { //target is a upper case alphabet
                return (castValue - charBaseU) + 10;
            }
            else if (castValue >= charBaseL && castValue < charBaseL + 26)
            { //target is a lower case alphabet
                return (castValue - charBaseL) + 36;
            }

            switch (value)
            {
                case '+':
                    return 62;
                case '-':
                    return 63;
                case '*':
                    return 64;
                case '/':
                    return 65;
                default:
                    return -1;
            }
        }
    }
}

