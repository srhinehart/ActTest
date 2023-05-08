using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows.Forms;
using TreeLog;
using CSharp411;

namespace Fluxus
{
    public static class Support_Serial
    {
        private static string PortPrefix()
        {
            // This function returns the prefix to remove when sorting a port list
            return OSInfo.RunningUnderWindows() ? "COM" : "/dev/ttyUSB";
            // We are excluding /dev/ttySnn under Linux, because these can appear whether they actually exist or not.
            // If non USB ports are needed under Linux, users can provide their own port lists to the functions that need them.
        }
        
        private static bool FeasiblePortName(PlatformID platform, string PortName)
        {
            bool Result = false;
            switch (platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    Result = PortName.Contains("COM"); // .Contains allows \\.\COM10, .StartsWith does not.
                    break;
                case PlatformID.Unix:	// includes Linux
                    Result = PortName.StartsWith("/dev/ttyUSB");
                    break;
            }
            return Result;
        }

        public static List<String> FeasiblePortNames()
        {
            List<String> Ports = new List<String>();
            PlatformID platform = Environment.OSVersion.Platform;
            foreach (string s in SerialPort.GetPortNames())
            {
                if (FeasiblePortName(platform, s))
                    Ports.Add(s);
            }
            return Ports;
        }

        public static bool IsStandardBitRate(int bitrate)
        {
            bool result = false;
            switch (bitrate)
            {
                //case 110:
                //case 300:
                //case 600:
                case 1200:
                case 2400:
                case 4800:
                case 9600:
                case 14400:
                case 19200:
                case 38400:
                case 57600:
                case 115200:
                case 230400:
                    result = true;
                    break;
            }
            return result;
        }

        public static SerialPort OpenPort(LogWrapper Log, uint eidInit, string PortName, int bitrate)
        {
            SerialPort sp = null;
            if (!string.IsNullOrEmpty(PortName))
            {
                uint eidPort = Log.LogEntry(eidInit, LogCat.Init, "Attempting to open port", PortName);
                sp = new SerialPort(PortName, bitrate);
                if (!sp.IsOpen)
                {
                    try
                    {
                        sp.Open();
                        // add default user to dialout group (and reboot) if exception occurs in above line
                    }
                    catch (Exception e)
                    {
                        // Windows generates an exception if port is already in use by this or another app,
                        // because SerialPort.IsOpen (stupidly) returns False.
                        if (e is System.UnauthorizedAccessException && OSInfo.RunningUnderWindows())
                            Log.LogEntry(eidPort, LogCat.Init, "Port already in use", PortName); // not a big deal
                        else
                            Log.LogEntry(eidPort, SeverityCode.Error, LogCat.Init, "Exception occurred", e.Message);
                        sp.Dispose();
                        sp = null;
                    }
                }
                else
                {
                    Log.LogEntry(eidPort, LogCat.Init, "Port already in use", PortName);
                    sp.Dispose();
                    sp = null;
                }
            }
            return sp;
        }

        private static string StopBitsString(StopBits setting)
        {
            switch(setting)
            {
                case System.IO.Ports.StopBits.None:
                    return "None";
                case System.IO.Ports.StopBits.One:
                    return "1";
                case System.IO.Ports.StopBits.OnePointFive:
                    return "1.5";
                case System.IO.Ports.StopBits.Two:
                    return "2";
                default:
                    return "unknown";
            }
        }

        public static SerialPort SelectPort(uint? eidInit, LogWrapper Log, string[] PortList, string PortPreference, int bitrate, string Usage)
        {
            uint eidPortInit = Log.LogEntry(eidInit, LogCat.Init, "Selecting serial port", Usage);
            SerialPort sp = OpenPort(Log, eidPortInit, PortPreference, bitrate);
            if (sp == null)
            {
                foreach (string s in PortList)
                {
                    sp = OpenPort(Log, eidPortInit, s, bitrate);
                    if (sp != null)
                        break;
                }
            }

            if (sp != null && sp.IsOpen)
            {
                Log.LogEntry(eidPortInit, LogCat.Init, "Opened " + sp.PortName,
                    string.Format("{0} bps, {1}-{2}-{3}", bitrate, sp.DataBits, sp.Parity.ToString()[0], StopBitsString(sp.StopBits)));
                //Log.LogEntry(eidPortInit, LogCat.Init, "Successfully opened port", sp.PortName);
            }
            return sp;
        }

        public static string[] PortList()
        {
            List<string> listPorts = FeasiblePortNames();
            string prefix = PortPrefix();

            // The line below orders by numeric port value, rather than ASCII, so COM10 will follow COM9, rather than COM1.
            var sorted = listPorts.OrderBy(port => Convert.ToInt32(port.Replace(prefix, string.Empty)));
            
            return sorted.ToArray();
        }

        public static void PopulateComboBoxWithPortNames(string[] PortList, ComboBox cb, string UnknownPortPlaceholder, string Selected)
        {
            if (cb != null)
            {
                cb.Items.Clear();
                if (UnknownPortPlaceholder != null & UnknownPortPlaceholder.Length > 0)
                    cb.Items.Add(UnknownPortPlaceholder);
                foreach (string s in PortList)
					cb.Items.Add(s);
                if (!string.IsNullOrEmpty(Selected))
                    cb.SelectedIndex = cb.Items.IndexOf(Selected);
            }
        }
    }
}
