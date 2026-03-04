using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityZed
{
    public class Zed
    {
        readonly string editorInstallationPath;
        readonly string projectPath;

        readonly IGenerator generator;

        public Zed(string editorInstallationPath)
        {
            this.editorInstallationPath = editorInstallationPath;
            projectPath = Directory.GetParent(Application.dataPath).FullName;

            var assembly = typeof(IGenerator).Assembly;
            var type = assembly.GetType("Microsoft.Unity.VisualStudio.Editor.SdkStyleProjectGeneration");
            generator = (IGenerator)Activator.CreateInstance(type);
        }

        public void addSettingsFileIfNeeded()
        {
            const string defaultSettings = @"{
            ""file_scan_exclusions"": [
                ""**/.*"",
                ""**/*~"",

                ""*.csproj"",
                ""*.sln"",

                ""**/*.meta"",
                ""**/*.booproj"",
                ""**/*.pibd"",
                ""**/*.suo"",
                ""**/*.user"",
                ""**/*.userprefs"",
                ""**/*.unityproj"",
                ""**/*.dll"",
                ""**/*.exe"",
                ""**/*.pdf"",
                ""**/*.mid"",
                ""**/*.midi"",
                ""**/*.wav"",
                ""**/*.gif"",
                ""**/*.ico"",
                ""**/*.jpg"",
                ""**/*.jpeg"",
                ""**/*.png"",
                ""**/*.psd"",
                ""**/*.tga"",
                ""**/*.tif"",
                ""**/*.tiff"",
                ""**/*.3ds"",
                ""**/*.3DS"",
                ""**/*.fbx"",
                ""**/*.FBX"",
                ""**/*.lxo"",
                ""**/*.LXO"",
                ""**/*.ma"",
                ""**/*.MA"",
                ""**/*.obj"",
                ""**/*.OBJ"",
                ""**/*.asset"",
                ""**/*.cubemap"",
                ""**/*.flare"",
                ""**/*.mat"",
                ""**/*.meta"",
                ""**/*.prefab"",
                ""**/*.unity"",

                ""build/"",
                ""Build/"",
                ""library/"",
                ""Library/"",
                ""obj/"",
                ""Obj/"",
                ""ProjectSettings/"",
                ""UserSettings/"",
                ""temp/"",
                ""Temp/"",
                ""logs"",
                ""Logs"",
            ]
        }";

            var settingsPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ".zed", "settings.json");
            if (!File.Exists(settingsPath))
            {
                Debug.Log("[ZED] settings file not found, creating default settings file.");
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                File.WriteAllText(settingsPath, defaultSettings);
            }
        }

        public void syncAll()
        {
            generator.Sync();
            addSettingsFileIfNeeded();
        }
        
        public void syncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            generator.SyncIfNeeded(addedFiles.Union(deletedFiles).Union(movedFiles).Union(movedFromFiles), importedFiles);
        }
        
        public bool openProject(string filePath = "", int line = -1, int column = -1)
        {
            if (!string.IsNullOrEmpty(filePath) && !generator.IsSupportedFile(filePath))
            {
                Debug.Log($"[ZED] File '{filePath}' is not supported by the generator.");
                return false;
            }

            generator.Sync();
            addSettingsFileIfNeeded();
            
            var args = new StringBuilder($"\"{projectPath}\" ");

            if (!string.IsNullOrEmpty(filePath))
            {
                args.Append($"\"{filePath}");

                if (line >= 0)
                {
                    args.Append(":");
                    args.Append(line);

                    if (column >= 0)
                    {
                        args.Append(":");
                        args.Append(column);
                    }
                }
            }

            Debug.Log(editorInstallationPath + " " + args);
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = editorInstallationPath,
                    Arguments = args.ToString(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (Process.Start(startInfo))
                {
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ZED] Failed to start Zed: {ex.Message}");
                return false;
            }
        }

        public void drawExternalTool()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var version = "0.9";
            var displayName = "Unity Zed";
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(GetType().Assembly);
            if (package != null)
            {
                version = package.version;
                displayName = package.displayName;
            }

            var style = new GUIStyle
            {
                richText = true,
                margin = new RectOffset(0, 4, 0, 0)
            };

            GUILayout.Label($"<size=10><color=grey>{displayName} v{version} enabled</color></size>", style);
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Generate .csproj files for:");
            EditorGUI.indentLevel++;
            settingsButton(ProjectGenerationFlag.Embedded, "Embedded packages", "");
            settingsButton(ProjectGenerationFlag.Local, "Local packages", "");
            settingsButton(ProjectGenerationFlag.Registry, "Registry packages", "");
            settingsButton(ProjectGenerationFlag.Git, "Git packages", "");
            settingsButton(ProjectGenerationFlag.BuiltIn, "Built-in packages", "");
            settingsButton(ProjectGenerationFlag.LocalTarBall, "Local tarball", "");
            settingsButton(ProjectGenerationFlag.Unknown, "Packages from unknown sources", "");
            settingsButton(ProjectGenerationFlag.PlayerAssemblies, "Player projects", "For each player project generate an additional csproj with the name 'project-player.csproj'");
            var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            rect.width = 252;
            if (GUI.Button(rect, "Regenerate project files"))
            {
                generator.Sync();
            }

            EditorGUI.indentLevel--;

            void settingsButton(ProjectGenerationFlag preference, string guiMessage, string toolTip)
            {
                var prevValue = generator.AssemblyNameProvider.ProjectGenerationFlag.HasFlag(preference);

                var newValue = EditorGUILayout.Toggle(new GUIContent(guiMessage, toolTip), prevValue);
                if (newValue != prevValue)
                {
                    generator.AssemblyNameProvider.ToggleProjectGeneration(preference);
                }
            }
        }
    }
}