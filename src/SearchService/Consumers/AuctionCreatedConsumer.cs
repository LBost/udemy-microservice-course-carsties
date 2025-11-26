using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers
{
    public class AuctionCreatedConsumer(IMapper mapper) : IConsumer<AuctionCreated>
    {
        private readonly IMapper mapper = mapper;

        public async Task Consume(ConsumeContext<AuctionCreated> context)
        {
            Console.WriteLine($"AuctionCreatedConsumer received a message: {context.Message.Id}");

            var item = mapper.Map<Item>(context.Message);
            try
            {
                await item.SaveAsync();
            }
            catch (Exception)
            {
                throw new MessageException(typeof(AuctionCreated), "Problem creating auction");
            }
        }
    }
}
