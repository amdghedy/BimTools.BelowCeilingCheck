using Autodesk.Revit.DB;

namespace BimTools.BelowCeilingCheck.Models
{
    /// <summary>A document (host or loaded link) together with its transform into host coordinates.</summary>
    public sealed class ModelSource
    {
        public ModelSource(Document document, Transform transform, bool isLink, string name, int instanceId)
        {
            Document = document;
            Transform = transform;
            IsLink = isLink;
            Name = name;
            InstanceId = instanceId;
        }

        public Document Document { get; private set; }

        public Transform Transform { get; private set; }

        public bool IsLink { get; private set; }

        public string Name { get; private set; }

        public int InstanceId { get; private set; }
    }
}
