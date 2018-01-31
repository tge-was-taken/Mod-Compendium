namespace ModCompendiumLibrary.Logging
{
    public class MessageBroadcastedEventArgs
    {
        public LogChannel Channel { get; }

        public Severity Severity { get; }

        public string Message { get; }

        public MessageBroadcastedEventArgs( LogChannel channel, Severity severity, string message )
        {
            Channel = channel;
            Severity = severity;
            Message = message;
        }
    }
}