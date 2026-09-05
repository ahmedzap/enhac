using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnHac.Models;

namespace EnHac.Services
{
    public class LogService
    {
        private static LogService _instance;
        private static readonly object _lock = new object();
        private readonly List<EncryptionLog> _logs;
        private readonly string _logFilePath;

        public static LogService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new LogService();
                    }
                }
                return _instance;
            }
        }

        public event EventHandler<EncryptionLog> LogAdded;

        private LogService()
        {
            _logs = new List<EncryptionLog>();
            _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "encryption_log.txt");
        }

        public void Add(EncryptionLog log)
        {
            lock (_lock)
            {
                log.Id = _logs.Count + 1;
                _logs.Add(log);
                LogAdded?.Invoke(this, log);

                if (SettingsService.Instance.AppSettings.EnableLogging)
                {
                    WriteToFileAsync(log);
                }
            }
        }

        public List<EncryptionLog> GetRecent(int count = 100)
        {
            lock (_lock)
            {
                return _logs.OrderByDescending(l => l.Timestamp).Take(count).ToList();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _logs.Clear();
                if (File.Exists(_logFilePath))
                {
                    File.Delete(_logFilePath);
                }
            }
        }

        public string Export(string format = "txt")
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                var sortedLogs = _logs.OrderBy(l => l.Timestamp).ToList();

                if (format == "csv")
                {
                    sb.AppendLine("Timestamp,Operation,FileName,Size,Algorithm,Status,Duration");
                    foreach (var log in sortedLogs)
                    {
                        sb.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.Operation},{log.FileName},{log.FileSize},{log.Algorithm},{(log.Success ? "Success" : "Failed")},{log.Duration.TotalSeconds:F2}");
                    }
                }
                else
                {
                    sb.AppendLine(new string('=', 80));
                    sb.AppendLine("ENCRYPTION LOG REPORT");
                    sb.AppendLine(new string('=', 80));
                    sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"Total Entries: {_logs.Count}");
                    sb.AppendLine(new string('-', 80));

                    foreach (var log in sortedLogs)
                    {
                        sb.AppendLine(log.ToString());
                    }
                }

                return sb.ToString();
            }
        }

        public void LogError(string message, Exception ex)
        {
            Add(new EncryptionLog
            {
                Timestamp = DateTime.Now,
                Operation = "ERROR",
                FileName = message,
                Success = false,
                ErrorMessage = ex?.Message ?? "Unknown error"
            });
        }

        private async void WriteToFileAsync(EncryptionLog log)
        {
            try
            {
                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        string line = $"{log.Timestamp:yyyy-MM-dd HH:mm:ss}|{log.Operation}|{log.FileName}|{log.FileSize}|{log.Algorithm}|{(log.Success ? "Success" : "Failed")}|{log.Duration.TotalSeconds:F2}";
                        File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
                    }
                });
            }
            catch
            {
                // تجاهل أخطاء الكتابة
            }
        }
    }
}