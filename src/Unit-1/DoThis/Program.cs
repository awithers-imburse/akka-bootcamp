using Akka.Actor;

namespace WinTail
{
    #region Program
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            // initialize MyActorSystem
            MyActorSystem = ActorSystem.Create("MyActorSystem");

            // create our actors
            //var consoleWriterProps = Props.Create(typeof(ConsoleWriterActor)); // using typeof syntax. note: do not use this normally
            var consoleWriterProps = Props.Create<ConsoleWriterActor>(); // using generic syntax
            var consoleWriterActor = MyActorSystem.ActorOf(consoleWriterProps, "consoleWriterActor");

            var validationProps = Props.Create(() => new ValidationActor(consoleWriterActor)); // using lambda props syntax
            var validationActor = MyActorSystem.ActorOf(validationProps, "validationActor");

            var consoleReaderProps = Props.Create<ConsoleReaderActor>(validationActor); // using generic syntax
            var consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProps, "consoleReaderActor");

            // tell console reader to begin
            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }
    }
    #endregion
}
