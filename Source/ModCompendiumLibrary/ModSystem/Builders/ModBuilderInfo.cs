using System;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public class ModBuilderInfo
    {
        public Type Type { get; }

        public string FriendlyName { get; }

        public Game? Game { get; }

        public bool IsGeneric => Game == null;

        internal ModBuilderInfo( Type type, string friendlyName, Game? game )
        {
            Type = type;
            FriendlyName = friendlyName;
            Game = game;
        }

        public bool IsForGame( Game game )
        {
            if ( IsGeneric )
                return false;

            return Game == game;
        }

        public IModBuilder Create()
        {
            return ( IModBuilder ) Activator.CreateInstance( Type, null, null );
        }

        public T Create< T >() where T : IModBuilder
        {
            return ( T ) Create();
        }
    }
}