using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor that validates user input and signals result to others.
    /// </summary>
    public class ValidationActor : UntypedActor
    {
        private readonly IActorRef _consoleWriterActor;

        public ValidationActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            var messageText = message as string;

            if (string.IsNullOrEmpty(messageText))
            {
                _consoleWriterActor.Tell(new Messages.NullInputError("No input received."));
            }
            else
            {
                var isValid = IsValid(messageText);

                if (isValid)
                {
                    _consoleWriterActor.Tell(new Messages.InputSuccess("Message was valid."));
                }
                else
                {
                    _consoleWriterActor.Tell(new Messages.ValidationError("Invalid: input had an odd number of characters."));
                }
            }

            // tell sender to continue doing its thing, whatever that may be
            Sender.Tell(new Messages.ContinueProcessing());
        }

        /// <summary>
        /// Validates <see cref="messageText"/>.
        /// Currently says messages are valid if contain an even number of characters.
        /// </summary>
        /// <param name="messageText"></param>
        /// <returns></returns>
        private bool IsValid(string messageText)
        {
            return messageText.Length % 2 == 0;
        }
    }
}
