using System;
using System.IO;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;
using CriPakTools;
using ModCompendiumLibrary.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public abstract class PersonaPortableModBuilder : IModBuilder
    {
        protected abstract Game Game { get; }

        public bool OutputUnmodifiedFiles { get; } = true;
        /// <inheritdoc />
        public VirtualFileSystemEntry Build(VirtualDirectory root, List<Mod> enabledMods, string hostOutputPath = null, string gameName = null, bool useCompression = false, bool useExtracted = false)
        {
            gameName = Game.ToString();

            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            // Get game config
            var config = ConfigStore.Get(Game) as PersonaPortableGameConfig;
            if (config == null)
            {
                // Unlikely
                throw new InvalidOperationException("Game config is missing.");
            }

            bool pc = Convert.ToBoolean(config.PC);
            if (!pc && string.IsNullOrWhiteSpace(config.CpkRootOrPath))
            {
                throw new InvalidConfigException("CPK path is not specified.");
            }

            // If PC Mode is enabled, clear contents
            if (pc)
            {
                if (Directory.Exists(hostOutputPath))
                    foreach (var directory in Directory.GetDirectories(hostOutputPath))
                    {
                        string[] stringArray = { "data00000", "data00001", "data00002", "data00003", "data00004", "data00005", "data00006", "movie00000", "movie00001", "movie00002", "snd" };
                        if (stringArray.Any(Path.GetFileName(directory).ToLower().Equals))
                            Directory.Delete(directory, true);
                    }
            }

            // Create temp folder
            var cpkRootDirectoryPath = Path.Combine(Path.GetTempPath(), $"{Game}ModCompilerTemp_" + Path.GetRandomFileName());
            Log.Builder.Trace($"Creating temp output directory: {cpkRootDirectoryPath}");
            Directory.CreateDirectory(cpkRootDirectoryPath);

            // Get files from CPK
            VirtualDirectory cpkRootDirectory;

            Log.Builder.Trace($"{nameof(config.CpkRootOrPath)} = {config.CpkRootOrPath}");

            if (!pc)
            {
                if (config.CpkRootOrPath.EndsWith(".cpk"))
                {
                    // If extraction is enabled, use files from CPK path 
                    useExtracted = Convert.ToBoolean(config.Extract);
                    if (useExtracted)
                    {
                        Log.Builder.Info($"Extracting CPK: {config.CpkRootOrPath}");

                        if (!File.Exists(config.CpkRootOrPath))
                        {
                            throw new InvalidConfigException($"CPK root path references a CPK file that does not exist: {config.CpkRootOrPath}.");
                        }

                        string[] args = { "-x", "-i", config.CpkRootOrPath, "-d", cpkRootDirectoryPath };
                        CriPakTools.Program.Main(args);
                    }
                    // Cpk file found & extracted, convert it to our virtual file system
                    cpkRootDirectory = VirtualDirectory.FromHostDirectory(cpkRootDirectoryPath);
                    cpkRootDirectory.Name = Path.GetFileNameWithoutExtension(config.CpkRootOrPath);
                }
                else
                {
                    Log.Builder.Info($"Mounting directory: {config.CpkRootOrPath}");

                    if (!Directory.Exists(config.CpkRootOrPath))
                    {
                        throw new InvalidConfigException($"CPK root path references a directory that does not exist: {config.CpkRootOrPath}.");
                    }

                    // No CPK file found, assume files are extracted
                    cpkRootDirectory = VirtualDirectory.FromHostDirectory(config.CpkRootOrPath);
                    cpkRootDirectory.Name = Path.GetDirectoryName(config.CpkRootOrPath);
                }
            }
            else
            {
                cpkRootDirectory = new VirtualDirectory();
            }

            Log.Builder.Info("Processing mod files");
            foreach (var entry in root)
            {
                if (entry.EntryType == VirtualFileSystemEntryType.Directory)
                {
                    var directory = (VirtualDirectory)entry;
                    var name = directory.Name.ToLowerInvariant();

                    // Skip recompiling CVMs if HostFS Mode
                    if (pc)
                        entry.MoveTo(cpkRootDirectory, true);
                    else
                    {
                        switch (name)
                        {
                            case "mod":
                            case "cache":
                            case "umd0":
                            case "umd1":
                            case "vita":
                            case "patch":
                            case "memst":
                                {
                                    // Move files in 'cpk' directory to 'mod' directory
                                    LogModFilesInDirectory(directory);

                                    foreach (var modFileEntry in directory)
                                    {
                                        modFileEntry.CopyTo(cpkRootDirectory);
                                    }
                                }
                                break;

                            default:
                                // Move directory to 'mod' directory
                                Log.Builder.Trace($"Adding directory {entry.FullName} to {cpkRootDirectory.Name}.cpk");
                                entry.CopyTo(cpkRootDirectory);
                                break;
                        }
                    }
                }
                else
                {
                    // Move file to 'mod' directory
                    Log.Builder.Trace($"Adding file {entry.FullName} to {cpkRootDirectory.Name}.cpk");
                    entry.CopyTo(cpkRootDirectory);
                }
            }

            useCompression = Convert.ToBoolean(config.Compression);

            // Build mod cpk
            if (!pc) 
            {
                var cpkModCompiler = new CpkModBuilder();
                var cpkFilePath = hostOutputPath != null ? Path.Combine(hostOutputPath, $"{cpkRootDirectory.Name}.cpk") : null;
                var cpkFile = cpkModCompiler.Build(cpkRootDirectory, enabledMods, cpkFilePath, gameName, useCompression);
                Log.Builder.Info("Done!");
                return cpkFile;
            }
            else
            {
                Directory.CreateDirectory(Path.GetFullPath(hostOutputPath));
                cpkRootDirectory.SaveToHost(hostOutputPath);
                Log.Builder.Info("Done!");
                return cpkRootDirectory;
            }
            
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

    [ModBuilder("Persona 3 Portable Mod Builder", Game = Game.Persona3Portable)]
    public class Persona3PortableModBuilder : PersonaPortableModBuilder
    {
        protected override Game Game => Game.Persona3Portable;
    }

    [ModBuilder("Persona 4 Golden Mod Builder", Game = Game.Persona4Golden)]
    public class Persona4GoldenModBuilder : PersonaPortableModBuilder
    {
        protected override Game Game => Game.Persona4Golden;
    }

    [ModBuilder("Persona 4 Dancing Mod Builder", Game = Game.Persona4Dancing)]
    public class Persona4DancingModBuilder : PersonaPortableModBuilder
    {
        protected override Game Game => Game.Persona4Dancing;
    }

    [ModBuilder("Persona Q Mod Builder", Game = Game.PersonaQ)]
    public class PersonaQModBuilder : PersonaPortableModBuilder
    {
        protected override Game Game => Game.PersonaQ;
    }

    [ModBuilder("Persona Q2 Mod Builder", Game = Game.PersonaQ2)]
    public class PersonaQ2ModBuilder : PersonaPortableModBuilder
    {
        protected override Game Game => Game.PersonaQ2;
    }
}