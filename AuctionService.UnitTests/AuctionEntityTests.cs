namespace AuctionService.UnitTests
{
    public class AuctionEntityTests
    {
        [Fact]
        public void HasReservedPrice_ReservePriceGTZero_True()
        {
            //arrange
            var auction = new AuctionService.Entities.Auction
            {
                Id = Guid.NewGuid(),
                ReservePrice = 100
            };
            //act
            var result = auction.HasReservePrice();
            //assert
            Assert.True(result);
        }

        [Fact]
        public void HasReservedPrice_ReservePriceIsZero_False()
        {
            //arrange
            var auction = new AuctionService.Entities.Auction
            {
                Id = Guid.NewGuid(),
                ReservePrice = 0
            };
            //act
            var result = auction.HasReservePrice();
            //assert
            Assert.False(result);
        }
    }
}
