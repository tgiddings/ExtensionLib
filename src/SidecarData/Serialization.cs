using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Com.DipoleCat.ExtensionLib
{
    /// <summary>
    /// Wraps an arbitrary (serializable) object, along with the sidecar ID of the object and the reference ID.
    /// Contains a <see cref="{SidecarDataWrapper}"/>, the functions of which cannot be directly incorporated
    /// into SidecarSerializable because SidecarSerializable must be used for polymorphic (de)serialization,
    /// and thus cannot implement <see cref="{IXmlSerializable}"/>
    /// </summary>
    /// <remarks>
    /// Users of ExtensionLib likely do not need to use this type. It is public primarily because XmlSerializer
    /// uses codegen which requires the serialized type to be public
    /// </remarks>
    [XmlType("ExtensionLibSidecar")]
    public class SidecarSerializable(
        NamespacedId sidecarId,
        long referenceId,
        object? data) : ThingSaveData
    {
        [XmlAttribute("sidecar_id")]
        public string SidecarId { get; set; } = sidecarId.ToString();

        [XmlAttribute("reference_id")]
        public long AttachedReferenceId { get; set; } = referenceId;

        [XmlElement("Data")]
        public SidecarDataWrapper DataWrapper { get; set; } = new SidecarDataWrapper(data);

        public SidecarSerializable(): this(NamespacedId.EMPTY,IReferencable.INVALID,null){}
    }

    /// <summary>
    /// Wraps an arbitrary (serializable) object, with custom XML serialization to allow types which implement
    /// <see cref="{IXmlSerializable}"/>. The default polymorphic serialization and deserialization cannot handle those.
    /// </summary>
    /// <remarks>
    /// Users of ExtensionLib likely do not need to use this type. It is public primarily because XmlSerializer
    /// uses codegen which requires the serialized type to be public
    /// </remarks>
    public struct SidecarDataWrapper(object? data) : IXmlSerializable
    {
        private static readonly string TypeAttributeName = "type";
        public Type Type { get; private set; } = data?.GetType() ?? typeof(object);

        private object? _data = data;
        
        public object? Data {
            readonly get => _data;
            set {
                this.Type = value?.GetType() ?? typeof(object);
                _data = value;
            }
        }

        public readonly XmlSchema GetSchema()
        {
            return null!;
        }

        public void ReadXml(XmlReader reader)
        {
            string? typeString = reader.GetAttribute(TypeAttributeName);

            if(typeString == null){
                Debug.LogError($"Deserializing SidecarData: missing attribute \"{TypeAttributeName}\"");
                reader.ReadEndElement();
                return;
            }

            var type = Type.GetType(typeString);

            //finish reading the <Sidecar> tag, and move into inner tag(s)
            reader.Read();

            try{
                //this XmlSerializer constructor caches internally, so no caching is needed by us
                var data = new XmlSerializer(type).Deserialize(reader);
                Data = data;
                reader.ReadEndElement();
            }
            catch (InvalidOperationException e){
                Debug.LogError($"Deserializing SidecarData: exception deserializing inner data");
                Debug.LogException(e);
                reader.ReadEndElement();
                return; //default-constructed SidecarData is a valid, empty SidecarData
            }
        }

        public readonly void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(TypeAttributeName,this.Type.FullName);
            //this XmlSerializer constructor caches internally, so no caching is needed by us
            new XmlSerializer(this.Type).Serialize(writer,Data);
        }

        public override readonly string ToString()
        {
            return Data?.ToString()??"null";
        }
    }
}