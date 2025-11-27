using AuctionService.Data;
using Contracts;
using MassTransit;

namespace AuctionService.Consumers
{
    public class BidPlacedConsumer(AuctionDbContext dbContext) : IConsumer<BidPlaced>
    {
        private readonly AuctionDbContext dbContext = dbContext;

        public async Task Consume(ConsumeContext<BidPlaced> context)
        {
            Console.WriteLine("--> Consuming bid placed");

            var auction = await dbContext.Auctions.FindAsync(context.Message.AuctionId);

            if (auction.CurrentHighBid == 0 || context.Message.BidStatus.Contains("Accepted") && context.Message.Amount > auction.CurrentHighBid)
            {
                auction.CurrentHighBid = context.Message.Amount;
                auction.UpdatedAt = DateTime.UtcNow;
                dbContext.Auctions.Update(auction);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
