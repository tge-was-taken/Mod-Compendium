using System;
using System.Linq;
using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.ModSystem;
using ModCompendiumLibrary.ModSystem.Builders;
using ModCompendiumLibrary.ModSystem.Mergers;

namespace ModCompendiumCLI
{
    internal class Program
    {
        private static void Main( string[] args )
        {
            Log.MessageBroadcasted += (s, e) =>
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

            var config = ConfigManager.Get< Persona4GameConfig >();
            //config.EnableMod( ModDatabase.Mods.Single( x => x.Title == "Picaro Mod" ) );

            var merger = new TopToBottomModMerger();
            //var merged = merger.Merge( config.EnabledModIds );
            var builder = GameModBuilder.Get( config.Game );
            //var task = builder.Build( merged, config.OutputDirectoryPath );
            foreach ( var mod in ModDatabase.Get(config.Game) )
            {
                Console.WriteLine( mod.Title );
            }

            //var modLoader = new XmlModLoader();
            //var modCombiner = new TopToBottomModMerger();
            //ModBuilder modBuilder;
            //VirtualFileSystemEntry modCompiledFile;

            //var mod = modLoader.Load( "Mods\\ExampleMod" );
            //var mod2 = modLoader.Load( "Mods\\Persona4PicaroMod" );
            //var modRoot = modCombiner.Merge( new List<Mod> { mod2, mod } );

            //{
            //    modBuilder = new PassthroughModBuilder();
            //    //modCompiledFile = modBuilder.Build( modRoot, "ExampleModPassthroughModCompilerOutput" );
            //}

            //{
            //    modBuilder = new Persona5CpkModBuilder();
            //    //modCompiledFile = modBuilder.Build( modRoot, "ExampleModCpkModCompilerOutput" );
            //}

            //{
            //    modBuilder = new Persona4ModBuilder();
            //    modCompiledFile = modBuilder.Build( modRoot, "ExampleModP4ModCompilerOutput" );
            //}

            ConfigManager.Save();
        }
    }

    class Progress : IProgress< int >
    {
        public void Report( int value )
        {
            Console.WriteLine( $"Progress: {value}" );
        }
    }
}
