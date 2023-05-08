using System;
using System.IO;
using TreeLog;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Fluxus
{
	// standardized categories for logging
	public enum LogCat
	{
		Init,
		Operation,
        SimOp,
		Phase,
		Task,
		Command,
		Response,
        Result,
		Prompt,
		Status,
		Detection,
        Laser,
        Meter,
        MFC,
        LLC,
        ATF,
        Stage,
        Interlock
	}

	public class LogWrapper : IDisposable
	{
        // Provides convenient overloads, and optional console-ish echoing for logged messages.
		public Logger InnerLog = null;
        public SeverityCode ConsoleEchoThreshold = SeverityCode.Warning;
        public List<string> myStrings = new List<string>();
        private TextBox myConsole = null;

		public LogWrapper()
		{
			// default constructor
            string strLogFileName = DefaultFileName();
			InnerLog = new Logger ();
			InnerLog.OpenLogFile (strLogFileName);	// writes first entry
		}

		public LogWrapper(string Filename)
		{
            // preferred constructor, which allows filename to be specified by caller
            InnerLog = new Logger();
            if (Filename == null || Filename.Length <= 0)
                Filename = DefaultFileName();
			InnerLog.OpenLogFile (Filename);	// writes first entry
		}

        public LogWrapper(string Filename, TextBox Console)
        {
            // Alternate constructor, which allows filename and TextBox to be specified by caller
            myConsole = Console;
            InnerLog = new Logger();
            if (Filename == null || Filename.Length <= 0)
                Filename = DefaultFileName();
            InnerLog.OpenLogFile(Filename);	// writes first entry
        }

		// Dispose() calls Dispose(true)
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

        // The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing) 
			{
                myConsole = null;   //we don't own myConsole, so we can't free it

                // free managed resources

                if (InnerLog != null)
				{
					InnerLog.Dispose();
					InnerLog = null;
				}

                if (myStrings != null)
                {
                    myStrings.Clear();
                    myStrings = null;
                }
			}
            // Unmanaged resources would be freed here, if there were any.
            // If unmanaged resources were used, this class would also need a finalizer, which called Dispose(false);
        }

        public string DefaultFileName()
        {
            return Path.Combine(Environment.CurrentDirectory, "LogFile.txt");
        }

        public void ConsoleWriteLine(string s)
        {
            myStrings.Add(s);
            if (myConsole != null)
                myConsole.Lines = myStrings.ToArray();
        }

        public void ConsoleWriteLine(string fmt, string data1, string data2)
        {
            if (myStrings != null)
            {
                myStrings.Add(string.Format(fmt, data1, data2));
                if (myConsole != null)
                    myConsole.Lines = myStrings.ToArray();
            }
        }
        
        private void Echo(bool EchoToConsole, string Msg)
		{
			if (EchoToConsole)
				ConsoleWriteLine (Msg);
		}

		private void Echo(bool EchoToConsole, string Msg1, string Msg2)
		{
            if (EchoToConsole) 
			{
				if (Msg2.Length > 0)
					ConsoleWriteLine ("{0}: {1}", Msg1, Msg2);
				else
					ConsoleWriteLine (Msg1);
			}
		}

		public uint LogEntry(uint? ParentEid, SeverityCode Severity, LogCat Category, string Msg)
		{
            uint eid = 0;
            if (InnerLog != null)
                eid = InnerLog.LogEntry(ParentEid, Severity, Category.ToString(), Msg, "");
			Echo(Severity >= ConsoleEchoThreshold, Msg);
			return eid;
		}

		public uint LogEntry(uint? ParentEid, SeverityCode Severity, LogCat Category, string Msg1, string Msg2)
		{
            uint eid = 0;
            if (InnerLog != null)
                eid = InnerLog.LogEntry(ParentEid, Severity, Category.ToString(), Msg1, Msg2);
            Echo(Severity >= ConsoleEchoThreshold, Msg1, Msg2);
			return eid;
		}

        public uint LogEntry(bool ForceEcho, uint? ParentEid, SeverityCode Severity, LogCat Category, string Msg1, string Msg2)
        {
            uint eid = 0;
            if (InnerLog != null)
                eid = InnerLog.LogEntry(ParentEid, Severity, Category.ToString(), Msg1, Msg2);
            Echo(ForceEcho || Severity >= ConsoleEchoThreshold, Msg1, Msg2);
            return eid;
        }

        public uint LogEntry(uint? ParentEid, LogCat Category, string Msg)
		{
			uint eid = 0;
            if (InnerLog != null)
                eid = InnerLog.LogEntry (ParentEid, SeverityCode.Info, Category.ToString(), Msg, "" );
            Echo(false, Msg);
			return eid;
		}

        public uint LogEntry(bool ForceEcho, uint? ParentEid, LogCat Category, string Msg)
        {
            uint eid = 0;
            if (InnerLog != null)
                eid = InnerLog.LogEntry(ParentEid, SeverityCode.Info, Category.ToString(), Msg, "");
            Echo(ForceEcho, Msg);
            return eid;
        }

        public uint LogEntry(uint? ParentEid, LogCat Category, string Msg1, string Msg2)
		{
            uint eid = 0;
            if (InnerLog != null)
                eid = InnerLog.LogEntry(ParentEid, SeverityCode.Info, Category.ToString(), Msg1, Msg2);
            Echo(false, Msg1, Msg2);
			return eid;
		}

        public uint LogEntry(bool ForceEcho, LogCat Category, string Msg1, string Msg2)
        {
            uint eid = 0;
            if (InnerLog != null)
                eid = InnerLog.LogEntry(SeverityCode.Info, Category.ToString(), Msg1, Msg2);
            Echo(ForceEcho, Msg1, Msg2);
            return eid;
        }

        public uint LogEntry(bool ForceEcho, uint? ParentEid, LogCat Category, string Msg1, string Msg2)
        {
            uint eid = 0;
            if (InnerLog != null)
                eid = InnerLog.LogEntry(ParentEid, SeverityCode.Info, Category.ToString(), Msg1, Msg2);
            Echo(ForceEcho, Msg1, Msg2);
            return eid;
        }
        
        public uint LogEntry(SeverityCode Severity, LogCat Category, string Msg)
		{
            uint eid = 0;
            if (InnerLog != null)
                eid = InnerLog.LogEntry(Severity, Category.ToString(), Msg, "");
			Echo(Severity >= ConsoleEchoThreshold, Msg);
			return eid;
		}

		public uint LogEntry(SeverityCode Severity, LogCat Category, string Msg1, string Msg2)
		{
            uint eid = 0;
            if (InnerLog != null)
                eid = InnerLog.LogEntry(Severity, Category.ToString(), Msg1, Msg2);
            Echo(Severity >= ConsoleEchoThreshold, Msg1, Msg2);
			return eid;
		}

		public uint LogEntry(LogCat Category, string Msg)
		{
            uint eid = 0;
            if (InnerLog != null)
                eid = InnerLog.LogEntry(SeverityCode.Info, Category.ToString(), Msg, "");
            Echo(false, Msg);
			return eid;
		}

		public uint LogEntry(LogCat Category, string Msg1, string Msg2)
		{
            uint eid = 0;
            if (InnerLog != null)
                eid = InnerLog.LogEntry(SeverityCode.Info, Category.ToString(), Msg1, Msg2);
            Echo(false, Msg1, Msg2);
			return eid;
		}
	}
}

