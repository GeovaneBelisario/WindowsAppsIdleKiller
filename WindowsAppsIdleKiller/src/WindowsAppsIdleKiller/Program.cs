using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Timers;

namespace WindowsAppsIdleKiller
{
    class Program
    {
        static Timer _timer = new Timer();
        static List<WindowsApplication> _processesApplication;

        static void Main(string[] args)
        {
            _timer.Elapsed += new ElapsedEventHandler((object source, ElapsedEventArgs e) => ExecuteSyncThread());
            _timer.Interval = 1000 * 60 * 5;
            _timer.Enabled = true;

            _processesApplication = new List<WindowsApplication>();

            ExecuteSyncThread();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
        }

        static System.Threading.Thread _thread;
        private static System.Threading.Thread Thread
        {
            get
            {
                if (_thread == null || !_thread.IsAlive)
                {
                    _thread = new System.Threading.Thread(new System.Threading.ThreadStart(ExecuteSync));
                }
                return _thread;
            }
        }

        private static string[] WindowsApplicationList = new string[] { "NameMyApplication" };

        private static void ExecuteSync()
        {
            try
            {
                foreach (var application in WindowsApplicationList)
                {
                    Process[] processes = Process.GetProcessesByName(application);

                    foreach (var process in processes)
                    {
                        try
                        {                            
                            var processMemory = _processesApplication.Where(w => w.Id == process.Id).FirstOrDefault();
                            if (processMemory == null)
                            {

                                var processAplication = new WindowsApplication(process.Id, process.ProcessName, process.TotalProcessorTime);
                                processAplication.ProcessIsIdleEvent += (object sender, ProcessApplicationArgs e) =>
                                {
                                    Process.GetProcessById(e.Id).Kill();

                                    Console.BackgroundColor = ConsoleColor.White;
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"{e.ProcessName} is idle...");
                                };

                                _processesApplication.Add(processAplication);
                                continue;
                            }

                            processMemory.SetEndCPUTime(process.TotalProcessorTime);
                        }
                        catch (Exception)
                        {
                            //throw;
                        }

                        Console.WriteLine($"{process.ProcessName}");
                    }
                }
                Console.WriteLine();
            }
            catch (Exception)
            {
                //throw;
            }
        }

        private static void ExecuteSyncThread()
        {
            if (Thread.IsAlive)
                return;
            _thread.Start();
        }

        public class WindowsApplication : IEquatable<WindowsApplication>
        {
            public WindowsApplication(int id, string processName, TimeSpan startCPUTime)
            {
                Id = id;
                ProcessName = processName;
                StartCPUTime = startCPUTime;
            }

            public event EventHandler<ProcessApplicationArgs> ProcessIsIdleEvent;

            public int Id { get; private set; }
            public string ProcessName { get; private set; }
            public TimeSpan StartCPUTime { get; private set; }

            public void SetEndCPUTime(TimeSpan endCPUTime)
            {
                if (StartCPUTime == endCPUTime)
                    ProcessIsIdleEvent?.Invoke(this, new ProcessApplicationArgs() { Id = this.Id, ProcessName = this.ProcessName });

                StartCPUTime = endCPUTime;
            }

            public bool Equals(WindowsApplication other)
            {
                return other.Id.Equals(this.Id);
            }

            public override string ToString()
            {
                return $"{this.ProcessName}";
            }
        }

        public class ProcessApplicationArgs
        {
            public int Id { get; set; }
            public string ProcessName { get; set; }
        }
    }
}
