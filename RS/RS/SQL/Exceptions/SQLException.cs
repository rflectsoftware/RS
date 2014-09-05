using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.SQL.Exceptions
{
    public class SQLException : Exception
    {
        public string QueryString { get; set; }

        #region Constructors

        public SQLException() : base() { }

        public SQLException(string Message) : base(Message) { }

        public SQLException(string Message, Exception InnerException) : base(Message, InnerException) { }

        #endregion
    }
}
