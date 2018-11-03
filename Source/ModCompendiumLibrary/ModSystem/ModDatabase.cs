using System;
using System.Collections.Generic;
using System.IO;
using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.ModSystem.Loaders;

namespace ModCompendiumLibrary.ModSystem
{
    public static class ModDatabase
    {
        private static Dictionary<Guid, Mod> sModById;
        private static Dictionary<Game, List<Mod>> sModsByGame;

        /// <summary>
        /// Gets the mods in the database.
        /// </summary>
        public static IEnumerable<Mod> Mods => sModById.Values;

        public static string ModDirectory { get; private set; }

        static ModDatabase()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize, or re-initialize the mod database.
        /// </summary>
        public static void Initialize()
        {
            Log.ModDatabase.Info( "Initializing mod database" );

            sModById = new Dictionary<Guid, Mod>();
            sModsByGame = new Dictionary<Game, List<Mod>>();
            foreach ( Game value in Enum.GetValues( typeof( Game ) ) )
            {
                sModsByGame[value] = new List<Mod>();
            }

            var config = ConfigStore.Get<ModDatabaseConfig>();
            ModDirectory = config.ModsDirectoryPath;

            if ( !Directory.Exists( ModDirectory ) )
            {
                Log.ModDatabase.Error( "Mods directory doesn't exist; creating new directory..." );
                Directory.CreateDirectory( ModDirectory );
            }
            else
            {
                // Todo: Different mod types?
                var modLoader = new XmlModLoader();

                foreach ( var directory in Directory.EnumerateDirectories( ModDirectory ) )
                    TryLoadModDirectory( modLoader, directory );
            }
        }

        private static void TryLoadModDirectory( XmlModLoader modLoader, string directory )
        {
            var localDirectoryPath = directory.Remove( 0, ModDirectory.Length );

            bool notAModDirectory = false;
            Mod mod = null;

            try
            {
                mod = modLoader.Load( directory );
            }
            catch ( ModXmlFileMissingException )
            {
                notAModDirectory = true;
            }
            catch ( ModXmlFileInvalidException e )
            {
                Log.ModDatabase.Error( $"Mod directory '{localDirectoryPath}' contains an invalid Mod.xml file: {e.Message}." );
            }
            catch ( ModDataDirectoryMissingException )
            {
                Log.ModDatabase.Error( $"Mod directory '{localDirectoryPath}' doesn't have a Data directory." );
            }
            catch ( Exception e )
            {
                Log.ModDatabase.Error(
                    $"Unhandled exception thrown while loading mod directory '{localDirectoryPath}':\n{e.Message}\n{e.StackTrace}" );

#if DEBUG
                        throw;
#endif
            }

            if ( mod != null )
            {
                sModById[ mod.Id ] = mod;
                sModsByGame[ mod.Game ].Add( mod );
            }

            if ( notAModDirectory )
            {
                // Recurse
                foreach ( var subDirectory in Directory.EnumerateDirectories( directory ) )
                    TryLoadModDirectory( modLoader, subDirectory );
            }
        }

        public static bool Exists( Guid id ) => sModById.ContainsKey( id );

        public static bool TryGet( Guid id, out Mod value ) => sModById.TryGetValue( id, out value );

        public static Mod Get( Guid id ) => sModById[id];

        public static IEnumerable<Mod> Get( Game game ) => sModsByGame[game];
    }
}
