//#define DEBUG
using Facepunch;
using Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins.NTeleportationExtensionMethods;
using Rust;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NTeleportation", "nivex", "1.8.9")]
    [Description("Multiple teleportation systems for admin and players")]
    class NTeleportation : RustPlugin
    {
        [PluginReference]
        private Plugin Clans, Economics, IQEconomic, ServerRewards, Friends, CompoundTeleport, ZoneManager, NoEscape, RaidBlock, PopupNotifications, BlockUsers;

        private Dictionary<string, BasePlayer> _codeToPlayer = new();
        private Dictionary<string, string> _playerToCode = new();

        private bool newSave;
        private const string NewLine = "\n";
        private const string TPA = "tpa";
        private static readonly string[] nullArg = { };
        private const string PermAdmin = "nteleportation.admin";
        private const string PermRestrictions = "nteleportation.norestrictions";
        private const string ConfigDefaultPermVip = "nteleportation.vip";
        private const string PermHome = "nteleportation.home";
        private const string PermWipeHomes = "nteleportation.wipehomes";
        private const string PermCraftHome = "nteleportation.crafthome";
        private const string PermCaveHome = "nteleportation.cavehome";
        private const string PermDeleteHome = "nteleportation.deletehome";
        private const string PermHomeHomes = "nteleportation.homehomes";
        private const string PermImportHomes = "nteleportation.importhomes";
        private const string PermRadiusHome = "nteleportation.radiushome";
        private const string PermCraftTpR = "nteleportation.crafttpr";
        private const string PermCaveTpR = "nteleportation.cavetpr";
        private const string PermTpR = "nteleportation.tpr";
        private const string PermTpA = "nteleportation.tpa";
        private const string PermTp = "nteleportation.tp";
        private const string PermDisallowTpToMe = "nteleportation.disallowtptome";
        private const string PermTpT = "nteleportation.tpt";
        private const string PermTpB = "nteleportation.tpb";
        private const string PermTpN = "nteleportation.tpn";
        private const string PermTpL = "nteleportation.tpl";
        private const string PermTpConsole = "nteleportation.tpconsole";
        private const string PermTpRemove = "nteleportation.tpremove";
        private const string PermTpSave = "nteleportation.tpsave";
        private const string PermExempt = "nteleportation.exemptfrominterruptcountdown";
        private const string PermFoundationCheck = "nteleportation.bypassfoundationcheck";
        private const string PermTpMarker = "nteleportation.tpmarker";
        private DynamicConfigFile dataConvert;
        private DynamicConfigFile dataDisabled;
        private DynamicConfigFile dataAdmin;
        private DynamicConfigFile dataHome;
        private DynamicConfigFile dataTPR;
        private DynamicConfigFile dataTPT;
        private Dictionary<ulong, AdminData> _Admin;
        private Dictionary<ulong, HomeData> _Home;
        private Dictionary<ulong, TeleportData> _TPR;
        private Dictionary<string, List<string>> TPT = new();
        private bool changedAdmin;
        private bool changedHome;
        private bool changedTPR;
        private bool changedTPT;
        private float boundary;
        private readonly Dictionary<ulong, float> TeleportCooldowns = new();
        private readonly Dictionary<ulong, TeleportTimer> TeleportTimers = new();
        private readonly Dictionary<ulong, Timer> PendingRequests = new();
        private readonly Dictionary<ulong, BasePlayer> PlayersRequests = new();
        private readonly Dictionary<int, string> ReverseBlockedItems = new();
        private readonly Dictionary<ulong, Vector3> teleporting = new();
        private SortedDictionary<string, Vector3> caves = new();
        private List<PrefabInfo> monuments = new();
        private bool outpostEnabled;
        private bool banditEnabled;

        private class PrefabInfo
        {
            public Quaternion rotation;
            public Vector2 positionXZ;
            public Vector3 position;
            public Vector3 extents;
            public string name;
            public string prefab;
            public bool sphere;
            public PrefabInfo() { }
            public PrefabInfo(Vector3 position, Quaternion rotation, Vector3 extents, float extra, string name, string prefab, bool sphere)
            {
                this.sphere = sphere;
                this.position = position;
                this.rotation = rotation;
                this.name = name;
                this.prefab = prefab;
                this.extents = extents + new Vector3(extra, extra, extra);
                positionXZ = position.XZ2D();
            }
            public bool IsInBounds(Vector3 a)
            {
                if (sphere)
                {
                    if (a.y > position.y + (extents.y * 1.75f))
                    {
                        return false;
                    }

                    return Vector3Ex.Distance2D(a, position) <= extents.Max();
                }

                Vector3 v = Quaternion.Inverse(rotation) * (a - position);

                return v.x <= extents.x && v.x > -extents.x && v.y <= extents.y && v.y > -extents.y && v.z <= extents.z && v.z > -extents.z;
            }
        }

        #region Configuration

        private static Configuration config;

        public class InterruptSettings
        {
            [JsonProperty(PropertyName = "Interrupt Teleport At Specific Monuments", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Monuments { get; set; } = new List<string>();

            [JsonProperty(PropertyName = "Above Water")]
            public bool AboveWater { get; set; } = true;

            [JsonProperty(PropertyName = "Under Water")]
            public bool UnderWater { get; set; }

            [JsonProperty(PropertyName = "Balloon")]
            public bool Balloon { get; set; } = true;

            [JsonProperty(PropertyName = "Boats")]
            public bool Boats { get; set; }

            [JsonProperty(PropertyName = "Cargo Ship")]
            public bool Cargo { get; set; } = true;

            [JsonProperty(PropertyName = "Cold")]
            public bool Cold { get; set; } = false;

            [JsonProperty(PropertyName = "Excavator")]
            public bool Excavator { get; set; } = false;

            [JsonProperty(PropertyName = "Hot")]
            public bool Hot { get; set; } = false;

            [JsonProperty(PropertyName = "Hostile")]
            public bool Hostile { get; set; } = false;

            [JsonProperty(PropertyName = "Hostile Includes Towns")]
            public bool IncludeHostileTown { get; set; } = true;

            [JsonProperty(PropertyName = "Hurt")]
            public bool Hurt { get; set; } = true;

            [JsonProperty(PropertyName = "Junkpiles")]
            public bool Junkpiles { get; set; }

            [JsonProperty(PropertyName = "Lift")]
            public bool Lift { get; set; } = true;

            [JsonProperty(PropertyName = "Monument")]
            public bool Monument { get; set; } = false;

            [JsonProperty(PropertyName = "Ignore Monument Marker Prefab")]
            public bool BypassMonumentMarker { get; set; } = false;

            [JsonProperty(PropertyName = "Mounted")]
            public bool Mounted { get; set; } = true;

            [JsonProperty(PropertyName = "Oil Rig")]
            public bool Oilrig { get; set; } = false;

            [JsonProperty(PropertyName = "Safe Zone")]
            public bool Safe { get; set; } = true;

            [JsonProperty(PropertyName = "Swimming")]
            public bool Swimming { get; set; } = false;
        }

        private void OnPlayerSleep(BasePlayer player)
        {
            DelayedTeleportHome(player);
        }

        private List<(Vector3 position, float sqrDistance)> safeZones = new();

        private bool IsSafeZone(Vector3 a, float extra = 0f) // SetUserTargetHitPos
        {
            if (safeZones.Count == 0)
            {
                foreach (var triggerSafeZone in TriggerSafeZone.allSafeZones)
                {
                    float radius = (triggerSafeZone.triggerCollider == null ? 25f : ColliderEx.GetRadius(triggerSafeZone.triggerCollider, triggerSafeZone.transform.localScale)) + extra;
                    Vector3 center = triggerSafeZone.triggerCollider?.bounds.center ?? triggerSafeZone.transform.position;
                    safeZones.Add((center, radius * radius));
                }
            }
            return safeZones.Exists(zone => (zone.position - a).sqrMagnitude <= zone.sqrDistance);
        }

        public bool IsAuthed(BasePlayer player, BuildingPrivlidge priv) => (priv.OwnerID == player.userID && config.Home.UsableIntoBuildingBlocked) || config.Home.UsableIntoBuildingBlocked || priv.IsAuthed(player);

        private List<ulong> delayedTeleports = new();

        public void DelayedTeleportHome(BasePlayer player)
        {
            ulong userid = player.userID;
            if (delayedTeleports.Contains(userid))
            {
                return;
            }
            delayedTeleports.Add(userid);
            timer.Once(0.2f, () =>
            {
                if (player.IsKilled() || player.IsDead() || player.IsConnected || !IsSafeZone(player.transform.position))
                {
                    delayedTeleports.Remove(userid);
                    return;
                }
                timer.Once(config.Settings.TeleportHomeSafeZone, () =>
                {
                    if (player.IsKilled() || player.IsDead() || player.IsConnected || !IsSafeZone(player.transform.position) || !delayedTeleports.Remove(userid))
                    {
                        return;
                    }
                    var homes = new List<(Vector3 position, BuildingManager.Building building, string home)>();
                    foreach (var (home, position) in GetPlayerHomes(player))
                    {
                        var priv = player.GetBuildingPrivilege(new OBB(position, player.transform.rotation, player.bounds));
                        if (priv.IsKilled() || !IsAuthed(player, priv))
                        {
                            continue;
                        }
                        var building = priv.GetBuilding();
                        if (building == null || !building.HasDecayEntities())
                        {
                            continue;
                        }
                        homes.Add((position, building, home));
                    }
                    if (homes.Count == 0)
                    {
                        foreach (var bed in SleepingBag.FindForPlayer(userid, true))
                        {
                            var building = bed.GetBuilding();
                            if (building == null || !building.HasDecayEntities())
                            {
                                continue;
                            }
                            homes.Add((bed.transform.position, building, bed.niceName));
                        }
                    }
                    if (homes.Count > 0)
                    {
                        homes.Sort((x, y) => x.building.decayEntities.Count.CompareTo(y.building.decayEntities.Count));
                        Puts($"Teleporting {player.displayName} ({player.userID}) in safe zone from {player.transform.position} to home ({homes[0].home}) at {homes[0].position}");
                        Teleport(player, homes[0].position, "home", 0uL, false, false, true, true, true);
                    }
                });
            });
        }

        private object OnPlayerRespawn(BasePlayer player)
        {
            if (player == null) return null;

            var settings = GetSettings("outpost");

            if (settings == null || settings.Location == Vector3.zero) return null;

            if (Interface.Oxide.CallHook("CanTeleport", player, settings.Location) != null) return null;

            if (player.State.unHostileTimestamp > TimeEx.currentTimestamp)
            {
                player.State.unHostileTimestamp = TimeEx.currentTimestamp;
                player.DirtyPlayerState();
                player.ClientRPC(RpcTarget.Player("SetHostileLength", player), 0f);
            }

            if (player.unHostileTime > UnityEngine.Time.realtimeSinceStartup)
            {
                player.unHostileTime = UnityEngine.Time.realtimeSinceStartup;
            }

            return new BasePlayer.SpawnPoint { pos = settings.Location, rot = Quaternion.identity };
        }

        public class PluginSettings
        {
            [JsonProperty(PropertyName = "Delay Saving Data On Server Save")]
            public double SaveDelay { get; set; } = 4.0;

            [JsonProperty("TPB")]
            public TPBSettings TPB = new TPBSettings();

            [JsonProperty(PropertyName = "Interrupt TP")]
            public InterruptSettings Interrupt { get; set; } = new InterruptSettings();

            [JsonProperty(PropertyName = "Auto Wake Up After Teleport")]
            public bool AutoWakeUp { get; set; }

            [JsonProperty(PropertyName = "Seconds Until Teleporting Home Offline Players Within SafeZones")]
            public float TeleportHomeSafeZone { get; set; }

            [JsonProperty(PropertyName = "Respawn Players At Outpost")]
            public bool RespawnOutpost { get; set; }

            [JsonProperty(PropertyName = "Block Teleport (NoEscape)")]
            public bool BlockNoEscape { get; set; }

            [JsonProperty(PropertyName = "Block Teleport (RaidBlock)")]
            public bool RaidBlock { get; set; }

            [JsonProperty(PropertyName = "Block Teleport (ZoneManager)")]
            public bool BlockZoneFlag { get; set; }

            [JsonProperty(PropertyName = "Block Map Marker Teleport (AbandonedBases)")]
            public bool BlockAbandoned { get; set; }

            [JsonProperty(PropertyName = "Block Map Marker Teleport (RaidableBases)")]
            public bool BlockRaidable { get; set; }

            [JsonProperty(PropertyName = "Chat Name")]
            public string ChatName { get; set; } = "<color=red>Teleportation</color> \n\n";

            [JsonProperty(PropertyName = "Chat Steam64ID")]
            public ulong ChatID { get; set; } = 76561199056025689;

            [JsonProperty(PropertyName = "Check Boundaries On Teleport X Y Z")]
            public bool CheckBoundaries { get; set; } = true;

            [JsonProperty(PropertyName = "Check Boundaries Min Height")]
            public float BoundaryMin { get; set; } = -100f;

            [JsonProperty(PropertyName = "Check Boundaries Max Height")]
            public float BoundaryMax { get; set; } = 2000f;

            [JsonProperty(PropertyName = "Check If Inside Rock")]
            public bool Rock { get; set; } = true;

            [JsonProperty(PropertyName = "Height To Prevent Teleporting To/From (0 = disabled)")]
            public float ForcedBoundary { get; set; }

            [JsonProperty(PropertyName = "Data File Directory (Blank = Default)")]
            public string DataFileFolder { get; set; } = string.Empty;

            [JsonProperty(PropertyName = "Draw Sphere On Set Home")]
            public bool DrawHomeSphere { get; set; } = true;

            [JsonProperty(PropertyName = "Homes Enabled")]
            public bool HomesEnabled { get; set; } = true;

            [JsonProperty(PropertyName = "TPR Enabled")]
            public bool TPREnabled { get; set; } = true;

            [JsonProperty(PropertyName = "Strict Foundation Check")]
            public bool StrictFoundationCheck { get; set; } = false;

            [JsonProperty(PropertyName = "Minimum Temp")]
            public float MinimumTemp { get; set; } = 0f;

            [JsonProperty(PropertyName = "Maximum Temp")]
            public float MaximumTemp { get; set; } = 40f;

            [JsonProperty(PropertyName = "Blocked Items", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, string> BlockedItems { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            //{
            //    ["explosive.timed"] = "You cannot teleport with C4!",
            //    ["gunpowder"] = "You cannot teleport with gun powder!",
            //    ["explosives"] = "You cannot teleport with explosives!",
            //};

            [JsonProperty(PropertyName = "Blocked Town Prefabs", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BlockedPrefabs { get; set; } = new();

            [JsonProperty(PropertyName = "Bypass CMD")]
            public string BypassCMD { get; set; } = "pay";

            [JsonProperty(PropertyName = "Use Economics")]
            public bool UseEconomics { get; set; } = false;

            [JsonProperty(PropertyName = "Use Server Rewards")]
            public bool UseServerRewards { get; set; } = false;

            [JsonProperty(PropertyName = "Wipe On Upgrade Or Change")]
            public bool WipeOnUpgradeOrChange { get; set; } = true;

            [JsonProperty(PropertyName = "Auto Generate Outpost Location")]
            public bool AutoGenOutpost { get; set; } = true;

            [JsonProperty(PropertyName = "Outpost Map Prefab", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Outpost { get; set; } = new List<string> { "outpost", "compound" };

            [JsonProperty(PropertyName = "Auto Generate Bandit Location")]
            public bool AutoGenBandit { get; set; } = true;

            [JsonProperty(PropertyName = "Bandit Map Prefab", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Bandit { get; set; } = new List<string> { "bandit_town" };

            [JsonProperty(PropertyName = "Show Time As Seconds Instead")]
            public bool UseSeconds { get; set; } = false;

            [JsonProperty(PropertyName = "Use Quick Teleport")]
            public bool Quick { get; set; } = true;

            [JsonProperty(PropertyName = "Chat Command Color")]
            public string ChatCommandColor = "#FFFF00";

            [JsonProperty(PropertyName = "Chat Command Argument Color")]
            public string ChatCommandArgumentColor = "#FFA500";

            [JsonProperty("Enable Popup Support")]
            public bool UsePopup = false;

            [JsonProperty("Send Messages To Player")]
            public bool SendMessages = true;

            [JsonProperty("Block All Teleporting From Inside Authorized Base")]
            public bool BlockAuthorizedTeleporting = false;

            [JsonProperty("Global Teleport Cooldown")]
            public float Global = 0f;

            [JsonProperty("Global VIP Teleport Cooldown")]
            public float GlobalVIP = 0f;

            [JsonProperty("Play Sounds Before Teleport")]
            public bool PlaySoundsBeforeTeleport = true;

            [JsonProperty("Sound Effects Before Teleport", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> DisappearEffects = new List<string>
            {
                "assets/prefabs/missions/portal/proceduraldungeon/effects/disappear.prefab"
            };

            [JsonProperty("Play Sounds After Teleport")]
            public bool PlaySoundsAfterTeleport = true;

            [JsonProperty("Sound Effects After Teleport", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> ReappearEffects = new List<string>
            {
                "assets/prefabs/missions/portal/proceduraldungeon/effects/appear.prefab"
            };
        }

        public class TPBSettings
        {
            [JsonProperty(PropertyName = "VIP Countdowns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> Countdowns { get; set; } = new Dictionary<string, int> { { ConfigDefaultPermVip, 0 } };

            [JsonProperty("Countdown")]
            public int Countdown = 0;

            [JsonProperty("Available After X Seconds")]
            public int Time = 0;
        }

        public class AdminSettings
        {
            [JsonProperty(PropertyName = "Announce Teleport To Target")]
            public bool AnnounceTeleportToTarget { get; set; } = false;

            [JsonProperty(PropertyName = "Usable By Admins")]
            public bool UseableByAdmins { get; set; } = true;

            [JsonProperty(PropertyName = "Usable By Moderators")]
            public bool UseableByModerators { get; set; } = true;

            [JsonProperty(PropertyName = "Location Radius")]
            public int LocationRadius { get; set; } = 25;

            [JsonProperty(PropertyName = "Teleport Near Default Distance")]
            public int TeleportNearDefaultDistance { get; set; } = 30;

            [JsonProperty(PropertyName = "Extra Distance To Block Monument Teleporting")]
            public int ExtraMonumentDistance { get; set; }
        }

        public class HomesSettings
        {
            [JsonProperty(PropertyName = "Homes Limit")]
            public int HomesLimit { get; set; } = 2;

            [JsonProperty(PropertyName = "VIP Homes Limits", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> VIPHomesLimits { get; set; } = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };

            [JsonProperty(PropertyName = "Allow Sethome At Specific Monuments", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> AllowedMonuments { get; set; } = new List<string> { "HQM Quarry", "Stone Quarry", "Sulfur Quarry", "Ice Lake", "Wild Swamp" };

            [JsonProperty(PropertyName = "Allow Sethome At All Monuments")]
            public bool AllowAtAllMonuments { get; set; } = false;

            [JsonProperty(PropertyName = "Allow Sethome On Tugboats")]
            public bool AllowTugboats { get; set; } = true;

            [JsonProperty(PropertyName = "Allow TPB")]
            public bool AllowTPB { get; set; } = true;

            [JsonProperty(PropertyName = "Cooldown")]
            public int Cooldown { get; set; } = 600;

            [JsonProperty(PropertyName = "Countdown")]
            public int Countdown { get; set; } = 15;

            [JsonProperty(PropertyName = "Daily Limit")]
            public int DailyLimit { get; set; } = 5;

            [JsonProperty(PropertyName = "VIP Daily Limits", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> VIPDailyLimits { get; set; } = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };

            [JsonProperty(PropertyName = "VIP Cooldowns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> VIPCooldowns { get; set; } = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };

            [JsonProperty(PropertyName = "VIP Countdowns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> VIPCountdowns { get; set; } = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };

            [JsonProperty(PropertyName = "Location Radius")]
            public int LocationRadius { get; set; } = 25;

            [JsonProperty(PropertyName = "Force On Top Of Foundation")]
            public bool ForceOnTopOfFoundation { get; set; } = true;

            [JsonProperty(PropertyName = "Check Foundation For Owner")]
            public bool CheckFoundationForOwner { get; set; } = true;

            [JsonProperty(PropertyName = "Use Friends")]
            public bool UseFriends { get; set; } = true;

            [JsonProperty(PropertyName = "Use Clans")]
            public bool UseClans { get; set; } = true;

            [JsonProperty(PropertyName = "Use Teams")]
            public bool UseTeams { get; set; } = true;

            [JsonProperty(PropertyName = "Usable Out Of Building Blocked")]
            public bool UsableOutOfBuildingBlocked { get; set; } = false;

            [JsonProperty(PropertyName = "Usable Into Building Blocked")]
            public bool UsableIntoBuildingBlocked { get; set; } = false;

            [JsonProperty(PropertyName = "Usable From Safe Zone Only")]
            public bool UsableFromSafeZoneOnly { get; set; } = false;

            [JsonProperty(PropertyName = "Allow Cupboard Owner When Building Blocked")]
            public bool CupOwnerAllowOnBuildingBlocked { get; set; } = true;

            [JsonProperty(PropertyName = "Allow Iceberg")]
            public bool AllowIceberg { get; set; } = false;

            [JsonProperty(PropertyName = "Allow Cave")]
            public bool AllowCave { get; set; }

            [JsonProperty(PropertyName = "Allow Crafting")]
            public bool AllowCraft { get; set; } = false;

            [JsonProperty(PropertyName = "Allow Above Foundation")]
            public bool AllowAboveFoundation { get; set; } = true;

            [JsonProperty(PropertyName = "Check If Home Is Valid On Listhomes")]
            public bool CheckValidOnList { get; set; } = false;

            [JsonProperty(PropertyName = "Pay")]
            public int Pay { get; set; } = 0;

            [JsonProperty(PropertyName = "Bypass")]
            public int Bypass { get; set; } = 0;

            [JsonProperty(PropertyName = "Hours Before Useable After Wipe")]
            public double Hours { get; set; } = 0;
        }

        public class TPTSettings
        {
            [JsonProperty(PropertyName = "Use Friends")]
            public bool UseFriends { get; set; }

            [JsonProperty(PropertyName = "Use Clans")]
            public bool UseClans { get; set; }

            [JsonProperty(PropertyName = "Use Teams")]
            public bool UseTeams { get; set; }

            [JsonProperty(PropertyName = "Allow Cave")]
            public bool AllowCave { get; set; }
            [JsonProperty(PropertyName = "Enabled Color")]
            public string EnabledColor { get; set; } = "green";

            [JsonProperty(PropertyName = "Disabled Color")]
            public string DisabledColor { get; set; } = "red";
        }

        // Added `TPR => Play Sounds To Request Target` (false)
        // Added `TPR => Play Sounds When Target Accepts` (false)

        public class DiscordSettings
        {
            [JsonProperty(PropertyName = "Webhook URL")]
            public string Webhook = "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

            [JsonProperty(PropertyName = "Log When TPA To Non-Ally Players More Than X Times")]
            public int TPA = 3;
        }

        public class TPRSettings
        {
            [JsonProperty(PropertyName = "Discord")]
            public DiscordSettings Discord { get; set; } = new();

            [JsonProperty("Play Sounds To Request Target")]
            public bool PlaySoundsToRequestTarget;

            [JsonProperty("Teleport Request Sound Effects", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> TeleportRequestEffects = new List<string>
            {
                "assets/prefabs/missions/portal/proceduraldungeon/effects/disappear.prefab"
            };

            [JsonProperty("Play Sounds When Target Accepts")]
            public bool PlaySoundsWhenTargetAccepts;

            [JsonProperty("Teleport Accept Sound Effects", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> TeleportAcceptEffects = new List<string>
            {
                "assets/prefabs/missions/portal/proceduraldungeon/effects/appear.prefab"
            };

            [JsonProperty(PropertyName = "Require Player To Be Friend, Clan Mate, Or Team Mate")]
            public bool UseClans_Friends_Teams { get; set; }

            [JsonProperty(PropertyName = "Require nteleportation.tpa to accept TPR requests")]
            public bool RequireTPAPermission { get; set; }

            [JsonProperty(PropertyName = "Allow Cave")]
            public bool AllowCave { get; set; }

            [JsonProperty(PropertyName = "Allow TPB")]
            public bool AllowTPB { get; set; } = true;

            [JsonProperty(PropertyName = "Use Blocked Users")]
            public bool UseBlockedUsers { get; set; } = false;

            [JsonProperty(PropertyName = "Cooldown")]
            public int Cooldown { get; set; } = 600;

            [JsonProperty(PropertyName = "Countdown")]
            public int Countdown { get; set; } = 15;

            [JsonProperty(PropertyName = "Daily Limit")]
            public int DailyLimit { get; set; } = 5;

            [JsonProperty(PropertyName = "VIP Daily Limits", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> VIPDailyLimits { get; set; } = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };

            [JsonProperty(PropertyName = "VIP Cooldowns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> VIPCooldowns { get; set; } = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };

            [JsonProperty(PropertyName = "VIP Countdowns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> VIPCountdowns { get; set; } = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };

            [JsonProperty(PropertyName = "Enable Request UI")]
            public bool UI { get; set; }

            [JsonProperty(PropertyName = "Request Duration")]
            public int RequestDuration { get; set; } = 30;

            [JsonProperty(PropertyName = "Block TPA On Ceiling")]
            public bool BlockTPAOnCeiling { get; set; } = true;

            [JsonProperty(PropertyName = "Usable Out Of Building Blocked")]
            public bool UsableOutOfBuildingBlocked { get; set; } = false;

            [JsonProperty(PropertyName = "Usable Into Building Blocked")]
            public bool UsableIntoBuildingBlocked { get; set; } = false;

            [JsonProperty(PropertyName = "Allow Cupboard Owner When Building Blocked")]
            public bool CupOwnerAllowOnBuildingBlocked { get; set; } = true;

            [JsonProperty(PropertyName = "Allow Crafting")]
            public bool AllowCraft { get; set; } = false;

            [JsonProperty(PropertyName = "Pay")]
            public int Pay { get; set; } = 0;

            [JsonProperty(PropertyName = "Bypass")]
            public int Bypass { get; set; } = 0;

            [JsonProperty(PropertyName = "Hours Before Useable After Wipe")]
            public double Hours { get; set; } = 0;
        }

        public class TownSettings
        {
            [JsonProperty(PropertyName = "Command Enabled")]
            public bool Enabled { get; set; } = true;

            [JsonProperty(PropertyName = "Set Position From Monument Marker Name")]
            public string MonumentMarkerName { get; set; } = "";

            [JsonProperty(PropertyName = "Set Position From Monument Marker Name Offset")]
            public string MonumentMarkerNameOffset { get; set; } = "0 0 0";

            [JsonProperty(PropertyName = "Allow TPB")]
            public bool AllowTPB { get; set; } = true;

            [JsonProperty(PropertyName = "Allow Cave")]
            public bool AllowCave { get; set; }

            [JsonProperty(PropertyName = "Cooldown")]
            public int Cooldown { get; set; } = 600;

            [JsonProperty(PropertyName = "Countdown")]
            public int Countdown { get; set; } = 15;

            [JsonProperty(PropertyName = "Daily Limit")]
            public int DailyLimit { get; set; } = 5;

            [JsonProperty(PropertyName = "VIP Daily Limits", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> VIPDailyLimits { get; set; } = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };

            [JsonProperty(PropertyName = "VIP Cooldowns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> VIPCooldowns { get; set; } = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };

            [JsonProperty(PropertyName = "VIP Countdowns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> VIPCountdowns { get; set; } = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } };

            [JsonProperty(PropertyName = "Location")]
            public Vector3 Location { get; set; } = Vector3.zero;

            [JsonProperty(PropertyName = "Locations", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<Vector3> Locations { get; set; } = new List<Vector3>();

            [JsonProperty(PropertyName = "Teleport To Random Location")]
            public bool Random { get; set; } = true;

            [JsonProperty(PropertyName = "Usable Out Of Building Blocked")]
            public bool UsableOutOfBuildingBlocked { get; set; } = false;

            [JsonProperty(PropertyName = "Allow Crafting")]
            public bool AllowCraft { get; set; } = false;

            [JsonProperty(PropertyName = "Pay")]
            public int Pay { get; set; } = 0;

            [JsonProperty(PropertyName = "Bypass")]
            public int Bypass { get; set; } = 0;

            [JsonProperty(PropertyName = "Hours Before Useable After Wipe")]
            public double Hours { get; set; } = 0;

            public bool CanCraft(BasePlayer player, string command)
            {
                return AllowCraft || player.IPlayer.HasPermission($"nteleportation.craft{command.ToLower()}");
            }

            public bool CanCave(BasePlayer player, string command)
            {
                return AllowCave || player.IPlayer.HasPermission($"nteleportation.cave{command.ToLower()}");
            }

            [JsonIgnore]
            public StoredData Teleports = new StoredData();

            [JsonIgnore]
            public string Command { get; set; }
        }

        private class Configuration
        {
            [JsonProperty(PropertyName = "Settings")]
            public PluginSettings Settings = new PluginSettings();

            [JsonProperty(PropertyName = "Admin")]
            public AdminSettings Admin = new AdminSettings();

            [JsonProperty(PropertyName = "Home")]
            public HomesSettings Home = new HomesSettings();

            [JsonProperty(PropertyName = "TPT")]
            public TPTSettings TPT = new TPTSettings();

            [JsonProperty(PropertyName = "TPR")]
            public TPRSettings TPR = new TPRSettings();

            [JsonProperty(PropertyName = "Dynamic Commands", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, TownSettings> DynamicCommands { get; set; } = DefaultCommands;
        }

        private static Dictionary<string, TownSettings> DefaultCommands = new Dictionary<string, TownSettings>
        {
            ["Town"] = new TownSettings() { Random = false },
            ["Island"] = new TownSettings() { AllowTPB = false },
            ["Outpost"] = new TownSettings(),
            ["Bandit"] = new TownSettings(),
        };

        public void InitializeDynamicCommands()
        {
            foreach (var entry in config.DynamicCommands)
            {
                if (!entry.Value.Enabled)
                {
                    continue;
                }
                else if (entry.Key.Equals("bandit", StringComparison.OrdinalIgnoreCase))
                {
                    if (CompoundTeleport == null || Convert.ToBoolean(CompoundTeleport?.Call("umodversion")))
                    {
                        banditEnabled = true;
                    }
                    else continue;
                }
                else if (entry.Key.Equals("outpost", StringComparison.OrdinalIgnoreCase))
                {
                    if (CompoundTeleport == null || Convert.ToBoolean(CompoundTeleport?.Call("umodversion")))
                    {
                        outpostEnabled = true;
                    }
                    else continue;
                }

                entry.Value.Command = entry.Key;
                RegisterCommand(entry.Key, nameof(CommandCustom));
            }

            RegisterCommand("ntp", nameof(CommandDynamic));
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            canSaveConfig = false;
            try
            {
                Config.Settings.Converters = new JsonConverter[] { new UnityVector3Converter() };
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
                canSaveConfig = true;
                SaveConfig();
            }
            catch (JsonException ex)
            {
                Puts(ex.ToString());
                LoadDefaultConfig();
            }
            config.Settings.ReappearEffects.Remove("");
            config.Settings.DisappearEffects.Remove("");
            config.TPR.TeleportRequestEffects.Remove("");
            config.TPR.TeleportAcceptEffects.Remove("");
        }

        private bool canSaveConfig = true;

        protected override void SaveConfig()
        {
            if (canSaveConfig)
            {
                Config.WriteObject(config);
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            Puts("Loaded default configuration.");
        }

        #endregion

        private class DisabledData
        {
            [JsonProperty("List of disabled commands")]
            public List<string> DisabledCommands = new List<string>();

            public DisabledData() { }
        }

        private DisabledData DisabledCommandData = new();

        private class AdminData
        {
            [JsonProperty("t")]
            public bool Town { get; set; }

            [JsonProperty("u")]
            public ulong UserID { get; set; }

            [JsonProperty("d")]
            public string Home { get; set; }

            [JsonProperty("pl")]
            public Vector3 PreviousLocation { get; set; }

            [JsonProperty("b")]
            public bool BuildingBlocked { get; set; }

            [JsonProperty("c")]
            public bool AllowCrafting { get; set; }

            [JsonProperty("cv")]
            public bool AllowCave { get; set; }

            [JsonProperty("l")]
            public Dictionary<string, Vector3> Locations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private class HomeData
        {
            public class Entry
            {
                public Vector3 Position;
                public BaseNetworkable Entity;
                public bool isEntity => !Entity.IsKilled();
                public bool wasEntity;
                public Entry() { }
                public Entry(Vector3 Position)
                {
                    this.Position = Position;
                }
                public Vector3 Get()
                {
                    if (isEntity)
                    {
                        return Entity.transform.position + Position;
                    }
                    return Position;
                }
            }

            public class Boat
            {
                public ulong Value;
                public Vector3 Offset;
                public Boat() { }
                public Boat(Entry entry)
                {
                    Offset = entry.Position;
                    Value = entry.Entity.net.ID.Value;
                }
            }

            [JsonProperty("l")]
            public Dictionary<string, Vector3> buildings { get; set; } = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);

            [JsonProperty("b")]
            public Dictionary<string, Boat> boats { get; set; } = new Dictionary<string, Boat>(StringComparer.OrdinalIgnoreCase);

            [JsonProperty("t")]
            public TeleportData Teleports { get; set; } = new TeleportData();

            [JsonIgnore]
            private Dictionary<string, Entry> Cache = new Dictionary<string, Entry>();

            [JsonIgnore]
            public Dictionary<string, Entry> Locations
            {
                get
                {
                    if (Cache.Count == 0)
                    {
                        InitializeBuildings();
                        InitializeBoats();
                    }
                    return Cache;
                }
            }

            private void InitializeBuildings()
            {
                foreach (var pair in buildings)
                {
                    Cache[pair.Key] = new Entry(pair.Value);
                }
            }

            private void InitializeBoats()
            {
                foreach (var (key, boat) in boats.ToList())
                {
                    var entity = BaseNetworkable.serverEntities.Find(new NetworkableId(boat.Value));
                    if (entity.IsKilled()) continue;
                    Cache[key] = new Entry
                    {
                        Position = boat.Offset,
                        wasEntity = true,
                        Entity = entity
                    };
                }
            }

            public bool TryGetValue(string key, out Entry homeEntry)
            {
                return Locations.TryGetValue(key, out homeEntry);
            }

            public void Set(string key, Entry homeEntry)
            {
                Locations[key] = homeEntry;
                if (homeEntry.isEntity)
                {
                    boats[key] = new Boat(homeEntry);
                }
                else buildings[key] = homeEntry.Get();
            }

            public bool Remove(string key)
            {
                bool removed = boats.Remove(key) || buildings.Remove(key);
                return Locations.Remove(key) || removed;
            }
        }

        public class TeleportData
        {
            [JsonProperty("a")]
            public int Amount { get; set; }

            [JsonProperty("d")]
            public string Date { get; set; }

            [JsonProperty("t")]
            public int Timestamp { get; set; }
        }

        private class TeleportTimer
        {
            public string Town { get; set; } = "";
            public ulong UserID { get; set; }
            public string Home { get; set; } = "";
            public Timer Timer { get; set; }
            public BasePlayer OriginPlayer { get; set; }
            public BasePlayer TargetPlayer { get; set; }
        }

        private List<string> GetMonumentMessages()
        {
            return new List<string>
            {
                "Abandoned Cabins",
                "Abandoned Supermarket",
                "Abandoned Military Base",
                "Airfield",
                "Arctic Research Base",
                "Bandit Camp",
                "Barn",
                "Crow's Nest",
                "Ferry Terminal",
                "Fishing Village",
                "Gas Station",
                "Giant Excavator Pit",
                "Harbor",
                "HQM Quarry",
                "Ice Lake",
                "Junkyard",
                "Large Barn",
                "Large Fishing Village",
                "Launch Site",
                "Lighthouse",
                "Military Tunnel",
                "Mining Outpost",
                "Missile Silo",
                "Mountain",
                "Oil Rig",
                "Large Oil Rig",
                "Outpost",
                "Oxum's Gas Station",
                "Power Plant",
                "Ranch",
                "Radtown",
                "Satellite Dish",
                "Sewer Branch",
                "Stone Quarry",
                "Substation",
                "Sulfur Quarry",
                "The Supermarket",
                "The Dome",
                "Train Tunnel",
                "Train Yard",
                "Underwater Lab",
                "Water Treatment Plant",
                "Water Well",
                "Wild Swamp",
            };
        }

        protected override void LoadDefaultMessages()
        {
            if (!_cmcCompleted)
            {
                timer.Once(1f, LoadDefaultMessages);
                return;
            }

            var monumentMessages = GetMonumentMessages();

            var en = new Dictionary<string, string>
            {
                {"ErrorTPR", "Teleporting to {0} is blocked ({1})"},
                {"AdminTP", "You teleported to {0}!"},
                {"AdminTPTarget", "{0} teleported to you!"},
                {"AdminTPPlayers", "You teleported {0} to {1}!"},
                {"AdminTPPlayer", "{0} teleported you to {1}!"},
                {"AdminTPPlayerTarget", "{0} teleported {1} to you!"},
                {"AdminTPCoordinates", "You teleported to {0}!"},
                {"AdminTPTargetCoordinates", "You teleported {0} to {1}!"},
                {"AdminTPOutOfBounds", "You tried to teleport to a set of coordinates outside the map boundaries!"},
                {"AdminTPBoundaries", "X and Z values need to be between -{0} and {0} while the Y value needs to be between -100 and 2000!"},
                {"AdminTPLocation", "You teleported to {0}!"},
                {"AdminTPLocationSave", "You have saved the current location!"},
                {"AdminTPLocationRemove", "You have removed the location {0}!"},
                {"AdminLocationList", "The following locations are available:"},
                {"AdminLocationListEmpty", "You haven't saved any locations!"},
                {"AdminTPBack", "You've teleported back to your previous location!"},
                {"AdminTPBackSave", "Your previous location has been saved, use /tpb to teleport back!"},
                {"AdminTPTargetCoordinatesTarget", "{0} teleported you to {1}!"},
                {"AdminTPConsoleTP", "You were teleported to {0}"},
                {"AdminTPConsoleTPPlayer", "You were teleported to {0}"},
                {"AdminTPConsoleTPPlayerTarget", "{0} was teleported to you!"},
                {"HomeTP", "You teleported to your home '{0}'!"},
                {"HomeAdminTP", "You teleported to {0}'s home '{1}'!"},
                {"HomeIce", "You can't use home on ice!"},
                {"HomeSave", "You have saved the current location as your home!"},
                {"HomeNoFoundation", "You can only use a home location on a foundation!"},
                {"HomeFoundationNotOwned", "You can't use home on someone else's house."},
                {"HomeFoundationUnderneathFoundation", "You can't use home on a foundation that is underneath another foundation."},
                {"HomeFoundationNotFriendsOwned", "You or a friend need to own the house to use home!"},
                {"HomeRemovedInvalid", "Your home '{0}' was removed because not on a foundation or not owned!"},
                {"HighWallCollision", "High Wall Collision!"},
                {"HomeRemovedDestroyed", "Your home '{0}' was removed because it no longer exists!"},
                {"HomeRemovedInsideBlock", "Your home '{0}' was removed because inside a foundation!"},
                {"HomeRemove", "You have removed your home {0}!"},
                {"HomeDelete", "You have removed {0}'s home '{1}'!"},
                {"HomeList", "The following homes are available:"},
                {"HomeListEmpty", "You haven't saved any homes!"},
                {"HomeMaxLocations", "Unable to set your home here, you have reached the maximum of {0} homes!"},
                {"HomeQuota", "You have set {0} of the maximum {1} homes!"},
                {"HomeTugboatNotAllowed", "You are not allowed to sethome on tugboats."},
                {"HomeTPStarted", "Teleporting to your home {0} in {1} seconds!"},
                {"PayToTown", "Standard payment of {0} applies to all {1} teleports!"},
                {"PayToTPR", "Standard payment of {0} applies to all tprs!"},
                {"HomeTPCooldown", "Your teleport is currently on cooldown. You'll have to wait {0} for your next teleport."},
                {"HomeTPCooldownBypass", "Your teleport was currently on cooldown. You chose to bypass that by paying {0} from your balance."},
                {"HomeTPCooldownBypassF", "Your teleport is currently on cooldown. You do not have sufficient funds - {0} - to bypass."},
                {"HomeTPCooldownBypassP", "You may choose to pay {0} to bypass this cooldown." },
                {"HomeTPCooldownBypassP2", "Type /home NAME {0}." },
                {"HomeTPLimitReached", "You have reached the daily limit of {0} teleports today!"},
                {"HomeTPAmount", "You have {0} home teleports left today!"},
                {"HomesListWiped", "You have wiped all the saved home locations!"},
                {"HomeTPBuildingBlocked", "You can't set your home if you are not allowed to build in this zone!"},
                {"HomeTPSwimming", "You can't set your home while swimming!"},
                {"HomeTPCrafting", "You can't set your home while crafting!"},
                {"Request", "You've requested a teleport to {0}!"},
                {"RequestUI", "<size=14><color=#FFA500>TP Request:\n</color> {0}</size>"},
                {"RequestTarget", "{0} requested to be teleported to you! Use '/tpa' to accept!"},
                {"RequestTargetOff", "Your request has been cancelled as the target is offline now." },
                {"RequestAccept", "<size=12>Accept</size>" },
                {"RequestReject", "<size=12>Reject</size>" },
                {"TPR_NoClan_NoFriend_NoTeam", "This command is only available to friends or teammates or clanmates!"},
                {"PendingRequest", "You already have a request pending, cancel that request or wait until it gets accepted or times out!"},
                {"PendingRequestTarget", "The player you wish to teleport to already has a pending request, try again later!"},
                {"NoPendingRequest", "You have no pending teleport request!"},
                {"Accept", "{0} has accepted your teleport request! Teleporting in {1} seconds!"},
                {"AcceptTarget", "You've accepted the teleport request of {0}!"},
                {"AcceptToggleOff", "You've disabled automatic /tpa!"},
                {"AcceptToggleOn", "You've enabled automatic /tpa!"},
                {"NotAllowed", "You are not allowed to use this command!"},
                {"Success", "You teleported to {0}!"},
                {"SuccessTarget", "{0} teleported to you!"},
                {"BlockedTeleportTarget", "You can't teleport to user \"{0}\", they have you teleport blocked!"},
                {"Cancelled", "Your teleport request to {0} was cancelled!"},
                {"CancelledTarget", "{0} teleport request was cancelled!"},
                {"TPCancelled", "Your teleport was cancelled!"},
                {"TPCancelledTarget", "{0} cancelled teleport!"},
                {"TPYouCancelledTarget", "You cancelled {0} teleport!"},
                {"TimedOut", "{0} did not answer your request in time!"},
                {"TimedOutTarget", "You did not answer {0}'s teleport request in time!"},
                {"TargetDisconnected", "{0} has disconnected, your teleport was cancelled!"},
                {"TPRCooldown", "Your teleport requests are currently on cooldown. You'll have to wait {0} to send your next teleport request."},
                {"TPRCooldownBypass", "Your teleport request was on cooldown. You chose to bypass that by paying {0} from your balance."},
                {"TPRCooldownBypassF", "Your teleport is currently on cooldown. You do not have sufficient funds - {0} - to bypass."},
                {"TPRCooldownBypassP", "You may choose to pay {0} to bypass this cooldown." },
                {"TPMoney", "{0} deducted from your account!"},
                {"TPNoMoney", "You do not have {0} in any account!"},
                {"TPRCooldownBypassP2", "Type /tpr {0}." },
                {"TPRCooldownBypassP2a", "Type /tpr NAME {0}." },
                {"TPRLimitReached", "You have reached the daily limit of {0} teleport requests today!"},
                {"TPRAmount", "You have {0} teleport requests left today!"},
                {"TPRTarget", "Your target is currently not available!"},
                {"TPRNoCeiling", "You cannot teleport while your target is on a ceiling!"},
                {"TPDead", "You can't teleport while being dead!"},
                {"TPWounded", "You can't teleport while wounded!"},
                {"TPTooCold", "You're too cold to teleport!"},
                {"TPTooHot", "You're too hot to teleport!"},
                {"TPBoat", "You can't teleport while on a boat!"},
                {"TPTugboat", "You can't teleport while on a tugboat!"},
                {"TPHostile", "Can't teleport to outpost or bandit when hostile!"},
                {"TPJunkpile", "You can't teleport from a junkpile!"},
                {"HostileTimer", "Teleport available in {0} minutes."},
                {"TPMounted", "You can't teleport while seated!"},
                {"TPBuildingBlocked", "You can't teleport while in a building blocked area!"},
                {"TPAboveWater", "You can't teleport while above water!"},
                {"TPUnderWater", "You can't teleport while under water!"},
                {"TPTargetBuildingBlocked", "You can't teleport into a building blocked area!"},
                {"TPTargetInsideBlock", "You can't teleport into a foundation!"},
                {"TPTargetInsideEntity", "You can't teleport into another entity!"},
                {"TPTargetInsideRock", "You can't teleport into a rock!"},
                {"TPSwimming", "You can't teleport while swimming!"},
                {"TPCargoShip", "You can't teleport from the cargo ship!"},
                {"TPOilRig", "You can't teleport from the oil rig!"},
                {"TPExcavator", "You can't teleport from the excavator!"},
                {"TPHotAirBalloon", "You can't teleport to or from a hot air balloon!"},
                {"TPLift", "You can't teleport while in an elevator or bucket lift!"},
                {"TPBucketLift", "You can't teleport while in a bucket lift!"},
                {"TPRegLift", "You can't teleport while in an elevator!"},
                {"TPSafeZone", "You can't teleport from a safezone!"},
                {"TPFlagZone", "You can't teleport from this zone!"},
                {"TPNoEscapeBlocked", "You can't teleport while blocked!"},
                {"TPCrafting", "You can't teleport while crafting!"},
                {"TPBlockedItem", "You can't teleport while carrying: {0}!"},
                {"TPHomeSafeZoneOnly", "You can only teleport home from within a safe zone!" },
                {"TooCloseToMon", "You can't teleport so close to the {0}!"},
                {"TooCloseToCave", "You can't teleport so close to a cave!"},
                {"HomeTooCloseToCave", "You can't set home so close to a cave!"},
                {"HomeTooCloseToMon", "You can't set home so close to a monument!"},
                {"CannotTeleportFromHome", "You must leave your base to be able to teleport!"},
                {"WaitGlobalCooldown", "You must wait {0} on your global teleport cooldown!" },
                {"DM_TownTP", "You teleported to {0}!"},
                {"DM_TownTPNoLocation", "<color=yellow>{0}</color> location is currently not set!"},
                {"DM_TownTPDisabled", "<color=yellow>{0}</color> is currently disabled in config file!"},
                {"DM_TownTPLocation", "You have set the <color=yellow>{0}</color> location to {1}!"},
                {"DM_TownTPCreated", "You have created the command: <color=yellow>{0}</color>"},
                {"DM_TownTPRemoved", "You have removed the command: <color=yellow>{0}</color>"},
                {"DM_TownTPDoesNotExist", "Command does not exist: <color=yellow>{0}</color>"},
                {"DM_TownTPExists", "Command <color=yellow>{0}</color> already exists!"},
                {"DM_TownTPLocationsCleared", "You have cleared all locations for {0}!"},
                {"DM_TownTPStarted", "Teleporting to {0} in {1} seconds!"},
                {"DM_TownTPCooldown", "Your teleport is currently on cooldown. You'll have to wait {0} for your next teleport."},
                {"DM_TownTPCooldownBypass", "Your teleport request was on cooldown. You chose to bypass that by paying {0} from your balance."},
                {"DM_TownTPCooldownBypassF", "Your teleport is currently on cooldown. You do not have sufficient funds ({0}) to bypass."},
                {"DM_TownTPCooldownBypassP", "You may choose to pay {0} to bypass this cooldown." },
                {"DM_TownTPCooldownBypassP2", "Type <color=yellow>/{0} {1}</color>" },
                {"DM_TownTPLimitReached", "You have reached the daily limit of {0} teleports today! You'll have to wait {1} for your next teleport."},
                {"DM_TownTPAmount", "You have {0} <color=yellow>{1}</color> teleports left today!"},

                { "Days", "Days" },
                { "Hours", "Hours" },
                { "Minutes", "Minutes" },
                { "Seconds", "Seconds" },

                {"BlockedMarkerTeleport", "You have been blocked from using the marker teleport!" },
                {"BlockedAuthMarkerTeleport", "You have been TC blocked from using the marker teleport!" },
                {"Interrupted", "Your teleport was interrupted!"},
                {"InterruptedTarget", "{0}'s teleport was interrupted!"},
                {"Unlimited", "Unlimited"},
                {
                    "TPInfoGeneral", string.Join(NewLine, new[]
                    {
                        "Please specify the module you want to view the info of.",
                        "The available modules are: ",
                    })
                },
                {
                    "TPHelpGeneral", string.Join(NewLine, new[]
                    {
                        "/tpinfo - Shows limits and cooldowns.",
                        "Please specify the module you want to view the help of.",
                        "The available modules are: ",
                    })
                },
                {
                    "TPHelpadmintp", string.Join(NewLine, new[]
                    {
                        "As an admin you have access to the following commands:",
                        "/tp \"targetplayer\" - Teleports yourself to the target player.",
                        "/tp \"player\" \"targetplayer\" - Teleports the player to the target player.",
                        "/tp x y z - Teleports you to the set of coordinates.",
                        "/tpl - Shows a list of saved locations.",
                        "/tpl \"location name\" - Teleports you to a saved location.",
                        "/tpsave \"location name\" - Saves your current position as the location name.",
                        "/tpremove \"location name\" - Removes the location from your saved list.",
                        "/tpb - Teleports you back to the place where you were before teleporting.",
                        "/home radius \"radius\" - Find all homes in radius.",
                        "/home delete \"player name|id\" \"home name\" - Remove a home from a player.",
                        "/home tp \"player name|id\" \"name\" - Teleports you to the home location with the name 'name' from the player.",
                        "/home homes \"player name|id\" - Shows you a list of all homes from the player."
                    })
                },
                {
                    "TPHelphome", string.Join(NewLine, new[]
                    {
                        "With the following commands you can set your home location to teleport back to:",
                        "/home add \"name\" - Saves your current position as the location name.",
                        "/home list - Shows you a list of all the locations you have saved.",
                        "/home remove \"name\" - Removes the location of your saved homes.",
                        "/home \"name\" - Teleports you to the home location."
                    })
                },
                {
                    "TPHelptpr", string.Join(NewLine, new[]
                    {
                        "With these commands you can request to be teleported to a player or accept someone else's request:",
                        "/tpr \"player name\" - Sends a teleport request to the player.",
                        "/tpa - Accepts an incoming teleport request.",
                        "/tpat - Toggle automatic /tpa on incoming teleport requests.",
                        "/tpc - Cancel teleport or request."
                    })
                },
                {
                    "TPSettingsGeneral", string.Join(NewLine, new[]
                    {
                        "Please specify the module you want to view the settings of. ",
                        "The available modules are:",
                    })
                },
                {
                    "TPSettingshome", string.Join(NewLine, new[]
                    {
                        "Home System has the current settings enabled:",
                        "Time between teleports: {0}",
                        "Daily amount of teleports: {1}",
                        "Amount of saved Home locations: {2}"
                    })
                },
                {
                    "TPSettingsbandit", string.Join(NewLine, new[]
                    {
                        "Bandit System has the current settings enabled:",
                        "Time between teleports: {0}",
                        "Daily amount of teleports: {1}"
                    })
                },
                {
                    "TPSettingsoutpost", string.Join(NewLine, new[]
                    {
                        "Outpost System has the current settings enabled:",
                        "Time between teleports: {0}",
                        "Daily amount of teleports: {1}"
                    })
                },
                {
                    "TPSettingstpr", string.Join(NewLine, new[]
                    {
                        "TPR System has the current settings enabled:",
                        "Time between teleports: {0}",
                        "Daily amount of teleports: {1}"
                    })
                },
                {
                    "TPSettingstown", string.Join(NewLine, new[]
                    {
                        "Town System has the current settings enabled:",
                        "Time between teleports: {0}",
                        "Daily amount of teleports: {1}"
                    })
                },
                {
                    "TPSettingsdynamic", string.Join(NewLine, new[]
                    {
                        "{0} System has the current settings enabled:",
                        "Time between teleports: {1}",
                        "Daily amount of teleports: {2}"
                    })
                },

                {"TPT_True", "enabled"},
                {"TPT_False", "disabled"},
                {"TPT_clan", "{1} clan has been {0}."},
                {"TPT_friend", "{1} friend has been {0}."},
                {"TPT_team", "{1} team has been {0}."},
                {"NotValidTPT", "Not valid, player is not"},
                {"NotValidTPTFriend", " a friend!"},
                {"NotValidTPTTeam", " on your team!"},
                {"NotValidTPTClan", " in your clan!"},
                {"TPTInfo", "{4} auto accepts teleport requests.\n<color={5}>Green</color> = <color={5}>Enabled</color>\n<color={6}>Red</color> = <color={6}>Disabled</color>\n\n/{0} <color={1}>clan</color> - Toggle {4} for clan members/allies.\n/{0} <color={2}>team</color> - Toggle {4} for teammates.\n/{0} <color={3}>friend</color> - Toggle {4} for friends."},

                {"PlayerNotFound", "The specified player couldn't be found please try again!"},
                {"MultiplePlayers", "Found multiple players: {0}"},
                {"CantTeleportToSelf", "You can't teleport to yourself!"},
                {"CantTeleportPlayerToSelf", "You can't teleport a player to himself!"},
                {"CantTeleportPlayerToYourself", "You can't teleport a player to yourself!"},
                {"TeleportPendingTPC", "You can't initiate another teleport while you have a teleport pending! Use /tpc to cancel this."},
                {"TeleportPendingTarget", "You can't request a teleport to someone who's about to teleport!"},
                {"LocationExists", "A location with this name already exists at {0}!"},
                {"LocationExistsNearby", "A location with the name {0} already exists near this position!"},
                {"LocationNotFound", "Couldn't find a location with that name!"},
                {"NoPreviousLocationSaved", "No previous location saved!"},
                {"HomeExists", "You have already saved a home location by this name!"},
                {"HomeExistsNearby", "A home location with the name {0} already exists near this position!"},
                {"HomeNotFound", "Couldn't find your home with that name!"},
                {"InvalidCoordinates", "The coordinates you've entered are invalid!"},
                {"InvalidHelpModule", "Invalid module supplied!"},
                {"InvalidCharacter", "You have used an invalid character, please limit yourself to the letters a to z and numbers."},
                {"NotUseable", "You must wait another {0}after the wipe to use this command." },
                {
                    "SyntaxCommandTP", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tp command as follows:",
                        "/tp \"targetplayer\" - Teleports yourself to the target player.",
                        "/tp \"player\" \"targetplayer\" - Teleports the player to the target player.",
                        "/tp x y z - Teleports you to the set of coordinates.",
                        "/tp \"player\" x y z - Teleports the player to the set of coordinates."
                    })
                },
                {
                    "SyntaxCommandTPL", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpl command as follows:",
                        "/tpl - Shows a list of saved locations.",
                        "/tpl \"location name\" - Teleports you to a saved location."
                    })
                },
                {
                    "SyntaxCommandTPSave", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpsave command as follows:",
                        "/tpsave \"location name\" - Saves your current position as 'location name'."
                    })
                },
                {
                    "SyntaxCommandTPRemove", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpremove command as follows:",
                        "/tpremove \"location name\" - Removes the location with the name 'location name'."
                    })
                },
                {
                    "SyntaxCommandTPN", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpn command as follows:",
                        "/tpn \"targetplayer\" - Teleports yourself the default distance behind the target player.",
                        "/tpn \"targetplayer\" \"distance\" - Teleports you the specified distance behind the target player."
                    })
                },
                {
                    "SyntaxCommandSetHome", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home add command as follows:",
                        "/home add \"name\" - Saves the current location as your home with the name 'name'."
                    })
                },
                {
                    "SyntaxCommandRemoveHome", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home remove command as follows:",
                        "/home remove \"name\" - Removes the home location with the name 'name'."
                    })
                },
                {
                    "SyntaxCommandHome", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home command as follows:",
                        "/home \"name\" - Teleports yourself to your home with the name 'name'.",
                        "/home \"name\" pay - Teleports yourself to your home with the name 'name', avoiding cooldown by paying for it.",
                        "/home add \"name\" - Saves the current location as your home with the name 'name'.",
                        "/home list - Shows you a list of all your saved home locations.",
                        "/home remove \"name\" - Removes the home location with the name 'name'."
                    })
                },
                {
                    "SyntaxCommandHomeAdmin", string.Join(NewLine, new[]
                    {
                        "/home radius \"radius\" - Shows you a list of all homes in radius(10).",
                        "/home delete \"player name|id\" \"name\" - Removes the home location with the name 'name' from the player.",
                        "/home tp \"player name|id\" \"name\" - Teleports you to the home location with the name 'name' from the player.",
                        "/home homes \"player name|id\" - Shows you a list of all homes from the player."
                    })
                },
                {
                    "SyntaxCommandTown", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /town command as follows:",
                        "/town - Teleports yourself to town.",
                        "/town pay - Teleports yourself to town, paying the penalty."
                    })
                },
                {
                    "SyntaxCommandTownAdmin", string.Join(NewLine, new[]
                    {
                        "/town set - Saves the current location as town.",
                    })
                },
                {
                    "SyntaxCommandOutpost", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /outpost command as follows:",
                        "/outpost - Teleports yourself to the Outpost.",
                        "/outpost pay - Teleports yourself to the Outpost, paying the penalty."
                    })
                },
                {
                    "SyntaxCommandOutpostAdmin", string.Join(NewLine, new[]
                    {
                        "/outpost set - Saves the current location as Outpost.",
                    })
                },
                {
                    "SyntaxCommandBandit", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /bandit command as follows:",
                        "/bandit - Teleports yourself to the Bandit Town.",
                        "/bandit pay - Teleports yourself to the Bandit Town, paying the penalty."
                    })
                },
                {
                    "SyntaxCommandBanditAdmin", string.Join(NewLine, new[]
                    {
                        "/bandit set - Saves the current location as Bandit Town.",
                    })
                },
                {
                    "SyntaxCommandHomeDelete", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home delete command as follows:",
                        "/home delete \"player name|id\" \"name\" - Removes the home location with the name 'name' from the player."
                    })
                },
                {
                    "SyntaxCommandHomeAdminTP", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home tp command as follows:",
                        "/home tp \"player name|id\" \"name\" - Teleports you to the home location with the name 'name' from the player."
                    })
                },
                {
                    "SyntaxCommandHomeHomes", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home homes command as follows:",
                        "/home homes \"player name|id\" - Shows you a list of all homes from the player."
                    })
                },
                {
                    "SyntaxCommandListHomes", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home list command as follows:",
                        "/home list - Shows you a list of all your saved home locations."
                    })
                },
                {
                    "SyntaxCommandTPR", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpr command as follows:",
                        "/tpr \"player name\" - Sends out a teleport request to 'player name'."
                    })
                },
                {
                    "SyntaxCommandTPA", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpa command as follows:",
                        "/tpa - Accepts an incoming teleport request."
                    })
                },
                {
                    "SyntaxCommandTPC", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpc command as follows:",
                        "/tpc - Cancels an teleport request."
                    })
                },
                {
                    "SyntaxConsoleCommandToPos", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the teleport.topos console command as follows:",
                        " > teleport.topos \"player\" x y z"
                    })
                },
                {
                    "SyntaxConsoleCommandToPlayer", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the teleport.toplayer console command as follows:",
                        " > teleport.toplayer \"player\" \"target player\""
                    })
                },
                {"LogTeleport", "{0} teleported to {1}."},
                {"LogTeleportPlayer", "{0} teleported {1} to {2}."},
                {"LogTeleportBack", "{0} teleported back to previous location."},
                {"DiscordLogTPA", "{0} and {1} accepted TPA {2} times"}
            };

            foreach (var key in config.DynamicCommands.Keys)
            {
                en[key] = key;
            }

            foreach (var key in monumentMessages)
            {
                en[key] = key;
            }

            lang.RegisterMessages(en, this, "en");

            var ru = new Dictionary<string, string>
            {
                {"ErrorTPR", "Телепорт к {0} блокирован ({1})"},
                {"AdminTP", "Вы телепортированы к {0}!"},
                {"AdminTPTarget", "{0} телепортировал вас!"},
                {"AdminTPPlayers", "Вы телепортировали {0} к {1}!"},
                {"AdminTPPlayer", "{0} телепортировал вас к {1}!"},
                {"AdminTPPlayerTarget", "{0} телепортировал {1} к вам!"},
                {"AdminTPCoordinates", "Вы телепортированы к {0}!"},
                {"AdminTPTargetCoordinates", "Вы телепортировали {0} к {1}!"},
                {"AdminTPOutOfBounds", "Вы пытались телепортироваться к координатам вне границ карты!"},
                {"AdminTPBoundaries", "Значения X и Z должны быть между -{0} и {0}, а значение Y между -100 и 2000!"},
                {"AdminTPLocation", "Вы телепортированы к {0}!"},
                {"AdminTPLocationSave", "Вы сохранили текущее местоположение!"},
                {"AdminTPLocationRemove", "Вы удалили местоположение {0}!"},
                {"AdminLocationList", "Доступны следующие местоположения:"},
                {"AdminLocationListEmpty", "Вы не сохранили никаких местоположений!"},
                {"AdminTPBack", "Вы телепортированы назад, в ваше предыдущее местоположение!"},
                {"AdminTPBackSave", "Ваше предыдущее местоположение сохранено, используйте <color=yellow>/tpb</color>, чтобы телепортироваться назад!"},
                {"AdminTPTargetCoordinatesTarget", "{0} телепортировал вас к {1}!"},
                {"AdminTPConsoleTP", "Вы были телепортированы к {0}"},
                {"AdminTPConsoleTPPlayer", "Вы были телепортированы к {0}"},
                {"AdminTPConsoleTPPlayerTarget", "{0} был телепортирован к вам!"},
                {"HomeTP", "Вы телепортированы в ваш дом '{0}'!"},
                {"HomeAdminTP", "Вы телепортированы к дому '{1}' принадлежащему {0}!"},
                {"HomeIce", "Дома на замерзшем озере запрещены!"},
                {"HomeSave", "Вы сохранили текущее местоположение как ваш дом!"},
                {"HomeNoFoundation", "Использовать местоположение в качестве дома разрешено только на фундаменте!"},
                {"HomeFoundationNotOwned", "Вы не можете использовать команду home в чужом доме."},
                {"HomeFoundationUnderneathFoundation", "Вы не можете использовать команду home на фундаменте, который находится под другим фундаментом."},
                {"HomeFoundationNotFriendsOwned", "Вы, или ваш друг, должны быть владельцем дома, чтобы использовать команду home!"},
                {"HomeRemovedInvalid", "Ваш дом '{0}' был удалён потому, что не на фундаменте, или у фундамента новый владелец!"},
                {"HighWallCollision", "Столкновение Высоких Стен!"},
                {"HomeRemovedDestroyed", "Ваш дом '{0}' удален, так как его больше не существует!"},
                {"HomeRemovedInsideBlock", "Ваш дом '{0}' был удалён потому, что внутри фундамента!"},
                {"HomeRemove", "Вы удалили свой дом {0}!"},
                {"HomeDelete", "Вы удалили дом '{1}' принадлежащий {0}!"},
                {"HomeList", "Доступны следующие дома:"},
                {"HomeListEmpty", "Вы не сохранили ни одного дома!"},
                {"HomeMaxLocations", "Невозможно установить здесь ваш дом, вы достигли лимита в {0} домов!"},
                {"HomeQuota", "Вы установили {0} из {1} максимально возможных домов!"},
                {"HomeTugboatNotAllowed", "Установка дома на буксирах запрещена!"},
                {"HomeTPStarted", "Телепортация в ваш дом {0} через {1} секунд!"},
                {"PayToTown", "Стандартный платеж {0} распространяется на все телепорты в город!"},
                {"PayToTPR", "Стандартный платеж {0} распространяется на все tpr'ы!"},
                {"HomeTPCooldown", "Ваш телепорт перезаряжается. Вам необходимо подождать {0} до следующей телепортации."},
                {"HomeTPCooldownBypass", "Ваш телепорт был на перезарядке. Вы выбрали избежать ожидания, оплатив {0} с вашего баланса."},
                {"HomeTPCooldownBypassF", "Ваш телепорт перезаряжается. У вас недостаточно средств - {0} - чтобы избежать ожидания."},
                {"HomeTPCooldownBypassP", "Вы можете выбрать оплатить {0} чтобы избежать ожидания перезарядки." },
                {"HomeTPCooldownBypassP2", "Напишите <color=yellow>/home \"название дома\" {0}</color>." },
                {"HomeTPLimitReached", "Вы исчерпали ежедневный лимит {0} телепортаций сегодня!"},
                {"HomeTPAmount", "У вас осталось {0} телепортаций домой сегодня!"},
                {"HomesListWiped", "Вы очистили все местоположения, сохранённые как дом!"},
                {"HomeTPBuildingBlocked", "Вы не можете сохранить местоположение в качестве дома, если у вас нет прав на строительство в этой зоне!"},
                {"HomeTPSwimming", "Вы не можете устанавливать местоположение а качестве дома пока плывёте!"},
                {"HomeTPCrafting", "Вы не можете устанавливать местоположение а качестве дома в процессе крафта!"},
                {"Request", "Вы запросили телепортацию к {0}!"},
                {"RequestUI", "<size=14><color=#FFA500>TP Request:\n</color> {0}</size>"},
                {"RequestTarget", "{0} запросил телепортацию к вам! Используйте <color=yellow>/tpa</color>, чтобы принять!"},
                {"RequestTargetOff", "Ваш запрос был отменен, так как цель сейчас не в сети." },
                {"RequestAccept", "<size=12>Принять</size>" },
                {"RequestReject", "<size=12>Отказаться</size>" },
                {"TPR_NoClan_NoFriend_NoTeam", "Эта команда доступна только друзьям, участникам команды или клана!"},
                {"PendingRequest", "У вас уже есть активный запрос, отмените его, ожидайте подтверждения, либо отмены по таймауту!"},
                {"PendingRequestTarget", "У игрока, к которому вы хотите телепортироваться уже есть активный запрос, попробуйте позже!"},
                {"NoPendingRequest", "У вас нет активных запросов на телепортацию!"},
                {"Accept", "{0} принял ваш запрос! Телепортация через {1} секунд!"},
                {"AcceptTarget", "Вы приняли запрос на телепортацию {0}!"},
                {"AcceptToggleOff", "Вы отключили автоматическое /tpa!"},
                {"AcceptToggleOn", "Вы включили автоматическое /tpa!"},
                {"NotAllowed", "Вам не разрешено использовать эту команду!"},
                {"Success", "Вы телепортированы к {0}!"},
                {"SuccessTarget", "{0} телепортирован к вам!"},
                {"BlockedTeleportTarget", "You can't teleport to user \"{0}\", they have you teleport blocked!"},
                {"Cancelled", "Ваш запрос на телепортацию к {0} был отменён!"},
                {"CancelledTarget", "Запрос на телепортацию {0} был отменён!"},
                {"TPCancelled", "Ваша телепортация отменена!"},
                {"TPCancelledTarget", "{0} отменил телепортацию!"},
                {"TPYouCancelledTarget", "Вы отменили телепортацию {0}!"},
                {"TimedOut", "{0} не ответил на ваш запрос во время!"},
                {"TimedOutTarget", "Вы не ответили вовремя на запрос телепортации от {0}!"},
                {"TargetDisconnected", "{0} отключился, ваша телепортация отменена!"},
                {"TPRCooldown", "Ваши запросы на телепортацию в данный момент на перезарядке. Вам необходимо подождать {0} прежде чем отправить следующий запрос."},
                {"TPRCooldownBypass", "Ваши запросы на телепортацию были на перезарядке. Вы выбрали избежать ожидания, оплатив {0} с вашего баланса."},
                {"TPRCooldownBypassF", "Ваши запросы на телепортацию в данный момент на перезарядке. У вас недостаточно средств - {0} - чтобы избежать ожидания."},
                {"TPRCooldownBypassP", "Вы можете выбрать оплатить {0} чтобы избежать ожидания перезарядки." },
                {"TPMoney", "{0} списано с вашего аккаунта!"},
                {"TPNoMoney", "У вас нет {0} ни на одном аккаунте!"},
                {"TPRCooldownBypassP2", "Напишите <color=yellow>/tpr {0}</color>." },
                {"TPRCooldownBypassP2a", "Напишите <color=yellow>/tpr \"имя игрока\" {0}</color>." },
                {"TPRLimitReached", "Вы исчерпали ежедневный лимит {0} запросов на телепортацию сегодня!"},
                {"TPRAmount", "У вас осталось {0} запросов на телепортацию на сегодня!"},
                {"TPRTarget", "Ваша цель в данный момент не доступна!"},
                {"TPRNoCeiling", "Вы не можете телепортироваться, пока ваша цель находится на потолке!"},
                {"TPDead", "Вы не можете телепортироваться, пока мертвы!"},
                {"TPWounded", "Вы не можете телепортироваться, будучи раненым!"},
                {"TPTooCold", "Вам слишком холодно для телепортации!"},
                {"TPTooHot", "Вам слишком жарко для телепортации!"},
                {"TPTugboat", "Вы не можете телепортироваться на этой лодке!"},
                {"TPBoat", "Вы не можете телепортироваться находясь на лодке!"},
                {"TPHostile", "Вы не можете телепортироваться в Город NPC или Лагерь бандитов пока враждебны!"},
                {"TPJunkpile", "Вы не можете телепортироваться с кучи мусора"},
                {"HostileTimer", "Телепорт станет доступен через {0} минут."},
                {"TPMounted", "Вы не можете телепортироваться, когда сидите!"},
                {"TPBuildingBlocked", "Вы не можете телепортироваться, находясь в зоне блокировки строительства!"},
                {"TPAboveWater", "Вы не можете телепортироваться находясь над водой!"},
                {"TPUnderWater", "Вы не можете телепортироваться под водой!"},
                {"TPTargetBuildingBlocked", "Вы не можете телепортироваться в зону, где блокировано строительство!"},
                {"TPTargetInsideBlock", "Вы не можете телепортироваться в фундамент!"},
                {"TPTargetInsideRock", "Вы не можете телепортироваться в скалу!"},
                {"TPSwimming", "Вы не можете телепортироваться, пока плывёте!"},
                {"TPCargoShip", "Вы не можете телепортироваться с грузового корабля!"},
                {"TPOilRig", "Вы не можете телепортироваться с нефтяной вышки!"},
                {"TPExcavator", "Вы не можете телепортироваться с экскаватора!"},
                {"TPHotAirBalloon", "Вы не можете телепортироваться с, или на воздушный шар!"},
                {"TPLift", "Вы не можете телепортироваться находясь в лифте или подъемнике!"},
                {"TPBucketLift", "Вы не можете телепортироваться находясь в ковшевом подъемнике!"},
                {"TPRegLift", "Вы не можете телепортироваться находясь в лифте!"},
                {"TPSafeZone", "Вы не можете телепортироваться из безопасной зоны!"},
                {"TPFlagZone", "Вы не можете телепортироваться из этой зоны!"},
                {"TPNoEscapeBlocked", "Вы не можете телепортироваться пока активна блокировка!"},
                {"TPCrafting", "Вы не можете телепортироваться в процессе крафта!"},
                {"TPBlockedItem", "Вы не можете телепортироваться пока несёте: {0}!"},
                {"TooCloseToMon", "Вы не можете телепортироваться так близко к {0}!"},
                {"TPHomeSafeZoneOnly", "Вы можете телепортироваться домой только из безопасной зоны!" },
                {"TooCloseToCave", "Вы не можете телепортироваться так близко к пещере!"},
                {"HomeTooCloseToCave", "Вы не можете сохранить местоположение в качестве дома так близко к пещере!"},
                {"HomeTooCloseToMon", "Вы не можете сохранить местоположение в качестве дома так близко к монументу!"},
                {"CannotTeleportFromHome", "Вы должны выйти из вашей базы, прежде чем телепортироваться!"},
                {"WaitGlobalCooldown", "Вы должны подождать {0}, пока ваш глобальный телепорт перезаряжается!" },

                {"DM_TownTP", "Вы телепортированы в {0}!"},
                {"DM_TownTPNoLocation", "Местоположение <color=yellow>{0}</color> в данный момент не установлено!"},
                {"DM_TownTPDisabled", "<color=yellow>{0}</color> в данный момент отключен в файле настройек!"},
                {"DM_TownTPLocation", "Вы установили местоположение <color=yellow>{0}</color> в {1}!"},
                {"DM_TownTPCreated", "Вы создали команду: <color=yellow>{0}</color>"},
                {"DM_TownTPRemoved", "Вы удалили команду: <color=yellow>{0}</color>"},
                {"DM_TownTPDoesNotExist", "Команда не существует: <color=yellow>{0}</color>"},
                {"DM_TownTPExists", "Команда <color=yellow>{0}</color> уже сущуствует!"},
                {"DM_TownTPLocationsCleared", "Вы стерли все точки телепорта для {0}!"},
                {"DM_TownTPStarted", "Телепортация в {0} через {1} секунд!"},
                {"DM_TownTPCooldown", "Ваш телепорт перезаряжается. Вам необходимо подождать {0} до следующей телепортации."},
                {"DM_TownTPCooldownBypass", "Ваш телепорт был на перезарядке. Вы выбрали избежать ожидания, оплатив {0} с вашего баланса."},
                {"DM_TownTPCooldownBypassF", "Ваш телепорт перезаряжается. У вас недостаточно средств ({0}) чтобы избежать ожидания."},
                {"DM_TownTPCooldownBypassP", "Вы можете выбрать оплатить {0} чтобы избежать ожидания перезарядки." },
                {"DM_TownTPCooldownBypassP2", "Введите <color=yellow>/{0} {1}</color>" },
                {"DM_TownTPLimitReached", "Вы исчерпали ежедневный лимит {0} телепортаций сегодня! Вам необходимо подождать {1} до следующей телепортации."},
                {"DM_TownTPAmount", "У вас осталось {0} телепортаций <color=yellow>{1}</color> сегодня!"},

                {"Days", "дней" },
                {"Hours", "часов" },
                {"Minutes", "минут" },
                {"Seconds", "секунд" },

                {"BlockedMarkerTeleport", "Вам заблокировано использование маркера телепортации!" },
                {"BlockedAuthMarkerTeleport", "Вам заблокировано использование маркера телепортации! (TC)" },
                {"Interrupted", "Ваша телепортация была прервана!"},
                {"InterruptedTarget", "Телепортация {0} была прервана!"},
                {"Unlimited", "Не ограничено"},
                {
                    "TPInfoGeneral", string.Join(NewLine, new[]
                    {
                        "Пожалуйста, укажите модуль, о котором вы хотите просмотреть информацию.",
                        "Доступные модули: ",
                    })
                },
                {
                    "TPHelpGeneral", string.Join(NewLine, new[]
                    {
                        "<color=yellow>/tpinfo</color> - Отображает лимиты и перезарядки.",
                        "Пожалуйста, укажите модуль, по которому вы хотите получить помощь.",
                        "Доступные модули: ",
                    })
                },
                {
                    "TPHelpadmintp", string.Join(NewLine, new[]
                    {
                        "Как админ, вы имеете доступ к следующим командам:",
                        "<color=yellow>/tp \"имя игрока\"</color> - Телепортирует вас к указанному игроку.",
                        "<color=yellow>/tp \"имя игрока\" \"имя игрока 2\"</color> - Телепортирует игрока с именем 'имя игрока' к игроку 'имя игрока 2'.",
                        "<color=yellow>/tp x y z</color> - Телепортирует вас к указанным координатам.",
                        "<color=yellow>/tpl</color> - Отображает список сохранённых местоположений.",
                        "<color=yellow>/tpl \"название местоположения\"</color> - Телепортирует вас в сохранённое местоположение.",
                        "<color=yellow>/tpsave \"название местоположения\"</color> - Сохраняет ваше текущее местоположение с указанным названием.",
                        "<color=yellow>/tpremove \"название местоположения\"</color> - Удаляет местоположение из списка сохранённых.",
                        "<color=yellow>/tpb</color> - Телепортирует вас назад на место, где вы были перед телепортацией.",
                        "<color=yellow>/home radius \"радиус\"</color> - Найти все дома в радиусе.",
                        "<color=yellow>/home delete \"имя игрока или ID\" \"название дома\"</color> - Удаляет дом с указанным именем принадлежащий указанному игроку.",
                        "<color=yellow>/home tp \"имя игрока или ID\" \"название дома\"</color> - Телепортирует вас в дом игрока с указанным названием принадлежащий указанному игроку.",
                        "<color=yellow>/home homes \"имя игрока или ID\"</color> - Отображает вам список всех домов, принадлежащих указанному игроку."
                    })
                },
                {
                    "TPHelphome", string.Join(NewLine, new[]
                    {
                        "Используя следующие команды, вы можете установить местоположение вашего дома, чтобы затем в него телепортироваться:",
                        "<color=yellow>/home add \"название дома\"</color> - Сохраняет ваше текущее местоположение как ваш дом с указанным названием.",
                        "<color=yellow>/home list</color> - Отображает список всех местоположений, сохранённых вами как дом.",
                        "<color=yellow>/home remove \"название дома\"</color> - Удаляет расположение сохранённого дома с указанным названием.",
                        "<color=yellow>/home \"название дома\"</color> - Телепортирует вас в местоположение дома с указанным названием."
                    })
                },
                {
                    "TPHelptpr", string.Join(NewLine, new[]
                    {
                        "Используя эти команды, вы можете отправить запрос на телепортацию к игроку, или принять чей-то запрос:",
                        "<color=yellow>/tpr \"имя игрока\"</color> - Отправляет запрос на телепортацию игроку с указанным именем.",
                        "<color=yellow>/tpa</color> - Принять входящий запрос на телепортацию.",
                        "<color=yellow>/tpat</color> - Вкл./Выкл. автоматическое принятие входящих запросов на телепортацию к вам /tpa.",
                        "<color=yellow>/tpc</color> - Отменить запрос на телепортацию."
                    })
                },
                {
                    "TPSettingsGeneral", string.Join(NewLine, new[]
                    {
                        "Пожалуйста, укажите модуль, настройки которого вы хотите просмотреть. ",
                        "Доступные модули:",
                    })
                },
                {
                    "TPSettingshome", string.Join(NewLine, new[]
                    {
                        "Система домов в данный момент имеет следующие включённые параметры:",
                        "Время между телепортами: {0}",
                        "Ежедневный лимит телепортаций: {1}",
                        "Количество сохранённых домов: {2}"
                    })
                },
                {
                    "TPSettingsbandit", string.Join(NewLine, new[]
                    {
                        "Система Лагерь бандитов в данный момент имеет следующие включённые параметры:",
                        "Время между телепортами: {0}",
                        "Ежедневный лимит телепортаций: {1}"
                    })
                },
                {
                    "TPSettingsoutpost", string.Join(NewLine, new[]
                    {
                        "Система Город NPC в данный момент имеет следующие включённые параметры:",
                        "Время между телепортами: {0}",
                        "Ежедневный лимит телепортаций: {1}"
                    })
                },
                {
                    "TPSettingstpr", string.Join(NewLine, new[]
                    {
                        "Система TPR в данный момент имеет следующие включённые параметры:",
                        "Время между телепортами: {0}",
                        "Ежедневный лимит телепортаций: {1}"
                    })
                },
                {
                    "TPSettingstown", string.Join(NewLine, new[]
                    {
                        "В Системе Городов включены следующие параметры:",
                        "Время между телепортами: {0}",
                        "Ежедневный лимит телепортаций: {1}"
                    })
                },
                {
                    "TPSettingsdynamic", string.Join(NewLine, new[]
                    {
                        "В Системе {0} включены следующие параметры:",
                        "Время между телепортами: {1}",
                        "Ежедневный лимит телепортаций: {2}"
                    })
                },

                {"TPT_True", "включено"},
                {"TPT_False", "выключено"},
                {"TPT_clan", "{1} clan теперь {0}."},
                {"TPT_friend", "{1} friend теперь {0}."},
                {"TPT_team", "{1} team теперь {0}."},
                {"NotValidTPT", "Неверно, игрок не"},
                {"NotValidTPTFriend", " друг!"},
                {"NotValidTPTTeam", " в вашей команде!"},
                {"NotValidTPTClan", " в вашем клане!"},

                {"TPTInfo", "{4} это авто-принятие запросов на телепортацию.\n<color={5}>Зелёный</color> = <color={5}>Включено</color>\n<color={6}>Красный</color> = <color={6}>Выключено</color>\n\n/{0} <color={1}>clan</color> - Включение/выключение {4} для членов клана или союзников.\n/{0} <color={2}>team</color> - Включение/выключение {4} для членов группы.\n/{0} <color={3}>friend</color> - Включение/выключение {4} для друзей."},
                {"PlayerNotFound", "Указанный игрок не обнаружен, пожалуйста попробуйте ещё раз!"},
                {"MultiplePlayers", "Найдено несколько игроков: {0}"},
                {"CantTeleportToSelf", "Вы не можете телепортироваться к самому себе!"},
                {"CantTeleportPlayerToSelf", "Вы не можете телепортровать игрока к самому себе!"},
                {"TeleportPendingTPC", "Вы не можете инициировать телепортацию, пока у вас есть активный запрос! Используйте <color=yellow>/tpc</color> чтобы отменить его."},
                {"TeleportPendingTarget", "Вы не можете отправить запрос к тому, кто в процессе телепортации!"},
                {"LocationExists", "Местоположение с таким названием уже существует в {0}!"},
                {"LocationExistsNearby", "Местоположение с названием {0} уже существует рядом с текущей позицией!"},
                {"LocationNotFound", "Не найдено местоположение с таким названием!"},
                {"NoPreviousLocationSaved", "Предыдущее местоположение не сохранено!"},
                {"HomeExists", "Вы уже сохранили дом с таким названием!"},
                {"HomeExistsNearby", "Дом с названием {0} уже существует рядом с текущей позицией!"},
                {"HomeNotFound", "Дом с таким названием не найден!"},
                {"InvalidCoordinates", "Вы указали неверные координаты!"},
                {"InvalidHelpModule", "Указан неверный модуль!"},
                {"InvalidCharacter", "Вы использовали недопустимый символ, ограничьтесь буквами от a до z и цифрами."},
                {
                    "SyntaxCommandTP", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/tp</color> возможно только следующим образом:",
                        "<color=yellow>/tp \"имя игрока\"</color> - Телепортирует вас к указанному игроку.",
                        "<color=yellow>/tp \"имя игрока\" \"имя игрока 2\"</color> - Телепортирует игрока с именем 'имя игрока' к игроку 'имя игрока 2'.",
                        "<color=yellow>/tp x y z</color> - Телепортирует вас к указанным координатам.",
                        "<color=yellow>/tp \"имя игрока\" x y z</color> - Телепортирует игрока с именем 'имя игрока' к указанным координатам."
                    })
                },
                {
                    "SyntaxCommandTPL", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/tpl</color> возможно только следующим образом:",
                        "<color=yellow>/tpl</color> - Отображает список сохранённых местоположений.",
                        "<color=yellow>/tpl \"название местоположения\"</color> - Телепортирует вас в место с указанным названием."
                    })
                },
                {
                    "SyntaxCommandTPSave", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/tpsave</color> возможно только следующим образом:",
                        "<color=yellow>/tpsave \"название местоположения\"</color> - Сохраняет ваше текущее местоположение с указанным названием."
                    })
                },
                {
                    "SyntaxCommandTPRemove", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/tpremove</color> возможно только следующим образом:",
                        "<color=yellow>/tpremove \"название местоположения\"</color> - Удаляет местоположение с указанным названием."
                    })
                },
                {
                    "SyntaxCommandTPN", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/tpn</color> возможно только следующим образом:",
                        "<color=yellow>/tpn \"имя игрока\"</color> - Телепортирует вас на расстояние по умолчанию позади игрока с указанным именем.",
                        "<color=yellow>/tpn \"имя игрока\" \"расстояние\"</color> - Телепортирует вас на указанное расстояние позади игрока с указанным именем."
                    })
                },
                {
                    "SyntaxCommandSetHome", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/home add</color> возможно только следующим образом:",
                        "<color=yellow>/home add \"название\"</color> - Сохраняет ваше текущее местоположение как ваш дом с указанным названием."
                    })
                },
                {
                    "SyntaxCommandRemoveHome", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/home remove</color> возможно только следующим образом:",
                        "<color=yellow>/home remove \"название\"</color> - Удаляет местоположение дома с указанным названием."
                    })
                },
                {
                    "SyntaxCommandHome", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/home</color> возможно только следующим образом:",
                        "<color=yellow>/home \"название\"</color> - Телепортирует вас в ваш дом с указанным названием.",
                        "<color=yellow>/home \"название\" pay</color> - Телепортирует вас в ваш дом с указанным названием, избегая перезарядки, заплатив за это.",
                        "<color=yellow>/home add \"название\"</color> - Сохраняет ваше текущее местоположение как ваш дом с указанным названием.",
                        "<color=yellow>/home list</color> - Отображает список всех местоположений, сохранённых вами как дом.",
                        "<color=yellow>/home remove \"название\"</color> - Удаляет местоположение дома с указанным названием."
                    })
                },
                {
                    "SyntaxCommandHomeAdmin", string.Join(NewLine, new[]
                    {
                        "<color=yellow>/home radius \"радиус\"</color> - Отображает список всех домов в радиусе(10).",
                        "<color=yellow>/home delete \"имя игрока или ID\" \"название\"</color> - Удаляет дом с указанным названием, принадлежащий указанному игроку.",
                        "<color=yellow>/home tp \"имя игрока или ID\" \"название\"</color> - Телепортирует вас в дом с указанным названием, принадлежащий указанному игроку.",
                        "<color=yellow>/home homes \"имя игрока или ID\"</color> - Отображает вам список всех домов, принадлежащих указанному игроку."
                    })
                },
                {
                    "SyntaxCommandTown", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/town</color> возможно только следующим образом:",
                        "<color=yellow>/town</color> - Телепортирует вас в Город.",
                        "<color=yellow>/town pay</color> - Телепортирует вас в Город с оплатой штрафа."
                    })
                },
                {
                    "SyntaxCommandTownAdmin", string.Join(NewLine, new[]
                    {
                        "<color=yellow>/town set</color> - Сохраняет текущее местоположение как Город.",
                    })
                },
                {
                    "SyntaxCommandOutpost", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/outpost</color> возможно только следующим образом:",
                        "<color=yellow>/outpost</color> - Телепортирует вас в Город NPC.",
                        "<color=yellow>/outpost pay</color> - Телепортирует вас в Город NPC с оплатой штрафа."
                    })
                },
                {
                    "SyntaxCommandOutpostAdmin", string.Join(NewLine, new[]
                    {
                        "<color=yellow>/outpost set</color> - Сохраняет текущее местоположение как Город NPC.",
                    })
                },
                {
                    "SyntaxCommandBandit", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/bandit</color> возможно только следующим образом:",
                        "<color=yellow>/bandit</color> - Телепортирует вас в Лагерь бандитов.",
                        "<color=yellow>/bandit pay</color> - Телепортирует вас в Лагерь бандитов с оплатой штрафа."
                    })
                },
                {
                    "SyntaxCommandBanditAdmin", string.Join(NewLine, new[]
                    {
                        "<color=yellow>/bandit set</color> - Сохраняет текущее местоположение как Лагерь бандитов.",
                    })
                },
                {
                    "SyntaxCommandHomeDelete", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/home delete</color> возможно только следующим образом:",
                        "<color=yellow>/home delete \"имя игрока или ID\" \"название\"</color> - Удаляет дом с указанным названием, принадлежащий указанному игроку."
                    })
                },
                {
                    "SyntaxCommandHomeAdminTP", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/home tp</color> возможно только следующим образом:",
                        "<color=yellow>/home tp \"имя игрока или ID\" \"название\"</color> - Телепортирует вас в дом игрока с указанным названием, принадлежащий указанному игроку."
                    })
                },
                {
                    "SyntaxCommandHomeHomes", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/home homes</color> возможно только следующим образом:",
                        "<color=yellow>/home homes \"имя игрока или ID\"</color> - Отображает вам список всех домов, принадлежащих указанному игроку."
                    })
                },
                {
                    "SyntaxCommandListHomes", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/home list</color> возможно только следующим образом:",
                        "<color=yellow>/home list</color> - Отображает список всех местоположений, сохранённых вами как дом."
                    })
                },
                {
                    "SyntaxCommandTPR", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/tpr</color> возможно только следующим образом:",
                        "<color=yellow>/tpr \"имя игрока или ID\"</color> - Отправляет указанному игроку запрос на телепортацию."
                    })
                },
                {
                    "SyntaxCommandTPA", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/tpa</color> возможно только следующим образом:",
                        "<color=yellow>/tpa</color> - Принять входящий запрос на телепортацию."
                    })
                },
                {
                    "SyntaxCommandTPC", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование команды <color=yellow>/tpc</color> возможно только следующим образом:",
                        "<color=yellow>/tpc</color> - Отменить запрос на телепортацию."
                    })
                },
                {
                    "SyntaxConsoleCommandToPos", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование консольной команды <color=orange>teleport.topos</color> возможно только следующим образом:",
                        " > <color=orange>teleport.topos \"имя игрока\" x y z</color>"
                    })
                },
                {
                    "SyntaxConsoleCommandToPlayer", string.Join(NewLine, new[]
                    {
                        "Произошла синтаксическая ошибка!",
                        "Использование консольной команды <color=orange>teleport.toplayer</color> возможно только следующим образом:",
                        " > <color=orange>teleport.toplayer \"имя игрока или ID\" \"имя игрока 2|id 2\"</color>"
                    })
                },
                {"LogTeleport", "{0} телепортирован к {1}."},
                {"LogTeleportPlayer", "{0} телепортировал {1} к {2}."},
                {"LogTeleportBack", "{0} телепортирован назад, в предыдущее местоположение."},
                {"DiscordLogTPA", "{0} and {1} accepted TPA {2} times"}
            };

            foreach (var key in config.DynamicCommands.Keys)
            {
                ru[key] = key;
            }

            foreach (var key in monumentMessages)
            {
                ru[key] = key;
            }

            lang.RegisterMessages(ru, this, "ru");

            var uk = new Dictionary<string, string>
            {
                {"ErrorTPR", "Телепорт до {0} блоковано ({1})"},
                {"AdminTP", "Ви телепортовані до {0}!"},
                {"AdminTPTarget", "{0} телепортував вас!"},
                {"AdminTPPlayers", "Ви телепортували {0} до {1}!"},
                {"AdminTPPlayer", "{0} телепортував вас до {1}!"},
                {"AdminTPPlayerTarget", "{0} телепортував {1} до вас!"},
                {"AdminTPCoordinates", "Ви телепортовані до {0}!"},
                {"AdminTPTargetCoordinates", "Ви телепортували {0} до {1}!"},
                {"AdminTPOutOfBounds", "Ви намагалися телепортуватися до координат поза межами карти!"},
                {"AdminTPBoundaries", "Значення X та Z повинні бути між -{0} та {0}, а значення Y між -100 та 2000!"},
                {"AdminTPLocation", "Ви телепортовані до {0}!"},
                {"AdminTPLocationSave", "Ви зберегли місцезнаходження!"},
                {"AdminTPLocationRemove", "Ви видалили розташування {0}!"},
                {"AdminLocationList", "Доступні такі розташування:"},
                {"AdminLocationListEmpty", "Ви не зберегли жодних місць!"},
                {"AdminTPBack", "Ви телепортовані назад, у ваше попереднє розташування!"},
                {"AdminTPBackSave", "Ваше попереднє місце збережено, використовуйте <color=yellow>/tpb</color>, щоб телепортуватися назад!"},
                {"AdminTPTargetCoordinatesTarget", "{0} телепортував вас до {1}!"},
                {"AdminTPConsoleTP", "Ви були телепортовані до {0}"},
                {"AdminTPConsoleTPPlayer", "Ви були телепортовані до {0}"},
                {"AdminTPConsoleTPPlayerTarget", "{0} був телепортований до вас!"},
                {"HomeTP", "Ви телепортовані до вашого будинку '{0}'!"},
                {"HomeAdminTP", "Ви телепортовані до будинку '{1}', що належить {0}!"},
                {"HomeIce", "Ви не можете зберегти місце розташування як будинок на крижаному озері!"},
                {"HomeSave", "Ви зберегли поточне розташування як ваш будинок!"},
                {"HomeNoFoundation", "Використовувати місцезнаходження як будинок дозволено тільки на фундаменті!"},
                {"HomeFoundationNotOwned", "Ви не можете використовувати команду home у чужому домі."},
                {"HomeFoundationUnderneathFoundation", "Ви не можете використовувати команду home на фундаменті, що знаходиться під іншим фундаментом."},
                {"HomeFoundationNotFriendsOwned", "Ви, або ваш друг, повинні бути власником будинку, щоб використати команду home!"},
                {"HomeRemovedInvalid", "Ваш будинок '{0}' був видалений тому, що не на фундаменті або у фундаменту новий власник!"},
                {"HighWallCollision", "Зіткнення Високих Стін!"},
                {"HomeRemovedDestroyed", "Ваш будинок '{0}' видалено, оскільки він більше не існує!"},
                {"HomeRemovedInsideBlock", "Ваш будинок '{0}' був видалений, тому що всередині фундаменту!"},
                {"HomeRemove", "Ви видалили свій будинок {0}!"},
                {"HomeDelete", "Ви видалили будинок '{1}', що належить {0}!"},
                {"HomeList", "Доступні такі будинки:"},
                {"HomeListEmpty", "Ви не зберегли жодного будинку!"},
                {"HomeMaxLocations", "Неможливо встановити тут ваш будинок, ви досягли ліміту в {0} будинків!"},
                {"HomeQuota", "Ви встановили {0} з {1} максимально можливих будинків!"},
                {"HomeTugboatNotAllowed", "You are not allowed to sethome on tugboats."},
                {"HomeTPStarted", "Телепортація до вашого будинку {0} через {1} секунд!"},
                {"PayToTown", "Стандартний платіж {0} поширюється на всі телепорти до міста!"},
                {"PayToTPR", "Стандартний платіж {0} поширюється на всі tpr'и!"},
                {"HomeTPCooldown", "Ваш телепорт перезаряджається. Вам необхідно почекати {0} до наступної телепортації."},
                {"HomeTPCooldownBypass", "Ваш телепорт був на перезарядженні. Ви обрали уникнути очікування, сплативши {0} з вашого балансу."},
                {"HomeTPCooldownBypassF", "Ваш телепорт перезаряджається. У вас недостатньо коштів - щоб уникнути очікування."},
                {"HomeTPCooldownBypassP", "Ви можете вибрати оплатити {0} щоб уникнути очікування перезаряджання." },
                {"HomeTPCooldownBypassP2", "Введіть <color=yellow>/home \"назва будинку\" {0}</color>." },
                {"HomeTPLimitReached", "Ви вичерпали щоденний ліміт {0} телепортацій сьогодні!"},
                {"HomeTPAmount", "У вас залишилось {0} телепортацій додому сьогодні!"},
                {"HomesListWiped", "Ви очистили всі місця, збережені як будинок!"},
                {"HomeTPBuildingBlocked", "Ви не можете зберегти місце розташування як будинок, якщо у вас немає прав на будівництво в цій зоні!"},
                {"HomeTPSwimming", "Ви не можете встановлювати місце розташування в якості будинку поки пливете!"},
                {"HomeTPCrafting", "Ви не можете встановлювати місце розташування як будинок в процесі крафту!"},
                {"Request", "Ви запросили телепортацію до {0}!"},
                {"RequestUI", "<size=14><color=#FFA500>TP Request:</color> {0}</size>"},
                {"RequestTarget", "{0} запросив телепортацію до вас! Використовуйте <color=yellow>/tpa</color>, щоб прийняти!"},
                {"RequestTargetOff", "Ваш запит було скасовано, оскільки ціль зараз не в мережі." },
                {"RequestAccept", "<size=12>Принять</size>" },
                {"RequestReject", "<size=12>Отказаться</size>" },
                {"TPR_NoClan_NoFriend_NoTeam", "Ця команда доступна лише друзям, учасникам команди або клану!"},
                {"PendingRequest", "У вас вже є активний запит, скасуйте його, чекайте на підтвердження, або скасування по таймууту!"},
                {"PendingRequestTarget", "У гравця, до якого ви хочете телепортуватися, вже є активний запит, спробуйте пізніше!"},
                {"NoPendingRequest", "Ви не маєте активних запитів на телепортацію!"},
                {"Accept", "{0} прийняв ваш запит! Телепортація через {1} секунд!"},
                {"AcceptTarget", "Ви отримали запит на телепортацію {0}!"},
                {"AcceptToggleOff", "Ви вимкнули автоматичне /tpa!"},
                {"AcceptToggleOn", "Ви ввімкнули автоматичне /tpa!"},
                {"NotAllowed", "Ви не можете використовувати цю команду!"},
                {"Success", "Ви телепортовані до {0}!"},
                {"SuccessTarget", "{0} телепортований до вас!"},
                {"BlockedTeleportTarget", "You can't teleport to user \"{0}\", they have you teleport blocked!"},
                {"Cancelled", "Ваш запит на телепортацію до {0} було скасовано!"},
                {"CancelledTarget", "Запит на телепортацію {0} було скасовано!"},
                {"TPCancelled", "Ваш телепорт скасовано!"},
                {"TPCancelledTarget", "{0} скасував телепортацію!"},
                {"TPYouCancelledTarget", "Ви скасували телепортацію {0}!"},
                {"TimedOut", "{0} не відповів на ваш запит під час!"},
                {"TimedOutTarget", "Ви не вчасно відповіли на запит телепортації від {0}!"},
                {"TargetDisconnected", "{0} відключився, ваша телепортація скасована!"},
                {"TPRCooldown", "Ваші запити на телепортацію на даний момент на перезарядці. Вам необхідно зачекати на {0}, перш ніж надіслати наступний запит."},
                {"TPRCooldownBypass", "Ваші запити на телепортацію були перезаряджені. Ви обрали уникнути очікування, сплативши {0} з вашого балансу."},
                {"TPRCooldownBypassF", "Ваші запити на телепортацію на даний момент на перезарядці. У вас недостатньо коштів - щоб уникнути очікування."},
                {"TPRCooldownBypassP", "Ви можете вибрати оплатити {0} щоб уникнути очікування перезаряджання." },
                {"TPMoney", "{0} списано з вашого облікового запису!"},
                {"TPNoMoney", "У вас немає жодного облікового запису {0}!"},
                {"TPRCooldownBypassP2", "Напишіть <color=yellow>/tpr {0}</color>." },
                {"TPRCooldownBypassP2a", "Напишіть <color=yellow>/tpr \"ім'я гравця\" {0}</color>." },
                {"TPRLimitReached", "Ви вичерпали щоденний ліміт {0} запитів на телепортацію сьогодні!"},
                {"TPRAmount", "У вас залишилось {0} запитів на телепортацію на сьогодні!"},
                {"TPRTarget", "Ваша мета зараз не доступна!"},
                {"TPRNoCeiling", "Ви не можете телепортуватися, поки ваша ціль знаходиться на стелі!"},
                {"TPDead", "Ви не можете телепортуватися, поки мертві!"},
                {"TPWounded", "Ви не можете телепортуватися, будучи пораненим!"},
                {"TPTooCold", "Вам надто холодно для телепортації!"},
                {"TPTooHot", "Вам дуже жарко для телепортації!"},
                {"TPTugboat", "Вы не можете телепортироваться на этой лодке!"},
                {"TPBoat", "Ви не можете телепортуватися, перебуваючи на човні!"},
                {"TPHostile", "Ви не можете телепортуватися в Місто NPC або Табір бандитів, поки ворожі!"},
                {"TPJunkpile", "Ви не можете телепортуватися з купи сміття"},
                {"HostileTimer", "Телепорт буде доступний через {0} хвилин."},
                {"TPMounted", "Ви не можете телепортуватись, коли сидите!"},
                {"TPBuildingBlocked", "Ви не можете телепортуватися, перебуваючи у зоні блокування будівництва!"},
                {"TPAboveWater", "Ви не можете телепортуватися, перебуваючи над водою!"},
                {"TPUnderWater", "Ви не можете телепортуватися під воду!"},
                {"TPTargetBuildingBlocked", "Ви не можете телепортуватися до зони, де блоковано будівництво!"},
                {"TPTargetInsideBlock", "Ви не можете телепортуватися у фундамент!"},
                {"TPTargetInsideRock", "Ви не можете телепортуватись у скелю!"},
                {"TPSwimming", "Ви не можете телепортуватися, поки пливете!"},
                {"TPCargoShip", "Ви не можете телепортуватись з вантажного корабля!"},
                {"TPOilRig", "Ви не можете телепортуватися з нафтової вежі!"},
                {"TPExcavator", "Ви не можете телепортувати з екскаватора!"},
                {"TPHotAirBalloon", "Ви не можете телепортуватися з, або на повітряну кулю!"},
                {"TPLift", "Ви не можете телепортуватися, перебуваючи в ліфті або підйомнику!"},
                {"TPBucketLift", "Ви не можете телепортуватися, перебуваючи в ковшовому підйомнику!"},
                {"TPRegLift", "Ви не можете телепортуватися, перебуваючи в ліфті!"},
                {"TPSafeZone", "Ви не можете телепортуватися із безпечної зони!"},
                {"TPFlagZone", "Ви не можете телепортуватися із цієї зони!"},
                {"TPNoEscapeBlocked", "Ви не можете телепортуватися поки активне блокування!"},
                {"TPCrafting", "Ви не можете телепортуватись у процесі крафту!"},
                {"TPBlockedItem", "Ви не можете телепортуватися поки що несете: {0}!"},
                {"TooCloseToMon", "Ви не можете телепортуватися так близько до {0}!"},
                {"TPHomeSafeZoneOnly", "Ви можете телепортуватися додому лише з безпечної зони!" },
                {"TooCloseToCave", "Ви не можете телепортуватись так близько до печери!"},
                {"HomeTooCloseToCave", "Ви не можете зберегти розташування як будинок так близько до печери!"},
                {"HomeTooCloseToMon", "Ви не можете зберегти розташування як будинок так близько до пам'ятника!"},
                {"CannotTeleportFromHome", "Ви повинні вийти з вашої бази, перш ніж телепортувати!"},
                {"WaitGlobalCooldown", "Ви повинні почекати, поки ваш глобальний телепорт перезаряджається!" },

                {"DM_TownTP", "Ви телепортовані в {0}!"},
                {"DM_TownTPNoLocation", "Розташування <color=yellow>{0}</color> на даний момент не встановлено!"},
                {"DM_TownTPDisabled", "<color=yellow>{0}</color> в даний момент вимкнено у файлі налаштувань!"},
                {"DM_TownTPLocation", "Ви встановили розташування <color=yellow>{0}</color> в {1}!"},
                {"DM_TownTPCreated", "Ви створили команду: <color=yellow>{0}</color>"},
                {"DM_TownTPRemoved", "Ви видалили команду: <color=yellow>{0}</color>"},
                {"DM_TownTPDoesNotExist", "Команда не існує: <color=yellow>{0}</color>"},
                {"DM_TownTPExists", "Команда <color=yellow>{0}</color> вже існує!"},
                {"DM_TownTPLocationsCleared", "Ви очистили всі місця для {0}!"},
                {"DM_TownTPStarted", "Телепортація в {0} через {1} секунд!"},
                {"DM_TownTPCooldown", "Ваш телепорт перезаряджається. Вам необхідно почекати {0} до наступної телепортації."},
                {"DM_TownTPCooldownBypass", "Ваш телепорт був на перезарядженні. Ви обрали уникнути очікування, сплативши {0} з вашого балансу."},
                {"DM_TownTPCooldownBypassF", "Ваш телепорт перезаряджається. У вас недостатньо коштів ({0}), щоб уникнути очікування."},
                {"DM_TownTPCooldownBypassP", "Ви можете вибрати оплатити {0} щоб уникнути очікування перезаряджання." },
                {"DM_TownTPCooldownBypassP2", "Введіть <color=yellow>/{0} {1}</color>" },
                {"DM_TownTPLimitReached", "Ви вичерпали щоденний ліміт {0} телепортацій сьогодні! Ви повинні почекати {1} до наступної телепортації."},
                {"DM_TownTPAmount", "У вас залишилось {0} телепортацій <color=yellow>{1}</color> сьогодні!"},

                {"Days", "днів" },
                {"Hours", "годин" },
                {"Minutes", "хвилин" },
                {"Seconds", "секунд" },

                {"BlockedMarkerTeleport", "Вам заблоковано використання маркера телепортації!" },
                {"BlockedAuthMarkerTeleport", "Вам заблоковано використання маркера телепортації! (TC)" },
                {"Interrupted", "Вашу телепортацію було перервано!"},
                {"InterruptedTarget", "Телепортація {0} була перервана!"},
                {"Unlimited", "Не обмежено"},
                {
                    "TPInfoGeneral", string.Join(NewLine, new[]
                    {
                        "Будь ласка, вкажіть модуль, про який ви хочете переглянути інформацію.",
                        "Доступні модулі: ",
                    })
                },
                {
                    "TPHelpGeneral", string.Join(NewLine, new[]
                    {
                        "<color=yellow>/tpinfo</color> - Відображає ліміти та перезарядження.",
                        "Будь ласка, вкажіть модуль, за яким ви хочете отримати допомогу.",
                        "Доступні модулі: ",
                    })
                },
                {
                    "TPHelpadmintp", string.Join(NewLine, new[]
                    {
                        "Як адмін, ви маєте доступ до наступних команд:",
                        "<color=yellow>/tp \"ім'я гравця\"</color> - Телепортує вас до вказаного гравця.",
                        "<color=yellow>/tp \"ім'я гравця\" \"ім'я гравця 2\"</color> - Телепортує гравця з ім'ям 'ім'я гравця' до гравця 'ім'я гравця 2'.",
                        "<color=yellow>/tp x y z</color> - Телепортує вас до зазначених координат.",
                        "<color=yellow>/tpl</color> - Відображає список збережених позицій.",
                        "<color=yellow>/tpl \"назва розташування\"</color> - Телепортує вас у збережене місцезнаходження.",
                        "<color=yellow>/tpsave \"назва розташування\"</color> - Зберігає ваше поточне розташування з вказаною назвою.",
                        "<color=yellow>/tpremove \"назва розташування\"</color> - Видаляє розташування зі списку збережених.",
                        "<color=yellow>/tpb</color> - Телепортує вас назад на місце, де ви були перед телепортацією.",
                        "<color=yellow>/home radius \"радіус\"</color> - Знайти всі будинки в радіусі.",
                        "<color=yellow>/home delete \"ім'я гравця або ID\" \"назва будинку\"</color> - Видаляє будинок із вказаним ім'ям, що належить вказаному гравцю.",
                        "<color=yellow>/home tp \"ім'я гравця або ID\" \"назва будинку\"</color> - Телепортує вас до будинку гравця із зазначеною назвою, що належить вказаному гравцю.",
                        "<color=yellow>/home homes \"ім'я гравця або ID\"</color> - Відображає список усіх будинків, що належать зазначеному гравцеві."
                    })
                },
                {
                    "TPHelphome", string.Join(NewLine, new[]
                    {
                        "Використовуючи наступні команди, ви можете встановити місце розташування вашого будинку, щоб потім до нього телепортуватися:",
                        "<color=yellow>/home add \"назва будинку\"</color> - Зберігає ваше поточне розташування як ваш будинок із зазначеною назвою.",
                        "<color=yellow>/home list</color> - Відображає список усіх місць розташування, збережених вами як будинок.",
                        "<color=yellow>/home remove \"назва будинку\"</color> - Видаляє розташування збереженого будинку з вказаною назвою.",
                        "<color=yellow>/home \"назва будинку\"</color> - Телепортує вас у місцезнаходження будинку з вказаною назвою."
                    })
                },
                {
                    "TPHelptpr", string.Join(NewLine, new[]
                    {
                        "Використовуючи ці команди, ви можете надіслати запит на телепортацію до гравця або прийняти чийсь запит:",
                        "<color=yellow>/tpr \"ім'я гравця\"</color> - Надсилає запит на телепортацію гравцю із зазначеним ім'ям.",
                        "<color=yellow>/tpa</color> - Прийняти запит на телепортацію.",
                        "<color=yellow>/tpat</color> - Увімк./Вимк. автоматичне прийняття запитів на телепортацію до вас /tpa.",
                        "<color=yellow>/tpc</color> - Скасувати запит на телепортацію."
                    })
                },
                {
                    "TPSettingsGeneral", string.Join(NewLine, new[]
                    {
                        "Будь ласка, вкажіть модуль, налаштування якого потрібно переглянути.",
                        "Доступні модулі:",
                    })
                },
                {
                    "TPSettingshome", string.Join(NewLine, new[]
                    {
                        "Система будинків на даний момент має такі включені параметри:",
                        "Час між телепортами: {0}",
                        "Щоденний ліміт телепортацій: {1}",
                        "Кількість збережених будинків: {2}"
                    })
                },
                {
                    "TPSettingsbandit", string.Join(NewLine, new[]
                    {
                        "Система Табір бандитів наразі має такі включені параметри:",
                        "Час між телепортами: {0}",
                        "Щоденний ліміт телепортацій: {1}"
                    })
                },
                {
                    "TPSettingsoutpost", string.Join(NewLine, new[]
                    {
                        "Система Місто NPC в даний момент має наступні параметри:",
                        "Час між телепортами: {0}",
                        "Щоденний ліміт телепортацій: {1}"
                    })
                },
                {
                    "TPSettingstpr", string.Join(NewLine, new[]
                    {
                        "Система TPR в даний момент має наступні параметри:",
                        "Час між телепортами: {0}",
                        "Щоденний ліміт телепортацій: {1}"
                    })
                },
                {
                    "TPSettingstown", string.Join(NewLine, new[]
                    {
                        "У Системі Міст включені такі параметри:",
                        "Час між телепортами: {0}",
                        "Щоденний ліміт телепортацій: {1}"
                    })
                },
                {
                    "TPSettingsdynamic", string.Join(NewLine, new[]
                    {
                        "У системі {0} включені такі параметри:",
                        "Час між телепортами: {1}",
                        "Щоденний ліміт телепортацій: {2}"
                    })
                },

                {"TPT_True", "enabled"},
                {"TPT_False", "disabled"},
                {"TPT_clan", "{1} clan has been {0}."},
                {"TPT_friend", "{1} friend has been {0}."},
                {"TPT_team", "{1} team has been {0}."},
                {"NotValidTPT", "Not valid, player is not"},
                {"NotValidTPTFriend", " a friend!"},
                {"NotValidTPTTeam", " on your team!"},
                {"NotValidTPTClan", " in your clan!"},

                {"TPTInfo", "{4} auto accepts teleport requests.\n<color={5}>Green</color> = <color={5}>Enabled</color>\n<color={6}>Red</color> = <color={6}>Disabled</color>\n\n/{0} <color={1}>clan</color> - Toggle {4} for clan members/allies.\n/{0} <color={2}>team</color> - Toggle {4} for teammates.\n/{0} <color={3}>friend</color> - Toggle {4} for friends."},

                {"PlayerNotFound", "Вказаного гравця не виявлено, будь ласка, спробуйте ще раз!"},
                {"MultiplePlayers", "Знайдено декілька гравців: {0}"},
                {"CantTeleportToSelf", "Ви не можете телепортуватися до себе!"},
                {"CantTeleportPlayerToSelf", "Ви не можете телепортувати гравця до себе!"},
                {"TeleportPendingTPC", "Ви не можете ініціювати телепортацію, поки ви маєте активний запит! Використовуйте <color=yellow>/tpc</color>, щоб скасувати його."},
                {"TeleportPendingTarget", "Ви не можете надіслати запит до того, хто в процесі телепортації!"},
                {"LocationExists", "Розташування з такою назвою вже існує в {0}!"},
                {"LocationExistsNearby", "Розташування з назвою {0} вже існує поряд із поточною позицією!"},
                {"LocationNotFound", "Не знайдено місцезнаходження з такою назвою!"},
                {"NoPreviousLocationSaved", "Попереднє розташування не збережене!"},
                {"HomeExists", "Ви вже зберегли будинок із такою назвою!"},
                {"HomeExistsNearby", "Будинок з назвою {0} вже існує поряд із поточною позицією!"},
                {"HomeNotFound", "Будинок з такою назвою не знайдено!"},
                {"InvalidCoordinates", "Ви вказали неправильні координати!"},
                {"InvalidHelpModule", "Вказано неправильний модуль!"},
                {"InvalidCharacter", "Ви використовували неприпустимий символ, обмежтеся літерами від a до z та цифрами."},
                {
                    "SyntaxCommandTP", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/tp</color> можливе лише так:",
                        "<color=yellow>/tp \"ім'я гравця\"</color> - Телепортує вас до вказаного гравця.",
                        "<color=yellow>/tp \"ім'я гравця\" \"ім'я гравця 2\"</color> - Телепортує гравця з ім'ям 'ім'я гравця' до гравця 'ім'я гравця 2'.",
                        "<color=yellow>/tp x y z</color> - Телепортує вас до зазначених координат.",
                        "<color=yellow>/tp \"ім'я гравця\" x y z</color> - Телепортує гравця з ім'ям 'ім'я гравця' до зазначених координат."
                    })
                },
                {
                    "SyntaxCommandTPL", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/tpl</color> можливе лише так:",
                        "<color=yellow>/tpl</color> - Відображає список збережених позицій.",
                        "<color=yellow>/tpl \"назва розташування\"</color> - Телепортує вас у місце із зазначеною назвою."
                    })
                },
                {
                    "SyntaxCommandTPSave", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/tpsave</color> можливе лише так:",
                        "<color=yellow>/tpsave \"назва розташування\"</color> - Зберігає ваше поточне розташування з вказаною назвою."
                    })
                },
                {
                    "SyntaxCommandTPRemove", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/tpremove</color> можливе лише так:",
                        "<color=yellow>/tpremove \"назва розташування\"</color> - Видаляє розташування за вказаною назвою."
                    })
                },
                {
                    "SyntaxCommandTPN", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/tpn</color> можливе лише так:",
                        "<color=yellow>/tpn \"ім'я гравця\"</color> - Телепортує вас на відстань за замовчуванням позаду гравця із зазначеним ім'ям.",
                        "<color=yellow>/tpn \"ім'я гравця\" \"відстань\"</color> - Телепортує вас на вказану відстань позаду гравця із вказаним ім'ям."
                    })
                },
                {
                    "SyntaxCommandSetHome", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/home add</color> можливе лише так:",
                        "<color=yellow>/home add \"назва\"</color> - Зберігає ваше поточне розташування як ваш будинок із зазначеною назвою."
                    })
                },
                {
                    "SyntaxCommandRemoveHome", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/home remove</color> можливе лише так:",
                        "<color=yellow>/home remove \"назва\"</color> - Видаляє розташування будинку з вказаною назвою."
                    })
                },
                {
                    "SyntaxCommandHome", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/home</color> можливе лише так:",
                        "<color=yellow>/home \"назва\"</color> - Телепортує вас у ваш будинок із зазначеною назвою.",
                        "<color=yellow>/home \"назва\" pay</color> - Телепортує вас у ваш будинок із зазначеною назвою, уникаючи перезарядки, заплативши за це.",
                        "<color=yellow>/home add \"назва\"</color> - Зберігає ваше поточне розташування як ваш будинок із зазначеною назвою.",
                        "<color=yellow>/home list</color> - Відображає список усіх місць розташування, збережених вами як будинок.",
                        "<color=yellow>/home remove \"назва\"</color> - Видаляє розташування будинку з вказаною назвою."
                    })
                },
                {
                    "SyntaxCommandHomeAdmin", string.Join(NewLine, new[]
                    {
                        "<color=yellow>/home radius \"радіус\"</color> - Відображає список усіх будинків у радіусі(10).",
                        "<color=yellow>/home delete \"ім'я гравця або ID\" \"назва\"</color> - Видаляє будинок із зазначеною назвою, що належить вказаному гравцю.",
                        "<color=yellow>/home tp \"ім'я гравця або ID\" \"назва\"</color> - Телепортує вас до будинку із зазначеною назвою, що належить вказаному гравцю.",
                        "<color=yellow>/home homes \"ім'я гравця або ID\"</color> - Відображає список усіх будинків, що належать вказаному гравцю."
                    })
                },
                {
                    "SyntaxCommandTown", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/town</color> можливе лише так:",
                        "<color=yellow>/town</color> - Телепортує вас до міста.",
                        "<color=yellow>/town pay</color> - Телепортує вас до міста з оплатою штрафу."
                    })
                },
                {
                    "SyntaxCommandTownAdmin", string.Join(NewLine, new[]
                    {
                        "<color=yellow>/town set</color> - Зберігає поточне розташування як Місто.",
                    })
                },
                {
                    "SyntaxCommandOutpost", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/outpost</color> можливе лише так:",
                        "<color=yellow>/outpost</color> - Телепортирует вас в Город NPC.",
                        "<color=yellow>/outpost pay</color> - Телепортує вас у Місто NPC з оплатою штрафу."
                    })
                },
                {
                    "SyntaxCommandOutpostAdmin", string.Join(NewLine, new[]
                    {
                        "<color=yellow>/outpost set</color> - Зберігає поточне розташування як Місто NPC.",
                    })
                },
                {
                    "SyntaxCommandBandit", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/bandit</color> можливе лише так:",
                        "<color=yellow>/bandit</color> - Телепортує вас до Табору бандитів.",
                        "<color=yellow>/bandit pay</color> - Телепортує вас до Табору бандитів з оплатою штрафу."
                    })
                },
                {
                    "SyntaxCommandBanditAdmin", string.Join(NewLine, new[]
                    {
                        "<color=yellow>/bandit set</color> - Зберігає поточне розташування як Табір бандитів.",
                    })
                },
                {
                    "SyntaxCommandHomeDelete", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/home delete</color> можливе лише так:",
                        "<color=yellow>/home delete \"ім'я гравця або ID\" \"назва\"</color> - Видаляє будинок із зазначеною назвою, що належить вказаному гравцю."
                    })
                },
                {
                    "SyntaxCommandHomeAdminTP", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/home tp</color> можливе лише так:",
                        "<color=yellow>/home tp \"ім'я гравця або ID\" \"назва\"</color> - Телепортує вас до будинку гравця із зазначеною назвою, що належить вказаному гравцю."
                    })
                },
                {
                    "SyntaxCommandHomeHomes", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/home homes</color> можливе лише так:",
                        "<color=yellow>/home homes \"ім'я гравця або ID\"</color> - Відображає список усіх будинків, що належать вказаному гравцю."
                    })
                },
                {
                    "SyntaxCommandListHomes", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/home list</color> можливе лише так:",
                        "<color=yellow>/home list</color> - Відображає список усіх місць розташування, збережених вами як будинок."
                    })
                },
                {
                    "SyntaxCommandTPR", string.Join(NewLine, new[]
                    {
                        "Сталася синтаксична помилка!",
                        "Використання команди <color=yellow>/tpr</color> можливе лише так:",
                        "<color=yellow>/tpr \"ім'я гравця або ID\"</color> - Надсилає вказаному гравцеві запит на телепортацію."
                    })
                },
                {
                    "SyntaxCommandTPA", string.Join(NewLine, new[]
                    {
                        "Відбулася синтаксична помилка!",
                        "Використання команди <color=yellow>/tpa</color> можливе лише таким чином:",
                        "<color=yellow>/tpa</color> - Прийняти вхідний запит на телепортацію."
                    })
                },
                {
                    "SyntaxCommandTPC", string.Join(NewLine, new[]
                    {
                        "Відбулася синтаксична помилка!",
                        "Використання команди <color=yellow>/tpc</color> можливе лише таким чином:",
                        "<color=yellow>/tpc</color> - Скасувати запит на телепортацію."
                    })
                },
                {
                    "SyntaxConsoleCommandToPos", string.Join(NewLine, new[]
                    {
                        "Відбулася синтаксична помилка!",
                        "Використання консольної команди <color=orange>teleport.topos</color> можливе лише таким чином:",
                        " > <color=orange>teleport.topos \"ім'я гравця\" x y z</color>"
                    })
                },
                {
                    "SyntaxConsoleCommandToPlayer", string.Join(NewLine, new[]
                    {
                        "Відбулася синтаксична помилка!",
                        "Використання консольної команди <color=orange>teleport.toplayer</color> можливе лише таким чином:",
                        " > <color=orange>teleport.toplayer \"ім'я гравця або ID\" \"ім'я гравця 2|id 2\"</color>"
                    })
                },
                {"LogTeleport", "{0} телепортовано до {1}."},
                {"LogTeleportPlayer", "{0} телепортував {1} до {2}."},
                {"LogTeleportBack", "{0} телепортований назад, до попереднього розташування."},
                {"DiscordLogTPA", "{0} and {1} accepted TPA {2} times"}
            };

            foreach (var key in config.DynamicCommands.Keys)
            {
                uk[key] = key;
            }

            foreach (var key in monumentMessages)
            {
                uk[key] = key;
            }

            lang.RegisterMessages(uk, this, "uk");
        }

        private void Init()
        {
            Unsubscribe(nameof(OnPlayerSleep));
            Unsubscribe(nameof(OnPlayerRespawn));
            Unsubscribe(nameof(OnPlayerViolation));
            Unsubscribe(nameof(OnEntityTakeDamage));
            Unsubscribe(nameof(OnPlayerSleepEnded));
            Unsubscribe(nameof(OnPlayerDisconnected));
            InitializeDynamicCommands();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player != null)
            {
                delayedTeleports.Remove(player.userID);
                GetPlayerCode(player);
            }
        }

        private string GetPlayerCode(BasePlayer player)
        {
            if (!_playerToCode.TryGetValue(player.UserIDString, out var code))
            {
                code = (player.userID % 90000 + 10000).ToString();
                _playerToCode[player.UserIDString] = code;
            }
            _codeToPlayer[code] = player;
            return code;
        }

        private Dictionary<string, StoredData> _DynamicData = new Dictionary<string, StoredData>();

        public class StoredData
        {
            public Dictionary<ulong, TeleportData> TPData = new Dictionary<ulong, TeleportData>();
            public bool Changed = true;
        }

        private void LoadDataAndPerms()
        {
            dataAdmin = GetFile("Admin");
            try { _Admin = dataAdmin.ReadObject<Dictionary<ulong, AdminData>>(); } catch (Exception ex) { Puts("Admin datafile: {0}", ex); }
            if (_Admin == null) { _Admin = new Dictionary<ulong, AdminData>(); changedAdmin = true; }

            dataHome = GetFile("Home");
            try { _Home = dataHome.ReadObject<Dictionary<ulong, HomeData>>(); } catch (Exception ex) { Puts("Home datafile: {0}", ex); }
            if (_Home == null) { _Home = new Dictionary<ulong, HomeData>(); changedHome = true; }
            if (!config.Home.AllowTugboats) _Home.Values.ToList().ForEach(homeData => { if (homeData?.boats?.Count > 0) { homeData.boats.Clear(); changedHome = true; } });

            dataTPT = GetFile("TPT");
            try { TPT = dataTPT.ReadObject<Dictionary<string, List<string>>>(); } catch { }
            if (TPT == null) { new Dictionary<string, List<string>>(); changedTPT = true; }

            foreach (var entry in config.DynamicCommands)
            {
                if (!entry.Value.Enabled) continue;

                var dcf = GetFile(entry.Key);
                Dictionary<ulong, TeleportData> data = null;

                try
                {
                    data = dcf.ReadObject<Dictionary<ulong, TeleportData>>();
                }
                catch
                {

                }

                if (data == null)
                {
                    data = new Dictionary<ulong, TeleportData>();
                }

                GetSettings(entry.Key).Teleports = _DynamicData[entry.Key] = new StoredData
                {
                    TPData = data,
                    Changed = true
                };
            }

            dataTPR = GetFile("TPR");
            try { _TPR = dataTPR.ReadObject<Dictionary<ulong, TeleportData>>(); } catch (Exception ex) { Puts("TPR: {0}", ex); }
            if (_TPR == null) { _TPR = new Dictionary<ulong, TeleportData>(); changedTPR = true; }

            dataDisabled = GetFile("DisabledCommands");
            try { DisabledCommandData = dataDisabled.ReadObject<DisabledData>(); } catch (Exception ex) { Puts("DC: {0}", ex); }
            if (DisabledCommandData == null) { DisabledCommandData = new DisabledData(); }

            permission.RegisterPermission("nteleportation.blocktpmarker", this);
            permission.RegisterPermission("nteleportation.skipwipewaittime", this);
            permission.RegisterPermission("nteleportation.locationradiusbypass", this);
            permission.RegisterPermission("nteleportation.ignoreglobalcooldown", this);
            permission.RegisterPermission("nteleportation.norestrictions", this);
            permission.RegisterPermission("nteleportation.globalcooldownvip", this);
            permission.RegisterPermission("nteleportation.tugboatsinterruptbypass", this);
            permission.RegisterPermission("nteleportation.tugboatssethomebypass", this);
            permission.RegisterPermission(PermAdmin, this);
            permission.RegisterPermission(PermFoundationCheck, this);
            permission.RegisterPermission(PermDeleteHome, this);
            permission.RegisterPermission(PermHome, this);
            permission.RegisterPermission(PermHomeHomes, this);
            permission.RegisterPermission(PermImportHomes, this);
            permission.RegisterPermission(PermRadiusHome, this);
            permission.RegisterPermission(PermDisallowTpToMe, this);
            permission.RegisterPermission(PermTp, this);
            permission.RegisterPermission(PermTpB, this);
            permission.RegisterPermission(PermTpR, this);
            permission.RegisterPermission(PermTpA, this);
            permission.RegisterPermission(PermTpConsole, this);
            permission.RegisterPermission(PermTpT, this);
            permission.RegisterPermission(PermTpN, this);
            permission.RegisterPermission(PermTpL, this);
            permission.RegisterPermission(PermTpRemove, this);
            permission.RegisterPermission(PermTpSave, this);
            permission.RegisterPermission(PermWipeHomes, this);
            permission.RegisterPermission(PermCraftHome, this);
            permission.RegisterPermission(PermCaveHome, this);
            permission.RegisterPermission(PermCraftTpR, this);
            permission.RegisterPermission(PermCaveTpR, this);
            permission.RegisterPermission(PermExempt, this);
            permission.RegisterPermission(PermTpMarker, this);

            CheckPerms(config.Home.VIPCooldowns);
            CheckPerms(config.Home.VIPCountdowns);
            CheckPerms(config.Home.VIPDailyLimits);
            CheckPerms(config.Home.VIPHomesLimits);
            CheckPerms(config.TPR.VIPCooldowns);
            CheckPerms(config.TPR.VIPCountdowns);
            CheckPerms(config.TPR.VIPDailyLimits);
            CheckPerms(config.Settings.TPB.Countdowns);

            foreach (var entry in config.DynamicCommands)
            {
                RegisterCommand(entry.Key, entry.Value, false);
            }
        }

        private bool CanBypassRestrictions(string userid) => permission.UserHasPermission(userid, "nteleportation.norestrictions");

        private void RegisterCommand(string command, string callback, string perm = null)
        {
            if (!string.IsNullOrEmpty(command) && !command.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                AddCovalenceCommand(command, callback, perm);
            }
        }

        private void UnregisterCommand(string command)
        {
            covalence.UnregisterCommand(command, this);
        }

        private void RegisterCommand(string key, TownSettings settings, bool justCreated)
        {
            CheckPerms(settings.VIPCooldowns);
            CheckPerms(settings.VIPCountdowns);
            CheckPerms(settings.VIPDailyLimits);

            string tpPerm = $"{Name}.tp{key}".ToLower();
            string craftPerm = $"{Name}.craft{key}".ToLower();
            string cavePerm = $"{Name}.cave{key}".ToLower();

            if (!permission.PermissionExists(tpPerm, this))
            {
                permission.RegisterPermission(tpPerm, this);
            }

            if (!permission.PermissionExists(craftPerm))
            {
                permission.RegisterPermission(craftPerm, this);
            }

            if (!permission.PermissionExists(cavePerm))
            {
                permission.RegisterPermission(cavePerm, this);
            }

            if (justCreated)
            {
                settings.Teleports = _DynamicData[key] = new();
            }
        }

        private DynamicConfigFile GetFile(string name)
        {
            var fileName = string.IsNullOrEmpty(config.Settings.DataFileFolder) ? $"{Name}{name}" : $"{config.Settings.DataFileFolder}{Path.DirectorySeparatorChar}{name}";
            var file = Interface.Oxide.DataFileSystem.GetFile(fileName);
            file.Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            file.Settings.Converters = new JsonConverter[] { new UnityVector3Converter(), new CustomComparerDictionaryCreationConverter<string>(StringComparer.OrdinalIgnoreCase) };
            return file;
        }

        private void SetGlobalCooldown(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "nteleportation.ignoreglobalcooldown"))
            {
                return;
            }
            if (config.Settings.GlobalVIP > 0f && permission.UserHasPermission(player.UserIDString, "nteleportation.globalcooldownvip"))
            {
                ulong userid = player.userID;
                TeleportCooldowns[userid] = Time.time + config.Settings.GlobalVIP;
                timer.Once(config.Settings.GlobalVIP, () => TeleportCooldowns.Remove(userid));
            }
            else if (config.Settings.Global > 0f)
            {
                ulong userid = player.userID;
                TeleportCooldowns[userid] = Time.time + config.Settings.Global;
                timer.Once(config.Settings.Global, () => TeleportCooldowns.Remove(userid));
            }
        }

        private float GetGlobalCooldown(BasePlayer player)
        {
            float cooldown;
            if (!TeleportCooldowns.TryGetValue(player.userID, out cooldown))
            {
                return 0f;
            }

            return cooldown - Time.time;
        }

        private bool IsEmptyMap()
        {
            foreach (var b in BuildingManager.server.buildingDictionary)
            {
                if (b.Value.HasDecayEntities() && b.Value.decayEntities.ToList().Exists(de => de != null && de.OwnerID.IsSteamId()))
                {
                    return false;
                }
            }
            return true;
        }

        private void CheckNewSave()
        {
            if (!newSave && !IsEmptyMap())
            {
                return;
            }

            bool changed = false;
            bool cleared = false;

            if (config.Settings.WipeOnUpgradeOrChange)
            {
                if (_Home.Count > 0)
                {
                    cleared = true;
                    _Home.Clear();
                    changedHome = true;
                }

                if (_TPR.Count > 0)
                {
                    cleared = true;
                    _TPR.Clear();
                    changedTPR = true;
                }

                foreach (var entry in config.DynamicCommands.ToList())
                {
                    if (entry.Value.Location != Vector3.zero || entry.Value.Locations.Count > 0)
                    {
                        entry.Value.Location = Vector3.zero;
                        entry.Value.Locations.Clear();
                        cleared = true;
                    }
                }

                if (cleared) Puts("Rust was upgraded or map changed - clearing homes and all locations!");
            }
            else
            {
                Puts("Rust was upgraded or map changed - homes, town, islands, outpost, bandit, etc may be invalid!");
            }

            foreach (var entry in config.DynamicCommands.ToList())
            {
                if (!string.IsNullOrEmpty(entry.Value.MonumentMarkerName))
                {
                    if (TrySetNewTownPosition(entry.Value))
                    {
                        changed = true;
                    }
                }
            }

            if (cleared || changed)
            {
                SaveConfig();
            }
        }

        bool TrySetNewTownPosition(TownSettings town)
        {
            foreach (var prefab in World.Serialization.world.prefabs)
            {
                if (prefab.id == 1724395471 && prefab.category == town.MonumentMarkerName)
                {
                    var pos = new Vector3(prefab.position.x, prefab.position.y, prefab.position.z);
                    try { pos += town.MonumentMarkerNameOffset.ToVector3(); } catch { }
                    if (pos.y < TerrainMeta.HeightMap.GetHeight(pos))
                    {
                        Puts("Invalid position set under the map for {0} {1}", prefab.category, pos);
                        Puts("You can specify an offset in the config to correct this:");
                        Puts("Set Position From Monument Marker Name Offset");
                        Puts("e.g: 0 15 0");
                        return false;
                    }
                    else
                    {
                        Puts($"Set {prefab.category} teleport position to: {pos}");
                        town.Locations.Clear();
                        town.Location = pos;
                        town.Locations.Add(pos);
                        return true;
                    }
                }
            }
            return false;
        }

        void OnServerInitialized()
        {
            if (config.Settings.Interrupt.Hurt || config.Settings.Interrupt.Cold || config.Settings.Interrupt.Hot)
            {
                Subscribe(nameof(OnEntityTakeDamage));
            }

            if (config.Settings.TeleportHomeSafeZone > 0f)
            {
                Subscribe(nameof(OnPlayerSleep));
            }

            if (config.Settings.RespawnOutpost)
            {
                Subscribe(nameof(OnPlayerRespawn));
            }

            Subscribe(nameof(OnPlayerSleepEnded));
            Subscribe(nameof(OnPlayerDisconnected));

            boundary = TerrainMeta.Size.x / 2;

            foreach (var item in config.Settings.BlockedItems)
            {
                var definition = ItemManager.FindItemDefinition(item.Key);
                if (definition == null)
                {
                    Puts("Blocked item not found: {0}", item.Key);
                    continue;
                }
                ReverseBlockedItems[definition.itemid] = item.Value;
            }

            LoadDataAndPerms();
            CheckNewSave();
            AddCovalenceCommands();
            foreach (var player in BasePlayer.activePlayerList) OnPlayerConnected(player);
            _cmc = ServerMgr.Instance.StartCoroutine(SetupMonuments());
        }

        private void AddCovalenceCommands()
        {
            AddCovalenceCommand("toggletpmarker", nameof(CommandBlockMapMarker));
            if (config.Settings.TPREnabled)
            {
                AddCovalenceCommand("tpr", nameof(CommandTeleportRequest));
            }
            if (config.Settings.HomesEnabled)
            {
                AddCovalenceCommand("home", nameof(CommandHome));
                AddCovalenceCommand("sethome", nameof(CommandSetHome));
                AddCovalenceCommand("listhomes", nameof(CommandListHomes));
                AddCovalenceCommand("removehome", nameof(CommandRemoveHome));
                AddCovalenceCommand("radiushome", nameof(CommandHomeRadius));
                AddCovalenceCommand("deletehome", nameof(CommandHomeDelete));
                AddCovalenceCommand("tphome", nameof(CommandHomeAdminTP));
                AddCovalenceCommand("homehomes", nameof(CommandHomeHomes));
            }
            AddCovalenceCommand("tnt", nameof(CommandToggle));
            AddCovalenceCommand("tp", nameof(CommandTeleport));
            AddCovalenceCommand("tpn", nameof(CommandTeleportNear));
            AddCovalenceCommand("tpl", nameof(CommandTeleportLocation));
            AddCovalenceCommand("tpsave", nameof(CommandSaveTeleportLocation));
            AddCovalenceCommand("tpremove", nameof(CommandRemoveTeleportLocation));
            AddCovalenceCommand("tpb", nameof(CommandTeleportBack));
            AddCovalenceCommand("tpa", nameof(CommandTeleportAccept));
            AddCovalenceCommand("tpat", nameof(CommandTeleportAcceptToggle));
            AddCovalenceCommand("tpt", nameof(CommandTeleportAcceptToggle));
            AddCovalenceCommand("atp", nameof(CommandTeleportAcceptToggle));
            AddCovalenceCommand("wipehomes", nameof(CommandWipeHomes));
            AddCovalenceCommand("tphelp", nameof(CommandTeleportHelp));
            AddCovalenceCommand("tpinfo", nameof(CommandTeleportInfo));
            AddCovalenceCommand("tpc", nameof(CommandTeleportCancel));
            AddCovalenceCommand("teleport.toplayer", nameof(CommandTeleportII));
            AddCovalenceCommand("teleport.topos", nameof(CommandTeleportII));
            AddCovalenceCommand("teleport.importhomes", nameof(CommandImportHomes));
            AddCovalenceCommand("spm", nameof(CommandSphereMonuments));
            AddCovalenceCommand("nteleportationinfo", nameof(CommandPluginInfo));
        }

        void OnNewSave(string strFilename)
        {
            newSave = true;
        }

        void OnServerSave()
        {
            if (config.Settings.SaveDelay > 0)
            {
                timer.Once((float)config.Settings.SaveDelay, SaveAllInstant);
            }
            else
            {
                SaveAllInstant();
            }
        }

        void SaveAllInstant()
        {
            SaveTeleportsAdmin();
            SaveTeleportsHome();
            SaveTeleportsTPR();
            SaveTeleportsTPT();
            SaveTeleportsTown();
        }

        void OnServerShutdown() => SaveAllInstant();

        void Unload()
        {
            SaveAllInstant();
            foreach (BasePlayer current in BasePlayer.activePlayerList)
            {
                DestroyTeleportRequestCUI(current);
            }
            if (_cmc != null)
            {
                ServerMgr.Instance.StopCoroutine(_cmc);
                _cmc = null;
            }
        }

        void OnEntityTakeDamage(BasePlayer player, HitInfo hitInfo)
        {
            if (player == null || player.IsNpc || !player.userID.IsSteamId() || hitInfo == null) return;
            if (teleporting.ContainsKey(player.userID) && (hitInfo.damageTypes.Has(DamageType.Fall) || hitInfo.damageTypes.Has(DamageType.Suicide)))
            {
                hitInfo.damageTypes = new DamageTypeList();
                RemoveProtections(player.userID);
                if (teleporting.Count == 0) Unsubscribe(nameof(OnPlayerViolation));
                return;
            }
            if (permission.UserHasPermission(player.userID.ToString(), PermExempt)) return;
            if (!TeleportTimers.TryGetValue(player.userID, out var teleportTimer)) return;
            DamageType major = hitInfo.damageTypes.GetMajorityDamageType();

            NextTick(() =>
            {
                if (player == null || !hitInfo.hasDamage) return;
                if (major == DamageType.Cold)
                {
                    if (config.Settings.Interrupt.Cold && player.metabolism.temperature.value <= config.Settings.MinimumTemp)
                    {
                        SendInterruptMessage(teleportTimer, player, "TPTooCold");
                    }
                }
                else if (major == DamageType.Heat)
                {
                    if (config.Settings.Interrupt.Hot && player.metabolism.temperature.value >= config.Settings.MaximumTemp)
                    {
                        SendInterruptMessage(teleportTimer, player, "TPTooHot");
                    }
                }
                else if (config.Settings.Interrupt.Hurt)
                {
                    SendInterruptMessage(teleportTimer, player, "Interrupted");
                }
            });
        }

        private void SendInterruptMessage(TeleportTimer teleportTimer, BasePlayer player, string key)
        {
            PrintMsgL(teleportTimer.OriginPlayer, key);
            if (teleportTimer.TargetPlayer != null)
            {
                PrintMsgL(teleportTimer.TargetPlayer, "InterruptedTarget", teleportTimer.OriginPlayer?.displayName);
            }
            teleportTimer.Timer.Destroy();
            TeleportTimers.Remove(player.userID);
            Interface.CallHook("OnTeleportInterrupted", player, teleportTimer.Home, teleportTimer.UserID, teleportTimer.Town);
        }

        object OnPlayerViolation(BasePlayer player, AntiHackType type, float amount)
        {
            if (type == AntiHackType.InsideTerrain && teleporting.ContainsKey(player.userID))
            {
                return true;
            }

            return null;
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null) return;
            delayedTeleports.Remove(player.userID);
            if (teleporting.ContainsKey(player.userID))
            {
                ulong userID = player.userID;
                timer.Once(3f, () =>
                {
                    RemoveProtections(userID);
                    if (teleporting.Count == 0) Unsubscribe(nameof(OnPlayerViolation));
                });
            }
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (player == null) return;
            if (config.Settings.TeleportHomeSafeZone > 0f)
            {
                DelayedTeleportHome(player);
            }
            if (PendingRequests.TryGetValue(player.userID, out var reqTimer))
            {
                var originPlayer = PlayersRequests[player.userID];
                if (originPlayer)
                {
                    PlayersRequests.Remove(originPlayer.userID);
                    PrintMsgL(originPlayer, "RequestTargetOff");
                }
                reqTimer.Destroy();
                PendingRequests.Remove(player.userID);
                PlayersRequests.Remove(player.userID);
            }
            if (TeleportTimers.TryGetValue(player.userID, out var teleportTimer))
            {
                teleportTimer.Timer.Destroy();
                TeleportTimers.Remove(player.userID);
            }
            RemoveProtections(player.userID);
        }

        private void SaveTeleportsAdmin()
        {
            if (_Admin == null || !changedAdmin) return;
            dataAdmin.WriteObject(_Admin);
            changedAdmin = false;
        }

        private void SaveTeleportsHome()
        {
            if (_Home == null || !changedHome) return;
            dataHome.WriteObject(_Home);
            changedHome = false;
        }

        private void SaveTeleportsTPR()
        {
            if (_TPR == null || !changedTPR) return;
            dataTPR.WriteObject(_TPR);
            changedTPR = false;
        }

        private void SaveTeleportsTPT()
        {
            if (TPT == null || !changedTPT) return;
            dataTPT.WriteObject(TPT);
            changedTPT = false;
        }

        private void SaveTeleportsTown()
        {
            foreach (var entry in _DynamicData.ToList())
            {
                if (entry.Value.Changed)
                {
                    var fileName = string.IsNullOrEmpty(config.Settings.DataFileFolder) ? $"{Name}{entry.Key}" : $"{config.Settings.DataFileFolder}{Path.DirectorySeparatorChar}{entry.Key}";
                    Interface.Oxide.DataFileSystem.WriteObject(fileName, entry.Value.TPData);
                    entry.Value.Changed = false;
                }
            }
        }

        private void SaveLocation(BasePlayer player, Vector3 position, string home, ulong uid, bool town, bool build = true, bool craft = true, bool cave = true)
        {
            if (player == null || _Admin == null || !IsAllowed(player, PermTpB)) return;
            if (!_Admin.TryGetValue(player.userID, out var adminData) || adminData == null)
                _Admin[player.userID] = adminData = new();
            adminData.PreviousLocation = position;
            adminData.BuildingBlocked = build;
            adminData.AllowCrafting = craft;
            adminData.AllowCave = cave;
            adminData.Town = town;
            adminData.Home = home;
            adminData.UserID = uid;
            changedAdmin = true;
            PrintMsgL(player, "AdminTPBackSave");
        }

        private void RemoveLocation(BasePlayer player)
        {
            if (!_Admin.TryGetValue(player.userID, out var adminData))
                return;
            adminData.PreviousLocation = Vector3.zero;
            changedAdmin = true;
        }

        private Coroutine _cmc;
        private bool _cmcCompleted;
        private List<PrefabInfo> PrefabVolumeInfo = new();

        public bool GetCustomMapPrefabRadius(Vector2 v, out float radius)
        {
            foreach (var info in PrefabVolumeInfo)
            {
                if ((v - info.positionXZ).magnitude > 1f)
                {
                    continue;
                }
                if (info.IsInBounds(v))
                {
                    radius = info.extents.Max();
                    return true;
                }
            }
            radius = 0f;
            return false;
        }

        public bool GetCustomMapPrefabName(ProtoBuf.PrefabData prefab, out string prefabName) => (prefabName = prefab.id switch
        {
            //79883367 => "assets/bundled/prefabs/modding/volumes_and_triggers/prevent_building_cylinder.prefab",
            //3073835983 => "assets/bundled/prefabs/modding/volumes_and_triggers/prevent_building_monument_cube.prefab",
            //131040489 => "assets/bundled/prefabs/modding/volumes_and_triggers/prevent_building_monument_sphere.prefab",
            //2208164178 => "assets/bundled/prefabs/modding/volumes_and_triggers/prevent_building_ramp.prefab",
            316558065u => "assets/bundled/prefabs/modding/volumes_and_triggers/safezonesphere.prefab",
            4190049974u => "assets/bundled/prefabs/modding/volumes_and_triggers/prevent_building_cube.prefab",
            3224970585u => "assets/bundled/prefabs/modding/volumes_and_triggers/prevent_building_sphere.prefab",
            _ => null
        }) != null;

        private void SetupVolumeOrTrigger(ProtoBuf.PrefabData prefab, string prefabName)
        {
            var extents = new Vector3(prefab.scale.x, prefab.scale.y, prefab.scale.z);
            if (extents.Max() <= 1f)
            {
                return;
            }
            var text = Utility.GetFileNameWithoutExtension(prefabName);
            var position = new Vector3(prefab.position.x, prefab.position.y, prefab.position.z);
            var rotation = new Quaternion(prefab.rotation.x, prefab.rotation.y, prefab.rotation.z, 0f);
            if (prefab.id == 316558065)
            {
                extents *= 2f;
            }
            PrefabVolumeInfo.Add(new PrefabInfo(position: position, rotation: rotation, extents: extents, extra: 0f, name: text, prefab: prefab.category, sphere: !text.Contains("cube")));
        }

        private IEnumerator SetupMonuments()
        {
            int checks = 0;
            foreach (var prefab in World.Serialization.world.prefabs)
            {
                if (GetCustomMapPrefabName(prefab, out var prefabName))
                {
                    SetupVolumeOrTrigger(prefab, prefabName);
                    continue;
                }
                if (prefab.id == 1724395471 && prefab.category != "IGNORE_MONUMENT" && prefab.category != "prevent_building_monument_sphere")
                {
                    yield return CalculateMonumentSize(new(prefab.position.x, prefab.position.y, prefab.position.z), new(prefab.rotation.x, prefab.rotation.y, prefab.rotation.z, 0f), default, prefab.category, "monument_marker");
                }
                if (++checks >= 1000)
                {
                    yield return CoroutineEx.waitForSeconds(0.0025f);
                    checks = 0;
                }
            }
            foreach (var monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                var objname = monument.name;
                if (objname.Contains("monument_marker") || objname.Contains("prevent_building_monument_sphere"))
                {
                    continue;
                }
                var position = monument.transform.position;
                var rotation = monument.transform.rotation;
                var name = monument.displayPhrase.english.Trim();
                if (name.Contains("Lake") || name.Contains("Canyon") || name.Contains("Oasis"))
                {
                    continue;
                }
                if (string.IsNullOrEmpty(name))
                {
                    if (objname.Contains("cave"))
                    {
                        name = objname.Contains("cave_small") ? "Small Cave" : objname.Contains("cave_medium") ? "Medium Cave" : "Large Cave";
                    }
                    else name = objname;
                }
                if (name.Contains('/'))
                {
                    name = Utility.GetFileNameWithoutExtension(objname);
                }
                if (name.Contains("Oil Rig"))
                {
                    boundary = Mathf.Max(boundary, monument.Distance(Vector3.zero) + 100f);
                }
                if (objname.Contains("cave"))
                {
                    name += UnityEngine.Random.Range(1000, 9999);
#if DEBUG
                    Puts($"Adding Cave: {name}, pos: {position}");
#endif
                    caves[name] = position;
                }
                else if (config.Settings.Outpost.Exists(objname.Contains))
                {
                    yield return SetupOutpost(monument);
                }
                else if (config.Settings.Bandit.Exists(objname.Contains))
                {
                    yield return SetupBandit(monument);
                }
                else
                {
                    yield return CalculateMonumentSize(position, rotation, monument.Bounds, string.IsNullOrEmpty(name) ? objname : name, objname);
                }
            }
            _cmcCompleted = true;
            _cmc = null;
        }

        private void RemoveNearBuildingBlocks(List<BaseEntity> ents)
        {
            List<Vector3> blocks = Pool.Get<List<Vector3>>();

            foreach (var ent in ents)
            {
                BuildingBlock block = ent as BuildingBlock;
                if (block != null && !block.IsDestroyed)
                {
                    blocks.Add(block.transform.position);
                }
            }

            for (int i = ents.Count - 1; i >= 0; i--)
            {
                BaseEntity entity = ents[i];

                if (entity.IsKilled())
                {
                    ents.RemoveAt(i);
                    continue;
                }

                bool shouldRemove = false;

                foreach (var blockPos in blocks)
                {
                    if (entity.Distance(blockPos) <= 3f)
                    {
                        shouldRemove = true;
                        break;
                    }
                }

                if (shouldRemove)
                {
                    ents.RemoveAt(i);
                    continue;
                }

                foreach (var other in ents)
                {
                    if (other.IsKilled() || other == entity || other is BaseChair)
                    {
                        continue;
                    }

                    if (entity.Distance(other.transform.position) <= 3f)
                    {
                        shouldRemove = true;
                        break;
                    }
                }

                if (shouldRemove)
                {
                    ents.RemoveAt(i);
                }
            }

            blocks.ResetToPool();
        }

        private IEnumerator SetupOutpost(PrefabInfo mi)
        {
            var outpost = GetSettings("outpost");

            if (outpost == null)
            {
                outpostEnabled = false;
                yield break;
            }

            var radius = mi.extents.Max();

            if (config.Settings.AutoGenOutpost && outpost.Location != Vector3.zero && outpost.Locations.Exists(a => OutOfRange(mi.position, a, radius, config.Settings.AutoGenOutpost)))
            {
#if DEBUG
                Puts("Invalid Outpost location detected");
#endif
                outpost.Location = Vector3.zero;
                outpost.Locations = new List<Vector3>();
            }
            if (config.Settings.AutoGenOutpost && outpost.Location == Vector3.zero)
            {
#if DEBUG
                Puts("  Looking for Outpost target");
#endif
                bool changedOutpost = false;
                var ents = Pool.Get<List<BaseEntity>>();
                Vis.Entities(mi.position, radius, ents);
                RemoveNearBuildingBlocks(ents);
                foreach (BaseEntity entity in ents)
                {
                    if (entity.IsKilled() || config.Settings.BlockedPrefabs.Contains(entity.ShortPrefabName) || config.Settings.BlockedPrefabs.Contains(entity.GetType().Name))
                    {
                        continue;
                    }
                    if (entity.OwnerID.IsSteamId() || OutOfRange(mi.position, entity.transform.position, radius, entity is BaseChair))
                    {
                        continue;
                    }
                    if (entity.prefabID == 3858860623 || entity.prefabID == 1729604075 || entity is NPCMissionProvider || entity is Workbench)
                    {
                        var position = entity.transform.position + entity.transform.forward + new Vector3(0f, 1f, 0f);
                        if (position.y < TerrainMeta.HeightMap.GetHeight(position))
                        {
                            continue;
                        }
                        outpost.Location = position;
                        if (!outpost.Locations.Contains(position)) outpost.Locations.Add(position);
                        changedOutpost = true;
#if DEBUG
                        Puts("  Adding Outpost target {0}", position);
#endif
                    }
                    else if (entity is BaseChair)
                    {
                        var position = (entity.transform.position + new Vector3(0f, 0.1f)) + entity.transform.right + new Vector3(0f, 1f, 0f);
                        if (position.y < TerrainMeta.HeightMap.GetHeight(position))
                        {
                            continue;
                        }
                        outpost.Location = position;
                        if (!outpost.Locations.Contains(position)) outpost.Locations.Add(position);
                        changedOutpost = true;
#if DEBUG
                        Puts("  Adding Outpost target {0}", position);
#endif
                    }
                }
                if (changedOutpost) SaveConfig();
                ents.ResetToPool();
            }

            if (outpost.Location == Vector3.zero)
            {
                outpostEnabled = false;
            }
            else if (!outpost.Locations.Contains(outpost.Location))
            {
                outpost.Locations.Add(outpost.Location);
            }

            yield return null;
        }

        private IEnumerator SetupOutpost(MonumentInfo monument)
        {
            var outpost = GetSettings("outpost");

            if (outpost == null)
            {
                outpostEnabled = false;
                yield break;
            }

            if (config.Settings.AutoGenOutpost && outpost.Location != Vector3.zero && outpost.Locations.Exists(a => OutOfRange(monument, a, config.Settings.AutoGenOutpost)))
            {
#if DEBUG
                Puts("Invalid Outpost location detected");
#endif
                outpost.Location = Vector3.zero;
                outpost.Locations = new List<Vector3>();
            }
            if (config.Settings.AutoGenOutpost && outpost.Location == Vector3.zero)
            {
#if DEBUG
                Puts("  Looking for Outpost target");
#endif
                bool changedOutpost = false;
                var ents = Pool.Get<List<BaseEntity>>();
                float radius = monument.Bounds.size.Max();
                Vis.Entities(monument.transform.position, radius, ents);
                RemoveNearBuildingBlocks(ents);
                foreach (BaseEntity entity in ents)
                {
                    if (entity.IsKilled() || config.Settings.BlockedPrefabs.Contains(entity.ShortPrefabName) || config.Settings.BlockedPrefabs.Contains(entity.GetType().Name))
                    {
                        continue;
                    }
                    if (entity.OwnerID.IsSteamId() || OutOfRange(monument, entity.transform.position, entity is BaseChair))
                    {
                        continue;
                    }
                    if (entity.prefabID == 3858860623 || entity.prefabID == 1729604075 || entity is NPCMissionProvider || entity is Workbench)
                    {
                        var position = entity.transform.position + entity.transform.forward + new Vector3(0f, 1f, 0f);
                        if (position.y < TerrainMeta.HeightMap.GetHeight(position))
                        {
                            continue;
                        }
                        outpost.Location = position;
                        if (!outpost.Locations.Contains(position)) outpost.Locations.Add(position);
                        changedOutpost = true;
#if DEBUG
                        Puts("  Adding Outpost target {0}", position);
#endif
                    }
                    else if (entity is BaseChair)
                    {
                        var position = (entity.transform.position + new Vector3(0f, 0.1f)) + entity.transform.right + new Vector3(0f, 1f, 0f);
                        if (position.y < TerrainMeta.HeightMap.GetHeight(position))
                        {
                            continue;
                        }
                        outpost.Location = position;
                        if (!outpost.Locations.Contains(position)) outpost.Locations.Add(position);
                        changedOutpost = true;
#if DEBUG
                        Puts("  Adding Outpost target {0}", position);
#endif
                    }
                }
                if (changedOutpost) SaveConfig();
                ents.ResetToPool();
            }

            if (outpost.Location == Vector3.zero)
            {
                outpostEnabled = false;
            }
            else if (!outpost.Locations.Contains(outpost.Location))
            {
                outpost.Locations.Add(outpost.Location);
            }

            yield return null;
        }

        private IEnumerator SetupBandit(PrefabInfo mi)
        {
            var bandit = GetSettings("bandit");

            if (bandit == null)
            {
                banditEnabled = false;
                yield break;
            }

            float radius = mi.extents.Max();

            if (config.Settings.AutoGenBandit && bandit.Location != Vector3.zero && bandit.Locations.Exists(a => OutOfRange(mi.position, a, radius, config.Settings.AutoGenBandit)))
            {
#if DEBUG
                Puts("Invalid Bandit location detected");
#endif
                bandit.Location = Vector3.zero;
                bandit.Locations = new List<Vector3>();
            }
            if (config.Settings.AutoGenBandit && bandit.Location == Vector3.zero)
            {
#if DEBUG
                Puts("  Looking for BanditTown target");
#endif
                bool changedBandit = false;
                var ents = Pool.Get<List<BaseEntity>>();
                Vis.Entities(mi.position, radius, ents);
                RemoveNearBuildingBlocks(ents);
                foreach (BaseEntity entity in ents)
                {
                    if (entity.IsKilled() || entity.OwnerID.IsSteamId() || OutOfRange(mi.position, entity.transform.position, radius, entity is BaseChair))
                    {
                        continue;
                    }
                    if (entity.prefabID == 3858860623 || entity.prefabID == 1729604075 || entity is NPCMissionProvider || entity is Workbench)
                    {
                        var position = entity.transform.position + entity.transform.forward + new Vector3(0f, 1f, 0f);
                        if (position.y < TerrainMeta.HeightMap.GetHeight(position))
                        {
                            continue;
                        }
                        bandit.Location = position;
                        if (!bandit.Locations.Contains(position)) bandit.Locations.Add(position);
                        changedBandit = true;

#if DEBUG
                        Puts("  Adding BanditTown target {0}", position);
#endif
                    }
                    else if (entity is BaseChair)
                    {
                        var position = (entity.transform.position + new Vector3(0f, 0.1f)) + entity.transform.forward + new Vector3(0f, 1f, 0f);
                        if (position.y < TerrainMeta.HeightMap.GetHeight(position))
                        {
                            continue;
                        }
                        bandit.Location = position;
                        if (!bandit.Locations.Contains(position)) bandit.Locations.Add(position);
                        changedBandit = true;
#if DEBUG
                        Puts("  Adding BanditTown target {0}", position);
#endif
                    }
                }
                ents.ResetToPool();
                if (changedBandit) SaveConfig();
            }

            if (bandit.Location == Vector3.zero)
            {
                banditEnabled = false;
            }
            else if (!bandit.Locations.Contains(bandit.Location))
            {
                bandit.Locations.Add(bandit.Location);
            }

            yield return null;
        }

        private IEnumerator SetupBandit(MonumentInfo monument)
        {
            var bandit = GetSettings("bandit");

            if (bandit == null)
            {
                banditEnabled = false;
                yield break;
            }

            float radius = monument.Bounds.size.Max();

            if (config.Settings.AutoGenBandit && bandit.Location != Vector3.zero && bandit.Locations.Exists(a => OutOfRange(monument, a, config.Settings.AutoGenBandit)))
            {
#if DEBUG
                Puts("Invalid Bandit location detected");
#endif
                bandit.Location = Vector3.zero;
                bandit.Locations = new List<Vector3>();
            }
            if (config.Settings.AutoGenBandit && bandit.Location == Vector3.zero)
            {
#if DEBUG
                Puts("  Looking for BanditTown target");
#endif
                bool changedBandit = false;
                var ents = Pool.Get<List<BaseEntity>>();
                Vis.Entities(monument.transform.position, radius, ents);
                RemoveNearBuildingBlocks(ents);
                foreach (BaseEntity entity in ents)
                {
                    if (entity.IsKilled() || entity.OwnerID.IsSteamId() || OutOfRange(monument, entity.transform.position, entity is BaseChair))
                    {
                        continue;
                    }
                    if (entity.prefabID == 3858860623 || entity.prefabID == 1729604075 || entity is NPCMissionProvider || entity is Workbench)
                    {
                        var position = entity.transform.position + entity.transform.forward + new Vector3(0f, 1f, 0f);
                        if (position.y < TerrainMeta.HeightMap.GetHeight(position))
                        {
                            continue;
                        }
                        bandit.Location = position;
                        if (!bandit.Locations.Contains(position)) bandit.Locations.Add(position);
                        changedBandit = true;

#if DEBUG
                        Puts("  Adding BanditTown target {0}", position);
#endif
                    }
                    else if (entity is BaseChair)
                    {
                        var position = (entity.transform.position + new Vector3(0f, 0.1f)) + entity.transform.forward + new Vector3(0f, 1f, 0f);
                        if (position.y < TerrainMeta.HeightMap.GetHeight(position))
                        {
                            continue;
                        }
                        bandit.Location = position;
                        if (!bandit.Locations.Contains(position)) bandit.Locations.Add(position);
                        changedBandit = true;
#if DEBUG
                        Puts("  Adding BanditTown target {0}", position);
#endif
                    }
                }
                ents.ResetToPool();
                if (changedBandit) SaveConfig();
            }

            if (bandit.Location == Vector3.zero)
            {
                banditEnabled = false;
            }
            else if (!bandit.Locations.Contains(bandit.Location))
            {
                bandit.Locations.Add(bandit.Location);
            }

            yield return null;
        }

        private T GetColliderFrom<T>(T expected, Vector3 a, string text) where T : Collider
        {
            List<T> colliders = Pool.Get<List<T>>();
            Vis.Colliders<T>(a, 5f, colliders, Layers.Mask.Prevent_Building, QueryTriggerInteraction.Collide);
            if (expected != null && colliders.Contains(expected))
            {
                colliders.ResetToPool();
                return expected;
            }
            T collider = null;
            float dist = float.MaxValue;
            foreach (var col in colliders)
            {
                float distance = Vector3.Distance(a, col.bounds.center);
                if (collider == null || distance < dist)
                {
                    dist = distance;
                    collider = col;
                }
            }
            colliders.ResetToPool();
            return collider;
        }

        public IEnumerator CalculateMonumentSize(Vector3 from, Quaternion rot, Bounds b, string text, string prefab)
        {
            Collider colliderFrom = GetColliderFrom<Collider>(null, from, text);
            bool sphere = colliderFrom is SphereCollider;
            int checks = 0;
            float x = 0f;
            float z = 0f;
            float radius = b == default || b.extents.Max() <= 1f ? 15f : Mathf.Min(b.extents.x, b.extents.z);
            if (sphere && GetCustomMapPrefabRadius(from.XZ2D(), out var xzr))
            {
                x = z = radius = xzr;
                goto exit;
            }
            bool hasTopology = false;
            List<Vector3> positions = new();
            Collider colliderTo = null;
            if (text == "Substation")
            {
                x = 25f;
                z = 25f;
                goto exit;
            }
            while (radius < World.Size / 2f)
            {
                int pointsOfInterest = 0;
                foreach (var to in GetCardinalPositions(from, rot, radius))
                {
                    if (colliderFrom != null)
                    {
                        colliderTo = GetColliderFrom<Collider>(colliderFrom, to, text);

                        if (colliderTo != null && colliderTo == colliderFrom)
                        {
                            positions.Add(to);
                            hasTopology = true;
                            pointsOfInterest = 4;
                        }
                    }
                    else if (ContainsTopology(TerrainTopology.Enum.Building | TerrainTopology.Enum.Monument, to, 5f))
                    {
                        positions.Add(to);
                        pointsOfInterest++;
                        hasTopology = true;
                    }
                    if (++checks >= 25)
                    {
                        yield return CoroutineEx.waitForSeconds(0.0025f);
                        checks = 0;
                    }
                }
                if (pointsOfInterest < 4)
                {
                    break;
                }
                radius += 5f;
            }
            CalculateFurthestDistances(text, positions, from, rot, out x, out z);
            if (!hasTopology && !sphere)
            {
                x = z = 75f;
            }
        exit:
            float y = Mathf.Min(100f, x, z);
            if (sphere)
            {
                x *= 0.75f;
                y *= 0.75f;
                z *= 0.75f;
            }
            if (text == "Launch Site")
            {
                x -= x * 0.20f;
                z -= z * 0.30f;
            }
            if (text == "Airfield")
            {
                x -= x * 0.25f;
                z -= z * 0.30f;
            }
            if (text == "Power Plant")
            {
                x -= x * 0.25f;
                z -= z * 0.25f;
            }
            if (x <= 0 && y <= 0 && z <= 0)
            {
                x = y = z = radius;
            }
            var mi = new PrefabInfo(from, rot, new Vector3(x, y, z), config.Admin.ExtraMonumentDistance, text, prefab, sphere);
            monuments.Add(mi);
#if DEBUG
            Puts($"Adding Monument: {text}, pos: {from}, size: {x} {y} {z}, radius: {radius}, bounds: {b.extents.Max()}");
#endif
            if (config.Settings.Outpost.Exists(text.Contains))
            {
                yield return SetupOutpost(mi);
            }
            else if (config.Settings.Bandit.Exists(text.Contains))
            {
                yield return SetupBandit(mi);
            }
        }

        public static List<Vector3> GetCardinalPositions(Vector3 center, Quaternion rotation, float radius)
        {
            List<Vector3> positions = new()
            {
                rotation * new Vector3(0f, 0f, radius) + center,
                rotation * new Vector3(0f, 0f, -radius) + center,
                rotation * new Vector3(radius, 0f, 0f) + center,
                rotation * new Vector3(-radius, 0f, 0f) + center
            };

            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 a = positions[i];

                float y = TerrainMeta.HeightMap.GetHeight(a);

                positions[i] = a.WithY(y);
            }

            return positions;
        }

        public void CalculateFurthestDistances(string text, List<Vector3> positions, Vector3 center, Quaternion rot, out float x, out float z)
        {
            float north = 0f, south = 0f, east = 0f, west = 0f;
            foreach (var position in positions)
            {
                Vector3 localPosition = Quaternion.Inverse(rot) * (position - center);
                north = (localPosition.z > 0) ? Mathf.Max(north, Mathf.Abs(localPosition.z)) : north;
                south = (localPosition.z < 0) ? Mathf.Max(south, Mathf.Abs(localPosition.z)) : south;
                east = (localPosition.x > 0) ? Mathf.Max(east, Mathf.Abs(localPosition.x)) : east;
                west = (localPosition.x < 0) ? Mathf.Max(west, Mathf.Abs(localPosition.x)) : west;
            }
            x = Mathf.Min(east, west);
            x -= Mathf.Max(5f, x * 0.05f);
            z = Mathf.Min(north, south);
            z -= Mathf.Max(5f, z * 0.05f);
        }

        private static void DrawText(BasePlayer player, float duration, Color color, Vector3 from, object text) => player?.SendConsoleCommand("ddraw.text", duration, color, from, $"<size=24>{text}</size>");
        private static void DrawLine(BasePlayer player, float duration, Color color, Vector3 from, Vector3 to) => player?.SendConsoleCommand("ddraw.line", duration, color, from, to);
        private static void DrawSphere(BasePlayer player, float duration, Color color, Vector3 from, float radius) => player?.SendConsoleCommand("ddraw.sphere", duration, color, from, radius);

        private bool TeleportInForcedBoundary(params BasePlayer[] players)
        {
            if (config.Settings.ForcedBoundary != 0f)
            {
                foreach (var player in players)
                {
                    if (!CanBypassRestrictions(player.UserIDString) && player.transform.localPosition.y >= config.Settings.ForcedBoundary)
                    {
                        PrintMsgL(player, "TPFlagZone");
                        return false;
                    }
                }
            }
            return true;
        }

        private void TeleportRequestUI(BasePlayer player, string displayName)
        {
            if (!config.TPR.UI || string.IsNullOrEmpty(displayName)) return;
            var elements = new CuiElementContainer();
            elements.Add(new CuiPanel { CursorEnabled = false, Image = { Color = "0 0 0 0.75" }, RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-154.835 87.648", OffsetMax = "135.234 155.152" } }, "Overlay", "TPR_MAIN_UI", "TPR_MAIN_UI");
            elements.Add(new CuiElement { Name = "TPR_INFO_LBL", Parent = "TPR_MAIN_UI", DestroyUi = "TPR_INFO_LBL", Components = { new CuiTextComponent { Text = _("RequestUI", player, displayName), Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperCenter }, new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-142.335 -3.676", OffsetMax = "142.335 30.076" } } });
            elements.Add(new CuiButton { Button = { Command = "ntp.accept", Color = "0 0.78 0 0.75" }, Text = { Text = _("RequestAccept", player), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }, RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-138.395 -28.883", OffsetMax = "-28.406 -8.589" } }, "TPR_MAIN_UI", "TPR_ACCEPT_BTN", "TPR_ACCEPT_BTN");
            elements.Add(new CuiButton { Button = { Command = "ntp.reject", Color = "0.78 0 0 0.75" }, Text = { Text = _("RequestReject", player), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }, RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "29.305 -28.883", OffsetMax = "139.295 -8.589" } }, "TPR_MAIN_UI", "TPR_REJECT_BTN", "TPR_REJECT_BTN");
            timer.Once(config.TPR.RequestDuration, () => DestroyTeleportRequestCUI(player));
            CuiHelper.AddUi(player, elements);
        }

        public void DestroyTeleportRequestCUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "TPR_MAIN_UI");
        }

        [ConsoleCommand("ntp.accept")]
        private void ccmdAccept(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            DestroyTeleportRequestCUI(player);
            CommandTeleportAccept(player.IPlayer, "tpa", new string[0]);
        }

        [ConsoleCommand("ntp.reject")]
        private void ccmdReject(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            DestroyTeleportRequestCUI(player);
            CommandTeleportCancel(player.IPlayer, "tpc", new string[0]);
        }

        private bool OutOfRange(MonumentInfo m, Vector3 a, bool checkHeight) => checkHeight && Mathf.Abs(a.y - TerrainMeta.HeightMap.GetHeight(a)) > 5f || m.Distance(a) > m.Bounds.extents.Max() * 0.5f;

        private bool OutOfRange(Vector3 m, Vector3 a, float r, bool checkHeight) => checkHeight && Mathf.Abs(a.y - TerrainMeta.HeightMap.GetHeight(a)) > 5f || (a - m).magnitude > r * 0.5f;

        private void CommandToggle(IPlayer user, string command, string[] args)
        {
            if (!user.IsAdmin) return;

            if (args.Length == 0)
            {
                user.Reply("tnt commandname");
                return;
            }

            string arg = args[0].ToLower();
            command = command.ToLower();

            if (arg == command) return;

            if (!DisabledCommandData.DisabledCommands.Contains(arg))
                DisabledCommandData.DisabledCommands.Add(arg);
            else DisabledCommandData.DisabledCommands.Remove(arg);

            dataDisabled.WriteObject(DisabledCommandData);
            user.Reply(string.Format("{0} {1}", DisabledCommandData.DisabledCommands.Contains(arg) ? "Disabled:" : "Enabled:", arg));
        }

        private void CommandTeleport(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!user.IsServer && (!IsAllowedMsg(player, PermTp) || !TeleportInForcedBoundary(player))) return;
            BasePlayer target;
            float x, y, z;
            switch (args.Length)
            {
                case 1:
                    if (player == null) return;
                    target = FindPlayersSingle(args[0], player);
                    if (target == null) return;
                    if (target == player)
                    {
#if DEBUG
                        Puts("Debug mode - allowing self teleport.");
#else
                        PrintMsgL(player, "CantTeleportToSelf");
                        return;
#endif
                    }
                    Teleport(player, target);
                    PrintMsgL(player, "AdminTP", target.displayName);
                    Puts(_("LogTeleport", null, player.displayName, target.displayName));
                    if (config.Admin.AnnounceTeleportToTarget)
                        PrintMsgL(target, "AdminTPTarget", player.displayName);
                    break;
                case 2:
                    var origin = FindPlayersSingle(args[0], player);
                    target = FindPlayersSingle(args[1], player);
                    if (target == null && origin != null)
                    {
                        var loc = GetAdminLocation(args[1]);
                        if (loc != Vector3.zero)
                        {
                            Teleport(origin, loc, "", target.userID, town: false, allowTPB: true, build: true, craft: true, cave: true);
                            return;
                        }
                    }
                    if (origin == null || target == null) return;
                    if (target == origin)
                    {
                        PrintMsgL(player, "CantTeleportPlayerToSelf");
                        return;
                    }
                    if (permission.UserHasPermission(target.UserIDString, PermDisallowTpToMe))
                    {
                        PrintMsgL(player, "CantTeleportPlayerToYourself");
                        return;
                    }
                    Teleport(origin, target);
                    Puts(_("LogTeleportPlayer", null, user.Name, origin.displayName, target.displayName));
                    if (player == null) return;
                    PrintMsgL(player, "AdminTPPlayers", origin.displayName, target.displayName);
                    PrintMsgL(origin, "AdminTPPlayer", player.displayName, target.displayName);
                    if (config.Admin.AnnounceTeleportToTarget)
                        PrintMsgL(target, "AdminTPPlayerTarget", player.displayName, origin.displayName);
                    break;
                case 3:
                    if (player == null) return;
                    if (!float.TryParse(args[0].Replace(",", string.Empty), out x) || !float.TryParse(args[1].Replace(",", string.Empty), out y) || !float.TryParse(args[2], out z))
                    {
                        PrintMsgL(player, "InvalidCoordinates");
                        return;
                    }
                    if (config.Settings.CheckBoundaries && !CheckBoundaries(x, y, z))
                    {
                        PrintMsgL(player, "AdminTPOutOfBounds");
                        PrintMsgL(player, "AdminTPBoundaries", boundary);
                        return;
                    }
                    Teleport(player, x, y, z);
                    PrintMsgL(player, "AdminTPCoordinates", player.transform.position);
                    Puts(_("LogTeleport", null, player.displayName, player.transform.position));
                    break;
                case 4:
                    target = FindPlayersSingle(args[0], player);
                    if (target == null) return;
                    if (player != null && permission.UserHasPermission(target.UserIDString, PermDisallowTpToMe) && target != player)
                    {
                        PrintMsgL(player, "CantTeleportPlayerToYourself");
                        return;
                    }
                    if (!float.TryParse(args[1].Replace(",", string.Empty), out x) || !float.TryParse(args[2].Replace(",", string.Empty), out y) || !float.TryParse(args[3], out z))
                    {
                        PrintMsgL(player, "InvalidCoordinates");
                        return;
                    }
                    if (config.Settings.CheckBoundaries && !CheckBoundaries(x, y, z))
                    {
                        PrintMsgL(player, "AdminTPOutOfBounds");
                        PrintMsgL(player, "AdminTPBoundaries", boundary);
                        return;
                    }
                    Teleport(target, x, y, z);
                    if (player == null) return;
                    if (player == target)
                    {
                        PrintMsgL(player, "AdminTPCoordinates", player.transform.position);
                        Puts(_("LogTeleport", null, player.displayName, player.transform.position));
                    }
                    else
                    {
                        PrintMsgL(player, "AdminTPTargetCoordinates", target.displayName, player.transform.position);
                        if (config.Admin.AnnounceTeleportToTarget)
                            PrintMsgL(target, "AdminTPTargetCoordinatesTarget", player.displayName, player.transform.position);
                        Puts(_("LogTeleportPlayer", null, player.displayName, target.displayName, player.transform.position));
                    }
                    break;
                default:
                    PrintMsgL(player, "SyntaxCommandTP");
                    break;
            }
        }

        private Vector3 GetAdminLocation(string value)
        {
            foreach (var adminData in _Admin.Values)
            {
                if (adminData.Locations.TryGetValue(value, out var loc))
                {
                    return loc;
                }
            }
            return Vector3.zero;
        }

        private void CommandTeleportNear(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, PermTpN)) return;
            switch (args.Length)
            {
                case 1:
                case 2:
                    var target = FindPlayersSingle(args[0], player);
                    if (target == null) return;
                    if (target == player)
                    {
#if DEBUG
                        Puts("Debug mode - allowing self teleport.");
#else
                        PrintMsgL(player, "CantTeleportToSelf");
                        return;
#endif
                    }
                    int distance = 0;
                    if (args.Length != 2 || !int.TryParse(args[1], out distance))
                        distance = config.Admin.TeleportNearDefaultDistance;
                    float x = UnityEngine.Random.Range(-distance, distance);
                    var z = (float)Math.Sqrt(Math.Pow(distance, 2) - Math.Pow(x, 2));
                    var destination = target.transform.position;
                    destination.x -= x;
                    destination.z -= z;
                    Teleport(player, GetGroundBuilding(destination), "", target.userID, town: false, allowTPB: true, build: true, craft: true, cave: true);
                    PrintMsgL(player, "AdminTP", target.displayName);
                    Puts(_("LogTeleport", null, player.displayName, target.displayName));
                    if (config.Admin.AnnounceTeleportToTarget)
                        PrintMsgL(target, "AdminTPTarget", player.displayName);
                    break;
                default:
                    PrintMsgL(player, "SyntaxCommandTPN");
                    break;
            }
        }

        private void CommandTeleportLocation(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, PermTpL)) return;
            AdminData adminData;
            if (!_Admin.TryGetValue(player.userID, out adminData) || adminData.Locations.Count <= 0)
            {
                PrintMsgL(player, "AdminLocationListEmpty");
                return;
            }
            switch (args.Length)
            {
                case 0:
                    PrintMsgL(player, "AdminLocationList");
                    foreach (var location in adminData.Locations)
                        PrintMsgL(player, $"{location.Key} {location.Value}");
                    break;
                case 1:
                    if (!adminData.Locations.TryGetValue(args[0], out var loc))
                    {
                        PrintMsgL(player, "LocationNotFound");
                        return;
                    }
                    Teleport(player, loc, args[0], 0uL, town: false, allowTPB: true, build: true, craft: true, cave: true);
                    PrintMsgL(player, "AdminTPLocation", args[0]);
                    break;
                default:
                    PrintMsgL(player, "SyntaxCommandTPL");
                    break;
            }
        }

        private void CommandSaveTeleportLocation(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, PermTpSave)) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandTPSave");
                return;
            }
            if (!_Admin.TryGetValue(player.userID, out var adminData))
                _Admin[player.userID] = adminData = new AdminData();
            if (adminData.Locations.TryGetValue(args[0], out var location))
            {
                PrintMsgL(player, "LocationExists", location);
                return;
            }
            var positionCoordinates = player.transform.position;
            if (!CanBypassRestrictions(player.UserIDString) && !permission.UserHasPermission(player.UserIDString, "nteleportation.locationradiusbypass"))
            {
                foreach (var loc in adminData.Locations)
                {
                    if ((positionCoordinates - loc.Value).magnitude < config.Admin.LocationRadius)
                    {
                        PrintMsgL(player, "LocationExistsNearby", loc.Key);
                        return;
                    }
                }
            }
            adminData.Locations[args[0]] = positionCoordinates;
            PrintMsgL(player, "AdminTPLocationSave");
            changedAdmin = true;
        }

        private void CommandRemoveTeleportLocation(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, PermTpRemove)) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandTPRemove");
                return;
            }
            if (!_Admin.TryGetValue(player.userID, out var adminData) || adminData.Locations.Count <= 0)
            {
                PrintMsgL(player, "AdminLocationListEmpty");
                return;
            }
            if (adminData.Locations.Remove(args[0]))
            {
                PrintMsgL(player, "AdminTPLocationRemove", args[0]);
                changedAdmin = true;
                return;
            }
            PrintMsgL(player, "LocationNotFound");
        }

        private void CommandTeleportBack(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, PermTpB)) return;
            if (args.Length != 0)
            {
                PrintMsgL(player, "SyntaxCommandTPB");
                return;
            }
            if (!_Admin.TryGetValue(player.userID, out var adminData) || adminData.PreviousLocation == Vector3.zero)
            {
                PrintMsgL(player, "NoPreviousLocationSaved");
                return;
            }
            if (TeleportTimers.ContainsKey(player.userID))
            {
                PrintMsgL(player, "TeleportPendingTPC");
                return;
            }
            if (!TeleportInForcedBoundary(player))
            {
                return;
            }
            if (!CanBypassRestrictions(player.UserIDString))
            {
                var err = CanPlayerTeleport(player, adminData.PreviousLocation, player.transform.position);
                if (err != null)
                {
                    SendReply(player, err);
                    return;
                }
                if (!string.IsNullOrEmpty(adminData.Home))
                {
                    err = CanPlayerTeleportHome(player, adminData.PreviousLocation);
                    if (err != null)
                    {
                        SendReply(player, err);
                        return;
                    }
                }
                err = CheckPlayer(player, adminData.BuildingBlocked, adminData.AllowCrafting, true, "tpb", adminData.AllowCave);
                if (err != null)
                {
                    PrintMsgL(player, err);
                    return;
                }
            }
            var countdown = GetLower(player, config.Settings.TPB.Countdowns, config.Settings.TPB.Countdown);
            if (countdown > 0f)
            {
                TeleportBack(player, adminData, countdown);
                return;
            }
            Teleport(player, adminData.PreviousLocation, adminData.Home, adminData.UserID, adminData.Town, allowTPB: false, build: adminData.BuildingBlocked, craft: adminData.AllowCrafting, cave: adminData.AllowCave);
            adminData.PreviousLocation = Vector3.zero;
            changedAdmin = true;
            PrintMsgL(player, "AdminTPBack");
            Puts(_("LogTeleportBack", null, player.displayName));
        }

        private void TeleportBack(BasePlayer player, AdminData adminData, int countdown)
        {
            string err = null;
            var location = adminData.PreviousLocation;
            TeleportTimers[player.userID] = new TeleportTimer
            {
                UserID = adminData.UserID,
                Home = adminData.Home,
                OriginPlayer = player,

                Timer = timer.Once(countdown, () =>
                {
#if DEBUG
                    Puts("Calling CheckPlayer from cmdChatHomeTP");
#endif
                    if (!CanBypassRestrictions(player.UserIDString))
                    {
                        err = CheckPlayer(player, config.Home.UsableOutOfBuildingBlocked, CanCraftHome(player), true, "home", CanCaveHome(player));
                        if (err != null)
                        {
                            PrintMsgL(player, "Interrupted");
                            PrintMsgL(player, err);
                            TeleportTimers.Remove(player.userID);
                            return;
                        }
                        err = CanPlayerTeleport(player, location, player.transform.position);
                        if (err != null)
                        {
                            PrintMsgL(player, "Interrupted");
                            PrintMsgL(player, err);
                            TeleportTimers.Remove(player.userID);
                            return;
                        }
                        if (!string.IsNullOrEmpty(adminData.Home))
                        {
                            err = CanPlayerTeleportHome(player, location);
                            if (err != null)
                            {
                                PrintMsgL(player, "Interrupted");
                                SendReply(player, err);
                                TeleportTimers.Remove(player.userID);
                                return;
                            }
                        }
                        err = CheckItems(player);
                        if (err != null)
                        {
                            PrintMsgL(player, "Interrupted");
                            PrintMsgL(player, "TPBlockedItem", err);
                            TeleportTimers.Remove(player.userID);
                            return;
                        }
                        err = IsInsideEntity(location, player.userID, "tpb");
                        if (err != null)
                        {
                            PrintMsgL(player, "Interrupted");
                            PrintMsgL(player, err);
                            TeleportTimers.Remove(player.userID);
                            return;
                        }
                        if (!TeleportInForcedBoundary(player))
                        {
                            return;
                        }
                    }
                    Teleport(player, location, adminData.Home, adminData.UserID, adminData.Town, false, adminData.BuildingBlocked, adminData.AllowCrafting, adminData.AllowCave);
                    adminData.PreviousLocation = Vector3.zero;
                    changedAdmin = true;
                    PrintMsgL(player, "AdminTPBack");
                    Puts(_("LogTeleportBack", null, player.displayName));
                    TeleportTimers.Remove(player.userID);
                })
            };
            if (countdown > 0)
            {
                PrintMsgL(player, "DM_TownTPStarted", location, countdown);
                Interface.CallHook("OnTeleportBackAccepted", player, location, countdown);
            }
        }

        private void CommandSetHome(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, PermHome)) return;
            if (!config.Settings.HomesEnabled) { user.Reply("Homes are not enabled in the config."); return; }
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandSetHome");
                return;
            }
            string err = null;
            if (!_Home.TryGetValue(player.userID, out var homeData))
                _Home[player.userID] = homeData = new HomeData();
            var limit = GetHigher(player, config.Home.VIPHomesLimits, config.Home.HomesLimit, true);
            if (!args[0].Replace("_", "").All(char.IsLetterOrDigit))
            {
                PrintMsgL(player, "InvalidCharacter");
                return;
            }
            if (homeData.TryGetValue(args[0], out var homeEntry))
            {
                PrintMsgL(player, "HomeExists", homeEntry.Get());
                return;
            }
            var position = player.transform.position;
            if (!CanBypassRestrictions(player.UserIDString))
            {
                var getUseableTime = GetUseableTime(player, config.Home.Hours);
                if (getUseableTime > 0.0)
                {
                    PrintMsgL(player, "NotUseable", FormatTime(player, getUseableTime));
                    return;
                }
                err = CheckPlayer(player, false, CanCraftHome(player), true, "sethome", CanCaveHome(player));
                if (err != null)
                {
                    PrintMsgL(player, err);
                    return;
                }
                if (!player.CanBuild())
                {
                    PrintMsgL(player, "HomeTPBuildingBlocked");
                    return;
                }
                if (limit > 0 && homeData.Locations.Count >= limit)
                {
                    PrintMsgL(player, "HomeMaxLocations", limit);
                    return;
                }
                if (config.Home.LocationRadius > 0 && !permission.UserHasPermission(player.UserIDString, "nteleportation.locationradiusbypass"))
                {
                    foreach (var loc in homeData.Locations)
                    {
                        if ((position - loc.Value.Get()).magnitude < config.Home.LocationRadius)
                        {
                            PrintMsgL(player, "HomeExistsNearby", loc.Key);
                            return;
                        }
                    }
                }
                err = CanPlayerTeleport(player, position);
                if (err != null)
                {
                    SendReply(player, err);
                    return;
                }
                err = CheckFoundation(player.userID, position, "sethome");
                if (err != null)
                {
                    PrintMsgL(player, err);
                    return;
                }
            }
            if (player.IsAdmin && config.Settings.DrawHomeSphere) DrawSphere(player, 30f, Color.blue, position, 2.5f);
            var entity = player.GetParentEntity() as Tugboat;
            if (entity != null)
            {
                if (!config.Home.AllowTugboats && !permission.UserHasPermission(player.UserIDString, "nteleportation.tugboatssethomebypass") && !CanBypassRestrictions(player.UserIDString))
                {
                    PrintMsgL(player, "HomeTugboatNotAllowed");
                    return;
                }
                homeData.Set(args[0], new HomeData.Entry
                {
                    Position = position - entity.transform.position,
                    wasEntity = true,
                    Entity = entity
                });
            }
            else homeData.Set(args[0], new HomeData.Entry(position));
            changedHome = true;
            PrintMsgL(player, "HomeSave");
            PrintMsgL(player, "HomeQuota", homeData.Locations.Count, limit);
            Interface.CallHook("OnHomeAdded", player, position, args[0]);
        }

        private void CommandRemoveHome(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            if (!config.Settings.HomesEnabled) { user.Reply("Homes are not enabled in the config."); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, PermHome)) return;
            if (player.IsAdmin && args.Length == 2 && args[0] == "all")
            {
                float radius;
                if (float.TryParse(args[1], out radius))
                {
                    int amount = 0;
                    foreach (var home in _Home.ToList())
                    {
                        foreach (var location in home.Value.Locations.ToList())
                        {
                            var position = location.Value.Get();
                            if (Vector3Ex.Distance2D(position, player.transform.position) < radius)
                            {
                                string username = covalence.Players.FindPlayerById(home.Key.ToString())?.Name ?? "N/A";
                                Puts("{0} ({1}) removed home from {2} ({3}) at {4}", player.displayName, player.userID, username, home.Key, position);
                                DrawText(player, 30f, Color.red, position, "X");
                                home.Value.Remove(location.Key);
                                amount++;
                            }
                        }
                    }

                    user.Reply($"Removed {amount} homes within {radius} meters");
                }
                else user.Reply("/removehome all <radius>");

                return;
            }
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandRemoveHome");
                return;
            }
            if (!_Home.TryGetValue(player.userID, out var homeData) || homeData.Locations.Count <= 0)
            {
                PrintMsgL(player, "HomeListEmpty");
                return;
            }
            if (homeData.TryGetValue(args[0], out var homeEntry))
            {
                Interface.CallHook("OnHomeRemoved", player, homeEntry.Get(), args[0]);
                homeData.Remove(args[0]);
                changedHome = true;
                PrintMsgL(player, "HomeRemove", args[0]);
            }
            else PrintMsgL(player, "HomeNotFound");
        }

        private void CommandHome(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            if (!config.Settings.HomesEnabled) { user.Reply("Homes are not enabled in the config."); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, PermHome)) return;
            if (args.Length == 0)
            {
                PrintMsgL(player, "SyntaxCommandHome");
                if (IsAllowed(player)) PrintMsgL(player, "SyntaxCommandHomeAdmin");
                return;
            }
            switch (args[0].ToLower())
            {
                case "add":
                    CommandSetHome(user, command, args.Skip(1));
                    break;
                case "list":
                    CommandListHomes(user, command, args.Skip(1));
                    break;
                case "remove":
                    CommandRemoveHome(user, command, args.Skip(1));
                    break;
                case "radius":
                    CommandHomeRadius(user, command, args.Skip(1));
                    break;
                case "delete":
                    CommandHomeDelete(user, command, args.Skip(1));
                    break;
                case "tp":
                    CommandHomeAdminTP(user, command, args.Skip(1));
                    break;
                case "homes":
                    CommandHomeHomes(user, command, args.Skip(1));
                    break;
                case "wipe":
                    CommandWipeHomes(user, command, args.Skip(1));
                    break;
                default:
                    cmdChatHomeTP(player, command, args);
                    break;
            }
        }

        private void CommandHomeRadius(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, PermRadiusHome)) return;
            float radius;
            if (args.Length != 1 || !float.TryParse(args[0], out radius)) radius = 10;
            var found = false;
            foreach (var homeData in _Home)
            {
                var toRemove = new List<string>();
                var target = RustCore.FindPlayerById(homeData.Key)?.displayName ?? homeData.Key.ToString();
                foreach (var location in homeData.Value.Locations)
                {
                    var position = location.Value.Get();
                    if ((player.transform.position - position).magnitude <= radius)
                    {
                        string err = null;
                        if (!location.Value.isEntity)
                        {
                            err = location.Value.wasEntity ? "HomeRemovedDestroyed" : CheckFoundation(homeData.Key, position, "radius");
                        }
                        if (err != null)
                        {
                            SendHomeError(player, toRemove, err, location.Key, position, err == "HomeRemovedDestroyed");
                            found = true;
                            continue;
                        }
                        if (player.IsAdmin)
                        {
                            var entity = GetFoundationOwned(position, homeData.Key);
                            if (entity == null)
                            {
                                DrawText(player, 30f, Color.blue, position, $"{target} - {location.Key} {position}");
                            }
                            else
                            {
                                DrawText(player, 30f, Color.blue, entity.CenterPoint() + new Vector3(0, .5f), $"{target} - {location.Key} {position}");
                                DrawMonument(player, entity.CenterPoint(), entity.bounds.extents, entity.transform.rotation, Color.blue, 30f);
                            }
                        }
                        PrintMsg(player, $"{target} - {location.Key} {position}");
                        found = true;
                    }
                }
                foreach (var key in toRemove)
                {
                    homeData.Value.Remove(key);
                    changedHome = true;
                }
            }
            if (!found)
                PrintMsgL(player, "HomeNoFound");
        }

        private void SendHomeError(BasePlayer player, List<string> toRemove, string err, string homeName, Vector3 position, bool wasEntity, bool send = true)
        {
            Interface.CallHook("OnHomeRemoved", player, position, homeName);
            if (toRemove != null)
            {
                toRemove.Add(homeName);
            }
            if (!send)
            {
                return;
            }
            if (!wasEntity)
            {
                PrintMsgL(player, "HomeRemovedInvalid", $"{homeName} {position} ({MapHelper.PositionToString(position)})");
                PrintMsgL(player, err);
            }
            else PrintMsgL(player, "HomeRemovedDestroyed", homeName);
        }

        private void CommandHomeDelete(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowed(player, PermDeleteHome)) return;
            if (args.Length != 2)
            {
                PrintMsgL(player, "SyntaxCommandHomeDelete");
                return;
            }
            var userId = FindPlayersSingleId(args[0], player);
            if (userId <= 0) return;
            if (!_Home.TryGetValue(userId, out var targetHome) || !targetHome.Remove(args[1]))
            {
                PrintMsgL(player, "HomeNotFound");
                return;
            }
            changedHome = true;
            PrintMsgL(player, "HomeDelete", args[0], args[1]);
        }

        private void CommandHomeAdminTP(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, PermAdmin)) return;
            if (args.Length != 2)
            {
                PrintMsgL(player, "SyntaxCommandHomeAdminTP");
                return;
            }
            var userId = FindPlayersSingleId(args[0], player);
            if (userId <= 0) return;
            if (!_Home.TryGetValue(userId, out var targetHome) || !targetHome.TryGetValue(args[1], out var homeEntry))
            {
                PrintMsgL(player, "HomeNotFound");
                return;
            }
            Teleport(player, homeEntry.Get(), "", userId, town: false, allowTPB: true, build: true, craft: true, cave: true);
            PrintMsgL(player, "HomeAdminTP", args[0], args[1]);
        }

        // Check that plugins are available and enabled for CheckEconomy()
        private bool UseEconomy()
        {
            return (config.Settings.UseEconomics && (Economics != null || IQEconomic != null)) || (config.Settings.UseServerRewards && ServerRewards != null);
        }

        // Check balance on multiple plugins and optionally withdraw money from the player
        private bool CheckEconomy(BasePlayer player, double bypass, bool withdraw = false, bool deposit = false)
        {
            if (player == null)
            {
                return false;
            }
            if (CanBypassRestrictions(player.UserIDString)) return true;
            bool foundmoney = false;
            // Check Economics first.  If not in use or balance low, check ServerRewards below
            if (config.Settings.UseEconomics)
            {
                if (Economics != null)
                {
                    var balance = Convert.ToDouble(Economics?.CallHook("Balance", (ulong)player.userID));

                    if (balance >= bypass)
                    {
                        foundmoney = true;
                        if (withdraw)
                        {
                            return Convert.ToBoolean(Economics?.CallHook("Withdraw", (ulong)player.userID, bypass));
                        }
                        else if (deposit)
                        {
                            Economics?.CallHook("Deposit", (ulong)player.userID, bypass);
                        }
                    }
                }
                else if (IQEconomic != null)
                {
                    var balance = Convert.ToInt32(IQEconomic?.CallHook("API_GET_BALANCE", (ulong)player.userID));
                    if (balance >= bypass)
                    {
                        foundmoney = true;
                        if (withdraw)
                        {
                            return Convert.ToBoolean(IQEconomic?.CallHook("API_REMOVE_BALANCE", (ulong)player.userID, (int)bypass));
                        }
                        else if (deposit)
                        {
                            IQEconomic?.CallHook("API_SET_BALANCE", (ulong)player.userID, (int)bypass);
                        }
                    }
                }
            }

            // No money via Economics, or plugin not in use.  Try ServerRewards.
            if (!foundmoney && config.Settings.UseServerRewards && ServerRewards != null)
            {
                var balance = Convert.ToDouble(ServerRewards?.Call("CheckPoints", (ulong)player.userID));
                if (balance >= bypass)
                {
                    foundmoney = true;
                    if (withdraw)
                    {
                        return Convert.ToBoolean(ServerRewards?.Call("TakePoints", (ulong)player.userID, (int)bypass));
                    }
                    else if (deposit)
                    {
                        ServerRewards?.Call("AddPoints", (ulong)player.userID, (int)bypass);
                    }
                }
            }

            // Just checking balance without withdrawal - did we find anything?
            return foundmoney;
        }

        private void cmdChatHomeTP(BasePlayer player, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { player.ChatMessage("Disabled command."); return; }
            if (!IsAllowedMsg(player, PermHome)) return;
            bool paidmoney = false;
            if (!config.Settings.HomesEnabled) { player.ChatMessage("Homes are not enabled in the config."); return; }
            if (args.Length < 1)
            {
                PrintMsgL(player, "SyntaxCommandHome");
                return;
            }
            if (!_Home.TryGetValue(player.userID, out var homeData) || homeData.Locations.Count <= 0)
            {
                PrintMsgL(player, "HomeListEmpty");
                return;
            }
            if (!homeData.TryGetValue(args[0], out var homeEntry))
            {
                PrintMsgL(player, "HomeNotFound");
                return;
            }
            int limit = 0;
            string err = null;
            var position = homeEntry.Get();
            var timestamp = Facepunch.Math.Epoch.Current;
            if (!CanBypassRestrictions(player.UserIDString))
            {
                if (!TeleportInForcedBoundary(player))
                {
                    return;
                }
                float globalCooldownTime = GetGlobalCooldown(player);
                if (globalCooldownTime > 0f)
                {
                    PrintMsgL(player, "WaitGlobalCooldown", FormatTime(player, (int)globalCooldownTime));
                    return;
                }
                if (config.Settings.BlockAuthorizedTeleporting && player.IsBuildingAuthed())
                {
                    PrintMsgL(player, "CannotTeleportFromHome");
                    return;
                }
                err = CheckPlayer(player, config.Home.UsableOutOfBuildingBlocked, CanCraftHome(player), true, "home", CanCaveHome(player));
                if (err != null)
                {
                    PrintMsgL(player, err);
                    return;
                }
                if (config.Settings.BlockNoEscape && Convert.ToBoolean(NoEscape?.Call("IsBlockedZone", position)))
                {
                    PrintMsgL(player, "TPNoEscapeBlocked");
                    return;
                }
                if (config.Settings.RaidBlock && RaidBlock != null && Convert.ToBoolean(RaidBlock?.Call("IsBlocked", player)))
                {
                    PrintMsgL(player, "TPNoEscapeBlocked");
                    return;
                }
                if (!homeEntry.isEntity)
                {
                    err = homeEntry.wasEntity ? "HomeRemovedDestroyed" : CheckFoundation(player.userID, position, "home");
                }
                if (err == null)
                {
                    err = CheckTargetLocation(player, position, config.Home.UsableIntoBuildingBlocked, config.Home.CupOwnerAllowOnBuildingBlocked);
                }
                if (err != null)
                {
                    SendHomeError(player, null, err, args[0], position, err == "HomeRemovedDestroyed");
                    homeData.Remove(args[0]);
                    changedHome = true;
                    return;
                }
                if (config.Settings.Interrupt.Monument)
                {
                    var monname = NearMonument(position, false, "");
                    if (!string.IsNullOrEmpty(monname))
                    {
                        if (monname.Contains(":")) monname = monname.Substring(0, monname.IndexOf(":"));
                        PrintMsgL(player, "TooCloseToMon", _(monname, player));
                        return;
                    }
                }
                var cooldown = GetLower(player, config.Home.VIPCooldowns, config.Home.Cooldown);
                var remain = cooldown - (timestamp - homeData.Teleports.Timestamp);
                if (cooldown > 0 && timestamp - homeData.Teleports.Timestamp < cooldown)
                {
                    var cmdSent = args.Length >= 2 ? args[1].ToLower() : string.Empty;

                    if (!string.IsNullOrEmpty(config.Settings.BypassCMD) && !paidmoney)
                    {
                        if (cmdSent == config.Settings.BypassCMD.ToLower() && config.Home.Bypass > -1)
                        {
                            bool foundmoney = CheckEconomy(player, config.Home.Bypass);

                            if (foundmoney)
                            {
                                CheckEconomy(player, config.Home.Bypass, true);
                                paidmoney = true;

                                if (config.Home.Bypass > 0)
                                {
                                    PrintMsgL(player, "HomeTPCooldownBypass", config.Home.Bypass);
                                }

                                if (config.Home.Pay > 0)
                                {
                                    PrintMsgL(player, "PayToHome", config.Home.Pay);
                                }
                            }
                            else
                            {
                                PrintMsgL(player, "HomeTPCooldownBypassF", config.Home.Bypass);
                                return;
                            }
                        }
                        else if (UseEconomy())
                        {
                            if (config.Home.Bypass > 0)
                            {
                                PrintMsgL(player, "HomeTPCooldown", FormatTime(player, remain));
                                PrintMsgL(player, "HomeTPCooldownBypassP", config.Home.Bypass);
                                PrintMsgL(player, "HomeTPCooldownBypassP2", config.Settings.BypassCMD);
                                return;
                            }
                        }
                        else
                        {
                            PrintMsgL(player, "HomeTPCooldown", FormatTime(player, remain));
                            return;
                        }
                    }
                    else
                    {
                        PrintMsgL(player, "HomeTPCooldown", FormatTime(player, remain));
                        return;
                    }
                }
                var currentDate = DateTime.Now.ToString("d");
                if (homeData.Teleports.Date != currentDate)
                {
                    homeData.Teleports.Amount = 0;
                    homeData.Teleports.Date = currentDate;
                }
                limit = GetHigher(player, config.Home.VIPDailyLimits, config.Home.DailyLimit, true);
                if (limit > 0 && homeData.Teleports.Amount >= limit)
                {
                    PrintMsgL(player, "HomeTPLimitReached", limit);
                    return;
                }
                err = CanPlayerTeleport(player, position, player.transform.position);
                if (err != null)
                {
                    SendReply(player, err);
                    return;
                }
                err = CanPlayerTeleportHome(player, position);
                if (err != null)
                {
                    SendReply(player, err);
                    return;
                }
                err = CheckItems(player);
                if (err != null)
                {
                    PrintMsgL(player, "TPBlockedItem", err);
                    return;
                }
                if (config.Home.UsableFromSafeZoneOnly && !player.InSafeZone())
                {
                    PrintMsgL(player, "TPHomeSafeZoneOnly");
                    return;
                }
            }
            if (TeleportTimers.ContainsKey(player.userID))
            {
                PrintMsgL(player, "TeleportPendingTPC");
                return;
            }
            var countdown = GetLower(player, config.Home.VIPCountdowns, config.Home.Countdown);
            TeleportTimers[player.userID] = new TeleportTimer
            {
                Home = args[0],
                OriginPlayer = player,
                Timer = timer.Once(countdown, () =>
                {
#if DEBUG
                    Puts("Calling CheckPlayer from cmdChatHomeTP");
#endif
                    position = homeEntry.Get();
                    if (!CanBypassRestrictions(player.UserIDString))
                    {
                        if (!TeleportInForcedBoundary(player))
                        {
                            return;
                        }
                        err = CheckPlayer(player, config.Home.UsableOutOfBuildingBlocked, CanCraftHome(player), true, "home", CanCaveHome(player));
                        if (err != null)
                        {
                            PrintMsgL(player, "Interrupted");
                            PrintMsgL(player, err);
                            if (paidmoney)
                            {
                                paidmoney = false;
                                CheckEconomy(player, config.Home.Bypass, false, true);
                            }
                            TeleportTimers.Remove(player.userID);
                            return;
                        }
                        err = CanPlayerTeleport(player, position, player.transform.position);
                        if (err != null)
                        {
                            PrintMsgL(player, "Interrupted");
                            PrintMsgL(player, err);
                            if (paidmoney)
                            {
                                paidmoney = false;
                                CheckEconomy(player, config.Home.Bypass, false, true);
                            }
                            TeleportTimers.Remove(player.userID);
                            return;
                        }
                        err = CheckItems(player);
                        if (err != null)
                        {
                            PrintMsgL(player, "Interrupted");
                            PrintMsgL(player, "TPBlockedItem", err);
                            if (paidmoney)
                            {
                                paidmoney = false;
                                CheckEconomy(player, config.Home.Bypass, false, true);
                            }
                            TeleportTimers.Remove(player.userID);
                            return;
                        }
                        if (!homeEntry.isEntity)
                        {
                            err = homeEntry.wasEntity ? "HomeRemovedDestroyed" : CheckFoundation(player.userID, position, "home");
                        }
                        if (err == null)
                        {
                            err = CheckTargetLocation(player, position, config.Home.UsableIntoBuildingBlocked, config.Home.CupOwnerAllowOnBuildingBlocked);
                        }
                        if (err != null)
                        {
                            SendHomeError(player, null, err, args[0], position, err == "HomeRemovedDestroyed");
                            homeData.Remove(args[0]);
                            changedHome = true;
                            if (paidmoney)
                            {
                                paidmoney = false;
                                CheckEconomy(player, config.Home.Bypass, false, true);
                            }
                            return;
                        }
                        if (UseEconomy())
                        {
                            if (config.Home.Pay < 0)
                            {
                                TeleportTimers.Remove(player.userID);
                                PrintMsgL(player, "DM_TownTPDisabled", "/home");
                                return;
                            }
                            else if (config.Home.Pay > 0)
                            {
                                if (!CheckEconomy(player, config.Home.Pay))
                                {
                                    TeleportTimers.Remove(player.userID);
                                    PrintMsgL(player, "TPNoMoney", config.Home.Pay);
                                    return;
                                }

                                if (!paidmoney)
                                {
                                    PrintMsgL(player, "TPMoney", (double)config.Home.Pay);
                                }

                                paidmoney = CheckEconomy(player, config.Home.Pay, true);
                            }
                        }
                    }
                    Teleport(player, position, args[0], 0uL, town: false, allowTPB: config.Home.AllowTPB, build: config.Home.UsableOutOfBuildingBlocked, craft: CanCraftHome(player), cave: CanCaveHome(player));
                    homeData.Teleports.Amount++;
                    homeData.Teleports.Timestamp = timestamp;
                    changedHome = true;
                    PrintMsgL(player, "HomeTP", args[0]);
                    if (limit > 0) PrintMsgL(player, "HomeTPAmount", limit - homeData.Teleports.Amount);
                    TeleportTimers.Remove(player.userID);
                })
            };

            if (countdown > 0)
            {
                PrintMsgL(player, "HomeTPStarted", args[0], countdown);
                Interface.CallHook("OnHomeAccepted", player, args[0], countdown);
            }
        }

        private void CommandListHomes(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!config.Settings.HomesEnabled) { user.Reply("Homes are not enabled in the config."); return; }
            if (!IsAllowedMsg(player, PermHome)) return;
            if (args.Length != 0)
            {
                PrintMsgL(player, "SyntaxCommandListHomes");
                return;
            }
            if (!_Home.TryGetValue(player.userID, out var homeData) || homeData.Locations.Count <= 0)
            {
                PrintMsgL(player, "HomeListEmpty");
                return;
            }
            PrintMsgL(player, "HomeList");
            ValidateHomes(player, homeData, true, false);
            foreach (var location in homeData.Locations)
                PrintMsgL(player, $"{location.Key} {location.Value.Get()} {MapHelper.PositionToString(location.Value.Get())}");
        }

        private void ValidateHomes(BasePlayer player, HomeData homeData, bool showRemoved, bool showLoc)
        {
            if (config.Home.CheckValidOnList)
            {
                string err = null;
                var toRemove = new List<string>();
                foreach (var location in homeData.Locations)
                {
                    var position = location.Value.Get();
                    if (!location.Value.isEntity)
                    {
                        err = location.Value.wasEntity ? "HomeRemovedDestroyed" : CheckFoundation(player.userID, position, "validate");
                    }
                    if (err != null)
                    {
                        SendHomeError(player, toRemove, err, location.Key, position, err == "HomeRemovedDestroyed", showRemoved);
                    }
                    else if (showLoc) PrintMsgL(player, $"{location.Key} {position} {MapHelper.PositionToString(position)}");
                }
                foreach (var key in toRemove)
                {
                    homeData.Remove(key);
                    changedHome = true;
                }
            }
        }

        private void CommandHomeHomes(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, PermHomeHomes)) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandHomeHomes");
                return;
            }
            var userId = FindPlayersSingleId(args[0], player);
            if (userId <= 0) return;
            HomeData homeData;
            if (!_Home.TryGetValue(userId, out homeData) || homeData.Locations.Count <= 0)
            {
                PrintMsgL(player, "HomeListEmpty");
                return;
            }
            PrintMsgL(player, "HomeList");
            var toRemove = new List<string>();
            foreach (var location in homeData.Locations)
            {
                var position = location.Value.Get();
                string err = null;
                if (!location.Value.isEntity)
                {
                    err = location.Value.wasEntity ? "HomeRemovedDestroyed" : CheckFoundation(userId, position, "homes");
                }
                if (err != null)
                {
                    SendHomeError(player, toRemove, err, location.Key, position, err == "HomeRemovedDestroyed");
                }
                else PrintMsgL(player, $"{location.Key} {position} ({MapHelper.PositionToString(position)})");
            }
            foreach (var key in toRemove)
            {
                homeData.Remove(key);
                changedHome = true;
            }
        }

        private void CommandTeleportAcceptToggle(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (player == null || !IsAllowedMsg(player, PermTpT)) { return; }
            if (Array.Exists(args, arg => arg == "friend" || arg == "clan" || arg == "team" || arg == "all"))
            {
                ToggleTPTEnabled(player, command, args);
            }
            string clan = IsEnabled(player.UserIDString, "clan") ? config.TPT.EnabledColor : config.TPT.DisabledColor;
            string team = IsEnabled(player.UserIDString, "team") ? config.TPT.EnabledColor : config.TPT.DisabledColor;
            string friend = IsEnabled(player.UserIDString, "friend") ? config.TPT.EnabledColor : config.TPT.DisabledColor;
            PrintMsgL(player, "TPTInfo", command, clan, team, friend, command.ToUpper(), config.TPT.EnabledColor, config.TPT.DisabledColor);
        }

        public bool IsOnSameTeam(ulong playerId, ulong targetId)
        {
            return RelationshipManager.ServerInstance.playerToTeam.TryGetValue(playerId, out var team) && team.members.Contains(targetId);
        }

        private bool AreFriends(string playerId, string targetId)
        {
            return Friends != null && Convert.ToBoolean(Friends?.Call("AreFriends", playerId, targetId));
        }

        private bool IsFriend(string playerId, string targetId)
        {
            return Friends != null && Convert.ToBoolean(Friends?.Call("IsFriend", playerId, targetId));
        }

        private bool IsInSameClan(string playerId, string targetId)
        {
            return Clans != null && Convert.ToBoolean(Clans?.Call("IsMemberOrAlly", playerId, targetId));
        }

        private bool InstantTeleportAccept(BasePlayer target, BasePlayer player)
        {
            if (!permission.UserHasPermission(target.UserIDString, PermTpT) || !permission.UserHasPermission(player.UserIDString, PermTpT))
            {
                return false;
            }

            if ((config.TPT.UseClans && IsInSameClan(player.UserIDString, target.UserIDString) && !TPT.ContainsKey(target.UserIDString))
                || (config.TPT.UseClans && IsEnabled(target.UserIDString, "clan") && IsInSameClan(player.UserIDString, target.UserIDString)))
            {
                CommandTeleportAccept(target.IPlayer, TPA, nullArg);
            }
            else if ((config.TPT.UseFriends && IsFriend(player.UserIDString, target.UserIDString) && !TPT.ContainsKey(target.UserIDString))
                     || (config.TPT.UseFriends && IsEnabled(target.UserIDString, "friend") && IsFriend(player.UserIDString, target.UserIDString)))
            {
                CommandTeleportAccept(target.IPlayer, TPA, nullArg);
            }
            else if ((config.TPT.UseTeams && IsOnSameTeam(player.userID, target.userID) && !TPT.ContainsKey(target.UserIDString))
                     || (config.TPT.UseTeams && IsEnabled(target.UserIDString, "team") && IsOnSameTeam(player.userID, target.userID)))
            {
                CommandTeleportAccept(target.IPlayer, TPA, nullArg);
            }

            return true;
        }

        private bool IsEnabled(string targetId, string value)
        {
            return TPT.TryGetValue(targetId, out var list) && list.Contains(value);
        }

        private void ToggleTPTEnabled(BasePlayer target, string command, string[] args)
        {
            if (args.Contains("all"))
            {
                args = new string[] { "friend", "clan", "team" };
            }
            if (!TPT.TryGetValue(target.UserIDString, out var list))
            {
                TPT[target.UserIDString] = list = new();
            }
            foreach (var arg in args)
            {
                if (!list.Remove(arg))
                {
                    list.Add(arg);
                }
            }
            if (list.IsEmpty())
            {
                TPT.Remove(target.UserIDString);
            }
            changedTPT = true;
        }

        private string GetMultiplePlayers(List<BasePlayer> players)
        {
            return string.Join(", ", players.Select(player => string.Format("<color={0}>{1}</color> - {2}", config.Settings.ChatCommandArgumentColor, GetPlayerCode(player), player.displayName)));
        }

        private double GetUseableTime(BasePlayer player, double hours) => hours <= 0.0 || permission.UserHasPermission(player.UserIDString, "nteleportation.skipwipewaittime") ? 0.0 : TimeSpan.FromHours(hours - DateTime.UtcNow.Subtract(SaveRestore.SaveCreatedTime).TotalHours).TotalSeconds;

        private void CommandTeleportRequest(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, PermTpR)) return;
            if (!config.Settings.TPREnabled) { user.Reply("TPR is not enabled in the config."); return; }
            if (args.Length == 0)
            {
                PrintMsgL(player, "SyntaxCommandTPR");
                return;
            }
            var targets = FindPlayers(args[0]);
            if (targets.Count <= 0)
            {
                PrintMsgL(player, "PlayerNotFound");
                return;
            }
            BasePlayer target = null;
            if (args.Length >= 2)
            {
                if (targets.Count > 1)
                {
                    PrintMsgL(player, "MultiplePlayers", GetMultiplePlayers(targets));
                    return;
                }
                else target = targets[0];
            }
            else
            {
                if (targets.Count > 1)
                {
                    PrintMsgL(player, "MultiplePlayers", GetMultiplePlayers(targets));
                    return;
                }

                target = targets[0];
            }

            if (target == player)
            {
#if DEBUG
                Puts("Debug mode - allowing self teleport.");
#else
                PrintMsgL(player, "CantTeleportToSelf");
                return;
#endif
            }
#if DEBUG
            Puts("Calling CheckPlayer from cmdChatTeleportRequest");
#endif
            if (!TeleportInForcedBoundary(player, target))
            {
                return;
            }

            if (IsBlockedUser(player.userID, target.userID))
            {
                PrintMsgL(player, "BlockedTeleportTarget", target.displayName.Sanitize());
                return;
            }
            if (!_TPR.TryGetValue(player.userID, out var tprData))
                _TPR[player.userID] = tprData = new();
            if (!CanBypassRestrictions(player.UserIDString))
            {
                var getUseableTime = GetUseableTime(player, config.TPR.Hours);
                if (getUseableTime > 0.0)
                {
                    PrintMsgL(player, "NotUseable", FormatTime(player, getUseableTime));
                    return;
                }
                string err = null;
                float globalCooldownTime = GetGlobalCooldown(player);
                if (globalCooldownTime > 0f)
                {
                    PrintMsgL(player, "WaitGlobalCooldown", FormatTime(player, (int)globalCooldownTime));
                    return;
                }
                if (config.Settings.BlockAuthorizedTeleporting && player.IsBuildingAuthed())
                {
                    PrintMsgL(player, "CannotTeleportFromHome");
                    return;
                }
                err = CheckPlayer(player, config.TPR.UsableOutOfBuildingBlocked, CanCraftTPR(player), true, "tpr", CanCaveTPR(player));
                if (err != null)
                {
                    PrintMsgL(player, err);
                    return;
                }
                var err2 = CheckPlayer(target, config.TPR.UsableIntoBuildingBlocked, CanCraftTPR(target), true, "tpr", CanCaveTPR(target));
                if (err2 != null)
                {
                    string error = string.Format(lang.GetMessage("ErrorTPR", this, player.UserIDString), target.displayName, lang.GetMessage(err2, this, player.UserIDString));
                    PrintMsg(player, error);
                    return;
                }
                err = CheckTargetLocation(target, target.transform.position, config.TPR.UsableIntoBuildingBlocked, config.TPR.CupOwnerAllowOnBuildingBlocked);
                if (err != null)
                {
                    PrintMsgL(player, err);
                    return;
                }
                if (config.TPR.BlockTPAOnCeiling)
                {
                    if (IsStandingOnEntity(target.transform.position, 20f, Layers.Mask.Construction, out var entity, new string[2] { "floor", "roof" }) && IsCeiling(entity as DecayEntity))
                    {
                        PrintMsgL(player, "TPRNoCeiling");
                        return;
                    }
                    if (IsBlockedOnIceberg(target.transform.position))
                    {
                        PrintMsgL(player, "HomeIce");
                        return;
                    }
                }
                var timestamp = Facepunch.Math.Epoch.Current;
                var currentDate = DateTime.Now.ToString("d");

                if (tprData.Date != currentDate)
                {
                    tprData.Amount = 0;
                    tprData.Date = currentDate;
                }

                var cooldown = GetLower(player, config.TPR.VIPCooldowns, config.TPR.Cooldown);
                if (cooldown > 0 && timestamp - tprData.Timestamp < cooldown)
                {
                    var cmdSent = args.Length >= 2 ? args[1].ToLower() : string.Empty;

                    if (!string.IsNullOrEmpty(config.Settings.BypassCMD))
                    {
                        if (cmdSent == config.Settings.BypassCMD.ToLower() && config.TPR.Bypass > -1)
                        {
                            if (CheckEconomy(player, config.TPR.Bypass))
                            {
                                CheckEconomy(player, config.TPR.Bypass, true);

                                if (config.TPR.Bypass > 0)
                                {
                                    PrintMsgL(player, "TPRCooldownBypass", config.TPR.Bypass);
                                }

                                if (config.TPR.Pay > 0)
                                {
                                    PrintMsgL(player, "PayToTPR", config.TPR.Pay);
                                }
                            }
                            else
                            {
                                PrintMsgL(player, "TPRCooldownBypassF", config.TPR.Bypass);
                                return;
                            }
                        }
                        else if (UseEconomy())
                        {
                            var remain = cooldown - (timestamp - tprData.Timestamp);
                            PrintMsgL(player, "TPRCooldown", FormatTime(player, remain));
                            if (config.TPR.Bypass > -1)
                            {
                                if (config.TPR.Bypass > 0)
                                {
                                    PrintMsgL(player, "TPRCooldownBypassP", config.TPR.Bypass);

                                    if (config.TPR.Pay > 0)
                                    {
                                        PrintMsgL(player, "PayToTPR", config.TPR.Pay);
                                    }

                                    PrintMsgL(player, "TPRCooldownBypassP2a", config.Settings.BypassCMD);
                                    return;
                                }
                            }
                            else return;
                        }
                        else
                        {
                            var remain = cooldown - (timestamp - tprData.Timestamp);
                            PrintMsgL(player, "TPRCooldown", FormatTime(player, remain));
                            return;
                        }
                    }
                    else
                    {
                        var remain = cooldown - (timestamp - tprData.Timestamp);
                        PrintMsgL(player, "TPRCooldown", FormatTime(player, remain));
                        return;
                    }
                }
                var limit = GetHigher(player, config.TPR.VIPDailyLimits, config.TPR.DailyLimit, true);
                if (limit > 0 && tprData.Amount >= limit)
                {
                    PrintMsgL(player, "TPRLimitReached", limit);
                    return;
                }
                err = CanPlayerTeleport(player, player.transform.position, target.transform.position);
                if (err != null)
                {
                    SendReply(player, err);
                    return;
                }
                err = CanPlayerTeleport(target, target.transform.position, player.transform.position);
                if (err != null)
                {
                    PrintMsgL(player, string.IsNullOrEmpty(err) ? "TPRTarget" : err);
                    return;
                }
                err = CheckItems(player);
                if (err != null)
                {
                    PrintMsgL(player, "TPBlockedItem", err);
                    return;
                }
            }
            if (TeleportTimers.ContainsKey(player.userID))
            {
                PrintMsgL(player, "TeleportPendingTPC");
                return;
            }
            if (TeleportTimers.ContainsKey(target.userID))
            {
                PrintMsgL(player, "TeleportPendingTarget");
                return;
            }
            if (PlayersRequests.ContainsKey(player.userID))
            {
                PrintMsgL(player, "PendingRequest");
                return;
            }
            if (PlayersRequests.ContainsKey(target.userID))
            {
                PrintMsgL(player, "PendingRequestTarget");
                return;
            }

            if (!config.TPR.UseClans_Friends_Teams || IsInSameClan(player.UserIDString, target.UserIDString) || AreFriends(player.UserIDString, target.UserIDString) || IsOnSameTeam(player.userID, target.userID) || CanBypassRestrictions(player.UserIDString))
            {
                PlayersRequests[player.userID] = target;
                PlayersRequests[target.userID] = player;
                PendingRequests[target.userID] = timer.Once(config.TPR.RequestDuration, () => { RequestTimedOut(player, target); });
                PrintMsgL(player, "Request", target.displayName);
                PrintMsgL(target, "RequestTarget", player.displayName);
                if (config.TPR.PlaySoundsToRequestTarget)
                {
                    SendEffect(target, config.TPR.TeleportRequestEffects);
                }
                if (Interface.CallHook("OnTeleportRequested", target, player) == null && !InstantTeleportAccept(target, player))
                {
                    TeleportRequestUI(target, player.displayName);
                }
            }
            else
            {
                PrintMsgL(player, "TPR_NoClan_NoFriend_NoTeam");
            }
        }

        private void CommandTeleportAccept(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            if (!config.Settings.TPREnabled) { user.Reply("TPR is not enabled in the config."); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, config.TPR.RequireTPAPermission ? PermTpA : PermTpR)) return;
            DestroyTeleportRequestCUI(player);
            if (args.Length != 0)
            {
                PrintMsgL(player, "SyntaxCommandTPA");
                return;
            }
            if (!PendingRequests.TryGetValue(player.userID, out var reqTimer))
            {
                PrintMsgL(player, "NoPendingRequest");
                DestroyTeleportRequestCUI(player);
                return;
            }
#if DEBUG
            Puts("Calling CheckPlayer from cmdChatTeleportAccept");
#endif
            string err = null;
            var originPlayer = PlayersRequests[player.userID];
            if (originPlayer == null)
            {
                PrintMsgL(player, "NoPendingRequest");
                return;
            }
            if (!CanBypassRestrictions(player.UserIDString))
            {
                if (!TeleportInForcedBoundary(originPlayer, player))
                {
                    return;
                }
                err = CheckPlayer(player, config.TPR.UsableIntoBuildingBlocked, CanCraftTPR(player), false, "tpa", CanCaveTPR(player));
                if (err != null)
                {
                    PrintMsgL(player, err);
                    return;
                }
                err = CheckPlayer(originPlayer, config.TPR.UsableOutOfBuildingBlocked, CanCraftTPR(originPlayer), true, "tpa", CanCaveTPR(originPlayer));
                if (err != null)
                {
                    PrintMsgL(originPlayer, err);
                    return;
                }
                err = CheckTargetLocation(originPlayer, player.transform.position, config.TPR.UsableIntoBuildingBlocked, config.TPR.CupOwnerAllowOnBuildingBlocked);
                if (err != null)
                {
                    PrintMsgL(player, err);
                    return;
                }
                err = CanPlayerTeleport(player, originPlayer.transform.position, player.transform.position);
                if (err != null)
                {
                    SendReply(player, err);
                    return;
                }
                if (config.TPR.BlockTPAOnCeiling)
                {
                    if (IsStandingOnEntity(player.transform.position, 20f, Layers.Mask.Construction, out var entity, new string[2] { "floor", "roof" }) && IsCeiling(entity as DecayEntity))
                    {
                        PrintMsgL(player, "TPRNoCeiling");
                        return;
                    }
                    if (IsBlockedOnIceberg(player.transform.position))
                    {
                        PrintMsgL(player, "HomeIce");
                        return;
                    }
                }
                float globalCooldownTime = GetGlobalCooldown(player);
                if (globalCooldownTime > 0f)
                {
                    PrintMsgL(player, "WaitGlobalCooldown", FormatTime(player, (int)globalCooldownTime));
                    return;
                }
                if (config.Settings.BlockAuthorizedTeleporting && player.IsBuildingAuthed())
                {
                    PrintMsgL(player, "CannotTeleportFromHome");
                    return;
                }
            }
            var countdown = GetLower(originPlayer, config.TPR.VIPCountdowns, config.TPR.Countdown);
            PrintMsgL(originPlayer, "Accept", player.displayName, countdown);
            PrintMsgL(player, "AcceptTarget", originPlayer.displayName);
            Interface.CallHook("OnTeleportAccepted", player, originPlayer, countdown);
            if (config.TPR.PlaySoundsWhenTargetAccepts)
            {
                SendEffect(originPlayer, config.TPR.TeleportAcceptEffects);
            }
            var timestamp = Facepunch.Math.Epoch.Current;
            TeleportTimers[originPlayer.userID] = new TeleportTimer
            {
                OriginPlayer = originPlayer,
                TargetPlayer = player,
                Timer = timer.Once(countdown, () =>
                {
#if DEBUG
                    Puts("Calling CheckPlayer from cmdChatTeleportAccept timer loop");
#endif
                    if (!CanBypassRestrictions(player.UserIDString))
                    {
                        if (!TeleportInForcedBoundary(originPlayer, player))
                        {
                            return;
                        }
                        if (config.Settings.BlockAuthorizedTeleporting && player.IsBuildingAuthed())
                        {
                            PrintMsgL(player, "CannotTeleportFromHome");
                            return;
                        }
                        err = CheckPlayer(originPlayer, config.TPR.UsableOutOfBuildingBlocked, CanCraftTPR(originPlayer), true, "tpa", CanCaveTPR(originPlayer)) ?? CheckPlayer(player, false, CanCraftTPR(player), true, "tpa", CanCaveTPR(player));
                        if (err != null)
                        {
                            PrintMsgL(player, "InterruptedTarget", originPlayer.displayName);
                            PrintMsgL(originPlayer, "Interrupted");
                            PrintMsgL(originPlayer, err);
                            TeleportTimers.Remove(originPlayer.userID);
                            return;
                        }
                        err = CheckTargetLocation(originPlayer, player.transform.position, config.TPR.UsableIntoBuildingBlocked, config.TPR.CupOwnerAllowOnBuildingBlocked);
                        if (err != null)
                        {
                            PrintMsgL(player, err);
                            PrintMsgL(originPlayer, "Interrupted");
                            PrintMsgL(originPlayer, err);
                            TeleportTimers.Remove(originPlayer.userID);
                            return;
                        }
                        err = CanPlayerTeleport(originPlayer, player.transform.position, originPlayer.transform.position);
                        if (err != null)
                        {
                            SendReply(player, err);
                            PrintMsgL(originPlayer, "Interrupted");
                            SendReply(originPlayer, err);
                            TeleportTimers.Remove(originPlayer.userID);
                            return;
                        }
                        err = CheckItems(originPlayer);
                        if (err != null)
                        {
                            PrintMsgL(player, "InterruptedTarget", originPlayer.displayName);
                            PrintMsgL(originPlayer, "Interrupted");
                            PrintMsgL(originPlayer, "TPBlockedItem", err);
                            TeleportTimers.Remove(originPlayer.userID);
                            return;
                        }
                        if (UseEconomy())
                        {
                            if (config.TPR.Pay > -1)
                            {
                                if (!CheckEconomy(originPlayer, config.TPR.Pay))
                                {
                                    if (config.TPR.Pay > 0)
                                    {
                                        PrintMsgL(originPlayer, "TPNoMoney", config.TPR.Pay);
                                    }

                                    PrintMsgL(player, "InterruptedTarget", originPlayer.displayName);
                                    TeleportTimers.Remove(originPlayer.userID);
                                    return;
                                }
                                else
                                {
                                    CheckEconomy(originPlayer, config.TPR.Pay, true);

                                    if (config.TPR.Pay > 0)
                                    {
                                        PrintMsgL(originPlayer, "TPMoney", (double)config.TPR.Pay);
                                    }
                                }
                            }
                        }
                    }
                    SendDiscordMessage(originPlayer, player);
                    Teleport(originPlayer, player.transform.position, "", player.userID, town: false, allowTPB: config.TPR.AllowTPB, build: config.TPR.UsableOutOfBuildingBlocked, craft: CanCraftTPR(player), CanCaveTPR(player));
                    var tprData = _TPR[originPlayer.userID];
                    tprData.Amount++;
                    tprData.Timestamp = timestamp;
                    changedTPR = true;
                    PrintMsgL(player, "SuccessTarget", originPlayer.displayName);
                    PrintMsgL(originPlayer, "Success", player.displayName);
                    var limit = GetHigher(originPlayer, config.TPR.VIPDailyLimits, config.TPR.DailyLimit, true);
                    if (limit > 0) PrintMsgL(originPlayer, "TPRAmount", limit - tprData.Amount);
                    TeleportTimers.Remove(originPlayer.userID);
                    Interface.CallHook("OnTeleportRequestCompleted", player, originPlayer);
                })
            };
            reqTimer.Destroy();
            PendingRequests.Remove(player.userID);
            PlayersRequests.Remove(player.userID);
            PlayersRequests.Remove(originPlayer.userID);
        }

        private void CommandWipeHomes(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!IsAllowedMsg(player, PermWipeHomes)) return;
            if (_Home.Count > 0) Puts("{0} ({1}) wiped homes", player.displayName, player.userID);
            _Home.Clear();
            changedHome = true;
            PrintMsgL(player, "HomesListWiped");
        }

        private void CommandTeleportHelp(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (player == null || !player.IsConnected || player.IsSleeping()) return;
            if (!config.Settings.HomesEnabled && !config.Settings.TPREnabled && !IsAllowedMsg(player)) return;
            if (args.Length == 1)
            {
                var key = $"TPHelp{args[0].ToLower()}";
                var msg = _(key, player);
                if (key.Equals(msg))
                    PrintMsgL(player, "InvalidHelpModule");
                else
                    PrintMsg(player, msg);
            }
            else
            {
                var msg = _("TPHelpGeneral", player);
                if (IsAllowed(player))
                    msg += NewLine + "/tphelp AdminTP";
                if (config.Settings.HomesEnabled)
                    msg += NewLine + "/tphelp Home";
                if (config.Settings.TPREnabled)
                    msg += NewLine + "/tphelp TPR";
                PrintMsg(player, msg);
            }
        }

        private List<string> _tpid = new List<string> { "home", "bandit", "outpost", "tpr", "town" };

        private void CommandTeleportInfo(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (player == null || !player.IsConnected || player.IsSleeping() || !TeleportInForcedBoundary(player)) return;
            if (args.Length == 1)
            {
                var module = args[0].ToLower();
                var settings = GetSettings(module);
                var msg = _(_tpid.Contains(module) || settings == null ? $"TPSettings{module}" : "TPSettingsdynamic", player);
                var timestamp = Facepunch.Math.Epoch.Current;
                var currentDate = DateTime.Now.ToString("d");
                int limit;
                int cooldown;

                switch (module)
                {
                    case "home":
                        if (!IsAllowedMsg(player, PermHome)) return;
                        limit = GetHigher(player, config.Home.VIPDailyLimits, config.Home.DailyLimit, true);
                        cooldown = GetLower(player, config.Home.VIPCooldowns, config.Home.Cooldown);
                        int homeLimits = GetHigher(player, config.Home.VIPHomesLimits, config.Home.HomesLimit, true);
                        PrintMsg(player, string.Format(msg, FormatTime(player, cooldown), limit > 0 ? limit.ToString() : _("Unlimited", player), homeLimits));
                        HomeData homeData;
                        if (!_Home.TryGetValue(player.userID, out homeData))
                            _Home[player.userID] = homeData = new HomeData();
                        if (homeData.Teleports.Date != currentDate)
                        {
                            homeData.Teleports.Amount = 0;
                            homeData.Teleports.Date = currentDate;
                        }
                        if (limit > 0) PrintMsgL(player, "HomeTPAmount", limit - homeData.Teleports.Amount);
                        if (cooldown > 0 && timestamp - homeData.Teleports.Timestamp < cooldown)
                        {
                            var remain = cooldown - (timestamp - homeData.Teleports.Timestamp);
                            PrintMsgL(player, "HomeTPCooldown", FormatTime(player, remain));
                        }
                        break;
                    case "tpr":
                        if (!IsAllowedMsg(player, PermTpR)) return;
                        limit = GetHigher(player, config.TPR.VIPDailyLimits, config.TPR.DailyLimit, true);
                        cooldown = GetLower(player, config.TPR.VIPCooldowns, config.TPR.Cooldown);
                        PrintMsg(player, string.Format(msg, FormatTime(player, cooldown), limit > 0 ? limit.ToString() : _("Unlimited", player)));
                        TeleportData tprData;
                        if (!_TPR.TryGetValue(player.userID, out tprData))
                            _TPR[player.userID] = tprData = new TeleportData();
                        if (tprData.Date != currentDate)
                        {
                            tprData.Amount = 0;
                            tprData.Date = currentDate;
                        }
                        if (limit > 0) PrintMsgL(player, "TPRAmount", limit - tprData.Amount);
                        if (cooldown > 0 && timestamp - tprData.Timestamp < cooldown)
                        {
                            var remain = cooldown - (timestamp - tprData.Timestamp);
                            PrintMsgL(player, "TPRCooldown", FormatTime(player, remain));
                        }
                        break;
                    default: // town island outpost bandit etc
                        if (settings == null)
                        {
                            PrintMsgL(player, "InvalidHelpModule");
                            break;
                        }

                        limit = GetHigher(player, settings.VIPDailyLimits, settings.DailyLimit, true);
                        cooldown = GetLower(player, settings.VIPCooldowns, settings.Cooldown);
                        if (_tpid.Contains(module)) PrintMsg(player, string.Format(msg, FormatTime(player, cooldown), limit > 0 ? limit.ToString() : _("Unlimited", player)));
                        else PrintMsg(player, string.Format(msg, module.SentenceCase(), FormatTime(player, cooldown), limit > 0 ? limit.ToString() : _("Unlimited", player)));
                        TeleportData tpData;
                        if (!settings.Teleports.TPData.TryGetValue(player.userID, out tpData))
                            settings.Teleports.TPData[player.userID] = tpData = new TeleportData();
                        if (tpData.Date != currentDate)
                        {
                            tpData.Amount = 0;
                            tpData.Date = currentDate;
                        }
                        var language = lang.GetMessage(settings.Command, this, user.Id);
                        if (limit > 0) PrintMsgL(player, "DM_TownTPAmount", limit - tpData.Amount, language);
                        if (!string.IsNullOrEmpty(config.Settings.BypassCMD) && cooldown > 0 && timestamp - tpData.Timestamp < cooldown)
                        {
                            if (Interface.CallHook("OnTeleportCooldownNotify", player) != null)
                            {
                                break;
                            }

                            var remain = cooldown - (timestamp - tpData.Timestamp);
                            PrintMsgL(player, "DM_TownTPCooldown", FormatTime(player, remain));

                            if (settings.Bypass > 0)
                            {
                                PrintMsgL(player, "DM_TownTPCooldownBypassP", settings.Bypass);
                                PrintMsgL(player, "DM_TownTPCooldownBypassP2", language, config.Settings.BypassCMD);
                            }
                        }
                        break;
                }
            }
            else
            {
                var msg = _("TPInfoGeneral", player);
                if (config.Settings.HomesEnabled && IsAllowed(player, PermHome))
                    msg += NewLine + "/tpinfo Home";
                if (config.Settings.TPREnabled && IsAllowed(player, PermTpR))
                    msg += NewLine + "/tpinfo TPR";
                foreach (var entry in config.DynamicCommands)
                {
                    if (entry.Value.Enabled)
                    {
                        if (command == "bandit" && !banditEnabled) continue;
                        if (command == "outpost" && !outpostEnabled) continue;
                        if (!IsAllowed(player, $"{Name}.tp{entry.Key}")) continue;
                        msg += NewLine + $"/tpinfo {entry.Key}";
                    }
                }
                PrintMsgL(player, msg);
            }
        }

        private void CommandTeleportCancel(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            var player = user.Object as BasePlayer;
            if (player == null || !player.IsConnected || player.IsSleeping()) return;
            DestroyTeleportRequestCUI(player);
            if (TeleportTimers.TryGetValue(player.userID, out var teleportTimer))
            {
                teleportTimer.Timer?.Destroy();
                PrintMsgL(player, "TPCancelled");
                PrintMsgL(teleportTimer.TargetPlayer, "TPCancelledTarget", player.displayName);
                TeleportTimers.Remove(player.userID);
                Interface.CallHook("OnTeleportRejected", player, teleportTimer.TargetPlayer);
                return;
            }
            foreach (var keyValuePair in TeleportTimers)
            {
                if (keyValuePair.Value.TargetPlayer != player) continue;
                keyValuePair.Value.Timer?.Destroy();
                PrintMsgL(keyValuePair.Value.OriginPlayer, "TPCancelledTarget", player.displayName);
                PrintMsgL(player, "TPYouCancelledTarget", keyValuePair.Value.OriginPlayer.displayName);
                TeleportTimers.Remove(keyValuePair.Key);
                Interface.CallHook("OnTeleportRejected", player, keyValuePair.Value.OriginPlayer);
                return;
            }
            if (!PlayersRequests.TryGetValue(player.userID, out var target))
            {
                PrintMsgL(player, "NoPendingRequest");
                return;
            }
            if (PendingRequests.TryGetValue(player.userID, out var reqTimer))
            {
                reqTimer.Destroy();
                PendingRequests.Remove(player.userID);
            }
            else if (PendingRequests.TryGetValue(target.userID, out reqTimer))
            {
                reqTimer.Destroy();
                PendingRequests.Remove(target.userID);
                var temp = player;
                player = target;
                target = temp;
            }
            PlayersRequests.Remove(target.userID);
            PlayersRequests.Remove(player.userID);
            PrintMsgL(player, "Cancelled", target.displayName);
            PrintMsgL(target, "CancelledTarget", player.displayName);
            Interface.CallHook("OnTeleportRejected", player, target);
        }

        private void CommandDynamic(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (!user.HasPermission(PermAdmin) || args.Length != 2 || args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                CommandTeleportInfo(user, command, args.Skip(1));
                return;
            }

            var value = args[1].ToLower();

            if (args[0].Equals("add", StringComparison.OrdinalIgnoreCase))
            {
                TownSettings settings;
                if (GetSettings(value) == null)
                {
                    config.DynamicCommands.Add(value, settings = new TownSettings());
                    RegisterCommand(value, settings, true);
                    RegisterCommand(value, nameof(CommandCustom));
                    PrintMsgL(user, "DM_TownTPCreated", value);
                    SaveConfig();
                }
                else PrintMsgL(user, "DM_TownTPExists", value);
            }
            else if (args[0].Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                var key = config.DynamicCommands.Keys.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(key))
                {
                    PrintMsgL(user, "DM_TownTPRemoved", key);
                    config.DynamicCommands.Remove(key);
                    UnregisterCommand(value);
                    SaveConfig();
                }
                else PrintMsgL(user, "DM_TownTPDoesNotExist", value);
            }
            else CommandTeleportInfo(user, command, args);
        }

        private void CommandCustom(IPlayer user, string command, string[] args)
        {
            CommandTown(user, command, args);
        }

        private TownSettings GetSettings(string command, ulong userid = 0uL)
        {
            if (command.Equals("home", StringComparison.OrdinalIgnoreCase) && _Home.ContainsKey(userid))
            {
                return new TownSettings
                {
                    VIPCooldowns = config.Home.VIPCooldowns,
                    Cooldown = config.Home.Cooldown,
                    Countdown = config.Home.Countdown,
                    VIPDailyLimits = config.Home.VIPDailyLimits,
                    DailyLimit = config.Home.DailyLimit,
                    Teleports = new StoredData
                    {
                        TPData = new Dictionary<ulong, TeleportData>
                        {
                            [userid] = _Home[userid].Teleports
                        }
                    }
                };
            }

            if (command.Equals("tpr", StringComparison.OrdinalIgnoreCase) && _TPR.ContainsKey(userid))
            {
                return new TownSettings
                {
                    VIPCooldowns = config.TPR.VIPCooldowns,
                    Cooldown = config.TPR.Cooldown,
                    Countdown = config.TPR.Countdown,
                    VIPDailyLimits = config.TPR.VIPDailyLimits,
                    DailyLimit = config.TPR.DailyLimit,
                    Teleports = new StoredData
                    {
                        TPData = new Dictionary<ulong, TeleportData>
                        {
                            [userid] = _TPR[userid]
                        }
                    }
                };
            }

            foreach (var x in config.DynamicCommands)
            {
                if (x.Key.Equals(command, StringComparison.OrdinalIgnoreCase))
                {
                    return x.Value;
                }
            }

            return null;
        }

        private bool IsServerCommand(IPlayer user, string command, string[] args)
        {
            if (!user.IsServer)
            {
                return false;
            }
            var settings = GetSettings(command);
            if (settings == null)
            {
                user.Reply($"Command '{command}' not found in config.");
                return false;
            }
            if (args.Length == 0)
            {
                string positions = string.Join(", ", settings.Locations.ToArray());
                user.Reply($"{command} locations: {positions}");
                return true;
            }
            if (args[0] == "clear")
            {
                settings.Location = Vector3.zero;
                settings.Locations.Clear();
                user.Reply($"{command} locations have been cleared.");
            }
            else
            {
                try
                {
                    var vector = string.Join(" ", args).ToVector3();
                    if (vector == Vector3.zero)
                    {
                        throw new InvalidCastException("zero");
                    }
                    if (Vector3.Distance(vector, Vector3.zero) < 50f)
                    {
                        throw new InvalidCastException("distance");
                    }
                    if (vector.y < TerrainMeta.HeightMap.GetHeight(vector))
                    {
                        throw new InvalidCastException("height");
                    }
                    if (!settings.Locations.Contains(vector))
                    {
                        settings.Locations.Insert(0, vector);
                        user.Reply($"{command} location manually set to: " + vector);
                    }
                    else user.Reply($"{command} location was already set to: " + vector);
                    settings.Location = vector;
                }
                catch
                {
                    user.Reply($"Invalid position specified ({string.Join(" ", args)})");
                    return true;
                }
            }
            if (command == "bandit")
            {
                banditEnabled = settings.Locations.Count > 0;
            }
            if (command == "outpost")
            {
                outpostEnabled = settings.Locations.Count > 0;
            }
            SaveConfig();
            return true;
        }

        private void CommandTown(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            if (IsServerCommand(user, command, args)) return;
            var player = user.Object as BasePlayer;
#if DEBUG
            Puts($"cmdChatTown: command={command}");
#endif
            if (!IsAllowedMsg(player, $"{Name}.tp{command}".ToLower()) || !TeleportInForcedBoundary(player)) return;

            if (!CanBypassRestrictions(player.UserIDString))
            {
                float globalCooldownTime = GetGlobalCooldown(player);
                if (globalCooldownTime > 0f)
                {
                    PrintMsgL(player, "WaitGlobalCooldown", FormatTime(player, (int)globalCooldownTime));
                    return;
                }

                if (config.Settings.BlockAuthorizedTeleporting && player.IsBuildingAuthed())
                {
                    PrintMsgL(player, "CannotTeleportFromHome");
                    return;
                }
            }

            var settings = GetSettings(command);

            if (settings == null)
            {
                return;
            }

            var language = lang.GetMessage(settings.Command, this, user.Id);

            // For admin using set, add, clear or show command locations
            if (args.Length >= 1 && IsAllowed(player, PermAdmin))
            {
                var param = args[0].ToLower();

                if (param.Equals("clear"))
                {
                    settings.Location = Vector3.zero;
                    settings.Locations.Clear();
                    SaveConfig();
                    PrintMsgL(player, "DM_TownTPLocationsCleared", language);
                    return;
                }
                else if (param.Equals("set"))
                {
                    if (settings.Locations.Count > 0)
                    {
                        settings.Locations.RemoveAt(0);
                    }
                    var position = player.transform.position;
                    settings.Locations.Insert(0, settings.Location = position);
                    SaveConfig();
                    PrintMsgL(player, "DM_TownTPLocation", language, position);
                    return;
                }
                else if (param.Equals("add"))
                {
                    var position = player.transform.position;
                    int num = settings.Locations.RemoveAll(x => Vector3.Distance(position, x) < 25f);
                    settings.Locations.Add(position);
                    SaveConfig();
                    PrintMsgL(player, "DM_TownTPLocation", language, position);
                    return;
                }
                else if (args[0].Equals("show", StringComparison.OrdinalIgnoreCase))
                {
                    settings.Locations.ForEach(x => DrawText(player, 30f, Color.green, x, command));
                    return;
                }
            }

            bool paidmoney = false;

            // Is command usage enabled?
            if (!settings.Enabled)
            {
                PrintMsgL(player, "DM_TownTPDisabled", language.SentenceCase());
                return;
            }

            if (settings.Location != Vector3.zero && !settings.Locations.Contains(settings.Location))
            {
                settings.Locations.Add(settings.Location);
            }

            // Is location set?
            if (settings.Locations.Count == 0)
            {
                PrintMsgL(player, "DM_TownTPNoLocation", language.SentenceCase());
                return;
            }

            // Are they trying to bypass cooldown or did they just type something else?
            if (args.Length == 1 && !string.IsNullOrEmpty(config.Settings.BypassCMD) && args[0].ToLower() != config.Settings.BypassCMD.ToLower() && !args[0].All(char.IsDigit))
            {
                string com = command ?? "town";
                string msg = "SyntaxCommand" + char.ToUpper(com[0]) + com.Substring(1);
                PrintMsgL(player, msg);
                if (IsAllowed(player)) PrintMsgL(player, msg + "Admin");
                return;
            }

            if (!settings.Teleports.TPData.TryGetValue(player.userID, out var teleportData))
            {
                settings.Teleports.TPData[player.userID] = teleportData = new TeleportData();
            }
            int limit = 0;
            var timestamp = Facepunch.Math.Epoch.Current;
            var currentDate = DateTime.Now.ToString("d");
            var mode = command == "bandit" ? "bandit" : command == "outpost" ? "outpost" : "town";
            // Setup vars for checks below
            string err = null;
            if (!CanBypassRestrictions(player.UserIDString))
            {
                var getUseableTime = GetUseableTime(player, settings.Hours);
                if (getUseableTime > 0.0)
                {
                    PrintMsgL(player, "NotUseable", FormatTime(player, getUseableTime));
                    return;
                }
                err = CheckPlayer(player, settings.UsableOutOfBuildingBlocked, settings.CanCraft(player, command), true, mode, settings.CanCave(player, command));
                if (err != null)
                {
                    PrintMsgL(player, err);
                    return;
                }
                var cooldown = GetLower(player, settings.VIPCooldowns, settings.Cooldown);

                if (teleportData.Date != currentDate)
                {
                    teleportData.Amount = 0;
                    teleportData.Date = currentDate;
                }
                limit = GetHigher(player, settings.VIPDailyLimits, settings.DailyLimit, true);
#if DEBUG
                Puts("Calling CheckPlayer from cmdChatTown");
#endif

                // Check and process cooldown, bypass, and payment for all modes
                if (cooldown > 0 && timestamp - teleportData.Timestamp < cooldown)
                {
                    var cmdSent = args.Length >= 1 ? args[0].ToLower() : string.Empty;

                    if (!string.IsNullOrEmpty(config.Settings.BypassCMD))
                    {
                        if (cmdSent == config.Settings.BypassCMD.ToLower() && settings.Bypass > -1)
                        {
                            bool foundmoney = CheckEconomy(player, settings.Bypass);

                            if (foundmoney)
                            {
                                CheckEconomy(player, settings.Bypass, true);
                                paidmoney = true;

                                if (settings.Bypass > 0)
                                {
                                    PrintMsgL(player, "DM_TownTPCooldownBypass", settings.Bypass);
                                }

                                if (settings.Pay > 0)
                                {
                                    PrintMsgL(player, "PayToTown", settings.Pay, language);
                                }
                            }
                            else
                            {
                                PrintMsgL(player, "DM_TownTPCooldownBypassF", settings.Bypass);
                                return;
                            }
                        }
                        else if (UseEconomy())
                        {
                            if (Interface.CallHook("OnTeleportCooldownNotify", player) != null)
                            {
                                return;
                            }
                            var remain = cooldown - (timestamp - teleportData.Timestamp);
                            PrintMsgL(player, "DM_TownTPCooldown", FormatTime(player, remain));
                            if (settings.Bypass > -1)
                            {
                                PrintMsgL(player, "DM_TownTPCooldownBypassP", settings.Bypass);
                                PrintMsgL(player, "DM_TownTPCooldownBypassP2", language, config.Settings.BypassCMD);
                            }
                            return;
                        }
                        else
                        {
                            if (Interface.CallHook("OnTeleportCooldownNotify", player) != null)
                            {
                                return;
                            }
                            var remain = cooldown - (timestamp - teleportData.Timestamp);
                            PrintMsgL(player, "DM_TownTPCooldown", FormatTime(player, remain));
                            return;
                        }
                    }
                    else
                    {
                        if (Interface.CallHook("OnTeleportCooldownNotify", player) != null)
                        {
                            return;
                        }
                        var remain = cooldown - (timestamp - teleportData.Timestamp);
                        PrintMsgL(player, "DM_TownTPCooldown", FormatTime(player, remain));
                        return;
                    }
                }

                if (limit > 0 && teleportData.Amount >= limit)
                {
                    var left = FormatTime(player, (int)SecondsUntilTomorrow());
                    PrintMsgL(player, "DM_TownTPLimitReached", limit, left);
                    return;
                }
            }
            if (TeleportTimers.ContainsKey(player.userID))
            {
                PrintMsgL(player, "TeleportPendingTPC");
                return;
            }

            Vector3 location;
            if (args.Length == 1 && int.TryParse(args[0], out var index))
            {
                index = Mathf.Clamp(index, 0, settings.Locations.Count - 1);
                location = settings.Locations[index];
            }
            else if (settings.Random)
            {
                location = settings.Locations.GetRandom();
            }
            else if (Vector3.Distance(settings.Location, Vector3.zero) > 5f)
            {
                location = settings.Location;
            }
            else location = settings.Locations[0];

            if (!CanBypassRestrictions(player.UserIDString))
            {
                err = CanPlayerTeleport(player, location, player.transform.position);
                if (err != null)
                {
                    SendReply(player, err);
                    return;
                }
                err = CheckItems(player);
                if (err != null)
                {
                    PrintMsgL(player, "TPBlockedItem", err);
                    return;
                }
            }
            int countdown = GetLower(player, settings.VIPCountdowns, settings.Countdown);
            TeleportTimers[player.userID] = new()
            {
                Town = command,
                OriginPlayer = player,
                Timer = timer.Once(countdown, () =>
                {
#if DEBUG
                    Puts($"Calling CheckPlayer from cmdChatTown {command} timer loop");
#endif
                    if (!CanBypassRestrictions(player.UserIDString))
                    {
                        if (!TeleportInForcedBoundary(player))
                        {
                            return;
                        }
                        if (config.Settings.BlockAuthorizedTeleporting && player.IsBuildingAuthed())
                        {
                            PrintMsgL(player, "CannotTeleportFromHome");
                            return;
                        }
                        err = CheckPlayer(player, settings.UsableOutOfBuildingBlocked, settings.CanCraft(player, command), true, mode, settings.CanCave(player, command));
                        if (err != null)
                        {
                            Interrupt(player, paidmoney, settings.Bypass);
                            PrintMsgL(player, err);
                            return;
                        }
                        err = CanPlayerTeleport(player, location, player.transform.position);
                        if (err != null)
                        {
                            Interrupt(player, paidmoney, settings.Bypass);
                            PrintMsgL(player, err);
                            return;
                        }
                        err = CheckItems(player);
                        if (err != null)
                        {
                            Interrupt(player, paidmoney, settings.Bypass);
                            PrintMsgL(player, "TPBlockedItem", err);
                            return;
                        }
                        if (settings.Locations.Count == 0)
                        {
                            Interrupt(player, paidmoney, settings.Bypass);
                            return;
                        }
                        if (UseEconomy())
                        {
                            if (settings.Pay < 0)
                            {
                                return;
                            }
                            if (settings.Pay > 0 && !CheckEconomy(player, settings.Pay))
                            {
                                Interrupt(player, false, 0);
                                PrintMsgL(player, "TPNoMoney", settings.Pay);
                                return;
                            }
                            if (settings.Pay > -1 && !paidmoney)
                            {
                                CheckEconomy(player, settings.Pay, true);

                                if (settings.Pay > 0)
                                {
                                    PrintMsgL(player, "TPMoney", (double)settings.Pay);
                                }
                            }
                        }
                    }
                    Teleport(player, location, command, 0uL, town: true, allowTPB: settings.AllowTPB, build: settings.UsableOutOfBuildingBlocked, craft: settings.CanCraft(player, command), cave: settings.CanCave(player, command));
                    teleportData.Amount++;
                    teleportData.Timestamp = timestamp;
                    settings.Teleports.Changed = true;
                    PrintMsgL(player, "DM_TownTP", language);
                    if (limit > 0) PrintMsgL(player, "DM_TownTPAmount", limit - teleportData.Amount, language);
                    TeleportTimers.Remove(player.userID);
                })
            };

            if (countdown > 0)
            {
                PrintMsgL(player, "DM_TownTPStarted", language, countdown);
                Interface.CallHook("OnTownAccepted", player, language, countdown);
            }
        }

        private double SecondsUntilTomorrow()
        {
            var tomorrow = DateTime.Now.AddDays(1).Date;
            return (tomorrow - DateTime.Now).TotalSeconds;
        }

        private void Interrupt(BasePlayer player, bool paidmoney, double bypass)
        {
            PrintMsgL(player, "Interrupted");
            if (paidmoney)
            {
                CheckEconomy(player, bypass, false, true);
            }
            TeleportTimers.Remove(player.userID);
        }

        private void CommandTeleportII(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (!user.IsAdmin && !IsAllowedMsg(player, PermTpConsole)) return;

            List<BasePlayer> players;
            switch (command)
            {
                case "teleport.topos":
                    if (args.Length < 4)
                    {
                        user.Reply(_("SyntaxConsoleCommandToPos", player));
                        return;
                    }
                    players = FindPlayers(args[0], true);
                    if (players.Count <= 0)
                    {
                        user.Reply(_("PlayerNotFound", player));
                        return;
                    }
                    if (players.Count > 1)
                    {
                        user.Reply(_("MultiplePlayers", player, GetMultiplePlayers(players)));
                        return;
                    }
                    var targetPlayer = players[0];
                    players.Clear();
                    float x;
                    if (!float.TryParse(args[1], out x)) x = -10000f;
                    float y;
                    if (!float.TryParse(args[2], out y)) y = -10000f;
                    float z;
                    if (!float.TryParse(args[3], out z)) z = -10000f;
                    if (!CheckBoundaries(x, y, z))
                    {
                        user.Reply(_("AdminTPOutOfBounds", player) + System.Environment.NewLine + _("AdminTPBoundaries", player, boundary));
                        return;
                    }
                    Teleport(targetPlayer, x, y, z);
                    if (config.Admin.AnnounceTeleportToTarget)
                        PrintMsgL(targetPlayer, "AdminTPConsoleTP", targetPlayer.transform.position);
                    user.Reply(_("AdminTPTargetCoordinates", player, targetPlayer.displayName, targetPlayer.transform.position));
                    Puts(_("LogTeleportPlayer", null, player?.displayName, targetPlayer.displayName, targetPlayer.transform.position));
                    break;
                case "teleport.toplayer":
                    if (args.Length < 2)
                    {
                        user.Reply(_("SyntaxConsoleCommandToPlayer", player));
                        return;
                    }
                    players = FindPlayers(args[0], true);
                    if (players.Count <= 0)
                    {
                        user.Reply(_("PlayerNotFound", player));
                        return;
                    }
                    if (players.Count > 1)
                    {
                        user.Reply(_("MultiplePlayers", player, GetMultiplePlayers(players)));
                        return;
                    }
                    var originPlayer = players[0];
                    players = FindPlayers(args[1], true);
                    if (players.Count <= 0)
                    {
                        user.Reply(_("PlayerNotFound", player));
                        return;
                    }
                    if (players.Count > 1)
                    {
                        user.Reply(_("MultiplePlayers", player, GetMultiplePlayers(players)));
                        players.Clear();
                        return;
                    }
                    targetPlayer = players[0];
                    if (targetPlayer == originPlayer)
                    {
                        players.Clear();
                        user.Reply(_("CantTeleportPlayerToSelf", player));
                        return;
                    }
                    players.Clear();
                    Teleport(originPlayer, targetPlayer);
                    user.Reply(_("AdminTPPlayers", player, originPlayer.displayName, targetPlayer.displayName));
                    PrintMsgL(originPlayer, "AdminTPConsoleTPPlayer", targetPlayer.displayName);
                    if (config.Admin.AnnounceTeleportToTarget)
                        PrintMsgL(targetPlayer, "AdminTPConsoleTPPlayerTarget", originPlayer.displayName);
                    Puts(_("LogTeleportPlayer", null, player?.displayName, originPlayer.displayName, targetPlayer.displayName));
                    break;
            }
        }

        private void CommandSphereMonuments(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;
            if (player == null || !player.IsAdmin) return;
            if (args.Contains("drawmonuments"))
            {
                var diameter = args.Length > 1 && float.TryParse(args[1], out var r) ? Mathf.Min(r, World.Size) : 200f;
                CommandDrawChecks(player, player.transform.position, diameter);
                return;
            }
            if (args.Contains("drawtopology"))
            {
                var diameter = args.Length > 1 && float.TryParse(args[1], out var r) ? Mathf.Min(r, World.Size) : 200f;
                CommandDrawTopology(player, player.transform.position, diameter);
                return;
            }
            foreach (var mi in monuments)
            {
                if (mi.sphere)
                {
                    DrawSphere(player, 30f, Color.black, mi.position, mi.extents.Max());
                }
                else DrawMonument(player, mi.position, mi.extents, mi.rotation, Color.blue, 30f);
                DrawText(player, 30f, Color.blue, mi.position, mi.name);
            }
            foreach (var (cave, vector) in caves)
            {
                string name = cave.Contains(':') ? cave[..cave.LastIndexOf(':')] : cave.TrimEnd();
                DrawSphere(player, 30f, Color.black, vector, 25f);
                DrawText(player, 30f, Color.cyan, vector, name);
            }
        }

        private void CommandDrawChecks(BasePlayer player, Vector3 a, float diameter)
        {
            int minPos = (int)(diameter / -2f);
            int maxPos = (int)(diameter / 2f);

            for (float x = minPos; x < maxPos; x += 5f)
            {
                for (float z = minPos; z < maxPos; z += 5f)
                {
                    var pos = new Vector3(a.x + x, 0f, a.z + z);

                    pos.y = TerrainMeta.HeightMap.GetHeight(pos);

                    var res = NearMonument(pos, false, "test") != null;

                    if (res) DrawText(player, 15f, Color.red, pos, "X");
                }
            }
        }

        private void CommandDrawTopology(BasePlayer player, Vector3 a, float diameter)
        {
            int minPos = (int)(diameter / -2f);
            int maxPos = (int)(diameter / 2f);

            for (float x = minPos; x < maxPos; x += 5f)
            {
                for (float z = minPos; z < maxPos; z += 5f)
                {
                    var pos = new Vector3(a.x + x, 0f, a.z + z);

                    pos.y = TerrainMeta.HeightMap.GetHeight(pos);

                    var res = IsMonument(pos); // ContainsTopology(TerrainTopology.Enum.Building | TerrainTopology.Enum.Monument, pos, 5f);

                    if (res) DrawText(player, 15f, Color.magenta, pos, "X");
                }
            }
        }

        private bool IsMonument(Vector3 v) => HasMonumentTopology(v) && !IsCave(v) && HasPreventBuildingCollider(v);

        private bool HasMonumentTopology(Vector3 v) => (TerrainMeta.TopologyMap.GetTopology(v, 5f) & (int)TerrainTopology.Enum.Monument) != 0;

        private bool IsCave(Vector3 v) => GamePhysics.CheckSphere<TerrainCollisionTrigger>(v, 5f, 262144, QueryTriggerInteraction.Collide);

        private bool HasPreventBuildingCollider(Vector3 v)
        {
            List<Collider> obj = Pool.Get<List<Collider>>();
            Vis.Colliders(v, 0f, obj, Layers.Mask.Prevent_Building | Layers.Mask.Trigger);
            bool preventbuilding = false;
            bool safezone = false;
            foreach (var collider in obj)
            {
                if (!preventbuilding && collider.gameObject.layer == (int)Layer.Prevent_Building)
                {
                    preventbuilding = true;
                }
                else if (!safezone && collider.GetComponent<TriggerSafeZone>() != null)
                {
                    safezone = true;
                }
            }
            Pool.FreeUnmanaged(ref obj);
            return preventbuilding && !safezone;
        }

        private void CommandImportHomes(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (DisabledCommandData.DisabledCommands.Contains(command)) { user.Reply("Disabled command: " + command); return; }
            var player = user.Object as BasePlayer;

            if (!user.IsAdmin && !IsAllowedMsg(player, PermImportHomes))
            {
                user.Reply(_("NotAllowed", player));
                return;
            }
            var fileName = string.IsNullOrEmpty(config.Settings.DataFileFolder) ? "m-Teleportation" : $"{config.Settings.DataFileFolder}{Path.DirectorySeparatorChar}m-Teleportation";
            var datafile = Interface.Oxide.DataFileSystem.GetFile(fileName);
            if (!datafile.Exists())
            {
                user.Reply("No m-Teleportation.json exists.");
                return;
            }
            datafile.Load();
            var allHomeData = datafile["HomeData"] as Dictionary<string, object>;
            if (allHomeData == null)
            {
                user.Reply(_("HomeListEmpty", player));
                return;
            }
            var count = 0;
            foreach (var kvp in allHomeData)
            {
                var homeDataOld = kvp.Value as Dictionary<string, object>;
                if (homeDataOld == null) continue;
                if (!homeDataOld.ContainsKey("HomeLocations")) continue;
                var homeList = homeDataOld["HomeLocations"] as Dictionary<string, object>;
                if (homeList == null) continue;
                var userId = Convert.ToUInt64(kvp.Key);
                HomeData homeData;
                if (!_Home.TryGetValue(userId, out homeData))
                    _Home[userId] = homeData = new HomeData();
                var target = RustCore.FindPlayerById(userId);
                foreach (var kvp2 in homeList)
                {
                    var positionData = kvp2.Value as Dictionary<string, object>;
                    if (positionData == null) continue;
                    if (!positionData.ContainsKey("x") || !positionData.ContainsKey("y") || !positionData.ContainsKey("z")) continue;
                    var position = new Vector3(Convert.ToSingle(positionData["x"]), Convert.ToSingle(positionData["y"]), Convert.ToSingle(positionData["z"]));
                    homeData.Set(kvp2.Key, new HomeData.Entry(position));
                    changedHome = true;
                    count++;
                    Interface.CallHook("OnHomeAdded", target, position, kvp2.Key);
                }
            }
            user.Reply(string.Format("Imported {0} homes.", count));
            if (!user.IsServer) Puts("Imported {0} homes.", count);
        }

        private void RequestTimedOut(BasePlayer player, BasePlayer target)
        {
            PlayersRequests.Remove(player.userID);
            PlayersRequests.Remove(target.userID);
            PendingRequests.Remove(target.userID);
            PrintMsgL(player, "TimedOut", target.displayName);
            PrintMsgL(target, "TimedOutTarget", player.displayName);
        }

        private void CommandPluginInfo(IPlayer user, string command, string[] args)
        {
            command = command.ToLower();
            if (!user.IsServer) return;
            user.Reply($"01. {permission.GetPermissionGroups("nteleportation.tp").Length}");
            user.Reply($"02. {permission.GetPermissionGroups("nteleportation.admin").Length}");
            user.Reply($"03. {permission.GetPermissionUsers("nteleportation.tp").Length}");
            user.Reply($"04. {permission.GetPermissionUsers("nteleportation.admin").Length}");
            user.Reply($"05. {permission.GroupHasPermission("admin", "nteleportation.tp")}");
            user.Reply($"06. {permission.GroupHasPermission("admin", "nteleportation.admin")}");
            user.Reply($"07. {permission.GroupHasPermission("default", "nteleportation.tp")}");
            user.Reply($"08. {permission.GroupHasPermission("default", "nteleportation.admin")}");
            user.Reply($"09. {BasePlayer.activePlayerList.Count(x => x?.Connection?.authLevel > 0)}");
            user.Reply($"10. {BasePlayer.activePlayerList.Count(x => IsAllowed(x))}");
            user.Reply($"11. {BasePlayer.activePlayerList.Count}");
        }

        #region Util

        private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder();

        private string FormatTime(BasePlayer player, double seconds) // Credits MoNaH
        {
            if (config.Settings.UseSeconds) return $"{seconds} {_("Seconds", player)}";

            TimeSpan _ts = TimeSpan.FromSeconds(seconds);

            _sb.Length = 0;

            if (_ts.TotalDays >= 1)
            {
                _sb.Append($"<color={config.Settings.ChatCommandArgumentColor}>{_ts.Days}</color> {_("Days", player)} ");
            }

            if (_ts.TotalHours >= 1)
            {
                _sb.Append($"<color={config.Settings.ChatCommandArgumentColor}>{_ts.Hours}</color> {_("Hours", player)} ");
            }

            if (_ts.TotalMinutes >= 1)
            {
                _sb.Append($"<color={config.Settings.ChatCommandArgumentColor}>{_ts.Minutes}</color> {_("Minutes", player)} ");
            }

            _sb.Append($"<color={config.Settings.ChatCommandArgumentColor}>{_ts.Seconds}</color> {_("Seconds", player)} ");

            return _sb.ToString();
        }

        #endregion

        #region Teleport

        public void Teleport(BasePlayer player, BasePlayer target, bool build = true, bool craft = true, bool cave = true) => Teleport(player, target.transform.position, "", target.userID, town: false, allowTPB: true, build: build, craft: craft, cave: cave);

        public void Teleport(BasePlayer player, float x, float y, float z, bool build = true, bool craft = true, bool cave = true) => Teleport(player, new Vector3(x, y, z), "", 0uL, town: false, allowTPB: true, build: build, craft: craft, cave: cave);

        [HookMethod("Teleport")]
        public void Teleport(BasePlayer player, Vector3 newPosition, string home, ulong uid, bool town, bool allowTPB, bool build = true, bool craft = true, bool cave = true)
        {
            if (!player.IsValid())
            {
                return;
            }

            if (Vector3.Distance(newPosition, Vector3.zero) < 5f)
            {
                return;
            }

            if (allowTPB)
            {
                if (config.Settings.TPB.Time > 0)
                {
                    RemoveLocation(player);
                    Vector3 position = player.transform.position;
                    timer.In(config.Settings.TPB.Time, () => SaveLocation(player, position, home, uid, town, build, craft, cave));
                }
                else SaveLocation(player, player.transform.position, home, uid, town, build, craft, cave);
            }

            if (config.Settings.PlaySoundsBeforeTeleport)
            {
                SendEffect(player, config.Settings.DisappearEffects);
            }

            newPosition.y += 0.1f;

            teleporting[player.userID] = newPosition;

            Subscribe(nameof(OnPlayerViolation));

            var oldPosition = player.transform.position;

            // credits to @ctv and @Def for their assistance

            player.PauseFlyHackDetection(5f);
            player.PauseSpeedHackDetection(5f);
            player.UpdateActiveItem(default);
            player.EnsureDismounted();
            player.Server_CancelGesture();

            if (player.HasParent())
            {
                player.SetParent(null, true, true);
            }

            if (player.IsConnected)
            {
                StartSleeping(player);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                player.ClientRPC(RpcTarget.Player(config.Settings.Quick ? "StartLoading_Quick" : "StartLoading", player), arg1: true);
            }

            player.Teleport(newPosition);

            if (player.IsConnected)
            {
                if (!player._limitedNetworking)
                {
                    player.UpdateNetworkGroup();
                    player.SendNetworkUpdateImmediate(false);
                }

                player.ClearEntityQueue(null);
                player.SendFullSnapshot();
                if (CanWake(player)) player.Invoke(() =>
                {
                    if (player && player.IsConnected)
                    {
                        if (player.limitNetworking) EndSleeping(player);
                        else player.EndSleeping();
                    }
                }, 0.5f);
            }

            if (!player._limitedNetworking)
            {
                player.ForceUpdateTriggers();
            }

            timer.Once(3f, () => RemoveProtections(player.userID));

            SetGlobalCooldown(player);

            if (config.Settings.PlaySoundsAfterTeleport)
            {
                SendEffect(player, config.Settings.ReappearEffects);
            }

            Interface.CallHook("OnPlayerTeleported", player, oldPosition, newPosition);
        }

        private bool CanWake(BasePlayer player)
        {
            if (!config.Settings.AutoWakeUp) return false;
            return player.IsOnGround() || player.limitNetworking || player.IsFlying || player.IsAdmin;
        }

        public void RemoveProtections(ulong userid)
        {
            teleporting.Remove(userid);
        }

        public void StartSleeping(BasePlayer player) // custom as to not cancel crafting, or remove player from vanish
        {
            if (!player.IsSleeping())
            {
                Interface.CallHook("OnPlayerSleep", player);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, b: true);
                player.sleepStartTime = Time.time;
                BasePlayer.sleepingPlayerList.Add(player);
                player.CancelInvoke("InventoryUpdate");
                player.CancelInvoke("TeamUpdate");
                player.inventory.loot.Clear();
                player.inventory.containerMain.OnChanged();
                player.inventory.containerBelt.OnChanged();
                player.inventory.containerWear.OnChanged();
                player.Invoke("TurnOffAllLights", 0f);
                if (!player._limitedNetworking)
                {
                    player.EnablePlayerCollider();
                    player.RemovePlayerRigidbody();
                }
                else player.RemoveFromTriggers();
                player.SetServerFall(wantsOn: true);
            }
        }

        private void EndSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
            {
                if (player.IsRestrained)
                {
                    player.inventory.SetLockedByRestraint(flag: true);
                }
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, b: false);
                player.sleepStartTime = -1f;
                BasePlayer.sleepingPlayerList.Remove(player);
                player.CancelInvoke(player.ScheduledDeath);
                player.InvokeRepeating("InventoryUpdate", 1f, 0.1f * UnityEngine.Random.Range(0.99f, 1.01f));
                if (RelationshipManager.TeamsEnabled())
                {
                    player.InvokeRandomized(player.TeamUpdate, 1f, 4f, 1f);
                }
                player.InvokeRandomized(player.UpdateClanLastSeen, 300f, 300f, 60f);
                player.inventory.containerMain.OnChanged();
                player.inventory.containerBelt.OnChanged();
                player.inventory.containerWear.OnChanged();
                Interface.CallHook("OnPlayerSleepEnded", player);
                EACServer.LogPlayerSpawn(player);
                if (player.State?.pings?.Count > 0)
                {
                    player.SendPingsToClient();
                }
                if (TutorialIsland.ShouldPlayerBeAskedToStartTutorial(player))
                {
                    player.ClientRPC(RpcTarget.Player("PromptToStartTutorial", player));
                }
            }
        }

        [PluginReference] Plugin RaidableBases, AbandonedBases;

        private List<string> blockMapMarker = new();

        private void CommandBlockMapMarker(IPlayer user, string command, string[] args)
        {
            if (!blockMapMarker.Remove(user.Id))
            {
                blockMapMarker.Add(user.Id);
            }
        }

        private void OnMapMarkerAdded(BasePlayer player, ProtoBuf.MapNote note)
        {
            if (player.IsAlive() && !blockMapMarker.Contains(player.UserIDString))
            {
                if (permission.UserHasPermission(player.UserIDString, "nteleportation.blocktpmarker"))
                {
                    PrintMsgL(player, "BlockedMarkerTeleport");
                }
                else if (permission.UserHasPermission(player.UserIDString, PermTpMarker))
                {
                    float y = TerrainMeta.HeightMap.GetHeight(note.worldPosition);
                    if (player.IsFlying) y = Mathf.Max(y, player.transform.position.y);
                    if (config.Settings.CheckBoundaries && !CheckBoundaries(note.worldPosition.x, y, note.worldPosition.z))
                    {
                        PrintMsgL(player, "AdminTPOutOfBounds");
                        PrintMsgL(player, "AdminTPBoundaries", boundary);
                    }
                    else if (EventTerritory(note.worldPosition))
                    {
                        PrintMsgL(player, "BlockedMarkerTeleport");
                    }
                    else if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, PermAdmin) && player.IsBuildingBlocked(note.worldPosition, Quaternion.identity, player.bounds))
                    {
                        PrintMsgL(player, "BlockedAuthMarkerTeleport");
                    }
                    else player.Teleport(note.worldPosition + new Vector3(0f, y, 0f));
                }
            }
        }

        private bool EventTerritory(Vector3 worldPosition)
        {
            if (config.Settings.BlockAbandoned && AbandonedBases != null && Convert.ToBoolean(AbandonedBases?.Call("EventTerritory", worldPosition))) return true;
            if (config.Settings.BlockRaidable && RaidableBases != null && Convert.ToBoolean(RaidableBases?.Call("EventTerritory", worldPosition))) return true;
            return false;
        }

        #endregion

        #region Checks
        private string CanPlayerTeleport(BasePlayer player, params Vector3[] vectors)
        {
            if (CanBypassRestrictions(player.UserIDString)) return null;
            foreach (var to in vectors)
            {
                var err = Interface.Oxide.CallHook("CanTeleport", player, to) as string;
                if (!string.IsNullOrEmpty(err)) return err;
            }
            return null;
        }

        private string CanPlayerTeleportHome(BasePlayer player, Vector3 homePos)
        {
            if (CanBypassRestrictions(player.UserIDString)) return null;
            var err = Interface.Oxide.CallHook("CanTeleportHome", player, homePos) as string;
            if (!string.IsNullOrEmpty(err)) return err;
            return null;
        }

        private bool CanCraftHome(BasePlayer player)
        {
            return config.Home.AllowCraft || permission.UserHasPermission(player.UserIDString, PermCraftHome) || CanBypassRestrictions(player.UserIDString);
        }

        private bool CanCaveHome(BasePlayer player)
        {
            return config.Home.AllowCave || permission.UserHasPermission(player.UserIDString, PermCaveHome) || CanBypassRestrictions(player.UserIDString);
        }

        private bool CanCraftTPR(BasePlayer player)
        {
            return config.TPR.AllowCraft || permission.UserHasPermission(player.UserIDString, PermCraftTpR) || CanBypassRestrictions(player.UserIDString);
        }

        private bool CanCaveTPR(BasePlayer player)
        {
            return config.TPR.AllowCave || permission.UserHasPermission(player.UserIDString, PermCaveTpR) || CanBypassRestrictions(player.UserIDString);
        }

        private List<string> monumentExceptions = new() { "outpost", "bandit", "substation", "swamp", "compound.prefab" };

        private bool IsInAllowedMonument(Vector3 target, string mode)
        {
            foreach (var mi in monuments)
            {
                if (config.Settings.Interrupt.BypassMonumentMarker && mi.prefab.Contains("monument_marker"))
                {
                    continue;
                }
                if (mi.IsInBounds(target))
                {
                    if (monumentExceptions.Exists(mi.name.ToLower().Contains))
                    {
                        return true;
                    }
                    return !config.Settings.Interrupt.Monument || mode != "sethome" && config.Settings.Interrupt.Monuments.Exists(value => mi.name.Contains(value, CompareOptions.OrdinalIgnoreCase));
                }
            }
            return false;
        }

        private string NearMonument(Vector3 target, bool check, string mode)
        {
            Dictionary<string, float> data = new();
            foreach (var mi in monuments)
            {
                if (monumentExceptions.Exists(mi.name.ToLower().Contains)) continue;
                if (!check && config.Settings.Interrupt.BypassMonumentMarker && mi.prefab.Contains("monument_marker")) continue;

                float dist = Vector3Ex.Distance2D(target, mi.position);
#if DEBUG
                Puts($"Checking {mi.name} dist: {dist}, realdistance: {mi.extents.Max()}, size: {mi.extents.Max() * 2f}, isinbounds: {mi.IsInBounds(target)}");
#endif
                if (mi.IsInBounds(target))
                {
                    if (config.Home.AllowedMonuments.Exists(m => mi.name.Equals(m, StringComparison.OrdinalIgnoreCase)))
                    {
                        return null;
                    }

                    if (config.Settings.Interrupt.Monuments.Count > 0 && mode != "sethome")
                    {
                        if (config.Settings.Interrupt.Monuments.Exists(value => mi.name.Contains(value, CompareOptions.OrdinalIgnoreCase)))
                        {
#if DEBUG
                            Puts($"{target} in range of {mi.name} at {dist}m {mi.extents} {mi.sphere}");
#endif
                            data[mi.name] = dist;
                        }

                        if (data.Count > 0)
                        {
                            continue;
                        }
#if DEBUG
                        Puts($"{target} is not blocked from {mi.name}");
#endif
                        return null;
                    }
#if DEBUG
                    Puts($"{target} in range of {mi.name} at {dist}m");
#endif
                    data[mi.name] = dist;
                }
            }
            if (data.Count > 0)
            {
                var s = data.OrderByDescending(pair => pair.Value).Take(2).Select(pair => (Name: pair.Key, Distance: pair.Value)).ToList();
                return s.Count > 1 && s[0].Distance > s[1].Distance ? s[1].Name : s[0].Name;
            }
            return null;
        }

        private string CheckPlayer(BasePlayer player, bool build = false, bool craft = false, bool origin = true, string mode = "home", bool allowcave = true)
        {
            if (CanBypassRestrictions(player.UserIDString)) return null;
            if (config.Settings.Interrupt.Oilrig || config.Settings.Interrupt.Excavator || config.Settings.Interrupt.Monument || mode == "sethome")
            {
                string monname = !config.Settings.Interrupt.Safe && player.InSafeZone() ? null : NearMonument(player.transform.position, false, mode);

                if (!string.IsNullOrEmpty(monname))
                {
                    if (mode == "sethome")
                    {
                        if (config.Home.AllowAtAllMonuments || config.Home.AllowedMonuments.Exists(value => monname.Contains(value, CompareOptions.OrdinalIgnoreCase)))
                        {
                            return null;
                        }
                        //player.ChatMessage(monname);
                        return "HomeTooCloseToMon";
                    }
                    else
                    {
                        if (config.Settings.Interrupt.Oilrig && monname.Contains("Oil Rig"))
                        {
                            return "TPOilRig";
                        }

                        if (config.Settings.Interrupt.Excavator && monname.Contains("Excavator"))
                        {
                            return "TPExcavator";
                        }

                        if (config.Settings.Interrupt.Monument)
                        {
                            if (config.Home.AllowedMonuments.Exists(value => monname.Contains(value, CompareOptions.OrdinalIgnoreCase)))
                            {
                                return null;
                            }

                            if (monname.Contains(":")) monname = monname.Substring(0, monname.IndexOf(":"));
                            return _("TooCloseToMon", player, _(monname, player));
                        }
                    }
                }
            }

#if DEBUG
            Puts($"CheckPlayer(): called mode is {mode}");
#endif
            switch (mode)
            {
                case "tpt":
                    allowcave = config.TPT.AllowCave;
                    break;
                case "home":
                    allowcave = config.Home.AllowCave;
                    break;
                case "tpa":
                case "tpr":
                    allowcave = config.TPR.AllowCave;
                    break;
                default:
#if DEBUG
                    Puts("Skipping cave check...");
#endif
                    break;
            }
            if (!allowcave)
            {
#if DEBUG
                Puts("Checking cave distance...");
#endif
                if (IsInCave(player.transform.position))
                {
                    return "TooCloseToCave";
                }
            }

            if (config.Settings.Interrupt.Hostile && (mode == "bandit" || mode == "outpost" || config.Settings.Interrupt.IncludeHostileTown && mode == "town"))
            {
                if (player.State.unHostileTimestamp > TimeEx.currentTimestamp || player.unHostileTime > UnityEngine.Time.realtimeSinceStartup)
                {
                    return "TPHostile";
                }
            }

            if (config.Settings.Interrupt.Junkpiles && IsOnJunkPile(player))
            {
                return "TPJunkpile";
            }

            if (config.Settings.Interrupt.Hurt && origin && player.IsWounded())
            {
                return "TPWounded";
            }

            if (config.Settings.Interrupt.Cold && player.metabolism.temperature.value <= config.Settings.MinimumTemp)
            {
                return "TPTooCold";
            }

            if (config.Settings.Interrupt.Hot && player.metabolism.temperature.value >= config.Settings.MaximumTemp)
            {
                return "TPTooHot";
            }

            if (config.Settings.Interrupt.Swimming && player.IsSwimming())
            {
                return "TPSwimming";
            }

            if (config.Settings.Interrupt.Cargo && player.GetComponentInParent<CargoShip>())
            {
                return "TPCargoShip";
            }

            if (config.Settings.Interrupt.Balloon && player.GetComponentInParent<HotAirBalloon>())
            {
                return "TPHotAirBalloon";
            }

            if (config.Settings.Interrupt.Lift && player.GetComponentInParent<Lift>())
            {
                return "TPBucketLift";
            }

            if (config.Settings.Interrupt.Lift && GetLift(player.transform.position))
            {
                return "TPRegLift";
            }

            if (config.Settings.Interrupt.Safe && player.InSafeZone())
            {
                return "TPSafeZone";
            }

            if (!craft && player.inventory.crafting.queue.Count > 0)
            {
                return "TPCrafting";
            }

            if (player.IsDead())
            {
                return "TPDead";
            }

            if (!build && !player.CanBuild())
            {
                return "TPBuildingBlocked";
            }

            if (config.Settings.BlockZoneFlag && ZoneManager != null && Convert.ToBoolean(ZoneManager?.Call("PlayerHasFlag", player, "notp")))
            {
                return "TPFlagZone";
            }

            if (config.Settings.BlockNoEscape && NoEscape != null && Convert.ToBoolean(NoEscape?.Call("IsBlocked", player)))
            {
                return "TPNoEscapeBlocked";
            }

            if (config.Settings.RaidBlock && RaidBlock != null && Convert.ToBoolean(RaidBlock?.Call("IsBlocked", player)))
            {
                return "TPNoEscapeBlocked";
            }

            var entity = GetStandingOnEntity<BaseMountable>(player, 1f, Layers.Mask.Vehicle_Detailed | Layers.Mask.Vehicle_Large);

            if (entity is BaseMountable)
            {
                if (entity is Tugboat)
                {
                    return !config.Home.AllowTugboats && !permission.UserHasPermission(player.UserIDString, "nteleportation.tugboatsinterruptbypass") ? "TPTugboat" : null;
                }

                if (config.Settings.Interrupt.Boats && entity is BaseBoat)
                {
                    return "TPBoat";
                }

                if (config.Settings.Interrupt.Mounted)
                {
                    //var ent1 = player.HasParent() && FindEntity<BaseMountable>(player.GetParentEntity()) is BaseMountable m1 ? m1.ShortPrefabName : "N/A";
                    //var ent2 = player.isMounted && FindEntity<BaseMountable>(player.GetMounted()) is BaseMountable m2 ? m2.ShortPrefabName : "N/A";
                    //var ent3 = GetStandingOnEntity<BaseMountable>(player.transform.position, 1f, Layers.Mask.Vehicle_Detailed | Layers.Mask.Vehicle_Large) is BaseMountable m3 ? m3.ShortPrefabName : "N/A";

                    //return $"{_("TPMounted", player)} (parent: {ent1}, mount: {ent2}, stand: {ent3})";
                    return "TPMounted";
                }
            }

            if (IsWaterBlockedAbove(player, entity))
            {
                return "TPAboveWater";
            }

            if (config.Settings.Interrupt.UnderWater && Math.Round(player.transform.position.y, 2) < Math.Round(TerrainMeta.WaterMap.GetHeight(player.transform.position), 2) && !IsInAllowedMonument(player.transform.position, mode))
            {
                return "TPUnderWater";
            }

            return null;
        }

        private bool IsWaterBlockedAbove(BasePlayer player, BaseEntity entity)
        {
            if (!config.Settings.Interrupt.AboveWater || !AboveWater(player.transform.position))
            {
                return false;
            }
            if ((config.Home.AllowTugboats || permission.UserHasPermission(player.UserIDString, "nteleportation.tugboatsinterruptbypass")) && entity is Tugboat)
            {
                return false;
            }
            if (!config.Settings.Interrupt.Boats && entity != null && entity.ShortPrefabName != "tugboat" && entity is BaseBoat)
            {
                return false;
            }
            return true;
        }

        private string CheckTargetLocation(BasePlayer player, Vector3 targetLocation, bool usableIntoBuildingBlocked, bool cupOwnerAllowOnBuildingBlocked)
        {
            if (CanBypassRestrictions(player.UserIDString)) return null;
            // ubb == UsableIntoBuildingBlocked
            // obb == CupOwnerAllowOnBuildingBlocked
            bool denied = false;
            var entities = FindEntitiesOfType<BaseEntity>(targetLocation, 3f, Layers.Mask.Construction | Layers.Mask.Vehicle_Large);
            foreach (var entity in entities)
            {
                if (entity is Tugboat)
                {
                    entities.ResetToPool();
                    if (usableIntoBuildingBlocked || player.CanBuild(entity.WorldSpaceBounds())) return null;
                    return "TPTargetBuildingBlocked";
                }
                if (!(entity is BuildingBlock block))
                {
                    continue;
                }
                if (CheckCupboardBlock(block, player, cupOwnerAllowOnBuildingBlocked))
                {
                    denied = false;
#if DEBUG
                    Puts("Cupboard either owned or there is no cupboard");
#endif
                }
                else if (usableIntoBuildingBlocked && player.userID != block.OwnerID)
                {
                    denied = false;
#if DEBUG
                    Puts("Player does not own block, but UsableIntoBuildingBlocked=true");
#endif
                }
                else if (player.userID == block.OwnerID)
                {
#if DEBUG
                    Puts("Player owns block");
#endif

                    if (!player.IsBuildingBlocked(targetLocation, Quaternion.identity, block.bounds))
                    {
#if DEBUG
                        Puts("Player not BuildingBlocked. Likely unprotected building.");
#endif
                        denied = false;
                        break;
                    }
                    else if (usableIntoBuildingBlocked)
                    {
#if DEBUG
                        Puts("Player not blocked because UsableIntoBuildingBlocked=true");
#endif
                        denied = false;
                        break;
                    }
                    else
                    {
#if DEBUG
                        Puts("Player owns block but blocked by UsableIntoBuildingBlocked=false");
#endif
                        denied = true;
                        break;
                    }
                }
                else
                {
#if DEBUG
                    Puts("Player blocked");
#endif
                    denied = true;
                    break;
                }
            }
            entities.ResetToPool();
            return denied ? "TPTargetBuildingBlocked" : null;
        }

        // Check that a building block is owned by/attached to a cupboard, allow tp if not blocked unless allowed by config
        private bool CheckCupboardBlock(BuildingBlock block, BasePlayer player, bool cupOwnerAllowOnBuildingBlocked)
        {
            // obb == CupOwnerAllowOnBuildingBlocked
            var building = block.GetBuilding();
            if (building != null)
            {
#if DEBUG
                Puts("Found building, checking privileges...");
                Puts($"Building ID: {building.ID}");
#endif
                // cupboard overlap.  Check privs.
                if (building.buildingPrivileges == null)
                {
#if DEBUG
                    Puts("No cupboard found, allowing teleport");
#endif
                    return true;
                }

                foreach (var priv in building.buildingPrivileges)
                {
                    if (priv.IsAuthed(player))
                    {
#if DEBUG
                        Puts("Player is authorized to the cupboard");
#endif
                        return true;
                    }
                }

                if (player.userID == block.OwnerID)
                {
                    if (cupOwnerAllowOnBuildingBlocked)
                    {
#if DEBUG
                        // player set the cupboard and is allowed in by config
                        Puts("Player owns cupboard with no auth, but allowed by CupOwnerAllowOnBuildingBlocked=true");
#endif
                        return true;
                    }
#if DEBUG
                    // player set the cupboard but is blocked by config
                    Puts("Player owns cupboard with no auth, but blocked by CupOwnerAllowOnBuildingBlocked=false");
#endif
                    return false;
                }

#if DEBUG
                // player not authed
                Puts("Player does not own cupboard and is not authorized");
#endif
                return false;
            }
#if DEBUG
            Puts("No cupboard or building found - we cannot tell the status of this block");
#endif
            return true;
        }

        private string CheckItems(BasePlayer player)
        {
            var backpack = player.inventory.GetBackpackWithInventory();
            foreach (var blockedItem in ReverseBlockedItems)
            {
                if (player.inventory.FindItemByItemID(blockedItem.Key) != null)
                {
                    return blockedItem.Value;
                }
                if (backpack != null && backpack.contents.FindItemByItemID(blockedItem.Key) != null)
                {
                    return blockedItem.Value;
                }
            }
            return null;
        }

        private static List<T> FindEntitiesOfType<T>(Vector3 a, float n, int m = -1) where T : BaseEntity
        {
            List<T> entities = Pool.Get<List<T>>();
            entities.Clear();
            Vis.Entities(a, n, entities, m, QueryTriggerInteraction.Collide);
            entities.RemoveAll(x => x.IsKilled());
            return entities;
        }

        private bool IsInsideEntity(Vector3 a)
        {
            bool faces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            RaycastHit hit;
            bool isHit = Physics.Raycast(a + new Vector3(0f, 0.015f, 0f), Vector3.up, out hit, 7f, Layers.Mask.Construction | Layers.Mask.Deployed, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = faces;
            if (isHit)
            {
                var e = hit.GetEntity();
                if (e == null || e.PrefabName.Contains(".grill") || e.PrefabName.Contains("hatch"))
                {
                    return false;
                }
                if (e is BuildingBlock)
                {
                    return e.ShortPrefabName.Contains("foundation");
                }
                if (e is SimpleBuildingBlock || e is IceFence || e is ElectricBattery || e is Door || e is BaseOven)
                {
                    return Math.Round(a.y, 2) < Math.Round(hit.point.y, 2);
                }
            }
            return false;
        }

        private string IsInsideEntity(Vector3 targetLocation, ulong userid, string mode)
        {
            if (IsInsideEntity(targetLocation))
            {
                return "TPTargetInsideEntity";
            }
            if (config.Settings.Rock && NearMonument(targetLocation, true, mode) == null && Exploits.TestInsideRock(targetLocation))
            {
                LogToFile("exploiters", $"{userid} sethome inside a rock at {targetLocation}", this, true);
                PrintMsgL(userid, "TPTargetInsideRock");
                return "TPTargetInsideRock";
            }
            return null;
        }

        private bool UnderneathFoundation(Vector3 a)
        {
            RaycastHit hit;
            if (Physics.Raycast(a + new Vector3(0f, 3f, 0f), Vector3.down, out hit, 5f, Layers.Mask.Construction, QueryTriggerInteraction.Ignore))
            {
                var e = hit.GetEntity();

                if (e is BuildingBlock && e.ShortPrefabName.Contains("foundation"))
                {
                    return Math.Round(a.y, 2) < Math.Round(hit.point.y, 2);
                }
            }
            return false;
        }

        private string CheckFoundation(ulong userid, Vector3 position, string mode)
        {
            if (CanBypassRestrictions(userid.ToString())) return null;
            string insideErr = IsInsideEntity(position, userid, mode);
            if (insideErr != null)
            {
                return insideErr;
            }
            if (IsBlockedOnIceberg(position))
            {
                return "HomeIce";
            }
            if (!config.Home.ForceOnTopOfFoundation || permission.UserHasPermission(userid.ToString(), PermFoundationCheck))
            {
                return null;
            }
            if (UnderneathFoundation(position))
            {
                return "HomeFoundationUnderneathFoundation";
            }
            if (!IsStandingOnEntity(position, 2f, Layers.Mask.Construction | Layers.Mask.Vehicle_Large, out var entity, !config.Home.AllowAboveFoundation ? new string[2] { "foundation", "tugboat" } : new string[3] { "floor", "foundation", "tugboat" }))
            {
                return "HomeNoFoundation";
            }
            if (!config.Home.CheckFoundationForOwner || entity is Tugboat || IsAlly(userid, entity.OwnerID, config.Home.UseTeams, config.Home.UseClans, config.Home.UseFriends))
            {
                return null;
            }
            return "HomeFoundationNotFriendsOwned";
        }

        private bool IsInTrainTunnels(Vector3 a) => EnvironmentManager.Check(a, EnvironmentType.TrainTunnels);

        private bool IsUnderground(Vector3 a) => EnvironmentManager.Check(a, EnvironmentType.Underground);

        private bool IsUnderwaterLab(Vector3 a) => EnvironmentManager.Check(a, EnvironmentType.UnderwaterLab, 1f);

        private bool IsInBounds(BuildingBlock block, Vector3 a)
        {
            OBB obb = new OBB(block.transform.position + new Vector3(0f, 1f), block.transform.lossyScale, block.transform.rotation, block.bounds);
            if (obb.Contains(a))
            {
                return true;
            }
            Matrix4x4 m = Matrix4x4.TRS(block.transform.position, block.transform.rotation, Vector3.one);
            Vector3 v = m.inverse.MultiplyPoint3x4(a);
            Vector3 extents = block.bounds.extents;
            return v.x <= extents.x && v.x > -extents.x && v.y <= extents.y && v.y > -extents.y && v.z <= extents.z && v.z > -extents.z;
        }

        private bool IsBlockedOnIceberg(Vector3 position)
        {
            if (config.Home.AllowIceberg) return false;
            if (!Physics.SphereCast(position + new Vector3(0f, 1f), 1f, Vector3.down, out var hit, 250f, Layers.Mask.Terrain | Layers.Mask.World)) return false;
            return hit.collider.name.Contains("ice_sheet") || hit.collider.name.Contains("iceberg");
        }

        private BuildingBlock GetFoundationOwned(Vector3 position, ulong userID)
        {
            if (!IsStandingOnEntity(position, 2f, Layers.Mask.Construction, out var entity, new string[1] { "foundation" }) || !PassesStrictCheck(entity, position)) return null;
            if (!config.Home.CheckFoundationForOwner || IsAlly(userID, entity.OwnerID, config.Home.UseTeams, config.Home.UseClans, config.Home.UseFriends)) return entity as BuildingBlock;
            return null;
        }

        private bool IsAlly(ulong playerId, ulong targetId, bool useTeams, bool useClans, bool useFriends)
        {
            if (playerId == targetId)
            {
                return true;
            }
            if (useTeams && RelationshipManager.ServerInstance.playerToTeam.TryGetValue(playerId, out var team) && team.members.Contains(targetId))
            {
                return true;
            }
            if (useClans && Clans != null && Clans.IsLoaded && Convert.ToBoolean(Clans?.Call("IsClanMember", playerId.ToString(), targetId.ToString())))
            {
                return true;
            }
            if (useFriends && Friends != null && Friends.IsLoaded && Convert.ToBoolean(Friends?.Call("AreFriends", playerId.ToString(), targetId.ToString())))
            {
                return true;
            }
            return false;
        }

        bool IsBlockedUser(ulong playerid, ulong targetid)
        {
            if (config.TPR.UseBlockedUsers && BlockUsers != null && BlockUsers.IsLoaded)
            {
#if DEBUG
                Puts("Is user blocked? {0} / {1}", playerid, targetid);
#endif
                if (Convert.ToBoolean(BlockUsers?.CallHook("IsBlockedUser", playerid, targetid)))
                {
#if DEBUG
                    Puts("  BlockUsers plugin returned true");
#endif
                    return true;
                }
#if DEBUG
                Puts("  BlockUsers plugin returned false");
#endif
            }
            return false;
        }

        private bool PassesStrictCheck(BaseEntity entity, Vector3 position)
        {
            if (!config.Settings.StrictFoundationCheck || entity is Tugboat)
            {
                return true;
            }
#if DEBUG
            Puts($"PassesStrictCheck() called for {entity.ShortPrefabName}");
#endif
            Vector3 center = entity.CenterPoint();

            if (IsExternalWallOverlapped(center, position)) return false;
#if DEBUG
            Puts($"  Checking block: {entity.name} @ center {center}, pos: {position}");
#endif
            if (entity.PrefabName.Contains("triangle.prefab"))
            {
                if (Math.Abs(center.x - position.x) < 0.46f && Math.Abs(center.z - position.z) < 0.46f)
                {
#if DEBUG
                    Puts($"    Found: {entity.ShortPrefabName} @ center: {center}, pos: {position}");
#endif
                    return true;
                }
            }
            else if (entity.PrefabName.Contains("foundation.prefab") || entity.PrefabName.Contains("floor.prefab"))
            {
                if (Math.Abs(center.x - position.x) < 0.7f && Math.Abs(center.z - position.z) < 0.7f)
                {
#if DEBUG
                    Puts($"    Found: {entity.ShortPrefabName} @ center: {center}, pos: {position}");
#endif
                    return true;
                }
            }

            return false;
        }

        private bool IsExternalWallOverlapped(Vector3 center, Vector3 position)
        {
            var walls = FindEntitiesOfType<BaseEntity>(center, 1.5f);
            foreach (var wall in walls)
            {
                if (wall.PrefabName.Contains("external.high"))
                {
#if DEBUG
                    Puts($"    Found: {wall.PrefabName} @ center {center}, pos {position}");
#endif
                    walls.ResetToPool();
                    return true;
                }
            }
            walls.ResetToPool();
            return false;
        }

        private T FindEntity<T>(BaseEntity entity) where T : BaseEntity
        {
            if (entity == null)
            {
                return null;
            }
            if (entity is T val)
            {
                return val;
            }
            if (!entity.HasParent())
            {
                return null;
            }
            var parent = entity.GetParentEntity();
            while (parent != null)
            {
                if (parent is T val2)
                {
                    return val2;
                }
                parent = parent.GetParentEntity();
            }
            return null;
        }

        private T GetStandingOnEntity<T>(BasePlayer player, float distance, int layerMask) where T : BaseEntity
        {
            if (player.HasParent())
            {
                var parent = FindEntity<T>(player.GetParentEntity());
                if (parent != null)
                {
                    return parent;
                }
            }
            if (player.isMounted)
            {
                var mounted = FindEntity<T>(player.GetMounted());
                if (mounted != null)
                {
                    return mounted;
                }
            }
            return GetStandingOnEntity<T>(player.transform.position, distance, layerMask);
        }

        private T GetStandingOnEntity<T>(Vector3 a, float distance, int layerMask) where T : BaseEntity
        {
            if (Physics.Raycast(a + new Vector3(0f, 0.1f, 0f), Vector3.down, out var hit, distance, layerMask, QueryTriggerInteraction.Ignore))
            {
                var entity = hit.GetEntity();
                if (entity is T n) return n;
            }
            if (layerMask != Layers.Mask.Construction)
            {
                var ents = FindEntitiesOfType<T>(a, distance, layerMask);
                T ent = ents.Count > 0 ? ents[0] : null;
                ents.ResetToPool();
                return ent;
            }
            return null;
        }

        private bool IsStandingOnEntity(Vector3 a, float distance, int layerMask, out BaseEntity entity, string[] prefabs)
        {
            entity = GetStandingOnEntity<BaseEntity>(a, distance, layerMask);
            if (entity == null || !PassesStrictCheck(entity, a)) return false;
            return Array.Exists(prefabs, entity.ShortPrefabName.Contains);
        }

        private bool IsCeiling(DecayEntity entity)
        {
            if (entity == null || entity.ShortPrefabName.Contains("roof"))
            {
                return true;
            }
            var building = entity.GetBuilding();
            if (building == null || !building.HasBuildingBlocks())
            {
                return true;
            }
            var data = new Dictionary<double, int>();
            foreach (var block in building.buildingBlocks)
            {
                if (block.IsKilled() || (!block.ShortPrefabName.Contains("floor") && !block.ShortPrefabName.Contains("roof")))
                {
                    continue;
                }
                var j = Math.Round(block.transform.position.y, 2);
                if (data.ContainsKey(j))
                {
                    data[j]++;
                }
                else
                {
                    data[j] = 1;
                }
            }
            var k = Math.Round(entity.transform.position.y, 2);
            var s = data.OrderByDescending(pair => pair.Value).Take(2).Select(pair => (Height: pair.Key, Count: pair.Value)).ToList();
            return s.Count == 0 || k >= (s.Count > 1 && s[1].Count > s[0].Count ? s[1].Height : s[0].Height);
        }

        private bool CheckBoundaries(float x, float y, float z)
        {
            return x <= boundary && x >= -boundary && y <= config.Settings.BoundaryMax && y >= config.Settings.BoundaryMin && z <= boundary && z >= -boundary;
        }

        private Vector3 GetGroundBuilding(Vector3 a)
        {
            a.y = TerrainMeta.HeightMap.GetHeight(a);
            RaycastHit hit;
            if (Physics.Raycast(a.WithY(200f), Vector3.down, out hit, Mathf.Infinity, Layers.Mask.Terrain | Layers.Mask.World | Layers.Mask.Construction | Layers.Mask.Deployed | Layers.Mask.Vehicle_Large, QueryTriggerInteraction.Ignore))
            {
                a.y = Mathf.Max(hit.point.y, a.y);
            }
            return a;
        }

        public bool AboveWater(Vector3 a)
        {
            return TerrainMeta.HeightMap.GetHeight(a) - TerrainMeta.WaterMap.GetHeight(a) < 0;
        }

        private static bool ContainsTopology(TerrainTopology.Enum mask, Vector3 position, float radius)
        {
            return (TerrainMeta.TopologyMap.GetTopology(position, radius) & (int)mask) != 0;
        }

        private bool IsInCave(Vector3 a)
        {
            return GamePhysics.CheckSphere<TerrainCollisionTrigger>(a, 5f, 262144, QueryTriggerInteraction.Collide) && ContainsTopology(TerrainTopology.Enum.Monument, a, 5f);
        }

        private bool GetLift(Vector3 position)
        {
            var lifts = FindEntitiesOfType<ProceduralLift>(position, 0.5f);
            bool result = lifts.Count > 0;
            lifts.ResetToPool();
            return result;
        }

        private bool IsOnJunkPile(BasePlayer player)
        {
            if (player.GetParentEntity() is JunkPile) return true;
            var junkpiles = FindEntitiesOfType<JunkPile>(player.transform.position, 3f, Layers.Mask.World);
            var result = junkpiles.Count > 0;
            junkpiles.ResetToPool();
            return result;
        }

        private bool IsAllowed(BasePlayer player, string perm = null)
        {
            if (player == null || !player.IsConnected)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(perm) && permission.UserHasPermission(player.UserIDString, perm))
            {
                return !player.IsSleeping();
            }

            if (player.net.connection.authLevel == 1)
            {
                return config.Admin.UseableByModerators;
            }
            else if (player.net.connection.authLevel >= 2)
            {
                return config.Admin.UseableByAdmins;
            }

            return false;
        }

        private bool IsAllowedMsg(BasePlayer player, string perm = null)
        {
            if (IsAllowed(player, perm)) return true;
            PrintMsgL(player, "NotAllowed");
            return false;
        }

        private Effect reusableSoundEffectInstance = new Effect();

        private void SendEffect(BasePlayer player, List<string> effects)
        {
            if (effects.Count != 0)
            {
                reusableSoundEffectInstance.Init(Effect.Type.Generic, player, 0, Vector3.zero, Vector3.forward, player.limitNetworking ? player.Connection : null);
                reusableSoundEffectInstance.pooledString = effects.GetRandom();
                if (string.IsNullOrEmpty(reusableSoundEffectInstance.pooledString))
                {
                    return;
                }
                if (player.limitNetworking)
                {
                    EffectNetwork.Send(reusableSoundEffectInstance, player.Connection);
                }
                else EffectNetwork.Send(reusableSoundEffectInstance);
            }
        }

        private int GetHigher(BasePlayer player, Dictionary<string, int> limits, int limit, bool unlimited)
        {
            if (unlimited && limit == 0) return limit;

            foreach (var l in limits)
            {
                if (permission.UserHasPermission(player.UserIDString, l.Key))
                {
                    if (unlimited && l.Value == 0) return l.Value;

                    limit = Math.Max(l.Value, limit);
                }
            }
            return limit;
        }

        private int GetLower(BasePlayer player, Dictionary<string, int> times, int time)
        {
            foreach (var l in times)
            {
                if (permission.UserHasPermission(player.UserIDString, l.Key))
                {
                    time = Math.Min(l.Value, time);
                }
            }
            return time;
        }

        private void CheckPerms(Dictionary<string, int> limits)
        {
            foreach (var limit in limits)
            {
                if (!permission.PermissionExists(limit.Key))
                {
                    permission.RegisterPermission(limit.Key, this);
                }
            }
        }
        #endregion

        #region Message

        protected new static void Puts(string format, params object[] args)
        {
            if (!string.IsNullOrEmpty(format))
            {
                Interface.Oxide.LogInfo("[{0}] {1}", "NTeleportation", (args.Length != 0) ? string.Format(format, args) : format);
            }
        }

        private string _(string msgId, BasePlayer player, params object[] args)
        {
            var msg = lang.GetMessage(msgId, this, player?.UserIDString);
            return args.Length > 0 ? string.Format(msg, args) : msg;
        }

        private void PrintMsgL(IPlayer user, string msgId, params object[] args)
        {
            if (user.IsServer)
            {
                string msg = lang.GetMessage(msgId, this, user.Id);
                if (!string.IsNullOrEmpty(msg))
                {
                    user.Reply(string.Format(msg, args));
                }
            }
            else PrintMsgL(user.Object as BasePlayer, msgId, args);
        }

        private void PrintMsgL(BasePlayer player, string msgId, params object[] args)
        {
            if (player == null) return;
            PrintMsg(player, _(msgId, player, args));
        }

        private void PrintMsgL(ulong userid, string msgId, params object[] args)
        {
            var player = BasePlayer.FindAwakeOrSleeping(userid.ToString());
            if (player == null) return;
            PrintMsgL(player, msgId, args);
        }

        private void PrintMsg(BasePlayer player, string message)
        {
            if (player == null || string.IsNullOrEmpty(message)) return;
            if (config.Settings.UsePopup)
            {
                PopupNotifications?.Call("CreatePopupNotification", config.Settings.ChatName + message, player);
            }
            if (config.Settings.SendMessages)
            {
                Player.Message(player, $"{config.Settings.ChatName}{message}", config.Settings.ChatID);
            }
        }

        private void SendDiscordMessage(BasePlayer player, BasePlayer target)
        {
            if (config.TPR.Discord.TPA <= 0 || string.IsNullOrEmpty(config.TPR.Discord.Webhook) || config.TPR.Discord.Webhook == "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks")
            {
                return;
            }
            if (player.userID == target.userID || IsAlly(player.userID, target.userID, true, true, true))
            {
                return;
            }
            var key = ((ulong)player.userID).CompareTo((ulong)target.userID) < 0 ? (player.userID, target.userID) : (target.userID, player.userID);
            if (teleportCounts.ContainsKey(key))
            {
                teleportCounts[key]++;
            }
            else
            {
                teleportCounts[key] = 1;
            }
            if (teleportCounts[key] >= config.TPR.Discord.TPA)
            {
                string message = _("DiscordLogTPA", null, $"{target.displayName} ({target.userID})", $"{player.displayName} ({player.userID})", teleportCounts[key]);
                discordMessages.Add(message);
                if (discordMessages.Count == 1)
                {
                    timer.Once(1f, CheckDiscordMessages);
                }
            }
        }

        private void CheckDiscordMessages()
        {
            string text = discordMessages[0];
            discordMessages.RemoveAt(0);
            Puts(text);
            if (discordMessages.Count > 0) timer.Once(1f, CheckDiscordMessages);
            try
            {
                var headers = new Dictionary<string, string>() { { "Content-Type", "application/json" } };
                var body = new DiscordMessage(text).ToJson();
                webrequest.Enqueue(config.TPR.Discord.Webhook, body, (code, response) => { }, this, Core.Libraries.RequestMethod.POST, headers);
            }
            catch { }
        }

        private List<string> discordMessages = new();
        private Dictionary<(ulong, ulong), int> teleportCounts = new();

        public class DiscordMessage
        {
            public DiscordMessage(string content)
            {
                Content = content;
            }

            [JsonProperty("content")]
            public string Content { get; set; }

            public string ToJson() => JsonConvert.SerializeObject(this);
        }

        #endregion

        #region DrawMonument
        void DrawMonument(BasePlayer player, Vector3 center, Vector3 extents, Quaternion rotation, Color color, float duration)
        {
            Vector3[] boxVertices = new Vector3[8]
            {
                center + rotation * new Vector3(extents.x, extents.y, extents.z),
                center + rotation * new Vector3(-extents.x, extents.y, extents.z),
                center + rotation * new Vector3(extents.x, -extents.y, extents.z),
                center + rotation * new Vector3(-extents.x, -extents.y, extents.z),
                center + rotation * new Vector3(extents.x, extents.y, -extents.z),
                center + rotation * new Vector3(-extents.x, extents.y, -extents.z),
                center + rotation * new Vector3(extents.x, -extents.y, -extents.z),
                center + rotation * new Vector3(-extents.x, -extents.y, -extents.z)
            };

            //foreach (var vector in boxVertices)
            //{
            //    DrawText(player, 30f, Color.red, vector, "X");
            //}

            DrawLine(player, duration, color, boxVertices[0], boxVertices[1]);
            DrawLine(player, duration, color, boxVertices[0], boxVertices[1]);
            DrawLine(player, duration, color, boxVertices[1], boxVertices[3]);
            DrawLine(player, duration, color, boxVertices[3], boxVertices[2]);
            DrawLine(player, duration, color, boxVertices[2], boxVertices[0]);
            DrawLine(player, duration, color, boxVertices[4], boxVertices[5]);
            DrawLine(player, duration, color, boxVertices[5], boxVertices[7]);
            DrawLine(player, duration, color, boxVertices[7], boxVertices[6]);
            DrawLine(player, duration, color, boxVertices[6], boxVertices[4]);
            DrawLine(player, duration, color, boxVertices[0], boxVertices[4]);
            DrawLine(player, duration, color, boxVertices[1], boxVertices[5]);
            DrawLine(player, duration, color, boxVertices[2], boxVertices[6]);
            DrawLine(player, duration, color, boxVertices[3], boxVertices[7]);
        }
        #endregion DrawMonument

        #region FindPlayer
        private ulong FindPlayersSingleId(string nameOrIdOrIp, BasePlayer player)
        {
            var targets = FindPlayers(nameOrIdOrIp, true);
            if (targets.Count > 1)
            {
                PrintMsgL(player, "MultiplePlayers", GetMultiplePlayers(targets));
                return 0;
            }
            ulong userId;
            if (targets.Count <= 0)
            {
                if (ulong.TryParse(nameOrIdOrIp, out userId)) return userId;
                PrintMsgL(player, "PlayerNotFound");
                return 0;
            }
            else
                userId = targets[0].userID;

            return userId;
        }

        private BasePlayer FindPlayersSingle(string value, BasePlayer player)
        {
            if (string.IsNullOrEmpty(value)) return null;
            BasePlayer target;
            if (_codeToPlayer.TryGetValue(value, out target) && target.IsValid())
            {
                return target;
            }
            var targets = FindPlayers(value, true);
            if (targets.Count <= 0)
            {
                PrintMsgL(player, "PlayerNotFound");
                return null;
            }
            if (targets.Count > 1)
            {
                PrintMsgL(player, "MultiplePlayers", GetMultiplePlayers(targets));
                return null;
            }

            return targets[0];
        }

        private List<BasePlayer> FindPlayers(string arg, bool all = false)
        {
            var players = new List<BasePlayer>();

            if (string.IsNullOrEmpty(arg))
            {
                return players;
            }

            if (_codeToPlayer.TryGetValue(arg, out var player) && player.IsValid())
            {
                if (all || player.IsConnected)
                {
                    players.Add(player);
                    return players;
                }
            }

            foreach (var target in all ? BasePlayer.allPlayerList : BasePlayer.activePlayerList)
            {
                if (target == null || string.IsNullOrEmpty(target.displayName) || players.Contains(target))
                {
                    continue;
                }

                if (target.UserIDString == arg || target.displayName.Contains(arg, CompareOptions.OrdinalIgnoreCase))
                {
                    players.Add(target);
                }
            }

            return players;
        }
        #endregion



        private bool API_HavePendingRequest(BasePlayer player)
        {
            return PendingRequests.ContainsKey(player.userID) || PlayersRequests.ContainsKey(player.userID) || TeleportTimers.ContainsKey(player.userID);
        }

        private bool API_HaveAvailableHomes(BasePlayer player)
        {
            if (!_Home.TryGetValue(player.userID, out var homeData))
            {
                _Home[player.userID] = homeData = new();
            }
            ValidateHomes(player, homeData, false, false);
            var limit = GetHigher(player, config.Home.VIPHomesLimits, config.Home.HomesLimit, true);
            var result = homeData.Locations.Count < limit || limit == 0;
            homeData.Locations.Clear();
            return result;
        }

        [HookMethod("API_GetHomes")]
        public Dictionary<string, Vector3> GetPlayerHomes(BasePlayer player)
        {
            if (!_Home.TryGetValue(player.userID, out var homeData))
            {
                _Home[player.userID] = homeData = new();
            }
            ValidateHomes(player, homeData, false, false);
            var result = homeData.Locations.ToDictionary(pair => pair.Key, pair => pair.Value.Get());
            homeData.Locations.Clear();
            return result;
        }

        private List<Vector3> API_GetLocations(string command)
        {
            return GetSettings(command)?.Locations ?? new();
        }

        private Dictionary<string, List<Vector3>> API_GetAllLocations()
        {
            return config.DynamicCommands.ToDictionary(pair => pair.Key, pair => pair.Value.Locations);
        }

        private int GetLimitRemaining(BasePlayer player, string type)
        {
            if (player == null || string.IsNullOrEmpty(type)) return -1;
            var settings = GetSettings(type, player.userID);
            if (settings == null) return -1;
            var currentDate = DateTime.Now.ToString("d");
            var limit = GetHigher(player, settings.VIPDailyLimits, settings.DailyLimit, true);
            if (!settings.Teleports.TPData.TryGetValue(player.userID, out var data))
            {
                settings.Teleports.TPData[player.userID] = data = new();
            }
            if (data.Date != currentDate)
            {
                data.Amount = 0;
                data.Date = currentDate;
            }
            return limit > 0 ? limit - data.Amount : 0;
        }

        private int GetCooldownRemaining(BasePlayer player, string type)
        {
            if (player == null || string.IsNullOrEmpty(type)) return -1;
            var settings = GetSettings(type, player.userID);
            if (settings == null) return -1;
            var currentDate = DateTime.Now.ToString("d");
            var timestamp = Facepunch.Math.Epoch.Current;
            var cooldown = GetLower(player, settings.VIPCooldowns, settings.Cooldown);
            if (!settings.Teleports.TPData.TryGetValue(player.userID, out var data))
            {
                settings.Teleports.TPData[player.userID] = data = new();
            }
            if (data.Date != currentDate)
            {
                data.Amount = 0;
                data.Date = currentDate;
            }
            return cooldown > 0 && timestamp - data.Timestamp < cooldown ? cooldown - (timestamp - data.Timestamp) : 0;
        }

        private int GetCountdownRemaining(BasePlayer player, string type)
        {
            if (player == null || string.IsNullOrEmpty(type))
            {
                return -1;
            }
            var settings = GetSettings(type, player.userID);
            return settings == null ? -1 : GetLower(player, settings.VIPCountdowns, settings.Countdown);
        }

        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                var o = Newtonsoft.Json.Linq.JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        }

        private class CustomComparerDictionaryCreationConverter<T> : CustomCreationConverter<IDictionary>
        {
            private readonly IEqualityComparer<T> comparer;

            public CustomComparerDictionaryCreationConverter(IEqualityComparer<T> comparer)
            {
                if (comparer == null)
                    throw new ArgumentNullException(nameof(comparer));
                this.comparer = comparer;
            }

            public override bool CanConvert(Type objectType)
            {
                return HasCompatibleInterface(objectType) && HasCompatibleConstructor(objectType);
            }

            private static bool HasCompatibleInterface(Type objectType)
            {
                return objectType.GetInterfaces().Where(i => HasGenericTypeDefinition(i, typeof(IDictionary<,>))).Exists(i => typeof(T).IsAssignableFrom(i.GetGenericArguments()[0]));
            }

            private static bool HasGenericTypeDefinition(Type objectType, Type typeDefinition)
            {
                return objectType.GetTypeInfo().IsGenericType && objectType.GetGenericTypeDefinition() == typeDefinition;
            }

            private static bool HasCompatibleConstructor(Type objectType)
            {
                return objectType.GetConstructor(new[] { typeof(IEqualityComparer<T>) }) != null;
            }

            public override IDictionary Create(Type objectType)
            {
                return Activator.CreateInstance(objectType, comparer) as IDictionary;
            }
        }

        public class Exploits
        {
            public static bool TestInsideRock(Vector3 a)
            {
                if (ContainsTopology(TerrainTopology.Enum.Monument, a, 25f))
                {
                    return false;
                }
                bool faces = Physics.queriesHitBackfaces;
                Physics.queriesHitBackfaces = true;
                bool flag = IsRockFaceUpwards(a);
                Physics.queriesHitBackfaces = faces;
                return flag || IsRockFaceDownwards(a);
            }

            private static bool IsRockFaceDownwards(Vector3 a)
            {
                Vector3 b = a + new Vector3(0f, 30f, 0f);
                Vector3 d = a - b;
                var hits = Physics.RaycastAll(b, d, d.magnitude, Layers.World);
                return Array.Exists(hits, hit => IsRock(hit.collider.name));
            }

            private static bool IsRockFaceUpwards(Vector3 point)
            {
                return Physics.Raycast(point, Vector3.up, out var hit, 30f, Layers.Mask.World) && IsRock(hit.collider.name);
            }

            private static bool IsRock(string name)
            {
                if (name.Contains("rock_formation_huge", CompareOptions.OrdinalIgnoreCase)) return false;
                return name.Contains("rock", CompareOptions.OrdinalIgnoreCase) || name.Contains("formation", CompareOptions.OrdinalIgnoreCase) || name.Contains("cliff", CompareOptions.OrdinalIgnoreCase);
            }
        }

        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
            PrintMsgL(player, "<size=14>NTeleportation</size> by <color=#ce422b>Nogrod</color>\n<color=#ffd479>/sethome NAME</color> - Set home on current foundation\n<color=#ffd479>/home NAME</color> - Go to one of your homes\n<color=#ffd479>/home list</color> - List your homes\n<color=#ffd479>/town</color> - Go to town, if set\n/tpb - Go back to previous location\n/tpr PLAYER - Request teleport to PLAYER\n/tpa - Accept teleport request");
        }
    }
}

namespace Oxide.Plugins.NTeleportationExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool All<T>(this IEnumerable<T> a, Func<T, bool> b) { foreach (T c in a) { if (!b(c)) { return false; } } return true; }
        public static T FirstOrDefault<T>(this IEnumerable<T> a, Func<T, bool> b = null) { using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (b == null || b(c.Current)) { return c.Current; } } } return default(T); }
        public static IEnumerable<V> Select<T, V>(this IEnumerable<T> a, Func<T, V> b) { var c = new List<V>(); using (var d = a.GetEnumerator()) { while (d.MoveNext()) { c.Add(b(d.Current)); } } return c; }
        public static string[] Skip(this string[] a, int b) { if (a.Length == 0 || b >= a.Length) { return Array.Empty<string>(); } int n = a.Length - b; string[] c = new string[n]; Array.Copy(a, b, c, 0, n); return c; }
        public static List<T> Take<T>(this IList<T> a, int b) { var c = new List<T>(); for (int i = 0; i < a.Count; i++) { if (c.Count == b) { break; } c.Add(a[i]); } return c; }
        public static Dictionary<T, V> ToDictionary<S, T, V>(this IEnumerable<S> a, Func<S, T> b, Func<S, V> c) { var d = new Dictionary<T, V>(); using (var e = a.GetEnumerator()) { while (e.MoveNext()) { d[b(e.Current)] = c(e.Current); } } return d; }
        public static List<T> ToList<T>(this IEnumerable<T> a) => new(a);
        public static List<T> Where<T>(this IEnumerable<T> a, Func<T, bool> b) { List<T> c = new(a is ICollection<T> n ? n.Count : 4); foreach (var d in a) { if (b(d)) { c.Add(d); } } return c; }
        public static List<T> OrderByDescending<T, TKey>(this IEnumerable<T> a, Func<T, TKey> s) { List<T> m = new(a); m.Sort((x, y) => Comparer<TKey>.Default.Compare(s(y), s(x))); return m; }
        public static int Count<T>(this IEnumerable<T> a, Func<T, bool> b) { int c = 0; foreach (T d in a) { if (b(d)) { c++; } } return c; }
        public static bool IsKilled(this BaseNetworkable a) => a == null || a.IsDestroyed || !a.IsFullySpawned();
        public static void ResetToPool<T>(this List<T> obj) { if (obj == null) return; obj.Clear(); Pool.FreeUnmanaged(ref obj); }
    }
}
