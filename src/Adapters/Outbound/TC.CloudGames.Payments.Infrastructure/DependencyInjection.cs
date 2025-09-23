namespace TC.CloudGames.Payments.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddTransient<IDateTimeProvider, DateTimeProvider>();
            services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
            services.AddSingleton<ITokenProvider, TokenProvider>();
            services.AddScoped<IUserContext, UserContext>();

            return services;
        }
    }
}
