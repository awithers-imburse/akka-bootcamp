using System;
using System.Collections.Generic;
using System.Diagnostics;
using Akka.Actor;

namespace ChartApp.Actors
{
    /// <summary>
    /// Actor responsible for monitoring a specific <see cref="PerformanceCounter"/>
    /// </summary>
    public class PerformanceCounterActor : UntypedActor
    {
        private readonly string _seriesName;
        private readonly Func<PerformanceCounter> _performanceCounterGenerator;
        private readonly HashSet<IActorRef> _subscriptions;
        private readonly ICancelable _cancelPublishing;
        private PerformanceCounter _counter;

        // Note: constructor args that are IDisposable objects should be delegated out as
        //       actor re-starts would otherwise result in a reference to a disposed object
        public PerformanceCounterActor(
            string seriesName,
            Func<PerformanceCounter> performanceCounterGenerator)
        {
            _seriesName = seriesName;
            _performanceCounterGenerator = performanceCounterGenerator;

            _subscriptions = new HashSet<IActorRef>();
            _cancelPublishing = new Cancelable(Context.System.Scheduler);
        }

        #region Actor lifecycle methods

        protected override void PreStart()
        {
            // create new perf counter instance within the actor through delegate,
            // ensures we have a valid reference to an IDisposable even after a restart...
            _counter = _performanceCounterGenerator();

            Context
                .System
                .Scheduler
                .ScheduleTellRepeatedly(
                    TimeSpan.FromMilliseconds(250), // delay
                    TimeSpan.FromMilliseconds(250), // interval
                    Self,                           // recipient
                    new GatherMetrics(),            // payload to send
                    Self,                           // sender
                    _cancelPublishing);             // cancellation token
        }

        protected override void PostStop()
        {
            try
            {
                // we must cancel the scheduler when we die otherwise we'll leave a resource leak
                // for continued message sends to dead subscriber actors
                _cancelPublishing.Cancel(false);

                // we should still be concerned with the disposing of IDisposable objects
                // to avoid resource leaks
                _counter.Dispose();
            }
            catch
            {
                // ignore object disposed exceptions
            }
            finally
            {
                base.PostStop();
            }
        }

        #endregion

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case GatherMetrics _:
                {
                    var metric = new Metric(_seriesName, _counter.NextValue());
                    foreach (var subscription in _subscriptions)
                    {
                        subscription.Tell(metric);
                    }

                    break;
                }
                case SubscribeCounter subscribeRequest:
                {
                    _subscriptions.Add(subscribeRequest.Subscriber);
                    break;
                }
                case UnsubscribeCounter unsubscribeRequest:
                {
                    _subscriptions.Remove(unsubscribeRequest.Subscriber);
                    break;
                }
            }
        }
    }
}
