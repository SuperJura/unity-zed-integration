using System.Diagnostics;
using System.Text;
using NiceIO;
using UnityEngine;

namespace UnityZed
{
    public class ZedProcess
    {
        private readonly NPath m_ExecPath;
        private readonly NPath m_ProjectPath;

        public ZedProcess(string execPath)
        {
            m_ExecPath = execPath;
            m_ProjectPath = new NPath(Application.dataPath).Parent;
        }

        public bool OpenProject(string filePath = "", int line = -1, int column = -1)
        {
            // always add project path
            var args = new StringBuilder($"\"{m_ProjectPath}\" ");

            // if file path is provided, add it too
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

            UnityEngine.Debug.Log(m_ExecPath + " " + args);
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = m_ExecPath.ToString(),
                    Arguments = args.ToString(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (Process.Start(startInfo)) { }
                return true;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"ZedProcess Failed to start Zed: {ex.Message}");
                return false;
            }
        }
    }
}
