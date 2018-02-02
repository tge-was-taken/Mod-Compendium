using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace ModCompendiumLibrary.FileParsers
{
    public class Ps2SystemConfig : IDictionary<string, string>
    {
        private IDictionary<string, string> mValueDictionary;

        public Ps2SystemConfig( string path )
        {
            using ( var stream = File.OpenRead( path ) )
                Parse( stream, false );
        }

        public Ps2SystemConfig( Stream stream, bool leaveOpen )
        {
            Parse( stream, leaveOpen );
        }

        private void Parse( Stream stream, bool leaveOpen )
        {
            mValueDictionary = new Dictionary< string, string >( StringComparer.InvariantCultureIgnoreCase );

            var reader = new StreamReader( stream );

            while ( !reader.EndOfStream )
            {
                var line = reader.ReadLine();
                if ( line == null )
                    break;

                var kvpString = line.Split( '=' );
                mValueDictionary[kvpString[0].Trim()] = kvpString[1].Trim();
            }

            if ( !leaveOpen )
                reader.Dispose();
        }

        /// <summary>
        /// Utility method that gets the path to the executable.
        /// </summary>
        /// <param name="normalize"></param>
        /// <returns></returns>
        public string GetExecutablePath(bool normalize)
        {
            if ( !mValueDictionary.TryGetValue( "BOOT2", out var executablePath ) )
                return null;

            if ( normalize )
            {
                // Normalize executable path
                if ( executablePath.StartsWith( "cdrom0:\\" ) )
                    executablePath = executablePath.Substring( 8 );

                if ( executablePath.EndsWith( ";1" ) )
                    executablePath = executablePath.Substring( 0, executablePath.Length - 2 );
            }

            return executablePath;
        }

        /// <summary>
        /// Utility method that gets the path to the executable from a stream containing system configurationd ata.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="leaveOpen"></param>
        /// <param name="normalize"></param>
        /// <returns></returns>
        public static string GetExecutablePath( Stream stream, bool leaveOpen, bool normalize )
        {
            var systemConfig = new Ps2SystemConfig( stream, leaveOpen );
            return systemConfig.GetExecutablePath( normalize );
        }

        #region IDictionary implementation
        public IEnumerator< KeyValuePair< string, string > > GetEnumerator()
        {
            return mValueDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ( ( IEnumerable ) mValueDictionary ).GetEnumerator();
        }

        public void Add( KeyValuePair< string, string > item )
        {
            mValueDictionary.Add( item );
        }

        public void Clear()
        {
            mValueDictionary.Clear();
        }

        public bool Contains( KeyValuePair< string, string > item )
        {
            return mValueDictionary.Contains( item );
        }

        public void CopyTo( KeyValuePair< string, string >[] array, int arrayIndex )
        {
            mValueDictionary.CopyTo( array, arrayIndex );
        }

        public bool Remove( KeyValuePair< string, string > item )
        {
            return mValueDictionary.Remove( item );
        }

        public int Count
        {
            get { return mValueDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return mValueDictionary.IsReadOnly; }
        }

        public bool ContainsKey( string key )
        {
            return mValueDictionary.ContainsKey( key );
        }

        public void Add( string key, string value )
        {
            mValueDictionary.Add( key, value );
        }

        public bool Remove( string key )
        {
            return mValueDictionary.Remove( key );
        }

        public bool TryGetValue( string key, out string value )
        {
            return mValueDictionary.TryGetValue( key, out value );
        }

        public string this[ string key ]
        {
            get { return mValueDictionary[ key ]; }
            set { mValueDictionary[ key ] = value; }
        }

        public ICollection< string > Keys
        {
            get { return mValueDictionary.Keys; }
        }

        public ICollection< string > Values
        {
            get { return mValueDictionary.Values; }
        }
        #endregion
    }
}
