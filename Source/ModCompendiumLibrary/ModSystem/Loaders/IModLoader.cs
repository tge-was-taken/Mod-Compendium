namespace ModCompendiumLibrary.ModSystem.Loaders
{
    public interface IModLoader
    {
        Mod Load( string basePath );

        void Save( Mod mod, string path );
    }
}