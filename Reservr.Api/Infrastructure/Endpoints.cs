using System.Reflection;

namespace Reservr.Api.Infrastructure;

/// <summary>
/// Implemented by every slice in Features/. Slices are discovered by reflection,
/// so adding a slice does not mean editing Program.cs.
/// </summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}

internal static class EndpointRegistration
{
    public static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(IEndpoint)));

        foreach (var type in endpointTypes)
        {
            ((IEndpoint)Activator.CreateInstance(type)!).MapEndpoint(app);
        }

        return app;
    }
}
