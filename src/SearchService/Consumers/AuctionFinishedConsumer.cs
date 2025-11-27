using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers
{
    public class AuctionFinishedConsumer : IConsumer<AuctionFinished>
    {
        public async Task Consume(ConsumeContext<AuctionFinished> context)
        {
            Console.WriteLine("--> Consuming Auction finished");

            var auction = await DB.Find<Item>().OneAsync(context.Message.AuctionId);

            if (context.Message.ItemSold)
            {
                auction.Winner = context.Message.Winner;
                auction.SoldAmount = context.Message.Amount ?? 0;
            }

            auction.Status = auction.SoldAmount < auction.ReservePrice ?
                "ReserveNotMet" : "Finished";

            await auction.SaveAsync();
        }
    }
}
