using AuctionService.Controllers;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AuctionService.RequestHelpers;
using AutoFixture;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AuctionService.UnitTests;

public class AuctionControllerTest
{
    private readonly Mock<IAuctionRepository> auctionRepository;
    private readonly Mock<IPublishEndpoint> publishEndpoint;
    private readonly Fixture fixture;
    private readonly AuctionsController auctionController;
    private readonly IMapper mapper;

    public AuctionControllerTest()
    {
        fixture = new Fixture();
        auctionRepository = new Mock<IAuctionRepository>();
        publishEndpoint = new Mock<IPublishEndpoint>();

        var mockMapper = new MapperConfiguration(mc =>
        {
            mc.AddMaps(typeof(MappingProfiles).Assembly);
        }, NullLoggerFactory.Instance).CreateMapper().ConfigurationProvider;

        mapper = new Mapper(mockMapper);
        auctionController = new AuctionsController(auctionRepository.Object, mapper, publishEndpoint.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = Utils.Helpers.GetClaimsPrincipal()
                }
            }
        };
    }

    [Fact]
    public async Task GetAuctions_WithNoParams_Return10Auctions()
    {
        //arrange
        var auctions = fixture.CreateMany<AuctionDto>(10).ToList();
        auctionRepository.Setup(repo => repo.GetAllAuctionsAsync(null))
            .ReturnsAsync(auctions);

        //act
        var result = await auctionController.GetAllAuctions(null);

        //assert
        Assert.Equal(10, result.Value.Count);
        Assert.IsType<ActionResult<List<AuctionDto>>>(result);
    }

    [Fact]
    public async Task GetAuctionById_WithValidGuid_ReturnAuction()
    {
        //arrange
        var auction = fixture.Create<AuctionDto>();

        auctionRepository
            .Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(auction);

        //act
        var result = await auctionController.GetAuctionById(auction.Id);

        //assert
        Assert.Equal(auction.Make, result.Value.Make);
        Assert.IsType<ActionResult<AuctionDto>>(result);
    }

    [Fact]
    public async Task GetAuctionById_WithInValidGuid_ReturnsNotFound()
    {
        //arrange
        auctionRepository
            .Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);

        //act
        var result = await auctionController.GetAuctionById(Guid.NewGuid());

        //assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateAuction_WithAuctionDto_ReturnsCreatedAtAction()
    {
        //arrange
        var auction = fixture.Create<CreateAuctionDto>();
        auctionRepository.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
        auctionRepository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

        //act
        var result = await auctionController.CreateAuction(auction);
        var createdAtActionResult = result.Result as CreatedAtActionResult;

        //assert
        Assert.NotNull(createdAtActionResult);
        Assert.Equal("GetAuctionById", createdAtActionResult!.ActionName);
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task CreateAuction_WithAuctionDto_ReturnsBadRequest()
    {
        //arrange
        var auction = fixture.Create<CreateAuctionDto>();
        auctionRepository.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
        auctionRepository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(false);

        //act
        var result = await auctionController.CreateAuction(auction);

        //assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateAuction_FailedSave_Returns400BadRequest()
    {
        //arrange
        var auction = fixture.Create<CreateAuctionDto>();
        auctionRepository.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
        auctionRepository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(false);

        //act
        var result = await auctionController.CreateAuction(auction);

        //assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAuction_WithUpdateAuctionDto_ReturnsOkResponse()
    {
        //arrange
        var auction = fixture.Build<Auction>().Without(a => a.Item).Create();
        auction.Item = fixture.Build<Item>().Without(x => x.Auction).Create();
        auction.Seller = "test";
        var updateDto = fixture.Create<UpdateAuctionDto>();

        auctionRepository.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>()))
            .ReturnsAsync(auction);
        auctionRepository.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(true);

        //act
        var result = await auctionController.UpdateAuction(auction.Id, updateDto);

        //assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task UpdateAuction_WithInvalidUser_Returns403Forbid()
    {
        //arrange
        var auction = fixture.Build<Auction>().Without(a => a.Item).Create();
        auction.Item = fixture.Build<Item>().Without(x => x.Auction).Create();
        auction.Seller = "not-test";
        var updateDto = fixture.Create<UpdateAuctionDto>();

        auctionRepository.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>()))
            .ReturnsAsync(auction);

        //act
        var result = await auctionController.UpdateAuction(auction.Id, updateDto);

        //assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateAuction_WithInvalidGuid_ReturnsNotFound()
    {
        //arrange
        var auction = fixture.Build<Auction>().Without(a => a.Item).Create();
        var updateDto = fixture.Create<UpdateAuctionDto>();

        auctionRepository.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);

        //act
        var result = await auctionController.UpdateAuction(auction.Id, updateDto);

        //assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithValidUser_ReturnsOkResponse()
    {
        //arrange
        var auction = fixture.Build<Auction>().Without(a => a.Item).Create();
        auction.Seller = "test";

        auctionRepository.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>()))
            .ReturnsAsync(auction);
        auctionRepository.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(true);

        //act
        var result = await auctionController.DeleteAuction(auction.Id);

        //assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithInvalidGuid_Returns404Response()
    {
        //arrange
        var auction = fixture.Build<Auction>().Without(a => a.Item).Create();
        auction.Seller = "test";
        auctionRepository.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);

        //act
        var result = await auctionController.DeleteAuction(auction.Id);

        //assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithInvalidUser_Returns403Response()
    {
        //arrange
        var auction = fixture.Build<Auction>().Without(a => a.Item).Create();
        auction.Seller = "not-test";

        auctionRepository.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>()))
            .ReturnsAsync(auction);

        //act
        var result = await auctionController.DeleteAuction(auction.Id);

        //assert
        Assert.IsType<ForbidResult>(result);
    }
}
