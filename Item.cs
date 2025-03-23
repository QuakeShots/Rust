// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Libraries.Item
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Oxide.Game.Rust.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries
{
    public class Item : Oxide.Core.Libraries.Library
    {
        internal static readonly RustCovalenceProvider Covalence = RustCovalenceProvider.Instance;

        public static global::Item GetItem(int itemId) => ItemManager.CreateByItemID(itemId);
    }
}
