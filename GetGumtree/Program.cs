﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using Aggregator;
using GitSharp;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Net.Http;
using File = System.IO.File;
using System.Threading;
using OpenQA.Selenium.Support.UI;
using Polly;

namespace GetGumtree
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        private static string secretKeyPushNotification = string.Empty;
        private static void Main(string[] args)
        {
            //var arg0 = @"C:\temp\Dropbox\jnk\WeSellCars\"; // file path
            //var arg1 = @"C:\git\apdekock.github.io\"; //repo path
            //var arg2 = @"_posts\2015-08-04-weMineData.markdown"; //post path
            //var arg3 = @"C:\Program Files\Git\cmd\git.exe"; //git path
            //var arg4 = @secrect key push notfications            
            //var arg5 = @"http://www.wesellcars.co.za/vehicle/category/all"    

            int ZeroCount = 0;
            secretKeyPushNotification = args[4];
            try
            {
                var chromeOptions = new ChromeOptions();
                chromeOptions.AddArguments("-incognito");
                chromeOptions.AddArguments("--start-fullscreen");
                chromeOptions.AddArguments("--kiosk");
                chromeOptions.AddUserProfilePreference("profile.default_content_setting_values.images", 2);

                using (IWebDriver driver = new ChromeDriver(args[0], chromeOptions))
                {
                    driver.Navigate().GoToUrl(args[5]);

                    var scrapedItems = new List<ScrapeItem>();

                    bool hasNextPage = true;

                    while (hasNextPage)
                    {
                        try
                        {
                            Policy
                                .Handle<StaleElementReferenceException>()
                                .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                    , (exception, timeSpan, retryAttempt) =>
                                    {
                                        Console.WriteLine("On " + timeSpan + ": " + exception.Message);
                                    }).Execute(() =>
                                        {
                                            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                                            wait.Until(driver1 => { Thread.Sleep(1000); return true; });
                                            List<ScrapeItem> collection = ScrapePage(args, driver);
                                            if (collection.Count == 0) { throw new StaleElementReferenceException("empty collection"); }
                                            scrapedItems.AddRange(collection);
                                        });

                            hasNextPage = driver.FindElements(By.CssSelector(".ListNavigation_Next")).FirstOrDefault() != null;
                            if (hasNextPage)
                            {
                                Policy
                                    .Handle<InvalidOperationException>()
                                    .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                        , (exception, timeSpan, retryAttempt) =>
                                        {
                                            Console.WriteLine("On " + timeSpan + ": " + exception.Message);
                                            driver.FindElements(By.CssSelector(".ListNavigation_Next")).First().SendKeys(Keys.PageDown);
                                        }).Execute(() =>
                                        {
                                            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                                            wait.Until(driver1 => { Thread.Sleep(3000); return true; });
                                            driver.FindElements(By.CssSelector(".ListNavigation_Next")).First().Click();
                                            wait.Until(driver1 => { Thread.Sleep(3000); return true; });


                                        });
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }

                        Console.WriteLine(string.Format("Items scraped: {0}", scrapedItems.Count));
                        if (scrapedItems.Count == 0)
                        {
                            ZeroCount++;
                        }
                        if (ZeroCount > 3)
                        {
                            throw new DivideByZeroException("ZeroCount Exceeded");
                        }
                    }

                    var dataAccess = new DataAccess.DataAccess();
                    var saveResult = dataAccess.SaveItems(scrapedItems);
                    sendNotification(string.Format("Scraped: {0} items from WeSellCars.", scrapedItems.Count), 1);
                    driver.Quit();
                }
            }
            catch (Exception e)
            {
                sendNotification(string.Format("Scraper exception: {0}", e.Message), 5);
                Console.WriteLine(e.Message);
            }
        }


        private static List<ScrapeItem> ScrapePage(string[] args, IWebDriver driver)
        {
            var scrapedItems = new List<ScrapeItem>();

            var vehicles = driver.FindElements(By.CssSelector(".vehicles"));

            foreach (var item in vehicles)
            {

                var title = string.Empty;
                var Branch = string.Empty;
                var Year = string.Empty;
                var Price = string.Empty;
                var Mileage = string.Empty;

                var values = item.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                var link = item.FindElement(By.CssSelector("img")).GetAttribute("src");
                title = values.First();

                foreach (var value in values)
                {
                    if (value.StartsWith("Year:"))
                    {
                        Year = new string(value.Where(Char.IsDigit).ToArray());
                    }
                    const string BranchText = "Branch:";
                    if (value.StartsWith(BranchText))
                    {
                        Branch = value.Remove(0, BranchText.Length);
                    }
                    if (value.Contains("Price:"))
                    {
                        Price = new string(value.Where(Char.IsDigit).ToArray());
                    }
                    if (value.StartsWith("Mileage:"))
                    {
                        Mileage = new string(value.Where(Char.IsDigit).ToArray());
                    }
                }

                try
                {
                    var scrapedItem = new ScrapeItem(title, new Uri(link), Year, Price, Mileage, Branch);
                    scrapedItems.Add(scrapedItem);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return scrapedItems;
        }

        public static void sendNotification(string message, int level)
        {
            sendNotification(message, level, secretKeyPushNotification);
        }
        public static void sendNotification(string message, int level, string secret)
        {
            var values = new Dictionary<string, string>
            {
                { "message", message },
                { "level", level.ToString() },
                { "secret", secret }
            };

            var content = new FormUrlEncodedContent(values);

            var response = client.PostAsync("https://api.pushjet.io/message", content).Result;
        }
    }
}
