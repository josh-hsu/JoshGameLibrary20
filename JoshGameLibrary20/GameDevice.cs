using System;
using System.IO;

namespace JoshGameLibrary20
{
    /// <summary>
    /// GameDevice is a generic device which supports basic device operations.
    /// </summary>
    public class GameDevice
    {
        private const String TAG = JoshGameLibrary20.TAG;

        public const int LOG_VERBOSE = 0;
        public const int LOG_DEBUG = 1;
        public const int LOG_WARNING = 2;
        public const int LOG_ERROR = 3;
        public const int LOG_FATAL = 4;

        public const int DEVICE_SYS_WINDOWS = 0;
        public const int DEVICE_SYS_LINUX = 1;
        public const int DEVICE_SYS_DARWIN = 2;

        /**
         * SCREENSHOT_EMPTY screenshot slot is fully released
         * SCREENSHOT_OPENED screenshot is opened and acquired by Application
         * SCREENSHOT_CLOSED screenshot is ready for use but not opened
         */
        public const int SCREENSHOT_EMPTY = 0;
        public const int SCREENSHOT_OPENED = 1;
        public const int SCREENSHOT_CLOSED = 2;

        public const int SCREENSHOT_IN_USE = -10;
        public const int SCREENSHOT_CLOSE_FAIL = -9;
        public const int SCREENSHOT_DUMP_FAIL = -8;
        public const int SCREENSHOT_INDEX_ERROR = -3;
        public const int SCREENSHOT_NO_ERROR = 0;

        public const int MOUSE_TAP = 0;
        public const int MOUSE_DOUBLE_TAP = 1;
        public const int MOUSE_TRIPLE_TAP = 2;
        public const int MOUSE_PRESS = 3;
        public const int MOUSE_RELEASE = 4;
        public const int MOUSE_MOVE_TO = 5;
        public const int MOUSE_SWIPE = 6;
        public const int MOUSE_EVENT_MAX = 7;

        public const int HW_EVENT_VIBRATOR = 0;
        public const int HW_EVENT_PROXIMITY = 1;
        public const int HW_EVENT_SOUND = 2;
        public const int HW_EVENT_CB_ONCHANGE = 0;
        public const int HW_EVENT_CB_NEW_VALUE = 1;

        protected bool mInitialized = false;
        private String mDeviceName;
        private IGameDeviceBasics mDeviceInterface;
        private Logger mLogger;

        private String[] mFilePaths;
        private int mFilePathCount;
        private FileStream[] mFileSlot;
        private int[] mFileState;

        /**
         * Initial function should be override
         *
         * @param objects The object array for specific device initialization
         * @return 0 upon success
         */
        public virtual int Init(Object[] objects)
        {
            /* Override needed */
            return 0;
        }

        /**
         * Initial of the GameDevice, this method must be called in extended device
         *
         * @param deviceInterface The IGameDevice implementation of specific device
         * @param deviceName The name of the device
         * @return 0 if success, -1 if illegal arguments
         */
        protected int Init(String deviceName, IGameDeviceBasics deviceInterface)
        {
            // verify the input parameters
            if (deviceName == null)
            {
                Log(LOG_ERROR, TAG, "Initial for NULL device name is not allowed");
                return -1;
            }
            mDeviceName = deviceName;

            if (deviceInterface == null)
            {
                Log(LOG_FATAL, TAG, "Initial for NULL device implement is not allowed");
                return -1;
            }
            mDeviceInterface = deviceInterface;

            mFilePaths = deviceInterface.QueryPreloadedPaths();
            mFilePathCount = deviceInterface.QueryPreloadedPathCount();
            mFileSlot = new FileStream[mFilePathCount];
            mFileState = new int[mFilePathCount];
            for (int i = 0; i < mFilePathCount; i++)
            {
                mFileState[i] = SCREENSHOT_EMPTY;
            }

            mLogger = new Logger(this);
            mInitialized = true;

            return 0;
        }

        /**
         * Get the name of the device
         *
         * @return String of the name of device
         */
        public String GetName()
        {
            return mDeviceName;
        }

        /**
         * Get if the device is full initialized
         * @return True if initial has been done
         */
        public bool GetInitialized()
        {
            return mInitialized;
        }

        /**
         * Get the system type of device
         * @return The system type or -1 upon failure
         */
        public int GetDeviceSystemType()
        {
            if (mDeviceInterface != null)
                return mDeviceInterface.GetSystemType();
            else
                return -1;
        }

        public virtual int[] GetScreenDimension()
        {
            /* Override needed */
            return new int[] { 0, 0 };
        }

        public virtual int GetScreenMainOrientation()
        {
            /* Override needed */
            return 0;
        }

        /**
         * Get the transaction waiting time of this device
         * @return The wait transaction time needed in milliseconds
         */
        public int GetWaitTransactionTimeMs()
        {
            if (mDeviceInterface != null)
                return mDeviceInterface.GetWaitTransactionTimeMs();
            else
                return -1;
        }

        /**
         * setDeviceEssentials
         * set special object that keep this device functional
         * this function might be useful for extended initialization
         *
         * @param object The object for extended initialization
         */
        public virtual void SetDeviceEssentials(Object obj)
        {
            /* Override needed */
        }

        /**
         * Override the device wait transaction time do it on your own risk
         * @param ms The wait transaction time in milliseconds
         */
        public void SetWaitTransactionTimeMs(int ms)
        {
            if (ms >= 0 && mDeviceInterface != null)
            {
                mDeviceInterface.SetWaitTransactionTimeMsOverride(ms);
            }
        }

        /**
         * use hardware simulated way to send input command
         * it is used to prevent our tool been detected by games or apps
         * @param enable True if we want to use hardware simulation otherwise False can be set
         * @return 0 if both supported and switched to selected mode, -9 if not supported, otherwise
         *         -1 will be returned.
         */
        public int SetHardwareSimulatedInput(bool enable)
        {
            if (mDeviceInterface != null)
                return mDeviceInterface.SetHWSimulatedInput(enable);
            else
                return -1;
        }

        /**
         * sendDeviceCommand
         * send out device command, normally this should be used in test not release version
         *
         * @param synced Determine if you want to wait for command to finish.
         * @param cmd The command string.
         * @return The index of command results you can further query later
         */
        public virtual int SendDeviceCommand(bool synced, String cmd)
        {
            return 0;
        }

        /**
         * getDeviceCommandResult
         * return the command result in the index of the command result slot
         *
         * @param index The index of the command result slot
         * @return The result of the index in the command result slot
         */
        public String GetDeviceCommandResult(int index)
        {
            return null;
        }

        /**
         * screenDump
         * make a screenshot at specific index of slot
         * If the previous screenshot is not closed yet, it will return an error. If forced
         * is not set. Note this will not open a file description for use, just doing dump
         * after a screen dump command is sent, it will sleep a period of time defined in function
         * getWaitTransactionTimeMs() to make screenshot ready to use.
         *
         * @param index The index of screenshot slot to save in
         * @param forced True if ignoring the screenshot is in use
         * @return 0 upon success
         */
        public int ScreenDump(int index, bool forced)
        {
            int ret;
            // checking if index legal
            if (index< 0 || index> mFilePathCount) {
                return SCREENSHOT_INDEX_ERROR;
            }

            if (mFileState[index] == SCREENSHOT_OPENED) {
                if (forced) {
                    Log(LOG_DEBUG, TAG, "screenshot is in use, force close it.");
                    ret = ScreenshotClose(index);
                    if (ret< 0) {
                        Log(LOG_ERROR, TAG, "screenshot in slot " + index + " is not able to close, error: " + ret);
                        return SCREENSHOT_CLOSE_FAIL;
                    }
                } else {
                    Log(LOG_WARNING, TAG, "screenshot in slot " + index + " is in use.");
                    return SCREENSHOT_IN_USE;
                }
            }

            ret = mDeviceInterface.DumpScreen(mFilePaths[index]);
            if (ret < 0)
            {
                Log(LOG_ERROR, TAG, "dumpscreen failed, ret = " + ret);
                mFileState[index] = SCREENSHOT_EMPTY;
                return SCREENSHOT_DUMP_FAIL;
            }
            mFileState[index] = SCREENSHOT_CLOSED;
            
            // sleep a waiting time for screenshot truly ready
            //Thread.sleep(getWaitTransactionTimeMs());
            
            return SCREENSHOT_NO_ERROR;
        }

        /**
         * query all screenshot slot state
         * @return All screenshot slot state
         */
        public int[] ScreenshotState()
        {
            return mFileState;
        }
        
        /**
         * query single screen shot state
         * @param index The index of the slot
         * @return The screenshot slot state of the index
         */
        public int ScreenshotState(int index)
        {
            return mFileState[index];
        }
        
        /**
         * screenshotOpen
         * get the file handle of specific screenshot at index of the slot
         * if the screenshot cannot be returned, an Exception will be thrown
         *
         * @param index The index of screenshot slot to open
         * @return 0 upon success
         */
        public FileStream ScreenshotOpen(int index)
        {
            FileStream dumpFile;
        
            // checking if index legal
            if (index < 0 || index > mFilePathCount)
            {
                Log(LOG_WARNING, TAG, "index " + index + " is not legal");
                return null;
            }
        
            if (mFileState[index] == SCREENSHOT_EMPTY)
            {
                Log(LOG_WARNING, TAG, "screenshot is empty at index " + index);
                return null;
            }
        
            if (mFileState[index] == SCREENSHOT_CLOSED)
            {
                try
                {
                    dumpFile = File.Open(mFilePaths[index], FileMode.Open, FileAccess.Write, FileShare.None);
                    mFileState[index] = SCREENSHOT_OPENED;
                    mFileSlot[index] = dumpFile;
                }
                catch (FileNotFoundException)
                {
                    Log(LOG_ERROR, TAG, "screenshot not found! file state might be wrong");
                    return null;
                }
            }
        
            return mFileSlot[index];
        }

        /**
         * screenshotClose
         * close the file handle of specific screenshot at index of the slot
         * if the screenshot cannot be closed, an Exception will be thrown
         * if the screenshot is already closed, return error code.
         *
         * @param index The index of screenshot slot to close
         * @return 0 upon success
         */
        public int ScreenshotClose(int index)
        {
            // checking if index legal
            if (index < 0 || index > mFilePathCount)
            {
                Log(LOG_WARNING, TAG, "index " + index + " is not legal");
                return SCREENSHOT_INDEX_ERROR;
            }
        
            if (mFileState[index] == SCREENSHOT_EMPTY ||
                    mFileState[index] == SCREENSHOT_CLOSED)
            {
                // already closed, do noting.
                return SCREENSHOT_NO_ERROR;
            }
        
            try
            {
                mFileSlot[index].Close();
                mFileState[index] = SCREENSHOT_CLOSED;
            }
            catch (IOException)
            {
                Log(LOG_ERROR, TAG, "close this file error, release it.");
                mFileSlot[index] = null;
                mFileState[index] = SCREENSHOT_EMPTY;
            }
        
            return SCREENSHOT_NO_ERROR;
        }
        
        /**
         * screenshotRelease
         * release and free the slot of screenshot
         * make the slot to SCREENSHOT_EMPTY state
         *
         * @param index The index of the slot you want to release
         * @return 0 upon success
         */
        public int ScreenshotRelease(int index)
        {
            // checking if index legal
            if (index < 0 || index > mFilePathCount)
            {
                Log(LOG_WARNING, TAG, "index " + index + " is not legal");
                return SCREENSHOT_INDEX_ERROR;
            }
        
            if (mFileState[index] == SCREENSHOT_EMPTY ||
                    mFileState[index] == SCREENSHOT_CLOSED)
            {
                mFileSlot[index] = null;                //nullify the file slot as free the space
                mFileState[index] = SCREENSHOT_EMPTY;   //mark the state as EMPTY
                return SCREENSHOT_NO_ERROR;
            }
        
            Log(LOG_WARNING, TAG, "screenshot at index " + index + " is in use. please close it first");
            return SCREENSHOT_IN_USE;
        }
        
        /**
         * dumpScreenManual
         * manual dump a screenshot at specific file path
         * @param path Path to save the screenshot
         */
        public void DumpScreenManual(String path)
        {
            if (mDeviceInterface == null)
            {
                throw new Exception("Fatal exception that device interface is null");
            }
        
            mDeviceInterface.DumpScreen(path);
            mDeviceInterface.DumpScreenPng(path + ".png");
        }
        
        /**
         * query preloaded screenshot path count for indexing
         * @return Total length of screenshot path count
         */
        public int GetScreenshotSlotCount()
        {
            if (mDeviceInterface == null)
            {
                throw new Exception("Fatal exception that device interface is null");
            }
        
            return mFilePaths.Length;
        }
        
        
        public int MouseInteract(int x, int y, int dx, int dy, int type)
        {
            if (mDeviceInterface == null)
            {
                throw new Exception("Fatal exception that device interface is null");
            }
        
            return mDeviceInterface.MouseEvent(x, y, dx, dy, type);
        }
        
        public int MouseInteract(int x, int y, int type)
        {
            if (mDeviceInterface == null)
            {
                throw new Exception("Fatal exception that device interface is null");
            }
        
            return mDeviceInterface.MouseEvent(x, y, 0, 0, type);
        }
        
        public String RunShellCommand(String cmd)
        {
            if (mDeviceInterface == null)
            {
                throw new Exception("Fatal exception that device interface is null");
            }
        
            return mDeviceInterface.RunShellCommand(cmd);
        }
        
        /**
         * log to device
         * @param level The level defined in {@link GameLibrary20}
         * @param tag The tag of this message
         * @param msg Message to be logged
         */
        public void Log(int level, String tag, String msg)
        {
            // write to console first
            Console.WriteLine(tag + ": " + msg);
            
            if (mDeviceInterface == null)
                throw new Exception("Fatal exception that device interface is null");
        
            mDeviceInterface.LogDevice(level, tag, msg);
            if (level == LOG_FATAL)
                throw new Exception("Fatal exception detected, throwing stack trace.");
        }
        
        /**
         * get the {@link Logger} wrapper for this device
         * @return The Logger wrapper
         */
        public Logger GetLogger()
        {
            return mLogger;
        }
        
        public int RegisterHardwareEvent(int hardwareType, IGameDeviceHWEventListener el)
        {
            if (mDeviceInterface == null)
                throw new Exception("Fatal exception that device interface is null");
        
            switch (hardwareType)
            {
                case HW_EVENT_VIBRATOR:
                case HW_EVENT_PROXIMITY:
                case HW_EVENT_SOUND:
                    return mDeviceInterface.RegisterEvent(hardwareType, el);
                default:
                    Log(LOG_WARNING, TAG, "Unsupported hw event type");
                    break;
            }
        
            return -1;
        }
        
        public int DeregisterHardwareEvent(int hardwareType, IGameDeviceHWEventListener el)
        {
            if (mDeviceInterface == null)
                throw new Exception("Fatal exception that device interface is null");
        
            return mDeviceInterface.DeregisterEvent(hardwareType, el);
        }
        
        public int StartDevice()
        {
            if (mDeviceInterface == null)
                throw new Exception("Fatal exception that device interface is null");
        
            return mDeviceInterface.OnStart();
        }
        
        public int DestroyDevice()
        {
            int ret = 0;
        
            if (mDeviceInterface != null)
                ret = mDeviceInterface.OnExit();
        
            return ret;
        }

        public class Logger
        {
            private readonly GameDevice mDevice;

            public Logger(GameDevice device)
            {
                if (device == null)
                    throw new Exception("Logger: initial with null GameDevice");

                mDevice = device;
            }

            public void D(String tag, String msg)
            {
                mDevice.Log(GameDevice.LOG_DEBUG, tag, msg);
            }

            public void E(String tag, String msg)
            {
                mDevice.Log(GameDevice.LOG_ERROR, tag, msg);
            }

            public void W(String tag, String msg)
            {
                mDevice.Log(GameDevice.LOG_WARNING, tag, msg);
            }

            public void F(String tag, String msg)
            {
                mDevice.Log(GameDevice.LOG_FATAL, tag, msg);
            }

            public void I(String tag, String msg)
            {
                mDevice.Log(GameDevice.LOG_VERBOSE, tag, msg);
            }

            public void V(String tag, String msg)
            {
                mDevice.Log(GameDevice.LOG_VERBOSE, tag, msg);
            }
        }
    }
}
