#define TRACE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Configuration;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace HostDetector
{
    public class HostDetectorSettings
    {
        public int absentPingInterval = 10000;
        public int presentPingInterval = 60000;
        public int presentPingRetries = 5;
        public int timeout = 1000;
        public int soundId = 1;
        public string address = "255.255.255.255";
        public void InitializeFromConfigFile()
        {
            var appSettings = ConfigurationManager.AppSettings;
            foreach (var key in appSettings.AllKeys) {
                if (key == "absentPingInterval") Int32.TryParse(appSettings[key], out this.absentPingInterval);
                if (key == "presentPingInterval") Int32.TryParse(appSettings[key], out this.presentPingInterval);
                if (key == "presentPingRetries") Int32.TryParse(appSettings[key], out this.presentPingRetries);
                if (key == "timeout") Int32.TryParse(appSettings[key], out this.timeout);
                if (key == "soundId") Int32.TryParse(appSettings[key], out this.soundId);
                if (key == "address") this.address = appSettings[key];
            }
        }
    }
    static class Program
    {
        static HostDetector detector;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool createdNew = false;
            Mutex mutex = null;
            try
            {
                mutex = new Mutex(true, "HostDetector", out createdNew);
            }
            catch {}
            if (mutex == null || !createdNew)
            {
                MessageBox.Show("HostDetector is already running", "HostDetector could not be started", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }
            else {
                try
                {
                    HostDetectorSettings settings = new HostDetectorSettings();
                    settings.InitializeFromConfigFile();
                    detector = new HostDetector(settings);
                    Thread newThread = new Thread(RunDetector);
                    newThread.IsBackground = true;
                    Monitor.Enter(detector); // GUI thread needs to set up callbacks
                    newThread.Start();
                    Application.Run(new FormMain(settings, detector));
                }
                finally
                {
                    mutex.Close();
                }
            }
        }
        private static void RunDetector()
        {
            detector.Detect();
        }
    }
    public class HostDetector
    {
        enum State { Present, Absent};
        State state = State.Absent;
        int absentCounter = 0;
        HostDetectorSettings settings;
        public HostDetector(HostDetectorSettings settings)
        {
            this.settings = settings;
            Trace.Listeners.Add(new TextWriterTraceListener("HostDetector.log"));
        }
        public event EventHandler NotifyPresent;
        public event EventHandler NotifyAbsent;
        private void StateChange(State newState)
        {
            state = newState;
            if(newState == State.Present && NotifyPresent != null)
            {
                NotifyPresent(this, EventArgs.Empty);
            }
            if (newState == State.Absent && NotifyAbsent != null)
            {
                NotifyAbsent(this, EventArgs.Empty);
            }
        }

        public void Detect()
        {
            Monitor.Enter(this); // Make sure GUI thread has set up callbacks.
            Monitor.Exit(this);
            while (true)
            {
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(settings.address, settings.timeout);
                Trace.WriteLine(String.Format("Entry: state={0}", state));
                if (state == State.Absent)
                {
                    if (reply.Status == IPStatus.Success){
                        StateChange(State.Present);
                    }
                    
                }
                else // present
                {
                    if(reply.Status == IPStatus.Success)
                    {
                        absentCounter=0;
                    }
                    else // IPStatus != success
                    {
                        absentCounter++;
                        if(absentCounter >= settings.presentPingRetries)
                        {
                            StateChange(State.Absent);
                            absentCounter = 0;
                        }
                    }
                }
                Trace.WriteLine(String.Format("Exit: state={0}, IPStatus={1}", state, reply.Status));
                Trace.Flush();
                Thread.Sleep(state == State.Present ? settings.presentPingInterval : settings.absentPingInterval);
            }
        }
    }
}
