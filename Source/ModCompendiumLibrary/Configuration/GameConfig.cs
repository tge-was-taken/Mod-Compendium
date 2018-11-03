using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ModCompendiumLibrary.ModSystem;

namespace ModCompendiumLibrary.Configuration
{
    public abstract class GameConfig : IConfigurable
    {
        private readonly Dictionary<Guid, ModConfig> mModConfigs;

        public abstract Game Game { get; }

        public string OutputDirectoryPath { get; set; }

        public IEnumerable<ModConfig> ModConfigs => mModConfigs.Values;

        protected GameConfig()
        {
            OutputDirectoryPath = $"Output\\{Game}";

            mModConfigs = new Dictionary<Guid, ModConfig>();

            // Initialize default mod configs for game mods in the database
            int priority = 0;
            foreach ( var mod in ModDatabase.Get( Game ) )
            {
                AddModConfig( mod.Id, priority++, false );
            }
        }

        public ModConfig GetModConfig( Guid id )
        {
            if ( !mModConfigs.TryGetValue( id, out var enabledMod ) )
            {
                enabledMod = mModConfigs[ id ] = new ModConfig( id, GetNextAvailableModPriority(), false );
            }

            return enabledMod;
        }

        public void SetModPriority( Guid modId, int priority )
        {
            GetModConfig( modId ).Priority = priority;
        }

        public int GetModPriority( Guid modId )
        {
            return GetModConfig( modId ).Priority;
        }

        public bool HasModConfig( Guid modId )
        {
            return mModConfigs.ContainsKey( modId );
        }

        public bool IsModEnabled( Guid modId )
        {
            return HasModConfig( modId ) && GetModConfig( modId ).Enabled;
        }

        public void EnableMod( Guid modId )
        {
            if ( !mModConfigs.TryGetValue( modId, out var enabledMod ) )
            {
                mModConfigs[ modId ] = new ModConfig( modId, GetNextAvailableModPriority(), true );
            }
            else
            {
                enabledMod.Enabled = true;
            }
        }

        public void AddModConfig( Guid modId, int priority, bool enabled )
        {
            mModConfigs[ modId ] = new ModConfig( modId, priority, enabled );
        }

        public void DisableMod( Guid modId )
        {
            var mod = GetModConfig( modId );
            mod.Enabled = false;
        }

        public void ClearEnabledMods() => mModConfigs.Clear();

        public int GetNextAvailableModPriority()
        {
            var usedPriorities = ModConfigs.Select( x => x.Priority ).Distinct().ToDictionary( x => x );
            var maxPriority = usedPriorities.Count == 0 ? 0 : usedPriorities.Values.Max();

            int priority;
            for ( priority = 0; priority < maxPriority + 1; priority++ )
            {
                if ( !usedPriorities.ContainsKey( priority ) )
                    return priority;
            }

            return priority;
        }

        // Serialization
        void IConfigurable.Deserialize( XElement element )
        {
            var xOutputDirectoryPath = element.Element( nameof( OutputDirectoryPath ) );
            if ( xOutputDirectoryPath != null )
                OutputDirectoryPath = xOutputDirectoryPath.Value;

            var xModConfigs = element.Element( nameof( ModConfigs ) );
            if ( xModConfigs != null )
            {
                DeserializeEnabledMods( xModConfigs );
            }
            else
            {
                // Legacy
                if ( ( xModConfigs = element.Element( "EnabledModIds" ) ) != null )
                {
                    DeserializeEnabledModsLegacy( xModConfigs );
                }
            }

            DeserializeCore( element );
        }

        private void DeserializeEnabledModsLegacy( XElement element )
        {
            foreach ( var xModId in element.Elements() )
            {
                if ( Guid.TryParse( xModId.Value, out var modId ) &&
                     modId != Guid.Empty &&
                     ModDatabase.Exists( modId ) )
                {
                    mModConfigs[ modId ] = new ModConfig( modId, GetNextAvailableModPriority(), true );
                }
            }
        }

        private void DeserializeEnabledMods( XElement element )
        {
            foreach ( var xModConfig in element.Elements() )
            {
                var modConfig = ModConfig.Deserialize( xModConfig );

                if ( modConfig.ModId != Guid.Empty &&
                     ModDatabase.Exists( modConfig.ModId ) )
                {
                    mModConfigs[modConfig.ModId] = modConfig;
                }
            }
        }

        protected abstract void DeserializeCore( XElement element );

        void IConfigurable.Serialize( XElement element )
        {
            element.AddNameValuePair( nameof( OutputDirectoryPath ), OutputDirectoryPath );
            element.Add( SerializeEnabledMods() );

            SerializeCore( element );
        }

        private XElement SerializeEnabledMods()
        {
            var modConfigsElement = new XElement( nameof( ModConfigs ) );
            foreach ( var mod in ModConfigs )
                modConfigsElement.Add( mod.Serialize() );

            return modConfigsElement;
        }

        protected abstract void SerializeCore( XElement element );
    }
}
