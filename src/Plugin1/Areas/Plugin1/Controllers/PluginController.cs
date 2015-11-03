using System;
using System.Web;
using System.Web.Mvc;

namespace Plugin1.Areas.Plugin1.Controllers
{
    public class PluginController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
