using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using SimplestStore.Models;
using Umbraco.Core.Models;
using Umbraco.Web.Mvc;
using Umbraco.Core;
using Umbraco.Core.Logging;
using umbraco.BusinessLogic;

namespace SimplestStore.Controllers
    {
    public class checkoutSurfaceController : SurfaceController
        {
        //
        // GET: /checkoutSurface/
        [Authorize]
        public ActionResult Index()
            {
            return View();
            }

        [HttpPost]
        public ActionResult ProcessPayment()
            {
            if (!ModelState.IsValid)
                return CurrentUmbracoPage();

            try
                {
                // get the current users basket
                Cart basketCart = (Cart)Session["Cart"];
                if (basketCart != null)
                    {
                    //check if the basket is noe empty and add you logic to process the payment here

                    TempData["checkoutSuccess"] = true;
                    //after processing empty the basket and redirect to the thank you page
                    basketCart.Clear();
                    return Redirect("/");
                    }
                else
                    {
                    return CurrentUmbracoPage();
                    }
                }
            catch (Exception ex)
                {
                var errorMessage = "Checkout Process Error<br /><br />" + ex.Message + "<br /><br />" + ex.InnerException + "<br /><br />" + ex.StackTrace;
                Log.Add(LogTypes.Error, 999, "Checkout Process Error. - \r\n \r\n" + errorMessage);
                LogHelper.Error(this.GetType(), "Checkout Process Error", ex);

                TempData["failure"] = true;
                return CurrentUmbracoPage();
                }
            }
        }
    }
