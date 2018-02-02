using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ModCompendiumLibrary.ModSystem;

namespace ModCompendiumLibrary.Configuration
{
    public abstract class GameConfig : IConfigurable
    {
        private readonly List< Guid > mEnabledModIds;

        public abstract Game Game { get; }

        public string OutputDirectoryPath { get; set; }

        public IEnumerable< Guid > EnabledModIds => mEnabledModIds;

        protected GameConfig()
        {
            OutputDirectoryPath = $"Output\\{Game}";
            mEnabledModIds = new List< Guid >();
        }

        public void SetModPriority( Guid modId, int priority )
        {
            if ( !EnabledModIds.Contains( modId ) )
            {
                throw new ArgumentException( "Mod isn't enabled", nameof( modId ) );
            }

            mEnabledModIds.Remove( modId );
            mEnabledModIds.Insert( priority, modId );
        }

        public int GetModPriority( Guid modId )
        {
            if ( !EnabledModIds.Contains( modId ) )
            {
                throw new ArgumentException( "Mod isn't enabled", nameof( modId ) );
            }

            return mEnabledModIds.IndexOf( modId );
        }

        public bool IsModEnabled( Guid modId )
        {
            return mEnabledModIds.Contains( modId );
        }

        public void EnableMod( Guid modId )
        {
            if ( !EnabledModIds.Contains( modId ) )
            {
                mEnabledModIds.Add( modId );
            }
        }

        public void DisableMod( Guid modId )
        {
            mEnabledModIds.Remove( modId );
        }

        public void ClearEnabledMods()
        {
            mEnabledModIds.Clear();
        }

        private void DeserializeEnabledMods( XElement element )
        {
            foreach ( var enabledModElement in element.Elements() )
            {
                if ( Guid.TryParse( enabledModElement.Value, out var id ) && id != Guid.Empty && ModDatabase.Exists( id ) )
                {
                    mEnabledModIds.Add( id );
                }
            }
        }

        protected abstract void DeserializeCore( XElement element );

        private XElement SerializeEnabledMods()
        {
            var enabledModsElement = new XElement( nameof( EnabledModIds ) );
            foreach ( var modId in EnabledModIds )
                enabledModsElement.Add( new XElement( "EnabledModId", modId ) );

            return enabledModsElement;
        }

        protected abstract void SerializeCore( XElement element );

        // Serialization
        void IConfigurable.Deserialize( XElement element )
        {
            var outputDirectoryElement = element.Element( nameof( OutputDirectoryPath ) );
            if ( outputDirectoryElement != null )
            {
                OutputDirectoryPath = outputDirectoryElement.Value;
            }

            var enabledModsElement = element.Element( nameof( EnabledModIds ) );
            if ( enabledModsElement != null )
            {
                DeserializeEnabledMods( enabledModsElement );
            }

            DeserializeCore( element );
        }

        void IConfigurable.Serialize( XElement element )
        {
            element.Add( new XElement( nameof( OutputDirectoryPath ), OutputDirectoryPath ) );
            element.Add( SerializeEnabledMods() );

            SerializeCore( element );
        }
    }
}
