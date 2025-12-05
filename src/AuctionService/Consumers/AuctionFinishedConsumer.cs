using AuctionService.Data;
using Contracts;
using MassTransit;

namespace AuctionService.Consumers
{
    public class AuctionFinishedConsumer(AuctionDbContext dbContext) : IConsumer<AuctionFinished>
    {
        private readonly AuctionDbContext dbContext = dbContext;

        public async Task Consume(ConsumeContext<AuctionFinished> context)
        {
            Console.WriteLine("--> Consuming auction finished");

            var auction = await dbContext.Auctions.FindAsync(Guid.Parse(context.Message.AuctionId));

            if (context.Message.ItemSold)
            {
                auction.Winner = context.Message.Winner;
                auction.SoldAmount = context.Message.Amount ?? 0;
            }

            auction.Status = auction.SoldAmount < auction.ReservePrice ?
                Entities.Status.ReserveNotMet : Entities.Status.Finished;

            await dbContext.SaveChangesAsync();
        }
    }
}
