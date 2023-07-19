using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miot
{
    //{ "id":1,"result":[{ "did":"11-2","siid":11,"piid":2,"code":0,"value":52}],"exe_time":90}
    //{"id":1,"result":{"did":"call-2-1","siid":2,"aiid":1,"code":0},"exe_time":110}
    //{"id":1,"result":[{"did":"2-1","siid":2,"piid":1,"code":0,"value":false}],"exe_time":170}
    //{"id":1,"result":[{"did":"2-1","siid":2,"piid":1,"code":0}],"exe_time":230}
    //{"id":1,"error":{"code":-9999,"message":"user ack timeout"},"exe_time":4010}
    public class ReceivedMessage : MiotMessage
    {
        public string Result { get; private set; }
        public List<Result> Results { get; private set; }
        public int ExeTime { get; private set; }

        private string _rowMessage;

        protected Regex msgRegex = new Regex(@"{\s*""id""\s*:\s*(\d*)\s*,\s*""result""\s*:\s*(\[?{.*}\]?)\s*,\s*""exe_time""\s*:\s*(\d*)\s*}");
        protected Regex errorRegex = new Regex(@"{\s*""id""\s*:\s*(\d*)\s*,\s*""error""\s*:\s*({[^}]*})\s*,\s*""exe_time""\s*:\s*(\d*)\s*}");

        protected Regex errorResRegex = new Regex(@"{\s*""code""\s*:\s*([-\d]*)\s*,\s*""message""\s*:\s*""\s*([^""]*)\s*""\s*}");
        protected Regex resRegex = new Regex(@"{\s*""did""\s*:\s*""([^""]*)""\s*,\s*""siid""\s*:\s*(\d*)\s*,\s*""piid""\s*:\s*(\d*)\s*,\s*""code""\s*:\s*(\d*)(,\s*""value""\s*:\s*([^}]*)\s*)?}");
        protected Regex resActionRegex = new Regex(@"{\s*""did""\s*:\s*""([^""]*)""\s*,\s*""siid""\s*:\s*(\d*)\s*,\s*""aiid""\s*:\s*(\d*)\s*,\s*""code""\s*:\s*(\d*)\s*}");

        protected ReceivedMessage(string message)
        {
            _rowMessage = message;
            var res = msgRegex.Match(message);
            if (!res.Success)
            {
                res = errorRegex.Match(message);
                if (!res.Success)
                {
                    throw new NotSupportResultMiotException("Unsupport result message:" + message);
                }
                string id = res.Groups[1].Value;
                Id = Convert.ToInt32(id);
                Result = res.Groups[2].Value;
                var error = errorResRegex.Match(Result);
                if (!error.Success)
                {
                    throw new NotSupportResultMiotException("Unsupport result message:" + message);
                }

                int Code = Convert.ToInt32(error.Groups[1].Value);
                string ErrorMessage = error.Groups[2].Value;
                Results = new List<Result>();
                Result r = Miot.Result.Build(Code, ErrorMessage);
                Results.Add(r);
            }
            else
            {
                Id = Convert.ToInt32(res.Groups[1].Value);

                Result = res.Groups[2].Value;
                ExeTime = Convert.ToInt32(res.Groups[3].Value);
                if (Result.Contains("aiid"))
                {
                    var resGroup = resActionRegex.Match(Result);
                    if (!resGroup.Success)
                    {
                        throw new NotSupportResultMiotException("Unsupport result message:" + message);
                    }
                    Results = new List<Result>();
                    string Did = resGroup.Groups[1].Value;
                    int Siid = Convert.ToInt32(resGroup.Groups[2].Value);
                    int Aiid = Convert.ToInt32(resGroup.Groups[3].Value);
                    int Code = Convert.ToInt32(resGroup.Groups[4].Value);
                    Result r = Miot.Result.Build(Did, Siid, Aiid, Code);
                    Results.Add(r);
                }
                else
                {
                    var resGroup = resRegex.Matches(Result);
                    if (resGroup.Count == 0)
                    {
                        throw new NotSupportResultMiotException("Unsupport result message:" + message);
                    }
                    Results = new List<Result>();
                    bool needVal = false;
                    if (Result.Contains("value"))
                    {
                        needVal = true;
                    }
                    for (int i = 0; i < resGroup.Count; i++)
                    {
                        string did = resGroup[i].Groups[1].Value;
                        int siid = Convert.ToInt32(resGroup[i].Groups[2].Value);
                        int piid = Convert.ToInt32(resGroup[i].Groups[3].Value);
                        int code = Convert.ToInt32(resGroup[i].Groups[4].Value);
                        object val = null;
                        if (needVal)
                            val = resGroup[i].Groups[6].Value.ToString();
                        Result r = Miot.Result.Build(did, siid, piid, code, val);
                        Results.Add(r);
                    }
                }
            }
            Received = true;
        }

        public static new ReceivedMessage BuildMessage(string message)
        {
            return new ReceivedMessage(message);
        }

        public override string ToString()
        {
            return _rowMessage;
        }
    }

    public class Result
    {
        public string Did { get; private set; }
        public string ErrorMessage { get; private set; }
        public int Siid { get; private set; }
        public int Piid { get; private set; }
        public int Aiid { get; private set; }
        public int Code { get; private set; }
        public object ResultValue { get; private set; }

        protected Result() { }


        public static Result Build(string did, int siid, int aiid, int code)
        {
            Result r = new Result();
            r.Did = did;
            r.Siid = siid;
            r.Aiid = aiid;
            r.Code = code;
            r.ErrorMessage = string.Empty;
            r.Piid = -1;
            r.ResultValue = null;
            return r;
        }

        public static Result Build(string did, int siid, int piid, int code, object val)
        {
            Result r = new Result();
            r.Did = did;
            r.Siid = siid;
            r.Piid = piid;
            r.Code = code;
            r.ErrorMessage = string.Empty;
            r.Aiid = -1;
            r.ResultValue = val;
            return r;
        }

        public static Result Build(int code, string errorMessage)
        {
            Result r = new Result();
            r.Did = string.Empty;
            r.Siid = -1;
            r.Piid = -1;
            r.Code = code;
            r.ErrorMessage = errorMessage;
            r.Aiid = -1;
            r.ResultValue = null;
            return r;
        }



        public override string ToString()
        {
            if (string.IsNullOrEmpty(ErrorMessage))
            {
                if (ResultValue == null && Aiid > 0)
                {
                    return $"{{\"did\":\"{Did}\", \"siid\":{Siid}, \"aiid\":{Aiid}, \"code\":{Code}}}";
                }
                else if (ResultValue != null)
                {
                    return $"{{\"did\":\"{Did}\", \"siid\":{Siid}, \"piid\":{Piid}, \"code\":{Code}, \"value\":{ResultValue}}}";
                }
                else
                {
                    return $"{{\"did\":\"{Did}\", \"siid\":{Siid}, \"piid\":{Piid}, \"code\":{Code}}}";
                }
            }
            else
            {
                return $"{{\"code\":{Code}, \"message\":{ErrorMessage}}}";
            }

        }
    }
}
