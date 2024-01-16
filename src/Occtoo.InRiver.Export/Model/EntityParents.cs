using inRiver.Remoting.Objects;
using System;
using System.Collections.Generic;

namespace Occtoo.Generic.Inriver.Model
{
    public class EntityParents
    {
        public EntityParents()
        {
            Parents = new List<Entity>();
            DeleteKeyProps = new Dictionary<string, string>();
        }

        public bool IsTreeCompleted { get; set; } = true;
        public string DataSource { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public List<Entity> Parents { get; set; }
        public Dictionary<string, string> DeleteKeyProps { get; set; }
    }
}