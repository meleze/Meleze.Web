using System.Web.Mvc;

namespace Meleze.Web.Mvc
{
    /// <summary>
    /// Allows only requests made from the local computer.
    /// </summary>
    public class LocalRequestFilterAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.Request.IsLocal)
            {
                filterContext.Result = new HttpUnauthorizedResult();
            }
        }
    }
}
