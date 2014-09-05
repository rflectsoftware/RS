using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.SQL.Exceptions
{
    public class SQLTimeoutException : SQLException
    {
        public bool Retry { get; set; }
        public int TimeOut { get; set; }

        #region Constructors

        public SQLTimeoutException() : base() { }

        public SQLTimeoutException(string Message) : base(Message) { }

        public SQLTimeoutException(string Message, Exception InnerException) : base(Message, InnerException) { }

        #endregion

        public override string ToString()
        {
            //Appends query to the exception message
            return base.ToString() + "\r\nSQL Query Text:\r\n" + QueryString;
        }
    }
}
