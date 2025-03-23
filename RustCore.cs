// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.RustCore
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using ConVar;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Core.RemoteConsole;
using Oxide.Game.Rust.Libraries.Covalence;
using Rust.Ai.Gen2;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Game.Rust
{
    public class RustCore : CSPlugin
    {
        internal readonly Oxide.Game.Rust.Libraries.Command cmdlib = Interface.Oxide.GetLibrary<Oxide.Game.Rust.Libraries.Command>();
        internal readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        internal readonly Oxide.Game.Rust.Libraries.Player Player = Interface.Oxide.GetLibrary<Oxide.Game.Rust.Libraries.Player>();
        internal static readonly RustCovalenceProvider Covalence = RustCovalenceProvider.Instance;
        internal readonly PluginManager pluginManager = Interface.Oxide.RootPluginManager;
        internal readonly IServer Server = RustCore.Covalence.CreateServer();
        internal readonly RustExtension Extension;
        internal bool serverInitialized;
        internal bool isPlayerTakingDamage;
        internal static string ipPattern = ":{1}[0-9]{1}\\d*";

        [HookMethod("GrantCommand")]
        private void GrantCommand(IPlayer player, string command, string[] args)
        {
            if (!this.PermissionsLoaded(player))
                return;
            if (args.Length < 3)
            {
                player.Reply(this.lang.GetMessage("CommandUsageGrant", (Plugin)this, player.Id));
            }
            else
            {
                string str1 = args[0];
                string str2 = args[1].Sanitize();
                string permission = args[2];
                if (!this.permission.PermissionExists(permission))
                    player.Reply(string.Format(this.lang.GetMessage("PermissionNotFound", (Plugin)this, player.Id), (object)permission));
                else if (str1.Equals("group"))
                {
                    if (!this.permission.GroupExists(str2))
                        player.Reply(string.Format(this.lang.GetMessage("GroupNotFound", (Plugin)this, player.Id), (object)str2));
                    else if (this.permission.GroupHasPermission(str2, permission))
                    {
                        player.Reply(string.Format(this.lang.GetMessage("GroupAlreadyHasPermission", (Plugin)this, player.Id), (object)str2, (object)permission));
                    }
                    else
                    {
                        this.permission.GrantGroupPermission(str2, permission, (Plugin)null);
                        player.Reply(string.Format(this.lang.GetMessage("GroupPermissionGranted", (Plugin)this, player.Id), (object)str2, (object)permission));
                    }
                }
                else if (str1.Equals("user"))
                {
                    IPlayer[] array = RustCore.Covalence.PlayerManager.FindPlayers(str2).ToArray<IPlayer>();
                    if (array.Length > 1)
                    {
                        player.Reply(string.Format(this.lang.GetMessage("PlayersFound", (Plugin)this, player.Id), (object)string.Join(", ", ((IEnumerable<IPlayer>)array).Select<IPlayer, string>((Func<IPlayer, string>)(p => p.Name)).ToArray<string>())));
                    }
                    else
                    {
                        IPlayer player1 = array.Length == 1 ? array[0] : (IPlayer)null;
                        if (player1 == null && !this.permission.UserIdValid(str2))
                        {
                            player.Reply(string.Format(this.lang.GetMessage("PlayerNotFound", (Plugin)this, player.Id), (object)str2));
                        }
                        else
                        {
                            string playerId = str2;
                            if (player1 != null)
                            {
                                playerId = player1.Id;
                                str2 = player1.Name;
                                this.permission.UpdateNickname(playerId, str2);
                            }
                            if (this.permission.UserHasPermission(str2, permission))
                            {
                                player.Reply(string.Format(this.lang.GetMessage("PlayerAlreadyHasPermission", (Plugin)this, player.Id), (object)playerId, (object)permission));
                            }
                            else
                            {
                                this.permission.GrantUserPermission(playerId, permission, (Plugin)null);
                                player.Reply(string.Format(this.lang.GetMessage("PlayerPermissionGranted", (Plugin)this, player.Id), (object)(str2 + " (" + playerId + ")"), (object)permission));
                            }
                        }
                    }
                }
                else
                    player.Reply(this.lang.GetMessage("CommandUsageGrant", (Plugin)this, player.Id));
            }
        }

        [HookMethod("GroupCommand")]
        private void GroupCommand(IPlayer player, string command, string[] args)
        {
            if (!this.PermissionsLoaded(player))
                return;
            if (args.Length < 2)
            {
                player.Reply(this.lang.GetMessage("CommandUsageGroup", (Plugin)this, player.Id));
                player.Reply(this.lang.GetMessage("CommandUsageGroupParent", (Plugin)this, player.Id));
                player.Reply(this.lang.GetMessage("CommandUsageGroupRemove", (Plugin)this, player.Id));
            }
            else
            {
                string str1 = args[0];
                string groupName = args[1];
                string groupTitle = args.Length >= 3 ? args[2] : "";
                int groupRank = args.Length == 4 ? int.Parse(args[3]) : 0;
                if (str1.Equals("add"))
                {
                    if (this.permission.GroupExists(groupName))
                    {
                        player.Reply(string.Format(this.lang.GetMessage("GroupAlreadyExists", (Plugin)this, player.Id), (object)groupName));
                    }
                    else
                    {
                        this.permission.CreateGroup(groupName, groupTitle, groupRank);
                        player.Reply(string.Format(this.lang.GetMessage("GroupCreated", (Plugin)this, player.Id), (object)groupName));
                    }
                }
                else if (str1.Equals("remove"))
                {
                    if (!this.permission.GroupExists(groupName))
                    {
                        player.Reply(string.Format(this.lang.GetMessage("GroupNotFound", (Plugin)this, player.Id), (object)groupName));
                    }
                    else
                    {
                        this.permission.RemoveGroup(groupName);
                        player.Reply(string.Format(this.lang.GetMessage("GroupDeleted", (Plugin)this, player.Id), (object)groupName));
                    }
                }
                else if (str1.Equals("set"))
                {
                    if (!this.permission.GroupExists(groupName))
                    {
                        player.Reply(string.Format(this.lang.GetMessage("GroupNotFound", (Plugin)this, player.Id), (object)groupName));
                    }
                    else
                    {
                        this.permission.SetGroupTitle(groupName, groupTitle);
                        this.permission.SetGroupRank(groupName, groupRank);
                        player.Reply(string.Format(this.lang.GetMessage("GroupChanged", (Plugin)this, player.Id), (object)groupName));
                    }
                }
                else if (str1.Equals("parent"))
                {
                    if (args.Length <= 2)
                        player.Reply(this.lang.GetMessage("CommandUsageGroupParent", (Plugin)this, player.Id));
                    else if (!this.permission.GroupExists(groupName))
                    {
                        player.Reply(string.Format(this.lang.GetMessage("GroupNotFound", (Plugin)this, player.Id), (object)groupName));
                    }
                    else
                    {
                        string str2 = args[2];
                        if (!string.IsNullOrEmpty(str2) && !this.permission.GroupExists(str2))
                            player.Reply(string.Format(this.lang.GetMessage("GroupParentNotFound", (Plugin)this, player.Id), (object)str2));
                        else if (this.permission.SetGroupParent(groupName, str2))
                            player.Reply(string.Format(this.lang.GetMessage("GroupParentChanged", (Plugin)this, player.Id), (object)groupName, (object)str2));
                        else
                            player.Reply(string.Format(this.lang.GetMessage("GroupParentNotChanged", (Plugin)this, player.Id), (object)groupName));
                    }
                }
                else
                {
                    player.Reply(this.lang.GetMessage("CommandUsageGroup", (Plugin)this, player.Id));
                    player.Reply(this.lang.GetMessage("CommandUsageGroupParent", (Plugin)this, player.Id));
                    player.Reply(this.lang.GetMessage("CommandUsageGroupRemove", (Plugin)this, player.Id));
                }
            }
        }

        [HookMethod("LangCommand")]
        private void LangCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.Reply(this.lang.GetMessage("CommandUsageLang", (Plugin)this, player.Id));
            }
            else
            {
                string str = args[0];
                string letterIsoLanguageName;
                try
                {
                    letterIsoLanguageName = new CultureInfo(str)?.TwoLetterISOLanguageName;
                }
                catch (CultureNotFoundException ex)
                {
                    player.Reply(this.lang.GetMessage("InvalidLanguageName", (Plugin)this, player.Id), str);
                    return;
                }
                if (player.IsServer)
                {
                    this.lang.SetServerLanguage(letterIsoLanguageName);
                    player.Reply(string.Format(this.lang.GetMessage("ServerLanguage", (Plugin)this, player.Id), (object)this.lang.GetServerLanguage()));
                }
                else
                {
                    this.lang.SetLanguage(letterIsoLanguageName, player.Id);
                    player.Reply(string.Format(this.lang.GetMessage("PlayerLanguage", (Plugin)this, player.Id), (object)letterIsoLanguageName));
                }
            }
        }

        [HookMethod("LoadCommand")]
        private void LoadCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
                player.Reply(this.lang.GetMessage("CommandUsageLoad", (Plugin)this, player.Id));
            else if (args[0].Equals("*") || args[0].Equals("all"))
            {
                Interface.Oxide.LoadAllPlugins();
            }
            else
            {
                foreach (string name in args)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        Interface.Oxide.LoadPlugin(name);
                        this.pluginManager.GetPlugin(name);
                    }
                }
            }
        }

        [HookMethod("PluginsCommand")]
        private void PluginsCommand(IPlayer player)
        {
            Plugin[] array = this.pluginManager.GetPlugins().Where<Plugin>((Func<Plugin, bool>)(pl => !pl.IsCorePlugin)).ToArray<Plugin>();
            HashSet<string> second = new HashSet<string>(((IEnumerable<Plugin>)array).Select<Plugin, string>((Func<Plugin, string>)(pl => pl.Name)));
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            foreach (PluginLoader pluginLoader in Interface.Oxide.GetPluginLoaders())
            {
                foreach (string key in pluginLoader.ScanDirectory(Interface.Oxide.PluginDirectory).Except<string>((IEnumerable<string>)second))
                {
                    string str;
                    dictionary[key] = pluginLoader.PluginErrors.TryGetValue(key, out str) ? str : "Unloaded";
                }
            }
            if (array.Length + dictionary.Count < 1)
            {
                player.Reply(this.lang.GetMessage("NoPluginsFound", (Plugin)this, player.Id));
            }
            else
            {
                string message = string.Format("Listing {0} plugins:", (object)(array.Length + dictionary.Count));
                int num = 1;
                foreach (Plugin plugin in ((IEnumerable<Plugin>)array).Where<Plugin>((Func<Plugin, bool>)(p => p.Filename != null)))
                    message += string.Format("\n  {0:00} \"{1}\" ({2}) by {3} ({4:0.00}s / {5}) - {6}", (object)num++, (object)plugin.Title, (object)plugin.Version, (object)plugin.Author, (object)plugin.TotalHookTime, (object)RustCore.FormatBytes(plugin.TotalHookMemory), (object)plugin.Filename.Basename());
                foreach (string key in dictionary.Keys)
                    message += string.Format("\n  {0:00} {1} - {2}", (object)num++, (object)key, (object)dictionary[key]);
                player.Reply(message);
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024L)
                return string.Format("{0:0} B", (object)bytes);
            if (bytes < 1048576L)
                return string.Format("{0:0} KB", (object)(bytes / 1024L));
            return bytes < 1073741824L ? string.Format("{0:0} MB", (object)(bytes / 1048576L)) : string.Format("{0:0} GB", (object)(bytes / 1073741824L));
        }

        [HookMethod("ReloadCommand")]
        private void ReloadCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
                player.Reply(this.lang.GetMessage("CommandUsageReload", (Plugin)this, player.Id));
            else if (args[0].Equals("*") || args[0].Equals("all"))
            {
                Interface.Oxide.ReloadAllPlugins();
            }
            else
            {
                foreach (string name in args)
                {
                    if (!string.IsNullOrEmpty(name))
                        Interface.Oxide.ReloadPlugin(name);
                }
            }
        }

        [HookMethod("RevokeCommand")]
        private void RevokeCommand(IPlayer player, string command, string[] args)
        {
            if (!this.PermissionsLoaded(player))
                return;
            if (args.Length < 3)
            {
                player.Reply(this.lang.GetMessage("CommandUsageRevoke", (Plugin)this, player.Id));
            }
            else
            {
                string str1 = args[0];
                string str2 = args[1].Sanitize();
                string permission = args[2];
                if (str1.Equals("group"))
                {
                    if (!this.permission.GroupExists(str2))
                        player.Reply(string.Format(this.lang.GetMessage("GroupNotFound", (Plugin)this, player.Id), (object)str2));
                    else if (!this.permission.GroupHasPermission(str2, permission))
                    {
                        player.Reply(string.Format(this.lang.GetMessage("GroupDoesNotHavePermission", (Plugin)this, player.Id), (object)str2, (object)permission));
                    }
                    else
                    {
                        this.permission.RevokeGroupPermission(str2, permission);
                        player.Reply(string.Format(this.lang.GetMessage("GroupPermissionRevoked", (Plugin)this, player.Id), (object)str2, (object)permission));
                    }
                }
                else if (str1.Equals("user"))
                {
                    IPlayer[] array = RustCore.Covalence.PlayerManager.FindPlayers(str2).ToArray<IPlayer>();
                    if (array.Length > 1)
                    {
                        player.Reply(string.Format(this.lang.GetMessage("PlayersFound", (Plugin)this, player.Id), (object)string.Join(", ", ((IEnumerable<IPlayer>)array).Select<IPlayer, string>((Func<IPlayer, string>)(p => p.Name)).ToArray<string>())));
                    }
                    else
                    {
                        IPlayer player1 = array.Length == 1 ? array[0] : (IPlayer)null;
                        if (player1 == null && !this.permission.UserIdValid(str2))
                        {
                            player.Reply(string.Format(this.lang.GetMessage("PlayerNotFound", (Plugin)this, player.Id), (object)str2));
                        }
                        else
                        {
                            string playerId = str2;
                            if (player1 != null)
                            {
                                playerId = player1.Id;
                                str2 = player1.Name;
                                this.permission.UpdateNickname(playerId, str2);
                            }
                            if (!this.permission.UserHasPermission(playerId, permission))
                            {
                                player.Reply(string.Format(this.lang.GetMessage("PlayerDoesNotHavePermission", (Plugin)this, player.Id), (object)str2, (object)permission));
                            }
                            else
                            {
                                this.permission.RevokeUserPermission(playerId, permission);
                                player.Reply(string.Format(this.lang.GetMessage("PlayerPermissionRevoked", (Plugin)this, player.Id), (object)(str2 + " (" + playerId + ")"), (object)permission));
                            }
                        }
                    }
                }
                else
                    player.Reply(this.lang.GetMessage("CommandUsageRevoke", (Plugin)this, player.Id));
            }
        }

        [HookMethod("ShowCommand")]
        private void ShowCommand(IPlayer player, string command, string[] args)
        {
            if (!this.PermissionsLoaded(player))
                return;
            if (args.Length < 1)
            {
                player.Reply(this.lang.GetMessage("CommandUsageShow", (Plugin)this, player.Id));
                player.Reply(this.lang.GetMessage("CommandUsageShowName", (Plugin)this, player.Id));
            }
            else
            {
                string str1 = args[0];
                string str2 = args.Length == 2 ? args[1].Sanitize() : string.Empty;
                if (str1.Equals("perms"))
                    player.Reply(string.Format(this.lang.GetMessage("Permissions", (Plugin)this, player.Id) + ":\n" + string.Join(", ", this.permission.GetPermissions())));
                else if (str1.Equals("perm"))
                {
                    if (args.Length < 2 || string.IsNullOrEmpty(str2))
                    {
                        player.Reply(this.lang.GetMessage("CommandUsageShow", (Plugin)this, player.Id));
                        player.Reply(this.lang.GetMessage("CommandUsageShowName", (Plugin)this, player.Id));
                    }
                    else
                    {
                        string[] permissionUsers = this.permission.GetPermissionUsers(str2);
                        string[] permissionGroups = this.permission.GetPermissionGroups(str2);
                        string message = string.Format(this.lang.GetMessage("PermissionPlayers", (Plugin)this, player.Id), (object)str2) + ":\n" + (permissionUsers.Length != 0 ? string.Join(", ", permissionUsers) : this.lang.GetMessage("NoPermissionPlayers", (Plugin)this, player.Id)) + "\n\n" + string.Format(this.lang.GetMessage("PermissionGroups", (Plugin)this, player.Id), (object)str2) + ":\n" + (permissionGroups.Length != 0 ? string.Join(", ", permissionGroups) : this.lang.GetMessage("NoPermissionGroups", (Plugin)this, player.Id));
                        player.Reply(message);
                    }
                }
                else if (str1.Equals("user"))
                {
                    if (args.Length < 2 || string.IsNullOrEmpty(str2))
                    {
                        player.Reply(this.lang.GetMessage("CommandUsageShow", (Plugin)this, player.Id));
                        player.Reply(this.lang.GetMessage("CommandUsageShowName", (Plugin)this, player.Id));
                    }
                    else
                    {
                        IPlayer[] array = RustCore.Covalence.PlayerManager.FindPlayers(str2).ToArray<IPlayer>();
                        if (array.Length > 1)
                        {
                            player.Reply(string.Format(this.lang.GetMessage("PlayersFound", (Plugin)this, player.Id), (object)string.Join(", ", ((IEnumerable<IPlayer>)array).Select<IPlayer, string>((Func<IPlayer, string>)(p => p.Name)).ToArray<string>())));
                        }
                        else
                        {
                            IPlayer player1 = array.Length == 1 ? array[0] : (IPlayer)null;
                            if (player1 == null && !this.permission.UserIdValid(str2))
                            {
                                player.Reply(string.Format(this.lang.GetMessage("PlayerNotFound", (Plugin)this, player.Id), (object)str2));
                            }
                            else
                            {
                                string playerId = str2;
                                if (player1 != null)
                                {
                                    playerId = player1.Id;
                                    string name = player1.Name;
                                    this.permission.UpdateNickname(playerId, name);
                                    str2 = name + " (" + playerId + ")";
                                }
                                string[] userPermissions = this.permission.GetUserPermissions(playerId);
                                string[] userGroups = this.permission.GetUserGroups(playerId);
                                string message = string.Format(this.lang.GetMessage("PlayerPermissions", (Plugin)this, player.Id), (object)str2) + ":\n" + (userPermissions.Length != 0 ? string.Join(", ", userPermissions) : this.lang.GetMessage("NoPlayerPermissions", (Plugin)this, player.Id)) + "\n\n" + string.Format(this.lang.GetMessage("PlayerGroups", (Plugin)this, player.Id), (object)str2) + ":\n" + (userGroups.Length != 0 ? string.Join(", ", userGroups) : this.lang.GetMessage("NoPlayerGroups", (Plugin)this, player.Id));
                                player.Reply(message);
                            }
                        }
                    }
                }
                else if (str1.Equals("group"))
                {
                    if (args.Length < 2 || string.IsNullOrEmpty(str2))
                    {
                        player.Reply(this.lang.GetMessage("CommandUsageShow", (Plugin)this, player.Id));
                        player.Reply(this.lang.GetMessage("CommandUsageShowName", (Plugin)this, player.Id));
                    }
                    else if (!this.permission.GroupExists(str2))
                    {
                        player.Reply(string.Format(this.lang.GetMessage("GroupNotFound", (Plugin)this, player.Id), (object)str2));
                    }
                    else
                    {
                        string[] usersInGroup = this.permission.GetUsersInGroup(str2);
                        string[] groupPermissions = this.permission.GetGroupPermissions(str2);
                        string message = string.Format(this.lang.GetMessage("GroupPlayers", (Plugin)this, player.Id), (object)str2) + ":\n" + (usersInGroup.Length != 0 ? string.Join(", ", usersInGroup) : this.lang.GetMessage("NoPlayersInGroup", (Plugin)this, player.Id)) + "\n\n" + string.Format(this.lang.GetMessage("GroupPermissions", (Plugin)this, player.Id), (object)str2) + ":\n" + (groupPermissions.Length != 0 ? string.Join(", ", groupPermissions) : this.lang.GetMessage("NoGroupPermissions", (Plugin)this, player.Id));
                        for (string groupParent = this.permission.GetGroupParent(str2); this.permission.GroupExists(groupParent); groupParent = this.permission.GetGroupParent(groupParent))
                            message = message + "\n" + string.Format(this.lang.GetMessage("ParentGroupPermissions", (Plugin)this, player.Id), (object)groupParent) + ":\n" + string.Join(", ", this.permission.GetGroupPermissions(groupParent));
                        player.Reply(message);
                    }
                }
                else if (str1.Equals("groups"))
                {
                    player.Reply(string.Format(this.lang.GetMessage("Groups", (Plugin)this, player.Id) + ":\n" + string.Join(", ", this.permission.GetGroups())));
                }
                else
                {
                    player.Reply(this.lang.GetMessage("CommandUsageShow", (Plugin)this, player.Id));
                    player.Reply(this.lang.GetMessage("CommandUsageShowName", (Plugin)this, player.Id));
                }
            }
        }

        [HookMethod("UnloadCommand")]
        private void UnloadCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
                player.Reply(this.lang.GetMessage("CommandUsageUnload", (Plugin)this, player.Id));
            else if (args[0].Equals("*") || args[0].Equals("all"))
            {
                Interface.Oxide.UnloadAllPlugins();
            }
            else
            {
                foreach (string name in args)
                {
                    if (!string.IsNullOrEmpty(name))
                        Interface.Oxide.UnloadPlugin(name);
                }
            }
        }

        [HookMethod("UserGroupCommand")]
        private void UserGroupCommand(IPlayer player, string command, string[] args)
        {
            if (!this.PermissionsLoaded(player))
                return;
            if (args.Length < 3)
            {
                player.Reply(this.lang.GetMessage("CommandUsageUserGroup", (Plugin)this, player.Id));
            }
            else
            {
                string str1 = args[0];
                string str2 = args[1].Sanitize();
                string groupName = args[2];
                IPlayer[] array = RustCore.Covalence.PlayerManager.FindPlayers(str2).ToArray<IPlayer>();
                if (array.Length > 1)
                {
                    player.Reply(string.Format(this.lang.GetMessage("PlayersFound", (Plugin)this, player.Id), (object)string.Join(", ", ((IEnumerable<IPlayer>)array).Select<IPlayer, string>((Func<IPlayer, string>)(p => p.Name)).ToArray<string>())));
                }
                else
                {
                    IPlayer player1 = array.Length == 1 ? array[0] : (IPlayer)null;
                    if (player1 == null && !this.permission.UserIdValid(str2))
                    {
                        player.Reply(string.Format(this.lang.GetMessage("PlayerNotFound", (Plugin)this, player.Id), (object)str2));
                    }
                    else
                    {
                        string playerId = str2;
                        if (player1 != null)
                        {
                            playerId = player1.Id;
                            string name = player1.Name;
                            this.permission.UpdateNickname(playerId, name);
                            str2 = name + "(" + playerId + ")";
                        }
                        if (!this.permission.GroupExists(groupName))
                            player.Reply(string.Format(this.lang.GetMessage("GroupNotFound", (Plugin)this, player.Id), (object)groupName));
                        else if (str1.Equals("add"))
                        {
                            this.permission.AddUserGroup(playerId, groupName);
                            player.Reply(string.Format(this.lang.GetMessage("PlayerAddedToGroup", (Plugin)this, player.Id), (object)str2, (object)groupName));
                        }
                        else if (str1.Equals("remove"))
                        {
                            this.permission.RemoveUserGroup(playerId, groupName);
                            player.Reply(string.Format(this.lang.GetMessage("PlayerRemovedFromGroup", (Plugin)this, player.Id), (object)str2, (object)groupName));
                        }
                        else
                            player.Reply(this.lang.GetMessage("CommandUsageUserGroup", (Plugin)this, player.Id));
                    }
                }
            }
        }

        [HookMethod("VersionCommand")]
        private void VersionCommand(IPlayer player)
        {
            if (player.IsServer)
            {
                string format = "Oxide.Rust Version: {0}\nOxide.Rust Branch: {1}";
                player.Reply(string.Format(format, (object)RustExtension.AssemblyVersion, (object)this.Extension.Branch));
            }
            else
            {
                string format = RustCore.Covalence.FormatText(this.lang.GetMessage("Version", (Plugin)this, player.Id));
                player.Reply(string.Format(format, (object)RustExtension.AssemblyVersion, (object)RustCore.Covalence.GameName, (object)this.Server.Version, (object)this.Server.Protocol));
            }
        }

        [HookMethod("SaveCommand")]
        private void SaveCommand(IPlayer player)
        {
            if (!this.PermissionsLoaded(player) || !player.IsAdmin)
                return;
            Interface.Oxide.OnSave();
            RustCore.Covalence.PlayerManager.SavePlayerData();
            player.Reply(this.lang.GetMessage("DataSaved", (Plugin)this, player.Id));
        }

        public RustCore()
        {
            this.Extension = Interface.Oxide.GetExtension<RustExtension>();
            this.Title = "Rust";
            this.Author = this.Extension.Author;
            this.Version = this.Extension.Version;
        }

        internal static IEnumerable<string> RestrictedCommands => (IEnumerable<string>)new string[4]
        {
      "ownerid",
      "moderatorid",
      "removeowner",
      "removemoderator"
        };

        private bool PermissionsLoaded(IPlayer player)
        {
            if (this.permission.IsLoaded)
                return true;
            player.Reply(string.Format(this.lang.GetMessage("PermissionsNotLoaded", (Plugin)this, player.Id), (object)this.permission.LastException.Message));
            return false;
        }

        [HookMethod("Init")]
        private void Init()
        {
            RemoteLogger.SetTag("game", this.Title.ToLower());
            RemoteLogger.SetTag("game version", this.Server.Version);
            this.AddCovalenceCommand(new string[3]
            {
        "oxide.plugins",
        "o.plugins",
        "plugins"
            }, "PluginsCommand", "oxide.plugins");
            this.AddCovalenceCommand(new string[3]
            {
        "oxide.load",
        "o.load",
        "plugin.load"
            }, "LoadCommand", "oxide.load");
            this.AddCovalenceCommand(new string[3]
            {
        "oxide.reload",
        "o.reload",
        "plugin.reload"
            }, "ReloadCommand", "oxide.reload");
            this.AddCovalenceCommand(new string[3]
            {
        "oxide.unload",
        "o.unload",
        "plugin.unload"
            }, "UnloadCommand", "oxide.unload");
            this.AddCovalenceCommand(new string[3]
            {
        "oxide.grant",
        "o.grant",
        "perm.grant"
            }, "GrantCommand", "oxide.grant");
            this.AddCovalenceCommand(new string[3]
            {
        "oxide.group",
        "o.group",
        "perm.group"
            }, "GroupCommand", "oxide.group");
            this.AddCovalenceCommand(new string[3]
            {
        "oxide.revoke",
        "o.revoke",
        "perm.revoke"
            }, "RevokeCommand", "oxide.revoke");
            this.AddCovalenceCommand(new string[3]
            {
        "oxide.show",
        "o.show",
        "perm.show"
            }, "ShowCommand", "oxide.show");
            this.AddCovalenceCommand(new string[3]
            {
        "oxide.usergroup",
        "o.usergroup",
        "perm.usergroup"
            }, "UserGroupCommand", "oxide.usergroup");
            this.AddCovalenceCommand(new string[3]
            {
        "oxide.lang",
        "o.lang",
        "lang"
            }, "LangCommand");
            this.AddCovalenceCommand(new string[2]
            {
        "oxide.save",
        "o.save"
            }, "SaveCommand");
            this.AddCovalenceCommand(new string[2]
            {
        "oxide.version",
        "o.version"
            }, "VersionCommand");
            foreach (KeyValuePair<string, Dictionary<string, string>> language in Localization.languages)
                this.lang.RegisterMessages(language.Value, (Plugin)this, language.Key);
            if (!this.permission.IsLoaded)
                return;
            int num = 0;
            foreach (string defaultGroup in Interface.Oxide.Config.Options.DefaultGroups)
            {
                if (!this.permission.GroupExists(defaultGroup))
                    this.permission.CreateGroup(defaultGroup, defaultGroup, num++);
            }
            ulong result;
            this.permission.RegisterValidate((Func<string, bool>)(s => ulong.TryParse(s, out result) && (result == 0UL ? 1 : (int)Math.Floor(Math.Log10((double)result) + 1.0)) >= 17));
            this.permission.CleanUp();
        }

        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (!this.serverInitialized)
                return;
            plugin.CallHook("OnServerInitialized", (object)false);
        }

        [HookMethod("IOnServerInitialized")]
        private void IOnServerInitialized()
        {
            if (this.serverInitialized)
                return;
            Analytics.Collect();
            if (!Interface.Oxide.Config.Options.Modded)
                Interface.Oxide.LogWarning("The server is currently listed under Community. Please be aware that Facepunch only allows admin tools (that do not affect gameplay) under the Community section");
            this.serverInitialized = true;
            Interface.CallHook("OnServerInitialized", (object)this.serverInitialized);
        }

        [HookMethod("OnServerSave")]
        private void OnServerSave()
        {
            Interface.Oxide.OnSave();
            RustCore.Covalence.PlayerManager.SavePlayerData();
        }

        [HookMethod("IOnServerShutdown")]
        private void IOnServerShutdown()
        {
            Interface.Oxide.CallHook("OnServerShutdown");
            Interface.Oxide.OnShutdown();
            RustCore.Covalence.PlayerManager.SavePlayerData();
        }

        private void ParseCommand(string argstr, out string command, out string[] args)
        {
            List<string> stringList = new List<string>();
            StringBuilder stringBuilder = new StringBuilder();
            bool flag = false;
            foreach (char c in argstr)
            {
                if (c == '"')
                {
                    if (flag)
                    {
                        string str = stringBuilder.ToString().Trim();
                        if (!string.IsNullOrEmpty(str))
                            stringList.Add(str);
                        stringBuilder.Clear();
                        flag = false;
                    }
                    else
                        flag = true;
                }
                else if (char.IsWhiteSpace(c) && !flag)
                {
                    string str = stringBuilder.ToString().Trim();
                    if (!string.IsNullOrEmpty(str))
                        stringList.Add(str);
                    stringBuilder.Clear();
                }
                else
                    stringBuilder.Append(c);
            }
            if (stringBuilder.Length > 0)
            {
                string str = stringBuilder.ToString().Trim();
                if (!string.IsNullOrEmpty(str))
                    stringList.Add(str);
            }
            if (stringList.Count == 0)
            {
                command = (string)null;
                args = (string[])null;
            }
            else
            {
                command = stringList[0];
                stringList.RemoveAt(0);
                args = stringList.ToArray();
            }
        }

        public static BasePlayer FindPlayer(string nameOrIdOrIp)
        {
            BasePlayer player = (BasePlayer)null;
            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
                if (!string.IsNullOrEmpty(activePlayer.UserIDString))
                {
                    if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                        return activePlayer;
                    if (!string.IsNullOrEmpty(activePlayer.displayName))
                    {
                        if (activePlayer.displayName.Equals(nameOrIdOrIp, StringComparison.OrdinalIgnoreCase))
                            return activePlayer;
                        if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                            player = activePlayer;
                        if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp) || activePlayer.net?.connection != null && activePlayer.net.ID.Equals((object)nameOrIdOrIp))
                            return activePlayer;
                    }
                }
            }
            foreach (BasePlayer sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (!string.IsNullOrEmpty(sleepingPlayer.UserIDString))
                {
                    if (sleepingPlayer.UserIDString.Equals(nameOrIdOrIp))
                        return sleepingPlayer;
                    if (!string.IsNullOrEmpty(sleepingPlayer.displayName))
                    {
                        if (sleepingPlayer.displayName.Equals(nameOrIdOrIp, StringComparison.OrdinalIgnoreCase))
                            return sleepingPlayer;
                        if (sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                            player = sleepingPlayer;
                    }
                }
            }
            return player;
        }

        public static BasePlayer FindPlayerByName(string name)
        {
            BasePlayer playerByName = (BasePlayer)null;
            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
                if (!string.IsNullOrEmpty(activePlayer.displayName))
                {
                    if (activePlayer.displayName.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return activePlayer;
                    if (activePlayer.displayName.Contains(name, CompareOptions.OrdinalIgnoreCase))
                        playerByName = activePlayer;
                }
            }
            foreach (BasePlayer sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (!string.IsNullOrEmpty(sleepingPlayer.displayName))
                {
                    if (sleepingPlayer.displayName.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return sleepingPlayer;
                    if (sleepingPlayer.displayName.Contains(name, CompareOptions.OrdinalIgnoreCase))
                        playerByName = sleepingPlayer;
                }
            }
            return playerByName;
        }

        public static BasePlayer FindPlayerById(ulong id)
        {
            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
                if ((long)(ulong)activePlayer.userID == (long)id)
                    return activePlayer;
            }
            foreach (BasePlayer sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if ((long)(ulong)sleepingPlayer.userID == (long)id)
                    return sleepingPlayer;
            }
            return (BasePlayer)null;
        }

        public static BasePlayer FindPlayerByIdString(string id)
        {
            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
                if (!string.IsNullOrEmpty(activePlayer.UserIDString) && activePlayer.UserIDString.Equals(id))
                    return activePlayer;
            }
            foreach (BasePlayer sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (!string.IsNullOrEmpty(sleepingPlayer.UserIDString) && sleepingPlayer.UserIDString.Equals(id))
                    return sleepingPlayer;
            }
            return (BasePlayer)null;
        }

        [HookMethod("IOnBaseCombatEntityHurt")]
        private object IOnBaseCombatEntityHurt(BaseCombatEntity entity, HitInfo hitInfo) => !(entity is BasePlayer) ? Interface.CallHook("OnEntityTakeDamage", (object)entity, (object)hitInfo) : (object)null;

        [HookMethod("IOnNpcTarget")]
        private object IOnNpcTarget(BaseNpc npc, BaseEntity target)
        {
            if (Interface.CallHook("OnNpcTarget", (object)npc, (object)target) == null)
                return (object)null;
            npc.SetFact(BaseNpc.Facts.HasEnemy, (byte)0);
            npc.SetFact(BaseNpc.Facts.EnemyRange, (byte)3);
            npc.SetFact(BaseNpc.Facts.AfraidRange, (byte)1);
            npc.playerTargetDecisionStartTime = 0.0f;
            return (object)0.0f;
        }

        [HookMethod("IOnNpcTarget")]
        private object IOnNpcTarget(SenseComponent sense, BaseEntity target)
        {
            if (!(bool)(UnityEngine.Object)sense || !(bool)(UnityEngine.Object)target)
                return (object)null;
            BaseEntity baseEntity = sense.baseEntity;
            if (!(bool)(UnityEngine.Object)baseEntity)
                return (object)null;
            return Interface.CallHook("OnNpcTarget", (object)baseEntity, (object)target) != null ? (object)false : (object)null;
        }

        [HookMethod("IOnEntitySaved")]
        private void IOnEntitySaved(BaseNetworkable baseNetworkable, BaseNetworkable.SaveInfo saveInfo)
        {
            if (!this.serverInitialized || saveInfo.forConnection == null)
                return;
            Interface.CallHook("OnEntitySaved", (object)baseNetworkable, (object)saveInfo);
        }

        [HookMethod("IOnLoseCondition")]
        private object IOnLoseCondition(Item item, float amount)
        {
            object[] args = new object[2]
            {
        (object) item,
        (object) amount
            };
            Interface.CallHook("OnLoseCondition", args);
            amount = (float)args[1];
            float condition = item.condition;
            item.condition -= amount;
            if ((double)item.condition <= 0.0 && (double)item.condition < (double)condition)
                item.OnBroken();
            return (object)true;
        }

        [HookMethod("ICanPickupEntity")]
        private object ICanPickupEntity(BasePlayer basePlayer, DoorCloser entity) => !(Interface.CallHook("CanPickupEntity", (object)basePlayer, (object)entity) is bool flag) || flag ? (object)null : (object)true;

        [HookMethod("IOnBasePlayerAttacked")]
        private object IOnBasePlayerAttacked(BasePlayer basePlayer, HitInfo hitInfo)
        {
            if (!this.serverInitialized || (UnityEngine.Object)basePlayer == (UnityEngine.Object)null || hitInfo == null || basePlayer.IsDead() || this.isPlayerTakingDamage || basePlayer is NPCPlayer)
                return (object)null;
            if (Interface.CallHook("OnEntityTakeDamage", (object)basePlayer, (object)hitInfo) != null)
                return (object)true;
            this.isPlayerTakingDamage = true;
            try
            {
                basePlayer.OnAttacked(hitInfo);
            }
            finally
            {
                this.isPlayerTakingDamage = false;
            }
            return (object)true;
        }

        [HookMethod("IOnBasePlayerHurt")]
        private object IOnBasePlayerHurt(BasePlayer basePlayer, HitInfo hitInfo) => !this.isPlayerTakingDamage ? Interface.CallHook("OnEntityTakeDamage", (object)basePlayer, (object)hitInfo) : (object)null;

        [HookMethod("OnServerUserSet")]
        private void OnServerUserSet(
          ulong steamId,
          ServerUsers.UserGroup group,
          string playerName,
          string reason,
          long expiry)
        {
            if (!this.serverInitialized || group != ServerUsers.UserGroup.Banned)
                return;
            string id = steamId.ToString();
            IPlayer playerById = RustCore.Covalence.PlayerManager.FindPlayerById(id);
            Interface.CallHook("OnPlayerBanned", (object)playerName, (object)steamId, (object)(playerById?.Address ?? "0"), (object)reason, (object)expiry);
            Interface.CallHook("OnUserBanned", (object)playerName, (object)id, (object)(playerById?.Address ?? "0"), (object)reason, (object)expiry);
        }

        [HookMethod("OnServerUserRemove")]
        private void OnServerUserRemove(ulong steamId)
        {
            if (!this.serverInitialized || !ServerUsers.users.ContainsKey(steamId) || ServerUsers.users[steamId].group != ServerUsers.UserGroup.Banned)
                return;
            string id = steamId.ToString();
            IPlayer playerById = RustCore.Covalence.PlayerManager.FindPlayerById(id);
            Interface.CallHook("OnPlayerUnbanned", (object)(playerById?.Name ?? "Unnamed"), (object)steamId, (object)(playerById?.Address ?? "0"));
            Interface.CallHook("OnUserUnbanned", (object)(playerById?.Name ?? "Unnamed"), (object)id, (object)(playerById?.Address ?? "0"));
        }

        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(Network.Connection connection)
        {
            string username = connection.username;
            string str1 = connection.userid.ToString();
            string str2 = Regex.Replace(connection.ipaddress, RustCore.ipPattern, "");
            uint authLevel = connection.authLevel;
            if (this.permission.IsLoaded)
            {
                this.permission.UpdateNickname(str1, username);
                OxideConfig.DefaultGroups defaultGroups = Interface.Oxide.Config.Options.DefaultGroups;
                if (!this.permission.UserHasGroup(str1, defaultGroups.Players))
                    this.permission.AddUserGroup(str1, defaultGroups.Players);
                if (authLevel >= 2U && !this.permission.UserHasGroup(str1, defaultGroups.Administrators))
                    this.permission.AddUserGroup(str1, defaultGroups.Administrators);
            }
            RustCore.Covalence.PlayerManager.PlayerJoin(connection.userid, username);
            object obj1 = Interface.CallHook("CanClientLogin", (object)connection);
            object obj2 = Interface.CallHook("CanUserLogin", (object)username, (object)str1, (object)str2);
            object obj3 = obj1 == null ? obj2 : obj1;
            switch (obj3)
            {
                case string _:
                label_7:
                    ConnectionAuth.Reject(connection, obj3 is string ? obj3.ToString() : this.lang.GetMessage("ConnectionRejected", (Plugin)this, str1));
                    return (object)true;
                case bool flag:
                    if (flag)
                        break;
                    goto label_7;
            }
            object obj4 = Interface.CallHook("OnUserApprove", (object)connection);
            object obj5 = Interface.CallHook("OnUserApproved", (object)username, (object)str1, (object)str2);
            return obj4 ?? obj5;
        }

        [HookMethod("IOnPlayerBanned")]
        private void IOnPlayerBanned(Network.Connection connection, AuthResponse status) => Interface.CallHook("OnPlayerBanned", (object)connection, (object)status.ToString());

        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(
          ulong playerId,
          string playerName,
          string message,
          Chat.ChatChannel channel,
          BasePlayer basePlayer)
        {
            if (string.IsNullOrEmpty(message) || message.Equals("text"))
                return (object)true;
            string chatCommandPrefix = CommandHandler.GetChatCommandPrefix(message);
            if (chatCommandPrefix != null)
            {
                this.TryRunPlayerCommand(basePlayer, message, chatCommandPrefix);
                return (object)false;
            }
            message = message.EscapeRichText();
            if ((UnityEngine.Object)basePlayer == (UnityEngine.Object)null || !basePlayer.IsConnected)
                return Interface.CallHook("OnPlayerOfflineChat", (object)playerId, (object)playerName, (object)message, (object)channel);
            object obj1 = Interface.CallHook("OnPlayerChat", (object)basePlayer, (object)message, (object)channel);
            object obj2 = Interface.CallHook("OnUserChat", (object)basePlayer.IPlayer, (object)message);
            return obj1 ?? obj2;
        }

        private void TryRunPlayerCommand(BasePlayer basePlayer, string message, string commandPrefix)
        {
            if ((UnityEngine.Object)basePlayer == (UnityEngine.Object)null)
                return;
            string message1 = message.Replace("\n", "").Replace("\r", "").Trim();
            if (string.IsNullOrEmpty(message1))
                return;
            string command;
            string[] args;
            this.ParseCommand(message1.Substring(commandPrefix.Length), out command, out args);
            if (command == null)
                return;
            if (!basePlayer.IsConnected)
            {
                Interface.CallHook("OnApplicationCommand", (object)basePlayer, (object)command, (object)args);
                Interface.CallHook("OnApplicationCommand", (object)basePlayer.IPlayer, (object)command, (object)args);
            }
            else
            {
                object obj1 = Interface.CallHook("OnPlayerCommand", (object)basePlayer, (object)command, (object)args);
                object obj2 = Interface.CallHook("OnUserCommand", (object)basePlayer.IPlayer, (object)command, (object)args);
                if ((obj1 == null ? obj2 : obj1) != null)
                    return;
                try
                {
                    if (RustCore.Covalence.CommandSystem.HandleChatMessage(basePlayer.IPlayer, message1) || this.cmdlib.HandleChatCommand(basePlayer, command, args) || !Interface.Oxide.Config.Options.Modded)
                        return;
                    basePlayer.IPlayer.Reply(string.Format(this.lang.GetMessage("UnknownCommand", (Plugin)this, basePlayer.IPlayer.Id), (object)command));
                }
                catch (Exception ex)
                {
                    Exception exception = ex;
                    string str1 = string.Empty;
                    string str2 = string.Empty;
                    StringBuilder stringBuilder = new StringBuilder();
                    for (; exception != null; exception = exception.InnerException)
                    {
                        string name = exception.GetType().Name;
                        str1 = (name + ": " + exception.Message).TrimEnd(' ', ':');
                        stringBuilder.AppendLine(exception.StackTrace);
                        if (exception.InnerException != null)
                            stringBuilder.AppendLine("Rethrow as " + name);
                    }
                    System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(ex, 0, true);
                    for (int index = 0; index < stackTrace.FrameCount; ++index)
                    {
                        MethodBase method = stackTrace.GetFrame(index).GetMethod();
                        if ((object)method != null && (object)method.DeclaringType != null && method.DeclaringType.Namespace == "Oxide.Plugins")
                            str2 = method.DeclaringType.Name;
                    }
                    Interface.Oxide.LogError(string.Format("Failed to run command '/{0}' on plugin '{1}'. ({2}){3}{4}", (object)command, (object)str2, (object)str1.Replace(System.Environment.NewLine, " "), (object)System.Environment.NewLine, (object)stackTrace));
                }
            }
        }

        [HookMethod("OnClientAuth")]
        private void OnClientAuth(Network.Connection connection) => connection.username = Regex.Replace(connection.username, "<[^>]*>", string.Empty);

        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(BasePlayer basePlayer)
        {
            this.lang.SetLanguage(basePlayer.net.connection.info.GetString("global.language", "en"), basePlayer.UserIDString);
            basePlayer.SendEntitySnapshot((BaseNetworkable)CommunityEntity.ServerInstance);
            RustCore.Covalence.PlayerManager.PlayerConnected(basePlayer);
            IPlayer playerById = RustCore.Covalence.PlayerManager.FindPlayerById(basePlayer.UserIDString);
            if (playerById != null)
            {
                basePlayer.IPlayer = playerById;
                Interface.CallHook("OnUserConnected", (object)playerById);
            }
            Interface.Oxide.CallHook("OnPlayerConnected", (object)basePlayer);
        }

        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(BasePlayer basePlayer, string reason)
        {
            IPlayer iplayer = basePlayer.IPlayer;
            if (iplayer != null)
                Interface.CallHook("OnUserDisconnected", (object)iplayer, (object)reason);
            RustCore.Covalence.PlayerManager.PlayerDisconnected(basePlayer);
        }

        [HookMethod("OnPlayerSetInfo")]
        private void OnPlayerSetInfo(Network.Connection connection, string key, string val)
        {
            if (!(key == "global.language"))
                return;
            this.lang.SetLanguage(val, connection.userid.ToString());
            BasePlayer player = connection.player as BasePlayer;
            if (!((UnityEngine.Object)player != (UnityEngine.Object)null))
                return;
            Interface.CallHook("OnPlayerLanguageChanged", (object)player, (object)val);
            if (player.IPlayer == null)
                return;
            Interface.CallHook("OnPlayerLanguageChanged", (object)player.IPlayer, (object)val);
        }

        [HookMethod("OnPlayerKicked")]
        private void OnPlayerKicked(BasePlayer basePlayer, string reason)
        {
            if (basePlayer.IPlayer == null)
                return;
            Interface.CallHook("OnUserKicked", (object)basePlayer.IPlayer, (object)reason);
        }

        [HookMethod("OnPlayerRespawn")]
        private object OnPlayerRespawn(BasePlayer basePlayer)
        {
            IPlayer iplayer = basePlayer.IPlayer;
            return iplayer == null ? (object)null : Interface.CallHook("OnUserRespawn", (object)iplayer);
        }

        [HookMethod("OnPlayerRespawned")]
        private void OnPlayerRespawned(BasePlayer basePlayer)
        {
            IPlayer iplayer = basePlayer.IPlayer;
            if (iplayer == null)
                return;
            Interface.CallHook("OnUserRespawned", (object)iplayer);
        }

        [HookMethod("IOnRconMessage")]
        private object IOnRconMessage(IPAddress ipAddress, string command)
        {
            if (ipAddress != null && !string.IsNullOrEmpty(command))
            {
                RemoteMessage message = RemoteMessage.GetMessage(command);
                if (string.IsNullOrEmpty(message?.Message))
                    return (object)null;
                if (Interface.CallHook("OnRconMessage", (object)ipAddress, (object)message) != null)
                    return (object)true;
                string[] source = Oxide.Core.CommandLine.Split(message.Message);
                if (source.Length >= 1)
                {
                    string lower = source[0].ToLower();
                    string[] array = ((IEnumerable<string>)source).Skip<string>(1).ToArray<string>();
                    if (Interface.CallHook("OnRconCommand", (object)ipAddress, (object)lower, (object)array) != null)
                        return (object)true;
                }
            }
            return (object)null;
        }

        [HookMethod("IOnRconInitialize")]
        private object IOnRconInitialize() => !Interface.Oxide.Config.Rcon.Enabled ? (object)null : (object)true;

        [HookMethod("IOnRunCommandLine")]
        private object IOnRunCommandLine()
        {
            foreach (KeyValuePair<string, string> keyValuePair in Facepunch.CommandLine.GetSwitches())
            {
                string str = keyValuePair.Value;
                if (str == "")
                    str = "1";
                ConsoleSystem.Run(ConsoleSystem.Option.Unrestricted with
                {
                    PrintOutput = false
                }, keyValuePair.Key.Substring(1), (object)str);
            }
            return (object)false;
        }

        [HookMethod("IOnServerCommand")]
        private object IOnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.Connection != null && (UnityEngine.Object)arg.Player() == (UnityEngine.Object)null)
                return (object)true;
            if (arg.cmd.FullName == "chat.say" || arg.cmd.FullName == "chat.teamsay" || arg.cmd.FullName == "chat.localsay")
                return (object)null;
            object obj1 = Interface.CallHook("OnServerCommand", (object)arg);
            object obj2 = Interface.CallHook("OnServerCommand", (object)arg.cmd.FullName, (object)RustCommandSystem.ExtractArgs(arg));
            return (obj1 == null ? obj2 : obj1) != null ? (object)true : (object)null;
        }

        [HookMethod("OnServerInformationUpdated")]
        private void OnServerInformationUpdated()
        {
            SteamServer.GameTags += ",^o";
            if (!Interface.Oxide.Config.Options.Modded)
                return;
            SteamServer.GameTags += "^z";
        }

        [HookMethod("IOnCupboardAuthorize")]
        private object IOnCupboardAuthorize(
          ulong userID,
          BasePlayer player,
          BuildingPrivlidge privlidge)
        {
            if ((long)userID == (long)(ulong)player.userID)
            {
                if (Interface.CallHook("OnCupboardAuthorize", (object)privlidge, (object)player) != null)
                    return (object)true;
            }
            else if (Interface.CallHook("OnCupboardAssign", (object)privlidge, (object)userID, (object)player) != null)
                return (object)true;
            return (object)null;
        }
    }
}
