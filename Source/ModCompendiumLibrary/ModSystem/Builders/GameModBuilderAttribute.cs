using System;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    internal class GameModBuilderAttribute : Attribute
    {
        public Game Game { get; }

        public GameModBuilderAttribute( Game game )
        {
            Game = game;
        }
    }
}