using System;

namespace Sitecore.Support.sitecore.login
{
    public partial class Default : Sitecore.sitecore.login.Default
    {
        protected new void LoginClicked(object sender, EventArgs e)
        {
            try
            {
                base.LoginClicked(sender, e);
            }
            catch (Exception ex)
            {
                if (ex.InnerException.Message == "The method or operation is not implemented.")
                {
                    Log.Warn("Sitecore Support patch #229727 - Exception while login. The user could be locked. Please check this via User Manager application", this);
                    Log.Warn(ex.Message, this);
                    Log.Warn(ex.InnerException.Message, this);
                    Log.Warn(ex.InnerException.StackTrace, this);
                    Log.Warn(ex.StackTrace, this);
                    this.RenderError("Your login attempt was not successful. You account could be locked. Please contact your Sitecore administrator.");
                }
            }
        }


        private void RenderError(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            string text2 = this.Translate.Text(text);
            this.FailureHolder.Visible = true;
            this.FailureText.Text = text2;
        }
    }
}
