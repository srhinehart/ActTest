using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;

namespace Fluxus
{
    public class IniFile
    {
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileStringW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true), SuppressUnmanagedCodeSecurity]
        private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] char[] lpReturnedString, int nSize, string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WritePrivateProfileString(string lpAppName,
           string lpKeyName, string lpString, string lpFileName);

        protected string IniFileName;
        public string FileName
        {
            get { return IniFileName; }
            set { IniFileName = value; }
        }

        public IniFile()
        {
        }

        public IniFile(string FileName)
        {
            IniFileName = FileName;
        }

        private string GetPrivateProfileString(string sectionName, string keyName, string Default)
        {
            char[] ret = new char[256];

            while (true)
            {
                int length = GetPrivateProfileString(sectionName, keyName, Default, ret, ret.Length, IniFileName);
                if (0 == length && 0 < Marshal.GetLastWin32Error())
                    //Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    return Default;

                // This function behaves differently if sectionName or keyName are null
                if (sectionName != null && keyName != null)
                {
                    // Single string return value
                    if (length == ret.Length - 1)
                    {
                        // Double the buffer size and call again
                        ret = new char[ret.Length * 2];
                    }
                    else
                    {
                        // Return simple string
                        return new string(ret, 0, length);
                    }
                }
                else
                {
                    // Multi-string returned
                    if (length == ret.Length - 2)
                    {
                        // Double the buffer size and call again
                        ret = new char[ret.Length * 2];
                    }
                    else
                    {
                        // Return multistring
                        return new string(ret, 0, length - 1);
                    }
                }
            }
        }

        public string[] GetSectionNames()
        {
            return GetPrivateProfileString(null, null, null).Split('\0');
        }

        public string[] GetKeyNames(string sectionName)
        {
            return GetPrivateProfileString(sectionName, null, null).Split('\0');
        }

        public string GetValue(string sectionName, string keyName, string Default)
        {
            return GetPrivateProfileString(sectionName, keyName, Default);
        }

        private void WritePrivateProfileString(string sectionName, string keyName, string value)
        {
            bool success = WritePrivateProfileString(sectionName, keyName, value, IniFileName);
            if (!success)
            {
                if (0 < Marshal.GetLastWin32Error())
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }

        public void DeleteSection(string sectionName)
        {
            WritePrivateProfileString(sectionName, null, null);
        }

        public void DeleteKey(string sectionName, string keyName)
        {
            WritePrivateProfileString(sectionName, keyName, null);
        }

        public void WriteValue(string sectionName, string keyName, string value)
        {
            WritePrivateProfileString(sectionName, keyName, value);
        }
    }
}
