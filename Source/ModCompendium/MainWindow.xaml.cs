using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ModCompendium.GuiConfig;
using ModCompendium.ViewModels;
using ModCompendiumLibrary;
using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.ModSystem;
using ModCompendiumLibrary.ModSystem.Builders;
using ModCompendiumLibrary.ModSystem.Mergers;
using MessageBox = Xceed.Wpf.Toolkit.MessageBox;

namespace ModCompendium
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Game Game { get; private set; }

        public List<ModViewModel> Mods { get; private set; }

        public GameConfig GameConfig { get; private set; }

        public ModOrderGuiConfig OrderConfig { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += ( s, e ) =>
            {
                Application.Current.Dispatcher.Invoke(
                    () =>
                    {
                        if ( e.ExceptionObject is Exception exception )
                        {
                            MessageBox.Show(
                                this, $"Unhandled exception occured:\n{exception.Message}\n{exception.StackTrace}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error );
                        }
                        else
                        {
                            MessageBox.Show( this, "Unhandled exception occured (no info)", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
                        }
                    } );
            };

            Log.MessageBroadcasted += Log_MessageBroadcasted;

            GameComboBox.ItemsSource = Enum.GetValues( typeof( Game ) ).Cast< Game >();
            GameComboBox.SelectedIndex = 0;
            OrderConfig = Config.Get<ModOrderGuiConfig>();
        }

        private void Log_MessageBroadcasted( object sender, MessageBroadcastedEventArgs e )
        {
            // Invoke on UI thread
            Application.Current.Dispatcher.Invoke( () =>
            {
                SolidColorBrush color;

                switch ( e.Severity )
                {
                    case Severity.Trace:
                        color = Brushes.Gray;
                        break;
                    case Severity.Info:
                        color = Brushes.Black;
                        break;
                    case Severity.Warning:
                        color = Brushes.Yellow;
                        break;
                    case Severity.Error:
                        color = Brushes.Red;
                        break;
                    case Severity.Fatal:
                        color = Brushes.Magenta;
                        break;

                    default:
                        color = Brushes.Black;
                        break;
                }

                var textRange = new TextRange( LogTextBox.Document.ContentEnd, LogTextBox.Document.ContentEnd )
                {
                    Text = $"[{e.Channel.Name}] {e.Severity}: {e.Message}\n"
                };

                textRange.ApplyPropertyValue( TextElement.ForegroundProperty, color );
            } );
        }

        protected override void OnClosed( EventArgs e )
        {
            CommitEnabledMods();
            Config.Save();
        }

        private void RefreshMods()
        {
            Mods = ModDatabase.Get( Game )
                              .OrderBy( x =>
                              {
                                  if ( OrderConfig.ModOrder.TryGetValue( x.Id, out var order ) )
                                  {
                                      return order;
                                  }
                                  else
                                  {
                                      return OrderConfig.ModOrder[ x.Id ] = 0;
                                  }
                              })
                              .Select( x => new ModViewModel( x ) )
                              .ToList();

            ModGrid.ItemsSource = Mods;
        }

        private void RefreshModDatabase()
        {
            ModDatabase.Initialize();
            RefreshMods();
        }

        private void GameComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            Game = ( Game ) GameComboBox.SelectedValue;
            GameConfig = Config.Get( Game );
            RefreshMods();
        }

        private void SettingsButton_Click( object sender, RoutedEventArgs e )
        {
            var settingsWindow = new GameConfigWindow( GameConfig ) { Owner = this };
            settingsWindow.ShowDialog();
        }

        private bool CommitEnabledMods()
        {
            var enabledMods = Mods.Where( x => x.Enabled )
                                  .Select( x => x.Id )
                                  .ToList();

            GameConfig.EnabledModIds.Clear();

            if ( enabledMods.Count == 0 )
                return false;

            GameConfig.EnabledModIds.AddRange( enabledMods );

            return true;
        }

        private void BuildButton_Click( object sender, RoutedEventArgs e )
        {
            if ( string.IsNullOrWhiteSpace(GameConfig.OutputDirectoryPath) )
            {
                MessageBox.Show( this, "Please specify an output directory in the settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
                return;
            }

            if ( Mods.Count == 0 )
            {
                MessageBox.Show( this, "No mods are available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
                return;
            }

            if ( !CommitEnabledMods() )
            {
                MessageBox.Show( this, "No mods are enabled.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
                return;
            }

            var task = Task.Factory.StartNew( () =>
            {
                var enabledMods = GameConfig.EnabledModIds.Select( ModDatabase.Get )
                                            .ToList();

                Log.General.Info( "Building mods:" );
                foreach ( var enabledMod in enabledMods )
                {
                    Log.General.Info( $"\t{enabledMod.Title}" );
                }

                var merger = new TopToBottomModMerger();
                var merged = merger.Merge( enabledMods );
                var builder = GameModBuilder.Get( Game );

                try
                {
                    builder.Build( merged, GameConfig.OutputDirectoryPath );
                }
                catch ( InvalidConfigException exception )
                {
                    Application.Current.Dispatcher.Invoke(
                        () => MessageBox.Show( this, $"Game configuration is invalid.\n{exception.Message}", "Error",
                                               MessageBoxButton.OK, MessageBoxImage.Error ) );

                    return false;
                }
                catch ( MissingFileException exception )
                {
                    Application.Current.Dispatcher.Invoke(
                        () => MessageBox.Show( this, $"A file is missing:\n{exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error ) );

                    return false;
                }
                catch ( Exception exception )
                {
                    Application.Current.Dispatcher.Invoke(
                        () => MessageBox.Show(
                            this, $"Unhandled exception occured while building:\n{exception.Message}\n{exception.StackTrace}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error ) );

#if DEBUG
                    throw;
#endif

#pragma warning disable 162
                    return false;
#pragma warning restore 162
                }

                return true;
            }, TaskCreationOptions.LongRunning );

            task.ContinueWith( ( t ) =>
            {
                Application.Current.Dispatcher.Invoke( () =>
                {
                    if (t.Result)
                        MessageBox.Show( this, "Done building!", "Done", MessageBoxButton.OK, MessageBoxImage.None );
                } );
            } );
        }

        private void ModGrid_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.Key == Key.Down )
            {
                ModGrid.CommitEdit();
                ++ModGrid.SelectedIndex;
            }
            else if ( e.Key == Key.Up )
            {
                ModGrid.CommitEdit();
                --ModGrid.SelectedIndex;
            }
        }

        private void UpButton_Click( object sender, RoutedEventArgs e )
        {
            UpOrDownButtonClick( true );
        }

        private void DownButton_Click( object sender, RoutedEventArgs e )
        {
            UpOrDownButtonClick( false );
        }

        private void UpOrDownButtonClick( bool isUp )
        {
            var selected = ( ModViewModel )ModGrid.SelectedValue;
            var selectedMod = ( Mod )selected;
            var selectedIndex = ModGrid.SelectedIndex;
            int targetIndex;

            if ( isUp )
            {
                targetIndex = selectedIndex - 1;
                if ( targetIndex < 0 )
                    return;
            }
            else
            {
                targetIndex = selectedIndex + 1;
                if ( targetIndex >= ModGrid.Items.Count )
                    return;
            }

            var target = ( Mod )( ModViewModel )ModGrid.Items[targetIndex];

            // Order
            OrderConfig.ModOrder[selectedMod.Id] = targetIndex;
            OrderConfig.ModOrder[target.Id] = selectedIndex;

            // Gui update
            Mods.Remove( selected );
            Mods.Insert( targetIndex, selected );
            ModGrid.Items.Refresh();
            ModGrid.SelectedIndex = targetIndex;
        }

        private void LogTextBox_TextChanged( object sender, TextChangedEventArgs e )
        {
            LogTextBox.ScrollToEnd();
        }

        private void RefreshButton_Click( object sender, RoutedEventArgs e )
        {
            CommitEnabledMods();
            Config.Save();
            Config.Load();
            RefreshModDatabase();
        }
    }
}
