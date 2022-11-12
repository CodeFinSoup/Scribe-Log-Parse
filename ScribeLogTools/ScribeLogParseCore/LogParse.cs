using System;
using System.Collections.Generic;
using System.IO;

namespace ScribeLogTools.Core
{
    #region Log struct
    /// <summary>
    /// Represents a single Scribe Log entry as a deserialized object
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

        /// <summary>
        /// Returns a delimited string representation of of the log.  
        /// </summary>
        /// <param name="delimiter">The delimiter to separate the properties by. Default: \t</param>
        /// <returns>Timestamp, Severity, Title, Win32_ThreadID, and Message in that order.</returns>
        public string GetString(string delimiter = "\t")
        {
            return
                Timestamp.ToString() + delimiter +
                this.Severity.ToString() + delimiter +
                Title + delimiter +
                Win32_ThreadID.ToString() + delimiter + Message;
        }

        /// <summary>
        /// Returns a delimited string representation of of the log.
        /// Replaces the Carriage Return New Line in the Messaage with another string.
        /// Intended to be used if you want the entire message on a single line.
        /// </summary>
        /// <param name="replacestring">This string will replace all instances of \r\n in the Message before returning the result</param>
        /// <param name="delimiter">The delimiter to separate the properties by. Default: \t</param>
        /// <returns>Timestamp, Severity, Title, Win32_ThreadID, and Message in that order.</returns>
        public string GetString(string replacestring, string delimiter = "\t")
        {
            return
                Timestamp.ToString() + delimiter +
                this.Severity.ToString() + delimiter +
                Title + delimiter +
                Win32_ThreadID.ToString() + delimiter +
                Message.Replace("\r\n", replacestring);
        }

        public static bool operator ==(Log a, Log b)
        {
            if (a == null || b == null) return false;
            if (ReferenceEquals(a, b)) return true;
            if (a.GetHashCode() == b.GetHashCode()) return true;
            return false;
        }

        public static bool operator !=(Log a, Log b)
        {
            if (a == null || b == null) return false; // equality compares against null should always return false;
            if (ReferenceEquals(a, b)) return false;
            if (a.GetHashCode() == b.GetHashCode()) return false;
            return true;
        }

        public override bool Equals(object that)
        {
            if (that == null) return false;
            if (ReferenceEquals(this, that)) return true;
            if (that.GetType() != typeof(Log)) return false;
            if (this.GetHashCode() == that.GetHashCode()) return true;
            return false;
        }

        public override int GetHashCode()
        {
            return this.GetString().GetHashCode();
        }
    }
    #endregion

    #region Severity enum
    /// <summary>
    /// Different possible severity levels of Logs:<para />
    /// Debug / Verbose<para />
    /// Info<para />
    /// Warning<para />
    /// Error<para />
    /// Trace<para />
    /// If a Severity does not match the above, it is assigned type Unknown by the parser.
    /// </summary>
    public enum Severity
    {
        // In the UI, the logs are called Debug
        // However when written to the logs, they are called Verbose
        // To help avoid confusion, referencing either Debug or Verbose
        // Should yield the same result
        Debug = 0,
        Verbose = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Trace = 4,
        Unknown = 5
    }
    #endregion

    #region ParseFile class
    // Number of hyphens used to delimit log entries
    // ----------------------------------------
    // Just for reference here

    /// <summary>
    /// Static class containing methods to parse Scribe Logs.
    /// </summary>
    public sealed class ParseFile
    {
        #region Helpers
        // TODO: Maybe make this configurable at runtime just in case they change it
        private const string Delimiter = "----------------------------------------";

        /// <summary>
        /// Read a string and figure out which Enum it belongs to.
        /// </summary>
        /// <param name="SeverityString"></param>
        /// <returns></returns>
        private static Severity SeverityParse(string SeverityString)
        {
            if (SeverityString.Equals("Debug")) return Severity.Debug; // No need for this statement. It's "Just in case" Tibco changes the logging format down the line.
            if (SeverityString.Equals("Verbose")) return Severity.Verbose;
            if (SeverityString.Contains("Info")) return Severity.Info;
            if (SeverityString.Contains("Warn")) return Severity.Warning;
            if (SeverityString.Equals("Error")) return Severity.Error;
            if (SeverityString.Equals("Trace")) return Severity.Trace;
            return Severity.Unknown;
        }

        /// <summary>
        /// Gets the field text in a log
        /// </summary>
        /// <param name="line">Line read from scribe log.</param>
        /// <returns>Everything after the first ':' char and trims whitespace.</returns>
        private static string GetFieldData(string line)
        {
            return line.Substring(line.IndexOf(':') + 1).Trim(' ');
        }
        // No need to create this class as an object
        private ParseFile() { }
        #endregion

        #region Parser
        /// <summary>
        /// This method parses a log and returns an array of Log objects
        /// </summary>
        /// <param name="filename">The full path to the log file you wish to parse.</param>
        /// <param name="Logs">Array of parsed Log objects from the file</param>
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
                        if (line is null) continue; // Line must not be null to process anything
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
                        else if ((SkipToNext && DelimiterRead) || !DelimiterRead || line.Trim().Equals("")) continue;
                        else
                        {
                            try
                            {
                                // Log text is always written in the same order making this easy to parse.
                                // Add values to the CurrentLog object to be added to the LogList later.
                                switch (CurrentLog.ItemsWritten)
                                {
                                    case 0:
                                        CurrentLog.Timestamp = DateTime.Parse(GetFieldData(line));
                                        CurrentLog.ItemsWritten++;
                                        break;
                                    case 1:
                                        CurrentLog.Severity = SeverityParse(GetFieldData(line));
                                        CurrentLog.ItemsWritten++;
                                        break;
                                    case 2:
                                        CurrentLog.Title = GetFieldData(line);
                                        CurrentLog.ItemsWritten++;
                                        break;
                                    case 3:
                                        CurrentLog.Win32_ThreadID = Convert.ToInt32(GetFieldData(line));
                                        CurrentLog.ItemsWritten++;
                                        break;
                                    case 4:
                                        CurrentLog.Message = GetFieldData(line);
                                        CurrentLog.ItemsWritten++;
                                        break;
                                    case 5:
                                        // Any data appearing at this point is just a multiple line message
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
        #endregion
    }
    #endregion
}
