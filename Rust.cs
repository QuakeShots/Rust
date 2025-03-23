// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Libraries.Rust
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Network;
using Oxide.Core.Libraries;
using ProtoBuf;
using System;
using System.Reflection;

namespace Oxide.Game.Rust.Libraries
{
    public class Rust : Oxide.Core.Libraries.Library
    {
        internal readonly Player Player = new Player();
        internal readonly Server Server = new Server();

        public override bool IsGlobal => false;

        [LibraryFunction("PrivateBindingFlag")]
        public BindingFlags PrivateBindingFlag() => BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [LibraryFunction("QuoteSafe")]
        public string QuoteSafe(string str) => str.Quote();

        [LibraryFunction("BroadcastChat")]
        public void BroadcastChat(string name, string message = null, string userId = "0") => this.Server.Broadcast(message, name, Convert.ToUInt64(userId));

        [LibraryFunction("SendChatMessage")]
        public void SendChatMessage(BasePlayer player, string name, string message = null, string userId = "0") => this.Player.Message(player, message, name, Convert.ToUInt64(userId));

        [LibraryFunction("RunClientCommand")]
        public void RunClientCommand(BasePlayer player, string command, params object[] args) => this.Player.Command(player, command, args);

        [LibraryFunction("RunServerCommand")]
        public void RunServerCommand(string command, params object[] args) => this.Server.Command(command, args);

        [LibraryFunction("UserIDFromConnection")]
        public string UserIDFromConnection(Connection connection) => connection.userid.ToString();

        [LibraryFunction("UserIDsFromBuildingPrivilege")]
        public Array UserIDsFromBuildingPrivlidge(BuildingPrivlidge priv) => (Array)priv.authorizedPlayers.Select<PlayerNameID, string>((Func<PlayerNameID, string>)(eid => eid.userid.ToString())).ToArray<string>();

        [LibraryFunction("UserIDFromPlayer")]
        public string UserIDFromPlayer(BasePlayer player) => player.UserIDString;

        [LibraryFunction("OwnerIDFromEntity")]
        public string OwnerIDFromEntity(BaseEntity entity) => entity.OwnerID.ToString();

        [LibraryFunction("FindPlayer")]
        public BasePlayer FindPlayer(string nameOrIdOrIp) => this.Player.Find(nameOrIdOrIp);

        [LibraryFunction("FindPlayerByName")]
        public BasePlayer FindPlayerByName(string name) => this.Player.Find(name);

        [LibraryFunction("FindPlayerById")]
        public BasePlayer FindPlayerById(ulong id) => this.Player.FindById(id);

        [LibraryFunction("FindPlayerByIdString")]
        public BasePlayer FindPlayerByIdString(string id) => this.Player.FindById(id);

        [LibraryFunction("ForcePlayerPosition")]
        public void ForcePlayerPosition(BasePlayer player, float x, float y, float z) => this.Player.Teleport(player, x, y, z);
    }
}
