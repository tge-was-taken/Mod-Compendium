using System;

namespace ModCompendiumLibrary.Logging
{
    public class LogChannel
    {
        public LogChannel(string name)
        {
            Name = name;
            Log.RegisterChannel( this );
        }

        public string Name { get; }

        public event EventHandler<MessageBroadcastedEventArgs> MessageBroadcasted;

        public void Trace( string message )
        {
            MessageBroadcasted?.Invoke( this, new MessageBroadcastedEventArgs( this, Severity.Trace, message ) );
        }

        public void Info( string message )
        {
            MessageBroadcasted?.Invoke( this, new MessageBroadcastedEventArgs( this, Severity.Info, message ) );
        }

        public void Warning( string message )
        {
            MessageBroadcasted?.Invoke( this, new MessageBroadcastedEventArgs( this, Severity.Warning, message ) );
        }

        public void Error( string message )
        {
            MessageBroadcasted?.Invoke( this, new MessageBroadcastedEventArgs( this, Severity.Error, message ) );
        }

        public void Fatal( string message )
        {
            MessageBroadcasted?.Invoke( this, new MessageBroadcastedEventArgs( this, Severity.Fatal, message ) );
        }
    }
}
