using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json;

namespace eft_dma_radar
{
    internal static class DyrkovMarketManager
    {
        /// <summary>
        /// Contains all Tarkov Loot mapped via BSGID String.
        /// </summary>
        public static ReadOnlyDictionary<string, LootItem> AllItems { get; }

        #region Static_Constructor
        static DyrkovMarketManager()
        {
            List<TarkovMarketItem> jsonItems = null;
            var allItems = new Dictionary<string, LootItem>(StringComparer.OrdinalIgnoreCase);
            // Get Market Loot
            if (!File.Exists("market.json") ||
            File.GetLastWriteTime("market.json").AddHours(24) < DateTime.Now) // only update every 24h
            {
                using (var client = new HttpClient())
                {
                    using var req = client.GetAsync("https://market_master.filter-editor.com/data/marketData_en.json").Result;
                    string json = req.Content.ReadAsStringAsync().Result;
                    jsonItems = JsonSerializer.Deserialize<List<TarkovMarketItem>>(json);
                    File.WriteAllText("market.json", json);
                }
            }
            else
            {
                var json = File.ReadAllText("market.json");
                jsonItems = JsonSerializer.Deserialize<List<TarkovMarketItem>>(json);
            }
            if (jsonItems is not null)
            {
                var jsonItemsFiltered = jsonItems.Where(x => x.isFunctional); // Filter only 'functional' items
                // Get Manual "Important" loot
                if (File.Exists("importantLoot.txt")) // Each line contains a BSG ID (item id) and nothing else
                {
                    foreach (var i in File.ReadAllLines("importantLoot.txt"))
                    {
                        var id = i.Split('#')[0].Trim(); // strip # comment char
                        var item = jsonItemsFiltered.FirstOrDefault(x => x.bsgId.Equals(id, StringComparison.OrdinalIgnoreCase));
                        if (item is not null)
                        {
                            allItems.TryAdd(item.bsgId, new LootItem()
                            {
                                Label = $"!!{item.shortName}",
                                Important = true,
                                AlwaysShow = true,
                                Item = item
                            });
                        }
                    }
                }
                else
                {
                    File.WriteAllText("importantLoot.txt", "5780cf7f2459777de4559322 ## Dorms Marked Key (example - one entry per line)");
                }
                foreach (var item in jsonItemsFiltered) // Add rest of loot to filter
                {
                    var value = GetItemValue(item);
                    allItems.TryAdd(item.bsgId, new LootItem()
                    {
                        Label = $"[{FormatNumber(value)}] {item.shortName}",
                        Item = item
                    });
                }
                AllItems = new(allItems); // update readonly ref
            }
            else throw new NullReferenceException("jsonItems");
        }
        #endregion

        #region Methods

        private static string FormatNumber(int num)
        {
            if (num >= 1000000)
                return (num / 1000000D).ToString("0.##") + "M";
            else if (num >= 1000)
                return (num / 1000D).ToString("0") + "K";

            else return num.ToString();
        }

        private static int GetItemValue(TarkovMarketItem item)
        {
            if (item.avg24hPrice > item.traderPrice)
                return item.avg24hPrice;
            else
                return item.traderPrice;
        }
        #endregion
    }

    #region Classes
    /// <summary>
    /// Class JSON Representation of Tarkov Market Data.
    /// </summary>
    public class TarkovMarketItem
    {
        public string uid { get; set; }
        public string name { get; set; } = "null";
        public List<string> tags { get; set; }
        public string shortName { get; set; } = "null";
        public int price { get; set; }
        public int basePrice { get; set; }
        public int avg24hPrice { get; set; } = 0;
        public int avg7daysPrice { get; set; }
        public string traderName { get; set; }
        public int traderPrice { get; set; } = 0;
        public string traderPriceCur { get; set; }
        public DateTime updated { get; set; }
        public int slots { get; set; }
        public double diff24h { get; set; }
        public double diff7days { get; set; }
        public string icon { get; set; }
        public string link { get; set; }
        public string wikiLink { get; set; }
        public string img { get; set; }
        public string imgBig { get; set; }
        public string bsgId { get; set; }
        public bool isFunctional { get; set; }
        public string reference { get; set; }
        public string apiKey { get; set; }
    }
    #endregion
}