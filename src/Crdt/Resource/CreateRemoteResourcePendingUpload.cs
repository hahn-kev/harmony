using Crdt.Changes;
using Crdt.Entities;

namespace Crdt.Resource;

public class CreateRemoteResourcePendingUploadChange: Change<RemoteResource>, IPolyType
{
    public CreateRemoteResourcePendingUploadChange(Guid resourceId) : base(resourceId)
    {
    }

    public override IObjectBase NewEntity(Commit commit)
    {
        return new RemoteResource
        {
            Id = EntityId
        };
    }

    public override ValueTask ApplyChange(RemoteResource entity, ChangeContext context)
    {
        return ValueTask.CompletedTask;
    }

    public static string TypeName => "create:pendingUpload";
}
