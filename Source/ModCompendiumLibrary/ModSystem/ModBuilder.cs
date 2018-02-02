using System;
using System.IO;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem
{
    public class ModBuilder
    {
        private Guid mId;
        private Game mGame;
        private string mTitle;
        private string mDescription;
        private string mVersion;
        private string mDate;
        private string mAuthor;
        private string mUrl;
        private string mUpdateUrl;
        private string mBaseDirectory;
        private string mDataDirectory;

        public ModBuilder()
        {
            mId = Guid.NewGuid();
            mGame = 0;
            mTitle = string.Empty;
            mDescription = string.Empty;
            mVersion = string.Empty;
            mDate = string.Empty;
            mAuthor = string.Empty;
            mUrl = string.Empty;
            mUpdateUrl = string.Empty;
            mDataDirectory = null;
            mBaseDirectory = null;
        }

        public ModBuilder SetId( Guid id )
        {
            mId = id;
            return this;
        }

        public ModBuilder SetGame( Game game )
        {
            mGame = game;
            return this;
        }

        public ModBuilder SetTitle( string title )
        {
            mTitle = title;
            return this;
        }

        public ModBuilder SetDescription( string description )
        {
            mDescription = description;
            return this;
        }

        public ModBuilder SetVersion( string version )
        {
            mVersion = version;
            return this;
        }

        public ModBuilder SetDate( string date )
        {
            mDate = date;
            return this;
        }

        public ModBuilder SetAuthor( string author )
        {
            mAuthor = author;
            return this;
        }

        public ModBuilder SetUrl( string url )
        {
            mUrl = url;
            return this;
        }

        public ModBuilder SetUpdateUrl( string updateUrl )
        {
            mUpdateUrl = updateUrl;
            return this;
        }

        public ModBuilder SetBaseDirectoryPath( string path )
        {
            mBaseDirectory = path;
            return this;
        }

        public ModBuilder SetDataDirectoryPath( string path )
        {
            mDataDirectory = path;
            return this;
        }

        public Mod Build()
        {
            if ( mGame == 0 )
                throw new InvalidOperationException( "Game isn't set" );

            if ( string.IsNullOrWhiteSpace( mTitle ) )
                throw new InvalidOperationException( "Title isn't set" );

            if ( string.IsNullOrWhiteSpace( mBaseDirectory ) )
                throw new InvalidOperationException( "Base directory isn't set" );

            if ( string.IsNullOrWhiteSpace( mDataDirectory ) )
            {
                mDataDirectory = Path.Combine( mBaseDirectory, "Data" );
            }

            return new Mod( mId, mGame, mTitle, mDescription, mVersion, mDate, mAuthor, mUrl, mUpdateUrl, mBaseDirectory, mDataDirectory );
        }
    }
}