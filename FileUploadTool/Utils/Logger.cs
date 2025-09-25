using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FileUploadTool.Utils
{
    public static class Logger
    {
        private static readonly string LogFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"FileUploadToolLogs");

        private static readonly string LogFile = Path.Combine(LogFolder, "FileUploadTool.log");

        public static void WriteLog(string message)
        {
            try
            {
                // Ensure log folder exists
                if (!Directory.Exists(LogFolder))
                    Directory.CreateDirectory(LogFolder);

                // Compose log entry
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";

                // Append to log file
                File.AppendAllText(LogFile, logEntry);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during writing log {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }
    }
}
