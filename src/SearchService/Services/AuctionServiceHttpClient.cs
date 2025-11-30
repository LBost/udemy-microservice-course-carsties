using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services;

public class AuctionServiceHttpClient(HttpClient httpClient, IConfiguration config)
{
    private readonly HttpClient httpClient = httpClient;
    private readonly IConfiguration config = config;

    public async Task<List<Item>> GetItemsForSearchDb()
    {
        var lastUpdated = await DB.Find<Item, string>()
            .Sort(x => x.Descending(a => a.UpdatedAt))
            .Project(p => p.UpdatedAt.ToString())
            .ExecuteAnyAsync();

        if (lastUpdated)
            return await httpClient.GetFromJsonAsync<List<Item>>(config["AuctionServiceUrl"] + "/api/auctions?date=" + lastUpdated);
        else
            return await httpClient.GetFromJsonAsync<List<Item>>(config["AuctionServiceUrl"] + "/api/auctions");
    }
}
