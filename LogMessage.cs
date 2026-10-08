using LibLogging_10.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.CompilerServices;

namespace LibLogging_10
{
    public class LogMessage
    {

        #region CONSTRUCTORS

        #region PUBLIC

        public LogMessage(String message, Enums.ELogLevel logLevel, String member, String file, int line, Exception? ex = null)
        {
            _created = DateTime.Now;
            _message = message;
            _level = logLevel;
            _ex = ex;
            _member = member;
            _file = file;
            _line = line;
        }

        #endregion // public

        #endregion // constructors

        #region METHODS

        #endregion // methods

        #region GET-SET

        #region INTERNAL

        internal int GetMessageLines
        {
            get
            {
                if (string.IsNullOrEmpty(GetLogMessage))
                    return 0;

                return GetLogMessage.Count(c => c == '\n') + 1;
            }
        }

        internal String GetLogMessage
        {
            get
            {
                String message;
                if (_level == ELogLevel.EXCEPTION)
                {
                    message = $"{_ex}";
                    _level = ELogLevel.EXCEPTION;
                }
                else if (_level == ELogLevel.ERROR && _ex != null)
                {
                    message = $"{_message}: {_ex}";
                }
                else
                {
                    message = _message;
                }

                String timeStamp = String.Format("{0:00}-{1:00}-{2:00} {3:00}:{4:00}:{5:00}.{6:000}", _created.Year, _created.Month, _created.Day, _created.Hour, _created.Minute, _created.Second, _created.Millisecond);

                String fileName = "";
                try
                {
                    if (OperatingSystem.IsLinux())
                    {
                        fileName = Path.GetFileName(
                            _file.Replace('\\', '/')
                        );
                    }
                    else
                        fileName = Path.GetFileName(_file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Some Exception Bro: {ex.Message}");
                    fileName = _file;
                }

                String pathInfo = $"[{_member}:{fileName}:{_line}]";

                return $"{timeStamp} | ({(int)_level}) | {pathInfo} | {message}";


            }
        }

        #endregion // internal

        #endregion // get-set

        #region VARIABLES

        private String _message = "";
        private Exception? _ex;
        private ELogLevel _level;
        private DateTime _created = DateTime.MinValue;

        private String _member;
        private String _file;
        private int _line;

        #endregion // variables

    }
}
