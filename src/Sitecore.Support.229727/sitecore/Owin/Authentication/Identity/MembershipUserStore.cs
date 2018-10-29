// © 2017 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Abstractions;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Owin.Authentication.Data;
using Sitecore.Owin.Authentication.Extensions;
using Sitecore.Owin.Authentication.Identity;
using Sitecore.Owin.Authentication.Infrastructure;
using Sitecore.Security.Accounts;
using static System.FormattableString;

namespace Sitecore.Support.Owin.Authentication.Identity
{
  public class MembershipUserStore<TUser> :
      IUserStore<TUser>,
      IUserLoginStore<TUser>,
      IUserSecurityStampStore<TUser>,
      IUserPasswordStore<TUser>,
      IUserClaimStore<TUser>,
      IUserLockoutStore<TUser, string>,
      IUserTwoFactorStore<TUser, string>,
      IQueryableUserStore<TUser, string>,
      IHttpContextEnsurable
      where TUser : ApplicationUser, new()
  {
    internal const string InMemoryVirtualUsersKey = "sc.InMemoryVirtualUsers";
    internal const string InMemoryClaimsKey = "identity.InMemoryClaims";

    private readonly object _lock = new object();

    [Obsolete("Use another constructor")]
    public MembershipUserStore([NotNull] HttpContextBase httpContext,
        [NotNull] BaseDomainManager domainManager,
        [NotNull] UserLoginsDataProvider<TUser> userLoginsDataProvider)
        : this(httpContext,
            domainManager,
            userLoginsDataProvider,
            ServiceLocator.ServiceProvider.GetService<BaseAuthenticationManager>(),
            ServiceLocator.ServiceProvider.GetService<IMembership>(),
            ServiceLocator.ServiceProvider.GetService<IVirtualUserLoginsDataProvider<TUser>>())
    {
    }

    [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login")]
    [Obsolete("Use another constructor")]
    public MembershipUserStore([NotNull] HttpContextBase httpContext,
        [NotNull] BaseDomainManager domainManager,
        [NotNull] UserLoginsDataProvider<TUser> userLoginsDataProvider,
        [NotNull] BaseAuthenticationManager baseAuthenticationManager,
        [NotNull] IMembership membershipWrapper,
        [NotNull] IVirtualUserLoginsDataProvider<TUser> virtualUserLoginsDataProvider)
        : this(httpContext, domainManager, userLoginsDataProvider, baseAuthenticationManager, membershipWrapper, virtualUserLoginsDataProvider, ServiceLocator.ServiceProvider.GetService<BaseLog>())
    {
    }

    [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login")]
    public MembershipUserStore([NotNull] HttpContextBase httpContext,
        [NotNull] BaseDomainManager domainManager,
        [NotNull] UserLoginsDataProvider<TUser> userLoginsDataProvider,
        [NotNull] BaseAuthenticationManager baseAuthenticationManager,
        [NotNull] IMembership membershipWrapper,
        [NotNull] IVirtualUserLoginsDataProvider<TUser> virtualUserLoginsDataProvider,
        [NotNull] BaseLog log)
    {
      Assert.ArgumentNotNull(httpContext, nameof(httpContext));
      Assert.ArgumentNotNull(domainManager, nameof(domainManager));
      Assert.ArgumentNotNull(userLoginsDataProvider, nameof(userLoginsDataProvider));
      Assert.ArgumentNotNull(baseAuthenticationManager, nameof(baseAuthenticationManager));
      Assert.ArgumentNotNull(membershipWrapper, nameof(membershipWrapper));
      Assert.ArgumentNotNull(virtualUserLoginsDataProvider, nameof(virtualUserLoginsDataProvider));

      this.HttpContext = httpContext;
      this.DomainManager = domainManager;
      this.AuthenticationManager = baseAuthenticationManager;
      this.UserLoginsDataProvider = userLoginsDataProvider;
      this.MembershipWrapper = membershipWrapper;
      this.VirtualUserLoginsDataProvider = virtualUserLoginsDataProvider;
      this.Log = log;
    }

    public HttpContextBase HttpContext { get; internal set; }

    [Obsolete("This property is no longer in use and will be removed in later release.")]
    public Collection<Claim> InMemoryTempClaims
    {
      get { return new Collection<Claim>(this.InMemoryClaims.ToList()); }
    }

    public IQueryable<TUser> Users
    {
      get
      {
        return this.DomainManager
            .GetDomains()
            .SelectMany(domain => domain.GetUsers())
            .Select(user => (TUser)user.ToApplicationUser())
            .AsQueryable();
      }
    }

    internal ConcurrentSet<Claim> InMemoryClaims
    {
      get
      {
        this.EnsureHttpContext();
        var inMemoryClaims = (ConcurrentSet<Claim>)this.HttpContext.Items[InMemoryClaimsKey];
        if (inMemoryClaims == null)
        {
          lock (_lock)
          {
            inMemoryClaims = (ConcurrentSet<Claim>)this.HttpContext.Items[InMemoryClaimsKey];
            if (inMemoryClaims == null)
            {
              inMemoryClaims = new ConcurrentSet<Claim>();
              this.HttpContext.Items[InMemoryClaimsKey] = inMemoryClaims;
            }
          }
        }

        return inMemoryClaims;
      }
    }

    internal ConcurrentSet<string> InMemoryVirtualUsers
    {
      get
      {
        this.EnsureHttpContext();
        var inMemoryVirtualUsers = (ConcurrentSet<string>)this.HttpContext.Items[InMemoryVirtualUsersKey];
        if (inMemoryVirtualUsers == null)
        {
          lock (_lock)
          {
            inMemoryVirtualUsers = (ConcurrentSet<string>)this.HttpContext.Items[InMemoryVirtualUsersKey];
            if (inMemoryVirtualUsers == null)
            {
              inMemoryVirtualUsers = new ConcurrentSet<string>();
              this.HttpContext.Items[InMemoryVirtualUsersKey] = inMemoryVirtualUsers;
            }
          }
        }

        return inMemoryVirtualUsers;
      }
    }

    [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login")]
    [NotNull]
    protected IVirtualUserLoginsDataProvider<TUser> VirtualUserLoginsDataProvider { get; }

    [NotNull]
    protected BaseLog Log { get; }

    [NotNull]
    protected BaseDomainManager DomainManager { get; }

    [NotNull]
    [Obsolete("Use UserLoginsDataProvider instead.")]
    protected UserStoreDataProvider<TUser> UserStoreDataProvider => (UserStoreDataProvider<TUser>)this.UserLoginsDataProvider;

    [NotNull]
    protected UserLoginsDataProvider<TUser> UserLoginsDataProvider { get; }

    [NotNull]
    protected BaseAuthenticationManager AuthenticationManager { get; }

    [NotNull]
    protected IMembership MembershipWrapper { get; }

    public virtual Task CreateAsync(TUser user)
    {
      User innerUser;
      if (!user.IsVirtual)
      {
        Guid userId;
        if (Guid.TryParse(user.Id, out userId))
        {
          MembershipCreateStatus status;
          this.MembershipWrapper.CreateUser(user.UserName, this.MembershipWrapper.GeneratePassword(32, 8), null, null, null, true, userId,
              out status);

          if (status != MembershipCreateStatus.Success)
          {
            throw new InvalidOperationException("Unable to create a user. Reason: " + status);
          }
        }
        else
        {
          throw new InvalidOperationException(
              "Unable to create a user. Reason: Cannot convert User Id to guid. User Id is \"" + user.Id + "\"");
        }

        innerUser = User.FromName(user.UserName, true);
      }
      else
      {
        this.EnsureHttpContext();
        innerUser = this.AuthenticationManager.BuildVirtualUser(user.UserName, true);
        innerUser.RuntimeSettings.Save();

        this.InMemoryVirtualUsers.Add(user.UserName);
      }
      user.GetType().GetProperty("InnerUser").SetValue(user, innerUser);
      return Task.CompletedTask;
    }

    public virtual Task UpdateAsync(TUser user)
    {
      if (user.InnerUser != null)
      {
        this.EnsureHttpContext();
        user.InnerUser.Profile.Save();
      }

      return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TUser user)
    {
      this.MembershipWrapper.DeleteUser(user.UserName);

      return Task.CompletedTask;
    }

    public virtual Task<TUser> FindByIdAsync(string userId)
    {
      if (string.IsNullOrEmpty(userId))
      {
        return Task.FromResult<TUser>(null);
      }

      Guid guid;
      MembershipUser membershipUser = null;
      if (Guid.TryParse(userId, out guid))
      {
        membershipUser = this.MembershipWrapper.GetUser(guid);
      }

      User user;
      if (membershipUser == null)
      {
        user = User.FromName(userId, true);

        if (!this.IsVirtualUser(user))
        {
          return Task.FromResult<TUser>(null);
        }

        user.RuntimeSettings.IsVirtual = true;
      }
      else
      {
        user = User.FromName(membershipUser.UserName, true);
      }

      ApplicationUser result = user.ToApplicationUser();

      return Task.FromResult((TUser)result);
    }

    public virtual Task<TUser> FindByNameAsync(string userName)
    {
      if (string.IsNullOrEmpty(userName))
      {
        return Task.FromResult<TUser>(null);
      }

      User user = User.FromName(userName, true);
      ID userId = user.GetId();

      if (!this.IsVirtualUser(user))
      {
        if (userId.IsNull)
        {
          return Task.FromResult<TUser>(null);
        }
      }
      else
      {
        user.RuntimeSettings.IsVirtual = true;
      }

      ApplicationUser result = user.ToApplicationUser();

      return Task.FromResult((TUser)result);
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    public virtual async Task AddLoginAsync([NotNull] TUser user, [NotNull] UserLoginInfo login)
    {
      Assert.ArgumentNotNull(user, nameof(user));
      Assert.ArgumentNotNull(login, nameof(login));

      if (!this.IsVirtualUser(user))
      {
        this.UserLoginsDataProvider.AddLogin(user, login);
      }
      else
      {
        await this.VirtualUserLoginsDataProvider.AddLoginAsync(user, login);
      }
    }

    public virtual async Task RemoveLoginAsync([NotNull] TUser user, [NotNull] UserLoginInfo login)
    {
      Assert.ArgumentNotNull(user, nameof(user));
      Assert.ArgumentNotNull(login, nameof(login));

      if (!this.IsVirtualUser(user))
      {
        this.UserLoginsDataProvider.RemoveLogin(user, login);
      }
      else
      {
        await this.VirtualUserLoginsDataProvider.RemoveLoginAsync(user, login);
      }
    }

    public virtual async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user)
    {
      IList<UserLoginInfo> userLoginInfos;

      if (!this.IsVirtualUser(user))
      {
        userLoginInfos = this.UserLoginsDataProvider.GetLogins(user);
      }
      else
      {
        userLoginInfos = await this.VirtualUserLoginsDataProvider.GetLoginsAsync(user);
      }

      return userLoginInfos;
    }

    public virtual async Task<TUser> FindAsync([NotNull] UserLoginInfo login)
    {
      Assert.ArgumentNotNull(login, nameof(login));

      string userId = this.UserLoginsDataProvider.Find(login);

      if (string.IsNullOrEmpty(userId))
      {
        userId = await this.VirtualUserLoginsDataProvider.FindAsync(login);
      }

      return await this.FindByIdAsync(userId);
    }

    public virtual Task SetSecurityStampAsync(TUser user, string stamp)
    {
      return Task.CompletedTask;
    }

    public virtual Task<string> GetSecurityStampAsync(TUser user)
    {
      return Task.FromResult(string.Empty);
    }

    public virtual Task SetPasswordHashAsync(TUser user, string passwordHash)
    {
      return Task.CompletedTask;
    }

    public virtual Task<string> GetPasswordHashAsync(TUser user)
    {
      return Task.FromResult(string.Empty);
    }

    public virtual Task<bool> HasPasswordAsync(TUser user)
    {
      return Task.FromResult(string.IsNullOrEmpty(this.MembershipWrapper.GetUser(user.UserName)?.GetPassword()));
    }

    public virtual Task<IList<Claim>> GetClaimsAsync(TUser user)
    {
      return Task.FromResult((IList<Claim>)this.InMemoryClaims.Concat(TemporaryClaimsStorage.TemporaryClaims).ToList());
    }

    public virtual Task AddClaimAsync(TUser user, Claim claim)
    {
      this.InMemoryClaims.Add(claim);
      return Task.CompletedTask;
    }

    public virtual Task RemoveClaimAsync(TUser user, Claim claim)
    {
      this.InMemoryClaims.Remove(claim);
      return Task.CompletedTask;
    }

    public virtual async Task<DateTimeOffset> GetLockoutEndDateAsync(TUser user)
    {
      if (await this.GetLockoutEnabledAsync(user))
      {
        return DateTimeOffset.MaxValue;
      }

      return DateTimeOffset.MinValue;
    }

    public virtual Task SetLockoutEndDateAsync(TUser user, DateTimeOffset lockoutEnd)
    {
      throw new NotImplementedException();
    }

    public virtual Task<int> IncrementAccessFailedCountAsync(TUser user)
    {
      return Task.FromResult(0);
    }

    public virtual Task ResetAccessFailedCountAsync(TUser user)
    {
      return Task.CompletedTask;
    }

    public virtual Task<int> GetAccessFailedCountAsync(TUser user)
    {
      return Task.FromResult(0);
    }

    public virtual Task<bool> GetLockoutEnabledAsync(TUser user)
    {
      return Task.FromResult(this.MembershipWrapper.GetUser(user.UserName)?.IsLockedOut ?? false);
    }

    [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Sitecore.Abstractions.BaseLog.Error(System.String,System.Exception,System.Object)")]
    [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
    public virtual Task SetLockoutEnabledAsync(TUser user, bool enabled)
    {
      MembershipUser membershipUser = Membership.GetUser(user.UserName);
      Assert.IsNotNull(user, typeof(MembershipUser));
      try
      {
        membershipUser?.UnlockUser();
      }
      catch (Exception ex)
      {
        this.Log.Error(Invariant($"User '{user.UserName}' can not be unlocked"), ex, this);
      }

      return Task.CompletedTask;
    }

    public virtual Task SetTwoFactorEnabledAsync(TUser user, bool enabled)
    {
      User scUser = user.InnerUser ?? User.FromName(user.UserName, true);
      scUser.Profile["TwoFactorEnabled"] = enabled.ToString();

      return Task.CompletedTask;
    }

    public virtual Task<bool> GetTwoFactorEnabledAsync(TUser user)
    {
      User scUser = user.InnerUser ?? User.FromName(user.UserName, true);
      bool result = MainUtil.StringToBool(scUser.Profile["TwoFactorEnabled"], false);
      return Task.FromResult(result);
    }

    internal bool IsVirtualUser(ApplicationUser user)
    {
      return this.IsVirtualUser(user.InnerUser);
    }

    internal virtual bool IsVirtualUser(User user)
    {
      Assert.ArgumentNotNull(user, nameof(user));

      this.EnsureHttpContext();

      return user.RuntimeSettings.IsVirtual || this.InMemoryVirtualUsers.Contains(user.Name);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
  }
}
