using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.ModSystem;
using ModCompendiumLibrary.Reflection;

namespace ModCompendiumLibrary.Configuration
{
    public static class ConfigStore
    {
        private static readonly Dictionary<Type, IConfigurable> sConfigurableByType;
        private static readonly Dictionary<Game, GameConfig> sGameConfigByGame;

        static ConfigStore()
        {
            Log.Config.Info( "Initializing configs" );

            var configurableTypes = TypeCache.Types
                                             .Where( x => x != typeof( GlobalConfig ) && x != typeof( ModDatabaseConfig ) && !x.IsAbstract &&
                                                          x.GetInterfaces().Contains( typeof( IConfigurable ) ) )
                                             .ToList();

            // Create instances & deserialize them
            sConfigurableByType = new Dictionary< Type, IConfigurable >();
            sGameConfigByGame = new Dictionary< Game, GameConfig >();

            // Initialize global config first
            InitializeConfigurable( typeof( GlobalConfig ) );

            // Then initialize mod database
            InitializeConfigurable( typeof( ModDatabaseConfig ) );

            // And then the rest
            foreach ( var type in configurableTypes )
                InitializeConfigurable( type );

            // Save configs in case this is the first run
            Save();
        }

        private static void InitializeConfigurable( Type type )
        {
            IConfigurable instance;

            try
            {
                instance = ( IConfigurable )Activator.CreateInstance( type, null, null );
            }
            catch ( Exception e )
            {
                Log.Config.Fatal( $"Failed to create instance of config: {type}" );
                Log.Config.Trace( e.Message );
                return;
            }

            if ( type.IsSubclassOf( typeof( GameConfig ) ) )
            {
                var gameConfigInstance = ( GameConfig )instance;
                sGameConfigByGame[gameConfigInstance.Game] = gameConfigInstance;
            }

            sConfigurableByType[ type ] = instance;
            LoadConfig( type, instance );
        }

        public static void Load()
        {
            Log.Config.Info( "Loading config files" );

            if ( !Directory.Exists( "Config" ) )
            {
                Log.Config.Error( "Config directory doesn't exist; creating new directory..." );
                Directory.CreateDirectory( "Config" );
            }

            foreach ( var kvp in sConfigurableByType )
            {
                var type = kvp.Key;
                var configurable = kvp.Value;

                LoadConfig( type, configurable );
            }
        }

        private static void LoadConfig( Type type, IConfigurable configurable )
        {
            // Deserialize config
            var configPath = $"Config\\{type.Name}.xml";
            if ( File.Exists( configPath ) )
            {
                Log.Config.Trace( $"Loading config file: {configPath}" );

                try
                {
                    var document = XDocument.Load( configPath );
                    if ( document.Root != null )
                        configurable.Deserialize( document.Root );
                }
                catch ( Exception e )
                {
                    Log.Config.Error( $"Failed to load config file: {configPath}" );
                    Log.Config.Trace( e.Message );
                }
            }
        }

        public static void Save()
        {
            Log.Config.Info( "Saving config files" );

            if ( !Directory.Exists( "Config" ) )
            {
                Log.Config.Error( "Config directory doesn't exist; creating new directory..." );
                Directory.CreateDirectory( "Config" );
            }

            foreach ( var kvp in sConfigurableByType )
            {
                var type = kvp.Key;
                var configurable = kvp.Value;

                SaveConfig( type, configurable );
            }
        }

        private static void SaveConfig( Type type, IConfigurable configurable )
        {
            var configPath = $"Config\\{type.Name}.xml";
            Log.Config.Trace( $"Saving config file: {configPath}" );

            try
            {
                // Serialize config
                var document = new XDocument();
                {
                    var rootElement = new XElement( type.Name );
                    configurable.Serialize( rootElement );
                    document.Add( rootElement );
                }

                document.Save( configPath );
            }
            catch ( Exception e )
            {
                Log.Config.Error( $"Failed to save config file: {configPath}" );
                Log.Config.Trace( e.Message );
            }
        }

        public static T Get< T >() where T : class
        {
            return sConfigurableByType[ typeof( T ) ] as T;
        }

        public static GameConfig Get( Game game )
        {
            return sGameConfigByGame[ game ];
        }
    }
}
