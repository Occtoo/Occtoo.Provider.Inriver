using System;

namespace Occtoo.Generic.Inriver.Model.ConnectorStates
{
    public class LinkListenerStateData : BaseStateData
    {
        public int LinkId { get; set; }
        public int SourceId { get; set; }
        public int TargetId { get; set; }
        public string LinkTypeId { get; set; }
        public int? LinkEntityId { get; set; }
        public DateTime Created { get; set; }
        public override int Id => LinkId;
    }
}
