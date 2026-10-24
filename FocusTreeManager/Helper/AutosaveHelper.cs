using System;
using System.IO;
using System.Text;
using System.Windows;
using FocusTreeManager.DataContract;
using FocusTreeManager.Model;

namespace FocusTreeManager.Helper
{
    /// <summary>
    /// Periodic recovery copy of the open project. The recovery copy is stored outside of the
    /// project folder (in %LocalAppData%) and never replaces the real project file. It is
    /// deleted as soon as the project is saved or the user quits on purpose, so if one is
    /// still there at the next load, the application did not close normally.
    /// </summary>
    public static class AutosaveHelper
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(3);

        private static string RecoveryFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FocusTreeManager", "Recovery");
            }
        }

        /// <summary>
        /// Stable (across runs) short hash of the path, so two projects with the same file
        /// name in different folders do not share a recovery file.
        /// </summary>
        private static string HashOf(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in text.ToLowerInvariant())
                {
                    hash = (hash ^ c) * 16777619;
                }
                return hash.ToString("x8");
            }
        }

        public static string GetRecoveryPath(string projectPath)
        {
            string name = Path.GetFileNameWithoutExtension(projectPath);
            return Path.Combine(RecoveryFolder,
                name + "_" + HashOf(Path.GetFullPath(projectPath)) + ".xh4prj.autosave");
        }

        public static void Save(ProjectModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Filename)) return;
            try
            {
                Directory.CreateDirectory(RecoveryFolder);
                string target = GetRecoveryPath(model.Filename);
                string temp = target + ".tmp";
                //Same preparation as a normal save, but written somewhere else. The real
                //project file is not touched.
                DataHolder.Instance.Project.UpdateDataContract(model);
                SerializationHelper.Serialize(temp, DataHolder.Instance.Project);
                //Write to a temporary file first, so a crash while writing can never
                //destroy the previous recovery copy.
                if (File.Exists(target))
                {
                    File.Delete(target);
                }
                File.Move(temp, target);
            }
            catch (Exception)
            {
                //The autosave must never disturb the user
            }
        }

        /// <summary>
        /// Returns the path of a recovery file that is more recent than the project file,
        /// or null if there is none.
        /// </summary>
        public static string FindNewerRecovery(string projectPath)
        {
            try
            {
                if (string.IsNullOrEmpty(projectPath)) return null;
                string recovery = GetRecoveryPath(projectPath);
                if (!File.Exists(recovery)) return null;
                if (!File.Exists(projectPath)) return recovery;
                return File.GetLastWriteTimeUtc(recovery) > File.GetLastWriteTimeUtc(projectPath)
                    ? recovery : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool AskRestore(string projectPath, string recoveryPath)
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine("A more recent automatic backup exists for this project.");
            message.AppendLine("The application probably did not close normally last time.");
            message.AppendLine();
            message.AppendLine("Project: " + Path.GetFileName(projectPath));
            message.AppendLine("Backup saved at: " + File.GetLastWriteTime(recoveryPath));
            message.AppendLine();
            message.AppendLine("Restore the backup? (Choose No to open the project file as it is.)");
            return MessageBox.Show(message.ToString(), "Restore backup",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public static void Discard(string projectPath)
        {
            try
            {
                if (string.IsNullOrEmpty(projectPath)) return;
                string recovery = GetRecoveryPath(projectPath);
                if (File.Exists(recovery))
                {
                    File.Delete(recovery);
                }
            }
            catch (Exception)
            {
                //Nothing to do
            }
        }
    }
}
