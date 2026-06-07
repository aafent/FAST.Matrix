using Microsoft.Extensions.DependencyInjection;

namespace FAST.Matrix.Contracts.Applets;

/// <summary>
/// Describes a single private service registration belonging to one applet.
/// Produced by the attribute scanner; consumed by <c>AppletContainerRegistry</c>.
/// </summary>
public sealed record AppletServiceDescriptor
{
    /// <summary>The service interface or abstract type to register.</summary>
    public required Type ServiceType { get; init; }

    /// <summary>The concrete implementation type.</summary>
    public required Type ImplementationType { get; init; }

    /// <summary>DI lifetime for this registration.</summary>
    public ServiceLifetime Lifetime { get; init; } = ServiceLifetime.Scoped;
}
