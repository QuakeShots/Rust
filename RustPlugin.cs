// Decompiled with JetBrains decompiler
// Type: Oxide.Plugins.RustPlugin
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Libraries;
using System;
using System.Reflection;
using UnityEngine;

namespace Oxide.Plugins
{
    public abstract class RustPlugin : CSharpPlugin
    {
        protected Oxide.Game.Rust.Libraries.Command cmd = Interface.Oxide.GetLibrary<Oxide.Game.Rust.Libraries.Command>();
        protected Oxide.Game.Rust.Libraries.Rust rust = Interface.Oxide.GetLibrary<Oxide.Game.Rust.Libraries.Rust>();
        protected Oxide.Game.Rust.Libraries.Item Item = Interface.Oxide.GetLibrary<Oxide.Game.Rust.Libraries.Item>();
        protected Player Player = Interface.Oxide.GetLibrary<Player>();
        protected Oxide.Game.Rust.Libraries.Server Server = Interface.Oxide.GetLibrary<Oxide.Game.Rust.Libraries.Server>();

        public override void HandleAddedToManager(PluginManager manager)
        {
            foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (field.GetCustomAttributes(typeof(OnlinePlayersAttribute), true).Length != 0)
                {
                    CSharpPlugin.PluginFieldInfo pluginFieldInfo = new CSharpPlugin.PluginFieldInfo((Plugin)this, field);
                    if (pluginFieldInfo.GenericArguments.Length != 2 || pluginFieldInfo.GenericArguments[0] != typeof(BasePlayer))
                        this.Puts("The " + field.Name + " field is not a Hash with a BasePlayer key! (online players will not be tracked)");
                    else if (!pluginFieldInfo.LookupMethod("Add", pluginFieldInfo.GenericArguments))
                        this.Puts("The " + field.Name + " field does not support adding BasePlayer keys! (online players will not be tracked)");
                    else if (!pluginFieldInfo.LookupMethod("Remove", typeof(BasePlayer)))
                        this.Puts("The " + field.Name + " field does not support removing BasePlayer keys! (online players will not be tracked)");
                    else if (pluginFieldInfo.GenericArguments[1].GetField("Player") == (FieldInfo)null)
                        this.Puts("The " + pluginFieldInfo.GenericArguments[1].Name + " class does not have a public Player field! (online players will not be tracked)");
                    else if (!pluginFieldInfo.HasValidConstructor(typeof(BasePlayer)))
                        this.Puts("The " + field.Name + " field is using a class which contains no valid constructor (online players will not be tracked)");
                    else
                        this.onlinePlayerFields.Add(pluginFieldInfo);
                }
            }
            foreach (MethodInfo method in this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                object[] customAttributes1 = method.GetCustomAttributes(typeof(ConsoleCommandAttribute), true);
                if (customAttributes1.Length != 0)
                {
                    if (customAttributes1[0] is ConsoleCommandAttribute commandAttribute1)
                        this.cmd.AddConsoleCommand(commandAttribute1.Command, (Plugin)this, method.Name);
                }
                else
                {
                    object[] customAttributes2 = method.GetCustomAttributes(typeof(ChatCommandAttribute), true);
                    if (customAttributes2.Length != 0 && customAttributes2[0] is ChatCommandAttribute commandAttribute2)
                        this.cmd.AddChatCommand(commandAttribute2.Command, (Plugin)this, method.Name);
                }
            }
            if (this.onlinePlayerFields.Count > 0)
            {
                foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
                    this.AddOnlinePlayer(activePlayer);
            }
            base.HandleAddedToManager(manager);
        }

        [HookMethod("OnPlayerConnected")]
        private void base_OnPlayerConnected(BasePlayer player) => this.AddOnlinePlayer(player);

        [HookMethod("OnPlayerDisconnected")]
        private void base_OnPlayerDisconnected(BasePlayer player, string reason) => this.NextTick((Action)(() =>
        {
            foreach (CSharpPlugin.PluginFieldInfo onlinePlayerField in this.onlinePlayerFields)
                onlinePlayerField.Call("Remove", (object)player);
        }));

        private void AddOnlinePlayer(BasePlayer player)
        {
            foreach (CSharpPlugin.PluginFieldInfo onlinePlayerField in this.onlinePlayerFields)
            {
                System.Type genericArgument = onlinePlayerField.GenericArguments[1];
                object instance;
                if (!(genericArgument.GetConstructor(new System.Type[1]
                {
          typeof (BasePlayer)
                }) == (ConstructorInfo)null))
                    instance = Activator.CreateInstance(genericArgument, (object)player);
                else
                    instance = Activator.CreateInstance(genericArgument);
                object obj = instance;
                genericArgument.GetField("Player").SetValue(obj, (object)player);
                onlinePlayerField.Call("Add", (object)player, obj);
            }
        }

        protected void PrintToConsole(BasePlayer player, string format, params object[] args)
        {
            if (player?.net == null)
                return;
            player.SendConsoleCommand("echo " + (args.Length != 0 ? string.Format(format, args) : format));
        }

        protected void PrintToConsole(string format, params object[] args)
        {
            if (BasePlayer.activePlayerList.Count < 1)
                return;
            ConsoleNetwork.BroadcastToAllClients("echo " + (args.Length != 0 ? string.Format(format, args) : format));
        }

        protected void PrintToChat(BasePlayer player, string format, params object[] args)
        {
            if (player?.net == null)
                return;
            player.SendConsoleCommand("chat.add", (object)2, (object)0, args.Length != 0 ? (object)string.Format(format, args) : (object)format);
        }

        protected void PrintToChat(string format, params object[] args)
        {
            if (BasePlayer.activePlayerList.Count < 1)
                return;
            ConsoleNetwork.BroadcastToAllClients("chat.add", (object)2, (object)0, args.Length != 0 ? (object)string.Format(format, args) : (object)format);
        }

        protected void SendReply(ConsoleSystem.Arg arg, string format, params object[] args)
        {
            BasePlayer player = arg.Connection?.player as BasePlayer;
            string format1 = args.Length != 0 ? string.Format(format, args) : format;
            if (player?.net != null)
                player.SendConsoleCommand("echo " + format1);
            else
                this.Puts(format1);
        }

        protected void SendReply(BasePlayer player, string format, params object[] args) => this.PrintToChat(player, format, args);

        protected void SendWarning(ConsoleSystem.Arg arg, string format, params object[] args)
        {
            BasePlayer player = arg.Connection?.player as BasePlayer;
            string message = args.Length != 0 ? string.Format(format, args) : format;
            if (player?.net != null)
                player.SendConsoleCommand("echo " + message);
            else
                Debug.LogWarning((object)message);
        }

        protected void SendError(ConsoleSystem.Arg arg, string format, params object[] args)
        {
            BasePlayer player = arg.Connection?.player as BasePlayer;
            string message = args.Length != 0 ? string.Format(format, args) : format;
            if (player?.net != null)
                player.SendConsoleCommand("echo " + message);
            else
                Debug.LogError((object)message);
        }

        protected void ForcePlayerPosition(BasePlayer player, Vector3 destination)
        {
            player.MovePosition(destination);
            if (!player.IsSpectating() || (double)Vector3.Distance(player.transform.position, destination) > 25.0)
                player.ClientRPC<Vector3>(RpcTarget.Player("ForcePositionTo", player), destination);
            else
                player.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
        }
    }
}
