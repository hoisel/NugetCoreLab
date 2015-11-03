using System.Web.Mvc;

namespace Plugin1
{
    public class PluginAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Plugin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Plugin_default",
                "Plugin/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );

            // To override package id based on convention, change it here...
            //GlobalFilters.Filters.Add(new PackageIdFilter("Package.Lab"));
        }
    }
}
