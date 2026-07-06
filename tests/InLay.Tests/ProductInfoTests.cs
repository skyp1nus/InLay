using AwesomeAssertions;
using InLay.Core;
using Xunit;

namespace InLay.Tests;

public class ProductInfoTests
{
    [Fact]
    public void Name_is_InLay()
    {
        ProductInfo.Name.Should().Be("InLay");
        ProductInfo.AutostartValueName.Should().Be("InLay");
    }

    [Fact]
    public void Identifiers_are_non_empty_and_distinct()
    {
        ProductInfo.MutexId.Should().NotBe(Guid.Empty);
        ProductInfo.ActivationEventId.Should().NotBe(Guid.Empty);
        ProductInfo.MutexId.Should().NotBe(ProductInfo.ActivationEventId);
    }

    [Fact]
    public void Kernel_object_names_are_session_local_and_embed_their_guids()
    {
        ProductInfo.MutexName.Should().StartWith(@"Local\InLay-")
            .And.Contain(ProductInfo.MutexId.ToString("N"));
        ProductInfo.ActivationEventName.Should().StartWith(@"Local\InLay-activate-")
            .And.Contain(ProductInfo.ActivationEventId.ToString("N"));
    }

    [Fact]
    public void Kernel_object_names_are_distinct()
    {
        ProductInfo.MutexName.Should().NotBe(ProductInfo.ActivationEventName);
    }
}
