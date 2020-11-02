using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace JoshGameLibrary20.services
{
    class DeviceScreen
    {
        private const string TAG = JoshGameLibrary20.TAG;

        /**
         * screenshot policies determine when or how to refresh a new screenshot
         * POLICY_DEFAULT: refresh policy default is manual mode
         * POLICY_STRICT: refresh every time
         * POLICY_MANUAL: refresh by caller itself
         */
        public const int POLICY_STRICT = 1;
        public const int POLICY_MANUAL = 2;
        public const int POLICY_DEFAULT = POLICY_STRICT;

        private readonly GameDevice mDevice;
        private readonly GameDevice.Logger Log; //the naming is just for easy use

        private int mScreenWidth;
        private int mScreenHeight;
        private int mScreenXOffset = 0;
        private int mScreenYOffset = 0;
        private int mCurrentGameOrientation;
        private int[] mAmbiguousRange = { 0x05, 0x05, 0x05 };
        private const int mMaxColorFinding = 10;
        private int mScreenshotPolicy = POLICY_DEFAULT;
        private int mScreenshotSlotCount;
        private int mScreenshotCurrentSlot;
        private bool mChatty = false;

        public DeviceScreen(GameDevice device)
        {
            if (device == null)
                throw new Exception("Initial DeviceScreen with null device");
            else
                mDevice = device;

            int[] resolution = device.GetScreenDimension();
            if (resolution == null || resolution.Length != 2)
            {
                //throw new IllegalArgumentException("Device report illegal resolution length");
                mScreenWidth = 1080;
                mScreenHeight = 2340;
            }
            else
            {
                mScreenWidth = resolution[0];
                mScreenHeight = resolution[1];
            }

            int orientation = device.GetScreenMainOrientation();
            if (orientation < 0)
                throw new FormatException("Device report illegal default screen orientation");
            else
                mCurrentGameOrientation = orientation;

            Log = mDevice.GetLogger();

            mScreenshotSlotCount = mDevice.GetScreenshotSlotCount();
            mScreenshotCurrentSlot = 0;
        }

        //
        // Screen setting override
        //
        public void SetScreenDimension(int w, int h)
        {
            mScreenHeight = h;
            mScreenWidth = w;
        }

        public void SetScreenDimension(int[] dims)
        {
            if (dims.Length != 2)
                throw new FormatException("dimension should have index of exact 2.");
            SetScreenDimension(dims[0], dims[1]);
        }

        public void SetGameOrientation(int o)
        {
            mCurrentGameOrientation = o;
        }

        public void SetScreenOffset(int xOffset, int yOffset)
        {
            mScreenXOffset = xOffset;
            mScreenYOffset = yOffset;
        }

        public void SetAmbiguousRange(int[] range)
        {
            mAmbiguousRange = range;
        }

        public void SetChatty(bool chatty)
        {
            mChatty = chatty;
        }

        public void SetScreenshotPolicy(int policy)
        {
            if (policy < 0 || policy > POLICY_MANUAL)
                mScreenshotPolicy = POLICY_DEFAULT;
            else
                mScreenshotPolicy = policy;
        }

        //
        // Main functions
        //

        // calculate the offset of dump file for
        // retrieving color of the specific point
        private int CalculateOffset(ScreenCoord coord)
        {
            int offset = 0;
            const int bpp = 4;

            if (mChatty)
            {
                if (coord.orientation == ScreenPoint.SO_Landscape)
                    Log.D(TAG, "Mapping (" + coord.x + ", " + coord.y +
                            ") to (" + (coord.x + mScreenYOffset) + ", " + (coord.y + mScreenXOffset) + ")");
                else
                    Log.D(TAG, "Mapping (" + coord.x + ", " + coord.y +
                            ") to (" + (coord.x + mScreenXOffset) + ", " + (coord.y + mScreenYOffset) + ")");
            }

            //if Android version is 7.0 or higher, the dump orientation will obey the device status
            if (mCurrentGameOrientation == ScreenPoint.SO_Portrait)
            {
                if (coord.orientation == ScreenPoint.SO_Portrait)
                {
                    offset = (mScreenWidth * (coord.y + mScreenYOffset) + (coord.x + mScreenXOffset)) * bpp;
                }
                else if (coord.orientation == ScreenPoint.SO_Landscape)
                {
                    offset = (mScreenWidth * (coord.x + mScreenYOffset) + (mScreenWidth - (coord.y + mScreenXOffset))) * bpp;
                }
            }
            else
            {
                if (coord.orientation == ScreenPoint.SO_Portrait)
                {
                    offset = (mScreenHeight * (mScreenWidth - (coord.x + mScreenXOffset)) + (coord.y + mScreenYOffset)) * bpp;
                }
                else if (coord.orientation == ScreenPoint.SO_Landscape)
                {
                    offset = (mScreenHeight * (coord.y + mScreenXOffset) + (coord.x + mScreenYOffset)) * bpp;
                }
            }

            return offset;
        }

        private bool ColorWithinRange(byte a, byte b, int range)
        {
            Byte byteA = a;
            Byte byteB = b;
            int src = byteA & 0xFF;
            int dst = byteB & 0xFF;
            int upperBound = src + range;
            int lowerBound = src - range;

            if (upperBound > 0xFF)
                upperBound = 0xFF;

            if (lowerBound < 0)
                lowerBound = 0;

            if (mChatty)
                Log.D(TAG, "compare range " + upperBound + " > " + lowerBound + " with " + dst);

            return (dst <= upperBound) && (dst >= lowerBound);
        }

        /**
         * return the slot count of screenshot slots
         * @return The slot count of screenshot slots
         */
        public int GetScreenshotSlotCount()
        {
            return mScreenshotSlotCount;
        }

        /**
         * request a refresh of screenshot in current slot
         * @return 0 upon success
         * @throws InterruptedException if screenshot cannot be done
         */
        public int RequestRefresh()
        {
            int ret;
            int index = mScreenshotCurrentSlot;

            mDevice.ScreenshotClose(index);

            ret = mDevice.ScreenDump(index, false);
            if (ret< 0) {
                if (ret == GameDevice.SCREENSHOT_IN_USE)
                    ret = mDevice.ScreenDump(index, true);
                else if (ret == GameDevice.SCREENSHOT_DUMP_FAIL)
                    throw new Exception("screenshot dump failed");

                if (ret == GameDevice.SCREENSHOT_CLOSE_FAIL)
                    throw new Exception("screenshot close failed");
            }

            return ret;
        }

        /**
         * compare two colors if they are the same
         * @param src First color
         * @param dest Second color for compare with
         * @return True if they have almost same color, i.e., the color
         *         differences are within than the ambiguous range.
         */
        public bool ColorCompare(ScreenColor src, ScreenColor dest)
        {
            bool result = ColorWithinRange(src.r, dest.r, mAmbiguousRange[0]) &&
                    ColorWithinRange(src.b, dest.b, mAmbiguousRange[1]) &&
                    ColorWithinRange(src.g, dest.g, mAmbiguousRange[2]);

            if (mChatty)
            {
                Log.D(TAG, "Source (" + src.r + ", " + src.g + ", " + src.b + "), " +
                        " Compare to (" + dest.r + ", " + dest.g + ", " + dest.b + ") ");
            }

            return result;
        }

        /**
         * change the current slot for further screenshot
         * @param index The target index of the slot
         * @param closeOld True if need to close previous screenshot; False if you like to preserve
         *                 previous screenshot for future need.
         * @return 0 if success
         */
        public int SetActiveSlot(int index, bool closeOld)
        {
            int ret = 0;

            if (index < 0 || index >= mScreenshotSlotCount)
                throw new IndexOutOfRangeException("index " + index + " is not legal");

            Log.D(TAG, "Change screenshot slot from " + mScreenshotCurrentSlot + " to " + index);
            if (mScreenshotCurrentSlot != index && closeOld)
            {
                // close opened screenshot slot, but even if release failed, we don't need to handle it
                if (mDevice.ScreenshotState(mScreenshotCurrentSlot) == GameDevice.SCREENSHOT_OPENED)
                {
                    ret = mDevice.ScreenshotClose(mScreenshotCurrentSlot);
                    if (ret != GameDevice.SCREENSHOT_NO_ERROR)
                    {
                        Log.W(TAG, "close screenshot failed: " + ret);
                    }
                    ret = mDevice.ScreenshotRelease(mScreenshotCurrentSlot);
                    if (ret != GameDevice.SCREENSHOT_NO_ERROR)
                    {
                        Log.W(TAG, "release screenshot failed " + ret);
                    }
                }
            }

            mScreenshotCurrentSlot = index;
            return ret;
        }

        /**
         * get current slot index
         * @return The current slot index
         */
        public int GetCurrentSlot()
        {
            return mScreenshotCurrentSlot;
        }

        /**
         * Get the color on screenshot at index
         * If refresh is needed and the slot is in use, force it.
         * [Synchronized method]
         * @param index The index of the screenshot slot
         * @param src The source coordinate location
         * @param refresh True if we need to take new screenshot before fetching color
         * @return The {@link ScreenColor} of the src coordination
         * @throws InterruptedException When interrupted or error happened
         * @throws ScreenshotErrorException When screenshot error happened
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public ScreenColor GetColorOnScreen(int index, ScreenCoord src, bool refresh)
        {
            FileStream dumpFile;
            int offset, ret = 0;
            byte[] colorInfo = new byte[4];

            offset = CalculateOffset(src);
            ScreenColor dest = new ScreenColor();

            try
            {
                if (refresh)
                    ret = RequestRefresh();

                dumpFile = mDevice.ScreenshotOpen(index);
                if (dumpFile == null)
                {
                    throw new Exception();
                }
                dumpFile.Seek(offset, SeekOrigin.Begin);
                dumpFile.Read(colorInfo, 0, 4);
                dest.r = colorInfo[0];
                dest.g = colorInfo[1];
                dest.b = colorInfo[2];
                dest.t = colorInfo[3];
            }
            catch (IOException e)
            {
                Log.E(TAG, "File operation failed: " + e.ToString());
                throw new JoshGameLibrary20.ScreenshotErrorException("screenshot error", ret);
            }
            catch (ThreadInterruptedException e)
            {
                Log.E(TAG, "File operation aborted by interrupt: " + e.ToString());
                throw e;
            }
            
            return dest;
        }

        /**
         * Get the color on a screenshot at current index
         * @param src The source coordinate location
         * @param refresh True if we need to take new screenshot before fetching color
         * @return The {@link ScreenColor} of the src coordination
         * @throws InterruptedException When interrupted or error happened
         * @throws ScreenshotErrorException When screenshot error happened
         */
        public ScreenColor GetColorOnScreen(ScreenCoord src, bool refresh)
        {
            return GetColorOnScreen(mScreenshotCurrentSlot, src, refresh);
        }

        /**
         * get multiple colors on screen
         * @param index The slot index
         * @param coords The coordination of colors
         * @param refresh True if request a new screenshot first
         * @return The array list of screen colors
         * @throws InterruptedException When interrupted or error happened
         * @throws ScreenshotErrorException When screenshot error happened
         */
        public ArrayList GetMultiColorOnScreen(int index, ArrayList coords, bool refresh)
        {
            ArrayList colors = new ArrayList();

            if (index< 0 || index >= mScreenshotSlotCount)
                throw new IndexOutOfRangeException("index " + index + " is not legal.");

            if (coords == null || coords.Count == 0)
                throw new FormatException("coords is null or size is zero");

            if(refresh)
                RequestRefresh();

            foreach(ScreenCoord coord in coords) {
                colors.Add(GetColorOnScreen(coord, false));
            }

            return colors;
        }

        /**
         * get multiple colors on screen on current slot
         * @param coords The coordination of colors
         * @param refresh True if request a new screenshot first
         * @return The array list of screen colors
         * @throws InterruptedException When interrupted or error happened
         * @throws ScreenshotErrorException When screenshot error happened
         */
        public ArrayList GetMultiColorOnScreen(ArrayList coords, bool refresh)
        {
            return GetMultiColorOnScreen(mScreenshotCurrentSlot, coords, refresh);
        }

        /**
         * Check if color at the point.coord is equal to point.color
         * @param point The point includes the coord to fetch color and determine if it's same as in color
         * @return True if color is the same or False if the colors are different
         * @throws InterruptedException When interrupted happened usually the signal from script
         * @throws ScreenshotErrorException When screenshot error happened
         */
        public bool ColorIs(ScreenPoint point)
        {
            bool refreshNeeded = false;
            if (point == null)
                throw new NullReferenceException("point is null");

            if (mScreenshotPolicy == POLICY_STRICT)
                refreshNeeded = true;

            ScreenColor currentColor = GetColorOnScreen(point.coord, refreshNeeded);
            return ColorCompare(currentColor, point.color);
        }

        /**
         * Check if colors in the points array are the same as in the screen
         * @param points The point includes the coord to fetch color and determine if it's same as in color
         * @return True if colors are the same or False if at least one of the colors are different
         * @throws InterruptedException When interrupted happened usually the signal from script
         * @throws ScreenshotErrorException When screenshot error happened
         */
        public bool ColorsAre(ArrayList points) {
            if (points == null)
                throw new NullReferenceException("point array is null");

            // refresh first, we do not like to refresh every point we'd like to check
            if (mScreenshotPolicy == POLICY_STRICT)
                RequestRefresh();

            foreach(ScreenPoint point in points) {
                ScreenColor currentColor = GetColorOnScreen(point.coord, false);
                if (!ColorCompare(currentColor, point.color))
                    return false;
            }

            return true;
        }

        /**
         * Check if all colors in the array are all in the specific region rect
         * Note that the colors in the array in unordered
         * @param rectLeftTop The LT of rect
         * @param rectRightBottom The RB of rect
         * @param colors The set of {@link ScreenColor} in match
         * @return True if all colors are in the rect. False if at least one color is not in the rect.
         * @throws InterruptedException When interrupted happened usually the signal from script
         * @throws ScreenshotErrorException When screenshot error happened
         */
        public bool ColorsAreInRect(ScreenCoord rectLeftTop, ScreenCoord rectRightBottom, ArrayList colors)
        {
            ArrayList coordList = new ArrayList();
            ArrayList colorsReturned;
            ArrayList checkList = new ArrayList();
            int colorCount, orientation;
            int x_start, x_end, y_start, y_end;
            
            // sanity check
            if (colors == null || rectLeftTop == null || rectRightBottom == null)
            {
                Log.W(TAG, "checkColorIsInRegion: colors cannot be null");
                throw new NullReferenceException("checkColorIsInRegion: colors cannot be null");
            }
            else
            {
                orientation = rectLeftTop.orientation;
                colorCount = colors.Count;
            }
            
            if (rectLeftTop.orientation != rectRightBottom.orientation)
            {
                Log.W(TAG, "checkColorIsInRegion: Src and Dest must in same orientation");
                throw new ArgumentException("checkColorIsInRegion: Src and Dest must in same orientation");
            }
            
            if (colorCount < 1 || colorCount > mMaxColorFinding)
            {
                Log.W(TAG, "checkColorIsInRegion: colors size should be bigger than 0 and smaller than " +
                        mMaxColorFinding);
                throw new ArgumentException("checkColorIsInRegion: colors size should be bigger than 0 and smaller than " +
                        mMaxColorFinding);
            }
            
            for (int i = 0; i < colorCount; i++)
                checkList.Add(false);
            
            if (rectLeftTop.x > rectRightBottom.x)
            {
                x_start = rectRightBottom.x;
                x_end = rectLeftTop.x;
            }
            else
            {
                x_start = rectLeftTop.x;
                x_end = rectRightBottom.x;
            }
            
            if (rectLeftTop.y > rectRightBottom.y)
            {
                y_start = rectRightBottom.y;
                y_end = rectLeftTop.y;
            }
            else
            {
                y_start = rectLeftTop.y;
                y_end = rectRightBottom.y;
            }
            
            for (int x = x_start; x <= x_end; x++)
            {
                for (int y = y_start; y <= y_end; y++)
                {
                    coordList.Add(new ScreenCoord(x, y, orientation));
                }
            }
            
            if (mChatty) Log.D(TAG, "FindColorInRange: now checking total " + coordList.Count + " points");
            
            colorsReturned = GetMultiColorOnScreen(coordList, true);
            foreach (ScreenColor color in colorsReturned)
            {
                for (int i = 0; i < colorCount; i++)
                {
                    ScreenColor sc = (ScreenColor)colors[i];
            
                    if ((bool)checkList[i])
                        continue;
            
                    if (ColorCompare(color, sc))
                    {
                        if (mChatty) Log.D(TAG, "FindColorInRange: Found color " + color.ToString());
                        checkList.Insert(i, true);
                    }
                }
            }
            
            foreach (bool b in checkList)
            {
                if (!b)
                    return false;
            }
            
            return true;
        }
    }
}
