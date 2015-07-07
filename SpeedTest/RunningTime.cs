using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeedTest
{
    public class RunningTime
    {
        public long RunningTimeInTicks { get; private set; }

        public double RunningTimeInMicroseconds { get { return ((double)RunningTimeInTicks * 1000) / ((double)Stopwatch.Frequency / 1000); } }

        private RunningTime(long runningTime) 
        {
            RunningTimeInTicks = runningTime;
        }
        
        public static RunningTime TestNow(Action action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            //var previousProcessorAffinity = Process.GetCurrentProcess().ProcessorAffinity;
            //var previousPriority = Thread.CurrentThread.Priority;
            //var previousPriorityClass = Process.GetCurrentProcess().PriorityClass;

            var stopwatch = new Stopwatch();

            //stopwatch.Reset();
            //stopwatch.Start();

            //while (stopwatch.ElapsedMilliseconds < 1500)  // A Warmup of 1000-1500 mS stabilizes the CPU cache and pipeline.
            //{
                
            //}

            //stopwatch.Stop();

            try
            {
                //Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2);
                //Thread.CurrentThread.Priority = ThreadPriority.Highest;
                //Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            
                stopwatch.Reset();
                stopwatch.Start();

                action();

                stopwatch.Stop();

                long runningTime = stopwatch.ElapsedTicks;

                return new RunningTime(runningTime);
            }
            finally
            {
                //Process.GetCurrentProcess().ProcessorAffinity = previousProcessorAffinity;
                //Thread.CurrentThread.Priority = previousPriority;
                //Process.GetCurrentProcess().PriorityClass = previousPriorityClass;
            }
        }

        public override string ToString()
        {


            return string.Format("Running time = {0}µs", RunningTimeInMicroseconds);
        }
    }
}
