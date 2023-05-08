using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.Data;
//using System.Data.OleDb;
//using System.Data.Odbc;
using System.Windows.Forms;

// This file is System.Windows.Forms-centric, because of the association between LogEntry and TreeNode objects

namespace TreeLog
{
    public enum ParserState
    {
        EatingLeadingSpaces, // Initial state when reading a new column
        QuotedField, // inside a quoted field
        NewQuote, // first quote hit in a quoted field.  May be an escaped quote or end of field
        UnquotedField,
        IgnoringToComma,
        IgnoringToEOL,  // used only for error recovery
    };

    public class LogEntry
    {
        // This class is designed to help a LogReader parse, organize, and display log entries. 

        private const int YEAR_VAL = 0;
        private const int MONTH_VAL = 1;
        private const int DAY_VAL = 2;
        private const int HOUR_VAL = 3;
        private const int MINUTE_VAL = 4;
        private const int SECOND_VAL = 5;
        private const int MS_VAL = 6;
        private const int TIME_VALUE_COUNT = 7;

        public uint OffsetFromParent;
        public uint Ticks;
        public DateTime EventTime;  // local time
        public SeverityCode Severity;
        public string Msg1;
        public string Msg2;
        public int CategoryIndex;   // index to string list
        public TreeNode Node = null;
        public SeverityCode DescendantSeverity = SeverityCode.Info; // reflects current worst-case severity of this node's descendants
        public bool WasExpanded = false;   // retains manual node expansion status across tree rebuilds and refreshes 

        public SeverityCode EffectiveSeverity(bool UsePropagatedSeverity)
        {
            return (UsePropagatedSeverity && DescendantSeverity > Severity) ? DescendantSeverity : Severity;
        }

        public SeverityCode SeverityFromString(string s)
        {
            SeverityCode Result = SeverityCode.Failure;
            try
            {
                char c = s[0];
                switch(c)
                {
                    case 'i':
                    case 'I': return SeverityCode.Info;
                    case 'w':
                    case 'W': return SeverityCode.Warning;
                    case 'e':
                    case 'E': return SeverityCode.Error;
                    default: return SeverityCode.Failure;
                }
            }
            catch { }
            return Result;
        }

        public uint UIntFromString(string s)
        {
            uint Result = 0;
            try
            {
                Result = Convert.ToUInt32(s);
            }
            catch { }
            return Result;
        }

        public static string ReplaceAny(string source, string targets, string replacement)
        {
            string Result = source;
            char[] targchars = targets.ToCharArray();
            int index = source.IndexOfAny(targchars);

            if (index < 0)
                return source;

            int copypos = 0;
            StringBuilder sb = new StringBuilder();
            while (index >= 0)
            {
                sb.Append(source.Substring(copypos, index - copypos));  // copy characters from copypos to just before target location
                sb.Append(replacement); // add replacement
                copypos = index + 1;
                index = source.IndexOfAny(targchars, copypos);
            }
            sb.Append(source.Substring(copypos)); // copy portion of source following last target char
            return sb.ToString();
        }

        public static DateTime ConvertTimeStamp(string strTime, DateTimeKind DefaultTimeKind)
        {
            DateTime Result;
            DateTimeKind TimeKind = DefaultTimeKind;
            strTime = ReplaceAny(strTime, "-:.,", " "); // . or , can separate seconds from milliseconds, to allow for German decimal separators

            int i = strTime.IndexOfAny(new char[] { 'z', 'Z' });
            if (i > 0)
            {
                // Z specifies that time is UTC
                TimeKind = DateTimeKind.Utc;
                strTime = ReplaceAny(strTime, "Zz", "");
            }

            string[] Fields = strTime.Split(new char[] { ' ' }, TIME_VALUE_COUNT, StringSplitOptions.None);

            Result = new DateTime(
                Convert.ToInt32(Fields[YEAR_VAL]),
                Convert.ToInt32(Fields[MONTH_VAL]),
                Convert.ToInt32(Fields[DAY_VAL]),
                Convert.ToInt32(Fields[HOUR_VAL]),
                Convert.ToInt32(Fields[MINUTE_VAL]),
                Convert.ToInt32(Fields[SECOND_VAL]),
                Convert.ToInt32(Fields[MS_VAL]),
                TimeKind);
            // Regardless of the input TimeKind, we want to use local time for display purposes
            if (TimeKind != DateTimeKind.Local)
                Result = Result.ToLocalTime();
            return Result;
        }

        public static string EscapeCSVstring(string Input)
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
    }

    public class EntryList : List<LogEntry>
    {
        public uint Time0_Ticks = 0;
        public DateTime Time0;

        public void AddEntry(LogEntry e, LogReader r)
        {
            if (e == null)
                return;

            Add(e); // add item to (unsorted) list
            int index = this.Count - 1; // index of latest entry

            if (index == 0)
                Time0_Ticks = e.Ticks;  // First log entry

            // Process LOG_* category (time synchronization) entries
            if ((e.CategoryIndex == r.LogOpenIndex)
                || (e.CategoryIndex == r.LogCloseIndex)
                || (e.CategoryIndex == r.LogSyncIndex))
            {
                Time0_Ticks = e.Ticks;
                Time0 = LogEntry.ConvertTimeStamp(e.Msg1, LogConsts.DefaultTimeKind_Input);
            }

            // Finish initalizing entry variables
            e.DescendantSeverity = SeverityCode.Info;   // this new entry has no descendants yet
            e.EventTime = Time0.AddMilliseconds((double)(e.Ticks - Time0_Ticks));

            bool RebuildRequired = false;

            // Propagate child severity for this entry up to the highest possible node (oldest ancestor)
            SeverityCode Severity = e.Severity;
            while (e.OffsetFromParent > 0)
            {
                index -= (int)e.OffsetFromParent;   // determine index of next ancestor
                if (index >= 0)
                {
                    e = this[index];
                    if (e.DescendantSeverity < Severity)
                    {
                        e.DescendantSeverity = Severity;    // new worst case, keep propagating severity upward
                        RebuildRequired = true; // propagating severity can change node visibility
                    }
                    else
                        break; // done - ancestor's DescendantSeverity already same or worse than that of newest entry
                }
                else
                    break;  // done - next oldest ancestor no longer present in list
            }

            if (r == null)
                return;

            if (r.tv != null)
            {
                if (RebuildRequired)
                {
                    r.RebuildTree();
                }
                else
                {
                    r.CreateNodeForVisibleEntry(this.Count - 1); // both e and index were changed during severity propagation
                }
            }
        }

        public LogEntry ParentEntry(LogEntry child)
        {
            int childindex = IndexOf(child);    // probably not super efficient
            return ParentEntry(childindex);
        }

        public LogEntry ParentEntry(int childindex)
        {
            LogEntry parent = null;
            if ((childindex >= 0) && (childindex < this.Count))
            {
                LogEntry child = this[childindex];
                if (child.OffsetFromParent > 0)
                {
                    int parentindex = childindex - (int)child.OffsetFromParent;
                    if (parentindex >= 0)
                        parent = this[parentindex];
                }
            }
            return parent;
        }
    }

    public class LogReader : IDisposable
    {
        public EntryList Entries = new EntryList();
        public List<String> Categories = new List<String>();
        //public List<int> VisibleCategories = new List<int>();
        public bool[] CategoryIsVisible = new bool[0];
        public StreamReader sr = null;
        public TreeView tv = null;
        string FieldData = "";
        ParserState ps = ParserState.EatingLeadingSpaces;
        FieldIndex fi = FieldIndex.OffsetToParent;
        uint LinesRead = 0;
        uint EntriesRead = 0;
        string[] fields = new string[LogConsts.LOG_FIELDS];
        uint Errors = 0;
        uint PreviousErrors = 0;	// cumulative errors from prior CheckForNewData() calls
		bool EntryError;            // indicates error in current entry
        string Descriptor;	        // May be file name
        bool Disposed = false;
        public int LogOpenIndex = -1;  // Category index for LOG_OPEN (>=0 if seen in log so far)
        public int LogCloseIndex = -1; // Category index for LOG_CLOSE
        public int LogSyncIndex = -1;   // Category index for LOG_SYNC

        private UInt64[] EntryCounts = new UInt64[4];   // count of entries by severity

        public UInt64 InfoEntryCount
        {
            get { return EntryCounts[(int)SeverityCode.Info]; }
        }

        public UInt64 WarningEntryCount
        {
            get { return EntryCounts[(int)SeverityCode.Warning]; }
        }

        public UInt64 ErrorEntryCount
        {
            get { return EntryCounts[(int)SeverityCode.Error] + Errors; }
            // File reading errors are included in error count
        }

        public UInt64 FailureEntryCount
        {
            get { return EntryCounts[(int)SeverityCode.Failure]; }
        }

        public bool UsePropagatedSeverity = true;
        public bool ShowTimeStamps = false;
        public SeverityCode VisibleSeverityThreshold = SeverityCode.Info;
        public int AutoExpandThreshold = (int)SeverityCode.Warning;
        
        protected bool NewDataAvailable = false;
        protected long OldLength = 0;

        public LogReader()
        {
            //DataTable dTable = ParseCSV(@"C:\Hexplain.txt");
            //string item = "2012-08-14 05:11:45:568Z";  // test time string
            //DateTime dtVal = LogEntry.ConvertTimeStamp(item, DateTimeKind.Local);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !Disposed)
            {
                // dispose managed resources here
                if (Entries != null)
                {
                    Entries.Clear();
                    Entries = null;
                }

                if (Categories != null)
                {
                    Categories.Clear();
                    Categories = null;
                }

                if (CategoryIsVisible != null)
                {
                    //VisibleCategories.Clear();
                    Array.Resize<bool>(ref CategoryIsVisible, 0);
                    CategoryIsVisible = null;
                }

                if (sr != null)
                {
                    sr.Dispose();
                    sr = null;
                }

                tv = null;  // we are not the owner, not ours to handle

                Disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void OpenLogFile(string FileName)
        {
            try
            {
                FileStream fs = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Write);
                NewSource(new StreamReader(fs), FileName);
            }
            catch (IOException e)
            {
                MessageBox.Show(String.Format("Unable to open file.\n{0}", e.ToString()));
            }
        }

        public void NewSource(StreamReader srNew, string sDescription)
        {
            this.Descriptor = sDescription;
            sr = srNew;
            Errors = 0;
            PreviousErrors = 0;
			EntriesRead = 0;
            LinesRead = 0;
            PrepareForNewEntry();
            GetNewData();
        }

        int CategoryIndex(string s)
        {
            // TODO: make case-insensitive
            int Result = Categories.IndexOf(s);
            if (Result < 0)
            {
                // new category
                Categories.Add(s);
                Result = Categories.IndexOf(s);
                //VisibleCategories.Length = Categories.Count;
                //VisibleCategories.Add(Result);  // new categories are visible by default
                Array.Resize<bool>(ref CategoryIsVisible, Categories.Count);
                CategoryIsVisible[Result] = true; // new categories are visible by default

                // cache indices of LOG_* categories
                if ((LogOpenIndex < 0) && (s.CompareTo("LOG_OPEN") == 0))
                    LogOpenIndex = Result;
                if ((LogCloseIndex < 0) && (s.CompareTo("LOG_CLOSE") == 0))
                    LogCloseIndex = Result;
                if ((LogSyncIndex < 0) && (s.CompareTo("LOG_SYNC") == 0))
                    LogSyncIndex = Result;
            }
            return Result;
        }

        public string CategoryString(int index)
        {
            return Categories[index];
        }


        public string NodeCaption(LogEntry e)
        {
            string Caption = "NULL LogEntry value";
            if (e != null)
            {
                StringBuilder sb = new StringBuilder();
                if (ShowTimeStamps)
                    sb.AppendFormat("{0}: ",e.EventTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"));
                sb.AppendFormat("{0}: ", CategoryString(e.CategoryIndex));
                sb.Append(e.Msg1);
                if (e.Msg2.Length > 0)
                {
                    sb.AppendFormat(", {0}", e.Msg2);
                }
                // Escape embedded carriage returns and linefeeds, or Mono's TreeView will wrap and center the text
                sb.Replace("\n", "\\n");
                sb.Replace("\r", "\\r");
                Caption = sb.ToString();
            }
            return Caption;
        }

        public void RebuildTree()
        {
            // This routine is called when severity propagation or filtering changes potentially affect node visibility,
            // or when auto-expansion flags change.
            if (tv != null)
            {
                foreach (LogEntry e in Entries)
                {
                    e.Node = null;
                }

                tv.Nodes.Clear();

                int index = 0;
                while (index < Entries.Count)
                {
                    CreateNodeForVisibleEntry(index);
                    index++;
                }
            }
        }

        public void RefreshTree()
        {
            // This routine called to update captions
            foreach (LogEntry e in Entries)
            {
                if (e.Node != null)
                {
                    e.Node.Text = NodeCaption(e);
                    if (e.WasExpanded || (int)e.DescendantSeverity >= AutoExpandThreshold)
                        e.Node.Expand();
                }
            }
        }


        public bool EntryShouldBeVisible(LogEntry e)
        {
            return (e != null &&
                e.EffectiveSeverity(UsePropagatedSeverity) >= VisibleSeverityThreshold &&
                //VisibleCategories.IndexOf(e.CategoryIndex) >= 0);
                CategoryIsVisible[e.CategoryIndex]);
        }

        public void CreateNodeForVisibleEntry(int index)
        {
            // Add a new tree node, if entry should be visible
            LogEntry e = Entries[index];
            LogEntry parentEntry = Entries.ParentEntry(index);
            TreeNode nodeVisibleParent = null;
            string caption = NodeCaption(e);
            int imageindex = (int)e.EffectiveSeverity(UsePropagatedSeverity);

            if (tv != null)
            {
                if (parentEntry != null && EntryShouldBeVisible(parentEntry))
                    nodeVisibleParent = parentEntry.Node;

                if (EntryShouldBeVisible(e))
                {
                    // Node is visible under current settings
                    if (nodeVisibleParent != null)
                    {
                        // both node and parent are visible.
                        e.Node = nodeVisibleParent.Nodes.Add(caption);

                        // Auto-expand parent node, as appropriate
                        if ((int)parentEntry.DescendantSeverity >= AutoExpandThreshold)
                            nodeVisibleParent.Expand();
                    }
                    else
                    {
                        // Node has no visible parent
                        e.Node = tv.Nodes.Add(caption);
                    }
                    e.Node.ImageIndex = imageindex;
                    e.Node.SelectedImageIndex = imageindex;
                    e.Node.Tag = e;
                }
            }
        }

        void PrepareForNewEntry()
        {
            int i = 0;
            while (i < LogConsts.LOG_FIELDS)
                fields[i++] = "";
            EntryError = false;
            FieldData = "";
            ps = ParserState.EatingLeadingSpaces;
            fi = FieldIndex.OffsetToParent;
        }

        void RecordError(string ErrorMsg, ParserState RecoveryState)
        {
            if (!EntryError)
            {
                EntryError = true;
                Errors++;
                fields[(int)FieldIndex.Msg2] = ErrorMsg;
                fields[(int)FieldIndex.Severity] = SeverityCode.Failure.ToString();
                ps = RecoveryState;
            }
        }

        void HandleLF()
        {
            // Handles LF in log input stream
            LinesRead++;
            switch (ps)
            {
                case ParserState.QuotedField:
                    FieldData += '\n';
                    break;
                case ParserState.NewQuote:
                case ParserState.EatingLeadingSpaces:
                case ParserState.UnquotedField:
                case ParserState.IgnoringToComma:
                case ParserState.IgnoringToEOL:
                    {
                        LogEntry entry = new LogEntry();
                        EntriesRead++;
                        if (fi != FieldIndex.Msg2)
                            RecordError(string.Format("Entry {0} (Line {1}) did not have {2} fields", EntriesRead,
                                LinesRead, LogConsts.LOG_FIELDS), ParserState.IgnoringToEOL);

                        if (!EntryError)
                            fields[(int)FieldIndex.Msg2] = FieldData;

                        // Transfer data from fields[] to entry
                        entry.OffsetFromParent = entry.UIntFromString(fields[(int)FieldIndex.OffsetToParent]);
                        entry.Ticks = entry.UIntFromString(fields[(int)FieldIndex.TickCount]);
                        entry.Severity = entry.SeverityFromString(fields[(int)FieldIndex.Severity]);
                        entry.CategoryIndex = CategoryIndex(fields[(int)FieldIndex.Category]);
                        entry.Msg1 = fields[(int)FieldIndex.Msg1];
                        entry.Msg2 = fields[(int)FieldIndex.Msg2];

                        this.EntryCounts[(int)entry.Severity]++;

                        Entries.AddEntry(entry, this);

                        PrepareForNewEntry();
                    }
                    break;
            }
        }

        void HandleSpace(char ch)
        {
            // Handles whitespace in log input stream
            switch (ps)
            {
                case ParserState.QuotedField:
                    FieldData += ch;
                    break;
                case ParserState.NewQuote:
                case ParserState.UnquotedField:
                    ps = ParserState.IgnoringToComma;
                    break;
                case ParserState.EatingLeadingSpaces:
                case ParserState.IgnoringToComma:
                case ParserState.IgnoringToEOL:
                    break;
            }
        }

        void HandleQuote()
        {
            // Handles quote in log input stream
            switch (ps)
            {
                case ParserState.QuotedField:
                    ps = ParserState.NewQuote;
                    break;
                case ParserState.NewQuote:
                    FieldData += '"';   // escaped quote
                    ps = ParserState.QuotedField;
                    break;
                case ParserState.UnquotedField:
                    FieldData += '"';   // escaped quote
                    break;
                case ParserState.EatingLeadingSpaces:
                    ps = ParserState.QuotedField;
                    break;
                case ParserState.IgnoringToComma:
                case ParserState.IgnoringToEOL:
                    break;
            }
        }

        void HandleComma()
        {
            // Handles comma in log input stream
            switch (ps)
            {
                case ParserState.QuotedField:
                    FieldData += ',';
                    break;
                case ParserState.NewQuote:
                case ParserState.EatingLeadingSpaces:
                case ParserState.UnquotedField:
                case ParserState.IgnoringToComma:
                    switch (fi)
                    {
                        case FieldIndex.OffsetToParent:
                        case FieldIndex.TickCount:
                        case FieldIndex.Severity:
                        case FieldIndex.Category:
                        case FieldIndex.Msg1:
                            fields[(int)fi] = FieldData;
                            fi++;
                            ps = ParserState.EatingLeadingSpaces;
                            break;
                        case FieldIndex.Msg2:
                            RecordError(string.Format("Entry {0} (Line {1}) has more than {2} fields", EntriesRead + 1, LinesRead + 1,
                                LogConsts.LOG_FIELDS), ParserState.IgnoringToEOL);
                            break;
                    }
                    FieldData = "";
                    break;
                case ParserState.IgnoringToEOL:
                    break;
            }
        }

        void HandleRest(char ch)
        {
            switch (ps)
            {
                case ParserState.QuotedField:
                case ParserState.UnquotedField:
                    FieldData += ch;
                    break;
                case ParserState.NewQuote:
                    ps = ParserState.QuotedField;
                    break;
                case ParserState.EatingLeadingSpaces:
                    ps = ParserState.UnquotedField;
                    FieldData += ch;
                    break;
                case ParserState.IgnoringToComma:
                case ParserState.IgnoringToEOL:
                    RecordError(string.Format("Some portion of Entry {0}, (Field \"{1}\", Line {2}) was ignored", EntriesRead + 1, fi, LinesRead + 1), ps);
                    break;
            }
        }

        public bool NewDataIsAvailable()
        {
            if (sr != null)
            {
                long NewLength = sr.BaseStream.Length;
                if (NewLength != OldLength)
                {
                    NewDataAvailable = true;
                    OldLength = NewLength;
                }
            }
            return NewDataAvailable;
        }

        public void GetNewData()
        {
            int index = 0;
            string Data = "";
            char ch;

			PreviousErrors = Errors;
			
            if (tv != null)
            {
                //Application.UseWaitCursor = true;
                //Application.DoEvents();
                tv.BeginUpdate();
            }

            try
            {
                if (sr != null)
                {
                    Data = sr.ReadToEnd();  // This reads entire stream into memory.  Depending on max file size, this may not be a good approach.
                }

                while (index < Data.Length)
                {
                    ch = Data[index++];
                    switch (ch)
                    {
                        case ' ':
                        case '\t':
                        case '\r':
                            HandleSpace(ch);
                            break;
                        case '\n':
                            HandleLF();
                            break;
                        case '"':
                            HandleQuote();
                            break;
                        case ',':
                            HandleComma();
                            break;
                        default:
                            HandleRest(ch);
                            break;
                    }

                }
                NewDataAvailable = false;
                OldLength = sr.BaseStream.Length;
            }
            finally
            {
                if (tv != null)
                {
                    tv.EndUpdate();
                    //Application.UseWaitCursor = false;
                }
            }
			
			uint NewErrors = Errors - PreviousErrors;
			if (NewErrors > 0)
            {
                string Msg = (PreviousErrors == 0) ?
                    String.Format("Entries with errors: {0}", NewErrors) :
                    String.Format("Recent entries with errors: {0}", NewErrors);
			    MessageBox.Show(Msg, "Error Alert: " + this.Descriptor);
			}
        }

        /*
        public static DataTable ParseCSV(string path)
        {
            if (!File.Exists(path))
                return null;

            string full = Path.GetFullPath(path);
            string file = Path.GetFileName(full);
            string dir = Path.GetDirectoryName(full);

            //create the "database" connection string 
            string connString = "Provider=Microsoft.Jet.OLEDB.4.0;"
              + "Data Source=\"" + dir + "\\\";"
              + "Extended Properties=\"text;HDR=Yes;FMT=Delimited\"";

            //string connString =@"Driver={Microsoft Text Driver (*.txt; *.csv)};Dbq=c:\;Extensions=asc,csv,tab,txt;";

            //create the database query
            string query = "SELECT * FROM " + file;

            //create a DataTable to hold the query results
            DataTable dTable = new DataTable();

            //create an OleDbDataAdapter to execute the query
            OleDbDataAdapter dAdapter = new OleDbDataAdapter(query, connString);
            //OdbcDataAdapter dAdapter = new OdbcDataAdapter(query, connString);

            try
            {
                //fill the DataTable
                dAdapter.Fill(dTable);
            }
            catch (InvalidOperationException )
            { }

            dAdapter.Dispose();

            return dTable;
        }
        */
    }
}
