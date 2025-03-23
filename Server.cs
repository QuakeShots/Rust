// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Libraries.Server
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries
{
    public class Server : Oxide.Core.Libraries.Library
    {
        public void Broadcast(string message, string prefix, ulong userId = 0, params object[] args)
        {
            if (string.IsNullOrEmpty(message))
                return;
            message = args.Length != 0 ? string.Format(Formatter.ToUnity(message), args) : Formatter.ToUnity(message);
            string str = prefix != null ? prefix + ": " + message : message;
            ConsoleNetwork.BroadcastToAllClients("chat.add", (object)2, (object)userId, (object)str);
        }

        public void Broadcast(string message, ulong userId = 0) => this.Broadcast(message, (string)null, userId);

        public void Command(string command, params object[] args) => ConsoleSystem.Run(ConsoleSystem.Option.Server, command, args);
    }
}
