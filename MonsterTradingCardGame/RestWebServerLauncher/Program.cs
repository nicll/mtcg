using System;
using System.Diagnostics;
using System.Threading;

namespace RestWebServerLauncher
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener() { TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ThreadId });
            new MessageServer().Start();
            Thread.CurrentThread.Join();
        }
    }
}
