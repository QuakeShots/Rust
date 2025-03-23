// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Cui.CuiElement
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Game.Rust.Cui
{
    public class CuiElement
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("parent")]
        public string Parent { get; set; }

        [JsonProperty("destroyUi", NullValueHandling = NullValueHandling.Ignore)]
        public string DestroyUi { get; set; }

        [JsonProperty("components")]
        public List<ICuiComponent> Components { get; } = new List<ICuiComponent>();

        [JsonProperty("fadeOut")]
        public float FadeOut { get; set; }

        [JsonProperty("update", NullValueHandling = NullValueHandling.Ignore)]
        public bool Update { get; set; }
    }
}
