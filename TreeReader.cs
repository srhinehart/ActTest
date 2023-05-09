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
using LogViewer;
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

    public class HourGlass : IDisposable
    {
        // Workaround for non-functional Application.UseWaitCursor
        public HourGlass()
        {
            Enabled = true;
        }
        public void Dispose()
        {
            Enabled = false;
        }
        public static bool Enabled
        {
            get { return Application.UseWaitCursor; }
            set
            {
                if (value == Application.UseWaitCursor) return;
                Application.UseWaitCursor = value;
                Form f = Form.ActiveForm;
                if (f != null && f.Handle != null)   // Send WM_SETCURSOR
                    SendMessage(f.Handle, 0x20, f.Handle, (IntPtr)1);
            }
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
    }

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

        // defaults for static Truncate() function
        private const int DefaultTruncThresh = 4000;

        public uint OffsetFromParent;
        public uint Ticks;
        public DateTime EventTime;  // local time
        public SeverityCode Severity;
        public string Msg1;
        public string Msg2;
        public int CategoryIndex;   // index to string list (in category appearance order)
        public TreeNode Node = null;
        public SeverityCode PropagatedSeverity; // reflects current worst-case severity of this node and all descendants
        public bool WasExpanded = false;   // retains manual node expansion status across tree rebuilds and refreshes 

        public SeverityCode EffectiveSeverity(bool UsePropagatedSeverity)
        {
            return UsePropagatedSeverity ? PropagatedSeverity : Severity;
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

        public static string Truncated(string msg, int TruncationThreshold = DefaultTruncThresh)
        {
            if (msg == null | msg.Length < TruncationThreshold)
                return msg;
            // MaxLines only comes into play if msg.Length reaches TruncationThreshold
            return msg.Substring(0, TruncationThreshold - 3) + "...";
        }

        public static string FirstNlines(string msg, int n)
        {
            if (msg == null)
                return msg;
            int LineCount = 0;
            StringBuilder sb = new StringBuilder();
            StringReader sr = new StringReader(msg);
            string line = null;
            while (LineCount++ < n)
            {
                line = sr.ReadLine();
                if (line == null)
                    break;
                sb.AppendLine(line);
            }
            if (line != null)
                sb.AppendLine("...");   // indicate string is not complete
            return sb.ToString();
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
            if ((e.CategoryIndex == r.LogOpenCatIdx)
                || (e.CategoryIndex == r.LogCloseCatIdx)
                || (e.CategoryIndex == r.LogSyncCatIdx))
            {
                Time0_Ticks = e.Ticks;
                Time0 = LogEntry.ConvertTimeStamp(e.Msg1, LogConsts.DefaultTimeKind_Input);
            }

            // Finish initalizing entry variables
            e.PropagatedSeverity = e.Severity;   // this new entry has no descendants yet
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
                    if (e.PropagatedSeverity < Severity)
                    {
                        e.PropagatedSeverity = Severity;    // new worst case, keep propagating severity upward
                        RebuildRequired = true; // propagating severity can change node visibility
                    }
                    else
                        break; // done - ancestor's PropagatedSeverity already same or worse than that of newest entry
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

        //public LogEntry ParentEntry(LogEntry child)
        //{
        //    int childindex = IndexOf(child);    // probably not super efficient
        //    return ParentEntry(childindex);
        //}

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
        // Re-implementation of LogReader that uses stand-alone TreeNode collection
        public static int NODE_BLOCK_SIZE = 4096;
        protected TreeNode[] RootNodes = null;
        protected int cntRootNodes = 0;
        protected int cntVisibleNodes = 0;

        public EntryList Entries = new EntryList();

        // Category items
        public List<String> Categories = new List<String>();    // in appearance order, new items added to end
        public bool[] CategoryIsVisible = new bool[0];          // in appearance order

        public int LogOpenCatIdx = -1;  // Category index for LOG_OPEN (>=0 if seen in log so far), in category appearance order
        public int LogCloseCatIdx = -1; // Category index for LOG_CLOSE
        public int LogSyncCatIdx = -1;  // Category index for LOG_SYNC

        public List<String> SortedCategories = new List<String>();  // must be updated after every category addition
        public List<int> AppearanceOrder = new List<int>();         // must be updated after every category addition
        // End Category variables

        public StreamReader sr = null;
        public TreeView tv = null;
        public frmLogViewer myOwner = null;    // for sending status bar messages
        protected StringBuilder FieldData = new StringBuilder("");
        protected ParserState ps = ParserState.EatingLeadingSpaces;
        protected FieldIndex fi = FieldIndex.OffsetToParent;
        protected uint LinesRead = 0;
        protected uint EntriesRead = 0;
        protected string[] fields = new string[LogConsts.LOG_FIELDS];
        protected uint Errors = 0;
        protected uint PreviousErrors = 0;	// cumulative errors from prior CheckForNewData() calls
		protected bool EntryError;            // indicates error in current entry
        protected string Descriptor;	        // May be file name
        protected bool Disposed = false;

        public bool UsePropagatedSeverity = true;
        public bool ShowTimeStamps = false;
        public SeverityCode VisibleSeverityThreshold = SeverityCode.Info;
        public int AutoExpandThreshold = (int)SeverityCode.Warning;
        
        protected bool NewDataAvailable = false;
        protected long OldLength = 0;
        protected bool ConsoleEchoFlag = false;

        public LogReader()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !Disposed)
            {
                // dispose managed resources here
                if (Entries != null)
                {
                    foreach (LogEntry e in Entries)
                        e.Node = null;    
                    Entries.Clear();
                    Entries = null;
                }

                if (RootNodes != null)
                {
                    Array.Resize<TreeNode>(ref RootNodes, 0);
                    RootNodes = null;
                }


                if (Categories != null)
                {
                    Categories.Clear();
                    Categories = null;
                }

                if (SortedCategories != null)
                {
                    SortedCategories.Clear();
                    SortedCategories = null;
                }

                if (AppearanceOrder != null)
                {
                    AppearanceOrder.Clear();
                    AppearanceOrder = null;
                }

                if (CategoryIsVisible != null)
                {
                    Array.Resize<bool>(ref CategoryIsVisible, 0);
                    CategoryIsVisible = null;
                }

                if (sr != null)
                {
                    sr.Dispose();
                    sr = null;
                }

                myOwner = null;
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
                FileStream fs = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
            // For initial read, disable treeview updates
            TreeView tvBackup = tv;
            tv = null;
            Console.WriteLine("Started reading {0}", sDescription);
            PrepareForNewEntry();
            uint tickStart = (uint)Environment.TickCount;
            GetNewData();
            uint tickDone = (uint)Environment.TickCount;
            Console.WriteLine("File read time: {0} ms", tickDone - tickStart);
            tv = tvBackup;
            RebuildTree();
        }

        protected int AddCategory(int SortedIndex, string s)
        {
            // Assumes s not already in Categories, and belongs at SortedIndex
            int AppearanceIndex = Categories.Count;
            Categories.Add(s);
            SortedCategories.Insert(SortedIndex, s);
            AppearanceOrder.Insert(SortedIndex, AppearanceIndex);
            Array.Resize<bool>(ref CategoryIsVisible, Categories.Count);
            CategoryIsVisible[AppearanceIndex] = true; // new categories are visible by default

            // TODO: cap categories at 64K

            // cache indices of LOG_* categories
            if ((LogOpenCatIdx < 0) && (s.CompareTo("LOG_OPEN") == 0))
                LogOpenCatIdx = AppearanceIndex;
            if ((LogCloseCatIdx < 0) && (s.CompareTo("LOG_CLOSE") == 0))
                LogCloseCatIdx = AppearanceIndex;
            if ((LogSyncCatIdx < 0) && (s.CompareTo("LOG_SYNC") == 0))
                LogSyncCatIdx = AppearanceIndex;
            return SortedIndex;
        }

        protected int CategoryIndex(string s)
        {
            int SortedIndex = SortedCategories.BinarySearch(s);
            if (SortedIndex < 0)
                SortedIndex = AddCategory(~SortedIndex, s);
            // SortedIndex is quick to find (with BinarySearch) but will change as new categories are added.
            // The caller needs the (unchanging) category appearance order 
            return AppearanceOrder[SortedIndex];
        }

        public string CategoryString(int index)
        {
            return Categories[index];   // in appearance order
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
                sb.Append(LogEntry.Truncated(e.Msg1, 80));
                if (e.Msg2.Length > 0)
                {
                    sb.AppendFormat(", {0}", LogEntry.Truncated(e.Msg2, 120));
                }
                // Escape embedded carriage returns and linefeeds, or Mono's TreeView will wrap and center the text
                sb.Replace("\n", "\\n");
                sb.Replace("\r", "\\r");
                Caption = sb.ToString();
            }
            return Caption;
        }

        protected void UpdateStatusBar(string msg, bool SendToConsole)
        {
            if (myOwner != null)
                myOwner.SetStatusMessage(msg);
            if (SendToConsole)
                Console.WriteLine(msg);
        }

        protected bool EntryShouldBeVisible_Propagated(LogEntry e)
        {
            return (e.PropagatedSeverity >= VisibleSeverityThreshold &&
                CategoryIsVisible[e.CategoryIndex]);
        }

        protected void RebuildTreeLoop_Propagated()
        {
            int index = 0;
            TreeNode nodeVisibleParent = null;
            TreeNode node = null;
            LogEntry e;
            LogEntry parentEntry;

            while (index < Entries.Count)
            {
                // For each entry, check visibility, and add tree node if entry is visible.
                nodeVisibleParent = null;
                node = null;
                e = Entries[index];
                parentEntry = Entries.ParentEntry(index);
                
                if (parentEntry != null && EntryShouldBeVisible_Propagated(parentEntry))
                    nodeVisibleParent = parentEntry.Node;

                if (EntryShouldBeVisible_Propagated(e))
                {
                    // Node is visible under current settings
                    string caption = NodeCaption(e);
                    if (nodeVisibleParent != null)
                    {
                        // both node and parent are visible.
                        node = nodeVisibleParent.Nodes.Add(caption);

                        // Auto-expand parent node, as appropriate
                        if ((int)parentEntry.PropagatedSeverity >= AutoExpandThreshold)
                            nodeVisibleParent.Expand();
                    }
                    else
                    {
                        // Node has no visible parent
                        node = new TreeNode(caption); //tv.Nodes.Add(caption);
                        int NextIndex = cntRootNodes++;

                        if (cntRootNodes > RootNodes.Length)
                        {
                            // grow array
                            Array.Resize<TreeNode>(ref RootNodes, RootNodes.Length + NODE_BLOCK_SIZE);
                        }
                        RootNodes[NextIndex] = node;
                    }
                    cntVisibleNodes++;
                    e.Node = node;
                    int imageindex = (int)e.PropagatedSeverity;
                    node.ImageIndex = imageindex;
                    node.SelectedImageIndex = imageindex;
                    node.Tag = e;
                }
                 
                index++;
                if ((index & 0x3FFF) == 0)
                {
                    String msg = String.Format("Processed entries: {0}", index);
                    UpdateStatusBar(msg, ConsoleEchoFlag);
                }
            }

        }

        protected bool EntryShouldBeVisible_Native(LogEntry e)
        {
            return (e.Severity >= VisibleSeverityThreshold &&
                CategoryIsVisible[e.CategoryIndex]);
        }

        protected void RebuildTreeLoop_Native()
        {
            int index = 0;
            TreeNode nodeVisibleParent = null;
            TreeNode node = null;
            LogEntry e;
            LogEntry parentEntry;

            while (index < Entries.Count)
            {
                // For each entry, check visibility, and add tree node if entry is visible.
                nodeVisibleParent = null;
                node = null;
                e = Entries[index];
                parentEntry = Entries.ParentEntry(index);

                if (parentEntry != null && EntryShouldBeVisible_Native(parentEntry))
                    nodeVisibleParent = parentEntry.Node;

                if (EntryShouldBeVisible_Native(e))
                {
                    // Node is visible under current settings
                    string caption = NodeCaption(e);
                    if (nodeVisibleParent != null)
                    {
                        // both node and parent are visible.
                        node = nodeVisibleParent.Nodes.Add(caption);

                        // Auto-expand parent node, as appropriate
                        if ((int)e.Severity >= AutoExpandThreshold)
                            nodeVisibleParent.Expand();
                    }
                    else
                    {
                        // Node has no visible parent
                        node = new TreeNode(caption); //tv.Nodes.Add(caption);
                        int NextIndex = cntRootNodes++;

                        if (cntRootNodes > RootNodes.Length)
                        {
                            // grow array
                            Array.Resize<TreeNode>(ref RootNodes, RootNodes.Length + NODE_BLOCK_SIZE);
                        }
                        RootNodes[NextIndex] = node;
                    }
                    cntVisibleNodes++;
                    e.Node = node;
                    int imageindex = (int)e.Severity;
                    node.ImageIndex = imageindex;
                    node.SelectedImageIndex = imageindex;
                    node.Tag = e;
                }

                index++;
                if ((index & 0x3FFF) == 0)
                {
                    String msg = String.Format("Processed entries: {0}", index);
                    UpdateStatusBar(msg, ConsoleEchoFlag);
                }
            }
        }


        public virtual void RebuildTree()
        {
            // This routine is called when severity propagation or filtering changes potentially affect node visibility,
            // when auto-expansion flags change, or when ShowTimeStamps setting is updated.
            if (tv != null)
            {
                UpdateStatusBar("Building tree of visible nodes", ConsoleEchoFlag);
                uint tickStarted = (uint)Environment.TickCount;
                foreach (LogEntry e in Entries)
                {
                    e.Node = null;
                }

                RootNodes = new TreeNode[NODE_BLOCK_SIZE];
                cntRootNodes = 0;
                cntVisibleNodes = 0;

                if (UsePropagatedSeverity)
                    RebuildTreeLoop_Propagated();
                else
                    RebuildTreeLoop_Native();

                uint tickDone = (uint)Environment.TickCount;
                Console.WriteLine("Processed {0} entries in {1} ms", Entries.Count, tickDone - tickStarted);
                Console.WriteLine("Root Nodes: {0}, Visible Nodes: {1}", cntRootNodes, cntVisibleNodes);
                Array.Resize<TreeNode>(ref RootNodes, cntRootNodes);

                using (new HourGlass())
                {
                    String msg = String.Format("Loading {0} nodes into tree. Please wait.", cntVisibleNodes);
                    UpdateStatusBar(msg, ConsoleEchoFlag);
                    tickStarted = (uint)Environment.TickCount;
                    tv.Nodes.Clear();
                    tv.Nodes.AddRange(RootNodes);
                    tickDone = (uint)Environment.TickCount;
                    msg = String.Format("Processed {0} nodes in {1} ms", cntVisibleNodes, tickDone - tickStarted);
                    UpdateStatusBar(msg, ConsoleEchoFlag);
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

        public virtual void CreateNodeForVisibleEntry(int index)
        {
            // Handles events that arrive after initial reading of log.
            // Add a new tree node, if entry should be visible
            LogEntry e = Entries[index];
            TreeNode nodeVisibleParent = null;
            TreeNode node = null;
            LogEntry parentEntry = Entries.ParentEntry(index);

            if (tv != null)
            {
                if (parentEntry != null && EntryShouldBeVisible(parentEntry))
                    nodeVisibleParent = parentEntry.Node;

                if (EntryShouldBeVisible(e))
                {
                    // Node is visible under current settings
                    string caption = NodeCaption(e);
                    if (nodeVisibleParent != null)
                    {
                        // both node and parent are visible.
                        node = nodeVisibleParent.Nodes.Add(caption);

                        // Auto-expand parent node, as appropriate
                        if ((int)parentEntry.EffectiveSeverity(UsePropagatedSeverity) >= AutoExpandThreshold)
                            nodeVisibleParent.Expand();
                    }
                    else
                    {
                        // Node has no visible parent
                        node = new TreeNode(caption); //tv.Nodes.Add(caption);
                        int NextIndex = cntRootNodes++;

                        if (cntRootNodes > RootNodes.Length)
                        {
                            // grow array
                            Array.Resize<TreeNode>(ref RootNodes, RootNodes.Length + NODE_BLOCK_SIZE);
                        }
                        RootNodes[NextIndex] = node;
                    }
                    cntVisibleNodes++;
                    e.Node = node;
                    int imageindex = (int)e.EffectiveSeverity(UsePropagatedSeverity);
                    node.ImageIndex = imageindex;
                    node.SelectedImageIndex = imageindex;
                    node.Tag = e;
                }
            }
        }

        protected void PrepareForNewEntry()
        {
            int i = 0;
            while (i < LogConsts.LOG_FIELDS)
                fields[i++] = "";
            EntryError = false;
            FieldData = new StringBuilder("");
            ps = ParserState.EatingLeadingSpaces;
            fi = FieldIndex.OffsetToParent;
            if ((LinesRead & 0x7FF) == 0)
            {
                String msg = String.Format("Lines read: {0}", LinesRead);
                UpdateStatusBar(msg, ConsoleEchoFlag);
            }
        }

        protected void RecordError(string ErrorMsg, ParserState RecoveryState)
        {
            Console.WriteLine(ErrorMsg);
            if (!EntryError)
            {
                EntryError = true;
                Errors++;
                fields[(int)FieldIndex.Msg2] = ErrorMsg;
                fields[(int)FieldIndex.Severity] = SeverityCode.Failure.ToString();
                ps = RecoveryState;
            }
        }

        protected void HandleLF()
        {
            // Handles LF in log input stream
            LinesRead++;
            switch (ps)
            {
                case ParserState.QuotedField:
                    FieldData.Append('\n');
                    break;
                case ParserState.NewQuote:
                case ParserState.EatingLeadingSpaces:
                case ParserState.UnquotedField:
                case ParserState.IgnoringToComma:
                case ParserState.IgnoringToEOL:
                    {
                        LogEntry entry = new LogEntry();
                        if (fi != FieldIndex.Msg2)
                            RecordError(string.Format("Entry {0} (Line {1}) did not have {2} fields", EntriesRead,
                                LinesRead, LogConsts.LOG_FIELDS), ParserState.IgnoringToEOL);

                        if (!EntryError)
                            fields[(int)FieldIndex.Msg2] = FieldData.ToString();

                        // Transfer data from fields[] to entry
                        entry.OffsetFromParent = entry.UIntFromString(fields[(int)FieldIndex.OffsetToParent]);
                        entry.Ticks = entry.UIntFromString(fields[(int)FieldIndex.TickCount]);
                        entry.Severity = entry.SeverityFromString(fields[(int)FieldIndex.Severity]);
                        entry.CategoryIndex = CategoryIndex(fields[(int)FieldIndex.Category]);
                        entry.Msg1 = fields[(int)FieldIndex.Msg1];
                        entry.Msg2 = fields[(int)FieldIndex.Msg2];

                        Entries.AddEntry(entry, this);

                        PrepareForNewEntry();
                    }
                    break;
            }
        }

        protected void HandleSpace(char ch)
        {
            // Handles whitespace in log input stream
            switch (ps)
            {
                case ParserState.QuotedField:
                    FieldData.Append(ch);
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

        protected void HandleQuote()
        {
            // Handles quote in log input stream
            switch (ps)
            {
                case ParserState.QuotedField:
                    ps = ParserState.NewQuote;
                    break;
                case ParserState.NewQuote:
                    FieldData.Append('"');   // escaped quote
                    ps = ParserState.QuotedField;
                    break;
                case ParserState.UnquotedField:
                    FieldData.Append('"');   // escaped quote
                    break;
                case ParserState.EatingLeadingSpaces:
                    ps = ParserState.QuotedField;
                    break;
                case ParserState.IgnoringToComma:
                case ParserState.IgnoringToEOL:
                    break;
            }
        }

        protected void HandleComma()
        {
            // Handles comma in log input stream
            switch (ps)
            {
                case ParserState.QuotedField:
                    FieldData.Append(',');
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
                            fields[(int)fi] = FieldData.ToString();
                            fi++;
                            ps = ParserState.EatingLeadingSpaces;
                            break;
                        case FieldIndex.Msg2:
                            RecordError(string.Format("Entry {0} (Line {1}) has more than {2} fields", EntriesRead + 1, LinesRead + 1,
                                LogConsts.LOG_FIELDS), ParserState.IgnoringToEOL);
                            break;
                    }
                    FieldData = new StringBuilder("");
                    break;
                case ParserState.IgnoringToEOL:
                    break;
            }
        }

        protected void HandleRest(char ch)
        {
            switch (ps)
            {
                case ParserState.QuotedField:
                case ParserState.UnquotedField:
                    FieldData.Append(ch);
                    break;
                case ParserState.NewQuote:
                    ps = ParserState.QuotedField;
                    break;
                case ParserState.EatingLeadingSpaces:
                    ps = ParserState.UnquotedField;
                    FieldData.Append(ch);
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
                tv.BeginUpdate();
            }

            try
            {
                using (new HourGlass())
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
            }
            finally
            {
                if (tv != null)
                    tv.EndUpdate();
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
    }
}
