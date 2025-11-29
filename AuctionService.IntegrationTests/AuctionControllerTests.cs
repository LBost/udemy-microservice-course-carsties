using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace AuctionService.IntegrationTests
{
    [Collection("CustomWebAppFactory")]
    public class AuctionControllerTests : IAsyncLifetime
    {
        private readonly CustomWebAppFactory factory;
        private readonly HttpClient httpClient;

        public AuctionControllerTests(CustomWebAppFactory factory)
        {
            this.factory = factory;
            httpClient = this.factory.CreateClient();
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public Task DisposeAsync()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
            DbHelper.ReinitDbForTests(db);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task GetAuctions_ReturnsSuccessStatusCode()
        {
            // Arrange
            var request = "/api/auctions";
            // Act
            var response = await httpClient.GetAsync(request);
            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Ford", content);
            Assert.Contains("Bugatti", content);
        }

        [Fact]
        public async Task GetAuctionById_ReturnsSuccessStatusCode()
        {
            // Arrange
            var auctionId = "afbee524-5972-4075-8800-7d1f9d7b0a0c";
            var request = $"/api/auctions/{auctionId}";
            // Act
            var response = await httpClient.GetAsync(request);
            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Ford", content);
            Assert.Contains("GT", content);
        }

        [Fact]
        public async Task GetAuctionById_ReturnsNotFound_ForInvalidId()
        {
            // Arrange
            var auctionId = "00000000-0000-0000-0000-000000000000";
            var request = $"/api/auctions/{auctionId}";
            // Act
            var response = await httpClient.GetAsync(request);
            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetAuctionById_WithInvalidGuid_Returns400()
        {
            // Arrange
            var auctionId = "notaguid";
            var request = $"/api/auctions/{auctionId}";
            // Act
            var response = await httpClient.GetAsync(request);
            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateAuction_WithNoAuth_Returns401()
        {
            // Arrange
            var auction = new CreateAuctionDto { Make = "Test" };
            var request = $"/api/auctions";
            // Act
            var response = await httpClient.PostAsJsonAsync(request, auction);
            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateAuction_WithAuth_Returns201()
        {
            // Arrange
            var auction = DbHelper.GetAuctionDtoForCreate();
            var request = $"/api/auctions";
            httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));
            // Act
            var response = await httpClient.PostAsJsonAsync(request, auction);
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdAuction = await response.Content.ReadFromJsonAsync<AuctionDto>();
            Assert.NotNull(createdAuction);
            Assert.Equal("bob", createdAuction.Seller);
        }

        [Fact]
        public async Task CreateAuction_WithInvalidCreateAuctionDto_ShouldReturn400()
        {
            // arrange
            var auction = DbHelper.GetAuctionDtoForCreate();
            auction.Make = ""; // invalid make
            var request = $"/api/auctions";
            httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));
            // act
            var response = await httpClient.PostAsJsonAsync(request, auction);
            // assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateAuction_WithValidUpdateDtoAndUser_ShouldReturn200()
        {
            // arrange
            var updatedAuction = new UpdateAuctionDto { Make = "updated" };
            var request = $"/api/auctions/{DbHelper.GetAuctionId}";
            httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser(DbHelper.GetAuctionSeller));
            // act
            var response = await httpClient.PutAsJsonAsync(request, updatedAuction);
            // assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateAuction_WithValidUpdateDtoAndInvalidUser_ShouldReturn403()
        {
            // arrange
            var updatedAuction = new UpdateAuctionDto { Make = "updated" };
            var request = $"/api/auctions/{DbHelper.GetAuctionId}";
            httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("test"));
            // act
            var response = await httpClient.PutAsJsonAsync(request, updatedAuction);
            // assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
