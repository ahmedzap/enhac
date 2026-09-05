using System;
using System.ComponentModel;

namespace EnHac.Models
{
    [Serializable]
    public class EncryptionSettings
    {
        public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.AES256;
        public CipherModeType CipherMode { get; set; } = CipherModeType.CBC;
        public int KeySize { get; set; } = 256;
        public int SaltSize { get; set; } = 256;
        public int Iterations { get; set; } = 100000;
        public int BufferSize { get; set; } = 8192;
        public PaddingModeType Padding { get; set; } = PaddingModeType.PKCS7;
        public bool PreserveFolderStructure { get; set; } = true;
        public bool DeleteOriginalFile { get; set; } = false;
        public bool AutoGenerateName { get; set; } = false;
        public bool AddTimestamp { get; set; } = false;
        public bool CompressBeforeEncrypt { get; set; } = false;
        public bool VerifyAfterEncryption { get; set; } = true;
        public string DefaultEncryptionExtension { get; set; } = ".encrypted";
    }

    public enum EncryptionAlgorithm
    {
        [Description("AES 128-bit")]
        AES128,
        [Description("AES 192-bit")]
        AES192,
        [Description("AES 256-bit")]
        AES256,
        [Description("Triple DES")]
        TripleDES
    }

    public enum CipherModeType
    {
        [Description("CBC")]
        CBC,
        [Description("ECB")]
        ECB,
        [Description("CFB")]
        CFB,
        [Description("CTS")]
        CTS
    }

    public enum PaddingModeType
    {
        [Description("PKCS7")]
        PKCS7,
        [Description("Zeros")]
        Zeros,
        [Description("ANSIX923")]
        ANSIX923,
        [Description("ISO10126")]
        ISO10126
    }
}