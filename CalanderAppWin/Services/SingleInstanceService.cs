using System;
using System.Threading;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// Enforces a single running instance. Because the app autostarts (HKCU Run) and keeps its
    /// process alive behind a tray icon, a second manual launch would otherwise spawn a duplicate
    /// process — two tray icons, two widgets, and last-writer-wins races on the shared
    /// events.json / appsettings.json. The first instance owns a named mutex and listens on a
    /// named event; a later launch signals that event (so the running app can surface itself) and
    /// then exits.
    /// </summary>
    public sealed class SingleInstanceService : IDisposable
    {
        private const string MutexName = @"Global\NepaliCalendar.App.SingleInstance.Mutex";
        private const string EventName = @"Global\NepaliCalendar.App.SingleInstance.Activate";

        private Mutex? _mutex;
        private EventWaitHandle? _activateEvent;
        private RegisteredWaitHandle? _registeredWait;

        /// <summary>True if this process is the first/only instance.</summary>
        public bool IsFirstInstance { get; private set; }

        /// <summary>Acquires the instance lock. Returns false if another instance already owns it.</summary>
        public bool TryAcquire()
        {
            _mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
            IsFirstInstance = createdNew;
            return createdNew;
        }

        /// <summary>
        /// First instance only: starts listening for later launches. <paramref name="onActivate"/>
        /// fires on a background thread — marshal to the UI thread inside it.
        /// </summary>
        public void ListenForActivation(Action onActivate)
        {
            _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
            _registeredWait = ThreadPool.RegisterWaitForSingleObject(
                _activateEvent,
                (_, _) => onActivate(),
                state: null,
                millisecondsTimeOutInterval: Timeout.Infinite,
                executeOnlyOnce: false);
        }

        /// <summary>Second instance: wakes the already-running instance, if the event exists.</summary>
        public static void SignalExistingInstance()
        {
            try
            {
                if (EventWaitHandle.TryOpenExisting(EventName, out var handle))
                {
                    handle.Set();
                    handle.Dispose();
                }
            }
            catch
            {
                // If we can't signal, the second instance simply exits quietly.
            }
        }

        public void Dispose()
        {
            _registeredWait?.Unregister(null);
            _activateEvent?.Dispose();

            try
            {
                _mutex?.ReleaseMutex();
            }
            catch
            {
                // Not owned / already released — nothing to do.
            }

            _mutex?.Dispose();
        }
    }
}
