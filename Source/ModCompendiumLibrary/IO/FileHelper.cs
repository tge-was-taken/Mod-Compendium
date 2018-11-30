using System.IO;

namespace ModCompendiumLibrary.IO
{
    public static class FileHelper
    {
        public static bool IsFileInUse( string path )
        {
            // https://stackoverflow.com/a/937558/4755778
            FileStream stream = null;

            try
            {
                stream = File.Open( path, FileMode.Open, FileAccess.Read, FileShare.None );
            }
            catch ( IOException )
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                stream?.Close();
            }

            //file is not locked
            return false;
        }
    }
}
