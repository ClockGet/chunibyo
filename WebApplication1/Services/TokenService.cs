using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace WebApplication1.Services
{
    public class TokenService
    {
        private static readonly string modulus = "0bea898754ba9bac397cea0ba4c7acc33abf38c344d4a8a8d7a15e5d426466a2" +
                                                 "073a5ba036807cd72949d5a1f4894c1516ffc7e0506083038e22815054e2dbb8" +
                                                 "860f075d79250bec84b443f3168c64e62114d86b1e01ec57fe3ce3c77840f79f" +
                                                 "0b2fc003b9a48c420d8434d8c9c84478d6fa179dea53d51f9ecd38fef52271b6d";
        private static readonly byte[] nonce = Encoding.UTF8.GetBytes("qoBxWEomqnCUujv9");
        private static readonly string pubkey = "10001";
        private static readonly string prikey = "1ee9b31e8190198a11191d92574829c7ceff84220ece213ce3289dc51217a4a9" +
                                                "7256d29c2ec7e515c8e6e26fe412ca6d6bdce289e9fa75e72334208465b2a08f" +
                                                "2a90107b0230462582d8871fd6310d2e5e87d4dc604a7ec11430709e7bf35122" +
                                                "c1e3d91556541799fc6c9ea2cfbcf56299e4f15e520221755eae07f9b719f2c1";
        private static readonly byte[] iv = Encoding.UTF8.GetBytes("0807060504030201");
        private static readonly byte[] choices = Encoding.ASCII.GetBytes("012345679abcdef");
        private BigInteger FromBytes(byte[] beBytes)
        {
            // 1、BigInteger的构造函数接受byte[]的格式是“低位在前（Litter Endian）”。所以以下两行是等价的：
            //    new BigInteger(new byte[]{1, 2, 3, 4})
            //    new BitInteger(new byte[]{1, 2, 3, 4, 0, 0, 0})
            // 2、BigInteger支持负数，如果byte[]的最高二进制位非零，则表示为负数，比如new byte[]{1,2,3, 0x80}就是负数。
            //    而RSA中的参数都是正整数，因此，Concat(0)用来保证正整数。
            // 3、如果输入的byte[]的格式是“高位在前(Big Endian)”，那么要先用Reverse翻转一次。
            return new BigInteger(beBytes.Reverse().Concat(new byte[] { 0 }).ToArray());
        }
        private byte[] CreateSecretKey(int size)
        {
            var key = new byte[size];
            Random r = new Random();
            for (int i = 0; i < size; i++)
            {
                key[i] = choices[r.Next(0, choices.Length)];
            }
            return key;
        }
        private string AesEncrypt(string text, byte[] secKey)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            int pad = 16 - buffer.Length % 16;
            buffer = buffer.Concat(Enumerable.Repeat((byte)pad, pad)).ToArray();
            byte[] cipherBytes = null;
            using (var aes = Aes.Create())
            {
                aes.Key = secKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(buffer, 0, buffer.Length);
                        cipherBytes = msEncrypt.ToArray();
                    }
                }
            }
            return Convert.ToBase64String(cipherBytes.ToArray());

        }
        private string AesDecrypt(string text, byte[] secKey)
        {
            byte[] cipherText = Convert.FromBase64String(text);
            byte[] buffer = new byte[1024];
            List<byte> bytes = new List<byte>();
            using (var aes = Aes.Create())
            {
                aes.Key = secKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (var msDecrypt = new MemoryStream(cipherText))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        int count;
                        while ((count = csDecrypt.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            bytes.AddRange(buffer.Take(count));
                        }
                    }
                }
            }
            var c = bytes[bytes.Count - 1];
            if (c > 0 && c <= 16)
            {
                bytes.RemoveRange(bytes.Count - c, c);
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
        private string RsaEncrypt(byte[] text, string pubKey, string modulus)
        {
            text = text.Reverse().ToArray();
            StringBuilder sb = new StringBuilder();
            foreach (var x in text)
            {
                sb.Append(x.ToString("x2"));
            }
            var a1 = BigInteger.Parse(sb.ToString(), System.Globalization.NumberStyles.HexNumber);
            var a2 = BigInteger.Parse(pubKey, System.Globalization.NumberStyles.HexNumber);
            var a3 = BigInteger.Parse(modulus, System.Globalization.NumberStyles.HexNumber);
            var rs = BigInteger.ModPow(a1, a2, a3);
            return rs.ToString("x2");
        }
        private byte[] RsaDecrypt(string text, string priKey, string modulus)
        {
            BigInteger b = BigInteger.Parse(text, System.Globalization.NumberStyles.HexNumber);
            var d = BigInteger.Parse(priKey, System.Globalization.NumberStyles.HexNumber);
            var n = BigInteger.Parse(modulus, System.Globalization.NumberStyles.HexNumber);
            var rs = BigInteger.ModPow(b, d, n);
            return rs.ToByteArray();
        }
        public string Encrypt(string text)
        {
            byte[] secKey = CreateSecretKey(16);
            string encText = AesEncrypt(AesEncrypt(text, nonce), secKey);
            string EncSecKey = RsaEncrypt(secKey, pubkey, modulus);
            return $"token1={Uri.EscapeDataString(encText)}&token2={EncSecKey}";
        }
        public string Decrypt(string token1, string token2)
        {
            var testSecKey = RsaDecrypt(token2, prikey, modulus);
            string firstD = AesDecrypt(token1, testSecKey);
            string secondD = AesDecrypt(firstD, nonce);
            return secondD;
        }
    }
}
