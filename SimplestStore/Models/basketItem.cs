using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web;

namespace SimplestStore.Models
    {

    public class basketItem
        {
        public IPublishedContent ProductPage { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public bool IsOptionalExtra { get; set; }
        public bool IsMainProduct { get; set; }
        public string Description { get; set; }
        public IPublishedContent ExtraMainProductPage { get; set; }
        public Cart GetCart()
            {
            Cart cart = (Cart)HttpContext.Current.Session["Cart"];
            if (cart == null)
                {
                cart = new Cart();
                HttpContext.Current.Session["Cart"] = cart;
                }
            return cart;
            }
        }

    public class basketCartItems
        {
        public Cart ViewCart { get; set; }
        }

    public class Cart
        {
        private List<basketItem> lineCollection = new List<basketItem>();

        public bool AddProduct(IPublishedContent productPage, int quantity, decimal price)
            {

            basketItem line = lineCollection.FirstOrDefault(product => product.ProductPage.Id == productPage.Id);
            var exsistingProduct = false;

            if (line == null)
                {
                lineCollection.Add(new basketItem
                {
                    ProductPage = productPage,
                    Quantity = quantity,
                    Price = price,
                    IsOptionalExtra = false,
                    IsMainProduct = true,
                    Description = "Quantity : " + quantity + " @ : £ " + price.ToString("c"),
                    ExtraMainProductPage = null
                });

                // for main products check and add an non optional extras set on the product
                CheckAndAddExtras(productPage, quantity, price, exsistingProduct);
                return true;
                }
            else if (line.Quantity > 0)
                {
                line.Quantity += quantity;
                line.Description = "Quantity : " + line.Quantity + " @ : £ " + price.ToString("c");

                exsistingProduct = true;
                // for main products check and add an non optional extras set on the product
                CheckAndAddExtras(productPage, quantity, price, exsistingProduct);
                return true;
                }
            else
                {
                return false;
                }

            }

        public bool AddExtraProduct(IPublishedContent mainProductPage, decimal mainProductPrice, IPublishedContent extraProductPage, int extraQuantity)
            {
            //var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
            // check if the main product for this extra is already in the basket and set a falg to use to work out if the extra needs to be a new line or not
            basketItem mainProductLine = lineCollection.FirstOrDefault(product => product.ProductPage.Id == mainProductPage.Id);
            bool exsistingProduct = mainProductLine != null;

            // check if this extra is already in the basket either on the same product or another product
            basketItem pageExtraLine = lineCollection.FirstOrDefault(extraProduct => extraProduct.ProductPage.Id == extraProductPage.Id && extraProduct.ExtraMainProductPage.Id == mainProductPage.Id);

            // check if there is a price set on the extra and if its already added to the basket
            if (extraProductPage.GetProperty("price").HasValue)
                {
                var extraPrice = Convert.ToDecimal(extraProductPage.GetPropertyValue("price").ToString());
                if (pageExtraLine == null)
                    {
                    AddPricedExtra(mainProductPage, extraProductPage, extraPrice, extraQuantity);
                    return true;
                    }
                else
                    {
                    if (exsistingProduct)
                        {
                        pageExtraLine.Quantity += extraQuantity;
                        pageExtraLine.Description = "Quantity : " + pageExtraLine.Quantity + " @ : £ " + extraPrice.ToString("c");
                        return true;
                        }
                    else
                        {
                        AddPricedExtra(mainProductPage, extraProductPage, extraPrice, extraQuantity);
                        return true;
                        }
                    }
                }
            // check if it has a percentage instead of a set price and if its already added to the basket
            else if (extraProductPage.GetProperty("pricePercentage").HasValue)
                {
                //var mainProductPrice = Convert.ToDecimal(mainProductPage.GetPropertyValue("productPrice").ToString());
                var extraPercentage = extraProductPage.GetPropertyValue("pricePercentage").ToString();
                if (pageExtraLine == null)
                    {
                    AddPercentageExtra(mainProductPage, extraProductPage, mainProductPrice, extraPercentage, extraQuantity);
                    return true;
                    }
                else
                    {
                    if (exsistingProduct)
                        {
                        pageExtraLine.Quantity += extraQuantity;
                        pageExtraLine.Description = mainProductPage.Name + " , " + extraProductPage.Name + "(Quantity : " + pageExtraLine.Quantity + " ), @ : " + extraPercentage + "% of £" + mainProductPrice.ToString("c");
                        return true;
                        }
                    else
                        {
                        AddPercentageExtra(mainProductPage, extraProductPage, mainProductPrice, extraPercentage, extraQuantity);
                        return true;
                        }
                    }
                }
            else
                {
                return false;
                }
            }
        public bool RemoveLine(IPublishedContent productToRemove)
            {
            if (productToRemove != null)
                {
                var productLine = lineCollection.FirstOrDefault(product => product.ProductPage.Id == productToRemove.Id);
                if (productLine != null && productLine.IsMainProduct)
                    {
                    lineCollection.RemoveAll(product => product.ProductPage.Id == productToRemove.Id);
                    var productLineExtras = lineCollection.Where(line => line.ExtraMainProductPage != null && line.ExtraMainProductPage.Id == productToRemove.Id).ToList();
                    if (productLineExtras.Any())
                        {
                        foreach (var extra in productLineExtras)
                            {
                            lineCollection.RemoveAll(extraLine => extraLine.ProductPage.Id == extra.ProductPage.Id);
                            }
                        }
                    return true;
                    }
                else
                    {
                    lineCollection.RemoveAll(product => product.ProductPage.Id == productToRemove.Id);
                    return true;
                    }
                }
            else
                {
                return false;
                }
            return false;
            }
        public decimal ComputeTotalValue()
            {
            return lineCollection.Sum(e => e.Price * e.Quantity);
            }
        public void Clear()
            {
            lineCollection.Clear();
            }
        public IEnumerable<basketItem> Lines
            {
            get { return lineCollection; }
            }

        //checked and add product extras
        public void CheckAndAddExtras(IPublishedContent productPage, int quantity, decimal price, bool exsistingProduct)
            {
            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

            //START - basket extras logic
            //get the extras set on the page to add to the database
            var productExtras = productPage.GetProperty("productExtras").Value.ToString();
            if (!string.IsNullOrEmpty(productExtras))
                {
                // check if there is more than one extra set on the page and loop through all of them
                if (productExtras.Contains(","))
                    {
                    var extras = productExtras.Split(',');
                    foreach (var extraId in extras)
                        {
                        var extraPageId = Convert.ToInt32(extraId);
                        IPublishedContent priceExtraPage = umbracoHelper.TypedContent(extraPageId);
                        if (priceExtraPage != null && priceExtraPage.GetProperty("isOptionalExtra").HasValue)
                            {
                            var isOptional = priceExtraPage.GetPropertyValue("isOptionalExtra").ToString();
                            if (isOptional == "False")
                                {
                                basketItem extraLine = lineCollection.Where(extraProduct => extraProduct.ProductPage.Id == priceExtraPage.Id).FirstOrDefault();
                                // check if there is a price set on the extra and if its already added to the basket
                                if (priceExtraPage.GetProperty("price").HasValue)
                                    {
                                    var extraPrice = Convert.ToDecimal(priceExtraPage.GetPropertyValue("price").ToString());
                                    if (extraLine == null)
                                        {
                                        AddPricedExtra(productPage, priceExtraPage, extraPrice, quantity);
                                        }
                                    else
                                        {
                                        //if this extra is already in the basket and the product has alrady been added just increase the quantity otherwise add it as a new line
                                        if (exsistingProduct)
                                            {
                                            extraLine.Quantity += quantity;
                                            extraLine.Description = "Quantity : " + extraLine.Quantity + " @ : £ " + extraPrice.ToString("c");
                                            }
                                        else
                                            {
                                            AddPricedExtra(productPage, priceExtraPage, extraPrice, quantity);
                                            }
                                        }
                                    }
                                // check if it has a percentage instead of a set price and if its already added to the basket
                                else if (priceExtraPage.GetProperty("pricePercentage").HasValue)
                                    {
                                    var extraPercentage = priceExtraPage.GetPropertyValue("pricePercentage").ToString();
                                    if (extraLine == null)
                                        {
                                        AddPercentageExtra(productPage, priceExtraPage, price, extraPercentage, quantity);
                                        }
                                    else
                                        {
                                        //if this extra is already in the basket and the product has alrady been added just increase the quantity otherwise add it as a new line
                                        if (exsistingProduct)
                                            {
                                            extraLine.Quantity += quantity;
                                            extraLine.Description = productPage.Name + " , " + priceExtraPage.Name + "(Quantity : " + extraLine.Quantity + " ), @ : " + extraPercentage + "% of £" + price.ToString("c");
                                            }
                                        else
                                            {
                                            AddPercentageExtra(productPage, priceExtraPage, price, extraPercentage, quantity);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                // if there is only one set then do the logic above for one item instead
                else
                    {
                    IPublishedContent priceExtraPage = umbracoHelper.TypedContent(productExtras);
                    if (priceExtraPage != null && priceExtraPage.GetProperty("isOptionalExtra").HasValue)
                        {
                        var isOptional = priceExtraPage.GetPropertyValue("isOptionalExtra").ToString();
                        if (isOptional == "False")
                            {
                            basketItem extraLine = lineCollection.Where(extraProduct => extraProduct.ProductPage.Id == priceExtraPage.Id).FirstOrDefault();
                            // check if there is a price set on the extra and if its already added to the basket
                            if (priceExtraPage.GetProperty("price").HasValue)
                                {
                                var extraPrice = Convert.ToDecimal(priceExtraPage.GetPropertyValue("price").ToString());
                                if (extraLine == null)
                                    {
                                    AddPricedExtra(productPage, priceExtraPage, extraPrice, quantity);
                                    }
                                else
                                    {
                                    //if this extra is already in the basket and the product has alrady been added just increase the quantity otherwise add it as a new line
                                    if (exsistingProduct)
                                        {
                                        extraLine.Quantity += quantity;
                                        extraLine.Description = "Quantity : " + extraLine.Quantity + " @ : £ " + extraPrice.ToString("c");
                                        }
                                    else
                                        {
                                        AddPricedExtra(productPage, priceExtraPage, extraPrice, quantity);
                                        }
                                    }
                                }
                            // check if it has a percentage instead of a set price and if its already added to the basket
                            else if (priceExtraPage.GetProperty("pricePercentage").HasValue)
                                {
                                var extraPercentage = priceExtraPage.GetPropertyValue("pricePercentage").ToString();
                                if (extraLine == null)
                                    {
                                    AddPercentageExtra(productPage, priceExtraPage, price, extraPercentage, quantity);
                                    }
                                else
                                    {
                                    //if this extra is already in the basket and the product has alrady been added just increase the quantity otherwise add it as a new line
                                    if (exsistingProduct)
                                        {
                                        extraLine.Quantity += quantity;
                                        extraLine.Description = productPage.Name + " , " + priceExtraPage.Name + "(Quantity : " + extraLine.Quantity + " ), @ : " + extraPercentage + "% of £" + price.ToString("c");
                                        }
                                    else
                                        {
                                        AddPercentageExtra(productPage, priceExtraPage, price, extraPercentage, quantity);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            //END - basket extras logic
            }

        public void AddPricedExtra(IPublishedContent mainProduct, IPublishedContent extraProduct, decimal extraPrice, int extraQuantinty)
        {

        decimal extraDiscountPrice = extraPrice;
        var extraDescription = mainProduct.Name + " , " + extraProduct.Name + " for : £ " + extraPrice.ToString("c");
            if (extraProduct.HasProperty("priceExtras") && extraProduct.HasValue("priceExtras"))
            {
            var priceExtrasIds = extraProduct.GetPropertyValue("priceExtras").ToString();
            extraDescription = DiscountMessage(extraProduct, priceExtrasIds, mainProduct, out extraDiscountPrice);
            }

            lineCollection.Add(new basketItem
            {
                ProductPage = extraProduct,
                Price = extraDiscountPrice,
                Quantity = extraQuantinty,
                IsOptionalExtra = false,
                IsMainProduct = false,
                Description = extraDescription,
                ExtraMainProductPage = mainProduct
            });
        }

        public void AddPercentageExtra(IPublishedContent mainProduct, IPublishedContent extraProduct, decimal mainProductPrice, string extraPercentage, int extraQuantinty)
            {
            var extraDiscount = Convert.ToDecimal(extraPercentage);
            var percentageTotalPrice = mainProductPrice * (extraDiscount / 100);

            decimal extraDiscountPrice = percentageTotalPrice;
            var extraDescription = mainProduct.Name + " , " + extraProduct.Name + " @ : " + extraPercentage + "% of £" + mainProductPrice.ToString("c");
            if (extraProduct.HasProperty("priceExtras") && extraProduct.HasValue("priceExtras"))
                {
                var priceExtrasIds = extraProduct.GetPropertyValue("priceExtras").ToString();
                extraDescription = DiscountMessage(extraProduct, priceExtrasIds, mainProduct, out extraDiscountPrice);
                }

            lineCollection.Add(new basketItem
            {
                ProductPage = extraProduct,
                Price = extraDiscountPrice,
                Quantity = extraQuantinty,
                IsOptionalExtra = false,
                IsMainProduct = false,
                Description = extraDescription,
                ExtraMainProductPage = mainProduct
            });
            }

        //checked and add price extras
        public string DiscountMessage(IPublishedContent itemPage, string priceExtrasIds ,  IPublishedContent  mainProductPage ,out decimal discountPrice)
            {
            discountPrice = 0;
            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
            var priceMessage = "";
            var pricePageIdsList = priceExtrasIds.Split(',');
            // discount logic for main products
            if (itemPage.HasProperty("productPrice") && itemPage.HasValue("productPrice"))
                {
                var itemPrice = Convert.ToDecimal(itemPage.GetPropertyValue("productPrice").ToString());
                priceMessage = "Original Price : " + itemPrice.ToString("c");
                if (pricePageIdsList.Any())
                    {
                    foreach (var pricePageId in pricePageIdsList)
                        {
                        var pricePage = umbracoHelper.TypedContent(pricePageId);
                        if (pricePage != null && (pricePage.HasProperty("priceValue") || pricePage.HasProperty("priceValuePercentage")))
                            {
                            var isDisount = pricePage.GetPropertyValue<bool>("isDiscount");
                            if (pricePage.HasValue("priceValue"))
                                {
                                var pricePagePrice = Convert.ToDecimal(pricePage.GetPropertyValue("priceValue").ToString());
                                if (isDisount)
                                    {
                                    discountPrice = itemPrice - pricePagePrice;
                                    priceMessage += ", less " + pricePage.Name
                                                            + " @ price : " + pricePagePrice.ToString("c")
                                                            + " = new price : " + discountPrice.ToString("c");
                                    }
                                else
                                    {
                                    discountPrice = itemPrice + pricePagePrice;
                                    priceMessage += ", add " + pricePage.Name
                                                            + " @ price : " + pricePagePrice.ToString("c")
                                                            + " = new price:" + discountPrice.ToString("c");
                                    }
                                }
                            else if (pricePage.HasValue("priceValuePercentage"))
                            {
                                var percentageValue = pricePage.GetPropertyValue("priceValuePercentage").ToString();
                                var pricePercent = Convert.ToDecimal(percentageValue);
                                var pricePagePercentPrice = itemPrice * (pricePercent / 100);
                                if (isDisount)
                                    {
                                    discountPrice = itemPrice - pricePagePercentPrice;
                                    priceMessage += ", less " + pricePage.Name
                                                            + " @ "+ percentageValue + "% of original price : " + pricePagePercentPrice.ToString("c")
                                                            + " = new price: " + discountPrice.ToString("c");
                                    }
                                else
                                    {
                                    discountPrice = itemPrice + pricePagePercentPrice;
                                    priceMessage += ", add " + pricePage.Name
                                                            + " @ " + percentageValue + "% of original price : " + pricePagePercentPrice.ToString("c")
                                                            + " = new price : " + discountPrice.ToString("c");
                                    }
                                }
                            }
                        }
                    }
                }
            // discount logic for main basket extras
            else if (mainProductPage != null && mainProductPage.Id > 0 && mainProductPage.HasProperty("productPrice") && mainProductPage.HasValue("productPrice"))
                {
                var itemMainPrice = Convert.ToDecimal(mainProductPage.GetPropertyValue("productPrice").ToString());

                    decimal itemPrice = 0;
                    if (itemPage.HasProperty("price") && itemPage.HasValue("price"))
                    {
                    itemPrice = Convert.ToDecimal(itemPage.GetPropertyValue("price").ToString());
                    priceMessage += "Original Extra Price : " + itemPrice.ToString("c");
                    }
                    else if (itemPage.HasProperty("pricePercentage") && itemPage.HasValue("pricePercentage"))
                    {
                    priceMessage = "Main product Price : " + itemMainPrice.ToString("c");
                    var extraDiscount = Convert.ToDecimal(itemPage.GetProperty("pricePercentage").Value.ToString());
                    itemPrice = itemMainPrice * (extraDiscount / 100);
                    priceMessage += ", Original Extra Price : " + itemPrice.ToString("c");
                    }
                
                if (pricePageIdsList.Any())
                    {
                    foreach (var pricePageId in pricePageIdsList)
                        {
                        var pricePage = umbracoHelper.TypedContent(pricePageId);
                        if (pricePage != null && (pricePage.HasProperty("priceValue") || pricePage.HasProperty("priceValuePercentage")))
                            {
                            var isDisount = pricePage.GetPropertyValue<bool>("isDiscount");
                            if (pricePage.HasValue("priceValue"))
                                {
                                var pricePagePrice = Convert.ToDecimal(pricePage.GetPropertyValue("priceValue").ToString());
                                if (isDisount)
                                    {
                                    discountPrice = itemPrice - pricePagePrice;
                                    priceMessage += ", less " + pricePage.Name
                                                            + " @ price : " + pricePagePrice.ToString("c")
                                                            + " = new price : " + discountPrice.ToString("c");
                                    }
                                else
                                    {
                                    discountPrice = itemPrice + pricePagePrice;
                                    priceMessage += ", add " + pricePage.Name
                                                            + " @ price : " + pricePagePrice.ToString("c")
                                                            + " = new price:" + discountPrice.ToString("c");
                                    }
                                }
                            else if (pricePage.HasValue("priceValuePercentage"))
                                {
                                var percentageValue = pricePage.GetPropertyValue("priceValuePercentage").ToString();
                                var pricePercent = Convert.ToDecimal(percentageValue);
                                var pricePagePercentPrice = itemPrice * (pricePercent / 100);
                                if (isDisount)
                                    {
                                    discountPrice = itemPrice - pricePagePercentPrice;
                                    priceMessage += ", less " + pricePage.Name
                                                            + " @ " + percentageValue + "% of original price : " + pricePagePercentPrice.ToString("c")
                                                            + " = new price: " + discountPrice.ToString("c");
                                    }
                                else
                                    {
                                    discountPrice = itemPrice + pricePagePercentPrice;
                                    priceMessage += ", add " + pricePage.Name
                                                            + " @ " + percentageValue + "% of original price : " + pricePagePercentPrice.ToString("c")
                                                            + " = new price : " + discountPrice.ToString("c");
                                    }
                                }
                            }
                        }
                    }
                }
            return priceMessage;
            }
        }
    }