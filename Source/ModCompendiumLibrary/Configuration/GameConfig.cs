using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ModCompendiumLibrary.ModSystem;

namespace ModCompendiumLibrary.Configuration
{
    public abstract class GameConfig : IConfigurable
    {
        public abstract Game Game { get; }

        public string OutputDirectoryPath { get; set; }

        public List<int> EnabledModIds { get; private set; }

        protected GameConfig()
        {
            OutputDirectoryPath = $"Output\\{Game}";
            EnabledModIds = new List< int >();
        }

        public void SetModPriority( int modId, int priority )
        {
            if ( !EnabledModIds.Contains( modId ) )
                throw new ArgumentException( "Mod isn't enabled", nameof( modId ) );

            EnabledModIds.Remove( modId );
            EnabledModIds.Insert( priority, modId );
        }

        public int GetModPriority( int modId )
        {
            if ( !EnabledModIds.Contains( modId ) )
                throw new ArgumentException( "Mod isn't enabled", nameof( modId ) );

            return EnabledModIds.IndexOf( modId );
        }

        public void EnableMod( int modId )
        {
            if ( !EnabledModIds.Contains(modId) )
            {
                EnabledModIds.Add( modId );
            }
        }

        public void DisableMod( int modId )
        {
            EnabledModIds.Remove( modId );
        }

        // Serialization
        void IConfigurable.Deserialize( XElement element )
        {
            OutputDirectoryPath = SerializationHelper.GetValueOrEmpty( element, nameof( OutputDirectoryPath ) );
            var enabledModsElement = element.Element( nameof( EnabledModIds ) );
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

                    if ( ModDatabase.Exists( id ) )
                    {
                        EnabledModIds.Add( id );
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
            var enabledModsElement = new XElement( nameof( EnabledModIds ) );
            foreach ( var modId in EnabledModIds )
            {
                enabledModsElement.Add( new XElement( "EnabledMod", modId ) );
            }

            return enabledModsElement;
        }

        protected abstract void SerializeCore( XElement element );
    }
}
