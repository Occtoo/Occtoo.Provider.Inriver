using inRiver.Remoting.Objects;
using System.ComponentModel;

namespace Occtoo.Generic.Infrastructure.Extensions
{
    public static class FieldExtensions
    {
        public static T GetData<T>(this Field field)
        {
            var data = field.Data;

            switch (data)
            {
                case null:
                    return default;
                case T response:
                    return response;
                default:
                    return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(data.ToString());
            }
        }
    }
}