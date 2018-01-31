using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ModCompendiumLibrary.ModSystem;

namespace ModCompendiumLibrary.Configuration
{
    public abstract class GameConfig : IConfigurable
    {
        private readonly List< Mod > mEnabledMods;

        public abstract Game Game { get; }

        public string OutputDirectoryPath { get; set; }

        public List<Mod> EnabledMods => mEnabledMods;

        protected GameConfig()
        {
            OutputDirectoryPath = $"Output\\{Game}";
            mEnabledMods = new List< Mod >();
        }

        public void SetModPriority( Mod mod, int priority )
        {
            if ( !mEnabledMods.Contains( mod ) )
                throw new ArgumentException( "Mod isn't enabled", nameof( mod ) );

            mEnabledMods.Remove( mod );
            mEnabledMods.Insert( priority, mod );
        }

        public int GetModPriority( Mod mod )
        {
            if ( !mEnabledMods.Contains( mod ) )
                throw new ArgumentException( "Mod isn't enabled", nameof( mod ) );

            return mEnabledMods.IndexOf( mod );
        }

        public void EnableMod( Mod mod )
        {
            if ( !mEnabledMods.Contains( mod ) )
            {
                mEnabledMods.Add( mod );
            }
        }

        public void DisableMod( Mod mod )
        {
            mEnabledMods.Remove( mod );
        }

        // Serialization
        void IConfigurable.Deserialize( XElement element )
        {
            OutputDirectoryPath = SerializationHelper.GetValueOrEmpty( element, nameof( OutputDirectoryPath ) );
            var enabledModsElement = element.Element( nameof( EnabledMods ) );
            if ( enabledModsElement != null )
                DeserializeEnabledMods( enabledModsElement );

            DeserializeCore( element );
        }

        private void DeserializeEnabledMods( XElement element )
        {
            foreach ( var enabledModElement in element.Elements() )
            {
                if ( enabledModElement.Name == "EnabledMod" )
                {
                    int id = int.Parse( enabledModElement.Value );

                    if ( ModDatabase.TryGet( id, out var value ) )
                    {
                        mEnabledMods.Add( value );
                    }
                }
            }
        }

        protected abstract void DeserializeCore( XElement element );

        void IConfigurable.Serialize( XElement element )
        {
            element.Add( new XElement( nameof( OutputDirectoryPath ), OutputDirectoryPath ) );
            element.Add( SerializeEnabledMods() );

            SerializeCore( element );
        }

        private XElement SerializeEnabledMods()
        {
            var enabledModsElement = new XElement( nameof( EnabledMods ) );
            foreach ( var enabledMod in EnabledMods )
            {
                enabledModsElement.Add( new XElement( "EnabledMod", enabledMod.Id ) );
            }

            return enabledModsElement;
        }

        protected abstract void SerializeCore( XElement element );
    }
}
