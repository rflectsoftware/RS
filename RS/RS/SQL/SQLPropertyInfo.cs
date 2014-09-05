using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using RS.Utilities;

namespace RS.SQL
{
    public class SQLPropertyInfo
    {
        //Private constructor so this class can't be created by outside object
        private SQLPropertyInfo(PropertyInfo pi, SQLTypeInfo TypeInfo)
        {
            this.PropertyInfo = pi;
            this.TypeInfo = TypeInfo;

            InitializeGet();
            InitializeSet();
        }

        public PropertyInfo PropertyInfo { get; protected set; }
        public SQLTypeInfo TypeInfo { get; protected set; }

        public Func<object, object> GetDelegate;
        public Action<object, object> SetDelegate;

        //Master database field that is selected to populate this property
        public string DatabaseFieldName { get; set; }

        //Child property that is used to udpate the master database field corrosponding to this property
        public string UpdateFromProperty { get; set; }

        public bool ReadOnly { get; set; }

        public SQLKeyFieldAttribute KeyField { get; set; }

        public object Get(object instance)
        {
            return this.GetDelegate(instance);
        }

        public void Set(object instance, object value)
        {
            this.SetDelegate(instance, value);
        }

        private void InitializeSet()
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(object), "value");

            //Use "value as T" rather than (T)value if we're not dealing with a value type
            UnaryExpression instanceCast = (!this.PropertyInfo.DeclaringType.IsValueType) ? Expression.TypeAs(instance, this.PropertyInfo.DeclaringType) : Expression.Convert(instance, this.PropertyInfo.DeclaringType);

            UnaryExpression valueCast = (!this.PropertyInfo.PropertyType.IsValueType) ? Expression.TypeAs(value, this.PropertyInfo.PropertyType) : Expression.Convert(value, this.PropertyInfo.PropertyType);

            this.SetDelegate = Expression.Lambda<Action<object, object>>(Expression.Call(instanceCast, this.PropertyInfo.GetSetMethod(), valueCast), new ParameterExpression[] { instance, value }).Compile();
        }

        private void InitializeGet()
        {
            var instance = Expression.Parameter(typeof(object), "instance");

            UnaryExpression instanceCast = (!this.PropertyInfo.DeclaringType.IsValueType) ? Expression.TypeAs(instance, this.PropertyInfo.DeclaringType) : Expression.Convert(instance, this.PropertyInfo.DeclaringType);

            this.GetDelegate = Expression.Lambda<Func<object, object>>(Expression.TypeAs(Expression.Call(instanceCast, this.PropertyInfo.GetGetMethod()), typeof(object)), instance).Compile();
        }

        public static SQLPropertyInfo FromPropertyInfo(PropertyInfo pi, SQLTypeInfo TypeInfo)
        {
            //If it's SQL ignored, just return null
            if (pi.GetFirstAttribute<SQLIgnoredFieldAttribute>() != null)
            {
                return null;
            }

            SQLPropertyInfo myResult = new SQLPropertyInfo(pi, TypeInfo);

            //Mark as read only now, if applicable
            myResult.ReadOnly = (pi.GetFirstAttribute<SQLReadOnlyFieldAttribute>() != null);

            myResult.KeyField = pi.GetFirstAttribute<SQLKeyFieldAttribute>();

            //Set the default mappings for standard properties
            SQLFieldAttribute FieldAttribute = pi.GetFirstAttribute<SQLFieldAttribute>();

            //Set the primary field name based on the field attribute if specified, or the property name itself if not
            myResult.DatabaseFieldName = (FieldAttribute == null ? myResult.PropertyInfo.Name : FieldAttribute.FieldName);

            //If the database field is a function of any kind it has to be read-only
            if (myResult.DatabaseFieldName.Contains('('))
            {
                myResult.ReadOnly = true;
            }
            else
            {
                //It should be an updatable property (unless flagged as read only by the user);
                //The update from property is always this property's name
                myResult.UpdateFromProperty = pi.Name;
            }

            return myResult;
        }

    }
}
