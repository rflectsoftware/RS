using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RS.Utilities
{
    public static class Extensions
    {
        #region Attributes

        public static T GetFirstAttribute<T>(this object Object)
        {
            List<T> myResult = GetAttributes<T>(Object);

            //If there are no results, return null (or default(T))
            if (myResult.Count() == 0)
            {
                return default(T);
            }

            //Otherwise, return the first result
            return myResult[0];
        }

        public static List<T> GetAttributes<T>(this object Object)
        {
            object[] myResult;

            if (Object is Type)
            {
                myResult = ((Type)Object).GetCustomAttributes(typeof(T), true);
            }
            else if (Object is PropertyInfo)
            {
                myResult = ((PropertyInfo)Object).GetCustomAttributes(typeof(T), true);
            }
            else
            {
                myResult = Object.GetType().GetCustomAttributes(typeof(T), true);
            }

            return new List<T>(myResult.ToList().Cast<T>());
        }

        #endregion

        #region Object Properties

        public static Type GetPropertyType(this object Object, string PropertyName)
        {
            //Supports recursion using "." in property name to get nested properties
            int dotLocation = PropertyName.IndexOf('.');

            //If there's no recursion, just pull the requested property of the current object
            if (dotLocation < 0)
            {
                return Object.GetType().GetProperty(PropertyName).GetType();
            }

            //If there is recursion, get the object referenced before the "." and call again on that object
            string currentProperty = PropertyName.Substring(0, dotLocation);
            object currentPropertyObject = Object.GetType().GetProperty(currentProperty).GetValue(Object, null);

            //Now that we have the child, call again with the portion after the first "."
            return GetPropertyType(currentPropertyObject, PropertyName.Substring(dotLocation + 1));
        }

        public static object GetPropertyValue(this object Object, string PropertyName)
        {
            //Supports recursion using "." in property name to get nested properties
            int dotLocation = PropertyName.IndexOf('.');

            //If there's no recursion, just pull the requested property of the current object
            if (dotLocation < 0)
            {
                if (Object == null) return null;
                return Object.GetType().GetProperty(PropertyName).GetValue(Object, null);
            }

            //If there is recursion, get the object referenced before the "." and call again on that object
            string currentProperty = PropertyName.Substring(0, dotLocation);
            object currentPropertyObject = Object.GetType().GetProperty(currentProperty).GetValue(Object, null);

            //Now that we have the child, call again with the portion after the first "."
            return GetPropertyValue(currentPropertyObject, PropertyName.Substring(dotLocation + 1));
        }

        #endregion

    }


}
