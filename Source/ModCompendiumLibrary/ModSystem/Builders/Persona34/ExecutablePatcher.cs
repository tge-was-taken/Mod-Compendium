using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public class ExecutablePatcher
    {
        private readonly Stream mExecutableStream;
        private readonly Game mGame;
        private byte[] mExecutableHeader;
        private List<CvmDirectoryInfo> mRootDirectories;
        private byte[] mExecutableFooter;

        public ExecutablePatcher( Stream executableStream, Game game )
        {
            mExecutableStream = executableStream;
            mGame = game;

            ReadCvmRootDirs();
        }

        public void PatchCvm( Stream stream, int cvmIndex )
        {
            //mRootDirectories[cvmIndex].Update( new CvmFile( stream, true ) );
            stream.Position = 0;
        }

        public Stream Build()
        {
            var stream = new MemoryStream();

            // write header
            stream.Write( mExecutableHeader, 0, mExecutableHeader.Length );

            // write cvm root dirs
            using ( var writer = new BinaryWriter( stream, Encoding.Default, true ) )
            {
                foreach ( var cvmRootDir in mRootDirectories )
                {
                    cvmRootDir.Write( writer );
                }
            }

            // write footer
            stream.Write( mExecutableFooter, 0, mExecutableFooter.Length );
            stream.Position = 0;

            return stream;
        }

        private int FindCvmRootDirsStart()
        {
            byte[] magic = new byte[8];

            while ( ( mExecutableStream.Position < mExecutableStream.Length ) && ( mExecutableStream.Position + magic.Length < mExecutableStream.Length ) )
            {
                mExecutableStream.Read( magic, 0, magic.Length );

                if ( magic[0] == '#' && magic[1] == 'D' && magic[2] == 'i' && magic[3] == 'r' && magic[4] == 'L' && magic[5] == 's' && magic[6] == 't' && magic[7] == '#' )
                {
                    return ( int )( mExecutableStream.Position - 20 );
                }
                else
                {
                    // Read every 4 bytes
                    mExecutableStream.Position -= 4;
                }
            }

            return -1;
        }

        private void ReadCvmRootDirs()
        {
            mRootDirectories = new List<CvmDirectoryInfo>();

            // find start of dir lists
            Console.WriteLine( "Scanning for cvm directory lists in executable..." );
            int dirListStart = FindCvmRootDirsStart();
            if ( dirListStart == -1 )
                throw new InvalidDataException( "No #DirLst# signature found in executable. Bad file." );

            Console.WriteLine( "Reading executable cvm directory lists..." );

            // Read header bytes
            mExecutableHeader = new byte[dirListStart];
            mExecutableStream.Position = 0;
            mExecutableStream.Read( mExecutableHeader, 0, mExecutableHeader.Length );

            // Read dir entries
            mExecutableStream.Position = dirListStart;
            using ( var reader = new BinaryReader( mExecutableStream, Encoding.Default, true ) )
            {
                int rootDirectoryCount = 3;
                if ( mGame == Game.Persona4 )
                    rootDirectoryCount = 4;

                for ( int i = 0; i < rootDirectoryCount; i++ )
                {
                    var directory = new CvmDirectoryInfo( null );
                    directory.Read( reader );

                    mRootDirectories.Add( directory );
                }
            }

            // Read footer bytes
            mExecutableFooter = new byte[mExecutableStream.Length - mExecutableStream.Position];
            mExecutableStream.Read( mExecutableFooter, 0, mExecutableFooter.Length );
        }
    }
}