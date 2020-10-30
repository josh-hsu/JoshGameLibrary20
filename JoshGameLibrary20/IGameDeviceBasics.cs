using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoshGameLibrary20
{
    public interface IGameDeviceBasics
    {
        /**
         * query preloaded screenshot paths for further screen dump
         * @return String array of screenshot paths supported
         */
        String[] QueryPreloadedPaths();

        /**
         * query the preloaded screenshot paths count
         * @return The path count
         */
        int QueryPreloadedPathCount();

        /**
         * dump screen for the path
         * @param path The path should be in the list of preloaded paths
         * @return 0 upon success
         */
        int DumpScreen(String path);

        /**
         * dump screen for the path and convert to png format
         * @param path The path should be in the list of preloaded paths
         * @return 0 upon success
         */
        int DumpScreenPng(String path);

        /**
         * deal with the mouse or touch screen events
         * @param x1 source x-axis coordination
         * @param y1 source y-axis coordination
         * @param x2 destination x-axis coordination (may not need)
         * @param y2 destination y-axis coordination (may not need)
         * @param event The event type
         * @return 0 upon success
         */
        int MouseEvent(int x1, int y1, int x2, int y2, int evt);

        /**
         * run privileged command such as dump screen or others
         * the result will be ignored
         * TODO: Redirect the pipe of output to internal path
         * @param command The command string send to device
         * @return 0 upon success
         */
        int RunCommand(String command);

        /**
         * run normal shell command
         * the result will be returned
         * @param command The command string send to device
         * @return The result of the command
         */
        String RunShellCommand(String command);

        /**
         * get the version description of this device
         * @return The version string
         */
        String GetVersion();

        /**
         * get the system type of this device
         * @return The system type of the device, can be one of Windows, Linux or Darwin.
         */
        int GetSystemType();

        /**
         * get the transaction time in milliseconds after every command
         * such as runCommand and dumpScreen
         * @return The transaction time in milliseconds
         */
        int GetWaitTransactionTimeMs();

        /**
         * set the transaction time in milliseconds after every command
         * such as runCommand and dumpScreen
         */
        void SetWaitTransactionTimeMsOverride(int ms);

        /**
         * use hardware simulated way to send input command
         * it is used to prevent our tool been detected by games or apps
         * @param enable True if we want to use hardware simulation otherwise False can be set
         * @return 0 if both supported and switched to selected mode, -9 if not supported, otherwise
         *         -1 will be returned.
         */
        int SetHWSimulatedInput(bool enable);

        /**
         * when the device has been activated, this method should be called
         * @return 0 upon success
         */
        int OnStart();

        /**
         * when the device has been disabled, this method should be called
         * @return 0 upon success
         */
        int OnExit();

        /**
         * log information into device
         * @param level Log level defined in {@link GameLibrary20}
         * @param tag Log tag label String
         * @param log Log text String
         */
        void LogDevice(int level, String tag, String log);

        /**
         * register vibrator event such as on, off
         * @return 0 upon success or
         */
        int RegisterEvent(int type, IGameDeviceHWEventListener el);

        /**
         * deregister vibrator event use the same listener object
         */
        int DeregisterEvent(int type, IGameDeviceHWEventListener el);
    }
}
