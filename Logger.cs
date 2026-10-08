using LibLogging_10.Enums;
using LibUtil_10.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LibUtil_10.FileTools;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

namespace LibLogging_10
{

    //**//******************************************************************************//**//
    //**//******************************************************************************//**//
    //**//******************************************************************************//**//
    //**//                                                                              //**//
    //**//  Author:     Brie Weiss                                                      //**//
    //**//  Date:       unknown                                                         //**//
    //**//  Usage:      Spins up a logbook on its own thread, messages are added and    //**//
    //**//              processed in a FIFO order                                       //**//
    //**//  Revision:   v. 1 - first revision                                           //**//
    //**//              v. 2 (20251123) added the ability to create a logbook based on  //**//
    //**//              String name instead of being confined to what is available in   //**//
    //**//              The ELogBooks enumerator                                        //**//
    //**//              v. 3 implements Microsoft.Extensions.Logging.ILogger in place   //**//
    //**//              of LibUtil_10.Interfaces.ILogger                                //**//
    //**//                                                                              //**//
    //**//******************************************************************************//**//
    //**//******************************************************************************//**//
    //**//******************************************************************************//**//

    public class Logger : ILogger, IDisposable
    {

        #region VARIABLES

        #region PRIVATE

        //private Thread _loggingThread;

        private String _logbookName;

        private volatile bool _disposed = false;

        private String _logFileName;

        private ActionProcessor<LogMessage> _logProcessor;

        private FileStream _logStream;

        private Enums.ELogLevel _logLevel;


        private String _dirFilePath = "";

        private const int MAX_FILE_NUMBER = 200;

        private int _curFileLine = 0;
        private const int MAX_FILE_LINES = 100000;

        #endregion // private

        #endregion // variables

        #region CONSTRUCTORS

        #region PUBLIC

        public Logger(String logbookName, ELogLevel logLevel, CancellationToken ct, bool storeInWorkingDirectory = false)
        {
            InitializeParameters(logbookName.ToLower().Replace(" ", "_"), logLevel, ct, storeInWorkingDirectory);  
        }

        #endregion // public

        #endregion // constructors

        #region METHODS

        #region PUBLIC

        #region LOGGING_METHODS


        public void Exception(Exception ex, [CallerMemberName] String member = "", [CallerFilePath] String file = "", [CallerLineNumber] int line = 0)
        {
            LogMessage logMsg = new LogMessage("", ELogLevel.EXCEPTION, member, file, line, ex);
            _logProcessor.Enqueue(logMsg);
        }

        public void Info(String message, [CallerMemberName] String member = "", [CallerFilePath] String file = "", [CallerLineNumber] int line = 0)
        {
            LogMessage logMsg = new LogMessage(message, ELogLevel.INFO,member, file, line);
            _logProcessor.Enqueue(logMsg);
        }

        public void Error(String message, [CallerMemberName] String member = "", [CallerFilePath] String file = "", [CallerLineNumber] int line = 0)
        {
            Error(message, null, member, file, line);
        }

        public void Error(String message, Exception? ex, [CallerMemberName] String member = "", [CallerFilePath] String file = "", [CallerLineNumber] int line = 0)
        {
            LogMessage logMsg = new LogMessage(message, ELogLevel.ERROR, member, file, line, ex);
            _logProcessor.Enqueue(logMsg);
        }

        public void Warn(String message, [CallerMemberName] String member = "", [CallerFilePath] String file = "", [CallerLineNumber] int line = 0)
        {
            LogMessage logMsg = new LogMessage(message, ELogLevel.WARN, member, file, line);
            _logProcessor.Enqueue(logMsg);
        }

        public void Debug(String message, [CallerMemberName] String member = "", [CallerFilePath] String file = "", [CallerLineNumber] int line = 0)
        {
            LogMessage logMsg = new LogMessage(message, ELogLevel.DEBUG, member, file, line);
            _logProcessor.Enqueue(logMsg);
        }

        public void Verbose(String message, [CallerMemberName] String member = "", [CallerFilePath] String file = "", [CallerLineNumber] int line = 0)
        {
            LogMessage logMsg = new LogMessage(message, ELogLevel.VERBOSE, member, file, line);
            _logProcessor.Enqueue(logMsg);
        }

        #endregion // logging_methods

        #region ILOGGER

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            ArgumentNullException.ThrowIfNull(formatter);

            String message = formatter(state, exception);

            if (String.IsNullOrEmpty(message) && exception == null) return;

            String source = eventId == default ? _logbookName : $"{_logbookName}:{eventId}";

            LogMessage logMsg = new LogMessage(message, ToELogLevel(logLevel), source, exception);
            _logProcessor.Enqueue(logMsg);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None) return false;

            return ToELogLevel(logLevel) <= _logLevel;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            // Scopes are not supported by the logbook.
            return null;
        }

        #endregion // iLogger

        public void Dispose()
        {
            if (_disposed) return;

            if (_logProcessor != null)
            {
                _logProcessor.Dispose();
            }

            _disposed = true;

        }


        public void Run()
        {


            _logProcessor.Run();

        }


        #endregion // public

        #region PRIVATE

        private static ELogLevel ToELogLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => ELogLevel.VERBOSE,
                LogLevel.Debug => ELogLevel.DEBUG,
                LogLevel.Information => ELogLevel.INFO,
                LogLevel.Warning => ELogLevel.WARN,
                LogLevel.Error => ELogLevel.ERROR,
                LogLevel.Critical => ELogLevel.EXCEPTION,
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null),
            };
        }


        /// <summary>
        /// Takes parameters from the constructors and centralizes their managment and usage here.
        /// </summary>
        /// <param name="logBookName"></param>
        /// <param name="logLevel"></param>
        /// <param name="ct"></param>
        /// <param name="storeInWorkingDirectory"></param>
        private void InitializeParameters(String logBookName, ELogLevel logLevel, CancellationToken ct, bool storeInWorkingDirectory)
        {
            _logbookName = logBookName;

            _logFileName = logBookName;

            _logProcessor = new ActionProcessor<LogMessage>(LogOut, ct);


            _dirFilePath = "";

            if (storeInWorkingDirectory)
            {
                _dirFilePath = FilePaths.CurrentRunningDirectory;
            }
            else _dirFilePath = FilePaths.AppDataApplicationPath;



            //String filePath = Path.Combine(dirPath, $"{_logFileName}.log");

            if (File.Exists(Path.Combine(_dirFilePath, $"{_logFileName}_000.log")))
            {
                IncrementFiles(_dirFilePath, 1);
            }

            String filePath = Path.Combine(_dirFilePath, $"{_logFileName}_000.log");


            if (!Directory.Exists(_dirFilePath))
            {
                Directory.CreateDirectory(_dirFilePath);
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }


            _logStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite);

            _logLevel = logLevel;


            _logProcessor.Initialize($"{_logFileName}_Processor");

        }

        private void LogOut(LogMessage logMsg)
        {

            String logMes = logMsg.GetLogMessage;
            byte[] msgOut = System.Text.Encoding.ASCII.GetBytes($"{logMes}\n");
            Console.WriteLine(logMes);

            _curFileLine += logMsg.GetMessageLines;

            _logStream.Write(msgOut, 0, msgOut.Length);
            _logStream.Flush();

            if (_curFileLine >= MAX_FILE_LINES)
            {
                Console.WriteLine($"New File");

                _curFileLine = 0;
                _logStream.Close();

                IncrementFiles(_dirFilePath, 1);
                
                
                String filePath = Path.Combine(_dirFilePath, $"{_logFileName}_000.log");

                _logStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite);

            }

        }



        private void IncrementFiles(String rootFilePath, int currFile)
        {

            String fileNum = String.Format("{0:000}", currFile);

            String fileName = $"{_logFileName}_{fileNum}.log";
            if (File.Exists(Path.Combine(rootFilePath, fileName)))
            {
                if (currFile >= MAX_FILE_NUMBER)
                {


                    try
                    {
                        File.Delete(Path.Combine(rootFilePath, fileName));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                else
                {

                    
                    IncrementFiles(rootFilePath, currFile + 1);

                }

            }



            String prevFileName = $"{_logFileName}_{String.Format("{0:000}", currFile - 1)}.log";


            try
            {
                File.Copy(Path.Combine(rootFilePath, prevFileName), Path.Combine(rootFilePath, fileName));
                File.Delete(Path.Combine(rootFilePath, prevFileName));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }


        }

        private void CancellationTriggered()
        {

        }

        #endregion // private

        #endregion // methods

        #region GET-SET

        #region PUBLIC


        #endregion // public

        #endregion // get-set

        

    }
}
