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
            get => mConfig.EnabledMods.Contains( mMod );
            set
            {
                if ( value )
                {
                    mConfig.EnableMod( mMod );
                }
                else
                {
                    mConfig.DisableMod( mMod );
                }
            }
        }

        public string Title => mMod.Title;

        public string Description => mMod.Description;

        public string Version => mMod.Version;

        public string Date => mMod.Date;

        public string Author => mMod.Author;

        public string Url => mMod.Url;

        public string UpdateUrl => mMod.UpdateUrl;

        public ModViewModel(Mod model)
        {
            mMod = model;
            mConfig = Config.Get( model.Game );
        }

        public static explicit operator Mod(ModViewModel viewModel) => viewModel.mMod;
    }
}
