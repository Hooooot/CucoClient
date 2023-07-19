using Miot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Miot
{
    internal class Program
    {
        static string ip;
        static string token;
        static MessageMethod method;

        static string param = string.Empty;
        static int siid = -1;
        static int piid = -1;
        static int aiid = -1;
        static object val;
        static bool simpleData = false;
        static bool binaryData = false;
        static bool decodeData = false;
        static byte[] encryptedData = null;

        static MiotDevice device;



        private struct ParametersStruct
        {
            public HandleParameters Handle;
            public string Description;
            public string Usage;
        }

        private delegate void HandleParameters(string[] args, ref int index, string nextArg);

        private static readonly Dictionary<string, ParametersStruct> SupportedParameters = new Dictionary<string, ParametersStruct>
        {
            { "-ip", new ParametersStruct{ Handle=HandleIp, Description="IP address of Miot device.", Usage="-ip 192.168.1.1"} },
            { "-t", new ParametersStruct{ Handle=HandleToken, Description="Token of Miot device.", Usage="-t 1c2b...a07"} },
            { "-m", new ParametersStruct{ Handle=HandleMethod, Description="Method of Miot device. Only can be one of \"get_properties\",\"set_properties\",\"action\".", Usage="-m \"action\""} },
            { "-p", new ParametersStruct{ Handle=HandleParams, Description="Method parameters of Miot device.", Usage="-m \"get_properties\" -p '[{ \"did\": \"11-2\", \"siid\": 11, \"piid\": 2}]'\n          -m \"set_properties\" -p '[{ \"did\": \"11-2\", \"siid\": 11, \"piid\": 2, \"value\": 1}]'\n          -m \"action\" -p '{ \"did\": \"11-2\", \"siid\": 11, \"aiid\": 2}'"} },
            { "-v", new ParametersStruct{ Handle=HandleValue, Description="Method parameters of Miot device.", Usage="-m \"get_properties\" -v <siid> <piid>\n          -m \"set_properties\" -v <siid> <piid> <value>\n          -m \"action\" -v <siid> <aiid>"} },
            { "-s", new ParametersStruct{ Handle=HandleSimplify, Description="Only show the received value. Cannot be used together with the -b.", Usage="-s"} },
            { "-b", new ParametersStruct{ Handle=HandleBinary, Description="Show the received binary message. Cannot be used together with the -s.", Usage="-b"} },
            { "-d", new ParametersStruct{ Handle=HandleDecode, Description="Decode the encrypted binary data. -t parameter is necessary.", Usage="-t 1c2b...a07 -d 21310070000000...B4"} },

            { "-?", new ParametersStruct{ Handle=HandleHelp, Description="Show help."} },
            { "-h", new ParametersStruct{ Handle=HandleHelp, Description="Show help."} },
        };


        public static void AnalysisParameters(string[] parameters)
        {
            if (parameters== null || parameters.Length == 0)
            {
                var s = -1;
                HandleHelp(parameters, ref s, null);
                return;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                string parameter = parameters[i].ToLower().Replace("/", "-").Replace("'", "\"");
                try
                {
                    var s = SupportedParameters[parameter];
                    string nextArg = string.Empty;
                    if (i < parameters.Length - 1)
                    {
                        nextArg = parameters[i + 1];
                    }
                    s.Handle(parameters, ref i, nextArg);
                }
                catch (Exception e)
                {
                    HandleError(parameters, ref i, e);
                    return;
                }
            }

            if (decodeData)
            {
                var packet = MiotProtocolPacket.Parse(encryptedData, token);
                Console.WriteLine(packet.Message);
            }
            else
            {
                device = new MiotDevice(ip, token);
                ReceivedMessage r;
                if (string.IsNullOrEmpty(param))
                {
                    if (method == MessageMethod.Action)
                    {
                        r = device.Send(SentMessage.BuildActionMessage(siid, aiid), out encryptedData);
                    }
                    else if (method == MessageMethod.GetProperties)
                    {
                        r = device.Send(SentMessage.BuildPropertiesMessage(method, siid, piid), out encryptedData);
                    }
                    else
                    {
                        r = device.Send(SentMessage.BuildPropertiesMessage(method, siid, piid, val), out encryptedData);
                    }
                }
                else
                {
                    r = device.Send(SentMessage.BuildMessage(method, param), out encryptedData);
                }
                if (simpleData)
                {
                    Console.WriteLine(r.Results[0].ResultValue);
                }
                else if (binaryData)
                {
                    Console.WriteLine(encryptedData.ToHexString());
                }
                else
                {
                    Console.WriteLine(r.Result);
                }
            }
        }
        private static void HandleIp(string[] args, ref int index, string nextArg)
        {
            var _ = new IPEndPoint(IPAddress.Parse(nextArg), 54321);
            ip = nextArg;
            index++;
        }

        private static void HandleToken(string[] args, ref int index, string nextArg)
        {
            token = nextArg.Replace("'", "");
            if (token.Length != 32)
            {
                throw new ArgumentMiotException("Illegal token:" + nextArg);
            }
            token = nextArg;
            index++;
        }

        private static void HandleMethod(string[] args, ref int index, string nextArg)
        {
            method = MessageMethod.Unknown.FromString(nextArg);
            index++;
        }

        /// <summary>
        /// -ps
        /// </summary>
        /// <param name="args"></param>
        /// <param name="index"></param>
        /// <param name="nextArg"></param>
        private static void HandleParams(string[] args, ref int index, string nextArg)
        {
            if (piid > 0)
            {
                throw new ArgumentMiotException("The -p command cannot be used together with the -v command");
            }
            param = nextArg.Replace("'", "\"");
            index++;
        }

        private static void HandleValue(string[] args, ref int index, string nextArg)
        {
            if (!string.IsNullOrEmpty(param))
            {
                throw new ArgumentMiotException("The -v command cannot be used together with the -p command");
            }
            if (method == MessageMethod.SetProperties)
            {
                if (index < args.Length - 3)
                {
                    try
                    {
                        siid = Convert.ToInt32(nextArg);
                        string next2Arg = args[index + 2].Trim();
                        string next3Arg = args[index + 3].Trim();
                        piid = Convert.ToInt32(next2Arg);
                        if (int.TryParse(next3Arg, out int outValInt))
                        {
                            val = outValInt;
                        }
                        else if (bool.TryParse(next3Arg, out bool outValBool))
                        {
                            val = outValBool;
                        }
                        else if (float.TryParse(next3Arg, out float outValFloat))
                        {
                            val = outValFloat;
                        }
                        else
                        {
                            val = next3Arg;
                        }
                        index += 3;
                    }
                    catch
                    {
                        throw new ArgumentMiotException("Illegal \"-v\" parameter data");
                    }
                }
                else
                {
                    throw new ArgumentMiotException("There is too little parameter data for \"-v\".");
                }
            }
            else if (method == MessageMethod.GetProperties)
            {
                if (index < args.Length - 2)
                {
                    try
                    {
                        siid = Convert.ToInt32(nextArg);
                        string next2Arg = args[index + 2].Trim();
                        piid = Convert.ToInt32(next2Arg);
                        index += 2;
                    }
                    catch
                    {
                        throw new ArgumentMiotException("Illegal \"-v\" parameter data");
                    }
                }
                else
                {
                    throw new ArgumentMiotException("There is too little parameter data for \"-v\".");
                }
            }
            else if (method == MessageMethod.Action)
            {
                if (index < args.Length - 2)
                {
                    try
                    {
                        siid = Convert.ToInt32(nextArg);
                        string next2Arg = args[index + 2].Trim();
                        aiid = Convert.ToInt32(next2Arg);
                        index += 2;
                    }
                    catch
                    {
                        throw new ArgumentMiotException("Illegal \"-v\" parameter data");
                    }
                }
                else
                {
                    throw new ArgumentMiotException("There is too little parameter data for \"-v\".");
                }
            }
            else
            {
                throw new ArgumentMiotException("Declare the \"-m\" parameter firstly.");
            }
        }

        private static void HandleSimplify(string[] args, ref int index, string nextArg)
        {
            if (binaryData)
            {
                throw new ArgumentMiotException("The -s command cannot be used together with the -b command");
            }
            binaryData = false;
            simpleData = true;
        }
        private static void HandleBinary(string[] args, ref int index, string nextArg)
        {
            if (simpleData)
            {
                throw new ArgumentMiotException("The -b command cannot be used together with the -s command");
            }
            binaryData = true;
            simpleData = false;
        }

        private static void HandleDecode(string[] args, ref int index, string nextArg)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentMiotException("\"-t\" parameter is necessary");
            }
            decodeData = true;
            encryptedData = nextArg.ToBytes();
            index++;
        }

        static void HandleHelp(string[] args, ref int index, string nextArg)
        {
            StringBuilder sb = new StringBuilder();
            sb = CommandInfoList(sb);
            Console.WriteLine(sb.ToString());
        }

        private static void HandleError(string[] args, ref int index, Exception e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Unknow command: {args[index]}, {e.Message}");
            sb.AppendLine();
            sb.AppendLine($"Support command:");
            sb = CommandInfoList(sb);
            Console.WriteLine(sb.ToString());
        }

        private static StringBuilder CommandInfoList(StringBuilder sb)
        {
            foreach (var cmd in SupportedParameters)
            {
                sb.AppendLine($"    {cmd.Key}: {cmd.Value.Description}");
                sb.AppendLine($"        Usage:");
                sb.AppendLine($"          {cmd.Value.Usage}");
                sb.AppendLine();
            }
            sb.AppendLine("Example:");
            sb.AppendLine("    set properties which SIID=2,PIID=1: client.exe -ip 192.168.1.1 -t 1c2bc1d0b63...79d47f0da07 -m \"set_properties\" -v 2 1 true");
            sb.AppendLine("    get properties which SIID=2,PIID=1: client.exe -ip 192.168.1.1 -t 1c2bc1d0b63...79d47f0da07 -m \"get_properties\" -v 2 1");
            sb.AppendLine("    do action which SIID=2,AIID=1     : client.exe -ip 192.168.1.1 -t 1c2bc1d0b63...79d47f0da07 -m \"action\" -v 2 1");
            sb.AppendLine("    client.exe -ip 192.168.1.1 -t 1c2bc1d0b63...79d47f0da07  -m \"action\" -v 2 1 -b");
            sb.AppendLine("    client.exe -t 1c2bc1d0b63...79d47f0da07  -d 21779DB87D904...097F");
            return sb;
        }


        static void Main(string[] args)
        {
            AnalysisParameters(args);
        }
    }
}
