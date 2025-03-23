// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Cui.CuiHelper
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Game.Rust.Cui
{
    public static class CuiHelper
    {
        public static string ToJson(List<CuiElement> elements, bool format = false) => JsonConvert.SerializeObject((object)elements, (Formatting)(format ? 1 : 0), new JsonSerializerSettings()
        {
            DefaultValueHandling = DefaultValueHandling.Ignore
        }).Replace("\\n", "\n");

        public static List<CuiElement> FromJson(string json) => JsonConvert.DeserializeObject<List<CuiElement>>(json);

        public static string GetGuid() => Guid.NewGuid().ToString().Replace("-", string.Empty);

        public static bool AddUi(BasePlayer player, List<CuiElement> elements) => CuiHelper.AddUi(player, CuiHelper.ToJson(elements));

        public static bool AddUi(BasePlayer player, string json)
        {
            if (player?.net == null || Interface.CallHook("CanUseUI", (object)player, (object)json) != null)
                return false;
            CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("AddUI", player.net.connection), json);
            return true;
        }

        public static bool DestroyUi(BasePlayer player, string elem)
        {
            if (player?.net == null)
                return false;
            Interface.CallHook("OnDestroyUI", (object)player, (object)elem);
            CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("DestroyUI", player.net.connection), elem);
            return true;
        }

        public static void SetColor(this ICuiColor elem, Color color) => elem.Color = string.Format("{0} {1} {2} {3}", (object)color.r, (object)color.g, (object)color.b, (object)color.a);

        public static Color GetColor(this ICuiColor elem) => ColorEx.Parse(elem.Color);
    }
}
