using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miot
{
    public static class ExtensionMethod
    {
        public static byte[] ToBytes(this string hexString)
        {
            if ((hexString.Length % 2) != 0)
            {
                throw new ArgumentException($"hex string length:{hexString.Length}");
            }

            byte[] convertedByte = new byte[hexString.Length / 2];
            for (int i = 0; i < convertedByte.Length; i++)
            {
                var by = hexString.Substring(i * 2, 2);
                convertedByte[i] = Convert.ToByte(by, 16);

            }
            return convertedByte;
        }

        public static string ToHexString(this byte[] bytes)
        {
            StringBuilder returnStr = new StringBuilder();
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr.Append(bytes[i].ToString("X2"));
                }
            }
            return returnStr.ToString();
        }

        public static byte[] Concat(this byte[] first, byte[] second)
        {
            byte[] b = new byte[first.Length + second.Length];
            first.CopyTo(b, 0);
            second.CopyTo(b, first.Length);
            return b;
        }

        public static byte[] Concat(this byte[] first, string hexString)
        {
            return Concat(first, hexString.ToBytes());
        }

        public static void CopyTo(this string hexString, byte[] targetArray, int startIndex)
        {
            hexString.ToBytes().CopyTo(targetArray, startIndex);
        }
    }
}
