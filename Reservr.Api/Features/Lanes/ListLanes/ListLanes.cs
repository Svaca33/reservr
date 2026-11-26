using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Reservr.Api.Infrastructure;

namespace Reservr.Api.Features.Lanes.ListLanes;

/// <summary>GET /lanes: all lanes, ordered by number.</summary>
public sealed class ListLanes : IEndpoint
{
    public sealed record LaneResponse(Guid Id, int Number);

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/lanes", Handle)
            .WithName(nameof(ListLanes))
            .WithTags("Lanes");

    public static async Task<Ok<LaneResponse[]>> Handle(ReservrDbContext db, CancellationToken cancellationToken)
    {
        var lanes = await db.Lanes
            .AsNoTracking()
            .OrderBy(l => l.Number)
            .Select(l => new LaneResponse(l.Id, l.Number))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(lanes);
    }
}
