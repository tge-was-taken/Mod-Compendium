using System;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.ModSystem.Builders;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumCLI
{
    internal class Program
    {
        private static void Main( string[] args )
        {
            Log.MessageBroadcasted += ( s, e ) =>
            {
                var currentColor = Console.ForegroundColor;

                switch ( e.Severity )
                {
                    case Severity.Trace:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case Severity.Info:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                    case Severity.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case Severity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case Severity.Fatal:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        break;
                }

                Console.WriteLine( $"{e.Channel.Name}: {e.Severity}: {e.Message}" );

                Console.ForegroundColor = currentColor;
            };

            var builder = new Ps2IsoModBuilder();
            var root = VirtualDirectory.FromHostDirectory( @"D:\Games\Sony PS2\temp\New folder" );
            var output = builder.Build( root, "test.iso" );
        }
    }
}
