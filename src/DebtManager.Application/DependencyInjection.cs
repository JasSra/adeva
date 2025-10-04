using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;
using System.Reflection;

namespace DebtManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assemblies = new[] { Assembly.GetExecutingAssembly() };
        services.AddMediatR(assemblies);
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddAutoMapper(assemblies);
        
        return services;
    }
}
