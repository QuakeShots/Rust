// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Cui.ComponentConverter
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Oxide.Game.Rust.Cui
{
    public class ComponentConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();

        public override object ReadJson(
          JsonReader reader,
          Type objectType,
          object existingValue,
          JsonSerializer serializer)
        {
            JObject jobject = JObject.Load(reader);
            string str = jobject["type"].ToString();
            if (str != null)
            {
                Type type;
                switch (str.Length)
                {
                    case 9:
                        if (str == "Countdown")
                        {
                            type = typeof(CuiCountdownComponent);
                            break;
                        }
                        goto label_26;
                    case 11:
                        if (str == "NeedsCursor")
                        {
                            type = typeof(CuiNeedsCursorComponent);
                            break;
                        }
                        goto label_26;
                    case 13:
                        switch (str[0])
                        {
                            case 'N':
                                if (str == "NeedsKeyboard")
                                {
                                    type = typeof(CuiNeedsKeyboardComponent);
                                    break;
                                }
                                goto label_26;
                            case 'R':
                                if (str == "RectTransform")
                                {
                                    type = typeof(CuiRectTransformComponent);
                                    break;
                                }
                                goto label_26;
                            default:
                                goto label_26;
                        }
                        break;
                    case 19:
                        if (str == "UnityEngine.UI.Text")
                        {
                            type = typeof(CuiTextComponent);
                            break;
                        }
                        goto label_26;
                    case 20:
                        if (str == "UnityEngine.UI.Image")
                        {
                            type = typeof(CuiImageComponent);
                            break;
                        }
                        goto label_26;
                    case 21:
                        if (str == "UnityEngine.UI.Button")
                        {
                            type = typeof(CuiButtonComponent);
                            break;
                        }
                        goto label_26;
                    case 22:
                        if (str == "UnityEngine.UI.Outline")
                        {
                            type = typeof(CuiOutlineComponent);
                            break;
                        }
                        goto label_26;
                    case 23:
                        if (str == "UnityEngine.UI.RawImage")
                        {
                            type = typeof(CuiRawImageComponent);
                            break;
                        }
                        goto label_26;
                    case 25:
                        switch (str[15])
                        {
                            case 'I':
                                if (str == "UnityEngine.UI.InputField")
                                {
                                    type = typeof(CuiInputFieldComponent);
                                    break;
                                }
                                goto label_26;
                            case 'S':
                                if (str == "UnityEngine.UI.ScrollView")
                                {
                                    type = typeof(CuiScrollViewComponent);
                                    break;
                                }
                                goto label_26;
                            default:
                                goto label_26;
                        }
                        break;
                    default:
                        goto label_26;
                }
                object instance = Activator.CreateInstance(type);
                serializer.Populate(jobject.CreateReader(), instance);
                return instance;
            }
        label_26:
            return (object)null;
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(ICuiComponent);

        public override bool CanWrite => false;
    }
}
