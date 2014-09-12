using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using RS.SQL;
using RS.Utilities;

namespace RS.SQL
{
    public class Database
    {
        private int LoadStackCounter;

        private Dictionary<Type, object> LoadingObjects;

        //This event passes exceptions along to the calling application for potential handling
        public delegate void SQLExceptionHandler(Exceptions.SQLException e);
        public event SQLExceptionHandler OnSQLException;

        public string ConnectionString { get; protected set; }
        public TimeSpan LastExecuteTime { get; protected set; }
        public TimeSpan TotalExecuteTimes { get; protected set; }
        public int QueriesExecuted { get; protected set; }

        protected MethodInfo GetSingleObjectMethodInfo { get; private set; }
        protected MethodInfo GetObjectCollectionMethodInfo { get; private set; }

        public Database(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;

            this.LoadingObjects = new Dictionary<Type, object>();
            this.LoadStackCounter = 0;
            this.QueriesExecuted = 0;

            Type[] parameterTypes = { typeof(string), typeof(string), typeof(string), typeof(List<SqlParameter>) };
            GetSingleObjectMethodInfo = typeof(Database).GetMethod("GetSingleObject", parameterTypes);

            parameterTypes = new Type[] { typeof(int?), typeof(string), typeof(string), typeof(string), typeof(List<SqlParameter>) };
            GetObjectCollectionMethodInfo = typeof(Database).GetMethod("GetObjectCollection", parameterTypes);
        }

        private void ProcessSQLException(Exceptions.SQLException e)
        {
            //If there are one or more handlers attached to handle this exception, then pass it through the event first
            if (OnSQLException != null)
            {
                OnSQLException(e);
            }
            //Otherwise, throw it as a real exception
            else
            {
                throw new Exception(e.Message);
            }
        }

        private enum SQLQueryType
        {
            DT,
            DS,
            Scalar,
            NonQuery,
            BulkInsert,
            BulkUpdate
        }

        public DataTable QueryDT(string Query, List<SqlParameter> Parameters = null, int Timeout = 1000)
        {
            return ExecuteSQL<DataTable>(SQLQueryType.DT, Query, Parameters, Timeout);
        }

        public DataSet QueryDS(string Query, List<SqlParameter> Parameters = null, int Timeout = 1000)
        {
            return ExecuteSQL<DataSet>(SQLQueryType.DS, Query, Parameters, Timeout);
        }

        public T ScalarQuery<T>(string Query, List<SqlParameter> Parameters = null, int Timeout = 1000)
        {
            return ExecuteSQL<T>(SQLQueryType.Scalar, Query, Parameters, Timeout);
        }

        public List<T> ScalarListQuery<T>(string Query, List<SqlParameter> Parameters = null, int Timeout = 1000)
        {
            DataTable DT = ExecuteSQL<DataTable>(SQLQueryType.DT, Query, Parameters, Timeout);

            List<T> myResult = new List<T>();

            foreach (DataRow row in DT.Rows)
            {
                try
                {
                    //Attempt to cast the current value as T
                    myResult.Add((T)Convert.ChangeType(row[0], typeof(T)));
                }
                catch(InvalidCastException)
                {
                    //If the cast fails, throw an exception
                    throw new Exception("The value \"" + row[0].ToString() + "\" cannot be cast as type \"" + typeof(T).ToString() + "\"");
                }
            }

            return myResult;
        }

        public int NonQuery(string Query, List<SqlParameter> Parameters = null, int Timeout = 1000)
        {
            return ExecuteSQL<int>(SQLQueryType.NonQuery, Query, Parameters, Timeout);
        }

        private T ExecuteSQL<T>(SQLQueryType QueryType, string Query, List<SqlParameter> Parameters = null, int Timeout = 1000)
        {
            //Tracks timeout
            Stopwatch sw = new Stopwatch();
            sw.Start();

            T result = default(T);

            try
            {
                using (SqlConnection DB = new SqlConnection(this.ConnectionString))
                {
                    DB.Open();

                    using (SqlCommand DBCommand = new SqlCommand(Query, DB))
                    {
                        //Set the timeout value
                        DBCommand.CommandTimeout = Timeout;

                        //Set the parameters if any were specified
                        if (Parameters != null)
                        {
                            foreach (SqlParameter p in Parameters)
                                DBCommand.Parameters.Add(p);
                        }

                        switch (QueryType)
                        {

                            case SQLQueryType.DT:

                                DataTable DT = new DataTable();

                                using (SqlDataAdapter DA = new SqlDataAdapter(DBCommand))
                                {
                                    DA.Fill(DT);
                                }

                                result = (T)((object)DT);

                                break;

                            case SQLQueryType.DS:

                                DataSet DS = new DataSet();

                                using (SqlDataAdapter DA = new SqlDataAdapter(DBCommand))
                                {
                                    DA.Fill(DS);
                                }

                                result = (T)((object)DS);

                                break;

                            case SQLQueryType.NonQuery:

                                result = (T)((object)DBCommand.ExecuteNonQuery());

                                break;

                            case SQLQueryType.Scalar:

                                object ScalarResult = (DBCommand.ExecuteScalar());

                                if (ScalarResult == DBNull.Value)
                                {
                                    result = default(T);
                                }
                                else
                                {
                                    result = (T)ScalarResult;
                                }

                                break;

                            default:

                                throw new Exception("Invalid Query Type!");
                        }

                    }
                    DB.Close();
                }
            }
            catch (Exception ex)
            {
                //First check for a time out exception
                if (ex.Message.StartsWith("Timeout expired"))
                {
                    Exceptions.SQLTimeoutException timeout = new Exceptions.SQLTimeoutException("SQL Time Out", ex);
                    timeout.QueryString = Query;
                    timeout.TimeOut = Timeout;

                    //Give the calling app a chance to handle this exception or retry since this error could be recovered given more time
                    ProcessSQLException(timeout);

                    //If the handling app wants us to retry, then retry now with the new timeout... 
                    if (timeout.Retry)
                    {
                        return ExecuteSQL<T>(QueryType, Query, Parameters, timeout.TimeOut);
                    }

                }

                System.Diagnostics.Debug.Print("\r\n*** SQL Exception: " + ex.Message + "\r\nQuery: " + Query + "\r\n");

                Exceptions.SQLException timeout2 = new Exceptions.SQLException("SQL Exception\r\n" + ex.Message, ex);

                timeout2.QueryString = Query;

                //Give the calling app a chance to handle this exception, or option to retry since this error could be recovered given more time
                ProcessSQLException(timeout2);
            }

            this.QueriesExecuted++;

            LastExecuteTime = sw.Elapsed;
            TotalExecuteTimes += sw.Elapsed;

            return result;
        }

        public List<T> GetObjectCollection<T>(int? Top = null, string WhereString = null, string GroupByString = null, string OrderByString = null, List<SqlParameter> Parameters = null) where T : new()
        {
            //Get a copy of the property info for this type for its select string and
            //to pass into ObjectFromDataRow to save repeated lookups in the future
            SQLTypeInfo sqlTypeInfo = SQLTypeInfo.FromType(typeof(T));

            string query = sqlTypeInfo.SelectQuery(Top, WhereString, GroupByString, OrderByString);

            return GetObjectCollection<T>(query, Parameters);
        }

        public List<T> GetObjectCollection<T>(string Query, List<SqlParameter> Parameters = null) where T : new()
        {
            this.LoadStackCounter++;

            List<T> myResult = new List<T>();

            //Get a copy of the property info for this type for its SELECT string and
            //to pass into ObjectFromDataRow row to save repeated lookups in the future
            SQLTypeInfo sqlTypeInfo = SQLTypeInfo.FromType(typeof(T));

            DataTable DTResult = QueryDT(Query, Parameters);

            foreach (DataRow R in DTResult.Rows)
            {
                myResult.Add(ObjectFromDataRow<T>(R, sqlTypeInfo));
            }
      
            this.LoadStackCounter--;

            //If this is the end of the loading stack, clear the loading objects dictionary
            if (LoadStackCounter == 0)
            {
                LoadingObjects.Clear();
            }

            return myResult;
        }

        public T GetSingleObjectByID<T>(object ID) where T : new()
        {
            SQLTypeInfo sqlTypeInfo = SQLTypeInfo.FromType(typeof(T));

            if (sqlTypeInfo.KeyFields.Count != 1)
            {
                throw new Exception("GetSingleObjectByID can only be used on objects with 1 Key Field defined!");
            }

            return GetSingleObject<T>(new FieldValue(sqlTypeInfo.KeyFields[0].DatabaseFieldName, ID));
        }

        public T GetSingleObject<T>(params FieldValue[] FieldValues) where T : new()
        {
            List<string> whereStrings = new List<string>();
            List<SqlParameter> sqlParameters = new List<SqlParameter>();

            foreach (FieldValue FV in FieldValues)
            {
                string ParamName = "@GSO" + FV.FieldName;
                ParamName = ParamName.Replace(".", "");
                whereStrings.Add(FV.FieldName + "=" + ParamName);
                sqlParameters.Add(new SqlParameter(ParamName, FV.Value));
            }

            return GetSingleObject<T>(string.Join(" AND ", whereStrings), null, null, sqlParameters);
        }

        public T GetSingleObject<T>(string WhereString = null, string GroupByString = null, string OrderByString = null, List<SqlParameter> Parameters = null) where T : new()
        {
            this.LoadStackCounter++;

            T obj = new T();
            string query = SQLTypeInfo.FromType(typeof(T)).SelectQuery(1, WhereString, GroupByString, OrderByString);

            DataTable DT = new DataTable();

            DT = QueryDT(query, Parameters);

            if (DT.Rows.Count > 0)
            {
                obj = ObjectFromDataRow<T>(DT.Rows[0]);
                this.LoadStackCounter--;
                return obj;
            }
            else
            {
                this.LoadStackCounter--;
                return default(T);
            }
        }

        public T ObjectFromDataRow<T>(DataRow R, SQLTypeInfo TypeInfo = null) where T : new()
        {
            //Create an output variable of the specified type to return
            T myResult = new T();

            //If the type info wasn't passed into the function, get it now
            if (TypeInfo == null)
            {
                TypeInfo = SQLTypeInfo.FromType(typeof(T));
            }

            //Loop through each SQL property on the object
            foreach (SQLPropertyInfo sqlProperty in TypeInfo.Properties)
            {
                object RecordValue = R[sqlProperty.PropertyInfo.Name];

                if (RecordValue != DBNull.Value)
                {
                    sqlProperty.Set(myResult, RecordValue);
                }
            }

            return myResult;
        }

        public bool DropTableIfExists(string TableName)
        {
            string Query = "IF OBJECT_ID('" + TableName + "', 'U') IS NOT NULL DROP TABLE " + TableName;

            try
            {
                NonQuery(Query);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public BulkResult BulkInsert(DataTable DT, string DestinationTable, List<SqlBulkCopyColumnMapping> ColumnMappings = null, int Timeout = 1000, int BatchSize = 0, int NotifyAfter = 0, Action<long> NotifyAfterFunction = null, bool IdentityInsert = false)
        {
            //If there's nothing to insert, return now
            if (DT.Rows.Count == 0)
            {
                return new BulkResult() { RowsAffected = 0, RowsInserted = 0, RowsUpdated = 0 };
            }

            BulkResult result = new BulkResult();

            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(this.ConnectionString, (IdentityInsert ? SqlBulkCopyOptions.KeepIdentity : SqlBulkCopyOptions.Default)))
            {
                bulkCopy.BulkCopyTimeout = Timeout;
                bulkCopy.DestinationTableName = DestinationTable;

                bulkCopy.BatchSize = BatchSize;
                bulkCopy.NotifyAfter = NotifyAfter;

                if (NotifyAfter > 0 && NotifyAfterFunction != null)
                {
                    bulkCopy.SqlRowsCopied += new SqlRowsCopiedEventHandler((o, e) => { NotifyAfterFunction(e.RowsCopied); });
                }

                if (ColumnMappings == null)
                {
                    foreach (DataColumn c in DT.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(c.ColumnName, c.ColumnName));
                    }
                }
                else
                {
                    foreach (SqlBulkCopyColumnMapping c in ColumnMappings)
                    {
                        bulkCopy.ColumnMappings.Add(c);
                    }
                }

                try
                {
                    bulkCopy.WriteToServer(DT);
                }
                catch (Exception ex)
                {
                    result.Exception = ex;
                    System.Diagnostics.Debug.Print("Bulk Insert Error: " + ex.Message);
                }

                bulkCopy.Close();

            }

            result.RowsInserted = DT.Rows.Count;
            result.RowsAffected = DT.Rows.Count;

            return result;
        }
    }
}
