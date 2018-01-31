using System;
using System.IO;
using System.Xml.Linq;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Loaders
{
    public class XmlModLoader : IModLoader
    {
        public Mod Load( string path )
        {
            Log.Loader.Trace( $"Loading mod directory: {path}" );

            var fullPath = Path.GetFullPath( path );

            // Deserialize mod metadata
            var modXmlPath = Path.Combine( fullPath, "Mod.xml" );
            if ( !File.Exists( modXmlPath ) )
                throw new ModXmlFileMissingException( modXmlPath );

            var mod = LoadModXml( modXmlPath );

            // Fetch files from 'Data' directory
            var dataDirectoryPath = Path.Combine( fullPath, "Data" );
            if ( !Directory.Exists( dataDirectoryPath ) )
                throw new ModDataDirectoryMissingException( dataDirectoryPath );

            Log.Loader.Trace( $"Loading mod data directory: {dataDirectoryPath}" );
            mod.DataDirectory = new VirtualDirectory( null, dataDirectoryPath, string.Empty );
            foreach ( string entryPath in Directory.EnumerateFileSystemEntries( dataDirectoryPath ) )
            {
                var fullEntryPath = Path.GetFullPath( entryPath );
                mod.DataDirectory.Add( CreateEntryRecursively( mod.DataDirectory, fullEntryPath ) );
            }

            return mod;
        }

        private Mod LoadModXml( string path )
        {
            Log.Loader.Trace( $"Loading mod xml: {path}" );

            var mod = new Mod();
            var document = XDocument.Load( path, LoadOptions.None );
            var rootNode = document.Root;
            if ( rootNode == null )
                throw new ModXmlFileInvalidException( "Root node is missing" );

            foreach ( var element in rootNode.Elements() )
            {
                switch ( element.Name.LocalName )
                {
                    case "Game":
                        {
                            if ( Enum.TryParse< Game >( element.Value, true, out var game ) )
                            {
                                mod.Game = game;
                            }
                            else
                            {
                                throw new ModXmlFileInvalidException( $"Invalid game specified: {element.Value}" );
                            }
                        }
                        break;

                    case "Title":
                        mod.Title = element.Value;
                        break;

                    case "Description":
                        mod.Description = element.Value;
                        break;

                    case "Version":
                        mod.Version = element.Value;
                        break;

                    case "Date":
                        mod.Date = element.Value;
                        break;

                    case "Author":
                        mod.Author = element.Value;
                        break;

                    case "Url":
                        mod.Url = element.Value;
                        break;

                    case "UpdateUrl":
                        mod.UpdateUrl = element.Value;
                        break;
                }
            }

            return mod;
        }

        private VirtualFileSystemEntry CreateEntryRecursively( VirtualDirectory parent, string path )
        {
            var name = Path.GetFileName( path );

            if ( File.Exists( path ) )
            {
                return new VirtualFile( parent, path, name );
            }
            else
            {
                var directory = new VirtualDirectory( parent, path, name );
                foreach ( var entryPath in Directory.EnumerateFileSystemEntries( path ) )
                {
                    directory.Add( CreateEntryRecursively( directory, entryPath ) );
                }

                return directory;
            }
        }
    }
}