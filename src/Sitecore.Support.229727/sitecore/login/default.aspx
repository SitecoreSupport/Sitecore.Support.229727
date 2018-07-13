<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Sitecore.Support.sitecore.login.Default" %>

<%@ Import Namespace="Sitecore.Configuration" %>
<%@ Import Namespace="Sitecore.SecurityModel.License" %>
<%@ Import Namespace="Sitecore.Resources" %>
<%@ Import Namespace="Sitecore.Web.UI" %>
<%@ Import Namespace="Sitecore" %> 

<!DOCTYPE html>

<html>
<head runat="server">
    <title>Welcome to Sitecore</title>
    <link rel="shortcut icon" href="/sitecore/images/favicon.ico" />
    <meta name="robots" content="noindex, nofollow" />
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <sc:PlatformFontStylesLink runat="server"/>
    <script type="text/javascript">
        if (window != top) {
            var urlParams = encodeURIComponent(top.location.pathname + top.location.search + top.location.hash);
            if (urlParams) {
                top.location.href = '<%#GetLoginPageUrl()%>' + '?returnUrl=' + (top.location.pathname[0] == '/' ? '' : '/') + urlParams;
          }
      }
  </script>

  <!-- Bootstrap for testing -->
  <link href="Login.css" rel="stylesheet" />

  <style>

      .login-outer {
        background: url('<%#GetBackgroundImageUrl() %>') no-repeat center center fixed;
		    background-size:cover;
      }

     
    </style>
</head>
<body class="sc">
  <div class="login-outer">
    <div class="login-main-wrap">
      <div class="login-box">
        <div class="logo-wrap">
          <img src="login/logo_new.png" alt="Sitecore logo" />
        </div>

        <form id="LoginForm" runat="server" class="form-signin" role="form">

          <div id="login">

                        <div class="scLoginFailedMessagesContainer">
                            <div id="credentialsError" class="scMessageBar scWarning" style="display: none">
                                <i class="scMessageBarIcon"></i>
                                <div class="scMessageBarTextContainer">
                                    <div class="scMessageBarText">
                                        <asp:Literal  ID="credentialsAreNotInput" runat="server" Text="<%#Translate.Text(Texts.Please_Enter_Your_Login_Credentials)%>"/>
                                    </div>
                                </div>
                            </div>
                            <asp:PlaceHolder runat="server" ID="FailureHolder" Visible="False">
                                <div id="loginFailedMessage" class="scMessageBar scWarning">
                                    <i class="scMessageBarIcon"></i>
                                    <div class="scMessageBarTextContainer">
                                        <div class="scMessageBarText">
                                            <asp:Literal ID="FailureText" runat="server"/>
                                        </div>
                                    </div>
                                </div>
                            </asp:PlaceHolder>
                        </div>

            <asp:PlaceHolder runat="server" ID="SuccessHolder" Visible="False">
              <div class="sc-messageBar">
                <div class="sc-messageBar-head alert alert-info">
                  <i class="alert-ico"></i>
                  <span class="sc-messageBar-messageText">
                      <asp:Literal runat="server" ID="SuccessText" />
                  </span>
                </div>
              </div>
            </asp:PlaceHolder>

                        <div class="form-wrap">
                            <asp:Label runat="server" ID="loginLbl" CssClass="login-label" Text="<%#Translate.Text(Texts.UserName)%>"/>
                            <asp:TextBox ID="UserName" CssClass="form-control" placeholder="<%#Translate.Text(Texts.ENTER_USER_NAME)%>" autofocus runat="server" ValidationGroup="Login" />
                            <asp:RequiredFieldValidator ID="UserNameRequired" runat="server" ControlToValidate="UserName" ValidationGroup="Login" />
                            <asp:Label runat="server" ID="passLabel" CssClass="login-label" Text="<%#Translate.Text(Texts.Password)%>"/>
                            <asp:TextBox ID="Password" CssClass="form-control" placeholder="<%#Translate.Text(Texts.ENTER_PASSWORD)%>" runat="server" TextMode="Password" ValidationGroup="Login" />
                            <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="Password" ValidationGroup="Login" />
                        </div>

            <asp:Button runat="server" ValidationGroup="Login" UseSubmitBehavior="True" CssClass="btn btn-primary btn-block" OnClick="LoginClicked" Text="<%#Translate.Text(Texts.LOG_IN)%>" />

            <div class="remember-me-wrap">
              <asp:PlaceHolder ID="PlaceHolder3" runat="server" Visible="<%# !Settings.Login.DisableLicenseInfo %>">
                <div class="license-info-link-wrap">
                    <a href="javascript:;" id="licenseOptionsLink" class="login-link"><%#Translate.Text(Texts.LicenseOptions)%></a>
                </div>
              </asp:PlaceHolder>
            
              <asp:PlaceHolder ID="PlaceHolder2" runat="server" Visible="<%# !Settings.Login.DisableRememberMe %>">
                <div class="remember-me-lnk">
                  <label class="checkbox login-label">
                    <asp:CheckBox runat="server" ID="RememberMe" />
                      <%#Translate.Text(Texts.RememberMe)%>
                  </label>
                </div>
              </asp:PlaceHolder>
                  
              <asp:PlaceHolder ID="PlaceHolder1" runat="server" Visible="<%# !Settings.Login.DisablePasswordRecovery %>">
                <div class="forgot-pass-link-wrap">
                  <asp:PlaceHolder ID="PlaceHolder4" runat="server" Visible="<%# !Settings.Login.DisableRememberMe %>">
                    <span class="forgot-pass-separator"></span>
                  </asp:PlaceHolder>
                  <a href="#" class="show-recovery"><%#Translate.Text(Texts.FORGOT_YOUR_PASSWORD)%></a>
                  <a href="#" class="show-recovery" runat="server" visible="False" ID="forgotPasswordLinkMessage"><%#Translate.Text(Texts.FORGOT_YOUR_PASSWORD)%></a>
                </div>
              </asp:PlaceHolder>
            </div>

            <hr ID="ExternalSignInsSeparator" class="sc-separator" runat="server" />

            <div class="external-signin">
              <asp:Repeater ID="ExternalSignIn" runat="server">
                  <ItemTemplate>
                      <asp:LinkButton CssClass='<%# "btn btn-default btn-block " + Eval("IdentityProvider") %>' PostBackUrl='<%# Eval("Href") %>'
                          runat="server">
                          <div class="sc-icon" style='<%# "background-image: url(" + Images.GetThemedImageSource((string)Eval("Icon"), ImageDimension.id24x24) + ")" %>'
                              runat="server"/>
                          <span class="external-signin-caption">
                              <asp:Literal Text='<%# Eval("Caption") %>'  runat="server"></asp:Literal>
                          </span>
                      </asp:LinkButton>
                  </ItemTemplate>
              </asp:Repeater>
            </div>
          </div>

          <div id="passwordRecovery" style="display: none">
            <h2 class="form-signin-heading"><%#Translate.Text(Texts.FORGOT_YOUR_PASSWORD)%></h2>
            <asp:PlaceHolder runat="server" Visible="<%# string.IsNullOrEmpty(Settings.MailServer) %>">
              <div class="sc-messageBar">
              <div class="sc-messageBar-head alert">
                  <i class="alert-ico"></i>
                  <span class="sc-messageBar-messageText">
                    <asp:Literal runat="server" Text="<%#Translate.Text(Texts.MAIL_SERVER_NOT_CONFIGURED_NO_PASSWORD_RECOVERY)%>"></asp:Literal>
                  </span>
                </div>
               </div>
            </asp:PlaceHolder>
            <asp:PlaceHolder runat="server" Visible="<%# !string.IsNullOrEmpty(Settings.MailServer) %>">
              <asp:TextBox disabled ID="UserNameForgot" ValidationGroup="Recovery" CssClass="form-control" placeholder="User name" required runat="server" />
              <asp:Button runat="server" ValidationGroup="Recovery" UseSubmitBehavior="True" CssClass="btn btn-lg btn-primary btn-block" OnClick="ForgotPasswordClicked" Text="Send" />
            </asp:PlaceHolder>
            <div class="forgot-pass-wrap">
              <a class="hide-recovery login-link" href="javascript:;"><%#Translate.Text(Texts.Back)%></a>
            </div>
          </div>
          
          <div id="licenseOptions" visible="<%# !Settings.Login.DisableLicenseInfo%>" style="display: none;">
<%--            <h2 class="form-signin-heading">License and browser information</h2>--%>
            <div class="license-info-wrap" runat="server">
              <ul>
                  <li><%#Translate.Text(Texts.SYSTEM_INFORMATION)%></li>
                  <li><%#Translate.Text(Texts.LicenseHolder)%> <%# License.Licensee%></li>
                  <li><%#Translate.Text(Texts.LicenseID)%> <%# License.LicenseID%></li>
                  <li><%#Translate.Text(Texts.SITECORE_VERSION)%> <%# About.VersionInformation()%></li>
              </ul>
              
              <iframe id="StartPage" runat="server" allowtransparency="true" frameborder="0" scrolling="auto"
                    marginheight="0" marginwidth="0" style="display: none; height: 105px;"></iframe>

            </div>
            <div class="login-link-wrap">
              <a href="javascript:;" id="licenseOptionsBack" class="login-link"><%#Translate.Text(Texts.Back)%></a>  
            </div>

          </div>
        </form>
      </div>
    </div>
  </div>
  <script type="text/javascript" src="/sitecore/login/login.js"></script>
</body>
</html>
