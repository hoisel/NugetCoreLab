
using NuGet;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PkgLab.Areas.Installation.Controllers
{
    public class UpdatesController : Controller
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="packageId"></param>
        /// <returns></returns>
        public ActionResult Check(string packageId)
        {
            var projectManager = GetProjectManager();
            var installed = GetInstalledPackage(projectManager, packageId);
            var update = projectManager.GetUpdate(installed);

            var installationState = new InstallationState
            {
                Installed = installed,
                Update = update
            };

            if (Request.IsAjaxRequest())
            {
                var result = new
                {
                    Version = (update != null ? update.Version.ToString() : null),
                    UpdateAvailable = (update != null)
                };
                return Json(result, JsonRequestBehavior.AllowGet);
            }

            return View(installationState);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="packageId"></param>
        /// <returns></returns>
        public ActionResult Upgrade(string packageId)
        {
            var projectManager = GetProjectManager();
            var installed = GetInstalledPackage(projectManager, packageId);
            var update = projectManager.GetUpdate(installed);

            projectManager.UpdatePackage(update);

            if (Request.IsAjaxRequest())
            {
                return Json(new { Success = true, Version = update.Version.ToString() }, JsonRequestBehavior.AllowGet);
            }

            return View(update);

        }


        public ActionResult Install(string packageId)
        {
#if debug
            packageId == "Plugin";
#endif
            var projectManager = GetProjectManager();

            var packageToInstall = projectManager.GetRemotePackages(packageId).FirstOrDefault(p => p.Id == packageId);
            projectManager.InstallPackage(packageToInstall);

            if (Request.IsAjaxRequest())
            {
                return Json(new { Success = true, Version = packageToInstall.Version.ToString() }, JsonRequestBehavior.AllowGet);
            }

            return View(packageToInstall);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private WebProjectManager GetProjectManager()
        {
            string packageSource = System.Configuration.ConfigurationManager.AppSettings["PackageSource"] ?? "https://www.nuget.org/api/v2/";// @"D:\dev\hg\AutoUpdateDemo\test-package-source";
            string siteRoot = Request.MapPath("~/");

            return new WebProjectManager(packageSource, siteRoot);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectManager"></param>
        /// <param name="packageId"></param>
        /// <returns></returns>
        private IPackage GetInstalledPackage(WebProjectManager projectManager, string packageId)
        {
            var installed = projectManager.GetInstalledPackages(packageId).Where(p => p.Id == packageId);

            var installedPackages = installed.ToList();
            var package = installedPackages.FirstOrDefault();
            if (package == null)
            {
                throw new InvalidOperationException(String.Format("The package for package ID '{0}' is not installed in this website. Copy the package into the App_Data/packages folder.", packageId));
            }
            return package;
        }
    }
}
