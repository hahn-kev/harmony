using Crdt.Changes;
using Crdt.Core;
using Crdt.Entities;

namespace Crdt.Resource;

public class CreateRemoteResourceChange(Guid resourceId, string remoteId) : Change<RemoteResource>(resourceId), IPolyType
{
    public string RemoteId { get; set; } = remoteId;

    public override IObjectBase NewEntity(Commit commit)
    {
        return new RemoteResource
        {
            Id = EntityId,
            RemoteId = RemoteId
        };
    }

    public override ValueTask ApplyChange(RemoteResource entity, ChangeContext context)
    {
        entity.RemoteId = RemoteId;
        return ValueTask.CompletedTask;
    }

    public static string TypeName => "create:remote-resource";
}
