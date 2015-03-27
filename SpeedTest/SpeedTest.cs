using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeedTests
{
    /// <summary>
    /// Speedtesting with tracking of mean, variance and standard deviation to monitor the stability of the results.
    /// Results are provided in ticks and in microseconds.
    /// Test a piece of code by simply calling TestNow. One can provided a number of observations. Know that every observation calls the routine in an increasing count based on 
    /// DefaultMinRunningTimePerObservation to achieve a lower deviation.
    /// I use static ctor to always have the class providing meaningfull property values.
    /// Timing is based on .NET Stopwatch.
    /// </summary>
    public class SpeedTest 
    {
        public delegate double PieceOfCodeReturningDummyValue(int loopIndex);

        public static TimeSpan DefaultMinRunningTimePerObservation = TimeSpan.FromSeconds(0.025);
        
        public string Label{ get; private set; }

    #region Result properties
        public double MeanInTicks { get; private set; }
        public double VarianceInTicks { get; private set; }
        public double StandardDeviationInTicks { get; private set; }

        public double MeanInMicroseconds { get { return (MeanInTicks * 1000) / (Stopwatch.Frequency / 1000); } }
        public double VarianceInMicroseconds { get { return (VarianceInTicks * 1000) / (Stopwatch.Frequency / 1000); } }
        public double StandardDeviationInMicroseconds { get { return (StandardDeviationInTicks * 1000) / (Stopwatch.Frequency / 1000); } }
    #endregion
                
        private SpeedTest(string label)
        {
            Label = label;
        }

        public static SpeedTest TestNow(PieceOfCodeReturningDummyValue pieceOfCode, string label, int numberOfObservations = 1)
        {
            var speedTest = new SpeedTest(label);

            speedTest.MeasureExecutionTime(pieceOfCode, DefaultMinRunningTimePerObservation, numberOfObservations);

            return speedTest;
        }

        /// <summary>
        /// Private helper to recalculate the mean, variance and standard deviation based on a set of doubles
        /// Should be separated from this class.
        /// </summary>
        private void RefreshStatistics(IEnumerable<double> values)
        {
            var newMean = values.Average();

            if (values.Count() > 0)
            {
                var newVariance = values.Sum(d => Math.Pow(d - newMean, 2)) / (values.Count() - 1);

                var newStandardDeviation = Math.Sqrt(newVariance);

                if (newStandardDeviation < StandardDeviationInTicks || StandardDeviationInTicks == 0.0)
                {
                    MeanInTicks = newMean;
                    VarianceInTicks = newVariance;
                    StandardDeviationInTicks = newStandardDeviation;
                }
            }
        }

        /// <summary>
        /// Thanks to https://www.itu.dk/people/sestoft/papers/benchmarking.pdf
        /// </summary>
        private double MeasureExecutionTime(PieceOfCodeReturningDummyValue action, TimeSpan minRunningTime, int numberOfObservations = 20) 
        {

            uint timesForSameCall = 1;
            double dummy = 1.0;
            long runningTime = 0;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var previousProcessorAffinity = Process.GetCurrentProcess().ProcessorAffinity;
            var previousPriority = Thread.CurrentThread.Priority;
            var previousPriorityClass = Process.GetCurrentProcess().PriorityClass;            
            
            try
            {
                Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2);
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

                var stopwatch = new Stopwatch();

                stopwatch.Reset();
                stopwatch.Start();

                while (stopwatch.ElapsedMilliseconds < 1500)  // A Warmup of 1000-1500 mS stabilizes the CPU cache and pipeline.
                {
                    double r = action(0);
                }

                stopwatch.Stop();

                var values = new List<double>();

                do
                {
                    timesForSameCall *= 2;

                    values.Clear();

                    for (int j = 0; j < numberOfObservations; j++)
                    {
                        stopwatch.Reset();
                        stopwatch.Start();

                        for (int i = 0; i < timesForSameCall; i++)
                            dummy = action(j);

                        stopwatch.Stop();

                        runningTime = stopwatch.ElapsedTicks;

                        double time = (double)runningTime / timesForSameCall;

                        values.Add(time);
                    }

                    RefreshStatistics(values);
                    Console.WriteLine(this);
                }
                while (TimeSpan.FromTicks(runningTime) < minRunningTime && timesForSameCall < uint.MaxValue);

                RefreshStatistics(values);
            }
            finally
            {
                Process.GetCurrentProcess().ProcessorAffinity = previousProcessorAffinity;
                Thread.CurrentThread.Priority = previousPriority;
                Process.GetCurrentProcess().PriorityClass = previousPriorityClass;
            }

            return Convert.ToDouble(dummy);
        }

        public override string ToString()
        {
            return string.Format("Performed {0} Mean = {1:0.00000}µs; Variance = {2:0.00000}µs; SDev = {3:0.00000}µs ", Label, MeanInMicroseconds, VarianceInMicroseconds, StandardDeviationInMicroseconds);
        }
    }
}
