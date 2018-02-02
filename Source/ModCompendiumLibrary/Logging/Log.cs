using System;
using System.Collections.Generic;

namespace ModCompendiumLibrary.Logging
{
    public static class Log
    {
        private static List< LogChannel > sChannels = new List< LogChannel >();

        public static readonly LogChannel General     = new LogChannel( nameof( General ));
        public static readonly LogChannel Config      = new LogChannel( nameof( Config ) );
        public static readonly LogChannel Builder     = new LogChannel( nameof( Builder ) );
        public static readonly LogChannel Merger      = new LogChannel( nameof( Merger ) );
        public static readonly LogChannel Loader      = new LogChannel( nameof( Loader ) );
        public static readonly LogChannel ModDatabase = new LogChannel( nameof( ModDatabase ) );

        public static event EventHandler< MessageBroadcastedEventArgs > MessageBroadcasted;

        internal static void RegisterChannel( LogChannel channel )
        {
            channel.MessageBroadcasted += ( s, e ) => MessageBroadcasted?.Invoke( null, e );
            sChannels.Add( channel );
        }
    }
}
