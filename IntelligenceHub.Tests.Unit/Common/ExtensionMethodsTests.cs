using IntelligenceHub.Common.Extensions;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Common
{
    public class ExtensionMethodsTests
    {
        [Fact]
        public void ConvertStringToFinishReason_ValidFinishReason_ReturnsExpectedEnum()
        {
            // Arrange
            string input = ChatFinishReason.Stop.ToString();

            // Act
            var result = input.ConvertStringToFinishReason();

            // Assert
            Assert.Equal(FinishReason.Stop, result);
        }

        [Fact]
        public void ConvertStringToFinishReason_InvalidFinishReason_ReturnsNull()
        {
            // Arrange
            string input = "InvalidReason";

            // Act
            var result = input.ConvertStringToFinishReason();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ConvertStringToRole_ValidRole_ReturnsExpectedEnum()
        {
            // Arrange
            string input = ChatMessageRole.Assistant.ToString();

            // Act
            var result = input.ConvertStringToRole();

            // Assert
            Assert.Equal(Role.Assistant, result);
        }

        [Fact]
        public void ConvertStringToRole_InvalidRole_ReturnsNull()
        {
            // Arrange
            string input = "InvalidRole";

            // Act
            var result = input.ConvertStringToRole();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ToCommaSeparatedString_ValidInput_ReturnsExpectedString()
        {
            // Arrange
            var input = new List<string> { "a", "b", "c" };

            // Act
            var result = input.ToCommaSeparatedString();

            // Assert
            Assert.Equal("a,b,c", result);
        }

        [Fact]
        public void ToCommaSeparatedString_EmptyInput_ReturnsEmptyString()
        {
            // Arrange
            var input = new List<string>();

            // Act
            var result = input.ToCommaSeparatedString();

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void ToCommaSeparatedString_NullInput_ThrowsArgumentNullException()
        {
            // Arrange
            List<string> input = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => input.ToCommaSeparatedString());
        }

        [Fact]
        public void ToStringArray_ValidInput_ReturnsExpectedArray()
        {
            // Arrange
            string input = "a, b , c";

            // Act
            var result = input.ToStringArray();

            // Assert
            Assert.Equal(new string[] { "a", "b", "c" }, result);
        }

        [Fact]
        public void ToStringArray_EmptyInput_ReturnsEmptyArray()
        {
            // Arrange
            string input = "";

            // Act
            var result = input.ToStringArray();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ToStringArray_NullInput_ThrowsArgumentNullException()
        {
            // Arrange
            string input = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => input.ToStringArray());
        }
    }

}
