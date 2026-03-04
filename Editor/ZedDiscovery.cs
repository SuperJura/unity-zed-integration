using System;
using Unity.CodeEditor;
using System.Collections.Generic;
using System.IO;
using System.Xml.XPath;
using System.Text;
using NiceIO;

namespace UnityZed
{
    public class ZedDiscovery
    {
        public CodeEditor.Installation[] GetInstallations()
        {
            var results = new List<CodeEditor.Installation>();

            var candidates = new List<(NPath path, TryGetVersion tryGetVersion)> {

                // [MacOS]
                ("/Applications/Zed.app/Contents/MacOS/cli", TryGetVersionFromPlist),
                ("/usr/local/bin/zed", null),

                // [Linux] (Flatpak)
                ("/var/lib/flatpak/app/dev.zed.Zed/current/active/files/bin/zed", null),

                // [Linux] (Repo)
                ("/usr/bin/zeditor", null),

                // [Linux] (NixOS)
                ("/run/current-system/sw/bin/zeditor", null),
                // [Linux] (NixOS HomeManager from Zed Flake)
                ("/etc/profiles/per-user/linx/bin/zed", null),
                // [Linux] (NixOS HomeManager from NixPkgs)
                ("/etc/profiles/per-user/linx/bin/zeditor", null),

                // [Linux] (Official Website)
                (NPath.HomeDirectory.Combine(".local/bin/zed"), null),
            };

#if UNITY_EDITOR_WIN
            // [Windows] Default install locations
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var windowsCandidates = new[]
            {
                Path.Combine(programFiles, "Zed", "bin", "zed.exe"),
                Path.Combine(programFilesX86, "Zed", "bin", "zed.exe"),
                Path.Combine(localAppData, "Programs", "Zed", "bin", "zed.exe"), // common for user-level installs
                Path.Combine(localAppData, "Zed", "bin", "zed.exe")
            };

            foreach (var winPath in windowsCandidates)
            {
                candidates.Add((winPath, null));
            }

            // [Windows] Check if 'zed.exe' is in PATH
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                var pathDirs = pathEnv.Split(Path.PathSeparator);
                foreach (var dir in pathDirs)
                {
                    var exePath = Path.Combine(dir.Trim(), "zed.exe");
                    if (File.Exists(exePath))
                    {
                        candidates.Add((exePath, null));
                    }
                }
            }
#endif

            foreach (var candidate in candidates)
            {
                var candidatePath = candidate.path;
                var candidateTryGetVersion = candidate.tryGetVersion ?? TryGetVersionFallback;

                if (candidatePath.FileExists())
                {
                    var name = new StringBuilder("Zed");

                    if (candidateTryGetVersion(candidatePath, out var version))
                        name.Append($" [{version}]");

                    results.Add(new()
                    {
                        Name = name.ToString(),
                        Path = candidatePath.MakeAbsolute().ToString(),
                    });

                    break;
                }
            }

            return results.ToArray();
        }

        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            foreach (var installed in GetInstallations())
            {
                if (installed.Path == editorPath)
                {
                    installation = installed;
                    return true;
                }
            }

            installation = default;
            return false;
        }

        //
        // TryGetVersion implementations
        //
        private delegate bool TryGetVersion(NPath path, out string vertion);

        private static bool TryGetVersionFallback(NPath path, out string version)
        {
            version = null;
            return false;
        }

        private static bool TryGetVersionFromPlist(NPath path, out string version)
        {
            version = null;

            var plistPath = path.Combine("../../").Combine("Info.plist");
            if (plistPath.FileExists() == false)
                return false;

            var xPath = new XPathDocument(plistPath.ToString());
            var xNavigator = xPath.CreateNavigator().SelectSingleNode("/plist/dict/key[text()='CFBundleShortVersionString']/following-sibling::string[1]/text()");
            if (xNavigator == null)
                return false;

            version = xNavigator.Value;
            return true;
        }
    }
}
