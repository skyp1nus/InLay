using AwesomeAssertions;
using InLay.Core;
using Xunit;

namespace InLay.Tests;

public class ProductInfoTests
{
    [Fact]
    public void NameIsInLay()
    {
        ProductInfo.Name.Should().Be("InLay");
        ProductInfo.AutostartValueName.Should().Be("InLay");
    }

    [Fact]
    public void IdentifiersAreNonEmptyAndDistinct()
    {
        ProductInfo.MutexId.Should().NotBe(Guid.Empty);
        ProductInfo.ActivationEventId.Should().NotBe(Guid.Empty);
        ProductInfo.MutexId.Should().NotBe(ProductInfo.ActivationEventId);
    }

    [Fact]
    public void KernelObjectNamesAreSessionLocalAndEmbedTheirGuids()
    {
        ProductInfo.MutexName.Should().StartWith(@"Local\InLay-")
            .And.Contain(ProductInfo.MutexId.ToString("N"));
        ProductInfo.ActivationEventName.Should().StartWith(@"Local\InLay-activate-")
            .And.Contain(ProductInfo.ActivationEventId.ToString("N"));
    }

    [Fact]
    public void KernelObjectNamesAreDistinct()
    {
        ProductInfo.MutexName.Should().NotBe(ProductInfo.ActivationEventName);
    }
}
