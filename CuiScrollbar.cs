// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Cui.CuiScrollbar
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Newtonsoft.Json;

namespace Oxide.Game.Rust.Cui
{
    public class CuiScrollbar
    {
        [JsonProperty("invert")]
        public bool Invert { get; set; }

        [JsonProperty("autoHide")]
        public bool AutoHide { get; set; }

        [JsonProperty("handleSprite")]
        public string HandleSprite { get; set; }

        [JsonProperty("size")]
        public float Size { get; set; }

        [JsonProperty("handleColor")]
        public string HandleColor { get; set; }

        [JsonProperty("highlightColor")]
        public string HighlightColor { get; set; }

        [JsonProperty("pressedColor")]
        public string PressedColor { get; set; }

        [JsonProperty("trackSprite")]
        public string TrackSprite { get; set; }

        [JsonProperty("trackColor")]
        public string TrackColor { get; set; }
    }
}
