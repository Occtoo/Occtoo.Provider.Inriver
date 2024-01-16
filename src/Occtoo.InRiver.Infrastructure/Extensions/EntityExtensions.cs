using inRiver.Remoting.Objects;

namespace Occtoo.Generic.Infrastructure.Extensions
{
    public static class EntityExtensions
    {
        public static T GetData<T>(this Entity entity, string fieldTypeId)
        {
            var field = entity.GetField(fieldTypeId);

            return field == null ? default : field.GetData<T>();
        }
    }
}