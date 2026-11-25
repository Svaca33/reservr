using Reservr.Domain;

namespace Reservr.Domain.Tests;

public class LaneTests
{
    [Fact]
    public void Lane_with_positive_number_is_created()
    {
        var lane = new Lane(1);

        Assert.Equal(1, lane.Number);
        Assert.NotEqual(Guid.Empty, lane.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Lane_with_non_positive_number_throws(int number)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Lane(number));
    }

    [Fact]
    public void Each_lane_gets_its_own_id()
    {
        Assert.NotEqual(new Lane(1).Id, new Lane(2).Id);
    }
}
