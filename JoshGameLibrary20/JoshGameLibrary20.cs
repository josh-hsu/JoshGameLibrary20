using JoshGameLibrary20.services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoshGameLibrary20
{
    ///
    /// <summary>
    /// JoshGameLibrary (GL) - Version 2.0<br/>
    /// This game control library require the following initial phase<br/>
    /// <br/>
    /// GameLibrary20 mGL = new GameLibrary20();<br/>
    /// mGL.chooseDevice(DEVICE_TYPE);      //DEVICE_TYPE could be DEVICE_TYPE_BLUE_STACK or DEVICE_TYPE_WINDOWS_10
    /// mGL.setDeviceEssentials(Objects);   //optional<br/>
    /// mGL.initDevice(Objects);            //if this return NO_ERROR, GL will mark mDeviceReady flag<br/>
    /// </summary>
    ///
    public class JoshGameLibrary20
    {
        public const String TAG = "GL20";
        public const int DEVICE_TYPE_ANDROID_INTERNAL = 1;
        public const int DEVICE_TYPE_NOX_PLAYER = 9;

        private bool mDeviceReady;
        private GameDevice mDevice;
        private DeviceScreen mScreenService;
        private DeviceInteract mInteractService;

        public JoshGameLibrary20()
        {
            mDeviceReady = false;
            mDevice = null;
        }
        public void GetInstance()
        {
            Console.WriteLine("Hello from JoshGameLibrary20");
        }

        public class ScreenshotErrorException : Exception
        {
            readonly int failReason;

            public ScreenshotErrorException(String message, int code) : base(message)
            {
                failReason = code;
            }

            public int GetFailReason() { return failReason; }
        }
}

}
