using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace TickAggregator.Worker.Extensions;

public static class OptionExtensions
{
    public static IServiceCollection AddValidatedOptions<T>(this IServiceCollection services, string section)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);

        services
            .AddOptions<T>()
            .BindConfiguration(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static T? GetValidatedOptions<T>(this IConfiguration configuration, string section) where T : class
    {
        var value = configuration.GetSection(section).Get<T>();
        if (value is null) return null;

        if (value is IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                if (item is null) continue;
                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(item, new ValidationContext(item), results, validateAllProperties: true))
                    throw new InvalidOperationException(string.Join("; ", results.Select(r => r.ErrorMessage)));
            }
        }
        else
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true))
                throw new InvalidOperationException(string.Join("; ", results.Select(r => r.ErrorMessage)));
        }

        return value;
    }
}
