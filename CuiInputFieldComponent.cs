// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Cui.CuiInputFieldComponent
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.UI;

namespace Oxide.Game.Rust.Cui
{
    public class CuiInputFieldComponent : ICuiComponent, ICuiColor
    {
        public string Type => "UnityEngine.UI.InputField";

        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;

        [JsonProperty("fontSize")]
        public int FontSize { get; set; }

        [JsonProperty("font")]
        public string Font { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("align")]
        public TextAnchor Align { get; set; }

        public string Color { get; set; }

        [JsonProperty("characterLimit")]
        public int CharsLimit { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("password")]
        public bool IsPassword { get; set; }

        [JsonProperty("readOnly")]
        public bool ReadOnly { get; set; }

        [JsonProperty("needsKeyboard")]
        public bool NeedsKeyboard { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("lineType")]
        public InputField.LineType LineType { get; set; }

        [JsonProperty("autofocus")]
        public bool Autofocus { get; set; }

        [JsonProperty("hudMenuInput")]
        public bool HudMenuInput { get; set; }
    }
}
