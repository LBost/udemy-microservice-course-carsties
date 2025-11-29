using AuctionService.Data;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Utils;
using Contracts;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace AuctionService.IntegrationTests
{
    [Collection("CustomWebAppFactory")]
    public class AuctionServiceBusTests : IAsyncLifetime
    {
        private readonly CustomWebAppFactory factory;
        private readonly HttpClient httpClient;
        private ITestHarness testHarness;

        public AuctionServiceBusTests(CustomWebAppFactory factory)
        {
            this.factory = factory;
            httpClient = this.factory.CreateClient();
            testHarness = this.factory.Services.GetTestHarness();
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
        public async Task CreateAuction_WithValidObject_ShouldProduceAuctionCreated()
        {
            // Arrange
            var auctionDto = DbHelper.GetAuctionDtoForCreate();
            var request = "/api/auctions";
            httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));
            // Act
            var response = await httpClient.PostAsJsonAsync(request, auctionDto);
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.True(await testHarness.Published.Any<AuctionCreated>());
            var publishedMessage = testHarness.Published.Select<AuctionCreated>().First().Context.Message;
            Assert.Equal(auctionDto.Make, publishedMessage.Make);
            Assert.Equal(auctionDto.Model, publishedMessage.Model);
            Assert.Equal(auctionDto.Color, publishedMessage.Color);
            Assert.Equal(auctionDto.Year, publishedMessage.Year);
            Assert.Equal(auctionDto.ReservePrice, publishedMessage.ReservePrice);
        }
    }
}
