namespace Sitecore.Support.Owin.Authentication.Services
{
  using Microsoft.AspNet.Identity;
  using Microsoft.Extensions.DependencyInjection;
  using Sitecore.DependencyInjection;
  using System.Diagnostics.CodeAnalysis;
  using Sitecore.Owin.Authentication.Identity;

  public class ServicesConfigurator : IServicesConfigurator
  {
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    public void Configure(IServiceCollection serviceCollection)
    {
      serviceCollection.AddScoped<IUserStore<ApplicationUser>, Sitecore.Support.Owin.Authentication.Identity.MembershipUserStore<ApplicationUser>>();
    }
  }
}
