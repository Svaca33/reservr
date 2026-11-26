using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Reservr.Api.Infrastructure;
using Reservr.Domain;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

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
        var useCases = Architecture.Types
            .Select(t => t.Namespace.FullName.Split('.'))
            .Where(segments => segments.Length >= 5 && string.Join('.', segments[..3]) == FeaturesNamespace)
            .Select(segments => string.Join('.', segments[..5]))
            .Distinct()
            .ToList();

        Assert.NotEmpty(useCases);

        var rules = useCases.Select(useCase =>
            Types().That().ResideInNamespaceOrBelow(useCase)
                .Should().NotDependOnAny(Types().That()
                    .ResideInNamespaceOrBelow(FeaturesNamespace)
                    .And().DoNotResideInNamespaceOrBelow(useCase))
                .Because($"{useCase} is a slice and slices never reference each other"));

        rules.Aggregate<IArchRule>((all, rule) => all.And(rule)).Check(Architecture);
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
