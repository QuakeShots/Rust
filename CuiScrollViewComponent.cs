// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Cui.CuiScrollViewComponent
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine.UI;

namespace Oxide.Game.Rust.Cui
{
    public class CuiScrollViewComponent : ICuiComponent
    {
        public string Type => "UnityEngine.UI.ScrollView";

        [JsonProperty("contentTransform")]
        public CuiRectTransform ContentTransform { get; set; }

        [JsonProperty("horizontal")]
        public bool Horizontal { get; set; }

        [JsonProperty("vertical")]
        public bool Vertical { get; set; }

        [JsonProperty("movementType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ScrollRect.MovementType MovementType { get; set; }

        [JsonProperty("elasticity")]
        public float Elasticity { get; set; }

        [JsonProperty("inertia")]
        public bool Inertia { get; set; }

        [JsonProperty("decelerationRate")]
        public float DecelerationRate { get; set; }

        [JsonProperty("scrollSensitivity")]
        public float ScrollSensitivity { get; set; }

        [JsonProperty("horizontalScrollbar")]
        public CuiScrollbar HorizontalScrollbar { get; set; }

        [JsonProperty("verticalScrollbar")]
        public CuiScrollbar VerticalScrollbar { get; set; }
    }
}
