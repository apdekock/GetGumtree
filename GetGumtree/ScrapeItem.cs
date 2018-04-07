using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetGumtree
{
    public class ScrapeItem
    {
        public ScrapeItem(string title, Uri link, string year, string price, string mileage, string branch)
        {
            Title = title;
            Link = link;
            var outYear = 0;
            if (int.TryParse(year, out outYear))
                Year = outYear;
            Branch = branch;
            var outmileage = 0;
            if (int.TryParse(mileage, out outmileage))
                Mileage = outmileage;
            var outPrice = 0;
            if (int.TryParse(price, out outPrice))
                Price = outPrice / 100;
        }

        public string Title { get; set; }
        public Uri Link { get; set; }
        public int Year { get; set; }
        public double Price { get; set; }
        public int Mileage { get; set; }
        public string Branch { get; set; }

        public override string ToString()
        {
            return string.Join(",", new string[] { Title, Link.AbsolutePath });
        }
    }
}
