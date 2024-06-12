using Crdt.Changes;
using Crdt.Core;
using Crdt.Entities;

namespace Crdt.Resource;

public class CreateRemoteResourceChange(Guid resourceId, string remoteId) : CreateChange<RemoteResource>(resourceId), IPolyType
{
    public string RemoteId { get; set; } = remoteId;
    public override ValueTask<IObjectBase> NewEntity(Commit commit, ChangeContext context)
    {
        return ValueTask.FromResult<IObjectBase>(new RemoteResource
        {
            Id = EntityId,
            RemoteId = RemoteId
        });
    }

    public static string TypeName => "create:remote-resource";
}
