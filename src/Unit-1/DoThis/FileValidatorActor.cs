using System.IO;
using Akka.Actor;

namespace WinTail
{
    public class FileValidatorActor : UntypedActor
    {
        private readonly IActorRef _consoleWriterActor;

        public FileValidatorActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            var filePath = message as string;

            if (string.IsNullOrEmpty(filePath))
            {
                // signal that the user needs to supply an input
                _consoleWriterActor.Tell(
                    new Messages.NullInputError("Input was blank, please try again\n"));

                // tell sender to continue doing its thing
                Sender.Tell(new Messages.ContinueProcessing());
            }
            else
            {
                var isValid = IsFileUri(filePath);

                if (isValid)
                {
                    // signal successful input
                    _consoleWriterActor.Tell(
                        new Messages.InputSuccess($"Starting processing for {filePath}"));

                    // start co-ordinator
                    Context.ActorSelection("akka://MyActorSystem/user/tailCoOrdinatorActor").Tell(
                        new TailCoordinatorActor.StartTail(filePath, _consoleWriterActor));
                }
                else
                {
                    // signal bad input
                    _consoleWriterActor.Tell(
                        new Messages.ValidationError($"{filePath} is not an existing URI on disk"));

                    // tell sender to keep doing its thing
                    Sender.Tell(
                        new Messages.ContinueProcessing());
                }
            }
        }

        /// <summary>
        /// Checks if file exists at path provided by user.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool IsFileUri(string path) => File.Exists(path);
    }
}
