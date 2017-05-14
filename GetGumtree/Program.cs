using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Aggregator;
using GitSharp;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Net.Http;
using File = System.IO.File;

namespace GetGumtree
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        private static void Main(string[] args)
        {
            //var arg0 = @"C:\temp\Dropbox\jnk\WeSellCars\"; // file path
            //var arg1 = @"C:\git\apdekock.github.io\"; //repo path
            //var arg2 = @"_posts\2015-08-04-weMineData.markdown"; //post path
            //var arg3 = @"C:\Program Files\Git\cmd\git.exe"; //git path
            //var arg4 = @secrect key push notfications            
            try
            {
                StringBuilder listOfLines = new StringBuilder();
                var chromeOptions = new ChromeOptions();
                chromeOptions.AddArguments("-incognito");
                using (IWebDriver driver = new ChromeDriver(Path.Combine(Directory.GetCurrentDirectory(), "WebDriverServer"), chromeOptions))
                {
                    driver.Navigate().GoToUrl("http://www.wesellcars.co.za/");
                    var findElement = driver.FindElements(By.CssSelector("#feed_1 > div > div.vehicles.grid > div.item"));

                    foreach (var item in findElement)
                    {
                        var image = item.FindElement(By.CssSelector("a"));
                        var link = image.GetAttribute("href");
                        var lines =
                            new List<string>(item.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                            {
                                link
                            };
                        var format = string.Join(",", lines);
                        listOfLines.AppendLine(format);
                        Console.WriteLine(format);
                    }

                    sendNotification(string.Format("Scraped: {0} items from WeSellCars.", findElement.Count), 1, args[4]);
                    driver.Quit();
                }

                var cTempCarsTxt = args[0] + @"\WeSellCars_" + DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss") + ".csv";
                var fileStream = File.Create(cTempCarsTxt);
                fileStream.Close();
                File.WriteAllText(cTempCarsTxt, listOfLines.ToString());


                StringBuilder postTemplate = new StringBuilder();
                postTemplate.AppendLine("---");
                postTemplate.AppendLine("layout: post ");
                postTemplate.AppendLine("title: \"Scraping and GitSharp, and Spark lines\" ");
                postTemplate.AppendLine("date: 2016-08-07");
                postTemplate.AppendLine(
                    "quote: \"If you get pulled over for speeding. Tell them your spouse has diarrhoea. — Phil Dunphy [Phil’s - osophy]\"");
                postTemplate.AppendLine("categories: scraping, auto generating post, gitsharp");
                postTemplate.AppendLine("---");
                postTemplate.AppendLine(
                    string.Format(
                        "This page is a daily re-generated post (last re-generated  **{0}**), that shows the movement of prices on the [www.weSellCars.co.za](http://www.wesellcars.co.za) website.",
                        DateTime.Now));
                postTemplate.AppendLine("");
                postTemplate.AppendLine("## Why?");
                postTemplate.AppendLine("");
                postTemplate.AppendLine(
                    "This post is the culmination of some side projects I've been playing around with. Scraping, looking for a way to integrate with git through C# and a challenge to use this blog (which has no back-end or support for any server side scripting) to dynamically update a post. I realise that would best be accomplished through just making new posts but I opted for an altered post as this is a tech blog, and multiple posts about car prices would not be appropriate.");
                postTemplate.AppendLine("");
                postTemplate.AppendLine("# Lessons learned");
                postTemplate.AppendLine("");
                postTemplate.AppendLine(
                    "* [GitSharp](http://www.eqqon.com/index.php/GitSharp) is limited and I needed to grab the project from [github](https://github.com/henon/GitSharp) in order to use it.");
                postTemplate.AppendLine(
                    "    The NuGet package kept on complaining about a **repositoryformatversion** setting in config [Core] that it required even though it was present, it still complained. So, I downloaded the source to debug the issue but then I did not encounter it. Apart from that - gitsharp did not allow me to push - and it seems the project does not have a lot of contribution activity (not criticising, just stating. I should probably take this up and contribute, especially as I would like to employ git as a file store for an application. Levering off the already refined functions coudl be a win but more on that in another post).");
                postTemplate.AppendLine(
                    "* Scraping with Selenium is probably not the best way - rather employ [HttpClient](https://msdn.microsoft.com/en-us/library/system.net.http.httpclient(v=vs.118).aspx).");
                postTemplate.AppendLine(
                    "* For quick, easy and painless sparklines [jQuery Sparklines](http://omnipotent.net/jquery.sparkline/#s-about)");
                postTemplate.AppendLine(
                    "* No backend required, just a simple process running on a server, that commits to a repo (ghPages) gets the job done.");

                postTemplate.AppendLine("");
                var aggregateData = new AggregateData(new FileSystemLocation(args[0]));
                var dictionary = aggregateData.Aggregate();

                var html = aggregateData.GetHTML(dictionary);
                if (dictionary.Count > 0)
                {
                    postTemplate.AppendLine("## The List");
                }
                postTemplate.AppendLine(html);

                // update post file
                FileInfo fi = new FileInfo(args[1] + args[2]);
                var streamWriter = fi.CreateText();
                streamWriter.WriteLine(postTemplate.ToString());

                streamWriter.Flush();
                streamWriter.Dispose();

                Repository repository = new Repository(args[1]);

                repository.Index.Add(args[1] + args[2]);
                Commit commited = repository.Commit(string.Format("Updated {0}", DateTime.Now),
                    new Author("Philip de Kock", "philipdekock@gmail.com"));
                if (commited.IsValid)
                {
                    string gitCommand = args[3];
                    const string gitPushArgument = @"push origin";
                    ProcessStartInfo psi = new ProcessStartInfo(gitCommand, gitPushArgument)
                    {
                        WorkingDirectory = args[1],
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
            }
            catch (Exception e)
            {
                sendNotification(string.Format("Scraper exception: {0}",e.Message), 5, args[4]);
                Console.WriteLine(e.Message);
            }
        }

        private static void sendNotification(string message, int level, string secret)
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
