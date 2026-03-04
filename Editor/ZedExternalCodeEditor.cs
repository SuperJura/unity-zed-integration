using System;
using System.Linq;
using Unity.CodeEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using Microsoft.Unity.VisualStudio.Editor;

namespace UnityZed
{
    public class ZedExternalCodeEditor : IExternalCodeEditor
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
            => CodeEditor.Register(new ZedExternalCodeEditor());

        private static readonly ZedDiscovery sDiscovery = new();
        
        Zed zed;

        public void Initialize(string editorInstallationPath)
        {
            zed = new Zed(editorInstallationPath);
        }

        public CodeEditor.Installation[] Installations
            => sDiscovery.GetInstallations();

        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
            => sDiscovery.TryGetInstallationForPath(editorPath, out installation);
        
        public bool OpenProject(string filePath = "", int line = -1, int column = -1) => zed.openProject(filePath, line, column);
        public void SyncAll() => zed.syncAll();
        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles) => zed.syncIfNeeded(addedFiles, deletedFiles, movedFiles, movedFromFiles, importedFiles);
        public void OnGUI() => zed.drawExternalTool();
    }
}
