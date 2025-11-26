using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers
{
    public class AuctionUpdatedConsumer(IMapper mapper) : IConsumer<AuctionUpdated>
    {
        private readonly IMapper mapper = mapper;

        public async Task Consume(ConsumeContext<AuctionUpdated> context)
        {
            Console.WriteLine($"AuctionUpdatedConsumer received a message: {context.Message.Id}");

            var item = mapper.Map<Item>(context.Message);

            var result = await DB.Update<Item>()
                .Match(i => i.ID == context.Message.Id)
                .ModifyOnly(x => new
                {
                    x.Make,
                    x.Model,
                    x.Color,
                    x.Mileage,
                    x.Year
                }, item)
                .ExecuteAsync();

            if (!result.IsAcknowledged)
                throw new MessageException(typeof(AuctionUpdated), "Problem updating mongodb");
        }
    }
}
