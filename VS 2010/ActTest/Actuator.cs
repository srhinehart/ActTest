using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using Fluxus;
using TreeLog;
using System.Windows.Forms;

namespace ActTest
{

    public enum ActStatus { Faulted = -2, Unknown = -1, LowerLimit = 0, UpperLimit = 1 };
    public enum OpType { Unknown, Command, Query, ParamSet };  // identifier terminator character meanings
 
    public class Actuator
    {
        protected SerialPort sp = null;
        protected string DataRead = "";
        protected string CmdPrompt = "Cmd> ";

        public bool Connected = false;
        protected uint? eidCommand = null;

        public static string StatusDescription(ActStatus status)
        {
            switch (status)
            {
                case ActStatus.Faulted:
                    return "Time out or switch fault";
                case ActStatus.LowerLimit:
                    return "At lower limit";
                case ActStatus.UpperLimit:
                    return "At upper limit";
                case ActStatus.Unknown:
                    return "Unknown position between limits";
                default:
                    return "Unrecognized status value";
            }
        }


        private string CalcCRC(string RawLine)
        {
            return string.Format("{0}${1:x4}", RawLine, Program.Settings.Crc16Ccitt(Encoding.ASCII.GetBytes(RawLine)));

        }

        private string FormatParamSetLine(string Name, string Value, bool UseCRC = true)
        {
            string RawLine = string.Format("{0}={1}", Name, Value);
            return UseCRC ? CalcCRC(RawLine) : RawLine;
        }

        private string FormatParamQueryLine(string Name, bool UseCRC = true)
        {
            string RawLine = string.Format("{0}?", Name);
            return UseCRC ? CalcCRC(RawLine) : RawLine;
        }

        public string AddCRC(string Cmd)
        {
            return CalcCRC(Cmd);
        }

        public string FormatUseCmdLine(string Name, bool UseCRC = true)
        {
            string RawLine = string.Format("{0}!", Name);
            return UseCRC ? CalcCRC(RawLine) : RawLine;
        }
        private OpResult VerifyResponse(uint? eidParent, OpType opType, string Response)
        {
            OpResult result = OpResult.Error;

            try
            {
                int index = Response.IndexOf("?Err");
                if (index < 0)
                {
                    // No error message
                    index = Response.IndexOf('$');
                    if (index > 0)
                    {
                        // must validate CRC if present
                        string Data = Response.Substring(0, index - 1);
                        byte[] payload = Encoding.ASCII.GetBytes(Data);
                        ushort calcCRC = Program.Settings.Crc16Ccitt(payload);
                        ushort dataCRC = Convert.ToUInt16(Response.Substring(index + 1), 16);
                        if (calcCRC == dataCRC)
                        {
                            result = OpResult.Success;
                            Response = Data;
                        }
                        else
                            Program.LogWrap.LogEntry(eidParent, SeverityCode.Error, LogCat.Response, "CRC mismatch",
                                 string.Format("calc CRC 0x{0:x2} != provided CRC 0x{1:x2}", calcCRC, dataCRC));
                    }
                    else
                        result = OpResult.Success;  // if no CRC, assume data is good
                }
                if (result == OpResult.Success)
                {
                    Program.LogWrap.LogEntry(eidParent, SeverityCode.Info, LogCat.Response, "Response value", Response);
                }
            }
            catch (Exception e)
            {
                Program.LogWrap.LogEntry(eidParent, SeverityCode.Failure, LogCat.Response, "Exception while verifying response", e.ToString());
            }

            Program.LogWrap.LogEntry(eidParent, result == OpResult.Success ? SeverityCode.Info : SeverityCode.Error, LogCat.Response, "Response verification", result.ToString());

            return result;
        }

        public static OpType GetOpType(string OpLine)
        {
            if (OpLine.IndexOf('?') == OpLine.Length - 1)
                return OpType.Query;
            if (OpLine.IndexOf('!') == OpLine.Length - 1)
                return OpType.Command;
            if (OpLine.IndexOf('=') > 0)
                return OpType.ParamSet;
            return OpType.Unknown;
        }

        public void AssignSerialPort(SerialPort port)
        {
            sp = port;
            if (sp != null)
            {
                sp.Encoding = Encoding.ASCII;
                sp.NewLine = "\r";  // Used only for writing, not reading
                sp.WriteLine("\0x1B");  // ESC to purge any partial command line
                Thread.Sleep(100);
                sp.ReadExisting();
            }
            else
                Connected = false;
        }


        public OpResult SendCommand(uint? eidParent, OpType opType, string Command, out string Response, string Purpose)
        {
            // Command is made from Unicode chars, but SerialPort.Encoding setting auto-converts to ASCII bytes before sending to device.
            eidCommand = null;  // clear eid from previous command, in case an exception occurs before new eid is returned.
            eidCommand = Program.LogWrap.LogEntry(eidParent, LogCat.Command, Purpose, Command);
            OpResult result = OpResult.Error;
            Response = null;
            int index = 0;
            if (Command != null)
            {
                if (sp != null)
                {
                    // auto flush any stale data
                    if (sp.BytesToRead > 0)
                        DataRead += sp.ReadExisting();
                    DataRead = "";
                    sp.WriteLine(Command);
                    result = OpResult.TimedOut; // presume the worst
                    uint tickStarted = (uint)Environment.TickCount;
                    while (ElapsedTicks(tickStarted) < 1000)
                    {
                        if (sp.BytesToRead > 0)
                            DataRead += sp.ReadExisting();
                        // Look for command prompt that marks end of response
                        index = DataRead.IndexOf(CmdPrompt);
                        if (index >= 0)
                        {
                            Response = DataRead.Substring(0, index);
                            DataRead = DataRead.Substring(index);
                            Program.LogWrap.LogEntry(eidCommand, LogCat.Response, "Raw Response", Response);
                            result = VerifyResponse(eidCommand, opType, Response);
                            break;
                        }
                        //Application.DoEvents();
                        Thread.Sleep(1);
                    }
                }
            }
            if (result != OpResult.Success)
                Program.LogWrap.LogEntry(eidCommand, SeverityCode.Error, LogCat.Status, "Bad OpResult", result.ToString());
            if (result == OpResult.TimedOut || result == OpResult.Unknown)
                Connected = false;
            return result;
        }

        public OpResult VerifyConnection(uint? eidParent)
        {
            string Response;
            string Expectation = "Fluxus Fluidics Head Controller";
            string OpLine = FormatParamQueryLine("ver");
            OpResult result = SendCommand(eidParent, OpType.Query, OpLine, out Response, "Verifying actuator connection");
            if (result == OpResult.Success)
            {
                // Verify 
                if (Response == null || Response.IndexOf(Expectation) < 0)
                {
                    result = OpResult.Error;
                    Program.LogWrap.LogEntry(eidParent, SeverityCode.Error, LogCat.Response, "Response value mismatch",
                        string.Format("Expected {0}, Received {1}", Expectation, Response));
                }
            }
            Connected = (result == OpResult.Success);
            return result;
        }

        public static uint ElapsedTicks(uint start)
        {
            return ((uint)Environment.TickCount) - start;
        }


        public OpResult MoveToUpperLimit()
        {
            string Line = FormatParamSetLine("targ", "1");
            string Response = null;
            return SendCommand(null, OpType.ParamSet, Line, out Response, "Moving to upper limit");
        }

        public OpResult MoveToLowerLimit()
        {
            string Line = FormatParamSetLine("targ", "0");
            string Response = null;
            return SendCommand(null, OpType.ParamSet, Line, out Response, "Moving to lower limit");
        }

        public OpResult QueryError(out string Response)
        {
            OpResult result = OpResult.Error;
            string Line = FormatParamQueryLine("error");
            Response = null;
            try
            {
                result = SendCommand(null, OpType.Query, Line, out Response, "Querying last error info");
            }
            catch (Exception e)
            {
                Program.LogWrap.LogEntry(eidCommand, SeverityCode.Failure, LogCat.Response, "Exception while processing error info", e.ToString());
            }
            return result;
        }

        public OpResult QueryStatus(out ActStatus Status)
        {
            Status = ActStatus.Faulted; // worst case
            string Line = FormatParamQueryLine("pos");
            string Response = null;
            OpResult result = OpResult.Error;
            int val = (int)Status;

            try
            {
                result = SendCommand(null, OpType.Query, Line, out Response, "Querying actuator status"); 
                if (result == OpResult.Success)
                {
                    val = Convert.ToInt32(Response);
                    Status = (ActStatus) val;
                    Program.LogWrap.LogEntry(eidCommand, SeverityCode.Info, LogCat.Response, "Response translation", Status.ToString());
                }
            }
            catch (Exception e)
            {
                Program.LogWrap.LogEntry(eidCommand, SeverityCode.Failure, LogCat.Response, "Exception while processing status response", e.ToString());
            }

            return result;
        }

    }
}
