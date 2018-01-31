using System;
using System.Windows;
using System.Windows.Controls;
using ModCompendiumLibrary;
using ModCompendiumLibrary.ModSystem;

namespace ModCompendium
{
    /// <summary>
    /// Interaction logic for NewModDialog.xaml
    /// </summary>
    public partial class NewModDialog : Window
    {
        private Game mGame;

        public Mod Mod { get; private set; }

        public NewModDialog(Game game)
        {
            InitializeComponent();
            mGame = game;
        }

        private void OkButton_Click( object sender, RoutedEventArgs e )
        {
            try
            {
                Mod = new ModBuilder()
                    .SetGame(mGame)
                    .SetTitle( TitleTextBox.Text )
                    .SetDescription( DescriptionTextBox.Text )
                    .SetVersion( VersionTextBox.Text )
                    .SetDate( DateTime.UtcNow.ToShortDateString() )
                    .SetAuthor( AuthorTextBox.Text )
                    .SetUrl( URLTextBox.Text )
                    .SetUpdateUrl( UpdateURLTextBox.Text )
                    .Build();
            }
            catch ( Exception exception )
            {
                DialogResult = false;
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click( object sender, RoutedEventArgs e )
        {
            Mod = null;
            DialogResult = false;
            Close();
        }

        private void TitleTextBox_TextChanged( object sender, TextChangedEventArgs e )
        {
            if ( TitleTextBox.Text.Length != 0 )
            {
                OkButton.IsEnabled = true;
            }
        }
    }
}
