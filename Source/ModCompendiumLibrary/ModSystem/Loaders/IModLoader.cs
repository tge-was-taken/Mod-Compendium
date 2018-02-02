namespace ModCompendiumLibrary.ModSystem.Loaders
{
    public interface IModLoader
    {
        Mod Load( string baseDirectoryPath );

        void Save( Mod mod );
    }
}