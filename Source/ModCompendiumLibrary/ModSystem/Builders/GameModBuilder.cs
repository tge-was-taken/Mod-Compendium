using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModCompendiumLibrary.Reflection;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public static class GameModBuilder
    {
        private static readonly Dictionary<Game, Type> sModBuilderTypeByGame;

        static GameModBuilder()
        {
            sModBuilderTypeByGame = new Dictionary< Game, Type>();
            var modBuilderTypes = TypeCache.Types
                                           .Where( x => x.GetInterfaces().Contains( typeof( IModBuilder ) ) );

            foreach ( var modBuilderType in modBuilderTypes )
            {
                var gameModBuilderAttribute = modBuilderType.GetCustomAttribute< GameModBuilderAttribute >();
                if ( gameModBuilderAttribute != null )
                {
                    sModBuilderTypeByGame[gameModBuilderAttribute.Game] = modBuilderType;
                }
            }
        }

        public static IModBuilder Get( Game game )
        {
            return ( IModBuilder ) Activator.CreateInstance( sModBuilderTypeByGame[ game ], null, null );
        }
    }
}
