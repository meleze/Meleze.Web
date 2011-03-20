using System;
using System.Reflection;
using System.Web.Mvc;

namespace Meleze.Web.Mvc
{
    /// <summary>
    /// Assign a controller action to a specific for button.
    /// The form posts to a single action and can have several buttons (with different names)
    /// that points to a controller method per button.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class AcceptButtonAttribute : ActionMethodSelectorAttribute
    {
        private string name;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The button's name</param>
        public AcceptButtonAttribute(string name)
            : base()
        {
            this.name = name;
        }

        public override bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo)
        {
            // When a form is submitted, the activated button's name is part of the form request
            return controllerContext.HttpContext.Request.Form[name] != null;
        }
    }
}
