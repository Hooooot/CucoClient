using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Miot
{
    public class MiotDevice : IDisposable
    {
        protected UdpClient _miotUdpClient;
        protected IPEndPoint _miotEndPoint;
        protected int _retryCount;
        protected int _timeout;
        protected MiotProtocolPacket _lastPacket;
        private bool disposedValue;
        public readonly string Token;


        public MiotDevice(string ip, string token, int timeout = 5, int retryCount = 3)
        {
            this._timeout = timeout;
            this._retryCount = retryCount;
            this.Token = token;
            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Parse(ip), 54321);
            _miotEndPoint = remoteIpEndPoint;
            _miotUdpClient = new UdpClient();
        }

        /// <summary>
        /// Sending handshake packet to devices
        /// </summary>
        public void Handshake()
        {
            Handshake(_retryCount);
        }

        /// <summary>
        /// Sending handshake packet to the device.
        /// </summary>
        /// <param name="retryCount">Retry counts.</param>
        public void Handshake(int retryCount)
        {
            MiotProtocolPacket packet = MiotProtocolPacket.Build(string.Empty, string.Empty, 0, 0);
            var miot = Send(packet, retryCount);
            _lastPacket = miot;
        }

        /// <summary>
        /// Send a packet to the device.
        /// </summary>
        /// <param name="packet">Miot protocol binary data packet.</param>
        /// <param name="retryCount">Retry counts.</param>
        /// <returns>Data returned by the device.</returns>
        /// <exception cref="InvalidDataReceivedMiotException">Thrown when the received data does not match the sent data.</exception>
        protected MiotProtocolPacket Send(MiotProtocolPacket packet, int retryCount)
        {
            try
            {
                _miotUdpClient.Send(packet.RowPacket, packet.Length, _miotEndPoint);
            }
            catch (Exception ex)
            {
                if (retryCount > 0)
                {
                    Send(packet, --retryCount);
                }
                else
                {
                    throw ex;
                }
            }

            IPEndPoint remoteIpep = new IPEndPoint(IPAddress.Any, 0);

            byte[] rec = _miotUdpClient.Receive(ref remoteIpep);
            var recPacket = MiotProtocolPacket.Parse(rec, Token);
            if ((packet.DeviceId != 0xffffffff && recPacket.DeviceId != packet.DeviceId) || !_miotEndPoint.Address.Equals(remoteIpep.Address))
            {
                if (retryCount > 0)
                {
                    Send(packet, --retryCount);
                }
                else
                {
                    throw new InvalidDataReceivedMiotException("The device id of result packet is not equal with message packet.");
                }
            }
            return recPacket;
        }

        public ReceivedMessage Send(SentMessage message, out byte[] receivedBytes)
        {
            Handshake();
            MiotProtocolPacket packet = MiotProtocolPacket.Build(message, Token, _lastPacket.DeviceId, _lastPacket.Timestamp);
            var miot = Send(packet, _retryCount);
            receivedBytes = miot.RowPacket;
            return ((ReceivedMessage)(miot.Message));
        }

        public ReceivedMessage Send(SentMessage message, out byte[] receivedBytes, int retryCount)
        {
            Handshake();
            MiotProtocolPacket packet = MiotProtocolPacket.Build(message, Token, _lastPacket.DeviceId, _lastPacket.Timestamp);
            var miot = Send(packet, retryCount);
            receivedBytes = miot.RowPacket;
            return ((ReceivedMessage)(miot.Message));
        }

        public ReceivedMessage Send(SentMessage message)
        {
            return Send(message, _retryCount);
        }

        public ReceivedMessage Send(SentMessage message, int retryCount)
        {
            Handshake();
            MiotProtocolPacket packet = MiotProtocolPacket.Build(message, Token, _lastPacket.DeviceId, _lastPacket.Timestamp);
            var miot = Send(packet, retryCount);
            return ((ReceivedMessage)(miot.Message));
        }

        public ReceivedMessage Send(MessageMethod method, string parameters)
        {
            return Send(method, parameters, _retryCount);
        }

        public ReceivedMessage Send(MessageMethod method, string parameters, int retryCount)
        {
            Handshake();
            var message = SentMessage.BuildMessage(method, parameters);

            MiotProtocolPacket packet = MiotProtocolPacket.Build(message, Token, _lastPacket.DeviceId, _lastPacket.Timestamp);
            var miot = Send(packet, retryCount);
            return ((ReceivedMessage)(miot.Message));
        }


        public Result GetProperties(int siid, int piid)
        {
            return GetProperties(siid, piid, _retryCount);
        }

        public Result GetProperties(int siid, int piid, int retryCount)
        {
            Handshake();
            var message = SentMessage.BuildPropertiesMessage(MessageMethod.GetProperties, siid, piid);

            MiotProtocolPacket packet = MiotProtocolPacket.Build(message, Token, _lastPacket.DeviceId, _lastPacket.Timestamp);
            var miot = Send(packet, retryCount);
            return ((ReceivedMessage)(miot.Message)).Results[0];
        }

        public Result SetProperties(int siid, int piid, object val)
        {
            return SetProperties(siid, piid, val, _retryCount);
        }
        public Result SetProperties(int siid, int piid, object val, int retryCount)
        {
            Handshake();
            var message = SentMessage.BuildPropertiesMessage(MessageMethod.SetProperties, siid, piid, val);

            MiotProtocolPacket packet = MiotProtocolPacket.Build(message, Token, _lastPacket.DeviceId, _lastPacket.Timestamp);
            var miot = Send(packet, retryCount);
            return ((ReceivedMessage)(miot.Message)).Results[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="siidPiidPairs">
        /// Format: [(siid1, piid1), (siid2, piid2)]
        /// </param>
        /// <returns></returns>
        public List<Result> GetProperties(List<Tuple<int, int>> siidPiidPairs)
        {
            return GetProperties(siidPiidPairs, _retryCount);
        }

        public List<Result> GetProperties(List<Tuple<int, int>> siidPiidPairs, int retryCount)
        {
            Handshake();
            List<MessageParam> lpp = new List<MessageParam>();
            foreach (Tuple<int, int> siidPiidPair in siidPiidPairs)
            {
                PropertiesParam p = new PropertiesParam();
                p.Did = $"{siidPiidPair.Item1}-{siidPiidPair.Item2}";
                p.Siid = siidPiidPair.Item1;
                p.Piid = siidPiidPair.Item2;
                lpp.Add(p);
            }
            MiotMessage message = SentMessage.BuildMessage(MessageMethod.GetProperties, lpp);
            MiotProtocolPacket packet = MiotProtocolPacket.Build(message, Token, _lastPacket.DeviceId, _lastPacket.Timestamp);
            var miot = Send(packet, retryCount);
            return ((ReceivedMessage)(miot.Message)).Results;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="param">The Properties: PropertiesParam.Val will be ignored.</param>
        /// <returns></returns>
        public List<Result> GetProperties(List<MessageParam> param)
        {
            return GetProperties(param, _retryCount);
        }

        public List<Result> GetProperties(List<MessageParam> param, int retryCount)
        {
            Handshake();
            var message = SentMessage.BuildMessage(MessageMethod.GetProperties, param);
            MiotProtocolPacket packet = MiotProtocolPacket.Build(message, Token, _lastPacket.DeviceId, _lastPacket.Timestamp);
            var miot = Send(packet, retryCount);
            return ((ReceivedMessage)(miot.Message)).Results;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="param"></param>
        /// <param name="retryCount"></param>
        /// <returns></returns>
        public List<Result> SetProperties(List<MessageParam> param)
        {
            return SetProperties(param, _retryCount);
        }
        public List<Result> SetProperties(List<MessageParam> param, int retryCount)
        {
            Handshake();
            MiotMessage message = SentMessage.BuildMessage(MessageMethod.SetProperties, param);
            MiotProtocolPacket packet = MiotProtocolPacket.Build(message, Token, _lastPacket.DeviceId, _lastPacket.Timestamp);
            var miot = Send(packet, retryCount);
            return ((ReceivedMessage)(miot.Message)).Results;
        }

        public Result Action(int siid, int aiid)
        {
            return Action(siid, aiid, "[]", _retryCount);
        }
        public Result Action(int siid, int aiid, string inVal)
        {
            return Action(siid, aiid, inVal, _retryCount);
        }
        public Result Action(int siid, int aiid, string inVal, int retryCount)
        {
            Handshake();
            var message = SentMessage.BuildActionMessage(siid, aiid, inVal);
            MiotProtocolPacket packet = MiotProtocolPacket.Build(message, Token, _lastPacket.DeviceId, _lastPacket.Timestamp);
            Console.WriteLine(message);
            FormatPacketToDisplay(packet.RowPacket);
            var miot = Send(packet, retryCount);
            FormatPacketToDisplay(miot.RowPacket);

            return ((ReceivedMessage)(miot.Message)).Results[0];
        }

        public void FormatPacketToDisplay(byte[] packet)
        {
            Console.WriteLine("Hex: " + packet.ToHexString());
            for (int i = 0; i < packet.Length; i++)
            {
                if (i % 8 == 0)
                {
                    Console.WriteLine();
                    Console.Write((i / 8).ToString("X2") + ": ");
                }
                if (i % 2 == 0)
                {
                    Console.Write(" ");
                }
                Console.Write(packet[i].ToString("X2"));
            }
            Console.WriteLine();
            Console.WriteLine();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _miotUdpClient?.Dispose();
                _miotUdpClient = null;

                disposedValue = true;
            }
        }

        // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~MiotDevice()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
