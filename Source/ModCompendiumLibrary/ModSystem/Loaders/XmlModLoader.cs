using System;
using System.IO;
using System.Xml.Linq;
using ModCompendiumLibrary.Logging;

namespace ModCompendiumLibrary.ModSystem.Loaders
{
    public class XmlModLoader : IModLoader
    {
        public Mod Load( string baseDirectoryPath )
        {
            Log.Loader.Trace( $"Loading mod directory: {baseDirectoryPath}" );

            // Check if Mod.xml exists
            var xmlPath = Path.Combine( baseDirectoryPath, "Mod.xml" );
            if ( !File.Exists( xmlPath ) )
                throw new ModXmlFileMissingException( xmlPath );

            // Check if Data directory exists
            var dataDirectoryPath = Path.Combine( baseDirectoryPath, "Data" );
            if ( !Directory.Exists( dataDirectoryPath ) )
            {
                Log.Loader.Error( $"Data directory is missing: {dataDirectoryPath}" );
                Directory.CreateDirectory( dataDirectoryPath );
            }

            // Set data directory
            var modBuilder = new ModBuilder();
            modBuilder.SetBaseDirectoryPath( baseDirectoryPath );
            modBuilder.SetDataDirectoryPath( dataDirectoryPath );

            return LoadModXml( xmlPath, modBuilder );
        }

        private Mod LoadModXml( string xmlPath, ModBuilder modBuilder )
        {
            Log.Loader.Trace( $"Loading mod xml: {xmlPath}" );
            var document = XDocument.Load( xmlPath, LoadOptions.None );
            var rootNode = document.Root;
            if ( rootNode == null )
                throw new ModXmlFileInvalidException( "Root node is missing" );

            bool hasId = false;

            foreach ( var element in rootNode.Elements() )
            {
                switch ( element.Name.LocalName )
                {
                    case nameof( Mod.Id ):
                        if ( Guid.TryParse( element.Value, out var id ) && id != Guid.Empty )
                        {
                            modBuilder.SetId( id );
                            hasId = true;
                        }
                        break;

                    case nameof( Mod.Game ):
                        {
                            if ( Enum.TryParse<Game>( element.Value, true, out var game ) )
                            {
                                modBuilder.SetGame( game );
                            }
                            else
                            {
                                throw new ModXmlFileInvalidException( $"Invalid game specified: {element.Value}" );
                            }
                        }
                        break;

                    case nameof( Mod.Title ):
                        modBuilder.SetTitle( element.Value );
                        break;

                    case nameof( Mod.Description ):
                        modBuilder.SetDescription( element.Value );
                        break;

                    case nameof( Mod.Version ):
                        modBuilder.SetVersion( element.Value );
                        break;

                    case nameof( Mod.Date ):
                        modBuilder.SetDate( element.Value );
                        break;

                    case nameof( Mod.Author ):
                        modBuilder.SetAuthor( element.Value );
                        break;

                    case nameof( Mod.Url ):
                        modBuilder.SetUrl( element.Value );
                        break;

                    case nameof( Mod.UpdateUrl ):
                        modBuilder.SetUpdateUrl( element.Value );
                        break;
                }
            }

            var mod = modBuilder.Build();

            if ( !hasId )
            {
                // Save xml if GUID is missing
                Log.Loader.Info( "Mod GUID is missing. Resaving xml..." );
                Save( mod );
            }

            return mod;
        }

        public void Save( Mod mod )
        {
            Log.Loader.Trace( $"Saving mod to directory: {mod.BaseDirectory}" );

            if ( !Directory.Exists( mod.BaseDirectory ) )
                Directory.CreateDirectory( mod.BaseDirectory );

            // Serialize mod xml
            var modXmlPath = Path.Combine( mod.BaseDirectory, "Mod.xml" );
            var document = new XDocument();
            var rootElement = new XElement( nameof( Mod ) );
            {
                rootElement.Add( new XElement( nameof( mod.Id ), mod.Id ) );
                rootElement.Add( new XElement( nameof( mod.Game ), mod.Game ) );
                rootElement.Add( new XElement( nameof( mod.Title ), mod.Title ) );
                rootElement.Add( new XElement( nameof( mod.Description ), mod.Description ) );
                rootElement.Add( new XElement( nameof( mod.Version ), mod.Version ) );
                rootElement.Add( new XElement( nameof( mod.Date ), mod.Date ) );
                rootElement.Add( new XElement( nameof( mod.Author ), mod.Author ) );
                rootElement.Add( new XElement( nameof( mod.Url ), mod.Url ) );
                rootElement.Add( new XElement( nameof( mod.UpdateUrl ), mod.UpdateUrl ) );
            }
            document.Add( rootElement );
            document.Save( modXmlPath );

            var prebuildScriptPath = Path.Combine( mod.BaseDirectory, "prebuild.bat" );
            if ( !File.Exists( prebuildScriptPath ) )
                File.Create( prebuildScriptPath ).Close();

            if ( string.IsNullOrWhiteSpace( mod.DataDirectory ) )
                mod.DataDirectory = Path.Combine( mod.BaseDirectory, "Data" );

            if ( !Directory.Exists( mod.DataDirectory ) )
                Directory.CreateDirectory( mod.DataDirectory );
        }
    }
}