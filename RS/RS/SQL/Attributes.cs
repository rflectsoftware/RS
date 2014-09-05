using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.SQL
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SQLTableAttribute : Attribute
    {
        public string TableName { get; protected set; }

        public SQLTableAttribute(string TableName)
        {
            this.TableName = TableName;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SQLKeyFieldAttribute : Attribute
    {
        public bool AutoNumber { get; protected set; }

        public SQLKeyFieldAttribute(bool AutoNumber = true)
        {
            this.AutoNumber = AutoNumber;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SQLFieldAttribute : Attribute
    {
        public string FieldName { get; protected set; }

        public SQLFieldAttribute(string FieldName)
        {
            this.FieldName = FieldName;

        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SQLIgnoredFieldAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SQLReadOnlyFieldAttribute : Attribute
    {
    }
}
