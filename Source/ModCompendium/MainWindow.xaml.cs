using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ModCompendium.Configs;
using ModCompendium.ViewModels;
using ModCompendiumLibrary;
using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.ModSystem;
using ModCompendiumLibrary.ModSystem.Builders;
using ModCompendiumLibrary.ModSystem.Builders.Utilities;
using ModCompendiumLibrary.ModSystem.Loaders;
using ModCompendiumLibrary.ModSystem.Mergers;
using MessageBox = Xceed.Wpf.Toolkit.MessageBox;

namespace ModCompendium
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string[] sGameNames =
        {
            "Persona 3",
            "Persona 3 Portable",
            "Persona 4",
            "Persona 4 Golden",
            "Persona 4 Dancing All Night",
            "Persona 5",
            "Persona 3 Dancing Moon Night",
            "Persona 5 Dancing Star Night",
            "Persona Q",
            "Persona Q2",
            "Catherine Full Body"
        };

        public Game SelectedGame { get; private set; }

        public List<ModViewModel> Mods { get; private set; }

        public GameConfig GameConfig { get; private set; }

        public MainWindowConfig Config { get; private set; }

        public ModViewModel SelectedModViewModel => ( ModViewModel ) ModGrid.SelectedItem;

        public Mod SelectedMod => ( Mod )SelectedModViewModel;

        public MainWindow()
        {
            InitializeComponent();
            InitializeLog();

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"Mod Compendium {version.Major}.{version.Minor}.{version.Revision}";
            Config = ConfigStore.Get<MainWindowConfig>();
            InitializeGameComboBox();
        }

        private void InitializeLog()
        {
            Log.MessageBroadcasted += Log_MessageBroadcasted;
        }

        private void InitializeGameComboBox()
        {
            GameComboBox.ItemsSource = sGameNames;
            GameComboBox.SelectedIndex = Math.Max( 0, ( int ) Config.SelectedGame - 1 );
        }

        private void RefreshMods()
        {
            Mods = ModDatabase.Get( SelectedGame )
                              .OrderBy( x => GameConfig.GetModPriority( x.Id ) )
                              .Select( x => new ModViewModel( x ) )
                              .ToList();

            ModGrid.ItemsSource = Mods;
        }

        private void RefreshModDatabase()
        {
            ModDatabase.Initialize();
            RefreshMods();
        }

        private bool UpdateGameConfigEnabledMods()
        {
            var enabledMods = Mods.Where( x => x.Enabled )
                                  .Select( x => x.Id )
                                  .ToList();

            GameConfig.ClearEnabledMods();

            if ( enabledMods.Count == 0 )
                return false;

            enabledMods.ForEach( GameConfig.EnableMod );

            return true;
        }

        private void UpdateWindowConfigModOrder()
        {
            for ( var i = 0; i < Mods.Count; i++ )
            {
                var mod = Mods[i];
                GameConfig.SetModPriority( mod.Id, i );
            }
        }

        private void UpdateConfigChangesAndSave()
        {
            UpdateGameConfigEnabledMods();
            UpdateWindowConfig();
            ConfigStore.Save();
        }

        private void UpdateWindowConfig()
        {
            UpdateWindowConfigModOrder();
            Config.SelectedGame = SelectedGame;
        }

        // Events
        private static void InvokeOnUIThread( Action action )
        {
            Application.Current.Dispatcher.BeginInvoke( action );
        }

        private void Log_MessageBroadcasted( object sender, MessageBroadcastedEventArgs e )
        {
            if ( e.Severity == Severity.Trace )
                return;

            // Invoke on UI thread
            InvokeOnUIThread( () =>
            {
                SolidColorBrush color;
                string severityIndicator;

                switch ( e.Severity )
                {
                    case Severity.Trace:
                        color = Brushes.Gray;
                        severityIndicator = "T";
                        break;
                    case Severity.Warning:
                        color = Brushes.Orange;
                        severityIndicator = "!";
                        break;
                    case Severity.Error:
                        color = Brushes.Red;
                        severityIndicator = "E";
                        break;
                    case Severity.Fatal:
                        color = Brushes.Magenta;
                        severityIndicator = "F";
                        break;

                    default:
                        color = Brushes.Black;
                        severityIndicator = "I";
                        break;
                }

                var textRange = new TextRange( LogTextBox.Document.ContentEnd, LogTextBox.Document.ContentEnd )
                {
                    Text = $"[{e.Channel.Name}] {severityIndicator}: {e.Message}\n"
                };

                textRange.ApplyPropertyValue( TextElement.ForegroundProperty, color );
            } );
        }

        protected override void OnClosed( EventArgs e )
        {
            UpdateConfigChangesAndSave();
        }

        private void GameComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            SelectedGame = ( Game )( GameComboBox.SelectedIndex + 1 );
            GameConfig = ConfigStore.Get( SelectedGame );
            RefreshMods();
        }

        private void SettingsButton_Click( object sender, RoutedEventArgs e )
        {
            var settingsWindow = new GameConfigWindow( GameConfig ) { Owner = this };
            settingsWindow.ShowDialog();
        }

        private void BuildButton_Click( object sender, RoutedEventArgs e )
        {
            if ( string.IsNullOrWhiteSpace( GameConfig.OutputDirectoryPath ) )
            {
                MessageBox.Show( this, "Please specify an output directory in the settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
                return;
            }

            if ( Mods.Count == 0 )
            {
                MessageBox.Show( this, "No mods are available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
                return;
            }

            if ( !UpdateGameConfigEnabledMods() )
            {
                MessageBox.Show( this, "No mods are enabled.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
                return;
            }

            var task = Task.Factory.StartNew( () =>
            {
                var enabledMods = GameConfig.ModConfigs.Where( x => x.Enabled )
                                            .OrderBy( x => x.Priority )
                                            .Select( x => x.ModId )
                                            .Select( x => ModDatabase.Get( x ) )
                                            .ToList();

                Log.General.Info( "Building mods:" );
                foreach ( var enabledMod in enabledMods )
                    Log.General.Info( $"\t{enabledMod.Title}" );

                // Run prebuild scripts
                RunModScripts( enabledMods, "prebuild.bat" );

                var merger = new TopToBottomModMerger();
                var merged = merger.Merge( enabledMods );

                // Todo
                var builder = ModBuilderManager.GetCompatibleModBuilders( SelectedGame ).First().Create();
                if ( UltraISOUtility.Available )
                {
                    if ( SelectedGame == Game.Persona3 )
                        builder = new Persona3IsoModBuilder();
                    else if ( SelectedGame == Game.Persona4 )
                        builder = new Persona4IsoModBuilder();
                }

                Log.General.Info( $"Output path: {GameConfig.OutputDirectoryPath}" );

#if !DEBUG
                try
#endif
                {
                    builder.Build( merged, GameConfig.OutputDirectoryPath );
                }
 #if !DEBUG
                catch ( InvalidConfigException exception )
                {
                    InvokeOnUIThread(
                        () => MessageBox.Show( this, $"SelectedGame configuration is invalid.\n{exception.Message}", "Error",
                                               MessageBoxButton.OK, MessageBoxImage.Error ) );

                    return false;
                }
                catch ( MissingFileException exception )
                {
                    InvokeOnUIThread(
                        () => MessageBox.Show( this, $"A file is missing:\n{exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error ) );

                    return false;
                }
                catch ( Exception exception )
                {
                    InvokeOnUIThread(
                        () => 
                        MessageBox.Show(
                            this, $"Unhandled exception occured while building:\n{exception.Message}\n{exception.StackTrace}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error ) );

#if DEBUG
                    throw;
#endif

#pragma warning disable 162
                    return false;
#pragma warning restore 162
                }
#endif

                return true;
            }, TaskCreationOptions.AttachedToParent );

            task.ContinueWith( ( t ) =>
            {
                InvokeOnUIThread( () =>
                {
                    if ( t.Result )
                    {
                        MessageBox.Show(this, "Done building!", "Done", MessageBoxButton.OK, MessageBoxImage.None);
                        RunPostBuildScript( "postbuild.bat" );
                    }
                } );
            } );
        }

        private static void RunModScripts( List<Mod> enabledMods, string scriptFileName )
        {
            foreach ( var enabledMod in enabledMods )
            {
                var scriptFilePath = Path.Combine( enabledMod.BaseDirectory, scriptFileName );
                if ( File.Exists( scriptFilePath ) )
                {
                    try
                    {
                        var info = new ProcessStartInfo( Path.GetFullPath( scriptFilePath ) );
                        info.WorkingDirectory = Path.GetFullPath( enabledMod.BaseDirectory );

                        var process = Process.Start( info );
                        process?.WaitForExit();
                    }
                    catch ( Exception )
                    {
                    }
                }
            }
        }

        private static void RunPostBuildScript(string scriptFileName)
        {
            var scriptFilePath = Path.Combine(Directory.GetCurrentDirectory(), scriptFileName);
            if (File.Exists(scriptFilePath))
            {
                try
                {
                    var info = new ProcessStartInfo(Path.GetFullPath(scriptFilePath));
                    info.WorkingDirectory = Path.GetFullPath(Directory.GetCurrentDirectory());

                    var process = Process.Start(info);
                    process?.WaitForExit();
                }
                catch (Exception)
                {
                }
            }
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
            GameConfig.SetModPriority( SelectedModViewModel.Id, targetIndex );
            GameConfig.SetModPriority( target.Id, selectedIndex );

            // Gui update
            Mods.Remove( SelectedModViewModel );
            Mods.Insert( targetIndex, SelectedModViewModel );
            ModGrid.Items.Refresh();
            ModGrid.SelectedIndex = targetIndex;
        }

        private void LogTextBox_TextChanged( object sender, TextChangedEventArgs e )
        {
            LogTextBox.ScrollToEnd();
        }

        private void RefreshButton_Click( object sender, RoutedEventArgs e )
        {
            // Save
            UpdateConfigChangesAndSave();

            // Reload
            ConfigStore.Load();
            RefreshModDatabase();
        }

        private void NewButton_Click( object sender, RoutedEventArgs e )
        {
            var newMod = new NewModDialog() { Owner = this };
            var result = newMod.ShowDialog();

            if ( !result.HasValue || !result.Value )
                return;

            // Get unique directory
            string modPath = Path.Combine( ModDatabase.ModDirectory, SelectedGame.ToString(), newMod.ModTitle );
            if ( Directory.Exists( modPath ) )
            {
                var newModPath = modPath;
                int i = 0;

                while ( Directory.Exists( newModPath ) )
                    newModPath = modPath + "_" + i++;

                modPath = newModPath;
            }

            // Build mod
            var mod = new ModBuilder()
                .SetGame( SelectedGame )
                .SetTitle( newMod.ModTitle )
                .SetDescription( newMod.Description )
                .SetVersion( newMod.Version )
                .SetDate( DateTime.UtcNow.ToShortDateString() )
                .SetAuthor( newMod.Author )
                .SetUrl( newMod.Url )
                .SetUpdateUrl( newMod.UpdateUrl )
                .SetBaseDirectoryPath(modPath)
                .Build();

            // Do actual saving
            var modLoader = new XmlModLoader();
            modLoader.Save( mod );

            // Reload
            RefreshModDatabase();
        }

        private void DeleteButton_Click( object sender, RoutedEventArgs e )
        {
            DeleteSelectedMod();
        }

        private void DeleteSelectedMod()
        {
            if ( MessageBox.Show( this, "Are you sure you want to delete this mod? The data will be lost forever.", "Warning",
                                  MessageBoxButton.OKCancel,
                                  MessageBoxImage.Exclamation ) == MessageBoxResult.OK )
            {
                Log.General.Warning( $"Deleting mod directory: {SelectedMod.BaseDirectory}" );
                Directory.Delete( SelectedMod.BaseDirectory, true );
                RefreshModDatabase();
            }
        }

        private void DataGridContextMenuOpenDirectory_Click( object sender, RoutedEventArgs e )
        {
            Process.Start( SelectedMod.BaseDirectory );
        }

        private void DataGridContextMenuMoveUp_Click( object sender, RoutedEventArgs e )
        {
            UpOrDownButtonClick( true );
        }

        private void DataGridContextMenuMoveDown_Click( object sender, RoutedEventArgs e )
        {
            UpOrDownButtonClick( false );
        }

        private void DataGridContextMenuDelete_Click( object sender, RoutedEventArgs e )
        {
            DeleteSelectedMod();
        }
    }
}
