using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.SQL
{
    public class BulkResult
    {
        public int RowsAffected { get; set; }
        public int RowsUpdated { get; set; }
        public int RowsInserted { get; set; }
        public Exception Exception { get; set; }
    }
}
