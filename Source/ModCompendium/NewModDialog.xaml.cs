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
        public string ModTitle => TitleTextBox.Text;

        public string Description => DescriptionTextBox.Text;

        public string Version => VersionTextBox.Text;

        public string Author => AuthorTextBox.Text;

        public string Url => URLTextBox.Text;

        public string UpdateUrl => UpdateURLTextBox.Text;

        public NewModDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = false;
            Close();
        }

        private void TitleTextBox_TextChanged( object sender, TextChangedEventArgs e )
        {
            OkButton.IsEnabled = TitleTextBox.Text.Length != 0;
        }
    }
}
