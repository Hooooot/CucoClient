using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miot
{
    public abstract class MiotMessage
    {
        public int Id { get; protected set; }

        public bool Received { get; protected set; }

        public static MiotMessage BuildMessage(string message)
        {

            if (message != null && message.Contains("exe_time"))
            {
                return ReceivedMessage.BuildMessage(message);
            }
            else
            {
                return string.IsNullOrEmpty(message)?null:SentMessage.BuildMessage(message);
            }
        }

        public abstract override string ToString();
    }
}
