using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class SmokeTests
{
    [Fact]
    public void EnumsExist() => Assert.Equal(0, (int)SkeletonTypeId.Marcher);
}
