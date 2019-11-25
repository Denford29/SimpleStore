using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using umbraco;

namespace SimplestStore.App_Code
    {
    public class siteHelpers
        {
        public static string LimitWordsInText(string stringText, int MaxWords)
            {
            var limitedText = "";
            if (!string.IsNullOrEmpty(stringText))
                {
                var nonHtmlString = library.StripHtml(stringText);
                var wordsListArray = nonHtmlString.Replace("\"", "").Split(' ');
                var wordCounter = 0;

                foreach (var word in wordsListArray)
                    {
                    wordCounter++;
                    if (wordCounter <= MaxWords)
                        {
                        limitedText += word.Trim() + " ";
                        }
                    }
                }
            return limitedText;
            }

        }
    }