using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Miot
{
    public class MiotProtocolPacket
    {
        
        private readonly byte[] _tokenBytes;
        private MiotMessage _message;

        public readonly string Token;
        public uint DeviceId;
        public uint Timestamp;


        public ushort Length { get; private set; }


        public MiotMessage Message
        {
            get => _message;
            set
            {
                byte[] encryptedMessage = null;
                _message = value;
                if (_tokenBytes == null)
                {
                    Length = 0x20;
                    RowPacket = Enumerable.Repeat<byte>(0xFF, Length).ToArray();
                    DeviceId = 0xFFFFFFFF;
                    Timestamp = 0xFFFFFFFF;
                }
                else
                {
                    encryptedMessage = Encrypt(_message.ToString());
                    Length = (ushort)(0x20 + encryptedMessage.Length);
                    RowPacket = new byte[Length];
                    RowPacket[4] = 0; // Unknown1
                    RowPacket[5] = 0;
                    RowPacket[6] = 0;
                    RowPacket[7] = 0;
                    RowPacket[8] = (byte)(DeviceId >> 24);
                    RowPacket[9] = (byte)(DeviceId >> 16);
                    RowPacket[10] = (byte)(DeviceId >> 8);
                    RowPacket[11] = (byte)DeviceId;
                    RowPacket[12] = (byte)(Timestamp >> 24);
                    RowPacket[13] = (byte)(Timestamp >> 16);
                    RowPacket[14] = (byte)(Timestamp >> 8);
                    RowPacket[15] = (byte)Timestamp;
                }

                RowPacket[0] = 0x21; // Magic Number
                RowPacket[1] = 0x31;

                RowPacket[2] = (byte)(Length >> 8); // Length
                RowPacket[3] = (byte)Length;


                if (_tokenBytes != null)
                {
                    Token.CopyTo(RowPacket, 16);
                    encryptedMessage.CopyTo(RowPacket, 32);
                    var md5 = MD5(RowPacket);
                    md5.CopyTo(RowPacket, 16);
                }
            }
        }

        public byte[] RowPacket { get; private set; }



        protected MiotProtocolPacket(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                this.Token = string.Empty;
                _tokenBytes = null;
            } 
            else if (token.Length != 32)
            {
                throw new ArgumentMiotException("Illegal token!");
            }
            else
            {
                this.Token = token;
                _tokenBytes = token.ToBytes();
            }
            
        }

        public static MiotProtocolPacket Build(MiotMessage message, string token, uint deviceId, uint timestamp)
        {
            var packet = new MiotProtocolPacket(token);
            packet.Timestamp = timestamp;
            packet.DeviceId = deviceId;
            packet.Message = message;
            return packet;
        }


        public static MiotProtocolPacket Build(string message, string token, uint deviceId, uint timestamp)
        {
            var packet = new MiotProtocolPacket(token);
            packet.Timestamp = timestamp;
            packet.DeviceId = deviceId;
            packet.Message = MiotMessage.BuildMessage(message);
            return packet;
        }

        public static MiotProtocolPacket Parse(byte[] packet, string token)
        {
            if (packet == null || packet.Length < 0x20)
            {
                throw new ArgumentMiotException("The packet parameter is illegal!");
            }
            var mppacket = new MiotProtocolPacket(token);
            mppacket.RowPacket = packet;
            mppacket.Length = (ushort)((packet[2] << 8) | packet[3]);
            mppacket.DeviceId = ((uint)((packet[8] << 24) | (packet[9] << 16) | (packet[10] << 8) | packet[11]));
            mppacket.Timestamp = ((uint)((packet[12] << 24) | (packet[13] << 16) | (packet[14] << 8) | packet[15]));
            if (mppacket.Length > 0x20)
            {
                string message = mppacket.Decrypt(packet.Skip(32).ToArray());
                if (message != null && message.Contains("exe_time"))
                {
                    mppacket._message = ReceivedMessage.BuildMessage(message);
                }
                else
                {
                    mppacket._message = SentMessage.BuildMessage(message);
                }
            }
            else
            {
                mppacket._message = null;
            }
            return mppacket;
        }


        protected static byte[] MD5(byte[] bytes)
        {
            return new MD5CryptoServiceProvider().ComputeHash(bytes);
        }

        /// <summary>
        /// key = MD5(token)
        /// iv  = MD5(key + token)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="token"></param>
        /// <returns>encrypted Message</returns>
        public byte[] Encrypt(string message)
        {
            if (_tokenBytes == null)
            {
                throw new ArgumentMiotException("The token cannot be null!");
            }
            byte[] keyArray = MD5(_tokenBytes);
            byte[] ivArray = MD5(keyArray.Concat(_tokenBytes));
            byte[] encryptedMessage = Encoding.ASCII.GetBytes(message);

            RijndaelManaged rijndaelCipher = new RijndaelManaged();
            rijndaelCipher.Mode = CipherMode.CBC;
            rijndaelCipher.Padding = PaddingMode.PKCS7;
            rijndaelCipher.KeySize = 128;
            rijndaelCipher.BlockSize = 128;
            rijndaelCipher.Key = keyArray;
            rijndaelCipher.IV = ivArray;
            ICryptoTransform transform = rijndaelCipher.CreateEncryptor();
            byte[] cipherBytes = transform.TransformFinalBlock(encryptedMessage, 0, encryptedMessage.Length);

            return cipherBytes;
        }
        public string Decrypt(byte[] encryptedMessage)//解密
        {
            if (_tokenBytes == null)
            {
                throw new ArgumentMiotException("The token cannot be empty!");
            }
            if (encryptedMessage == null || encryptedMessage.Length == 0)
            {
                throw new ArgumentMiotException("The encryptedMessage cannot be empty!");
            }

            byte[] keyArray = MD5(_tokenBytes);
            byte[] ivArray = MD5(keyArray.Concat(_tokenBytes));

            RijndaelManaged rijndaelCipher = new RijndaelManaged();
            rijndaelCipher.Mode = CipherMode.CBC;
            rijndaelCipher.Padding = PaddingMode.PKCS7;
            rijndaelCipher.KeySize = 128;
            rijndaelCipher.BlockSize = 128;
            rijndaelCipher.Key = keyArray;
            rijndaelCipher.IV = ivArray;
            ICryptoTransform transform = rijndaelCipher.CreateDecryptor();

            byte[] cipherBytes = transform.TransformFinalBlock(encryptedMessage, 0, encryptedMessage.Length);
            return Encoding.ASCII.GetString(cipherBytes);
        }

        public override string ToString()
        {
            return RowPacket.ToHexString();
        }
    }
}
