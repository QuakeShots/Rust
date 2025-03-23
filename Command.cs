// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Libraries.Command
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Libraries.Covalence;
using System;
using System.Collections.Generic;

namespace Oxide.Game.Rust.Libraries
{
    public class Command : Oxide.Core.Libraries.Library
    {
        internal readonly Dictionary<string, Command.ConsoleCommand> consoleCommands;
        internal readonly Dictionary<string, Command.ChatCommand> chatCommands;
        private readonly Dictionary<Plugin, Oxide.Core.Event.Callback<Plugin, PluginManager>> pluginRemovedFromManager;

        public Command()
        {
            this.consoleCommands = new Dictionary<string, Command.ConsoleCommand>();
            this.chatCommands = new Dictionary<string, Command.ChatCommand>();
            this.pluginRemovedFromManager = new Dictionary<Plugin, Oxide.Core.Event.Callback<Plugin, PluginManager>>();
        }

        [LibraryFunction("AddChatCommand")]
        public void AddChatCommand(string name, Plugin plugin, string callback) => this.AddChatCommand(name, plugin, (Action<BasePlayer, string, string[]>)((player, command, args) => plugin.CallHook(callback, (object)player, (object)command, (object)args)));

        public void AddChatCommand(
          string command,
          Plugin plugin,
          Action<BasePlayer, string, string[]> callback)
        {
            string lowerInvariant = command.ToLowerInvariant();
            if (!this.CanOverrideCommand(command, "chat"))
            {
                Interface.Oxide.LogError("{0} tried to register command '{1}', this command already exists and cannot be overridden!", (object)(plugin?.Name ?? "An unknown plugin"), (object)lowerInvariant);
            }
            else
            {
                Command.ChatCommand chatCommand;
                if (this.chatCommands.TryGetValue(lowerInvariant, out chatCommand))
                {
                    string str = chatCommand.Plugin?.Name ?? "an unknown plugin";
                    Interface.Oxide.LogWarning((plugin?.Name ?? "An unknown plugin") + " has replaced the '" + lowerInvariant + "' chat command previously registered by " + str);
                }
                RustCommandSystem.RegisteredCommand registeredCommand;
                if (RustCore.Covalence.CommandSystem.registeredCommands.TryGetValue(lowerInvariant, out registeredCommand))
                {
                    string str = registeredCommand.Source?.Name ?? "an unknown plugin";
                    Interface.Oxide.LogWarning((plugin?.Name ?? "An unknown plugin") + " has replaced the '" + lowerInvariant + "' command previously registered by " + str);
                    RustCore.Covalence.CommandSystem.UnregisterCommand(lowerInvariant, registeredCommand.Source);
                }
                chatCommand = new Command.ChatCommand(lowerInvariant, plugin, callback);
                this.chatCommands[lowerInvariant] = chatCommand;
                if (plugin == null || this.pluginRemovedFromManager.ContainsKey(plugin))
                    return;
                this.pluginRemovedFromManager[plugin] = plugin.OnRemovedFromManager.Add(new Action<Plugin, PluginManager>(this.plugin_OnRemovedFromManager));
            }
        }

        [LibraryFunction("AddConsoleCommand")]
        public void AddConsoleCommand(string command, Plugin plugin, string callback) => this.AddConsoleCommand(command, plugin, (Func<ConsoleSystem.Arg, bool>)(arg => plugin.CallHook(callback, (object)arg) != null));

        public void AddConsoleCommand(
          string command,
          Plugin plugin,
          Func<ConsoleSystem.Arg, bool> callback)
        {
            if (plugin != null && !this.pluginRemovedFromManager.ContainsKey(plugin))
                this.pluginRemovedFromManager[plugin] = plugin.OnRemovedFromManager.Add(new Action<Plugin, PluginManager>(this.plugin_OnRemovedFromManager));
            string[] source = command.Split('.');
            string str1 = source.Length >= 2 ? source[0].Trim() : "global";
            string key = source.Length >= 2 ? string.Join(".", ((IEnumerable<string>)source).Skip<string>(1).ToArray<string>()) : source[0].Trim();
            string str2 = str1 + "." + key;
            Command.ConsoleCommand consoleCommand1 = new Command.ConsoleCommand(str2);
            if (!this.CanOverrideCommand(str1 == "global" ? key : str2, "console"))
            {
                Interface.Oxide.LogError("{0} tried to register command '{1}', this command already exists and cannot be overridden!", (object)(plugin?.Name ?? "An unknown plugin"), (object)str2);
            }
            else
            {
                Command.ConsoleCommand consoleCommand2;
                if (this.consoleCommands.TryGetValue(str2, out consoleCommand2))
                {
                    if (consoleCommand2.OriginalCallback != null)
                        consoleCommand1.OriginalCallback = consoleCommand2.OriginalCallback;
                    string str3 = consoleCommand2.Callback.Plugin?.Name ?? "an unknown plugin";
                    Interface.Oxide.LogWarning((plugin?.Name ?? "An unknown plugin") + " has replaced the '" + command + "' console command previously registered by " + str3);
                    ConsoleSystem.Index.Server.Dict.Remove(consoleCommand2.RustCommand.FullName);
                    if (str1 == "global")
                        ConsoleSystem.Index.Server.GlobalDict.Remove(consoleCommand2.RustCommand.Name);
                    ConsoleSystem.Index.All = ConsoleSystem.Index.Server.Dict.Values.ToArray<ConsoleSystem.Command>();
                }
                RustCommandSystem.RegisteredCommand registeredCommand;
                if (RustCore.Covalence.CommandSystem.registeredCommands.TryGetValue(str1 == "global" ? key : str2, out registeredCommand))
                {
                    if (registeredCommand.OriginalCallback != null)
                        consoleCommand1.OriginalCallback = registeredCommand.OriginalCallback;
                    string str4 = registeredCommand.Source?.Name ?? "an unknown plugin";
                    Interface.Oxide.LogWarning((plugin?.Name ?? "An unknown plugin") + " has replaced the '" + str2 + "' command previously registered by " + str4);
                    RustCore.Covalence.CommandSystem.UnregisterCommand(str1 == "global" ? key : str2, registeredCommand.Source);
                }
                consoleCommand1.AddCallback(plugin, callback);
                ConsoleSystem.Command command1;
                if (ConsoleSystem.Index.Server.Dict.TryGetValue(str2, out command1))
                {
                    if (command1.Variable)
                    {
                        Interface.Oxide.LogError((plugin?.Name ?? "An unknown plugin") + " tried to register the " + key + " console variable as a command!");
                        return;
                    }
                    consoleCommand1.OriginalCallback = command1.Call;
                }
                ConsoleSystem.Index.Server.Dict[str2] = consoleCommand1.RustCommand;
                if (str1 == "global")
                    ConsoleSystem.Index.Server.GlobalDict[key] = consoleCommand1.RustCommand;
                ConsoleSystem.Index.All = ConsoleSystem.Index.Server.Dict.Values.ToArray<ConsoleSystem.Command>();
                this.consoleCommands[str2] = consoleCommand1;
            }
        }

        [LibraryFunction("RemoveChatCommand")]
        public void RemoveChatCommand(string command, Plugin plugin)
        {
            Command.ChatCommand command1 = this.chatCommands.Values.Where<Command.ChatCommand>((Func<Command.ChatCommand, bool>)(x => x.Plugin == plugin)).FirstOrDefault<Command.ChatCommand>((Func<Command.ChatCommand, bool>)(x => x.Name == command));
            if (command1 == null)
                return;
            this.RemoveChatCommand(command1);
        }

        [LibraryFunction("RemoveConsoleCommand")]
        public void RemoveConsoleCommand(string command, Plugin plugin)
        {
            Command.ConsoleCommand command1 = this.consoleCommands.Values.Where<Command.ConsoleCommand>((Func<Command.ConsoleCommand, bool>)(x => x.Callback.Plugin == plugin)).FirstOrDefault<Command.ConsoleCommand>((Func<Command.ConsoleCommand, bool>)(x => x.Name == command));
            if (command1 == null)
                return;
            this.RemoveConsoleCommand(command1);
        }

        private void RemoveChatCommand(Command.ChatCommand command)
        {
            if (!this.chatCommands.ContainsKey(command.Name))
                return;
            this.chatCommands.Remove(command.Name);
        }

        private void RemoveConsoleCommand(Command.ConsoleCommand command)
        {
            if (!this.consoleCommands.ContainsKey(command.Name))
                return;
            this.consoleCommands.Remove(command.Name);
            if (command.OriginalCallback != null)
            {
                ConsoleSystem.Index.Server.Dict[command.RustCommand.FullName].Call = command.OriginalCallback;
                if (!command.RustCommand.FullName.StartsWith("global."))
                    return;
                ConsoleSystem.Index.Server.GlobalDict[command.RustCommand.Name].Call = command.OriginalCallback;
            }
            else
            {
                ConsoleSystem.Index.Server.Dict.Remove(command.RustCommand.FullName);
                if (command.Name.StartsWith("global."))
                    ConsoleSystem.Index.Server.GlobalDict.Remove(command.RustCommand.Name);
                ConsoleSystem.Index.All = ConsoleSystem.Index.Server.Dict.Values.ToArray<ConsoleSystem.Command>();
            }
        }

        internal bool HandleChatCommand(BasePlayer sender, string name, string[] args)
        {
            Command.ChatCommand chatCommand;
            if (!this.chatCommands.TryGetValue(name.ToLowerInvariant(), out chatCommand))
                return false;
            chatCommand.HandleCommand(sender, name, args);
            return true;
        }

        private void plugin_OnRemovedFromManager(Plugin sender, PluginManager manager)
        {
            foreach (Command.ConsoleCommand command in this.consoleCommands.Values.Where<Command.ConsoleCommand>((Func<Command.ConsoleCommand, bool>)(c => c.Callback.Plugin == sender)).ToArray<Command.ConsoleCommand>())
                this.RemoveConsoleCommand(command);
            foreach (Command.ChatCommand command in this.chatCommands.Values.Where<Command.ChatCommand>((Func<Command.ChatCommand, bool>)(c => c.Plugin == sender)).ToArray<Command.ChatCommand>())
                this.RemoveChatCommand(command);
            Oxide.Core.Event.Callback<Plugin, PluginManager> callback;
            if (!this.pluginRemovedFromManager.TryGetValue(sender, out callback))
                return;
            callback.Remove();
            this.pluginRemovedFromManager.Remove(sender);
        }

        private bool CanOverrideCommand(string command, string type)
        {
            string[] source = command.Split('.');
            string str1 = source.Length >= 2 ? source[0].Trim() : "global";
            string str2 = source.Length >= 2 ? string.Join(".", ((IEnumerable<string>)source).Skip<string>(1).ToArray<string>()) : source[0].Trim();
            string str3 = str1 + "." + str2;
            RustCommandSystem.RegisteredCommand registeredCommand;
            if (RustCore.Covalence.CommandSystem.registeredCommands.TryGetValue(command, out registeredCommand) && registeredCommand.Source.IsCorePlugin)
                return false;
            if (type == "chat")
            {
                Command.ChatCommand chatCommand;
                if (this.chatCommands.TryGetValue(command, out chatCommand) && chatCommand.Plugin.IsCorePlugin)
                    return false;
            }
            else
            {
                Command.ConsoleCommand consoleCommand;
                if (type == "console" && this.consoleCommands.TryGetValue(str1 == "global" ? str2 : str3, out consoleCommand) && consoleCommand.Callback.Plugin.IsCorePlugin)
                    return false;
            }
            return !RustCore.RestrictedCommands.Contains<string>(command) && !RustCore.RestrictedCommands.Contains<string>(str3);
        }

        internal struct PluginCallback
        {
            public readonly Plugin Plugin;
            public readonly string Name;
            public Func<ConsoleSystem.Arg, bool> Call;

            public PluginCallback(Plugin plugin, string name)
            {
                this.Plugin = plugin;
                this.Name = name;
                this.Call = (Func<ConsoleSystem.Arg, bool>)null;
            }

            public PluginCallback(Plugin plugin, Func<ConsoleSystem.Arg, bool> callback)
            {
                this.Plugin = plugin;
                this.Call = callback;
                this.Name = (string)null;
            }
        }

        internal class ConsoleCommand
        {
            public readonly string Name;
            public Command.PluginCallback Callback;
            public readonly ConsoleSystem.Command RustCommand;
            public Action<ConsoleSystem.Arg> OriginalCallback;
            internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();

            public ConsoleCommand(string name)
            {
                this.Name = name;
                string[] strArray = this.Name.Split('.');
                this.RustCommand = new ConsoleSystem.Command()
                {
                    Name = strArray[1],
                    Parent = strArray[0],
                    FullName = name,
                    ServerUser = true,
                    ServerAdmin = true,
                    Client = true,
                    ClientInfo = false,
                    Variable = false,
                    Call = new Action<ConsoleSystem.Arg>(this.HandleCommand)
                };
            }

            public void AddCallback(Plugin plugin, string name) => this.Callback = new Command.PluginCallback(plugin, name);

            public void AddCallback(Plugin plugin, Func<ConsoleSystem.Arg, bool> callback) => this.Callback = new Command.PluginCallback(plugin, callback);

            public void HandleCommand(ConsoleSystem.Arg arg)
            {
                this.Callback.Plugin?.TrackStart();
                int num = this.Callback.Call(arg) ? 1 : 0;
                this.Callback.Plugin?.TrackEnd();
            }
        }

        internal class ChatCommand
        {
            public readonly string Name;
            public readonly Plugin Plugin;
            private readonly Action<BasePlayer, string, string[]> _callback;

            public ChatCommand(string name, Plugin plugin, Action<BasePlayer, string, string[]> callback)
            {
                this.Name = name;
                this.Plugin = plugin;
                this._callback = callback;
            }

            public void HandleCommand(BasePlayer sender, string name, string[] args)
            {
                this.Plugin?.TrackStart();
                Action<BasePlayer, string, string[]> callback = this._callback;
                if (callback != null)
                    callback(sender, name, args);
                this.Plugin?.TrackEnd();
            }
        }
    }
}
