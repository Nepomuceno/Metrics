using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Metrics.Core
{
    public abstract class Metric
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public string Source { get; set; }
        public string Session { get; set; }
        public MetricType MetricType { get; set; }
        public virtual Measure Measure { get; set; }
        public abstract void Start();
    }

    public class MeterMetric : Metric
    {
        public override void Start()
        {

        }

    }



    public enum MetricType
    {
    }

    public abstract  class Measure
    {
        public abstract string ValueAsString { get; }
    }

    public class Meter : Measure
    {

        private readonly AtomicLong _count = new AtomicLong();
        private readonly long _startTime = DateTime.UtcNow.Ticks;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

        private EWMA _m1Rate = EWMA.OneMinuteEWMA();
        private EWMA _m5Rate = EWMA.FiveMinuteEWMA();
        private EWMA _m15Rate = EWMA.FifteenMinuteEWMA();

        private readonly CancellationTokenSource _token = new CancellationTokenSource();

        public Meter()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!_token.IsCancellationRequested)
                {
                    await Task.Delay(Interval, _token.Token);
                    Tick();
                }
            }, _token.Token);
        }

        public override string ValueAsString
        {
            get { return _count.Get().ToString(); }
        }
        private void Tick()
        {
            _m1Rate.Tick();
            _m5Rate.Tick();
            _m15Rate.Tick();
        }

        public void Mark(long n)
        {
            _count.AddAndGet(n);
            _m1Rate.Update(n);
            _m5Rate.Update(n);
            _m15Rate.Update(n);
        }

        /// <summary>
        ///  Returns the number of events which have been marked
        /// </summary>
        /// <returns></returns>
        public long Count
        {
            get { return _count.Get(); }
        }

        /// <summary>
        /// Returns the mean rate at which events have occured since the meter was created
        /// </summary>
        public double MeanRate
        {
            get
            {
                if (Count != 0)
                {
                    var elapsed = (DateTime.Now.Ticks - _startTime) * 100; // 1 DateTime Tick == 100ns
                    return ConvertNanosRate(Count / (double)elapsed);
                }
                return 0.0;
            }
        }

    }

    public class Histogram : Measure
    {
        public override string ValueAsString
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class Timer : Measure
    {
        public override string ValueAsString
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class Counter : Measure
    {
        public override string ValueAsString
        {
            get { throw new NotImplementedException(); }
        }
    }

    public interface IMetricService
    {
        Task StartAsync(IMetricConfiguration metricConfiguration);
        /// <summary>
        /// Gets a specif metric for a id source and session
        /// </summary>
        /// <param name="id">the metrics id</param>
        /// <param name="source">the metrics source</param>
        /// <param name="session">the metric session</param>
        /// <returns>A matric for this combination</returns>
        Task<T> GetMetricAsync<T>(string id, string source, string session) where T : Metric;

        Task<IEnumerable<Metric>> GetMetricsAsync(string source, string session);
    }

    public interface IMetricConfiguration
    {
    }
}
