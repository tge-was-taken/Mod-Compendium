using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.IO;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public abstract class PS4PKGModBuilder : IModBuilder
    {
        protected abstract Game Game { get; }

        /// <inheritdoc />
        public VirtualFileSystemEntry Build(VirtualDirectory root, List<Mod> enabledMods, string hostOutputPath = null, string gameName = null, bool useCompression = false, bool useExtracted = false)
        {
            gameName = Game.ToString();

            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            //Get game config
            var config = ConfigStore.Get(Game) as PKGGameConfig ?? throw new InvalidOperationException("Game config is missing.");
            //Set cpk name
            string cpkName = "patch1R";
            if (config.Region.Split('-')[1] == "CUSA17416")
            {
                cpkName = "mod";
            }

            Log.Builder.Info($"Building {gameName} Mod");
            Log.Builder.Info("Processing mod files");

            var modFilesDirectory = new VirtualDirectory(null, cpkName);
            foreach (var entry in root)
            {
                if (entry.EntryType == VirtualFileSystemEntryType.Directory)
                {
                    var directory = (VirtualDirectory)entry;
                    var name = directory.Name.ToLowerInvariant();

                    switch (name)
                    {
                        case "mod":
                        case "data":
                            {
                                // Move files in 'cpk' directory to 'mod' directory
                                LogModFilesInDirectory(directory);

                                foreach (var modFileEntry in directory)
                                {
                                    modFileEntry.CopyTo(modFilesDirectory);
                                }
                            }
                            break;

                        default:
                            // Move directory to 'mod' directory
                            Log.Builder.Trace($"Adding directory {entry.FullName} to {cpkName}.cpk");
                            entry.CopyTo(modFilesDirectory);
                            break;
                    }
                }
                else
                {
                    // Move file to 'mod' directory
                    Log.Builder.Trace($"Adding file {entry.FullName} to {cpkName}.cpk");
                    entry.CopyTo(modFilesDirectory);
                }
            }

            bool.TryParse(config.Compression, out useCompression);

            // Build mod cpk
            Log.Builder.Info($"Building {cpkName}.cpk");
            var cpkModCompiler = new CpkModBuilder();

            if (hostOutputPath != null) Directory.CreateDirectory(hostOutputPath);

            var cpkFilePath = hostOutputPath != null ? Path.Combine(hostOutputPath, $"{cpkName}.cpk") : null;
            var cpkNotWritable = File.Exists(cpkFilePath) && FileHelper.IsFileInUse(cpkFilePath);
            var cpkFileBuildPath = hostOutputPath != null ? cpkNotWritable ? Path.Combine(Path.GetTempPath(), $"{cpkName}.cpk") : cpkFilePath : null;
            var cpkFile = cpkModCompiler.Build(modFilesDirectory, enabledMods, cpkFileBuildPath, gameName, useCompression);

            if (cpkFileBuildPath != cpkFilePath)
            {
                File.Copy(cpkFileBuildPath, cpkFilePath, true);
                File.Delete(cpkFileBuildPath);
                cpkFile = VirtualFile.FromHostFile(cpkFilePath);
            }

            //Build update pkg
            if (File.Exists(cpkFilePath))
            {
                string programPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string moddedCpk = $"{programPath}\\Dependencies\\GenGP4\\{config.Region.Split('-')[1]}-patch\\USRDIR\\{cpkName}.cpk";
                string outputPKG = $"{programPath}\\Output\\Persona5Royal\\{config.Region}_00-PERSONA5R0000000-A0101-V0100.pkg";

                Log.Builder.Info($"Building update .pkg");
                if (string.IsNullOrWhiteSpace(config.PKGPath))
                    throw new InvalidConfigException("Original game .pkg not specified.");
                if (!File.Exists(config.PKGPath))
                    throw new InvalidConfigException("Original game .pkg not found at specified location.");
                //Copy to patch folder
                if (File.Exists(moddedCpk))
                    File.Delete(moddedCpk);
                File.Copy(cpkFilePath, moddedCpk);
                using (WaitForFile(moddedCpk, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { };
                //Edit XML/SFO
                string mods = "";
                foreach (var mod in enabledMods)
                    mods += $"{mod.Title} v{mod.Version} by {mod.Author}\n";
                File.WriteAllText($"{programPath}\\Dependencies\\GenGP4\\{config.Region.Split('-')[1]}-patch\\sce_sys\\changeinfo\\changeinfo.xml", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<changeinfo>\n  <changes app_ver=\"01.01\">\n    <![CDATA[\n{mods}\n    ]]>\n  </changes>\n</changeinfo>");
                //Create GP4
                RunCMD($"{programPath}\\Dependencies\\GenGP4\\gengp4.exe", $"\"{programPath}\\Dependencies\\GenGP4\\{config.Region.Split('-')[1]}-patch\"");
                //Edit GP4 with path to PKG
                using (WaitForFile($"{programPath}\\Dependencies\\GenGP4\\{config.Region.Split('-')[1]}-patch.gp4", FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { };
                string[] gp4Lines = File.ReadAllLines($"{programPath}\\Dependencies\\GenGP4\\{config.Region.Split('-')[1]}-patch.gp4");
                for (int i = 0; i < gp4Lines.Length; i++)
                {
                    gp4Lines[i] = gp4Lines[i].Replace("Dependencies\\GenGP4\\", "").Replace(programPath + "\\", "");
                    if (gp4Lines[i].Contains("package content_id"))
                        gp4Lines[i] = $"    <package content_id=\"{config.Region}_00-PERSONA5R0000000\" passcode=\"00000000000000000000000000000000\" storage_type=\"digital25\" app_type=\"full\" app_path=\"{config.PKGPath}\"/>";
                }
                string gp4Text = string.Join("\n", gp4Lines);
                File.WriteAllText($"{programPath}\\Dependencies\\GenGP4\\{config.Region.Split('-')[1]}-patch-2.gp4", gp4Text);
                Thread.Sleep(1000);
                using (WaitForFile($"{programPath}\\Dependencies\\GenGP4\\{config.Region.Split('-')[1]}-patch-2.gp4", FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { };
                if (File.Exists($"{programPath}\\Dependencies\\GenGP4\\{config.Region.Split('-')[1]}-patch-2.gp4"))
                {
                    if (File.Exists($"{programPath}\\Dependencies\\GenGP4\\{config.Region.Split('-')[1]}-patch.gp4"))
                        File.Delete($"{programPath}\\Dependencies\\GenGP4\\{config.Region.Split('-')[1]}-patch.gp4");
                    File.Move($"{programPath}\\Dependencies\\GenGP4\\{config.Region.Split('-')[1]}-patch-2.gp4", $"{programPath}\\Dependencies\\GenGP4\\{config.Region.Split('-')[1]}-patch.gp4");
                }
                Thread.Sleep(1000);
                using (WaitForFile($"{programPath}\\Dependencies\\GenGP4\\{config.Region.Split('-')[1]}-patch.gp4", FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { };
                Thread.Sleep(1000);
                if (File.Exists(outputPKG))
                    File.Delete(outputPKG);
                //Create PKG from GP4
                RunCMD($"{programPath}\\Dependencies\\GenGP4\\orbis-pub-cmd.exe", $"img_create --oformat pkg --tmp_path ../../Output/Persona5Royal {config.Region.Split('-')[1]}-patch.gp4 ../../Output/Persona5Royal");
                //Rename PKG after finished
                while (!File.Exists(outputPKG) || new FileInfo(outputPKG).Length <= 0) { Thread.Sleep(1000); }
                using (WaitForFile(outputPKG, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { };
                while (true)
                {
                    if (File.ReadAllBytes(outputPKG)[0] == 0x7F)
                        break;
                }
                foreach (Process proc in Process.GetProcessesByName("orbis-pub-cmd"))
                    proc.Kill();
                foreach (Process proc in Process.GetProcessesByName("cmd"))
                    proc.Kill();
                string newOutputPKG = outputPKG.Replace($"{config.Region}_00-PERSONA5R0000000-A0101-V0100", $"{config.Region.Split('-')[1]}_00-PERSONA5R-MOD");
                if (File.Exists(newOutputPKG))
                    File.Delete(newOutputPKG);
                File.Move(outputPKG, newOutputPKG);
                Directory.Delete($"{programPath}\\Output\\Persona5Royal\\ps4pub", true);
            }

            Log.Builder.Info("Done!");

            return cpkFile;
        }

        private void LogModFilesInDirectory(VirtualDirectory directory)
        {
            foreach (var entry in directory)
            {
                if (entry.EntryType == VirtualFileSystemEntryType.File)
                    Log.Builder.Trace($"Adding mod file: {entry.FullName}");
                else
                    LogModFilesInDirectory((VirtualDirectory)entry);
            }
        }

        private void RunCMD(string filename, string args)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "cmd";
            start.WorkingDirectory = Path.GetDirectoryName(filename);
            start.Arguments = $"/K {Path.GetFileName(filename)} {args}";
            start.UseShellExecute = true;
            start.RedirectStandardOutput = false;
            //start.WindowStyle = ProcessWindowStyle.Hidden;
            
            using (Process process = Process.Start(start))
            {

            }
        }

        FileStream WaitForFile(string fullPath, FileMode mode, FileAccess access, FileShare share)
        {
            for (int numTries = 0; numTries < 10; numTries++)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(fullPath, mode, access, share);
                    return fs;
                }
                catch (System.IO.IOException)
                {
                    if (fs != null)
                    {
                        fs.Dispose();
                    }
                    Thread.Sleep(100);
                }
            }

            return null;
        }
    }

    [ModBuilder("P5R Mod Builder", Game = Game.Persona5Royal)]
    public class Persona5RoyalModBuilder : PS4PKGModBuilder
    {
        protected override Game Game => Game.Persona5Royal;
    }
}
