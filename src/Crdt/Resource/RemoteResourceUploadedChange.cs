using Crdt.Changes;
using Crdt.Entities;

namespace Crdt.Resource;

/// <summary>
/// used when a resource is uploaded to the remote server, stores the remote url in the resource entity
/// </summary>
/// <param name="entityId"></param>
/// <param name="remoteId"></param>
public class RemoteResourceUploadedChange(Guid entityId, string remoteId) : Change<RemoteResource>(entityId), IPolyType
{
    public string RemoteId { get; set; } = remoteId;
    public static string TypeName => "uploaded:RemoteResource";

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
}
