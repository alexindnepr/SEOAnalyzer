using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TestSEOAnalyser.Models;

namespace TestSEOAnalyser.Controllers
{
    public class HomeController : Controller
    {
        //Home/Index
        public ActionResult Index()
        {
            return View();
        }

        //Home/GetResult
        public ActionResult GetResult(SeoAnalyserModel model)
        {
            if(model == null)
                return PartialView("Error"); 

            var result = new SeoAnalyserResultModel(model);
            return PartialView("Result", result);
        }

        //Home/Error
        public ActionResult Error()
        {
            return View();
        }

    }
}