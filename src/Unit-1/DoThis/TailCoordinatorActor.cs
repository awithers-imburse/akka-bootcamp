using System;
using Akka.Actor;

namespace WinTail
{
    public class TailCoordinatorActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            if (message is StartTail startMessage)
            {
                // the TailActor instance created here is a child of this instance of TailCoordinatorActor
                Context.ActorOf(Props.Create(() => new TailActor(
                    startMessage.ReporterActor, 
                    startMessage.FilePath)));
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                maxNrOfRetries: 10,
                withinTimeRange: TimeSpan.FromSeconds(30), 
                localOnlyDecider: x =>
                {
                    return x switch
                    {
                        // simulate a non-critical exception
                        ArithmeticException _ => Directive.Resume,

                        // simulate unrecoverable exception
                        NotSupportedException _ => Directive.Stop,

                        // otherwise return failing actor
                        _ => Directive.Restart
                    };
                });
        }

        #region Message types

        /// <summary>
        /// Start tailing the file at user-specified path.
        /// </summary>
        public class StartTail
        {
            public string FilePath { get; }
            public IActorRef ReporterActor { get; }

            public StartTail(string filePath, IActorRef reporterActor)
            {
                FilePath = filePath;
                ReporterActor = reporterActor;
            }
        }

        /// <summary>
        /// Stop tailing the file at user-specified path.
        /// </summary>
        public class StopTail
        {
            public string FilePath { get; }

            public StopTail(string filePath)
            {
                FilePath = filePath;
            }
        }

        #endregion
    }
}
