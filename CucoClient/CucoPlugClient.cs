using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miot
{
    public class CucoPlugClient : MiotDevice
    {
        public CucoPlugClient(string ip, string token, int timeout = 5, int retryCount = 3) : base(ip, token, timeout, retryCount)
        {
        }
    }
}
