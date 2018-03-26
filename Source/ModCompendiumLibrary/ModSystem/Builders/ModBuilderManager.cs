using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModCompendiumLibrary.Reflection;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public static class ModBuilderManager
    {
        private static readonly Dictionary<Game, List<ModBuilderInfo>> mModBuilderInfoByGame;
        private static readonly Dictionary<Type, ModBuilderInfo> mModBuilderInfoByType;

        static ModBuilderManager()
        {
            var modBuilderTypes = TypeCache.Types
                                           .Where( x => x.GetInterfaces().Contains( typeof( IModBuilder ) ) && !x.IsAbstract );

            var modBuilderInfos = new List< ModBuilderInfo >();
            foreach ( var type in modBuilderTypes )
            {
                var attribute = type.GetCustomAttribute< ModBuilderAttribute >();
                if ( attribute == null )
                    continue;

                var info = new ModBuilderInfo( type, attribute.DisplayName, attribute.IsGeneric ? null : (Game?)attribute.Game );
                modBuilderInfos.Add( info );
            }

            // Cache mod builders compatible with each game
            mModBuilderInfoByGame = new Dictionary< Game, List< ModBuilderInfo > >();
            var genericModBuilders = modBuilderInfos.Where( x => x.IsGeneric ).ToList();
            foreach ( Game game in Enum.GetValues(typeof(Game)) )
            {
                var gameSpecificModBuilders = modBuilderInfos.Where( x => x.IsForGame( game ) ).ToList();
                var compatibleModBuilders = new List< ModBuilderInfo >();
                compatibleModBuilders.AddRange( gameSpecificModBuilders );
                compatibleModBuilders.AddRange( genericModBuilders );
                mModBuilderInfoByGame[game] = compatibleModBuilders;
            }

            // Cache mod builder infos by type
            mModBuilderInfoByType = modBuilderInfos.ToDictionary( x => x.Type );
        }

        public static IEnumerable<ModBuilderInfo> GetCompatibleModBuilders( Game game )
        {
            return mModBuilderInfoByGame[ game ];
        }

        public static ModBuilderInfo GetModBuilderInfo<T>() where T : IModBuilder
        {
            return mModBuilderInfoByType[ typeof(T) ];
        }
    }
}
