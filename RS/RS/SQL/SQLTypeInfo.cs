using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RS.Utilities;

namespace RS.SQL
{
    public class SQLTypeInfo
    {
        public enum SQLKeyType
        {
            None = 0,
            AutoNumber = 1,
            Single = 2,
            Multiple = 3
        }

        //Private constructor
        private SQLTypeInfo()
        {
            Properties = new List<SQLPropertyInfo>();
        }

        //Cached list of type info so that we don't have to reflect over and over for known types already used by the application in progress
        public static Dictionary<Type, SQLTypeInfo> CachedSQLTypeInfos = new Dictionary<Type, SQLTypeInfo>();

        public string TableName { get; set; }

        public List<SQLPropertyInfo> Properties { get; set; }

        public List<SQLPropertyInfo> KeyFields { get; protected set; }

        public SQLKeyType KeyType { get; protected set; }

        private string BaseSelectQuery;

        public string SelectQuery(int? Top = null, string WhereString = null, string GroupByString = null, string OrderByString = null)
        {
            string MyResult = BaseSelectQuery;

            if (!string.IsNullOrWhiteSpace(WhereString)) MyResult += " WHERE " + WhereString;
            if (!string.IsNullOrWhiteSpace(GroupByString)) MyResult += " GROUP BY " + GroupByString;
            if (!string.IsNullOrWhiteSpace(OrderByString)) MyResult += " ORDER BY " + OrderByString;

            if (Top.HasValue)
            {
                MyResult = MyResult.Replace("{TOP}", "TOP " + Top.Value.ToString() + " ");
            }
            else
            {
                MyResult = MyResult.Replace("{TOP}", "");
            }

            return MyResult;
        }

        public static SQLTypeInfo FromType(Type T)
        {
            SQLTypeInfo MyResult = null;

            if (CachedSQLTypeInfos.TryGetValue(T, out MyResult))
            {
                return MyResult;
            }
            else
            {
                return GetSQLInfo(T);
            }
        }

        private static SQLTypeInfo GetSQLInfo(Type T, string Prefix = "")
        {
            SQLTypeInfo myResult = new SQLTypeInfo();

            //Add this item to the cached type info now that the object exists and we know the type
            CachedSQLTypeInfos.Add(T, myResult);

            //Get the table name to pull the class data from
            SQLTableAttribute TableAttribute = T.GetFirstAttribute<SQLTableAttribute>();

            //If no table attribute is specified, default to the class name...
            if (TableAttribute == null)
            {
                myResult.TableName = T.Name;
            }
            else
            {
                myResult.TableName = TableAttribute.TableName;
            }

            //Load the sql information for each property of the class type that's a key field
            foreach (PropertyInfo pi in T.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.GetFirstAttribute<SQLKeyFieldAttribute>() != null))
            {
                SQLPropertyInfo sqlPI = SQLPropertyInfo.FromPropertyInfo(pi, myResult);

                if (sqlPI != null)
                {
                    myResult.Properties.Add(sqlPI);
                }
            }

            myResult.KeyFields = (from x in myResult.Properties where x.KeyField != null select x).ToList();

            if (myResult.KeyFields.Count == 0)
            {
                myResult.KeyType = SQLKeyType.None;
            }
            else if (myResult.KeyFields.Count == 1)
            {
                if (myResult.KeyFields[0].KeyField.AutoNumber)
                {
                    myResult.KeyType = SQLKeyType.AutoNumber;
                }
                else
                {
                    myResult.KeyType = SQLKeyType.Single;
                }
            }
            else
            {
                myResult.KeyType = SQLKeyType.Multiple;
            }

            //Load the sql information for each property of the class type that's not a key field
            foreach (PropertyInfo pi in T.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.GetFirstAttribute<SQLKeyFieldAttribute>() == null))
            {
                SQLPropertyInfo sqlPI = SQLPropertyInfo.FromPropertyInfo(pi, myResult);

                //Skip SQLIgnored Properties
                if (sqlPI != null)
                {
                    myResult.Properties.Add(sqlPI);
                }
            }

            BuildSelectSQL(myResult);

            return myResult;
        }

        public FieldValue[] GetMatchFields<T>(T Obj)
        {
            List<FieldValue> myResult = new List<FieldValue>();

            foreach (SQLPropertyInfo P in this.KeyFields)
            {
                myResult.Add(new FieldValue(P.DatabaseFieldName, Obj.GetPropertyValue(P.UpdateFromProperty)));
            }

            return myResult.ToArray();
        }

        private static void BuildSelectSQL(SQLTypeInfo S)
        {
            //Get the property list
            List<SQLPropertyInfo> selectProperties = S.Properties.ToList();

            List<string> selectFields = new List<string>();

            foreach (SQLPropertyInfo property in selectProperties)
            {
                string selectFieldName;     //Final select field value to put into output statement
                bool useAlias = false;      //Whether the field has to be aliased as the property name or not

                //If the field is a function or has a table alias, don't modify the field, but do use an alias
                if (property.DatabaseFieldName.Contains('(') | property.DatabaseFieldName.Contains('.'))
                {
                    selectFieldName = property.DatabaseFieldName;
                    useAlias = true;
                }
                else
                {
                    //Wrap the field in brackets for safety
                    selectFieldName = "[" + property.DatabaseFieldName + "]";

                    //If the field name doesn't match the property name, set it to alias as the property name
                    if (property.DatabaseFieldName.ToUpper() != property.PropertyInfo.Name.ToUpper())
                    {
                        useAlias = true;
                    }
                }

                //If it's supposed to be aliased at this point, append it here
                if (useAlias)
                {
                    selectFieldName += " AS '" + property.PropertyInfo.Name + "'";
                }

                //Add it to the final select statement
                selectFields.Add(selectFieldName);
            }

            StringBuilder myResult = new StringBuilder("SELECT {TOP}");

            myResult.Append(string.Join(", ", selectFields.Distinct()));
            myResult.Append(" FROM " + S.TableName);

            S.BaseSelectQuery = myResult.ToString();
        }
    }
}
