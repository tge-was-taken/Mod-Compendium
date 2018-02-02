using System;
using System.IO;

namespace ModCompendiumLibrary.ModSystem
{
    public class ModBuilder
    {
        private string mAuthor;
        private string mBaseDirectory;
        private string mDataDirectory;
        private string mDate;
        private string mDescription;
        private Game mGame;
        private Guid mId;
        private string mTitle;
        private string mUpdateUrl;
        private string mUrl;
        private string mVersion;

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
            {
                throw new InvalidOperationException( "Game isn't set" );
            }

            if ( string.IsNullOrWhiteSpace( mTitle ) )
            {
                throw new InvalidOperationException( "Title isn't set" );
            }

            if ( string.IsNullOrWhiteSpace( mBaseDirectory ) )
            {
                throw new InvalidOperationException( "Base directory isn't set" );
            }

            if ( string.IsNullOrWhiteSpace( mDataDirectory ) )
            {
                mDataDirectory = Path.Combine( mBaseDirectory, "Data" );
            }

            return new Mod( mId, mGame, mTitle, mDescription, mVersion, mDate, mAuthor, mUrl, mUpdateUrl, mBaseDirectory, mDataDirectory );
        }
    }
}
