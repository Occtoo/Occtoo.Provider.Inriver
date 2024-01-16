using inRiver.Remoting.Objects;
using System;

namespace Occtoo.Generic.Inriver.Model.ConnectorStates
{
    public class EntityListenerStateData : BaseStateData
    {
        public Entity Entity { get; set; }
        public string[] Fields { get; set; }
        public DateTime Created { get; set; }
        public override int Id => Entity.Id;
    }
}
