// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Libraries.Player
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Network;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Game.Rust.Libraries
{
    public class Player : Oxide.Core.Libraries.Library
    {
        private static readonly string ipPattern = ":{1}[0-9]{1}\\d*";
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();

        public CultureInfo Language(BasePlayer player)
        {
            try
            {
                return CultureInfo.GetCultureInfo(player.net.connection.info.GetString("global.language", "en"));
            }
            catch (CultureNotFoundException ex)
            {
                return CultureInfo.GetCultureInfo("en");
            }
        }

        public string Address(Connection connection) => Regex.Replace(connection.ipaddress, Player.ipPattern, "");

        public string Address(BasePlayer player) => player?.net?.connection == null ? (string)null : this.Address(player.net.connection);

        public int Ping(Connection connection) => Net.sv.GetAveragePing(connection);

        public int Ping(BasePlayer player) => this.Ping(player.net.connection);

        public bool IsAdmin(ulong id) => ServerUsers.Is(id, ServerUsers.UserGroup.Owner) || DeveloperList.Contains(id);

        public bool IsAdmin(string id) => this.IsAdmin(Convert.ToUInt64(id));

        public bool IsAdmin(BasePlayer player) => this.IsAdmin((ulong)player.userID);

        public bool IsBanned(ulong id) => ServerUsers.Is(id, ServerUsers.UserGroup.Banned);

        public bool IsBanned(string id) => this.IsBanned(Convert.ToUInt64(id));

        public bool IsBanned(BasePlayer player) => this.IsBanned((ulong)player.userID);

        public bool IsConnected(BasePlayer player) => player.IsConnected;

        public bool IsSleeping(ulong id) => (bool)(UnityEngine.Object)BasePlayer.FindSleeping(id);

        public bool IsSleeping(string id) => this.IsSleeping(Convert.ToUInt64(id));

        public bool IsSleeping(BasePlayer player) => this.IsSleeping((ulong)player.userID);

        public void Ban(ulong id, string reason = "")
        {
            if (this.IsBanned(id))
                return;
            BasePlayer byId = this.FindById(id);
            ServerUsers.Set(id, ServerUsers.UserGroup.Banned, byId?.displayName ?? "Unknown", reason);
            ServerUsers.Save();
            if (!((UnityEngine.Object)byId != (UnityEngine.Object)null) || !this.IsConnected(byId))
                return;
            this.Kick(byId, reason);
        }

        public void Ban(string id, string reason = "") => this.Ban(Convert.ToUInt64(id), reason);

        public void Ban(BasePlayer player, string reason = "") => this.Ban(player.UserIDString, reason);

        public void Heal(BasePlayer player, float amount) => player.Heal(amount);

        public void Hurt(BasePlayer player, float amount) => player.Hurt(amount);

        public void Kick(BasePlayer player, string reason = "") => player.Kick(reason);

        public void Kill(BasePlayer player) => player.Die((HitInfo)null);

        public void Rename(BasePlayer player, string name)
        {
            name = string.IsNullOrEmpty(name.Trim()) ? player.displayName : name;
            SingletonComponent<ServerMgr>.Instance.persistance.SetPlayerName((ulong)player.userID, name);
            player.net.connection.username = name;
            player.displayName = name;
            player._name = name;
            player.SendNetworkUpdateImmediate();
            player.IPlayer.Name = name;
            this.permission.UpdateNickname(player.UserIDString, name);
            if (player.net.group == BaseNetworkable.LimboNetworkGroup)
                return;
            List<Connection> connections = Facepunch.Pool.Get<List<Connection>>();
            for (int index = 0; index < Net.sv.connections.Count; ++index)
            {
                Connection connection = Net.sv.connections[index];
                if (connection.connected && connection.isAuthenticated && connection.player is BasePlayer && (UnityEngine.Object)connection.player != (UnityEngine.Object)player)
                    connections.Add(connection);
            }
            player.OnNetworkSubscribersLeave(connections);
            Facepunch.Pool.FreeUnmanaged<Connection>(ref connections);
            if (player.limitNetworking)
                return;
            player.syncPosition = false;
            player._limitedNetworking = true;
            Interface.Oxide.NextTick((Action)(() =>
            {
                player.syncPosition = true;
                player._limitedNetworking = false;
                player.UpdateNetworkGroup();
                player.SendNetworkUpdate();
            }));
        }

        public void Teleport(BasePlayer player, Vector3 destination)
        {
            if (!player.IsAlive())
                return;
            if (player.IsSpectating())
                return;
            try
            {
                player.EnsureDismounted();
                player.SetParent((BaseEntity)null, true, true);
                player.SetServerFall(true);
                player.MovePosition(destination);
                player.ClientRPC<Vector3>(RpcTarget.Player("ForcePositionTo", player), destination);
            }
            finally
            {
                player.SetServerFall(false);
            }
        }

        public void Teleport(BasePlayer player, BasePlayer target) => this.Teleport(player, this.Position(target));

        public void Teleport(BasePlayer player, float x, float y, float z) => this.Teleport(player, new Vector3(x, y, z));

        public void Unban(ulong id)
        {
            if (!this.IsBanned(id))
                return;
            ServerUsers.Remove(id);
            ServerUsers.Save();
        }

        public void Unban(string id) => this.Unban(Convert.ToUInt64(id));

        public void Unban(BasePlayer player) => this.Unban((ulong)player.userID);

        public Vector3 Position(BasePlayer player) => player.transform.position;

        public BasePlayer Find(string nameOrIdOrIp)
        {
            foreach (BasePlayer player in this.Players)
            {
                if (nameOrIdOrIp.Equals(player.displayName, StringComparison.OrdinalIgnoreCase) || nameOrIdOrIp.Equals(player.UserIDString) || nameOrIdOrIp.Equals(player.net.connection.ipaddress))
                    return player;
            }
            return (BasePlayer)null;
        }

        public BasePlayer FindById(string id)
        {
            foreach (BasePlayer player in this.Players)
            {
                if (id.Equals(player.UserIDString))
                    return player;
            }
            return (BasePlayer)null;
        }

        public BasePlayer FindById(ulong id)
        {
            foreach (BasePlayer player in this.Players)
            {
                if (id.Equals((ulong)player.userID))
                    return player;
            }
            return (BasePlayer)null;
        }

        public ListHashSet<BasePlayer> Players => BasePlayer.activePlayerList;

        public ListHashSet<BasePlayer> Sleepers => BasePlayer.sleepingPlayerList;

        public void Message(
          BasePlayer player,
          string message,
          string prefix,
          ulong userId = 0,
          params object[] args)
        {
            if (string.IsNullOrEmpty(message))
                return;
            message = args.Length != 0 ? string.Format(Formatter.ToUnity(message), args) : Formatter.ToUnity(message);
            string str = prefix != null ? prefix + " " + message : message;
            if (Interface.CallHook("OnMessagePlayer", (object)str, (object)player, (object)userId) != null)
                return;
            player.SendConsoleCommand("chat.add", (object)2, (object)userId, (object)str);
        }

        public void Message(BasePlayer player, string message, ulong userId = 0) => this.Message(player, message, (string)null, userId);

        public void Reply(
          BasePlayer player,
          string message,
          string prefix,
          ulong userId = 0,
          params object[] args)
        {
            this.Message(player, message, prefix, userId, args);
        }

        public void Reply(BasePlayer player, string message, ulong userId = 0) => this.Message(player, message, (string)null, userId);

        public void Command(BasePlayer player, string command, params object[] args) => player.SendConsoleCommand(command, args);

        public void DropItem(BasePlayer player, int itemId)
        {
            Vector3 position = player.transform.position;
            PlayerInventory playerInventory = this.Inventory(player);
            for (int slot1 = 0; slot1 < playerInventory.containerMain.capacity; ++slot1)
            {
                global::Item slot2 = playerInventory.containerMain.GetSlot(slot1);
                if (slot2.info.itemid == itemId)
                    slot2.Drop(position + new Vector3(0.0f, 1f, 0.0f) + position / 2f, (position + new Vector3(0.0f, 0.2f, 0.0f)) * 8f);
            }
            for (int slot3 = 0; slot3 < playerInventory.containerBelt.capacity; ++slot3)
            {
                global::Item slot4 = playerInventory.containerBelt.GetSlot(slot3);
                if (slot4.info.itemid == itemId)
                    slot4.Drop(position + new Vector3(0.0f, 1f, 0.0f) + position / 2f, (position + new Vector3(0.0f, 0.2f, 0.0f)) * 8f);
            }
            for (int slot5 = 0; slot5 < playerInventory.containerWear.capacity; ++slot5)
            {
                global::Item slot6 = playerInventory.containerWear.GetSlot(slot5);
                if (slot6.info.itemid == itemId)
                    slot6.Drop(position + new Vector3(0.0f, 1f, 0.0f) + position / 2f, (position + new Vector3(0.0f, 0.2f, 0.0f)) * 8f);
            }
        }

        public void DropItem(BasePlayer player, global::Item item)
        {
            Vector3 position = player.transform.position;
            PlayerInventory playerInventory = this.Inventory(player);
            for (int slot1 = 0; slot1 < playerInventory.containerMain.capacity; ++slot1)
            {
                global::Item slot2 = playerInventory.containerMain.GetSlot(slot1);
                if (slot2 == item)
                    slot2.Drop(position + new Vector3(0.0f, 1f, 0.0f) + position / 2f, (position + new Vector3(0.0f, 0.2f, 0.0f)) * 8f);
            }
            for (int slot3 = 0; slot3 < playerInventory.containerBelt.capacity; ++slot3)
            {
                global::Item slot4 = playerInventory.containerBelt.GetSlot(slot3);
                if (slot4 == item)
                    slot4.Drop(position + new Vector3(0.0f, 1f, 0.0f) + position / 2f, (position + new Vector3(0.0f, 0.2f, 0.0f)) * 8f);
            }
            for (int slot5 = 0; slot5 < playerInventory.containerWear.capacity; ++slot5)
            {
                global::Item slot6 = playerInventory.containerWear.GetSlot(slot5);
                if (slot6 == item)
                    slot6.Drop(position + new Vector3(0.0f, 1f, 0.0f) + position / 2f, (position + new Vector3(0.0f, 0.2f, 0.0f)) * 8f);
            }
        }

        public void GiveItem(BasePlayer player, int itemId, int quantity = 1) => this.GiveItem(player, Item.GetItem(itemId), quantity);

        public void GiveItem(BasePlayer player, global::Item item, int quantity = 1) => player.inventory.GiveItem(ItemManager.CreateByItemID(item.info.itemid, quantity), (ItemContainer)null);

        public PlayerInventory Inventory(BasePlayer player) => player.inventory;

        public void ClearInventory(BasePlayer player) => this.Inventory(player)?.Strip();

        public void ResetInventory(BasePlayer player)
        {
            PlayerInventory playerInventory = this.Inventory(player);
            if (!((UnityEngine.Object)playerInventory != (UnityEngine.Object)null))
                return;
            playerInventory.DoDestroy();
            playerInventory.ServerInit(player);
        }
    }
}
