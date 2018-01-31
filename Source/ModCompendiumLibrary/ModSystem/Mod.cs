using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem
{
    public class Mod
    {
        public Game Game { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Version { get; set; }

        public string Date { get; set; }

        public string Author { get; set; }

        public string Url { get; set; }

        public string UpdateUrl { get; set; }

        public VirtualDirectory DataDirectory { get; set; }

        public int Id { get; set; }

        public Mod()
        {
        }
    }
}
