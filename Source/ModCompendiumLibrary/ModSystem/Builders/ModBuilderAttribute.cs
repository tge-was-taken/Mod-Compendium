using System;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    internal class ModBuilderAttribute : Attribute
    {
        private Game mGame;

        public string DisplayName { get; }

        public Game Game
        {
            get { return mGame; }
            set { mGame = value; IsGeneric = false; }
        }

        public bool IsGeneric { get; private set; } = true;

        public ModBuilderAttribute( string displayName )
        {
            DisplayName = displayName;
        }
    }
}