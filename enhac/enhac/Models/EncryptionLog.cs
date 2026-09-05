using System;

namespace EnHac.Models
{
    public class EncryptionLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Operation { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string Algorithm { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public string OutputPath { get; set; }

        public override string ToString()
        {
            string status = Success ? "✅" : "❌";
            string duration = Duration.TotalSeconds < 1
                ? $"{Duration.TotalMilliseconds:F0}ms"
                : $"{Duration.TotalSeconds:F2}s";

            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {status} {Operation} | {FileName} | {FormatSize(FileSize)} | {duration}";
        }

        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}