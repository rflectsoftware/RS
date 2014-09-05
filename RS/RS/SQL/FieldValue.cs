using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.SQL
{
    public class FieldValue
    {
        public string FieldName { get; protected set; }
        public object Value { get; protected set; }

        public FieldValue(string FieldName, object Value)
        {
            this.FieldName = FieldName;
            this.Value = Value;
        }

        public bool Equals(FieldValue obj)
        {
            if (this.FieldName != obj.FieldName)
            {
                return false;
            }
            else
            {
                return (this.Value.Equals(obj.Value));
            }
        }
    }
}
