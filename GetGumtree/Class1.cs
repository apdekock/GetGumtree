using GetGumtree;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public class DataAccess
    {

        public DataAccess()
        {
        }

        public string SaveItems(List<ScrapeItem> items)
        {
            var result = string.Empty;
            try
            {
                var sqlConnection = "Data Source=tcp:willowcrest.database.windows.net,1433;Integrated Security=False;Initial Catalog=GetWeSellCars;Persist Security Info=False;User ID=-admin-pdk@willowcrest;Password=sLpIQEFR%3k!k5&I8f1TvblFR999;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=300;";
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.Open();
                    using (SqlCommand insertScrape = new SqlCommand("INSERT INTO dbo.Scrape (Date) VALUES (@value);SELECT CAST(scope_identity() AS int)", conn))
                    {
                        insertScrape.Parameters.AddWithValue("@value", DateTime.Now);
                        var newID = (int)insertScrape.ExecuteScalar();

                        foreach (var item in items)
                        {
                            try
                            {
                                using (SqlCommand insertItem = new SqlCommand("INSERT INTO dbo.ScrapeItem ([Title] ,[Link] ,[Price] ,[Year] ,[Branch] ,[Mileage] ,[ScrapeID])"
                                     + " VALUES (@title, @link, @price, @year, @branch, @mileage, @scrapeId)", conn))
                                {
                                    insertItem.Parameters.AddWithValue("@title", item.Title);
                                    insertItem.Parameters.AddWithValue("@link", item.Link.AbsoluteUri);
                                    insertItem.Parameters.AddWithValue("@price", item.Price);
                                    insertItem.Parameters.AddWithValue("@year", item.Year);
                                    insertItem.Parameters.AddWithValue("@branch", item.Branch);
                                    insertItem.Parameters.AddWithValue("@mileage", item.Mileage);
                                    insertItem.Parameters.AddWithValue("@scrapeId", newID);

                                    var rows = insertItem.ExecuteNonQuery();
                                    Console.WriteLine(string.Format("Rows inserted: {0} - {1}", rows, item.Title));
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                        }
                        Program.sendNotification(string.Format("Azure Function App: Items Persisted - {0}", newID), 1);
                    }
                }
            }
            catch (Exception e)
            {
                result = e.Message;
                Console.WriteLine(e.Message);
                throw;
            }
            return result;
        }


    }


}
