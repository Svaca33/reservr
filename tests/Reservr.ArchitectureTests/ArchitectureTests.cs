using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Reservr.Api.Infrastructure;
using Reservr.Domain;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static ArchUnitNET.Fluent.Slices.SliceRuleDefinition;

namespace Reservr.ArchitectureTests;

/// <summary>
/// Rules a slice (one use case: one command or one query) must follow.
/// A slice lives in its own namespace Reservr.Api.Features.&lt;Area&gt;.&lt;UseCase&gt;.
/// </summary>
public class ArchitectureTests
{
    private const string FeaturesNamespace = "Reservr.Api.Features";

    // Exactly an area and a use-case segment. A use case has no sub-namespaces:
    // ArchUnitNET would treat each sub-namespace as a separate slice.
    private const string UseCaseNamespacePattern = @"^Reservr\.Api\.Features\.[^.]+\.[^.]+$";

    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Lane).Assembly;
    private static readonly System.Reflection.Assembly ApiAssembly = typeof(IEndpoint).Assembly;

    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(DomainAssembly, ApiAssembly)
        .Build();

    [Fact]
    public void Domain_does_not_depend_on_EF_Core_or_ASP_NET_Core()
    {
        // includeReferenced: EF Core and ASP.NET Core are not loaded into the architecture,
        // so their types are only known as dependency targets.
        var frameworkTypes = Types(true).That()
            .ResideInNamespaceOrBelow("Microsoft.EntityFrameworkCore")
            .Or().ResideInNamespaceOrBelow("Microsoft.AspNetCore");

        Types().That().ResideInAssembly(DomainAssembly)
            .Should().NotDependOnAny(frameworkTypes)
            .Check(Architecture);
    }

    [Fact]
    public void Use_case_does_not_depend_on_another_use_case()
    {
        // (*).(*) captures area and use case, so each use case is its own slice.
        // Types in other namespaces (directly in an area, or below a use case) do not match
        // and are ignored here; A3 forbids them, so together the two rules leave no gap.
        Slices().Matching("Reservr.Api.Features.(*).(*)")
            .Should().NotDependOnEachOther()
            .Check(Architecture);
    }

    [Fact]
    public void Every_feature_type_is_inside_a_use_case_namespace()
    {
        Types().That().ResideInNamespaceOrBelow(FeaturesNamespace)
            .Should().ResideInNamespaceMatching(UseCaseNamespacePattern)
            .Because("a slice lives in exactly Reservr.Api.Features.<Area>.<UseCase>, with no sub-namespaces")
            .Check(Architecture);
    }
}
