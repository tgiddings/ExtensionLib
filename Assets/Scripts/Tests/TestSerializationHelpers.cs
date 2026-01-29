using NUnit.Framework;
using Com.DipoleCat.ExtensionLib.Networking;
using System.IO;
using Assets.Scripts.Networking;

public class TestSerializationHelpers
{
    [Test]
    public void TestVarInt()
    {
        uint[] testValues = {1,2,3,5,56,127,128,1600,1700,12345,123_456_789,1_234_567_890,4_234_567_890,uint.MaxValue};
        // Use the Assert class to test conditions
        foreach(uint testValue in testValues){
            using MemoryStream stream = new(5);
            var writer = new RocketBinaryWriter(stream);
            Serialization.WriteVarInt(testValue, writer);
            stream.Seek(0,SeekOrigin.Begin);
            var reader = new RocketBinaryReader(stream);
            Assert.AreEqual(testValue, Serialization.ReadVarInt(reader));
        }
    }
    [Test]
    public void TestVarLong()
    {
        ulong[] testValues = {1,2,3,5,56,127,128,1600,1700,12345,123_456_789,1_234_567_890,4_234_567_890,1_234_567_890_987_654_321,ulong.MaxValue};
        // Use the Assert class to test conditions
        foreach(ulong testValue in testValues){
            using MemoryStream stream = new(10);
            var writer = new RocketBinaryWriter(stream);
            Serialization.WriteVarLong(testValue, writer);
            stream.Seek(0,SeekOrigin.Begin);
            var reader = new RocketBinaryReader(stream);
            Assert.AreEqual(testValue, Serialization.ReadVarLong(reader));
        }
    }
}
