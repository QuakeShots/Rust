// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Cui.CuiRawImageComponent
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Newtonsoft.Json;

namespace Oxide.Game.Rust.Cui
{
    public class CuiRawImageComponent : ICuiComponent, ICuiColor
    {
        public string Type => "UnityEngine.UI.RawImage";

        [JsonProperty("sprite")]
        public string Sprite { get; set; }

        public string Color { get; set; }

        [JsonProperty("material")]
        public string Material { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("png")]
        public string Png { get; set; }

        [JsonProperty("steamid")]
        public string SteamId { get; set; }

        [JsonProperty("fadeIn")]
        public float FadeIn { get; set; }
    }
}
