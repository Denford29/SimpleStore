using System;
using System.Globalization;
using System.Web.Mvc;
using SimplestStore.Models;
using Umbraco.Web.Mvc;

namespace SimplestStore.Controllers
{

    public class BasketSurfaceController : SurfaceController
    {
        
        //
        // GET: /basketSurface/
        [ChildActionOnly]
        public ActionResult Index()
        {
            return PartialView("addToBasket", CurrentPage);
        }

        [HttpPost]
        public ActionResult SiteWideAddToBasket(int productPageId , decimal productPrice)
            {

            if (!ModelState.IsValid)
                return CurrentUmbracoPage();

            //set a flag we gona update with the results of adding to the basket and create the var's we need in the logic
            var currentBasket = new basketItem();

            var productPage = Umbraco.TypedContent(productPageId);
            if (productPage != null)
                {
                    bool addedToCart;
                    if (!string.IsNullOrEmpty(productPrice.ToString(CultureInfo.InvariantCulture)))
                    {
                    var productPriceValue = productPrice;
                    addedToCart = currentBasket.GetCart().AddProduct(productPage, 1, productPriceValue);
                    }
                else
                    {
                    addedToCart = currentBasket.GetCart().AddProduct(productPage, 1, 1);
                    }

                // create some temp data to use to show if the product has been added successfully or not
                if (addedToCart)
                    {
                    TempData["success"] = "You have successfully added the product to the basket...";
                    }
                else
                    {
                    TempData["failure"] = "Opps, There was an error adding the product your basket...";
                    }

                return RedirectToCurrentUmbracoPage();
                }
            else
                {
                TempData["failure"] = "Opps, There was an error adding the product your basket...";
                return CurrentUmbracoPage();
                }
            }

        [HttpPost]
        public ActionResult ProductPageAddToBasket(decimal productPrice)
        {
            if (!ModelState.IsValid)
                return CurrentUmbracoPage();

            //set a flag we gona update with the results of adding to the basket and create the var's we need in the logic
            bool addedToCart;
            var currentBasket = new basketItem();

            if (!string.IsNullOrEmpty(productPrice.ToString(CultureInfo.InvariantCulture)))
            {
                var productPriceValue = productPrice;
                addedToCart = currentBasket.GetCart().AddProduct(CurrentPage, 1, productPriceValue);
            }
            else
            {
            addedToCart = currentBasket.GetCart().AddProduct(CurrentPage, 1, 1);
            }

            // create some temp data to use to show if the product has been added successfully or not
            if (addedToCart)
                {
                TempData["success"] = "You have successfully added the product to the basket...";
                }
            else
                {
                TempData["failure"] = "Opps, There was an error adding the product your basket...";
                }

            return RedirectToCurrentUmbracoPage();
        }

        [HttpPost]
        public ActionResult ExtraProductAddToBasket(int extraProductPageId, int mainProductPageId)
            {
            if (!ModelState.IsValid)
                return CurrentUmbracoPage();

            //set a flag we gona update with the results of adding to the basket and create the var's we need in the logic
            var addedToCart = false;
            var currentBasket = new basketItem();
            var extraMainPage = Umbraco.TypedContent(mainProductPageId);
            if (extraMainPage != null && extraMainPage.Id > 0 && extraMainPage.GetProperty("productPrice").HasValue)
                {
                var productPrice = extraMainPage.GetProperty("productPrice").Value.ToString();
                var extraProductPage = Umbraco.TypedContent(extraProductPageId);
                if (extraProductPage != null && !string.IsNullOrEmpty(productPrice))
                    {
                    var productPriceValue = Convert.ToDecimal(productPrice);
                    addedToCart = currentBasket.GetCart().AddExtraProduct(extraMainPage, productPriceValue, extraProductPage, 1);
                    }
                }
            else
                {
                TempData["failure"] = "Opps, There was an error adding the extra your basket...";
                }

            // create some temp data to use to show if the product has been added successfully or not
            if (addedToCart)
                {
                TempData["success"] = "You have successfully added the extra product to the basket...";
                }
            else
                {
                TempData["failure"] = "Opps, There was an error adding the extra your basket...";
                }

            return RedirectToCurrentUmbracoPage();
            }


        [HttpPost]
        public ActionResult RemoveFromBasket(int productPageId)
        {
            if (!ModelState.IsValid)
                return CurrentUmbracoPage();

            var currentBasket = new basketItem();
            var removedFromCart = false;

            var productPage = Umbraco.TypedContent(productPageId);
            if (productPage != null)
            {
            removedFromCart = currentBasket.GetCart().RemoveLine(productPage);
            }

            // create some temp data to use to show if the product has been added successfully or not
            if (removedFromCart)
                {
                TempData["success"] = "You have successfully removed the extra product to the basket...";
                }
            else
                {
                TempData["failure"] = "Opps, There was an error removing the extra your basket...";
                }

            return RedirectToCurrentUmbracoPage();
        }

        [HttpPost]
        public ActionResult EmptyBasket()
            {
            var currentBasket = new basketItem();
            currentBasket.GetCart().Clear();
            return RedirectToCurrentUmbracoPage();
            }
    }
}
