
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibLogging_10
{
    public class ActionProcessor<T> : IDisposable
    {

        #region DELEGATES

        public delegate void ProcessActionCallback(T action);

        #endregion // delegates

        #region CONSTRUCTORS

        #region PUBLIC

        public ActionProcessor(ProcessActionCallback callback, CancellationToken ct)
        {
            _callback = callback;
            _ct = ct;
            _ct.Register(CancellationTokenTriggered);
        }

        #endregion // public

        #endregion // constructors

        #region METHODS

        #region PUBLIC

        #region VIRTUAL

        public virtual void Dispose()
        {
            Dispose(true);
        }

        #endregion // virtual

        public void Initialize(string threadName = "ActionProcessor", bool backgroundThread = true)
        {
            _threadName = threadName;

            try
            {
                //_thread = new Thread(RunThread)
                //{
                //    IsBackground = backgroundThread,
                //    Name = threadName
                //};

                for (int i = 0; i < _thread.Length; i++)
                {
                    _thread[i] = new Thread(RunThread)
                    {
                        Name = $"{threadName}_Thread{i}",
                        IsBackground = backgroundThread
                    };

                }
            }
            catch
            {
                throw;
            }

            _intialized = true;
        }

        public void Run()
        {
            if (!_intialized) throw new Exception($"ActionProcessor Not Initialized!");
            if (_thread == null) throw new Exception($"Thread Not Intialized!");

            //_thread.Start();
            for (int i = 0; i < _thread.Length; i++)
            {
                _thread[i].Start();
            }
        }

        public void Enqueue(T action)
        {
            try
            {
                _actionQueue.Enqueue(action);
                _trigger.Set();
            }
            catch (ObjectDisposedException)
            {
                // catch gracefully
            }
        }

        #endregion // public

        #region PRIVATE

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                
                if (disposing)
                {
                    _disposed = true;
                    _trigger.Set();

                    for (int i = 0; i < _thread.Length; i++)
                    {
                        _thread[i].Join();
                    }
                    _trigger.Dispose();

                }

            }
        }

        private void RunThread()
        {
            while (!_ct.IsCancellationRequested && !_disposed)
            {
                _trigger.WaitOne();
                while (_actionQueue.TryDequeue(out var action))
                {
                    if (_ct.IsCancellationRequested || _disposed)
                        return;

                    _callback?.Invoke(action);
                }
            }
        }

        private void CancellationTokenTriggered()
        {
            _trigger.Set();
        }

        #endregion // private

        #endregion // methods

        #region GET-SET

        #region PUBLIC

        //public T AddToActionQueue
        //{
        //    set
        //    {
        //        _actionQueue.Enqueue(value);
        //        _trigger.Set();
        //    }
        //}

        #endregion // public

        #endregion // get-set

        #region VARIABLES

        private Thread[] _thread = new Thread[1];

        private bool _intialized = false;

        private CancellationToken _ct;

        private readonly AutoResetEvent _trigger = new(false);

        private readonly ConcurrentQueue<T> _actionQueue = new();

        private volatile bool _disposed = false;

        private ProcessActionCallback _callback;

        private String _threadName = "";

        #endregion // variables

    }
}
