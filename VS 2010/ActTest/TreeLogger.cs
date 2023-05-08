using System;
using System.Text;
using System.Threading;
using System.IO;

namespace TreeLog
{
    public enum SeverityCode { Info, Warning, Error, Failure, COUNT };

    public enum FieldIndex
    {
        OffsetToParent,     // offset from parent entry (if > 0)
        TickCount,
        Severity,           // SeverityCode above
        Category,           // Name of standardized category (from .ToString() call on LogWrapper.LogCat value)
        Msg1,               // Primary message.  Time string for LOG_* categories
        Msg2                // Secondary message, or details.  Optional fileName string for LOG_* categories
    };

    public static class LogConsts
    {
        public const int LOG_FIELDS = 6;
        public const uint FIRST_ENTRY_ID = 0;
        public const int SyncIntervalDays = 20;   // keep less than 49 days, which is when tick counter overflows 
        public static string strTimeFormat = "\"{0:yyyy-MM-dd HH:mm:ss.fff}Z\""; // UTC sortable format, with milliseconds, and surrounding quotes
        public const DateTimeKind DefaultTimeKind_Input = DateTimeKind.Local;   // assume unlabeled timestrings are local
    }

    public class Logger : IDisposable
    {
        // Logging object designed for use with multiple threads.
        protected StreamWriter sw = null;
        protected uint EntryID = LogConsts.FIRST_ENTRY_ID;
        protected string CurrentFileName = null;
        protected uint Time0Ticks = 0; // Ticks since last time synch
        private uint LastLoggedTickCount = 0;
        private uint TimeSyncEntries = 0;
        private object lockObj = new object();

        private DateTime dtLastSync = DateTime.Now;
        private TimeSpan tsSyncInterval = TimeSpan.FromDays(LogConsts.SyncIntervalDays);

        private UInt64[] EntryCounts = new UInt64[(int)SeverityCode.COUNT];

        public UInt64 InfoEntryCount
        {
            get { return EntryCounts[(int)SeverityCode.Info] + TimeSyncEntries; }
            // TimeSync entries, which are used to sync, open, close, and bridge log files
            // are handled separately from explicit LogEntry calls.
            // These entries always have a SeverityCode of Info.
        }

        public UInt64 WarningEntryCount
        {
            get { return EntryCounts[(int)SeverityCode.Warning]; }
        }

        public UInt64 ErrorEntryCount
        {
            get { return EntryCounts[(int)SeverityCode.Error]; }
        }

        public UInt64 FailureEntryCount
        {
            get { return EntryCounts[(int)SeverityCode.Failure]; }
        }

        internal uint TickCount()
        {
            // Calculates ticks since Time0Ticks (last TimeSync).
            // Write a new time synch entry if needed, to keep tick counter
            // synchronized with time.
            uint Result = (uint)Environment.TickCount - Time0Ticks;
            DateTime dtNow = DateTime.Now;
            bool TicksWrapped = (Result < LastLoggedTickCount); // tick count has wrapped (~49days)
            bool IntervalElapsed = (dtNow - dtLastSync) > tsSyncInterval;

            if (TicksWrapped || IntervalElapsed)
            {
                // Re-synchronize tick count with current DateTime.
                WriteTimeSyncEntry("LOG_SYNC", CurrentFileName);
                Result = 0;
            }
            LastLoggedTickCount = Result;
            return Result;
        }
        
        internal uint LogWrite(uint? ParentEntryID, string FormattedDataLine, bool IsTimeSyncEntry)
        {
            lock (lockObj)
            {
                uint Ticks = TickCount(); // Calls WriteTimeSyncEntry, if needed
                EntryID++;
                if (IsTimeSyncEntry)
                    TimeSyncEntries++;

                uint ParentIDOffset = ParentEntryID.HasValue ? EntryID - ParentEntryID.Value : 0;
                string strTemp = string.Format("{0},{1},{2}", ParentIDOffset, Ticks.ToString(), FormattedDataLine);

                if (sw != null)
                {
                    sw.WriteLine(strTemp);
                    sw.Flush();
                }
            }
            return EntryID;
        }

        internal string EscapeCSVstring(string Input)
        {
            if (Input == null)
                return "";

            bool QuotesRequired = false;
            StringBuilder sb = new StringBuilder(Input);    // start with unmodified Input
            int sourceindex = 0;
            int destindex = 0;
            char c;

            while (sourceindex < Input.Length)
            {
                c = Input[sourceindex++];
                destindex++;
                switch (c)
                {
                    case '\"':
                        sb.Insert(destindex++, c);   // quotes are escaped by doubling them
                        QuotesRequired = true;
                        break;
                    case '\t':
                    case '\n':
                    case '\r':
                    case ' ':
                    case ',':
                        QuotesRequired = true;
                        break;
                }
            }

            if (QuotesRequired)
            {
                sb.Insert(0, '\"');
                sb.Append('\"');
            }
            return sb.ToString();
        }


        internal uint WriteTimeSyncEntry(string Category, string FileName)
        {
            lock (lockObj)
            {
                dtLastSync = DateTime.Now;
                Time0Ticks = (uint)Environment.TickCount;
                LastLoggedTickCount = 0;

                string strTime = String.Format(LogConsts.strTimeFormat, dtLastSync.ToUniversalTime()); // Universal sortable format, with fractional seconds
                string DataLine = String.Format("0,0,{0},{1},{2},{3}", SeverityCode.Info.ToString(), Category, strTime,
                    EscapeCSVstring(FileName));
                EntryID++;
                TimeSyncEntries++;
                if (sw != null)
                {
                    sw.WriteLine(DataLine);
                    sw.Flush();
                }
            }
            return EntryID;
        }

        public string Filename
		{
			get { return CurrentFileName; }
		}

        public bool StreamOk()
        {
            return sw != null;
        }

		// Dispose() calls Dispose(true)
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		// NOTE: Leave out the finalizer altogether if this class doesn't 
		// own unmanaged resources itself, but leave the other methods
		// exactly as they are. 
		~Logger() 
		{
			// Finalizer calls Dispose(false)
			Dispose(false);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (sw != null)
                {
                    CloseLogFile(null);
                }
            }
            // free native resources if there are any.
        }

        public uint LogEntry(uint? ParentEntryID, SeverityCode Severity, string Category, string Msg1, string Msg2)
        {
            string DataLine = String.Format("{0},{1},{2},{3}", Severity.ToString(), EscapeCSVstring(Category),
                EscapeCSVstring(Msg1), EscapeCSVstring(Msg2));
            // ^ above string includes everything but the ParentOffset and TickCount, which are handled by LogWrite

            return LogWrite(ParentEntryID, DataLine, false);
        }

        public uint LogEntry(SeverityCode Severity, string Category, string Msg1, string Msg2)
        {
            // Parent-less entry
            string DataLine = String.Format("{0},{1},{2},{3}", Severity.ToString(), EscapeCSVstring(Category),
                EscapeCSVstring(Msg1), EscapeCSVstring(Msg2));
            // ^ above string includes everything but the ParentOffset and TickCount, which are handled by LogWrite

            return LogWrite(null, DataLine, false);
        }

        public uint LogEntry(SeverityCode Severity, string Category, string Msg1)
        {
            return LogEntry(Severity, Category, Msg1, "");
        }

        public uint ResetTickCounter()
        {
            // writes an entry to synchronize Tick counter with universal time value
            return WriteTimeSyncEntry("LOG_SYNC", CurrentFileName);
        }

        public uint OpenLogFile(string NewFileName)
        {
            uint Result = LogConsts.FIRST_ENTRY_ID;

            // If sw points to an open log file with a different name, a closing entry will be written to the existing file.
            try
            {
                StreamWriter newsw = new StreamWriter(NewFileName, false, Encoding.Default);
                if (sw != null)
                    CloseLogFile(NewFileName);  // last entry of old file will point to new file
                string PrevFileName = CurrentFileName;
                CurrentFileName = NewFileName;
                sw = newsw;
                //sw.WriteLine("OffsetFromParent,TickCount,Severity,Category,Msg1,Msg2");  // CSV header line
                Result = WriteTimeSyncEntry("LOG_OPEN", PrevFileName);
            }
            catch
            {
                // any exception from new StreamWriter() will cause the original streamwriter (sw) to be used
            }
            return Result;
        }

        internal uint CloseLogFile(string NextFileName)
        {
            //uint Result = WriteTimeSyncEntry("LOG_CLOSE", NextFileName);  // sets tick count to 0
            uint Result = LogConsts.FIRST_ENTRY_ID;
            if (sw != null)
            {
                DateTime dtNow = DateTime.Now;
                string strTime = String.Format(LogConsts.strTimeFormat, dtNow.ToUniversalTime()); // Universal sortable format, with fractional seconds
                string DataLine = String.Format("{0},LOG_CLOSE,{1},{2}", SeverityCode.Info.ToString(), strTime,
                                                EscapeCSVstring(NextFileName));
                Result = LogWrite(null, DataLine, true);
                //sw.Flush();   // already handled by LogWrite
                sw.Close();
                sw = null;
            }
            return Result;
        }
        
        public uint CloseLogFile()
        {
            return CloseLogFile(null);
        }

	}   // End of Logger class
}
