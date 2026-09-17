using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace LostIsland.Data.Save
{
    /// <summary>
    /// 存档加密工具
    /// 使用简单的XOR加密，防止玩家直接修改存档文件
    /// </summary>
    public static class SaveEncryption
    {
        /// <summary>
        /// 默认加密密钥（基于应用名，实际应使用设备ID等唯一标识）
        /// </summary>
        private static readonly byte[] DefaultKey = Encoding.UTF8.GetBytes("LostIsland_SaveKey_2024");

        /// <summary>
        /// 加密字符串
        /// </summary>
        public static string Encrypt(string plainText, string customKey = null)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            byte[] keyBytes = string.IsNullOrEmpty(customKey)
                ? DefaultKey
                : Encoding.UTF8.GetBytes(customKey);

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = XorBytes(plainBytes, keyBytes);

            // 转Base64以便存储
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        public static string Decrypt(string cipherText, string customKey = null)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                byte[] keyBytes = string.IsNullOrEmpty(customKey)
                    ? DefaultKey
                    : Encoding.UTF8.GetBytes(customKey);

                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] decryptedBytes = XorBytes(cipherBytes, keyBytes);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                // 解密失败返回原文（可能是未加密的旧存档）
                return cipherText;
            }
        }

        /// <summary>
        /// XOR字节加密
        /// </summary>
        private static byte[] XorBytes(byte[] data, byte[] key)
        {
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ key[i % key.Length]);
            }
            return result;
        }

        /// <summary>
        /// 生成简单校验和（用于验证存档完整性）
        /// </summary>
        public static string GenerateChecksum(string data)
        {
            if (string.IsNullOrEmpty(data)) return "0";

            int hash = 0;
            foreach (char c in data)
            {
                hash = ((hash << 5) + hash) + c;
            }
            return hash.ToString("X8");
        }

        /// <summary>
        /// 验证校验和
        /// </summary>
        public static bool VerifyChecksum(string data, string expectedChecksum)
        {
            string actual = GenerateChecksum(data);
            return actual == expectedChecksum;
        }
    }
}
