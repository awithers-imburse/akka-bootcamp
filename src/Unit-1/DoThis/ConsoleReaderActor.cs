using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for reading FROM the console. 
    /// Also responsible for calling <see cref="ActorSystem.Terminate"/>.
    /// </summary>
    class ConsoleReaderActor : UntypedActor
    {
        public const string StartCommand = "start";
        public const string ExitCommand = "exit";

        private IActorRef _consoleWriterActor;

        public ConsoleReaderActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            if (message.Equals(StartCommand))
            {
                DoPrintInstructions();
            }
            else if (message is Messages.InputError)
            {
                _consoleWriterActor.Tell(message as Messages.InputError);
            }

            GetAndValidateInput();
        }

        private void DoPrintInstructions()
        {
            Console.WriteLine("Write whatever you want into the console!");
            Console.WriteLine("Some entries will pass validation, and some won't...\n\n");
            Console.WriteLine($"Type '{ExitCommand}' to quit this application at any time.\n");
        }

        /// <summary>
        /// Reads input from console, validates it, then signals appropriate response
        /// (continue processing, error, success, etc).
        /// </summary>
        private void GetAndValidateInput()
        {
            var message = Console.ReadLine();

            if (string.IsNullOrEmpty(message))
            {
                // signal that user needs to supply an input, as previously received input was blank
                Self.Tell(new Messages.NullInputError("No input received."));
            }
            else if (string.Equals(message, ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                // shut down the entire actor system (allows the process to exit)
                Context.System.Terminate();
            }
            else
            {
                var isValid = IsValid(message);

                if (isValid)
                {
                    _consoleWriterActor.Tell(new Messages.InputSuccess("Message was valid."));

                    Self.Tell(new Messages.ContinueProcessing());
                }
                else
                {
                    Self.Tell(new Messages.ValidationError("Invalid: input had an odd number of characters."));
                }
            }
        }

        /// <summary>
        /// Validates <see cref="message"/>.
        /// Currently says messages are valid if contain an even number of characters.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool IsValid(string message)
        {
            return message.Length % 2 == 0;
        }
    }
}