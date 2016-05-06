using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;

namespace ServiceAPIExtensions.Business
{
    public class ConversionHelpers
    {
        public static dynamic ConvertObjectToExpando(object srcObj)
        {
            var dic = new ExpandoObject() as IDictionary<string, object>;
            var srcType = srcObj.GetType();
            var props = srcType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty);
            foreach (var p in props)
            {
                dic.Add(p.Name, p.GetValue(srcObj));
            }
            return (dynamic)dic;
        }
    }
}