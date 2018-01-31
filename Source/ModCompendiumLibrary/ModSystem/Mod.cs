using System;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem
{
    public class Mod
    {
        public Guid Id { get; set; }

        public Game Game { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Version { get; set; }

        public string Date { get; set; }

        public string Author { get; set; }

        public string Url { get; set; }

        public string UpdateUrl { get; set; }

        public string BaseDirectory { get; set; }

        public string DataDirectory { get; set; }

        internal Mod( Guid id, Game game, string title, string description, string version, string date, string author, string url, string updateUrl, string baseDirectory, string dataDirectory )
        {
            Id = id;
            Game = game;
            Title = title;
            Description = description;
            Version = version;
            Date = date;
            Author = author;
            Url = url;
            UpdateUrl = updateUrl;
            BaseDirectory = baseDirectory;
            DataDirectory = dataDirectory;
        }
    }
}
