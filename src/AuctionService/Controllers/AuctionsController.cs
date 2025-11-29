using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionService.Controllers
{
    [ApiController]
    [Route("api/auctions")]
    public class AuctionsController(IAuctionRepository repository, IMapper mapper, IPublishEndpoint publishEndpoint) : ControllerBase
    {
        private readonly IAuctionRepository repository = repository;
        private readonly IMapper _mapper = mapper;
        private readonly IPublishEndpoint publishEndpoint = publishEndpoint;

        [HttpGet]
        public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
        {
            return await repository.GetAllAuctionsAsync(date);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
        {
            var auction = await repository.GetAuctionByIdAsync(id);

            if (auction == null) return NotFound();

            return auction;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
        {
            var auction = _mapper.Map<Auction>(auctionDto);
            auction.Seller = User.Identity.Name;

            repository.AddAuction(auction);

            var newAuction = _mapper.Map<AuctionDto>(auction);
            await publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

            var result = await repository.SaveChangesAsync();

            if (!result) return BadRequest("Could not save Auction to the Database");

            return CreatedAtAction(nameof(GetAuctionById), new { auction.Id }, newAuction);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
        {
            var auction = await repository.GetAuctionEntityById(id);

            if (auction == null) return NotFound();
            if (auction.Seller != User.Identity.Name) return Forbid();

            auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
            auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
            auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
            auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
            auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

            await publishEndpoint.Publish(_mapper.Map<AuctionUpdated>(auction));

            var result = await repository.SaveChangesAsync();
            if (result) return Ok();
            return BadRequest("Problem saving changes.");
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteAuction(Guid id)
        {
            var auction = await repository.GetAuctionEntityById(id);

            if (auction == null) return NotFound();
            if (auction.Seller != User.Identity.Name) return Forbid();

            await publishEndpoint.Publish<AuctionDeleted>(new { Id = auction.Id.ToString() });

            repository.RemoveAuction(auction);

            var result = await repository.SaveChangesAsync();
            if (!result) return BadRequest("Could not update DB");
            return Ok();
        }
    }
}
