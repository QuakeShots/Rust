// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Cui.CuiElementContainer
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using System.Collections.Generic;

namespace Oxide.Game.Rust.Cui
{
    public class CuiElementContainer : List<CuiElement>
    {
        public string Add(CuiButton button, string parent = "Hud", string name = null, string destroyUi = null)
        {
            if (string.IsNullOrEmpty(name))
                name = CuiHelper.GetGuid();
            this.Add(new CuiElement()
            {
                Name = name,
                Parent = parent,
                FadeOut = button.FadeOut,
                DestroyUi = destroyUi,
                Components = {
          (ICuiComponent) button.Button,
          (ICuiComponent) button.RectTransform
        }
            });
            if (!string.IsNullOrEmpty(button.Text.Text))
                this.Add(new CuiElement()
                {
                    Parent = name,
                    FadeOut = button.FadeOut,
                    Components = {
            (ICuiComponent) button.Text,
            (ICuiComponent) new CuiRectTransformComponent()
          }
                });
            return name;
        }

        public string Add(CuiLabel label, string parent = "Hud", string name = null, string destroyUi = null)
        {
            if (string.IsNullOrEmpty(name))
                name = CuiHelper.GetGuid();
            this.Add(new CuiElement()
            {
                Name = name,
                Parent = parent,
                FadeOut = label.FadeOut,
                DestroyUi = destroyUi,
                Components = {
          (ICuiComponent) label.Text,
          (ICuiComponent) label.RectTransform
        }
            });
            return name;
        }

        public string Add(CuiPanel panel, string parent = "Hud", string name = null, string destroyUi = null)
        {
            if (string.IsNullOrEmpty(name))
                name = CuiHelper.GetGuid();
            CuiElement cuiElement = new CuiElement()
            {
                Name = name,
                Parent = parent,
                FadeOut = panel.FadeOut,
                DestroyUi = destroyUi
            };
            if (panel.Image != null)
                cuiElement.Components.Add((ICuiComponent)panel.Image);
            if (panel.RawImage != null)
                cuiElement.Components.Add((ICuiComponent)panel.RawImage);
            cuiElement.Components.Add((ICuiComponent)panel.RectTransform);
            if (panel.CursorEnabled)
                cuiElement.Components.Add((ICuiComponent)new CuiNeedsCursorComponent());
            if (panel.KeyboardEnabled)
                cuiElement.Components.Add((ICuiComponent)new CuiNeedsKeyboardComponent());
            this.Add(cuiElement);
            return name;
        }

        public string ToJson() => this.ToString();

        public override string ToString() => CuiHelper.ToJson((List<CuiElement>)this);
    }
}
