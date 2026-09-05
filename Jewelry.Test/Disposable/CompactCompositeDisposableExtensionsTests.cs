using Jewelry.Disposable;
using Xunit;

namespace Jewelry.Test.Disposable;

public class CompactCompositeDisposableExtensionsTests
{
    [Fact]
    public void Add_AddsActionToCompactCompositeDisposable()
    {
        // Arrange
        var compositeDisposable = new CompactCompositeDisposable();
        var isDisposed = false;

        // Act
        compositeDisposable.Add(DisposeAction);
        compositeDisposable.Dispose();

        // Assert
        Assert.True(isDisposed);

        return;

        void DisposeAction() => isDisposed = true;
    }
}
