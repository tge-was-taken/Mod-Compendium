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
        private static Dictionary< Guid, Mod > sModById;
        private static Dictionary< Game, List< Mod > > sModsByGame;

        /// <summary>
        ///     Gets the mods in the database.
        /// </summary>
        public static IEnumerable< Mod > Mods => sModById.Values;

        public static string ModDirectory { get; private set; }

        static ModDatabase()
        {
            Initialize();
        }

        /// <summary>
        ///     Initialize, or re-initialize the mod database.
        /// </summary>
        public static void Initialize()
        {
            Log.ModDatabase.Info( "Initializing mod database" );

            sModById = new Dictionary< Guid, Mod >();
            sModsByGame = new Dictionary< Game, List< Mod > >();
            foreach ( Game value in Enum.GetValues( typeof( Game ) ) )
                sModsByGame[ value ] = new List< Mod >();

            var config = ConfigManager.Get< ModDatabaseConfig >();
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

                foreach ( string directory in Directory.EnumerateDirectories( ModDirectory ) )
                {
                    string localDirectoryPath = directory.Remove( 0, ModDirectory.Length );

                    Mod mod = null;

                    try
                    {
                        mod = modLoader.Load( directory );
                    }
                    catch ( ModXmlFileMissingException )
                    {
                        Log.ModDatabase.Error( $"Mod directory '{localDirectoryPath}' doesn't contain a Mod.xml file." );
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
                }
            }
        }

        public static bool Exists( Guid id )
        {
            return sModById.ContainsKey( id );
        }

        public static bool TryGet( Guid id, out Mod value )
        {
            return sModById.TryGetValue( id, out value );
        }

        public static Mod Get( Guid id )
        {
            return sModById[ id ];
        }

        public static IEnumerable< Mod > Get( Game game )
        {
            return sModsByGame[ game ];
        }
    }
}
