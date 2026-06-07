using Microsoft.Extensions.DependencyInjection;

namespace FAST.Matrix.Contracts.Attributes;

/// <summary>
/// Declares a private service registration for an applet.
/// Apply one or more instances to the class implementing <see cref="Applets.IApplet"/>.
/// The Matrix engine reads these at discovery time (before instantiation) via reflection,
/// building the applet's isolated DI sandbox before <c>OnAppletInitAsync</c> is called.
/// </summary>
/// <example>
/// <code>
/// [AppletService(typeof(ICustomerRepository), typeof(SqlCustomerRepository))]
/// [AppletService(typeof(IInvoiceService), typeof(InvoiceService), ServiceLifetime.Singleton)]
/// public class CustomerGroupsApplet : IApplet { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AppletServiceAttribute : Attribute
{
    /// <summary>The service interface or abstract type to register.</summary>
    public Type ServiceType { get; }

    /// <summary>The concrete implementation type.</summary>
    public Type ImplementationType { get; }

    /// <summary>DI lifetime. Defaults to <see cref="ServiceLifetime.Scoped"/>.</summary>
    public ServiceLifetime Lifetime { get; }

    /// <param name="serviceType">The service interface or abstract type.</param>
    /// <param name="implementationType">The concrete implementation.</param>
    /// <param name="lifetime">DI lifetime (default: Scoped).</param>
    public AppletServiceAttribute(
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (!implementationType.IsClass || implementationType.IsAbstract)
            throw new ArgumentException(
                $"ImplementationType '{implementationType.FullName}' must be a concrete class.",
                nameof(implementationType));

        if (!serviceType.IsAssignableFrom(implementationType))
            throw new ArgumentException(
                $"'{implementationType.FullName}' does not implement or inherit '{serviceType.FullName}'.",
                nameof(implementationType));

        ServiceType = serviceType;
        ImplementationType = implementationType;
        Lifetime = lifetime;
    }
}
