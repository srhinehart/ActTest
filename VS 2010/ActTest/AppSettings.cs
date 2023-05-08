using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using CSharp411;
using Fluxus;

namespace ActTest
{
    public enum ControllerStates
    {
        Unknown,
        Idle,
        PurgingStaleData,
        SendingCommand,
        ExpectingResponse,
        ProcessingResponse
    };

    public enum OpResult
    {
        Unknown = 0,
        Success,
        //ConfirmationRequested,
        TimedOut,
        Error
    };

    public class AppSettings
	{
        // class to hold application settings, some of which are configurable via .ini file entries

        private const string SectionSystem = "System";  // this section can have Type, Port, and BitRate entries
        private const string    KeyPort = "Port";

        private const string SectionPaths = "Paths";
        private const string    KeyLogs = "Logs";
        private const string    KeyProfiles = "Profiles";

        // .Ini section and key names for GUI settings
        public static string SectionFormMain = "WindowMain";
        //public static string SectionFormVideo = "WindowVideo";
        private const string    KeyFont = "Font";
        private const string    KeyFontMono = "FontMono";   // Monospaced font for hex controls
        private const string    KeySize = "Size";
        private const string    KeyPosition = "Position";

        public const int DefBitRate = 115200;

        protected ushort[] tableCRC = new ushort[256];

        private void BuildCRCtable()
        {
            const ushort poly = 0x1021; // CRC-16 CCITT truncated polynomial (actual value is 0x11021, but highest bit is understood)
            ushort temp, a;
            for (int i = 0; i < tableCRC.Length; ++i)
            {
                temp = 0;
                a = (ushort)(i << 8);
                for (int j = 0; j < 8; ++j)
                {
                    if (((temp ^ a) & 0x8000) != 0)
                        temp = (ushort)((temp << 1) ^ poly);
                    else
                        temp <<= 1;
                    a <<= 1;
                }
                tableCRC[i] = temp;
            }
        }

        public ushort Crc16Ccitt(byte[] bytes)
        {
            ushort initialValue = 0x1D0F; //0xffff;
            // initialValue is 0x1D0F, instead of 0xFFFF, to compensate for the fact that we are not adding 16 zero bits to the end of the bytes[] array.
            // 0x1D0F would be the CRC for an empty message, after adding the appropriate padding of 16 zero bits.

            ushort crc = initialValue;
            for (int i = 0; i < bytes.Length; ++i)
            {
                crc = (ushort)((crc << 8) ^ tableCRC[((crc >> 8) ^ (0xff & bytes[i]))]);
            }
            return crc;
        }


        private string strLogPath;
        public string LogPath
        { get { return strLogPath; } }

        private string strLogFilename = null;
		public string LogFilename
		{
			get { return strLogFilename; }
		}

        private string strProfilePath;
        public string ProfilePath
        {  get { return strProfilePath; } }

        private string strPrefSystemPort = "COM3";
        public string SystemPort
        {
            get { return strPrefSystemPort; }
        }

#if false
        private int myBitRate = DefBitRate;
        public int SystemBitRate
        {
            get { return myBitRate; }
        }

        private string strPrefLaserPort = "COM3";
        public string LaserPort
        {
            get { return strPrefLaserPort; }
        }

        private int myLaserBitRate = DefBitRate;
        public int LaserBitRate
        {
            get { return myLaserBitRate; }
        }
#endif

        public static string cszCompanyFolderName = "Fluxus";
        
        public string AppDataPath()
        {
            string result = null;
            try
            {
                string BasePath = Environment.GetFolderPath(OSInfo.RunningUnderWindows() ?
                    Environment.SpecialFolder.CommonApplicationData :  // typically C:\ProgramData under Windows 8, /usr/share under Linux
                    Environment.SpecialFolder.ApplicationData); // Need alternate path for Linux, due to rights, and dir crowding in /usr/share
                    
                if (!string.IsNullOrEmpty(BasePath))
                {
                    result = Path.Combine(BasePath, cszCompanyFolderName);
                    result = Path.Combine(result, Application.ProductName);
                    if (!Directory.Exists(result))
                        Directory.CreateDirectory(result);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception occured while trying to create app data directory.");
                Console.WriteLine(e.ToString()); 
                result = null;
            }
            return result;
        }

#if false
        public string IniFilename(string IniPath)
        {
            //return Path.ChangeExtension(Application.ExecutablePath, ".ini");    // typically don't have rights here
            string result = null;
            try
            {
                result = string.Format("{0}.ini", Application.ProductName);
                result = Path.Combine(IniPath, result);
            }
            catch
            {
                result = null;
            }
            return result;
        }

        private IniFile myIni = null;
        public IniFile MainIniFile()
        {
            return myIni;
        }

        private string strPrefProfile = null; //"test.xml";
        public string PreferredProfile
        {
            get { return strPrefProfile; }
        }

        private LaserTypes myLaserType = LaserTypes.EthosII;
        public LaserTypes LaserType
        {
            get { return myLaserType; }
        }

        private LaserSystemTypes mySystemType = LaserSystemTypes.EthosII;
        public LaserSystemTypes LaserSystemType
        {
            get { return mySystemType; }
        }

        public void PopulateSystemTypeComboBox(ComboBox cbo, LaserSystemTypes CurrentType)
        {
            if (cbo != null)
            {
                cbo.Items.Clear();
                LaserSystemTypes lst = LaserSystemTypes.Unknown;
                while (lst < LaserSystemTypes.COUNT)
                {
                    cbo.Items.Add(lst.ToString());
                    lst++;
                }
                cbo.SelectedIndex = cbo.Items.IndexOf(CurrentType.ToString());
            }
        }

        public void SaveMainWindowSettingsToIni(Font fnt, Size? size, Point? point)
        {
            string Section = SectionFormMain;
            if (myIni != null)
            {
                //if (fnt != null)
                //    myIni.WriteValue(Section, KeyFont, PropertyBag.Tag_Font(fnt));
                if (size.HasValue)
                    myIni.WriteValue(Section, KeySize, PropertyBag.Tag_Size(size.Value));
                if (point.HasValue)
                    myIni.WriteValue(Section, KeyPosition, PropertyBag.Tag_Point(point.Value));
            }
        }

        public void SaveVideoWindowSettingsToIni(Size? size, Point? point)
        {
            string Section = SectionFormVideo;
            if (myIni != null)
            {
                //if (fnt != null)
                //    myIni.WriteValue(Section, KeyFont, PropertyBag.Tag_Font(fnt));
                if (size.HasValue)
                    myIni.WriteValue(Section, KeySize, PropertyBag.Tag_Size(size.Value));
                if (point.HasValue)
                    myIni.WriteValue(Section, KeyPosition, PropertyBag.Tag_Point(point.Value));
            }
        }

        public void ApplySystemSettings(LaserSystemTypes SysType, string Port, int BitRate)
        {
            mySystemType = SysType;
            myBitRate = BitRate;
            strPrefSystemPort = Port;
        }

        public void SaveSystemSettingsToIni()
        {
            if (myIni != null)
            {
                //myIni.WriteValue(SectionSystem, KeyType, mySystemType.ToString());
                myIni.WriteValue(SectionSystem, KeyPort, SystemPort.ToString());
                myIni.WriteValue(SectionSystem, KeyBitRate, SystemBitRate.ToString());
            }
        }

        public Size? GetSpecifiedSize(string Section)
        {
            Size? result = null;
            if (myIni != null)
            {
                string s = myIni.GetValue(Section, KeySize, "");
                result = PropertyBag.SizeViaTag(s);
            }
            return result;
        }

        public Point? GetSpecifiedPoint(string Section)
        {
            Point? result = null;
            if (myIni != null)
            {
                string s = myIni.GetValue(Section, KeyPosition, "");
                result = PropertyBag.PointViaTag(s);
            }
            return result;
        }

        public Font GetSpecifiedFont(string Section)
        {
            Font result = null;
            if (myIni != null)
            {
                string s = myIni.GetValue(Section, KeyFont, "");
                result = PropertyBag.FontViaTag(s);
            }
            return result;
        }
#endif

        public AppSettings()
		{
            Console.WriteLine("Configuring App Data Path");
            string AppPath = AppDataPath();
            Console.WriteLine("AppPath: {0}, {1} chars", AppPath, AppPath == null ? -1 : AppPath.Length);
#if false
            string IniPath = IniFilename(AppPath);
            myIni = new IniFile(IniPath);
            Console.WriteLine("IniPath: {0}, {1} chars", IniPath, IniPath == null ? -1 : IniPath.Length);
#endif
            Console.WriteLine("Building LogPath");
            strLogPath = Path.Combine(AppPath, "Logs");    // default
            strProfilePath = Path.Combine(AppPath, "Profiles"); // default
            
#if false
            Console.WriteLine("Building IniFile");

            if (!File.Exists(IniPath))
            {
                // Create default .ini file
                myIni.WriteValue(SectionSystem, KeyPort, strPrefSystemPort);

                myIni.WriteValue(SectionPaths, KeyLogs, strLogPath);
                myIni.WriteValue(SectionPaths, KeyProfiles, strProfilePath);
            }
            // set log filename
            strLogPath = myIni.GetValue(SectionPaths, KeyLogs, strLogPath); 
#endif
            if (!Directory.Exists(strLogPath))
                Directory.CreateDirectory(strLogPath);
            Console.WriteLine("Building Log filename");
            strLogFilename = Path.Combine(strLogPath,
				string.Format ("{0:yyyy-MM-dd_HH_mm_ss}_{1}.log", DateTime.Now, Application.ProductName));

            BuildCRCtable();

        }
    }
}

