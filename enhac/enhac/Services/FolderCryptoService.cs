using EnHac.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace EnHac.Services
{
    public class FolderCryptoService
    {
        private readonly EncryptionSettings _settings;
        private int _totalFiles;
        private int _processedFiles;
        private readonly ConcurrentBag<string> _failedFiles;
        private readonly ConcurrentBag<string> _skippedFiles;

        public int TotalFiles => _totalFiles;
        public int ProcessedFiles => _processedFiles;
        public IReadOnlyCollection<string> FailedFiles => _failedFiles.ToList().AsReadOnly();
        public IReadOnlyCollection<string> SkippedFiles => _skippedFiles.ToList().AsReadOnly();

        public event EventHandler<FolderProgressEventArgs> FileProcessed;
        public event EventHandler<int> ProgressChanged;

        public FolderCryptoService(EncryptionSettings settings)
        {
            _settings = settings ?? new EncryptionSettings();
            _failedFiles = new ConcurrentBag<string>();
            _skippedFiles = new ConcurrentBag<string>();
        }

        /// <summary>
        /// تشفير مجلد كامل
        /// </summary>
        public async Task EncryptFolderAsync(string sourceFolder, string outputFolder, string password, bool preserveStructure = true)
        {
            await ProcessFolderAsync(sourceFolder, outputFolder, password, preserveStructure, true);
        }

        /// <summary>
        /// فك تشفير مجلد كامل
        /// </summary>
        public async Task DecryptFolderAsync(string sourceFolder, string outputFolder, string password, bool preserveStructure = true)
        {
            await ProcessFolderAsync(sourceFolder, outputFolder, password, preserveStructure, false);
        }

        /// <summary>
        /// معالجة المجلد (تشفير أو فك تشفير)
        /// </summary>
        private async Task ProcessFolderAsync(string sourceFolder, string outputFolder, string password, bool preserveStructure, bool isEncrypting)
        {
            if (string.IsNullOrEmpty(sourceFolder))
                throw new ArgumentNullException(nameof(sourceFolder));

            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentNullException(nameof(outputFolder));

            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            if (!Directory.Exists(sourceFolder))
                throw new DirectoryNotFoundException($"المجلد غير موجود: {sourceFolder}");

            if (!HasReadPermission(sourceFolder))
                throw new UnauthorizedAccessException($"لا توجد صلاحية للقراءة من المجلد: {sourceFolder}");

            Reset();

            try
            {
                Directory.CreateDirectory(outputFolder);
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException($"لا توجد صلاحية لإنشاء مجلد الإخراج: {outputFolder}");
            }

            string pattern = isEncrypting ? "*.*" : "*.encrypted";
            var files = GetAllFilesSafe(sourceFolder, pattern);

            if (files.Count == 0)
            {
                string msg = isEncrypting ? "لا توجد ملفات قابلة للقراءة في المجلد." : "لا توجد ملفات مشفرة قابلة للقراءة في المجلد.";
                throw new InvalidOperationException(msg);
            }

            _totalFiles = files.Count;

            await Task.Run(() =>
            {
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
                };

                Parallel.ForEach(files, parallelOptions, file =>
                {
                    ProcessSingleFile(file, sourceFolder, outputFolder, password, preserveStructure, isEncrypting);
                });
            });
        }

        /// <summary>
        /// معالجة ملف واحد
        /// </summary>
        private void ProcessSingleFile(string file, string sourceFolder, string outputFolder, string password, bool preserveStructure, bool isEncrypting)
        {
            string fileName = Path.GetFileName(file);

            try
            {
                // التحقق من وجود الملف
                if (!File.Exists(file))
                {
                    AddSkipped(file, "الملف لم يعد موجوداً");
                    return;
                }

                // التحقق من صلاحية القراءة
                if (!HasReadPermission(file))
                {
                    AddSkipped(file, "لا توجد صلاحية للقراءة");
                    return;
                }

                // التحقق من أن الملف ليس قيد الاستخدام
                if (IsFileLocked(file))
                {
                    AddSkipped(file, "الملف قيد الاستخدام من قبل برنامج آخر");
                    return;
                }

                // التحقق من حجم الملف
                var fileInfo = new FileInfo(file);
                long maxSize = (long)SettingsService.Instance.AppSettings.MaxFileSizeMB * 1024 * 1024;
                if (fileInfo.Length > maxSize)
                {
                    AddSkipped(file, $"الملف كبير جداً (أكبر من {SettingsService.Instance.AppSettings.MaxFileSizeMB}MB)");
                    return;
                }

                // حساب المسار النسبي
                string relativePath;
                if (preserveStructure && file.Length > sourceFolder.Length)
                {
                    relativePath = file.Substring(sourceFolder.Length).TrimStart(Path.DirectorySeparatorChar);
                }
                else
                {
                    relativePath = fileName;
                }

                // بناء مسار الملف الناتج
                string outputPath;
                if (isEncrypting)
                {
                    outputPath = Path.Combine(outputFolder, relativePath + ".encrypted");
                }
                else
                {
                    string decryptedName = relativePath;
                    if (decryptedName.EndsWith(".encrypted", StringComparison.OrdinalIgnoreCase))
                    {
                        decryptedName = decryptedName.Substring(0, decryptedName.Length - 10);
                    }
                    outputPath = Path.Combine(outputFolder, decryptedName);
                }

                // التحقق من صلاحية الكتابة في مجلد الإخراج
                string outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir))
                {
                    if (!Directory.Exists(outputDir))
                    {
                        try
                        {
                            Directory.CreateDirectory(outputDir);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            AddFailed(file, "لا توجد صلاحية لإنشاء مجلد الإخراج");
                            return;
                        }
                    }

                    if (!HasWritePermission(outputDir))
                    {
                        AddFailed(file, "لا توجد صلاحية للكتابة في مجلد الإخراج");
                        return;
                    }
                }

                // تنفيذ التشفير أو فك التشفير
                var crypto = new CryptoService(_settings);

                if (isEncrypting)
                {
                    crypto.EncryptFile(file, outputPath, password);
                }
                else
                {
                    crypto.DecryptFile(file, outputPath, password);
                }

                // تحديث العداد
                int processed = Interlocked.Increment(ref _processedFiles);
                int percent = (int)((double)processed / _totalFiles * 100);

                FileProcessed?.Invoke(this, new FolderProgressEventArgs
                {
                    FilePath = file,
                    OutputPath = outputPath,
                    Success = true,
                    ProcessedFiles = processed,
                    TotalFiles = _totalFiles
                });

                ProgressChanged?.Invoke(this, Math.Min(percent, 100));
            }
            catch (UnauthorizedAccessException ex)
            {
                AddFailed(file, $"خطأ صلاحية: {ex.Message}");
            }
            catch (IOException ex)
            {
                AddFailed(file, $"خطأ ملف: {ex.Message}");
            }
            catch (CryptographicException ex)
            {
                AddFailed(file, $"خطأ تشفير: {ex.Message}");
            }
            catch (Exception ex)
            {
                AddFailed(file, $"خطأ غير متوقع: {ex.Message}");
            }
        }

        /// <summary>
        /// التحقق من صلاحية القراءة
        /// </summary>
        private bool HasReadPermission(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return true;
                }
                else if (Directory.Exists(path))
                {
                    Directory.GetFiles(path);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// التحقق من صلاحية الكتابة
        /// </summary>
        private bool HasWritePermission(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    string testFile = Path.Combine(path, Guid.NewGuid().ToString() + ".tmp");
                    using (File.Create(testFile, 1, FileOptions.DeleteOnClose)) { }
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// التحقق مما إذا كان الملف قيد الاستخدام
        /// </summary>
        private bool IsFileLocked(string filePath)
        {
            try
            {
                using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// الحصول على جميع الملفات بشكل آمن
        /// </summary>
        private List<string> GetAllFilesSafe(string folderPath, string pattern)
        {
            var files = new List<string>();

            try
            {
                if (!HasReadPermission(folderPath))
                {
                    _skippedFiles.Add($"{Path.GetFileName(folderPath)}: لا توجد صلاحية للقراءة");
                    return files;
                }

                // إضافة ملفات المجلد الحالي
                try
                {
                    var currentFiles = Directory.GetFiles(folderPath, pattern);
                    files.AddRange(currentFiles);
                }
                catch (UnauthorizedAccessException)
                {
                    _skippedFiles.Add($"{Path.GetFileName(folderPath)}: لا توجد صلاحية لقراءة الملفات");
                }

                // إضافة ملفات المجلدات الفرعية
                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(folderPath);
                }
                catch (UnauthorizedAccessException)
                {
                    return files;
                }

                foreach (string subDir in subDirs)
                {
                    string dirName = Path.GetFileName(subDir);

                    // تخطي مجلدات النظام والمجلدات المخفية
                    if (IsSystemFolder(dirName))
                    {
                        _skippedFiles.Add($"{dirName}: مجلد نظام - تم التخطي");
                        continue;
                    }

                    // تخطي المجلدات التي تبدأ بـ .
                    if (dirName.StartsWith("."))
                    {
                        _skippedFiles.Add($"{dirName}: مجلد مخفي - تم التخطي");
                        continue;
                    }

                    try
                    {
                        var subFiles = GetAllFilesSafe(subDir, pattern);
                        files.AddRange(subFiles);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        _skippedFiles.Add($"{dirName}: لا توجد صلاحية للقراءة");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                _skippedFiles.Add($"{Path.GetFileName(folderPath)}: لا توجد صلاحية للوصول");
            }

            return files;
        }

        /// <summary>
        /// التحقق مما إذا كان المجلد من مجلدات النظام أو مجلدات لا يجب الوصول إليها
        /// </summary>
        private bool IsSystemFolder(string folderName)
        {
            string[] systemFolders = {
                "System Volume Information",
                "$Recycle.Bin",
                "Recovery",
                "Config.Msi",
                "MSOCache",
                "Windows",
                "Program Files",
                "Program Files (x86)",
                "ProgramData",
                "AppData",
                "Local Settings",
                "Application Data",
                "Cookies",
                "NetHood",
                "PrintHood",
                "Recent",
                "SendTo",
                "Start Menu",
                "Templates",
                "Microsoft",
                "Microsoft.NET",
                "Windows Defender",
                "Windows Mail",
                "Windows NT",
                "Windows Photo Viewer",
                "Windows Sidebar",
                "WindowsPowerShell",
                ".git",
                "node_modules",
                "__pycache__",
                "bin",
                "obj",
                ".vs",
                ".vscode"
            };

            return systemFolders.Any(f =>
                folderName.Equals(f, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// إضافة ملف فاشل
        /// </summary>
        private void AddFailed(string file, string error)
        {
            _failedFiles.Add($"{Path.GetFileName(file)}: {error}");

            FileProcessed?.Invoke(this, new FolderProgressEventArgs
            {
                FilePath = file,
                Success = false,
                ErrorMessage = error,
                ProcessedFiles = _processedFiles,
                TotalFiles = _totalFiles
            });
        }

        /// <summary>
        /// إضافة ملف متخطى
        /// </summary>
        private void AddSkipped(string file, string reason)
        {
            _skippedFiles.Add($"{Path.GetFileName(file)}: {reason}");

            FileProcessed?.Invoke(this, new FolderProgressEventArgs
            {
                FilePath = file,
                Success = false,
                ErrorMessage = $"⏭️ تم التخطي: {reason}",
                ProcessedFiles = _processedFiles,
                TotalFiles = _totalFiles
            });
        }

        /// <summary>
        /// إعادة تعيين العدادات
        /// </summary>
        private void Reset()
        {
            _totalFiles = 0;
            _processedFiles = 0;
            while (_failedFiles.TryTake(out _)) { }
            while (_skippedFiles.TryTake(out _)) { }
        }
    }

    public class FolderProgressEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public string OutputPath { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int ProcessedFiles { get; set; }
        public int TotalFiles { get; set; }
    }
}