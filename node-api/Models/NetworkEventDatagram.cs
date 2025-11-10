using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// Base class for all packet network event datagrams.
/// Supports multiple transport protocols (UDP, HTTP, etc.) and uses JSON polymorphism
/// with the "@type" discriminator field to deserialize to specific event types.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "@type")]
[JsonDerivedType(typeof(L2Trace), "L2Trace")]
[JsonDerivedType(typeof(NodeUpEvent), "NodeUpEvent")]
[JsonDerivedType(typeof(NodeDownEvent), "NodeDownEvent")]
[JsonDerivedType(typeof(NodeStatusReportEvent), "NodeStatus")]
[JsonDerivedType(typeof(LinkUpEvent), "LinkUpEvent")]
[JsonDerivedType(typeof(LinkDisconnectionEvent), "LinkDownEvent")]
[JsonDerivedType(typeof(LinkStatus), "LinkStatus")]
[JsonDerivedType(typeof(CircuitUpEvent), "CircuitUpEvent")]
[JsonDerivedType(typeof(CircuitDisconnectionEvent), "CircuitDownEvent")]
[JsonDerivedType(typeof(CircuitStatus), "CircuitStatus")]
public record NetworkEventDatagram
{
    /// <summary>
    /// The event type discriminator (e.g., "NodeUpEvent", "LinkUpEvent", "L2Trace").
    /// This property returns the discriminator value based on the runtime type by
    /// looking up the matching JsonDerivedType attribute.
    /// During JSON deserialization, System.Text.Json uses the "@type" field to determine
    /// which derived type to instantiate.
    /// 
    /// Note: This property is ignored during serialization because the polymorphic
    /// serialization infrastructure automatically writes the @type field.
    /// </summary>
    [JsonIgnore]
    public string DatagramType
    {
        get
        {
            // Get all JsonDerivedType attributes on the base class
            var type = GetType();
            var attributes = Attribute.GetCustomAttributes(typeof(NetworkEventDatagram), typeof(JsonDerivedTypeAttribute));
            
            // Find the attribute that matches this instance's runtime type
            foreach (JsonDerivedTypeAttribute attr in attributes)
            {
                if (attr.DerivedType == type)
                {
                    return attr.TypeDiscriminator?.ToString() ?? string.Empty;
                }
            }
            
            // Fallback: return empty string if no matching attribute found
            return string.Empty;
        }
        init { } // Allow init for backwards compat with existing code that sets it
    }
}
