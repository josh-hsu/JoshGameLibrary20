using System;
using System.Threading;

namespace JoshGameLibrary20.services
{
    class DeviceInteract
    {
        private const String TAG = JoshGameLibrary20.TAG;

        private readonly GameDevice mDevice;
        private readonly GameDevice.Logger Log; //the naming is just for easy use

        private int mRandomMouseInputShift = 0;
        private int mScreenWidth = -1;
        private int mScreenHeight = -1;
        private int mScreenXOffset = 0;
        private int mScreenYOffset = 0;
        private int mCurrentGameOrientation;
        private bool mChatty = true;
        private bool mHardwareSimulated = false;

        public DeviceInteract(JoshGameLibrary20 gl, GameDevice device)
        {
            if (device == null)
                throw new Exception("Initial DeviceScreen with null device");
            else
                mDevice = device;

            Log = mDevice.GetLogger();

            int[] resolution = device.GetScreenDimension();
            if (resolution == null || resolution.Length != 2)
            {
                Log.W(TAG, "Auto detect for device resolution failed, use default 1080x2340. Override this.");
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
                throw new Exception("Device report illegal default screen orientation");
            else
                mCurrentGameOrientation = orientation;
        }

        public void SetChatty(bool chatty)
        {
            mChatty = chatty;
        }

        public void SetScreenDimension(int w, int h)
        {
            mScreenWidth = w;
            mScreenHeight = h;
        }

        public void SetScreenDimension(int[] dims)
        {
            if (dims.Length != 2)
                throw new Exception("dimension should have index of exact 2.");
            SetScreenDimension(dims[0], dims[1]);
        }

        public void SetScreenOffset(int xOffset, int yOffset)
        {
            mScreenXOffset = xOffset;
            mScreenYOffset = yOffset;
        }

        public void SetGameOrientation(int orientation)
        {
            mCurrentGameOrientation = orientation;
        }

        public void SetMouseInputShift(int ran)
        {
            mRandomMouseInputShift = ran;
        }

        private int MouseInteract(int x, int y, int tx, int ty, int type)
        {
            int ret = 0;
            Random random = new Random();
            int x_shift = (int)(random.NextDouble() * mRandomMouseInputShift) - mRandomMouseInputShift / 2;
            int y_shift = (int)(random.NextDouble() * mRandomMouseInputShift) - mRandomMouseInputShift / 2;

            x = x + x_shift;
            y = y + y_shift;

            if (mScreenHeight > 0 && y > (mCurrentGameOrientation == ScreenPoint.SO_Portrait ? mScreenHeight : mScreenWidth))
                y = (mCurrentGameOrientation == ScreenPoint.SO_Portrait ? mScreenHeight : mScreenWidth);
            else if (y < 0)
                y = 0;

            if (mScreenWidth > 0 && x > (mCurrentGameOrientation == ScreenPoint.SO_Landscape ? mScreenHeight : mScreenWidth))
                x = (mCurrentGameOrientation == ScreenPoint.SO_Portrait ? mScreenHeight : mScreenWidth);
            else if (x < 0)
                x = 0;

            switch (type)
            {
                case GameDevice.MOUSE_TAP:
                case GameDevice.MOUSE_DOUBLE_TAP:
                case GameDevice.MOUSE_TRIPLE_TAP:
                case GameDevice.MOUSE_PRESS:
                case GameDevice.MOUSE_MOVE_TO:
                case GameDevice.MOUSE_RELEASE:
                    ret = mDevice.MouseInteract(x, y, type);
                    break;
                case GameDevice.MOUSE_SWIPE:
                    ret = mDevice.MouseInteract(x, y, tx, ty, type);
                    break;
                default:
                    Log.W(TAG, "touchOnScreen: type " + type + "is invalid.");
                    break;
            }

            return ret;
        }

        private ScreenCoord GetCalculatedOffsetCoord(ScreenCoord coord1)
        {
            ScreenCoord coord;

            if (coord1.orientation == ScreenPoint.SO_Portrait)
            {
                coord = new ScreenCoord(coord1.x + mScreenXOffset, coord1.y + mScreenYOffset, coord1.orientation);
            }
            else
            {
                coord = new ScreenCoord(coord1.x + mScreenYOffset, coord1.y + mScreenXOffset, coord1.orientation);
            }

            return coord;
        }

        private int MouseInteractSingleCoord(ScreenCoord coord1, int type)
        {
            int ret;
            ScreenCoord coord = GetCalculatedOffsetCoord(coord1);

            if (mCurrentGameOrientation != coord.orientation)
                ret = MouseInteract(coord.y, mScreenWidth - coord.x, 0, 0, type);
            else
                ret = MouseInteract(coord.x, coord.y, 0, 0, type);

            return ret;
        }

        public int MouseSwipe(ScreenCoord start, ScreenCoord end)
        {
            int ret;
            ScreenCoord coordStart = GetCalculatedOffsetCoord(start);
            ScreenCoord coordEnd = GetCalculatedOffsetCoord(end);

            if (mCurrentGameOrientation != start.orientation)
                ret = MouseInteract(coordStart.y, mScreenWidth - coordStart.x, coordEnd.y, mScreenWidth - coordEnd.x, GameDevice.MOUSE_SWIPE);
            else
                ret = MouseInteract(coordStart.x, coordStart.y, coordEnd.x, coordEnd.y, GameDevice.MOUSE_SWIPE);

            return ret;
        }

        public int MouseClick(ScreenCoord coord)
        {
            return MouseInteractSingleCoord(coord, GameDevice.MOUSE_TAP);
        }

        public int MouseDoubleClick(ScreenCoord coord)
        {
            return MouseInteractSingleCoord(coord, GameDevice.MOUSE_DOUBLE_TAP);
        }

        public int MouseTripleClick(ScreenCoord coord)
        {
            return MouseInteractSingleCoord(coord, GameDevice.MOUSE_TRIPLE_TAP);
        }

        public int MouseDown(ScreenCoord coord)
        {
            return MouseInteractSingleCoord(coord, GameDevice.MOUSE_PRESS);
        }

        public int MouseMoveTo(ScreenCoord coord)
        {
            return MouseInteractSingleCoord(coord, GameDevice.MOUSE_MOVE_TO);
        }

        public int MouseUp(ScreenCoord coord)
        {
            return MouseInteractSingleCoord(coord, GameDevice.MOUSE_RELEASE);
        }

        class VibrationEventListener: GameDeviceHWEventListener
        {
            private bool hwEventOnChanged = false;

            public override void OnEvent(GameDevice device, int evt, object data)
            {
                if (evt == GameDevice.HW_EVENT_CB_ONCHANGE) {
                    int value = (Int32)data;
                    device.GetLogger().D(TAG, "On change callback event for vibrator: " + value);
                    if (value == 1)
                    {
                        hwEventOnChanged = true;
                    }
                } else {
                    device.GetLogger().W(TAG, "Unknown callback event for vibrator: " + evt);
                }
            }

            public bool GetVibrated()
            {
                return hwEventOnChanged;
            }
        }

        public void WaitUntilDeviceVibrate(int timeoutMs)
        {
            int threadSleepTimeMs = 100;
            int threadLoopCount = timeoutMs / threadSleepTimeMs + 1;
            VibrationEventListener eventListener = new VibrationEventListener();
           
            try
            {
                if (mDevice.RegisterHardwareEvent(GameDevice.HW_EVENT_VIBRATOR, eventListener) < 0)
                {
                    Log.E(TAG, "could not register vibrator event.");
                    return;
                }

                while (threadLoopCount-- > 0)
                {
                    if (eventListener.GetVibrated())
                    {
                        Log.D(TAG, "detect vibration, end event pulling looping");
                        return;
                    }
                    Thread.Sleep(threadSleepTimeMs);
                }
            }
            finally
            { // note that the exception will be rethrown
                mDevice.DeregisterHardwareEvent(GameDevice.HW_EVENT_VIBRATOR, eventListener);
            }
        }
    }
}
