using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Aggregator
{
    public class AggregateData
    {
        private readonly IDataRepository _dataRepository;

        public AggregateData(IDataRepository dataRepository)
        {
            _dataRepository = dataRepository;
        }

        public Dictionary<string, SortedItem> Aggregate()
        {
            var dataItems = _dataRepository.GetDataItems();

            var cars = new Dictionary<string, SortedItem>();

            foreach (var dataItem in dataItems)
            {
                foreach (var lineItem in dataItem.LineItems)
                {
                    var uniqueKey = lineItem.Description + lineItem.Link;
                    if (!cars.ContainsKey(uniqueKey))
                    {
                        var p = new SortedDictionary<DateTime, double> { { dataItem.DateTime, lineItem.Price } };
                        cars.Add(uniqueKey, new SortedItem { Prices = p, Link = lineItem.Link, LineItem = lineItem });
                    }
                    else
                    {
                        var dictionary = cars[uniqueKey];
                        dictionary.Prices[dataItem.DateTime] = lineItem.Price;
                        dictionary.Link = lineItem.Link;
                        dictionary.LineItem = lineItem;
                    }
                }
            }

            var amountOfDaysAgo = DateTime.Now.AddDays(-3);

            var orderedList = cars.Where(c => c.Value.Prices.Keys.Max() > amountOfDaysAgo).OrderByDescending(f => 1 - (f.Value.Prices.Values.Last() / f.Value.Prices.Values.First()));

            return orderedList.ToDictionary(d => d.Key, y => new SortedItem() { Prices = y.Value.Prices, Link = y.Value.Link, LineItem = y.Value.LineItem });
        }

        public string GetHTML(Dictionary<string, SortedItem> aggregate)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<ol>");
            foreach (var car in aggregate)
            {
                const string template = "<li><div><a Href='{3}'>{0} ([{4} days] R {5} to R {1})</a> <span class=\"sparklines\">{2}</span></div></li>";
                var link = string.IsNullOrWhiteSpace(car.Value.Link) ? "http://www.wesellcars.co.za" : car.Value.Link;
                sb.AppendLine(string.Format(template, car.Value.LineItem.Description, car.Value.Prices.Last().Value, string.Join(",", car.Value.Prices.Select(c => c.Value)), link, car.Value.Age, car.Value.Prices.First().Value));
            }
            sb.AppendLine("</ol>");
            sb.AppendLine("<script type=\"text/javascript\"> $('.sparklines').sparkline('html'); </script>");
            return sb.ToString();
        }
    }

    public class SortedItem
    {
        public string Link { get; set; }
        public SortedDictionary<DateTime, double> Prices { get; set; }
        public int Age
        {
            get
            {
                if (Prices == null)
                {
                    return 0;
                }
                else
                {
                    return Prices.Count();
                }
            }
        }

        public LineItem LineItem { get; set; }
    }

    public class DataPoint
    {
        public DataPoint(DateTime dateTime)
        {
            DateTime = dateTime;
            LineItems = new List<LineItem>();
        }

        public DateTime DateTime { get; set; }
        public List<LineItem> LineItems { get; }

        public void AddLineItem(LineItem item)
        {
            LineItems.Add(item);
        }
    }

    public class FileSystemLocation : IDataRepository
    {
        private readonly string _path;

        public FileSystemLocation(string path)
        {
            _path = path;
        }


        public IEnumerable<DataPoint> GetDataItems()
        {
            var dataPoints = new List<DataPoint>();
            var directoryInfo = new DirectoryInfo(_path);
            foreach (var file in directoryInfo.GetFiles())
            {
                var datePart = file.Name.Substring(11);
                var day = Convert.ToInt32(datePart.Substring(0, 2));
                var month = Convert.ToInt32(datePart.Substring(3, 2));
                var year = Convert.ToInt32(datePart.Substring(6, 4));
                DataPoint dPoint = new DataPoint(new DateTime(year, month, day));
                try
                {
                    var streamReader = file.OpenText();
                    var fileContent = streamReader.ReadToEnd();
                    var lines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        try
                        {
                            dPoint.LineItems.Add(new LineItem(line));
                        }
                        catch (Exception)
                        {
                            // skip line
                        }
                    }
                }
                catch (Exception)
                {
                    // skip file
                }
                dataPoints.Add(dPoint);
            }
            return dataPoints;
        }
    }

    public class LineItem
    {
        public LineItem(string lineContent)
        {
            var lastFiledSeperator = lineContent.LastIndexOf(',');
            var lastSection = lineContent.Substring(lastFiledSeperator);

            if (lastSection.StartsWith(",http"))
            {
                Link = lineContent.Substring(lastFiledSeperator + 1);
                var last = lineContent.Remove(lastFiledSeperator);
                lastFiledSeperator = last.LastIndexOf(',');
                lastSection = last.Substring(lastFiledSeperator);
            }
            var descriptionComponents = new List<string>();
            double price = 0;
            if (!Double.TryParse(string.Concat(lastSection.Where(Char.IsDigit)), out price))
            {
                var strings = lineContent.Split(new char[] { ',' });
                foreach (var item in strings)
                {
                    if (item.Contains("Price"))
                    {
                        price = Double.Parse(string.Concat(item.Where(Char.IsDigit)));
                    }
                    else if (!item.StartsWith("http"))
                    {
                        descriptionComponents.Add(item);
                    }
                }
                Description = string.Join(",", descriptionComponents);
            }
            else
            {
                Description = lineContent.Remove(lastFiledSeperator);
            }
            Price = price;
        }

        public string Description { get; set; }
        public double Price { get; set; }
        public string Link { get; set; }
    }
    public interface IDataRepository
    {
        IEnumerable<DataPoint> GetDataItems();
    }
}
