using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ScribeLogTools
{
    namespace Core
    {

        /// <summary>
        /// The logs as a deserialized object
        /// </summary>
        public struct Log
        {
            public DateTime Timestamp { get; internal set; }
            public Severity Severity { get; internal set; }
            public string Title { get; internal set; }
            public int Win32_ThreadID { get; internal set; }
            public string Message { get; internal set; }
            internal int ItemsWritten { get; set; }

            /// <summary>
            /// Sets default values in the struct
            /// </summary>
            internal void Clear()
            {
                Timestamp = DateTime.MinValue;
                Severity = Severity.Unknown;
                Title = "";
                Win32_ThreadID = 0;
                Message = "";
                ItemsWritten = 0;
            }

            public override string ToString()
            {
                return
                    Timestamp.ToString() + "\t" +
                    this.Severity.ToString() + "\t" +
                    Title + "\t" +
                    Win32_ThreadID.ToString() + "\t" + Message;
            }

            /// <summary>
            /// Replaces the Carriage Return New Line in the Messaage with another string.
            /// Intended to be used if you want the entire message on a single line.
            /// </summary>
            /// <param name="replacestring"></param>
            /// <returns></returns>
            public string ToString(string replacestring)
            {
                return
                    Timestamp.ToString() + "\t" +
                    this.Severity.ToString() + "\t" +
                    Title + "\t" +
                    Win32_ThreadID.ToString() + "\t" +
                    Message.Replace("\r\n", replacestring);
            }
        }

        public enum Severity
        {
            Debug = 0,
            Verbose = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Trace = 4,
            Unknown = 5
        }

        // Number of hyphens used to delimit log entries
        // ----------------------------------------
        // Just for reference here

        public class ParseFile
        {
            // TODO: Maybe make this configurable at runtime just in case they change it
            private const string Delimiter = "----------------------------------------";

            /// <summary>
            /// Read a string and figure out which Enum it belongs to.
            /// </summary>
            /// <param name="SeverityString"></param>
            /// <returns></returns>
            private static Severity SeverityParse(string SeverityString)
            {
                if (SeverityString.Equals("Debug")) return Severity.Debug;
                if (SeverityString.Equals("Verbose")) return Severity.Verbose;
                if (SeverityString.Contains("Info")) return Severity.Info;
                if (SeverityString.Contains("Warn")) return Severity.Warning;
                if (SeverityString.Equals("Error")) return Severity.Error;
                if (SeverityString.Equals("Trace")) return Severity.Trace;
                return Severity.Unknown;
            }

            /// <summary>
            /// This method parses a log and returns an array of Log objects
            /// </summary>
            /// <param name="filename"></param>
            /// <param name="Logs"></param>
            public static void ParseLogFile(string filename, out Log[] Logs)
            {
                // This is what we will ultimately return through the out argument.
                var LogList = new List<Log>();

                try
                {
                    // Begin reading logs based on the specified file name
                    using (StreamReader sr = new StreamReader(filename))
                    {
                        Log CurrentLog = new Log();
                        CurrentLog.Clear();
                        bool DelimiterRead = false;
                        while (!sr.EndOfStream)
                        {
                            bool SkipToNext = false;
                            string line = sr.ReadLine();
                            if (line == null) continue; // Line must not be null to process anything
                            else if (line.Equals(Delimiter)) // check to see if we hit a delimiter
                            {
                                // Check to se if we already hit a delimiter. 
                                // If we did, that is the end of this log.
                                // We should add it to the list and start over.
                                if (DelimiterRead)
                                {
                                    if (!SkipToNext) LogList.Add(CurrentLog);
                                    CurrentLog.Clear();
                                    DelimiterRead = false;
                                    SkipToNext = false;
                                    continue;
                                }
                                // if we have not yet seen a delimiter, then this must be the start of the log.
                                // set this bool to note that we are processing everything to the next delimiter
                                // as a single log
                                DelimiterRead = true;
                                continue;
                            }

                            // If an error has occurred or the line is blank, don't bother processing the current line.
                            else if ((SkipToNext && DelimiterRead) || !DelimiterRead || line.Equals("")) continue;
                            else
                            {
                                try
                                {
                                    // Log text is always written in the same order making this easy to parse.
                                    // Add values to the CurrentLog object to be added to the LogList later.
                                    switch (CurrentLog.ItemsWritten)
                                    {
                                        case 0:
                                            CurrentLog.Timestamp = DateTime.Parse(line.Substring(line.IndexOf(':') + 1).Trim(' '));
                                            CurrentLog.ItemsWritten++;
                                            break;
                                        case 1:
                                            CurrentLog.Severity = SeverityParse(line.Substring(line.IndexOf(':') + 1).Trim(' '));
                                            CurrentLog.ItemsWritten++;
                                            break;
                                        case 2:
                                            CurrentLog.Title = line.Substring(line.IndexOf(':') + 1).Trim(' ');
                                            CurrentLog.ItemsWritten++;
                                            break;
                                        case 3:
                                            CurrentLog.Win32_ThreadID = Convert.ToInt32(line.Substring(line.IndexOf(':') + 1).Trim(' '));
                                            CurrentLog.ItemsWritten++;
                                            break;
                                        case 4:
                                            CurrentLog.Message = line.Substring(line.IndexOf(':') + 1);
                                            CurrentLog.ItemsWritten++;
                                            break;
                                        case 5:
                                            CurrentLog.Message += ("\r\n" + line);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                catch
                                {
                                    // There's a chance something may fail trying to parse.
                                    // If it does, we just ignore it and keep reading to the next delimiter
                                    // This variable will be reset when processing a new log begins
                                    SkipToNext = true;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // TODO: Error Parsing Aborted.
                    // Write output somewhere.
                    Logs = new Log[] { };
                    return;
                }

                Logs = LogList.ToArray();
            }

            // No need to create this class as an object
            private ParseFile() { }
        }
    }
}
