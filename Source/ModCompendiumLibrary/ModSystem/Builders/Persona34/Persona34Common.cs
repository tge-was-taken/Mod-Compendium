using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscUtils.Iso9660;
using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public static class Persona34Common
    {
        public static VirtualDirectory GetRootDirectory( Persona34GameConfig config, out CDReader isoFileSystem )
        {
            if ( config.DvdRootOrIsoPath.EndsWith( ".iso" ) )
            {
                Log.Builder.Info( $"Mounting ISO: {config.DvdRootOrIsoPath}" );

                if ( !File.Exists( config.DvdRootOrIsoPath ) )
                    throw new InvalidConfigException( $"Dvd root path references an ISO file that does not exist: {config.DvdRootOrIsoPath}." );

                // Iso file found, convert it to our virtual file system
                isoFileSystem = new CDReader( File.OpenRead( config.DvdRootOrIsoPath ), false );
                return isoFileSystem.ToVirtualDirectory();
            }
            else
            {
                Log.Builder.Info( $"Mounting directory: {config.DvdRootOrIsoPath}" );

                if ( !Directory.Exists( config.DvdRootOrIsoPath ) )
                    throw new InvalidConfigException( $"Dvd root path references a directory that does not exist: {config.DvdRootOrIsoPath}." );

                // No iso file found, assume files are extracted
                var dvdRootDirectory = VirtualDirectory.FromHostDirectory( config.DvdRootOrIsoPath );
                dvdRootDirectory.Name = string.Empty;
                isoFileSystem = null;
                return dvdRootDirectory;
            }
        }
    }
}
