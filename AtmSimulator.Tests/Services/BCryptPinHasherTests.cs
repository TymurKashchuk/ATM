using AtmSimulator.Services.Security;
using FluentAssertions;

namespace AtmSimulator.Tests.Services
{
    public class BCryptPinHasherTests
    {
        private readonly IPinHasher _hasher = new BCryptPinHasher();

        [Fact]
        public void Hash_GeneratesSecureNonPlaintextHash()
        {
            var pin = "1234";
            var hash = _hasher.Hash(pin);

            hash.Should().NotBeNullOrEmpty();
            hash.Should().NotBe(pin);
            hash.Should().StartWith("$2"); // BCrypt hash prefix
        }

        [Fact]
        public void Verify_CorrectPin_ReturnsTrue()
        {
            var pin = "4321";
            var hash = _hasher.Hash(pin);

            _hasher.Verify(pin, hash).Should().BeTrue();
        }

        [Fact]
        public void Verify_WrongPin_ReturnsFalse()
        {
            var pin = "4321";
            var hash = _hasher.Hash(pin);

            _hasher.Verify("9999", hash).Should().BeFalse();
        }

        [Fact]
        public void Verify_LegacyPlaintextPin_ReturnsTrueAndNeedsRehash()
        {
            var legacyPin = "1234";

            _hasher.Verify("1234", legacyPin).Should().BeTrue();
            _hasher.NeedsRehash(legacyPin).Should().BeTrue();
        }

        [Fact]
        public void NeedsRehash_HashedPin_ReturnsFalse()
        {
            var hash = _hasher.Hash("1234");

            _hasher.NeedsRehash(hash).Should().BeFalse();
        }
    }
}
