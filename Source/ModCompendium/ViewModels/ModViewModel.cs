using System;
using System.Linq;
using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.ModSystem;

namespace ModCompendium.ViewModels
{
    public class ModViewModel
    {
        private readonly Mod mMod;
        private readonly GameConfig mConfig;

        public bool Enabled
        {
            get => mConfig.IsModEnabled( Id );
            set
            {
                if ( value )
                {
                    mConfig.EnableMod( Id );
                }
                else
                {
                    mConfig.DisableMod( Id );
                }
            }
        }

        public string Title
        {
            get => mMod.Title;
            set => mMod.Title = value;
        }

        public string Description
        {
            get => mMod.Description;
            set => mMod.Description = value;
        }

        public string Version
        {
            get => mMod.Version;
            set => mMod.Version = value;
        }

        public string Author
        {
            get => mMod.Author;
            set => mMod.Author = value;
        }

        public string Date
        {
            get => mMod.Date;
            set => mMod.Date = value;
        }

        public string Url
        {
            get => mMod.Url;
            set => mMod.Url = value;
        }

        public string UpdateUrl
        {
            get => mMod.UpdateUrl;
            set => mMod.UpdateUrl = value;
        }

        public Guid Id => mMod.Id;

        public ModViewModel( Mod model )
        {
            mMod = model;
            mConfig = ConfigStore.Get( model.Game );
        }

        public static explicit operator ModViewModel( Mod mod ) => new ModViewModel( mod );

        public static explicit operator Mod( ModViewModel viewModel ) => viewModel.mMod;
    }
}
