// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Cui.CuiButtonComponent
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine.UI;

namespace Oxide.Game.Rust.Cui
{
    public class CuiButtonComponent : ICuiComponent, ICuiColor
    {
        public string Type => "UnityEngine.UI.Button";

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("close")]
        public string Close { get; set; }

        [JsonProperty("sprite")]
        public string Sprite { get; set; }

        [JsonProperty("material")]
        public string Material { get; set; }

        public string Color { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("imagetype")]
        public Image.Type ImageType { get; set; }

        [JsonProperty("fadeIn")]
        public float FadeIn { get; set; }
    }
}
