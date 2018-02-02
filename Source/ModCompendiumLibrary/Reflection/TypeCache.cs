using System;
using System.Collections.Generic;

namespace ModCompendiumLibrary.Reflection
{
    public static class TypeCache
    {
        public static List<Type> Types { get; }

        static TypeCache()
        {
            Types = new List< Type >();
            foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies() )
            {
                Types.AddRange( assembly.GetTypes() );
            }
        }
    }
}
