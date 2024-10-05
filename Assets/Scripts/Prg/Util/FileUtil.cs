using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Prg.Util
{
    /// <summary>
    /// Collection of file related utilities.<br />
    /// Ideas form: Encryption And Decryption Using A Symmetric Key In C#<br />
    /// https://www.c-sharpcorner.com/article/encryption-and-decryption-using-a-symmetric-key-in-c-sharp/
    /// </summary>
    public static class FileUtil
    {
        private static readonly Encoding Encoding = PlatformUtil.Encoding;

        // https://simple.wikipedia.org/wiki/Largest_known_prime_number
        private static readonly byte[] WikiNumber1 =
            Encoding.ASCII.GetBytes("170141183460469231731687303715884105727");

        private static readonly byte[] WikiNumber2 =
            Encoding.ASCII.GetBytes("20988936657440586486151264256610222593863921"[..16]);

        private const string DataPrefix = "{SHA256}";

        public static void WriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents, Encoding);
        }

        public static void WriteAllText(string path, string contents, string key)
        {
            File.WriteAllText(path, WriteString(contents, key), Encoding);
        }

        public static string ReadAllText(string path)
        {
            return File.ReadAllText(path, Encoding);
        }

        public static string ReadAllText(string path, string key)
        {
            return ReadString(File.ReadAllText(path, Encoding), key);
        }

        public static string[] ReadAllLines(string path)
        {
            return File.ReadAllLines(path, Encoding);
        }

        public static string[] ReadAllLines(string path, string key)
        {
            // Try to follow semantics of // https://learn.microsoft.com/en-us/dotnet/api/system.io.file.readalllines
            // A line is defined as a sequence of characters followed by a carriage return ('\r'), a line feed ('\n'),
            // or a carriage return immediately followed by a line feed.
            // The resulting string does not contain the terminating carriage return and/or line feed.
            return ReadString(File.ReadAllText(path, Encoding), key)
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        }

        /// <summary>
        /// Converts a plaint text string to encrypted and base64 encoded string with prefix.
        /// </summary>
        public static string WriteString(string plainText, string key)
        {
            using var algo = CreateAlgo(key);
            var encryptor = algo.CreateEncryptor(algo.Key, algo.IV);

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(plainText);
            }
            var array = memoryStream.ToArray();
            return $"{DataPrefix}{Convert.ToBase64String(array)}";
        }

        /// <summary>
        /// Converts (decrypts) a prefixed, encrypted base64 encoded string to plain text string.
        /// </summary>
        public static string ReadString(string cipherText, string key)
        {
            // Note that SymmetricAlgorithm.Clear is called automatically on Dispose-
            using var algo = CreateAlgo(key);
            using var decryptor = algo.CreateDecryptor(algo.Key, algo.IV);

            var payload = new StringBuilder(cipherText)
                .Remove(0, DataPrefix.Length)
                .ToString();
            var buffer = Convert.FromBase64String(payload);
            using var memoryStream = new MemoryStream(buffer);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var streamReader = new StreamReader(cryptoStream);
            return streamReader.ReadToEnd();
        }

        private static SymmetricAlgorithm CreateAlgo(string key)
        {
            var aes = Aes.Create();
            var passwordBytes = new Rfc2898DeriveBytes(key, WikiNumber1, 19, HashAlgorithmName.SHA256);
            aes.Key = passwordBytes.GetBytes(32);
            aes.IV = WikiNumber2;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }
    }
}
