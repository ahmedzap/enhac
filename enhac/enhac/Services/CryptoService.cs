using System;
using System.IO;
using System.Security.Cryptography;
using EnHac.Models;

namespace EnHac.Services
{
    public class CryptoService
    {
        private readonly EncryptionSettings _settings;

        public CryptoService(EncryptionSettings settings)
        {
            _settings = settings ?? new EncryptionSettings();
        }

        /// <summary>
        /// تشفير ملف
        /// </summary>
        public void EncryptFile(string inputFile, string outputFile, string password, IProgress<int> progress = null)
        {
            if (!File.Exists(inputFile))
                throw new FileNotFoundException("الملف غير موجود", inputFile);

            // توليد Salt و IV عشوائيين
            byte[] salt = new byte[32];
            byte[] iv = new byte[16];
            RandomNumberGenerator.Fill(salt);
            RandomNumberGenerator.Fill(iv);

            // اشتقاق المفتاح من كلمة المرور
            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                salt,
                _settings.Iterations,
                HashAlgorithmName.SHA256
            );
            byte[] key = deriveBytes.GetBytes(32);

            // فتح الملفات
            using var inputStream = File.OpenRead(inputFile);
            using var outputStream = File.Create(outputFile);

            // كتابة الترويسة (Header)
            byte[] signature = System.Text.Encoding.UTF8.GetBytes("ENCRYPTED");
            outputStream.Write(signature, 0, signature.Length);
            outputStream.Write(salt, 0, salt.Length);
            outputStream.Write(iv, 0, iv.Length);

            // إنشاء AES
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // تشفير
            using var encryptor = aes.CreateEncryptor();
            using var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);

            byte[] buffer = new byte[_settings.BufferSize];
            long totalBytes = inputStream.Length;
            long processedBytes = 0;
            int bytesRead;
            int lastPercent = -1;

            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                cryptoStream.Write(buffer, 0, bytesRead);
                processedBytes += bytesRead;

                if (progress != null && totalBytes > 0)
                {
                    int percent = (int)(processedBytes * 100 / totalBytes);
                    if (percent != lastPercent)
                    {
                        progress.Report(Math.Min(percent, 100));
                        lastPercent = percent;
                    }
                }
            }

            cryptoStream.FlushFinalBlock();
            progress?.Report(100);
        }

        /// <summary>
        /// فك تشفير ملف
        /// </summary>
        public void DecryptFile(string inputFile, string outputFile, string password, IProgress<int> progress = null)
        {
            if (!File.Exists(inputFile))
                throw new FileNotFoundException("الملف غير موجود", inputFile);

            using var inputStream = File.OpenRead(inputFile);

            // قراءة الترويسة (Header)
            byte[] signature = new byte[9];
            int sigRead = inputStream.Read(signature, 0, 9);
            if (sigRead != 9)
                throw new InvalidDataException("الملف تالف أو غير مكتمل");

            string sigText = System.Text.Encoding.UTF8.GetString(signature);
            if (sigText != "ENCRYPTED")
                throw new InvalidDataException("هذا الملف غير مشفر بهذا البرنامج");

            // قراءة الـ Salt
            byte[] salt = new byte[32];
            int saltRead = inputStream.Read(salt, 0, 32);
            if (saltRead != 32)
                throw new InvalidDataException("الملف تالف - لا يمكن قراءة Salt");

            // قراءة الـ IV
            byte[] iv = new byte[16];
            int ivRead = inputStream.Read(iv, 0, 16);
            if (ivRead != 16)
                throw new InvalidDataException("الملف تالف - لا يمكن قراءة IV");

            // اشتقاق المفتاح من كلمة المرور ونفس الـ Salt
            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                salt,
                _settings.Iterations,
                HashAlgorithmName.SHA256
            );
            byte[] key = deriveBytes.GetBytes(32);

            // إنشاء AES لفك التشفير
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // فك التشفير
            using var decryptor = aes.CreateDecryptor();
            using var cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);
            using var outputStream = File.Create(outputFile);

            try
            {
                byte[] buffer = new byte[_settings.BufferSize];
                int bytesRead;
                long totalBytes = inputStream.Length - inputStream.Position;
                long processedBytes = 0;
                int lastPercent = -1;

                while ((bytesRead = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outputStream.Write(buffer, 0, bytesRead);
                    processedBytes += bytesRead;

                    if (progress != null && totalBytes > 0)
                    {
                        int percent = (int)(processedBytes * 100 / totalBytes);
                        if (percent != lastPercent)
                        {
                            progress.Report(Math.Min(percent, 100));
                            lastPercent = percent;
                        }
                    }
                }

                outputStream.Flush();
                progress?.Report(100);
            }
            catch (CryptographicException ex)
            {
                outputStream.Close();
                if (File.Exists(outputFile))
                {
                    try { File.Delete(outputFile); } catch { }
                }
                throw new CryptographicException("فشل فك التشفير. كلمة المرور غير صحيحة أو الملف تالف.", ex);
            }
            catch (Exception)
            {
                outputStream.Close();
                if (File.Exists(outputFile))
                {
                    try { File.Delete(outputFile); } catch { }
                }
                throw;
            }
        }
    }
}