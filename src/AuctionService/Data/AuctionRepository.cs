using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Data
{
    public class AuctionRepository(AuctionDbContext dbContext, IMapper mapper) : IAuctionRepository
    {
        private readonly AuctionDbContext dbContext = dbContext;
        private readonly IMapper mapper = mapper;

        public void AddAuction(Auction auction)
        {
            dbContext.Auctions.Add(auction);
        }

        public async Task<AuctionDto> GetAuctionByIdAsync(Guid id)
        {
            return await dbContext.Auctions
                .ProjectTo<AuctionDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Auction> GetAuctionEntityById(Guid id)
        {
            return await dbContext.Auctions
                .Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<AuctionDto>> GetAllAuctionsAsync(string date)
        {
            var query = dbContext.Auctions
                .OrderBy(x => x.Item.Make)
                .AsQueryable();

            if (!string.IsNullOrEmpty(date))
                query = query
                    .Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);

            return await query
                .ProjectTo<AuctionDto>(mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await dbContext.SaveChangesAsync() > 0;
        }

        public void RemoveAuction(Auction auction)
        {
            dbContext.Auctions.Remove(auction);
        }
    }
}
