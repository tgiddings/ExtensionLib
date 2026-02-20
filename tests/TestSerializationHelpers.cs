using NUnit.Framework;
using Com.DipoleCat.ExtensionLib.Networking;
using System.IO;
using Assets.Scripts.Networking;

namespace Com.DipoleCat.ExtensionLib.Tests
{
    public class TestSerializationHelpers
    {
        [Test]
        public void TestVarInt()
        {
            uint[] testValues = { 1, 2, 3, 5, 56, 127, 128, 1600, 1700, 12345, 123_456_789, 1_234_567_890, 4_234_567_890, uint.MaxValue };
            // Use the Assert class to test conditions
            foreach (uint testValue in testValues)
            {
                var writer = new RocketBinaryWriter(5);
                Serialization.WriteVarInt(testValue, writer);
                using var stream = new MemoryStream(writer.AsSpan().ToArray());
                var reader = new RocketBinaryReader(stream);
                Assert.That(Serialization.ReadVarInt(reader), Is.EqualTo(testValue));
            }
        }
        [Test]
        public void TestVarLong()
        {
            ulong[] testValues = { 1, 2, 3, 5, 56, 127, 128, 1600, 1700, 12345, 123_456_789, 1_234_567_890, 4_234_567_890, 1_234_567_890_987_654_321, ulong.MaxValue };
            // Use the Assert class to test conditions
            foreach (ulong testValue in testValues)
            {
                var writer = new RocketBinaryWriter(10);
                Serialization.WriteVarLong(testValue, writer);
                using var stream = new MemoryStream(writer.AsSpan().ToArray());
                var reader = new RocketBinaryReader(stream);
                Assert.That(Serialization.ReadVarLong(reader), Is.EqualTo(testValue));
            }
        }
    }
}
