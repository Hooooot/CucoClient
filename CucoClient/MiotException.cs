using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miot
{
    [Serializable]
    public class MiotException : Exception 
    {
        public MiotException() : base() { }
        public MiotException(string message) : base(message) { }
        public MiotException(string message, Exception innerException) : base(message, innerException) { }
    }

    [Serializable]
    public class InvalidDataReceivedMiotException : MiotException 
    {
        public InvalidDataReceivedMiotException(): base() { }
        public InvalidDataReceivedMiotException(string message): base(message) { }
        public InvalidDataReceivedMiotException(string message, Exception innerException) : base(message, innerException) { }
    }

    [Serializable]
    public class NotSupportResultMiotException : MiotException
    {
        public NotSupportResultMiotException() : base() { }
        public NotSupportResultMiotException(string message): base(message) { }
        public NotSupportResultMiotException(string message, Exception innerException) : base(message, innerException) { }
    }

    [Serializable]
    public class NotSupportMessageMiotException : MiotException
    {
        public NotSupportMessageMiotException() : base() { }
        public NotSupportMessageMiotException(string message) : base(message) { }
        public NotSupportMessageMiotException(string message, Exception innerException) : base(message, innerException) { }
    }


    [Serializable]
    public class ArgumentMiotException : MiotException
    {
        public ArgumentMiotException() : base() { }
        public ArgumentMiotException(string message) : base(message) { }
        public ArgumentMiotException(string message, Exception innerException) : base(message, innerException) { }
    }

}
