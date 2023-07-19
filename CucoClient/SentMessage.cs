using Miot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miot
{
    //{ "id": 1, "method": "get_properties", "params": [{ "did": "11-2", "siid": 11, "piid": 2}]}
    //{"id": 1, "method": "action", "params": {"did": "call-2-1", "siid": 2, "aiid": 1, "in": []}}
    public class SentMessage : MiotMessage
    {
        private static ushort _autoIncrementId;

        protected static ushort AutoIncrementId
        {
            get
            {
                _autoIncrementId++;
                if (_autoIncrementId >= 9999)
                {
                    _autoIncrementId = 1;
                }
                return _autoIncrementId;
            }
        }

        protected static Regex sentRegex = new Regex(@"{\s*""id""\s*:\s*(\d*)\s*,\s*""method""\s*:\s*""([^""]*)""\s*,\s*""params""\s*:\s*\s*(\[?{.*}\]?)\s*}");

        public MessageMethod Method { get; set; }

        private string _param;
        private List<MessageParam> _params { get; set; }

        private string rowMessage;



        protected SentMessage()
        {
            rowMessage = null;
            _param = null;
            _params = null;
            Received = false;
        }

        public void AddParams(MessageParam p)
        {
            if (Method != MessageMethod.GetProperties || Method != MessageMethod.SetProperties || Method != MessageMethod.Action)
            {
                throw new ArgumentMiotException("Method error!");
            }
            _params.Add(p);
        }


        public static new SentMessage BuildMessage(string message)
        {
            var msgGroups = sentRegex.Match(message);
            if (!msgGroups.Success)
            {
                throw new NotSupportMessageMiotException("The message text is not support! Message:" + message);
            }
            SentMessage miotMessage = new SentMessage
            {
                Id = Convert.ToUInt16(msgGroups.Groups[1].Value),
                Method = MessageMethod.Unknown.FromString(msgGroups.Groups[2].Value),
                _param = msgGroups.Groups[3].Value,
                rowMessage = message
            };
            return miotMessage;
        }

        public static SentMessage BuildMessage(MessageMethod method, string parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentMiotException("Parameters cannot be null!");
            }
            SentMessage miotMessage = new SentMessage
            {
                Id = AutoIncrementId,
                Method = method,
                _param = parameters
            };
            return miotMessage;
        }

        public static SentMessage BuildMessage(MessageMethod method, List<MessageParam> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentMiotException("Parameters cannot be null!");
            }
            SentMessage miotMessage = new SentMessage
            {
                Id = AutoIncrementId,
                Method = method,
                _params = parameters
            };
            return miotMessage;
        }

        public static SentMessage BuildPropertiesMessage(MessageMethod method, List<Dictionary<string, object>> parameters)
        {
            SentMessage miotMessage = new SentMessage();
            miotMessage.Id = AutoIncrementId;
            miotMessage.Method = method;
            StringBuilder sb = new StringBuilder();

            sb.Append("[");
            foreach (var ps in parameters)
            {
                sb.Append("{");
                foreach (var key in ps.Keys)
                {
                    var val = ps[key];
                    if (val.GetType() == typeof(string))
                        sb.Append($" \"{key}\": \"{val}\",");
                    else if (val.GetType() == typeof(bool))
                        sb.Append($" \"{key}\": \"{val.ToString().ToLower()}\",");
                    else
                        sb.Append($" \"{key}\": {val},");
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append("},");
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append("]");
            miotMessage._param = sb.ToString();
            return miotMessage;
        }

        public static SentMessage BuildActionMessage(Dictionary<string, object> parameters)
        {
            SentMessage miotMessage = new SentMessage();
            miotMessage.Id = AutoIncrementId;
            miotMessage.Method = MessageMethod.Action;
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            foreach (var key in parameters.Keys)
            {
                var val = parameters[key];
                if (val.GetType() == typeof(string))
                    sb.Append($" \"{key}\": \"{val}\",");
                else if (val.GetType() == typeof(bool))
                    sb.Append($" \"{key}\": \"{val.ToString().ToLower()}\",");
                else
                    sb.Append($" \"{key}\": {val},");
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append("}");
            miotMessage._param = sb.ToString();
            return miotMessage;
        }


        public static SentMessage BuildPropertiesMessage(MessageMethod method, int siid, int piid, object val = null)
        {
            string param;
            SentMessage miotMessage = new SentMessage
            {
                Id = AutoIncrementId,
                Method = method
            };

            if (val == null)
            {
                param = $"[{{ \"did\": \"{siid}-{piid}\", \"siid\": {siid}, \"piid\": {piid}}}]";
            }
            else
            {
                if (val.GetType() == typeof(string))
                {
                    param = $"[{{ \"did\": \"{siid}-{piid}\", \"siid\": {siid}, \"piid\": {piid}, \"value\": \"{val}\"}}]";
                }
                else if (val.GetType() == typeof(bool))
                {
                    param = $"[{{ \"did\": \"{siid}-{piid}\", \"siid\": {siid}, \"piid\": {piid}, \"value\": {val.ToString().ToLower()}}}]";
                }
                else
                {
                    param = $"[{{ \"did\": \"{siid}-{piid}\", \"siid\": {siid}, \"piid\": {piid}, \"value\": {val}}}]";
                }
            }
            miotMessage._param = param;
            return miotMessage;
        }

        public static SentMessage BuildActionMessage(int siid, int aiid, string inVal = "[]")
        {
            SentMessage miotMessage = new SentMessage
            {
                Id = AutoIncrementId,
                Method = MessageMethod.Action
            };
            if (inVal == null)
            {
                inVal = "[]";
            }
            string param = $"{{\"did\": \"call-{siid}-{aiid}\", \"siid\": {siid}, \"aiid\": {aiid}, \"in\": {inVal}}}";
            miotMessage._param = param;
            return miotMessage;
        }


        public override string ToString()
        {
            if (rowMessage != null)
            {
                return rowMessage;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"{{\"id\": {Id}, \"method\": \"{this.Method.GetString()}\", \"params\": ");

            if (_params == null)
            {
                sb.Append(_param);
            }
            else
            {
                if (this.Method == MessageMethod.GetProperties)
                {
                    sb.Append("[");
                    foreach (var p in _params)
                    {
                        PropertiesParam pp = (PropertiesParam)p;
                        sb.Append($"{{\"did\": \"{pp.Did}\", \"siid\": {pp.Siid}, \"piid\": {pp.Piid}}},");
                    }
                    sb.Remove(sb.Length - 1, 1);
                    sb.Append("]");
                }
                else if (this.Method == MessageMethod.SetProperties)
                {
                    sb.Append("[");
                    foreach (var p in _params)
                    {
                        PropertiesParam pp = (PropertiesParam)p;
                        sb.Append($"{{\"did\": \"{pp.Did}\", \"siid\": {pp.Siid}, \"piid\": {pp.Piid}, \"value\": {pp.Val}}},");
                    }
                    sb.Remove(sb.Length - 1, 1);
                    sb.Append("]");
                }
                else if (this.Method == MessageMethod.Action)
                {
                    sb.Append("{");
                    ActionParam p = (ActionParam)_params[0];
                    sb.Append($"\"did\": \"{p.Did}\", \"siid\": {p.Siid}, \"piid\": {p.Aiid}, \"in\": {p.In}");
                    sb.Append("}");
                }
                else
                {
                    throw new ArgumentMiotException("Unknown method! Method value:" + Method);
                }
            }

            sb.Append("}");
            return sb.ToString();
        }
    }

    public interface MessageParam
    {

    }

    public struct PropertiesParam : MessageParam
    {
        public string Did;
        public int Siid;
        public int Piid;
        public object Val;
    }

    public struct ActionParam : MessageParam
    {
        public string Did;
        public int Siid;
        public int Aiid;
        public string In;
    }

    public enum MessageMethod
    {
        Unknown,
        GetProperties,
        SetProperties,
        Action
    }

    internal static class MethodEnumExtension
    {
        public static string GetString(this MessageMethod method)
        {
            switch (method)
            {
                case MessageMethod.GetProperties:
                    return "get_properties";
                case MessageMethod.SetProperties:
                    return "set_properties";
                case MessageMethod.Action:
                    return "action";
            }
            throw new ArgumentMiotException("Unknown method! Method value:" + ((int)method).ToString());
        }

        public static MessageMethod FromString(this MessageMethod method2, string method)
        {
            switch (method.ToLower())
            {
                case "get_properties":
                    return MessageMethod.GetProperties;
                case "set_properties":
                    return MessageMethod.SetProperties;
                case "action":
                    return MessageMethod.Action;

            }
            throw new ArgumentMiotException("Unknown method! Method value:" + method);
        }
    }
}

