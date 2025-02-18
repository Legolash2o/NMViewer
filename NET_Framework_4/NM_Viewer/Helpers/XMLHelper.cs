using System;
using System.Xml.Linq;

namespace NM_Viewer.Helpers
{
    public static class XMLHelper
    {
        public static T GetValue<T>(this XElement element, string attributeName)
        {
            try
            {
                XName name = XName.Get(attributeName);
                var value = element.Attribute(name);
                if (value == null)
                    return default(T);

                return (T)Convert.ChangeType(value.Value, typeof(T));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw e;
            }

            return default(T);
        }

        public static XElement GetElement(this XElement element, string elementName)
        {
            XName name = XName.Get(elementName);
            return element.Element(name);
        }


    }
}
