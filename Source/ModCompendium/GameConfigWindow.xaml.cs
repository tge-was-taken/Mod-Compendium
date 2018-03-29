using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.WindowsAPICodePack.Dialogs;
using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary;

namespace ModCompendium
{
    /// <summary>
    /// Interaction logic for GameConfigWindow.xaml
    /// </summary>
    public partial class GameConfigWindow : Window
    {
        private readonly GameConfig mConfig;

        public GameConfigWindow(GameConfig config)
        {
            InitializeComponent();
            DataContext = config;
            mConfig = config;

            // Add game specific settings
            if ( config.Game == Game.Persona3 || config.Game == Game.Persona4 )
            {
                var p34Config = ( Persona34GameConfig ) config;
                
                // Add extra row
                ConfigPropertyGrid.RowDefinitions.Add( new RowDefinition() );

                // Dvd root directory path label
                {
                    var dvdRootPathLabel = new Label()
                    {
                        Content = "ISO Path",
                        ToolTip = "Path to an unmodified ISO of " + config.Game.ToString(),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Height = 25,
                        Width = 120
                    };

                    Grid.SetRow( dvdRootPathLabel, 2 );
                    Grid.SetColumn( dvdRootPathLabel, 0 );
                    ConfigPropertyGrid.Children.Add( dvdRootPathLabel );
                }

                // Dvd root directory text box
                TextBox dvdRootPathTextBox;
                {
                    dvdRootPathTextBox = new TextBox()
                    {
                        VerticalContentAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Height = 20,
                        TextWrapping = TextWrapping.Wrap,
                        Width = 291,
                    };

                    dvdRootPathTextBox.SetBinding( TextBox.TextProperty, new Binding( nameof(Persona34GameConfig.DvdRootOrIsoPath) ) );

                    Grid.SetRow( dvdRootPathTextBox, 2 );
                    Grid.SetColumn( dvdRootPathTextBox, 1 );
                    ConfigPropertyGrid.Children.Add( dvdRootPathTextBox );
                }

                // Dvd root directory text box button
                {
                    var dvdRootPathTextBoxButton = new Button()
                    {
                        Content = "...",
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Width = 20,
                        Height = 20
                    };

                    dvdRootPathTextBoxButton.Click += ( s, e ) =>
                    {
                        var file = SelectFile( new CommonFileDialogFilter( "ISO file", ".iso" ) );
                        if ( file != null )
                        {
                            p34Config.DvdRootOrIsoPath = file;
                            dvdRootPathTextBox.GetBindingExpression( TextBox.TextProperty ).UpdateTarget();
                        }
                    };

                    Grid.SetRow( dvdRootPathTextBoxButton, 2 );
                    Grid.SetColumn( dvdRootPathTextBoxButton, 1 );
                    ConfigPropertyGrid.Children.Add( dvdRootPathTextBoxButton );
                }
            }
        }

        private void ButtonOk_Click( object sender, RoutedEventArgs e )
        {
            Close();
        }

        private void ButtonOutputDirectoryPath_Click( object sender, RoutedEventArgs e )
        {
            var directory = SelectDirectory();
            if ( directory != null )
            {
                mConfig.OutputDirectoryPath = directory;
                OutputDirectoryPathTextBox.GetBindingExpression( TextBox.TextProperty ).UpdateTarget();
            }
        }

        private string SelectDirectory()
        {
            var dialog = new CommonOpenFileDialog();
            dialog.AllowNonFileSystemItems = true;
            dialog.IsFolderPicker = true;
            dialog.EnsurePathExists = true;
            dialog.EnsureValidNames = true;
            dialog.DefaultFileName = "Select directory";
            dialog.Title = "Select directory";

            if ( dialog.ShowDialog() == CommonFileDialogResult.Ok )
            {
                return dialog.FileName;
            }

            return null;
        }
        private string SelectFile( params CommonFileDialogFilter[] filters )
        {
            var dialog = new CommonOpenFileDialog();
            dialog.AllowNonFileSystemItems = true;
            dialog.IsFolderPicker = false;
            dialog.EnsurePathExists = true;
            dialog.EnsureValidNames = true;
            dialog.DefaultFileName = "Select file";
            dialog.Title = "Select file";
            foreach ( var filter in filters )
                dialog.Filters.Add( filter );

            if ( dialog.ShowDialog() == CommonFileDialogResult.Ok )
            {
                return dialog.FileName;
            }

            return null;
        }
    }
}
