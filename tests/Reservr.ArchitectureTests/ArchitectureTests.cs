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

    // Area and use-case segment, optionally followed by sub-namespaces of the use case.
    private const string UseCaseNamespacePattern = @"^Reservr\.Api\.Features\.[^.]+\.[^.]+(\..+)?$";

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
        // (*).(*) captures area and use case, so a slice is e.g. "Lanes - ListLanes";
        // the trailing .. keeps sub-namespaces of a use case inside its slice.
        // Types directly in an area namespace do not match and are ignored here; A3 forbids them.
        Slices().Matching("Reservr.Api.Features.(*).(*)..")
            .Should().NotDependOnEachOther()
            .Check(Architecture);
    }

    [Fact]
    public void Every_feature_type_is_inside_a_use_case_namespace()
    {
        Types().That().ResideInNamespaceOrBelow(FeaturesNamespace)
            .Should().ResideInNamespaceMatching(UseCaseNamespacePattern)
            .Because("a slice lives in Reservr.Api.Features.<Area>.<UseCase>")
            .Check(Architecture);
    }
}
