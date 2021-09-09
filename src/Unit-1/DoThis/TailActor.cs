using Akka.Actor;
using System.IO;
using System.Text;

namespace WinTail
{
    /// <summary>
    /// Monitors the file at <see cref="_filePath"/> for changes and sends file updates to console.
    /// </summary>
    public class TailActor : UntypedActor
    {
        private readonly IActorRef _reporterActor;
        private readonly string _filePath;
        private readonly FileObserver _observer;
        private readonly Stream _fileStream;
        private readonly StreamReader _fileStreamReader;

        public TailActor(IActorRef reporterActor, string filePath)
        {
            _reporterActor = reporterActor;
            _filePath = filePath;

            var fullFilePath = Path.GetFullPath(_filePath);

            // start watching file for changes
            _observer = new FileObserver(Self, fullFilePath);
            _observer.Start();

            // open file stream with shared read/write so we can write while file is open
            _fileStream = new FileStream(
                fullFilePath, 
                FileMode.Open, 
                FileAccess.Read, 
                FileShare.ReadWrite);

            _fileStreamReader = new StreamReader(_fileStream, Encoding.UTF8);

            // read the initial contents of the file and send it to console as first message
            var initialText = _fileStreamReader.ReadToEnd();

            Self.Tell(
                new InitialRead(_filePath, initialText));
        }

        protected override void OnReceive(object message)
        {
            if (message is InitialRead initialMessage)
            {
                _reporterActor.Tell(initialMessage.Text);
            }
            else if (message is FileError errorMessage)
            {
                _reporterActor.Tell($"Tail error: {errorMessage.Reason}");
            }
            else if (message is FileWrite)
            {
                var allText = _fileStreamReader.ReadToEnd();

                if (!string.IsNullOrEmpty(allText))
                    _reporterActor.Tell(allText);
            }
        }

        #region Message types

        /// <summary>
        /// Signal that the file has changed, and we need to read the next line of the file.
        /// </summary>
        public class FileWrite
        {
            public string FileName { get; }

            public FileWrite(string fileName)
            {
                FileName = fileName;
            }
        }

        /// <summary>
        /// Signal that the OS had an error accessing the file.
        /// </summary>
        public class FileError
        {
            public string FileName { get; }
            public string Reason { get; }

            public FileError(string fileName, string reason)
            {
                FileName = fileName;
                Reason = reason;
            }
        }

        /// <summary>
        /// Signal to read the initial contents of the file at actor startup.
        /// </summary>
        public class InitialRead
        {
            public string FileName { get; }
            public string Text { get; }

            public InitialRead(string fileName, string text)
            {
                FileName = fileName;
                Text = text;
            }
        }

        #endregion
    }
}
