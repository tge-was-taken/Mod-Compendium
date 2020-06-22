using ModCompendiumLibrary.Configuration;
using ModCompendiumLibrary.IO;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public abstract class ModCpkModBuilder : IModBuilder
    {
        protected abstract Game Game { get; }

        /// <inheritdoc />
        public VirtualFileSystemEntry Build(VirtualDirectory root, string hostOutputPath = null, string gameName = null, bool useCompression = false, bool useExtracted = false)
        {
            gameName = Game.ToString();

            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            //Get game config
            var config = ConfigStore.Get(Game) as ModCpkGameConfig ?? throw new InvalidOperationException("Game config is missing.");

            Log.Builder.Info($"Building {gameName} Mod");
            Log.Builder.Info("Processing mod files");

            var modFilesDirectory = new VirtualDirectory(null, "mod");
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
                            Log.Builder.Trace($"Adding directory {entry.FullName} to mod.cpk");
                            entry.CopyTo(modFilesDirectory);
                            break;
                    }
                }
                else
                {
                    // Move file to 'mod' directory
                    Log.Builder.Trace($"Adding file {entry.FullName} to mod.cpk");
                    entry.CopyTo(modFilesDirectory);
                }
            }

            bool.TryParse(config.Compression, out useCompression);

            // Build mod cpk
            Log.Builder.Info("Building mod.cpk");
            var cpkModCompiler = new CpkModBuilder();

            if (hostOutputPath != null) Directory.CreateDirectory(hostOutputPath);

            var cpkFilePath = hostOutputPath != null ? Path.Combine(hostOutputPath, "mod.cpk") : null;
            var cpkNotWritable = File.Exists(cpkFilePath) && FileHelper.IsFileInUse(cpkFilePath);
            var cpkFileBuildPath = hostOutputPath != null ? cpkNotWritable ? Path.Combine(Path.GetTempPath(), "mod.cpk") : cpkFilePath : null;
            var cpkFile = cpkModCompiler.Build(modFilesDirectory, cpkFileBuildPath, gameName, useCompression);

            if (cpkFileBuildPath != cpkFilePath)
            {
                File.Copy(cpkFileBuildPath, cpkFilePath, true);
                File.Delete(cpkFileBuildPath);
                cpkFile = VirtualFile.FromHostFile(cpkFilePath);
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
    }

    [ModBuilder("P5D Mod Builder", Game = Game.Persona5Dancing)]
    public class P5DModCpkBuilder : ModCpkModBuilder
    {
        protected override Game Game => Game.Persona5Dancing;
    }

    [ModBuilder("P3D Mod Builder", Game = Game.Persona3Dancing)]
    public class P3DModCpkBuilder : ModCpkModBuilder
    {
        protected override Game Game => Game.Persona3Dancing;
    }

    [ModBuilder("P5 Mod Builder", Game = Game.Persona5)]
    public class P5ModCpkBuilder : ModCpkModBuilder
    {
        protected override Game Game => Game.Persona5;
    }
}
