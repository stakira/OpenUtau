using System;
using OpenUtau.Core.Editing;
using Xunit;

namespace OpenUtau.Core.Editing.Tests;

public class AddPhoneticHintsTests
{
    [Fact]
    public void AddPhoneticHints_Should_()
    {
        // Arrange
            var sut = new AddPhoneticHints();
            
        // Act
        var result = sut.AddPhoneticHints();

        // Assert
            Assert.NotNull(result);
    }
}