using System.Collections.Generic;
using NUnit.Framework;
using Com.DipoleCat.ExtensionLib;
using System.Linq;
using System;
using Assets.Scripts.Networking;
using System.IO;

public class TestRegistries{
    [Test]
    public void TestConsistency()
    {
        List<NamespacedId> test_values = new(){
            new NamespacedId("test:test1"),
            new NamespacedId("test","test2"),
            new NamespacedId("test2","test3")
        };

        IMutableRegistry registry = new SimpleRegistry();

        foreach(NamespacedId test_value in test_values){
            registry.Register(test_value);
            Assert.Throws<ArgumentException>(()=>{
                registry.Register(test_value);
            },
            $"registry allowed ID {test_value} to be registered twice");
        }

        //correct count
        Assert.AreEqual(test_values.Count,registry.Count);

        //distinct indices
        foreach ((NamespacedId id1, NamespacedId id2) in from id1 in test_values
                                                         from id2 in test_values
                                                         where id1 != id2
                                                         select (id1,id2)){
            uint? index1 = registry.GetIndex(id1);
            uint? index2 = registry.GetIndex(id2);
            Assert.NotNull(index1);
            Assert.NotNull(index2);
            Assert.AreNotEqual(index1,index2);
        }
        
        //distinct IDs
        foreach ((uint index1, uint index2) in from index1 in Enumerable.Range(0,test_values.Count)
                                               from index2 in Enumerable.Range(0,test_values.Count)
                                               where index1 != index2
                                               select ((uint)index1,(uint)index2)){
            NamespacedId? id1 = registry.GetId(index1);
            NamespacedId? id2 = registry.GetId(index2);
            Assert.NotNull(id1);
            Assert.NotNull(id2);
            Assert.AreNotEqual(id1,id2);
        }

        //Frozen copy
        var frozen_registry = registry.FrozenCopy();

        //correct count
        Assert.AreEqual(test_values.Count,frozen_registry.Count);

        //matching IDs
        foreach(uint index in Enumerable.Range(0,test_values.Count).Select(v => (uint)v))
        {
            var id1 = registry.GetId(index);
            var id2 = frozen_registry.GetId(index);
            Assert.NotNull(id1);
            Assert.NotNull(id2);
            Assert.AreEqual(id1,id2);
        }

        //matching indices
        foreach(NamespacedId id in test_values){
            var index1 = registry.GetIndex(id);
            var index2 = registry.GetIndex(id);
            Assert.NotNull(index1);
            Assert.NotNull(index2);
            Assert.AreEqual(index1,index2);
        }
    }

    [Test]
    public void TestSerialization(){
        List<NamespacedId> test_values = new(){
            new NamespacedId("test:test1"),
            new NamespacedId("test","test2"),
            new NamespacedId("test2","test3")
        };

        IMutableRegistry registry = new SimpleRegistry();

        foreach(NamespacedId test_value in test_values){
            registry.Register(test_value);
            Assert.Throws<ArgumentException>(()=>{
                registry.Register(test_value);
            },
            $"registry allowed ID {test_value} to be registered twice");
        }

        var writer = new RocketBinaryWriter(5000);
        registry.Serialize(writer);
        using var stream = new MemoryStream(writer.AsSpan().ToArray());
        var reader = new RocketBinaryReader(stream);
        IRegistry registry2 = Registry.Deserialize(reader);

        Assert.AreEqual(registry.Count,registry2.Count);
        foreach(uint index in Enumerable.Range(0,registry.Count).Select(v=>(uint)v)){
            var id1 = registry.GetId(index);
            var id2 = registry2.GetId(index);
            Assert.AreEqual(id1,id2);
        }
    }
}