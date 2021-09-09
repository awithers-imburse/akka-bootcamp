using Akka.Actor;

namespace WinTail
{
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            // initialize MyActorSystem
            MyActorSystem = ActorSystem.Create("MyActorSystem");

            // create our actors
            var consoleWriterProps = Props.Create<ConsoleWriterActor>();
            var consoleWriterActor = MyActorSystem.ActorOf(consoleWriterProps, "consoleWriterActor");

            var tailCoOrdinatorProps = Props.Create(() => new TailCoordinatorActor());
            var tailCoOrdinatorActor = MyActorSystem.ActorOf(tailCoOrdinatorProps, "tailCoOrdinatorActor");

            var fileValidatorProps = Props.Create(() => new FileValidatorActor(consoleWriterActor, tailCoOrdinatorActor));
            var fileValidatorActor = MyActorSystem.ActorOf(fileValidatorProps, "fileValidatorActor");

            var consoleReaderProps = Props.Create<ConsoleReaderActor>(fileValidatorActor);
            var consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProps, "consoleReaderActor");

            // tell console reader to begin
            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }
    }
}
