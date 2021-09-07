using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for serializing message writes to the console.
    /// (write one message at a time, champ :)
    /// </summary>
    class ConsoleWriterActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            if (message is Messages.InputError)
            {
                var errorMessage = message as Messages.InputError;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errorMessage.Reason);
            }
            else if (message is Messages.InputSuccess)
            {
                var successMessage = message as Messages.InputSuccess;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(successMessage.Reason);
            }
            else
            {
                Console.WriteLine(message);
            }

            Console.ResetColor();
        }
    }
}
