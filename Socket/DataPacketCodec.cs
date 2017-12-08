using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace KR_MobilData.Socket
{
    public class DataPacketCodec
    {
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="encryptString">加密字符串</param>
        /// <param name="encryptKey">加密密钥(8位)</param>
        /// <returns></returns>
        public static string Encode(string encryptString, string encryptKey)
        {
            DESCryptoServiceProvider dCSP = new DESCryptoServiceProvider();
            byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey);
            byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);
            MemoryStream mStream = new MemoryStream();
            CryptoStream cStream = new CryptoStream(mStream, dCSP.CreateEncryptor(rgbKey, new byte[8]),
                CryptoStreamMode.Write);
            cStream.Write(inputByteArray, 0, inputByteArray.Length);
            cStream.FlushFinalBlock();
            return Convert.ToBase64String(mStream.ToArray());
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="decryptString">解密字符串</param>
        /// <param name="decryptKey">密钥(8位)</param>
        /// <returns></returns>
        public static string Decode(string decryptString, string decryptKey)
        {
            byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey);
            byte[] inputByteArray = Convert.FromBase64String(decryptString);
            DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();
            MemoryStream mStream = new MemoryStream();
            CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, new byte[8]),
                CryptoStreamMode.Write);
            cStream.Write(inputByteArray, 0, inputByteArray.Length);
            cStream.FlushFinalBlock();
            return Encoding.UTF8.GetString(mStream.ToArray());
        }
    }
}