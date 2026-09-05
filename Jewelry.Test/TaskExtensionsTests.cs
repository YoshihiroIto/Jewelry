using System;
using System.Threading.Tasks;
using Xunit;

namespace Jewelry.Test;

public class TaskExtensionsTests
{
    [Fact]
    public async Task WithTimeoutReturnsResultWhenTaskCompletesBeforeTimeout()
    {
        // Arrange
        var task = Task.FromResult("result");

        // Act
        var result = await task.WithTimeout(TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task WithTimeoutThrowsWhenTaskDoesNotCompleteBeforeTimeout()
    {
        // Arrange
        var completion = new TaskCompletionSource<string>();

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(
            () => completion.Task.WithTimeout(TimeSpan.FromMilliseconds(100)));
    }
}
