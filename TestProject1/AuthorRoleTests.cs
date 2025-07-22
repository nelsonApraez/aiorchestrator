using Integration.Models;

namespace Integration.Tests
{
    public class AuthorRoleTests
    {
        [Fact]
        public void Constructor_ShouldSetLabel()
        {
            // Arrange
            var label = "test";

            // Act
            var authorRole = new AuthorRole(label);

            // Assert
            Assert.Equal(label, authorRole.Label);
        }

        [Fact]
        public void Equals_ShouldReturnTrueForEquivalentLabels()
        {
            // Arrange
            var role1 = new AuthorRole("test");
            var role2 = new AuthorRole("TEST");

            // Act
            var result = role1.Equals(role2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Equals_ShouldReturnFalseForDifferentLabels()
        {
            // Arrange
            var role1 = new AuthorRole("test1");
            var role2 = new AuthorRole("test2");

            // Act
            var result = role1.Equals(role2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetHashCode_ShouldReturnSameHashCodeForEquivalentLabels()
        {
            // Arrange
            var role1 = new AuthorRole("test");
            var role2 = new AuthorRole("TEST");

            // Act
            var hashCode1 = role1.GetHashCode();
            var hashCode2 = role2.GetHashCode();

            // Assert
            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public void ToString_ShouldReturnLabel()
        {
            // Arrange
            var label = "test";
            var authorRole = new AuthorRole(label);

            // Act
            var result = authorRole.ToString();

            // Assert
            Assert.Equal(label, result);
        }

        [Fact]
        public void OperatorEquals_ShouldReturnTrueForEquivalentLabels()
        {
            // Arrange
            var role1 = new AuthorRole("test");
            var role2 = new AuthorRole("TEST");

            // Act
            var result = role1 == role2;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void OperatorNotEquals_ShouldReturnFalseForEquivalentLabels()
        {
            // Arrange
            var role1 = new AuthorRole("test");
            var role2 = new AuthorRole("TEST");

            // Act
            var result = role1 != role2;

            // Assert
            Assert.False(result);
        }
    }
}
