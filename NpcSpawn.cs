using System;
using Facepunch;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using Rust;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Oxide.Core;
using UnityEngine;
using UnityEngine.AI;
using System.IO;
using ProtoBuf;
using Newtonsoft.Json;
using Rust.Ai;
using Oxide.Plugins.NpcSpawnExtensionMethods;

namespace Oxide.Plugins
{
    [Info("NpcSpawn", "rustmods.ru", "2.8.0")]
    internal class NpcSpawn : RustPlugin
    {
        #region Config
        private const bool En = false;

        private PluginConfig _config;

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a default config...");
            _config = PluginConfig.DefaultConfig();
            _config.PluginVersion = Version;
            SaveConfig();
            Puts("Creation of the default config completed!");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
            if (_config.PluginVersion < Version) UpdateConfigValues();
        }

        private void UpdateConfigValues()
        {
            Puts("Config update detected! Updating config values...");
            _config.PluginVersion = Version;
            Puts("Config update completed!");
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        private class PluginConfig
        {
            [JsonProperty(En ? "Can NpcSpawn NPCs attack animals? [true/false]" : "Могут ли кастомные NPC атаковать животных? [true/false]")] public bool CanTargetAnimal { get; set; }
            [JsonProperty(En ? "Can NpcSpawn NPCs attack other NPCs? [true/false]" : "Могут ли кастомные NPC атаковать других NPC? [true/false]")] public bool CanTargetNpc { get; set; }
            [JsonProperty(En ? "Can NpcSpawn NPCs attack sleeping players? [true/false]" : "Могут ли кастомные NPC атаковать спящих игроков? [true/false]")] public bool CanTargetSleepingPlayer { get; set; }
            [JsonProperty(En ? "Can NpcSpawn NPCs attack wounded players? [true/false]" : "Могут ли кастомные NPC атаковать игроков в состоянии Wounded? [true/false]")] public bool CanTargetWoundedPlayer { get; set; }
            [JsonProperty(En ? "Can NpcSpawn NPCs attack players in SafeZone? [true/false]" : "Могут ли кастомные NPC атаковать игроков в SafeZone? [true/false]")] public bool CanTargetSafeZonePlayer { get; set; }
            [JsonProperty(En ? "Prefab path used for all NpcSpawn NPCs" : "Используемый prefab для кастомных NPC")] public string Prefab { get; set; }
            [JsonProperty(En ? "Configuration version" : "Версия конфигурации")] public VersionNumber PluginVersion { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig
                {
                    CanTargetAnimal = false,
                    CanTargetNpc = false,
                    CanTargetSleepingPlayer = false,
                    CanTargetWoundedPlayer = false,
                    CanTargetSafeZonePlayer = false,
                    Prefab = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab",
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion Config

        #region NpcConfig
        internal class NpcBelt { public string ShortName; public int Amount; public ulong SkinID; public HashSet<string> Mods; public string Ammo; }

        internal class NpcWear { public string ShortName; public ulong SkinID; }

        internal class NpcConfig
        {
            public string Name { get; set; }
            public HashSet<NpcWear> WearItems { get; set; }
            public HashSet<NpcBelt> BeltItems { get; set; }
            public string Kit { get; set; }
            public float Health { get; set; }
            public float RoamRange { get; set; }
            public float ChaseRange { get; set; }
            public float SenseRange { get; set; }
            public float ListenRange { get; set; }
            public float AttackRangeMultiplier { get; set; }
            public bool CheckVisionCone { get; set; }
            public float VisionCone { get; set; }
            public bool HostileTargetsOnly { get; set; }
            public float DamageScale { get; set; }
            public float TurretDamageScale { get; set; }
            public float AimConeScale { get; set; }
            public bool DisableRadio { get; set; }
            public bool CanRunAwayWater { get; set; }
            public bool CanSleep { get; set; }
            public float SleepDistance { get; set; }
            public float Speed { get; set; }
            public int AreaMask { get; set; }
            public int AgentTypeID { get; set; }
            public string HomePosition { get; set; }
            public float MemoryDuration { get; set; }
            public HashSet<string> States { get; set; }
        }
        #endregion NpcConfig

        #region Methods
        private static bool IsCustomScientist(BaseEntity entity) => entity != null && entity.skinID == 11162132011012;

        private ScientistNPC SpawnNpc(Vector3 position, JObject configJson)
        {
            CustomScientistNpc npc = CreateCustomNpc(position, configJson.ToObject<NpcConfig>());
            if (npc == null) return null;
            npc.skinID = 11162132011012;
            Scientists.Add(npc.net.ID.Value, npc);
            return npc;
        }

        private CustomScientistNpc CreateCustomNpc(Vector3 position, NpcConfig config)
        {
            ScientistNPC scientistNpc = GameManager.server.CreateEntity(_config.Prefab, position, Quaternion.identity, false) as ScientistNPC;
            ScientistBrain scientistBrain = scientistNpc.GetComponent<ScientistBrain>();

            CustomScientistNpc customScientist = scientistNpc.gameObject.AddComponent<CustomScientistNpc>();
            CustomScientistBrain customScientistBrain = scientistNpc.gameObject.AddComponent<CustomScientistBrain>();

            CopySerializableFields(scientistNpc, customScientist);
            CopySerializableFields(scientistBrain, customScientistBrain);

            UnityEngine.Object.DestroyImmediate(scientistNpc, true);
            UnityEngine.Object.DestroyImmediate(scientistBrain, true);

            customScientist.Config = config;
            customScientist.Brain = customScientistBrain;
            customScientist.enableSaving = false;
            customScientist.gameObject.AwakeFromInstantiate();
            customScientist.Spawn();

            return customScientist;
        }

        private static void CopySerializableFields<T>(T src, T dst)
        {
            FieldInfo[] srcFields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in srcFields)
            {
                object value = field.GetValue(src);
                field.SetValue(dst, value);
            }
        }

        private void AddTargetRaid(CustomScientistNpc npc, HashSet<BuildingBlock> foundations) { if (IsCustomScientist(npc)) npc.Foundations = foundations; }

        private void AddTargetGuard(CustomScientistNpc npc, BaseEntity target)
        {
            if (!IsCustomScientist(npc) || target == null) return;
            if (npc.GuardTarget != null) return;
            npc.AddTargetGuard(target);
        }

        private void SetParentEntity(CustomScientistNpc npc, BaseEntity parent, Vector3 pos) { if (IsCustomScientist(npc) && parent != null) npc.SetParentEntity(parent, pos); }

        private void SetHomePosition(CustomScientistNpc npc, Vector3 pos) { if (IsCustomScientist(npc)) npc.HomePosition = pos; }

        private void SetCurrentWeapon(CustomScientistNpc npc, Item weapon) { if (IsCustomScientist(npc)) npc.EquipCurrentWeapon(weapon); }

        private void SetCustomNavMesh(CustomScientistNpc npc, Transform transform, string navMeshName)
        {
            if (!IsCustomScientist(npc) || transform == null || !AllNavMeshes.ContainsKey(navMeshName)) return;
            Dictionary<int, Dictionary<int, PointNavMeshFile>> navMesh = AllNavMeshes[navMeshName];
            for (int i = 0; i < navMesh.Count; i++)
            {
                if (!npc.CustomNavMesh.ContainsKey(i)) npc.CustomNavMesh.Add(i, new Dictionary<int, PointNavMesh>());
                for (int j = 0; j < navMesh[i].Count; j++)
                {
                    PointNavMeshFile pointNavMesh = navMesh[i][j];
                    npc.CustomNavMesh[i].Add(j, new PointNavMesh { Position = transform.TransformPoint(pointNavMesh.Position.ToVector3()), Enabled = pointNavMesh.Enabled });
                }
            }
            npc.InitCustomNavMesh();
        }

        private BasePlayer GetCurrentTarget(CustomScientistNpc npc) => IsCustomScientist(npc) && npc.IsBasePlayerTarget ? npc.GetBasePlayerTarget : null;

        private void AddStates(CustomScientistNpc npc, HashSet<string> states)
        {
            if (states.Contains("RoamState")) npc.Brain.AddState(new CustomScientistBrain.RoamState(npc));
            if (states.Contains("ChaseState")) npc.Brain.AddState(new CustomScientistBrain.ChaseState(npc));
            if (states.Contains("CombatState")) npc.Brain.AddState(new CustomScientistBrain.CombatState(npc));
            if (states.Contains("CombatStationaryState")) npc.Brain.AddState(new CustomScientistBrain.CombatStationaryState(npc));
            if (states.Contains("RaidState")) npc.Brain.AddState(new CustomScientistBrain.RaidState(npc));
            if (states.Contains("RaidStateMelee")) npc.Brain.AddState(new CustomScientistBrain.RaidStateMelee(npc));
            if (states.Contains("SledgeState")) npc.Brain.AddState(new CustomScientistBrain.SledgeState(npc));
            if (states.Contains("BlazerState")) npc.Brain.AddState(new CustomScientistBrain.BlazerState(npc));
        }
        #endregion Methods

        #region Controller
        internal class DefaultSettings { public float EffectiveRange; public float AttackLengthMin; public float AttackLengthMax; }

        private static readonly Dictionary<string, DefaultSettings> Weapons = new Dictionary<string, DefaultSettings>
        {
            ["rifle.bolt"] = new DefaultSettings { EffectiveRange = 150f, AttackLengthMin = -1f, AttackLengthMax = -1f },
            ["speargun"] = new DefaultSettings { EffectiveRange = 15f, AttackLengthMin = -1f, AttackLengthMax = -1f },
            ["bow.compound"] = new DefaultSettings { EffectiveRange = 15f, AttackLengthMin = -1f, AttackLengthMax = -1f },
            ["crossbow"] = new DefaultSettings { EffectiveRange = 15f, AttackLengthMin = -1f, AttackLengthMax = -1f },
            ["bow.hunting"] = new DefaultSettings { EffectiveRange = 15f, AttackLengthMin = -1f, AttackLengthMax = -1f },
            ["smg.2"] = new DefaultSettings { EffectiveRange = 20f, AttackLengthMin = 0.4f, AttackLengthMax = 0.4f },
            ["shotgun.double"] = new DefaultSettings { EffectiveRange = 10f, AttackLengthMin = 0.3f, AttackLengthMax = 1f },
            ["pistol.eoka"] = new DefaultSettings { EffectiveRange = 10f, AttackLengthMin = -1f, AttackLengthMax = -1f },
            ["rifle.l96"] = new DefaultSettings { EffectiveRange = 150f, AttackLengthMin = -1f, AttackLengthMax = -1f },
            ["pistol.nailgun"] = new DefaultSettings { EffectiveRange = 15f, AttackLengthMin = 0f, AttackLengthMax = 0.46f },
            ["pistol.python"] = new DefaultSettings { EffectiveRange = 15f, AttackLengthMin = 0.175f, AttackLengthMax = 0.525f },
            ["pistol.semiauto"] = new DefaultSettings { EffectiveRange = 15f, AttackLengthMin = 0f, AttackLengthMax = 0.46f },
            ["pistol.prototype17"] = new DefaultSettings { EffectiveRange = 15f, AttackLengthMin = 0f, AttackLengthMax = 0.46f },
            ["smg.thompson"] = new DefaultSettings { EffectiveRange = 20f, AttackLengthMin = 0.4f, AttackLengthMax = 0.4f },
            ["shotgun.waterpipe"] = new DefaultSettings { EffectiveRange = 10f, AttackLengthMin = -1f, AttackLengthMax = -1f },
            ["multiplegrenadelauncher"] = new DefaultSettings { EffectiveRange = 20f, AttackLengthMin = -1f, AttackLengthMax = -1f },
            ["snowballgun"] = new DefaultSettings { EffectiveRange = 5f, AttackLengthMin = 2f, AttackLengthMax = 2f },
            ["legacy bow"] = new DefaultSettings { EffectiveRange = 15f, AttackLengthMin = -1f, AttackLengthMax = -1f },
            ["blunderbuss"] = new DefaultSettings { EffectiveRange = 10f, AttackLengthMin = 0.3f, AttackLengthMax = 1f },
            ["revolver.hc"] = new DefaultSettings { EffectiveRange = 20f, AttackLengthMin = 0.4f, AttackLengthMax = 0.4f },
            ["t1_smg"] = new DefaultSettings { EffectiveRange = 20f, AttackLengthMin = 0.4f, AttackLengthMax = 0.4f },
            ["minicrossbow"] = new DefaultSettings { EffectiveRange = 15f, AttackLengthMin = -1f, AttackLengthMax = -1f }
        };

        private static readonly HashSet<string> MeleeWeapons = new HashSet<string>
        {
            "bone.club",
            "knife.bone",
            "knife.butcher",
            "candycaneclub",
            "knife.combat",
            "longsword",
            "mace",
            "machete",
            "paddle",
            "pitchfork",
            "salvaged.cleaver",
            "salvaged.sword",
            "spear.stone",
            "spear.wooden",
            "chainsaw",
            "hatchet",
            "jackhammer",
            "pickaxe",
            "axe.salvaged",
            "hammer.salvaged",
            "icepick.salvaged",
            "stonehatchet",
            "stone.pickaxe",
            "torch",
            "sickle",
            "rock",
            "snowball",
            "mace.baseballbat",
            "concretepickaxe",
            "concretehatchet",
            "lumberjack.hatchet",
            "lumberjack.pickaxe",
            "diverhatchet",
            "diverpickaxe",
            "knife.skinning",
            "vampire.stake",
            "shovel",
            "spear.cny",
            "frontier_hatchet"
        };

        private static readonly HashSet<string> FirstDistanceWeapons = new HashSet<string>
        {
            "speargun",
            "bow.compound",
            "crossbow",
            "bow.hunting",
            "shotgun.double",
            "pistol.eoka",
            "flamethrower",
            "pistol.m92",
            "pistol.nailgun",
            "multiplegrenadelauncher",
            "shotgun.pump",
            "pistol.python",
            "pistol.revolver",
            "pistol.semiauto",
            "pistol.prototype17",
            "snowballgun",
            "shotgun.spas12",
            "shotgun.waterpipe",
            "shotgun.m4",
            "legacy bow",
            "military flamethrower",
            "blunderbuss",
            "minicrossbow"
        };

        private static readonly HashSet<string> SecondDistanceWeapons = new HashSet<string>
        {
            "smg.2",
            "smg.mp5",
            "rifle.semiauto",
            "smg.thompson",
            "rifle.sks",
            "revolver.hc",
            "t1_smg"
        };

        private static readonly HashSet<string> ThirdDistanceWeapons = new HashSet<string>
        {
            "rifle.ak",
            "rifle.lr300",
            "lmg.m249",
            "rifle.m39",
            "hmlmg",
            "rifle.ak.ice",
            "rifle.ak.diver",
            "minigun",
            "rifle.ak.med"
        };

        private static readonly HashSet<string> FourthDistanceWeapons = new HashSet<string>
        {
            "rifle.bolt",
            "rifle.l96"
        };

        public class CustomScientistNpc : ScientistNPC
        {
            public NpcConfig Config { get; set; } = null;

            public Vector3 HomePosition { get; set; } = Vector3.zero;

            public float DistanceFromBase => Vector3.Distance(transform.position, HomePosition);

            public override void ServerInit()
            {
                base.ServerInit();

                HomePosition = string.IsNullOrEmpty(Config.HomePosition) ? transform.position : Config.HomePosition.ToVector3();

                if (NavAgent == null) NavAgent = GetComponent<NavMeshAgent>();
                if (NavAgent != null)
                {
                    NavAgent.areaMask = Config.AreaMask;
                    NavAgent.agentTypeID = Config.AgentTypeID;
                }

                startHealth = Config.Health;
                _health = Config.Health;

                damageScale = Config.DamageScale;

                if (Config.DisableRadio)
                {
                    CancelInvoke(PlayRadioChatter);
                    RadioChatterEffects = Array.Empty<GameObjectRef>();
                    DeathEffects = Array.Empty<GameObjectRef>();
                }

                inventory.containerWear.ClearItemsContainer();
                inventory.containerBelt.ClearItemsContainer();
                if (!string.IsNullOrEmpty(Config.Kit) && _ins.Kits != null) _ins.Kits.Call("GiveKit", this, Config.Kit);
                else UpdateInventory();

                if (IsBomber) SpawnTimedExplosive();

                InvokeRepeating(LightCheck, 1f, 30f);
                InvokeRepeating(UpdateTick, 1f, 2f);
            }

            private void UpdateInventory()
            {
                if (Config.WearItems.Count > 0)
                {
                    foreach (NpcWear wear in Config.WearItems)
                    {
                        Item item = ItemManager.CreateByName(wear.ShortName, 1, wear.SkinID);
                        if (item == null) continue;
                        if (!item.MoveToContainer(inventory.containerWear)) item.Remove();
                    }
                }
                if (Config.BeltItems.Count > 0)
                {
                    foreach (NpcBelt belt in Config.BeltItems)
                    {
                        Item item = ItemManager.CreateByName(belt.ShortName, belt.Amount, belt.SkinID);
                        if (item == null) continue;
                        if (item.MoveToContainer(inventory.containerBelt))
                        {
                            foreach (string shortname in belt.Mods)
                            {
                                ItemDefinition mod = ItemManager.FindItemDefinition(shortname);
                                if (mod == null) continue;
                                item.contents.AddItem(mod, 1);
                            }
                        }
                        else item.Remove();
                    }
                }
            }

            private void OnDestroy()
            {
                if (HealCoroutine != null) ServerMgr.Instance.StopCoroutine(HealCoroutine);
                if (FireC4Coroutine != null) ServerMgr.Instance.StopCoroutine(FireC4Coroutine);
                if (FireRocketLauncherCoroutine != null) ServerMgr.Instance.StopCoroutine(FireRocketLauncherCoroutine);
                CancelInvoke();
                if (BomberTimedExplosive.IsExists()) BomberTimedExplosive.Kill();
            }

            private void UpdateTick()
            {
                if (CanRunAwayWater()) RunAwayWater();
                if (CanThrownGrenade()) ThrownGrenade(CurrentTarget.transform.position);
                if (CanHeal()) HealCoroutine = ServerMgr.Instance.StartCoroutine(Heal());
                EquipWeapon();
                TryRaidWithoutFoundations();
                UpdateGuardPosition();
                UpdateSleep();
            }

            #region Targeting
            public new BaseEntity GetBestTarget()
            {
                BaseEntity target = null;
                float single = float.MinValue;
                foreach (SimpleAIMemory.SeenInfo info in Brain.Senses.Memory.All)
                {
                    BaseEntity entity = info.Entity;
                    if (entity != CurrentTarget && !CanSeeTarget(entity)) continue;
                    if (!CanTargetEntity(entity)) continue;
                    float single2 = GetSingle2(entity);
                    if (single2 <= single) continue;
                    target = entity;
                    single = single2;
                }
                return target;
            }

            private float GetSingle2(BaseEntity entity)
            {
                float single2 = 1f - Mathf.InverseLerp(1f, Brain.SenseRange, Vector3.Distance(entity.transform.position, transform.position));
                single2 += Mathf.InverseLerp(Brain.VisionCone, 1f, Vector3.Dot((entity.transform.position - eyes.position).normalized, eyes.BodyForward())) / 2f;
                single2 += Brain.Senses.Memory.IsLOS(entity) ? 2f : 0f;
                return single2;
            }

            internal bool CanTargetEntity(BaseEntity target)
            {
                if (target == null || target.Health() <= 0f) return false;
                if (target is BasePlayer)
                {
                    BasePlayer basePlayer = target as BasePlayer;
                    if (basePlayer.IsDead()) return false;
                    object hook = Interface.CallHook("OnCustomNpcTarget", this, basePlayer);
                    if (hook is bool) return (bool)hook;
                    if (basePlayer.userID.IsSteamId()) return CanTargetPlayer(basePlayer);
                    if (basePlayer.skinID != 0 && _ins.SkinIDs.Contains(basePlayer.skinID)) return true;
                    if (basePlayer is NPCPlayer) return CanTargetNpcPlayer(basePlayer as NPCPlayer);
                    return false;
                }
                if (target is BaseAnimalNPC) return CanTargetAnimal(target as BaseAnimalNPC);
                if (target is Drone) return CanTargetDrone(target as Drone);
                return false;
            }

            internal bool CanTargetNpcPlayer(NPCPlayer target)
            {
                if (target is FrankensteinPet) return true;
                if (target.skinID == 11162132011012) return false;
                return _ins._config.CanTargetNpc;
            }

            internal bool CanTargetPlayer(BasePlayer target)
            {
                if (target._limitedNetworking) return false;
                if (!_ins._config.CanTargetSleepingPlayer && target.IsSleeping()) return false;
                if (!_ins._config.CanTargetWoundedPlayer && target.IsWounded()) return false;
                if (!_ins._config.CanTargetSafeZonePlayer && target.InSafeZone()) return false;
                return true;
            }

            internal bool CanTargetAnimal(BaseAnimalNPC animal)
            {
                if (animal.IsDead()) return false;
                if (animal.skinID == 11491311214163) return false;
                if (Vector3.Distance(transform.position, animal.transform.position) > 30f) return false;
                return _ins._config.CanTargetAnimal;
            }

            private bool CanTargetDrone(Drone target) => !(CurrentWeapon is BaseMelee);

            public BaseEntity CurrentTarget { get; set; }

            public float DistanceToTarget => Vector3.Distance(transform.position, CurrentTarget.transform.position);

            public bool IsBasePlayerTarget => CurrentTarget is BasePlayer;

            internal BasePlayer GetBasePlayerTarget => CurrentTarget as BasePlayer;

            internal void SetKnown(BaseEntity entity)
            {
                for (int i = 0; i < Brain.Senses.Memory.All.Count; i++)
                {
                    SimpleAIMemory.SeenInfo info = Brain.Senses.Memory.All[i];
                    if (info.Entity != entity) continue;
                    info.Position = entity.transform.position;
                    info.Timestamp = Time.realtimeSinceStartup;
                    return;
                }
                Brain.Senses.Memory.All.Add(new SimpleAIMemory.SeenInfo { Entity = entity, Position = entity.transform.position, Timestamp = Time.realtimeSinceStartup });
            }
            #endregion Targeting

            #region Visible
            private int VisibleLayerMask { get; } = 1218519041;

            internal new bool CanSeeTarget(BaseEntity target)
            {
                if (target == null) return false;
                Vector3 main = isMounted ? eyes.worldMountedPosition : IsDucked() ? eyes.worldCrouchedPosition : IsCrawling() ? eyes.worldCrawlingPosition : eyes.worldStandingPosition;
                if (target is BasePlayer)
                {
                    BasePlayer targetBp = target as BasePlayer;
                    if (!targetBp.IsVisibleSpecificLayers(main, targetBp.CenterPoint(), VisibleLayerMask) && !targetBp.IsVisibleSpecificLayers(main, targetBp.transform.position, VisibleLayerMask) && !targetBp.IsVisibleSpecificLayers(main, targetBp.eyes.position, VisibleLayerMask)) return false;
                    if (!IsVisibleSpecificLayers(targetBp.CenterPoint(), main, VisibleLayerMask) && !IsVisibleSpecificLayers(targetBp.transform.position, main, VisibleLayerMask) && !IsVisibleSpecificLayers(targetBp.eyes.position, main, VisibleLayerMask)) return false;
                }
                else
                {
                    if (!target.IsVisibleSpecificLayers(main, target.CenterPoint(), VisibleLayerMask) && !target.IsVisibleSpecificLayers(main, target.transform.position, VisibleLayerMask)) return false;
                    if (!IsVisibleSpecificLayers(target.CenterPoint(), main, VisibleLayerMask) && !IsVisibleSpecificLayers(target.transform.position, main, VisibleLayerMask)) return false;
                }
                return true;
            }
            #endregion Visible

            #region Equip Weapons
            public AttackEntity CurrentWeapon { get; set; }
            private bool IsEquipping { get; set; } = false;

            private bool CanEquipWeapon()
            {
                if (inventory == null || inventory.containerBelt == null) return false;
                if (IsEquipping) return false;
                if (IsFireRocketLauncher) return false;
                if (IsHealing) return false;
                return true;
            }

            public override void EquipWeapon(bool skipDeployDelay = false)
            {
                if (!CanEquipWeapon()) return;
                Item weapon = null;
                if (CurrentTarget == null)
                {
                    if (CurrentWeapon == null)
                    {
                        Dictionary<int, List<Item>> weapons = new Dictionary<int, List<Item>> { [0] = new List<Item>(), [1] = new List<Item>(), [2] = new List<Item>(), [3] = new List<Item>(), [4] = new List<Item>() };
                        foreach (Item item in inventory.containerBelt.itemList)
                        {
                            int type = GetTypeWeaponItem(item);
                            if (type == -1) continue;
                            weapons[type].Add(item);
                        }
                        if (weapons[3].Count > 0) weapon = weapons[3].GetRandom();
                        else if (weapons[2].Count > 0) weapon = weapons[2].GetRandom();
                        else if (weapons[1].Count > 0) weapon = weapons[1].GetRandom();
                        else if (weapons[4].Count > 0) weapon = weapons[4].GetRandom();
                        else if (weapons[0].Count > 0) weapon = weapons[0].GetRandom();
                    }
                    else return;
                }
                else
                {
                    float distanceToTarget = DistanceToTarget;
                    int type = -1;
                    foreach (Item item in inventory.containerBelt.itemList)
                    {
                        int currentType = GetTypeWeaponItem(item);
                        if (currentType == -1) continue;
                        if (type == -1)
                        {
                            weapon = item;
                            type = currentType;
                        }
                        else
                        {
                            if (type == currentType) continue;
                            float oldDistance = type > 0 ? Config.AttackRangeMultiplier * type * 10f : 2f;
                            float newDistance = currentType > 0 ? Config.AttackRangeMultiplier * currentType * 10f : 2f;
                            if ((oldDistance > distanceToTarget && newDistance > distanceToTarget && newDistance < oldDistance) ||
                                (oldDistance < distanceToTarget && newDistance > distanceToTarget) ||
                                (oldDistance < distanceToTarget && newDistance < distanceToTarget && newDistance > oldDistance))
                            {
                                weapon = item;
                                type = currentType;
                            }
                        }
                    }
                }
                EquipCurrentWeapon(weapon);
            }

            internal void EquipCurrentWeapon(Item weapon)
            {
                if (weapon == null) return;
                AttackEntity attackEntity = weapon.GetHeldEntity() as AttackEntity;
                if (attackEntity == null) return;
                if (CurrentWeapon == attackEntity) return;
                IsEquipping = true;
                UpdateActiveItem(weapon.uid);
                CurrentWeapon = attackEntity;
                attackEntity.TopUpAmmo();
                if (attackEntity is Chainsaw) (attackEntity as Chainsaw).ServerNPCStart();
                if (attackEntity is BaseProjectile)
                {
                    if (Weapons.ContainsKey(weapon.info.shortname))
                    {
                        attackEntity.effectiveRange = Weapons[weapon.info.shortname].EffectiveRange;
                        attackEntity.attackLengthMin = Weapons[weapon.info.shortname].AttackLengthMin;
                        attackEntity.attackLengthMax = Weapons[weapon.info.shortname].AttackLengthMax;
                    }
                    attackEntity.aiOnlyInRange = true;
                    BaseProjectile baseProjectile = attackEntity as BaseProjectile;
                    if (baseProjectile.MuzzlePoint == null) baseProjectile.MuzzlePoint = baseProjectile.transform;
                    NpcBelt npcBelt = Config.BeltItems.FirstOrDefault(x => x.ShortName == weapon.info.shortname);
                    if (npcBelt != null)
                    {
                        string ammo = npcBelt.Ammo;
                        if (!string.IsNullOrEmpty(ammo))
                        {
                            baseProjectile.primaryMagazine.ammoType = ItemManager.FindItemDefinition(ammo);
                            baseProjectile.SendNetworkUpdateImmediate();
                            if (npcBelt.ShortName == "multiplegrenadelauncher") AmmoTypeGrenadeLauncher = ammo;
                        }
                    }
                }
                Invoke(FinishEquiping, 1.5f);
            }

            private void FinishEquiping() => IsEquipping = false;

            private static int GetTypeWeaponItem(Item item)
            {
                if (MeleeWeapons.Contains(item.info.shortname)) return 0;
                if (FirstDistanceWeapons.Contains(item.info.shortname)) return 1;
                if (SecondDistanceWeapons.Contains(item.info.shortname)) return 2;
                if (ThirdDistanceWeapons.Contains(item.info.shortname)) return 3;
                if (FourthDistanceWeapons.Contains(item.info.shortname)) return 4;
                return -1;
            }
            #endregion Equip Weapons

            public override void AttackerInfo(PlayerLifeStory.DeathInfo info)
            {
                base.AttackerInfo(info);
                if (CurrentWeapon != null) info.inflictorName = CurrentWeapon.ShortPrefabName;
                info.attackerName = displayName;
            }

            public override float GetAimConeScale() => Config.AimConeScale;

            public override string displayName => Config.Name;

            internal bool IsAttackingBaseProjectile { get; set; } = false;

            public override bool IsNpc
            {
                get
                {
                    if (_ins?._config == null || Brain == null || Brain.CurrentState == null) return true;
                    if (IsAttackingBaseProjectile && (_ins._config.CanTargetNpc || _ins._config.CanTargetAnimal) && (Brain.CurrentState.StateType == AIState.Combat || Brain.CurrentState.StateType == AIState.CombatStationary)) return false;
                    if (HasBarricadeTriggerBox()) return false;
                    return true;
                }
            }

            #region NPCBarricadeTriggerBox
            private bool HasBarricadeTriggerBox()
            {
                List<BoxCollider> list = Pool.Get<List<BoxCollider>>();
                Vis.Colliders<BoxCollider>(transform.position, 2f, list, 1 << 18);
                bool hasCollider = list.Any(x => x.gameObject.GetComponent<NPCBarricadeTriggerBox>() != null);
                Pool.FreeUnmanaged(ref list);
                return hasCollider;
            }
            #endregion NPCBarricadeTriggerBox

            #region Heal
            private Coroutine HealCoroutine { get; set; } = null;
            private bool IsHealing { get; set; } = false;

            private bool CanHeal()
            {
                if (IsHealing || health >= Config.Health || CurrentTarget != null || IsFireC4 || IsFireRocketLauncher || IsEquipping || inventory == null || inventory.containerBelt == null) return false;
                return inventory.containerBelt.itemList.Any(x => x.info.shortname == "syringe.medical");
            }

            private IEnumerator Heal()
            {
                IsHealing = true;
                Item syringe = inventory.containerBelt.itemList.FirstOrDefault(x => x.info.shortname == "syringe.medical");
                CurrentWeapon = null;
                UpdateActiveItem(syringe.uid);
                MedicalTool medicalTool = syringe.GetHeldEntity() as MedicalTool;
                yield return CoroutineEx.waitForSeconds(1.5f);
                if (medicalTool != null) medicalTool.ServerUse();
                InitializeHealth(health + 15f > Config.Health ? Config.Health : health + 15f, Config.Health);
                yield return CoroutineEx.waitForSeconds(2f);
                IsHealing = false;
                EquipWeapon();
            }
            #endregion Heal

            #region Grenades
            private HashSet<string> Barricades { get; } = new HashSet<string>
            {
                "barricade.cover.wood",
                "barricade.sandbags",
                "barricade.concrete",
                "barricade.stone"
            };
            private bool IsReloadGrenade { get; set; } = false;
            private bool IsReloadSmoke { get; set; } = false;

            private void FinishReloadGrenade() => IsReloadGrenade = false;

            private void FinishReloadSmoke() => IsReloadSmoke = false;

            private bool CanThrownGrenade()
            {
                if (IsReloadGrenade || CurrentTarget == null || !IsBasePlayerTarget || inventory == null || inventory.containerBelt == null) return false;
                return DistanceToTarget < 15f && inventory.containerBelt.itemList.Any(x => x.info.shortname == "grenade.f1" || x.info.shortname == "grenade.beancan" || x.info.shortname == "grenade.molotov" || x.info.shortname == "grenade.flashbang") && (!CanSeeTarget(CurrentTarget) || IsBehindBarricade());
            }

            internal bool IsBehindBarricade() => CanSeeTarget(CurrentTarget) && IsBarricade();

            private bool IsBarricade()
            {
                SetAimDirection((CurrentTarget.transform.position - transform.position).normalized);
                RaycastHit[] hits = Physics.RaycastAll(eyes.HeadRay());
                GamePhysics.Sort(hits);
                return hits.Select(x => x.GetEntity() as Barricade).Any(x => x != null && Barricades.Contains(x.ShortPrefabName) && Vector3.Distance(transform.position, x.transform.position) < DistanceToTarget);
            }

            private void ThrownGrenade(Vector3 target)
            {
                Item item = inventory.containerBelt.itemList.FirstOrDefault(x => x.info.shortname == "grenade.f1" || x.info.shortname == "grenade.beancan" || x.info.shortname == "grenade.molotov" || x.info.shortname == "grenade.flashbang");
                if (item == null) return;
                GrenadeWeapon weapon = item.GetHeldEntity() as GrenadeWeapon;
                if (weapon == null) return;
                Brain.Navigator.Stop();
                SetAimDirection((target - transform.position).normalized);
                weapon.ServerThrow(target);
                IsReloadGrenade = true;
                Invoke(FinishReloadGrenade, 10f);
            }

            internal void ThrownSmoke()
            {
                if (IsReloadSmoke) return;
                Item item = inventory.containerBelt.itemList.FirstOrDefault(x => x.info.shortname == "grenade.smoke");
                if (item == null) return;
                GrenadeWeapon weapon = item.GetHeldEntity() as GrenadeWeapon;
                if (weapon == null) return;
                weapon.ServerThrow(transform.position);
                IsReloadSmoke = true;
                Invoke(FinishReloadSmoke, 30f);
            }
            #endregion Grenades

            #region Run Away Water
            internal bool IsRunAwayWater { get; set; } = false;

            private bool CanRunAwayWater()
            {
                if (!Config.CanRunAwayWater || IsRunAwayWater) return false;
                if (CurrentTarget == null)
                {
                    if (transform.position.y < -0.25f) return true;
                    else return false;
                }
                if (transform.position.y > -0.25f || TerrainMeta.HeightMap.GetHeight(CurrentTarget.transform.position) > -0.25f) return false;
                if (CurrentWeapon is BaseProjectile && DistanceToTarget < EngagementRange()) return false;
                if (CurrentWeapon is BaseMelee && DistanceToTarget < CurrentWeapon.effectiveRange) return false;
                return true;
            }

            private void RunAwayWater()
            {
                IsRunAwayWater = true;
                CurrentTarget = null;
                Invoke(FinishRunAwayWater, 20f);
            }

            private void FinishRunAwayWater() => IsRunAwayWater = false;
            #endregion Run Away Water

            #region Raid
            internal bool IsRaidState { get; set; } = false;
            internal bool IsRaidStateMelee { get; set; } = false;

            internal bool IsReloadC4 { get; set; } = false;
            internal bool IsReloadRocketLauncher { get; set; } = false;

            internal bool IsFireRocketLauncher { get; set; } = false;
            internal bool IsFireC4 { get; set; } = false;

            private Coroutine FireC4Coroutine { get; set; } = null;
            private Coroutine FireRocketLauncherCoroutine { get; set; } = null;

            internal BaseCombatEntity Turret { get; set; } = null;
            internal BaseCombatEntity PlayerTarget { get; set; } = null;
            internal HashSet<BuildingBlock> Foundations { get; set; } = new HashSet<BuildingBlock>();
            internal BaseCombatEntity CurrentRaidTarget { get; set; } = null;

            internal float DistanceToCurrentRaidTarget => Vector3.Distance(transform.position, CurrentRaidTarget.transform.position);

            internal void AddTurret(BaseCombatEntity turret)
            {
                if (!Turret.IsExists() || Vector3.Distance(transform.position, turret.transform.position) < Vector3.Distance(transform.position, Turret.transform.position))
                {
                    Turret = turret;
                    BuildingBlock block = GetNearEntity<BuildingBlock>(Turret.transform.position, 0.1f, 1 << 21);
                    CurrentRaidTarget = block.IsExists() ? block : Turret;
                }
            }

            private static T GetNearEntity<T>(Vector3 position, float radius, int layerMask) where T : BaseCombatEntity
            {
                List<T> list = Pool.Get<List<T>>();
                Vis.Entities<T>(position, radius, list, layerMask);
                T result = list.Count == 0 ? null : list.Min(s => Vector3.Distance(position, s.transform.position));
                Pool.FreeUnmanaged(ref list);
                return result;
            }

            internal BaseCombatEntity GetRaidTarget()
            {
                UpdateTargets();

                BaseCombatEntity main = null;

                if (IsRaidState)
                {
                    if (Turret != null)
                    {
                        BuildingBlock block = GetNearEntity<BuildingBlock>(Turret.transform.position, 0.1f, 1 << 21);
                        main = block.IsExists() ? block : Turret;
                    }
                    else if (Foundations.Count > 0) main = Foundations.Min(x => Vector3.Distance(transform.position, x.transform.position));
                    else if (PlayerTarget != null) main = PlayerTarget;
                }
                else if (IsRaidStateMelee)
                {
                    if (Foundations.Count > 0) main = Foundations.Min(x => Vector3.Distance(transform.position, x.transform.position));
                }

                if (main == null) return null;

                if (IsMounted()) return main;

                NavMeshHit navMeshHit;
                if (IsRaidState)
                {
                    float heightGround = TerrainMeta.HeightMap.GetHeight(main.transform.position);

                    if (main.transform.position.y - heightGround > 15f)
                    {
                        main = GetNearEntity<BuildingBlock>(new Vector3(main.transform.position.x, heightGround, main.transform.position.z), 15f, 1 << 21);
                        if (main == null) return null;
                    }

                    if (NavMesh.SamplePosition(main.transform.position, out navMeshHit, 30f, NavAgent.areaMask))
                    {
                        NavMeshPath path = new NavMeshPath();
                        if (NavMesh.CalculatePath(transform.position, navMeshHit.position, NavAgent.areaMask, path))
                        {
                            if (path.status == NavMeshPathStatus.PathComplete) return main;
                            else return GetNearEntity<BaseCombatEntity>(path.corners.Last(), 5f, 1 << 8 | 1 << 21);
                        }
                    }

                    Vector2 pos1 = new Vector2(transform.position.x, transform.position.z);
                    Vector2 pos2 = new Vector2(main.transform.position.x, main.transform.position.z);
                    Vector2 pos3 = pos1 + (pos2 - pos1).normalized * (Vector2.Distance(pos1, pos2) - 30f);
                    Vector3 pos = new Vector3(pos3.x, 0f, pos3.y);
                    pos.y = TerrainMeta.HeightMap.GetHeight(pos);

                    main = GetNearEntity<BuildingBlock>(pos, 15f, 1 << 21);
                    if (main == null) return null;

                    if (NavMesh.SamplePosition(main.transform.position, out navMeshHit, 30f, NavAgent.areaMask))
                    {
                        NavMeshPath path = new NavMeshPath();
                        if (NavMesh.CalculatePath(transform.position, navMeshHit.position, NavAgent.areaMask, path))
                        {
                            if (path.status == NavMeshPathStatus.PathComplete) return main;
                            else return GetNearEntity<BaseCombatEntity>(path.corners.Last(), 5f, 1 << 8 | 1 << 21);
                        }
                    }
                }
                else if (IsRaidStateMelee)
                {
                    if (NavMesh.SamplePosition(main.transform.position, out navMeshHit, 30f, NavAgent.areaMask))
                    {
                        NavMeshPath path = new NavMeshPath();
                        if (NavMesh.CalculatePath(transform.position, navMeshHit.position, NavAgent.areaMask, path))
                        {
                            if (path.status == NavMeshPathStatus.PathComplete && Vector3.Distance(navMeshHit.position, main.transform.position) < 6f) return main;
                            else return GetNearEntity<BaseCombatEntity>(path.corners.Last(), 6f, 1 << 8 | 1 << 21);
                        }
                    }
                }

                return main;
            }

            private void UpdateTargets()
            {
                if (!Turret.IsExists()) Turret = null;
                if (!PlayerTarget.IsExists()) PlayerTarget = null;
                foreach (BuildingBlock ent in Foundations.Where(x => !x.IsExists())) Foundations.Remove(ent);
                if (!CurrentRaidTarget.IsExists()) CurrentRaidTarget = null;
            }

            internal bool StartExplosion(BaseCombatEntity target)
            {
                if (target == null) return false;
                if (CanThrownC4(target))
                {
                    FireC4Coroutine = ServerMgr.Instance.StartCoroutine(ThrownC4(target));
                    return true;
                }
                if (CanRaidRocketLauncher(target))
                {
                    ThrownSmoke();
                    FireRocketLauncherCoroutine = ServerMgr.Instance.StartCoroutine(ProcessFireRocketLauncher(target));
                    return true;
                }
                return false;
            }

            internal bool HasRocketLauncher() => inventory.containerBelt.itemList.Any(x => x.info.shortname == "rocket.launcher");

            private bool CanRaidRocketLauncher(BaseCombatEntity target) => !IsReloadRocketLauncher && !IsFireRocketLauncher && !IsEquipping && !IsHealing && HasRocketLauncher() && Vector3.Distance(transform.position, target.transform.position) < 30f;

            private IEnumerator ProcessFireRocketLauncher(BaseCombatEntity target)
            {
                IsFireRocketLauncher = true;
                EquipRocketLauncher();
                if (!IsMounted()) SetDucked(true);
                Brain.Navigator.Stop();
                Brain.Navigator.SetFacingDirectionEntity(target);
                yield return CoroutineEx.waitForSeconds(1.5f);
                if (target.IsExists())
                {
                    if (target.ShortPrefabName.Contains("foundation"))
                    {
                        Brain.Navigator.ClearFacingDirectionOverride();
                        SetAimDirection((target.transform.position - new Vector3(0f, 1.5f, 0f) - transform.position).normalized);
                    }
                    FireRocketLauncher();
                    IsReloadRocketLauncher = true;
                    Invoke(FinishReloadRocketLauncher, 6f);
                }
                IsFireRocketLauncher = false;
                EquipWeapon();
                Brain.Navigator.ClearFacingDirectionOverride();
                if (!IsMounted()) SetDucked(false);
            }

            private void EquipRocketLauncher()
            {
                Item item = inventory.containerBelt.itemList.FirstOrDefault(x => x.info.shortname == "rocket.launcher");
                CurrentWeapon = null;
                UpdateActiveItem(item.uid);
            }

            private void FireRocketLauncher()
            {
                RaycastHit raycastHit;
                SignalBroadcast(Signal.Attack, string.Empty);
                Vector3 vector3 = IsMounted() ? eyes.position + new Vector3(0f, 0.5f, 0f) : eyes.position;
                Vector3 modifiedAimConeDirection = AimConeUtil.GetModifiedAimConeDirection(2.25f, eyes.BodyForward());
                float single = 1f;
                if (Physics.Raycast(vector3, modifiedAimConeDirection, out raycastHit, single, 1236478737)) single = raycastHit.distance - 0.1f;
                TimedExplosive rocket = GameManager.server.CreateEntity("assets/prefabs/ammo/rocket/rocket_basic.prefab", vector3 + modifiedAimConeDirection * single) as TimedExplosive;
                rocket.creatorEntity = this;
                ServerProjectile serverProjectile = rocket.GetComponent<ServerProjectile>();
                serverProjectile.InitializeVelocity(GetInheritedProjectileVelocity(modifiedAimConeDirection) + modifiedAimConeDirection * serverProjectile.speed * 2f);
                rocket.Spawn();
            }

            private void FinishReloadRocketLauncher() => IsReloadRocketLauncher = false;

            internal bool HasC4() => inventory.containerBelt.itemList.Any(x => x.info.shortname == "explosive.timed");

            private bool CanThrownC4(BaseCombatEntity target) => !IsReloadC4 && !IsFireC4 && HasC4() && Vector3.Distance(transform.position, target.transform.position) < 5f;

            private IEnumerator ThrownC4(BaseCombatEntity target)
            {
                Item item = inventory.containerBelt.itemList.FirstOrDefault(x => x.info.shortname == "explosive.timed");
                IsFireC4 = true;
                Brain.Navigator.Stop();
                Brain.Navigator.SetFacingDirectionEntity(target);
                yield return CoroutineEx.waitForSeconds(1.5f);
                if (target.IsExists())
                {
                    ThrownWeapon weapon = item.GetHeldEntity() as ThrownWeapon;
                    if (weapon != null) weapon.ServerThrow(target.transform.position);
                    IsReloadC4 = true;
                    Invoke(FinishReloadC4, 15f);
                }
                IsFireC4 = false;
                Brain.Navigator.ClearFacingDirectionOverride();
            }

            private void FinishReloadC4() => IsReloadC4 = false;

            private static bool IsTeam(ulong playerId, ulong targetId)
            {
                if (playerId == 0 || targetId == 0) return false;
                if (playerId == targetId) return true;
                RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindPlayersTeam(playerId);
                if (playerTeam != null && playerTeam.members.Contains(targetId)) return true;
                if (_ins.plugins.Exists("Friends") && (bool)_ins.Friends.Call("AreFriends", playerId, targetId)) return true;
                if (_ins.plugins.Exists("Clans") && _ins.Clans.Author == "k1lly0u" && (bool)_ins.Clans.Call("IsMemberOrAlly", playerId.ToString(), targetId.ToString())) return true;
                return false;
            }

            private void TryRaidWithoutFoundations()
            {
                if (!IsRaidState || Foundations.Count != 0) return;
                if (CurrentTarget == null || CurrentTarget is Drone)
                {
                    PlayerTarget = null;
                    CurrentRaidTarget = null;
                }
                else if (IsBasePlayerTarget)
                {
                    bool isNull = true;
                    BuildingBlock block = GetNearEntity<BuildingBlock>(CurrentTarget.transform.position, 0.1f, 1 << 21);
                    if (block.IsExists() && IsTeam(GetBasePlayerTarget.userID, block.OwnerID))
                    {
                        PlayerTarget = block;
                        isNull = false;
                    }
                    Tugboat tugboat = CurrentTarget.GetParentEntity() as Tugboat;
                    if (tugboat.IsExists())
                    {
                        PlayerTarget = tugboat;
                        isNull = false;
                    }
                    BaseVehicle vehicle = GetBasePlayerTarget.GetMountedVehicle();
                    if (vehicle.IsExists() && (vehicle is SubmarineDuo || vehicle is BaseSubmarine))
                    {
                        PlayerTarget = vehicle;
                        isNull = false;
                    }
                    if (isNull)
                    {
                        PlayerTarget = null;
                        CurrentRaidTarget = null;
                    }
                }
            }
            #endregion Raid

            #region Guard
            private Vector3 BeforeGuardHomePosition { get; set; } = Vector3.zero;
            internal BaseEntity GuardTarget { get; set; } = null;

            internal void AddTargetGuard(BaseEntity target)
            {
                BeforeGuardHomePosition = HomePosition;
                GuardTarget = target;
            }

            private void UpdateGuardPosition()
            {
                if (BeforeGuardHomePosition == Vector3.zero) return;
                if (GuardTarget.IsExists()) HomePosition = GuardTarget.transform.position;
                else
                {
                    HomePosition = BeforeGuardHomePosition;
                    BeforeGuardHomePosition = Vector3.zero;
                    GuardTarget = null;
                    Interface.Oxide.CallHook("OnCustomNpcGuardTargetEnd", this);
                }
            }
            #endregion Guard

            #region Parent
            private BaseEntity ParentEntity { get; set; } = null;
            private Vector3 LocalPos { get; set; } = Vector3.zero;

            internal void SetParentEntity(BaseEntity parent, Vector3 pos)
            {
                ParentEntity = parent;
                LocalPos = pos;
                InvokeRepeating(UpdateHomePositionParent, 0f, 0.1f);
            }

            private void UpdateHomePositionParent()
            {
                if (ParentEntity != null) HomePosition = ParentEntity.transform.TransformPoint(LocalPos);
                else
                {
                    LocalPos = Vector3.zero;
                    CancelInvoke(UpdateHomePositionParent);
                }
            }
            #endregion Parent

            #region Multiple Grenade Launcher
            internal bool IsReloadGrenadeLauncher { get; set; } = false;
            private int CountAmmoInGrenadeLauncher { get; set; } = 6;
            private string AmmoTypeGrenadeLauncher { get; set; } = "40mm_grenade_he";

            internal void FireGrenadeLauncher()
            {
                RaycastHit raycastHit;
                SignalBroadcast(Signal.Attack, string.Empty);
                Vector3 vector3 = IsMounted() ? eyes.position + new Vector3(0f, 0.5f, 0f) : eyes.position;
                Vector3 modifiedAimConeDirection = AimConeUtil.GetModifiedAimConeDirection(0.675f, eyes.BodyForward());
                float single = 1f;
                if (Physics.Raycast(vector3, modifiedAimConeDirection, out raycastHit, single, 1236478737)) single = raycastHit.distance - 0.1f;
                TimedExplosive grenade = GameManager.server.CreateEntity($"assets/prefabs/ammo/40mmgrenade/{AmmoTypeGrenadeLauncher}.prefab", vector3 + modifiedAimConeDirection * single) as TimedExplosive;
                grenade.creatorEntity = this;
                ServerProjectile serverProjectile = grenade.GetComponent<ServerProjectile>();
                serverProjectile.InitializeVelocity(GetInheritedProjectileVelocity(modifiedAimConeDirection) + modifiedAimConeDirection * serverProjectile.speed * 2f);
                grenade.Spawn();
                CountAmmoInGrenadeLauncher--;
                if (CountAmmoInGrenadeLauncher == 0)
                {
                    IsReloadGrenadeLauncher = true;
                    Invoke(FinishReloadGrenadeLauncher, 8f);
                }
            }

            private void FinishReloadGrenadeLauncher()
            {
                CountAmmoInGrenadeLauncher = 6;
                IsReloadGrenadeLauncher = false;
            }
            #endregion Multiple Grenade Launcher

            #region Flame Thrower
            internal bool IsReloadFlameThrower { get; set; } = false;

            internal void FireFlameThrower()
            {
                FlameThrower flameThrower = CurrentWeapon as FlameThrower;
                if (flameThrower == null || flameThrower.IsFlameOn()) return;
                if (flameThrower.ammo <= 0)
                {
                    IsReloadFlameThrower = true;
                    Invoke(FinishReloadFlameThrower, 4f);
                    return;
                }
                flameThrower.SetFlameState(true);
                Invoke(flameThrower.StopFlameState, 0.25f);
            }

            private void FinishReloadFlameThrower()
            {
                FlameThrower flameThrower = CurrentWeapon as FlameThrower;
                if (flameThrower == null) return;
                flameThrower.TopUpAmmo();
                IsReloadFlameThrower = false;
            }
            #endregion Flame Thrower

            #region Melee Weapon
            internal void UseMeleeWeapon(bool damage = true)
            {
                BaseMelee weapon = CurrentWeapon as BaseMelee;
                if (weapon.HasAttackCooldown()) return;
                weapon.StartAttackCooldown(weapon.repeatDelay * 2f);
                SignalBroadcast(Signal.Attack, string.Empty, null);
                if (weapon.swingEffect.isValid) Effect.server.Run(weapon.swingEffect.resourcePath, weapon.transform.position, Vector3.forward, net.connection, false);
                if (weapon is Chainsaw)
                {
                    Chainsaw chainsaw = weapon as Chainsaw;
                    chainsaw.SetAttackStatus(true);
                    Invoke(() => chainsaw.SetAttackStatus(false), chainsaw.attackSpacing + 0.5f);
                }
                if (weapon is Jackhammer)
                {
                    Jackhammer jackhammer = weapon as Jackhammer;
                    jackhammer.SetEngineStatus(true);
                    Invoke(() => jackhammer.SetEngineStatus(false), jackhammer.attackSpacing + 0.5f);
                }
                if (!damage) return;
                Vector3 vector31 = eyes.BodyForward();
                for (int i = 0; i < 2; i++)
                {
                    List<RaycastHit> list = Pool.Get<List<RaycastHit>>();
                    GamePhysics.TraceAll(new Ray(eyes.position - (vector31 * (i == 0 ? 0f : 0.2f)), vector31), i == 0 ? 0f : weapon.attackRadius, list, weapon.effectiveRange + 0.2f, 1219701521);
                    bool flag = false;
                    for (int j = 0; j < list.Count; j++)
                    {
                        RaycastHit item = list[j];
                        BaseEntity entity = item.GetEntity();
                        if (entity != null && entity != this && !entity.EqualNetID(this) && !entity.isClient)
                        {
                            float single = weapon.damageTypes.Sum(x => x.amount);
                            entity.OnAttacked(new HitInfo(this, entity, DamageType.Slash, single * weapon.npcDamageScale * Config.DamageScale));
                            HitInfo hitInfo = Pool.Get<HitInfo>();
                            hitInfo.HitEntity = entity;
                            hitInfo.HitPositionWorld = item.point;
                            hitInfo.HitNormalWorld = -vector31;
                            if (entity is BaseNpc || entity is BasePlayer) hitInfo.HitMaterial = StringPool.Get("Flesh");
                            else hitInfo.HitMaterial = StringPool.Get(item.GetCollider().sharedMaterial != null ? item.GetCollider().sharedMaterial.GetName() : "generic");
                            weapon.ServerUse_OnHit(hitInfo);
                            Effect.server.ImpactEffect(hitInfo);
                            Pool.Free(ref hitInfo);
                            flag = true;
                            if (entity.ShouldBlockProjectiles()) break;
                        }
                    }
                    Pool.FreeUnmanaged(ref list);
                    if (flag) break;
                }
            }
            #endregion Melee Weapon

            #region Custom Move
            internal Dictionary<int, Dictionary<int, PointNavMesh>> CustomNavMesh = new Dictionary<int, Dictionary<int, PointNavMesh>>();

            internal int CurrentI { get; set; }
            internal int CurrentJ { get; set; }
            internal List<PointPath> Path { get; set; } = new List<PointPath>();
            internal CustomNavMeshController NavMeshController { get; set; }

            public class PointPath { public Vector3 Position; public int I; public int J; }

            internal void InitCustomNavMesh()
            {
                if (NavAgent.enabled) NavAgent.enabled = false;

                NavMeshController = gameObject.AddComponent<CustomNavMeshController>();
                NavMeshController.enabled = false;

                Vector3 result = Vector3.zero;
                float finishDistance = float.PositiveInfinity;
                for (int i = 0; i < CustomNavMesh.Count; i++)
                {
                    for (int j = 0; j < CustomNavMesh[i].Count; j++)
                    {
                        PointNavMesh pointNavMesh = CustomNavMesh[i][j];
                        if (!pointNavMesh.Enabled) continue;
                        float pointDistance = Vector3.Distance(pointNavMesh.Position, transform.position);
                        if (pointDistance < finishDistance)
                        {
                            result = pointNavMesh.Position;
                            CurrentI = i; CurrentJ = j;
                            finishDistance = pointDistance;
                        }
                    }
                }
                transform.position = result;
            }

            private void CalculatePath(Vector3 targetPos)
            {
                if (Path.Count > 0 && Vector3.Distance(Path.Last().Position, targetPos) <= 1.5f) return;
                Vector3 finishPos = GetNearPos(targetPos);
                if (Vector3.Distance(transform.position, finishPos) <= 1.5f) return;
                HashSet<Vector3> blacklist = new HashSet<Vector3>();
                Vector3 currentPos = CustomNavMesh[CurrentI][CurrentJ].Position, nextPos = Vector3.zero;
                int currentI = CurrentI, nextI, currentJ = CurrentJ, nextJ;
                List<PointPath> unsortedPath = Pool.Get<List<PointPath>>();
                int protection = 500;
                while (nextPos != finishPos && protection > 0)
                {
                    protection--;
                    FindNextPosToTarget(currentI, currentJ, blacklist, targetPos, out nextPos, out nextI, out nextJ);
                    if (nextPos == Vector3.zero) break;
                    blacklist.Add(currentPos);
                    currentPos = nextPos;
                    currentI = nextI; currentJ = nextJ;
                    unsortedPath.Add(new PointPath { Position = nextPos, I = nextI, J = nextJ });
                }
                PointPath currentPoint = unsortedPath.Last();
                List<PointPath> reversePath = Pool.Get<List<PointPath>>(); reversePath.Add(currentPoint);
                protection = 500;
                while ((Math.Abs(currentPoint.I - CurrentI) > 1 || Math.Abs(currentPoint.J - CurrentJ) > 1) && protection > 0)
                {
                    protection--;
                    PointPath nextPoint = null;
                    float finishDistance = float.PositiveInfinity;
                    foreach (PointPath point in unsortedPath)
                    {
                        if (Math.Abs(point.I - currentPoint.I) > 1) continue;
                        if (Math.Abs(point.J - currentPoint.J) > 1) continue;
                        if (reversePath.Contains(point)) continue;
                        float pointDistance = Vector3.Distance(point.Position, transform.position);
                        if (pointDistance < finishDistance)
                        {
                            nextPoint = point;
                            finishDistance = pointDistance;
                        }
                    }
                    if (nextPoint == null) break;
                    reversePath.Add(nextPoint);
                    currentPoint = nextPoint;
                }
                Pool.FreeUnmanaged(ref unsortedPath);
                Path.Clear();
                for (int i = reversePath.Count - 1; i >= 0; i--) Path.Add(reversePath[i]);
                Pool.FreeUnmanaged(ref reversePath);
                if (!NavMeshController.enabled && Path.Count > 0) NavMeshController.enabled = true;
            }

            private void FindNextPosToTarget(int currentI, int currentJ, ICollection<Vector3> blacklist, Vector3 targetPos, out Vector3 nextPos, out int nextI, out int nextJ)
            {
                nextPos = Vector3.zero; nextI = 0; nextJ = 0;
                float finishDistance = float.PositiveInfinity, pointDistance;

                if (currentI > 0)
                {
                    if (currentJ + 1 < CustomNavMesh[currentI - 1].Count)
                    {
                        PointNavMesh point1 = CustomNavMesh[currentI - 1][currentJ + 1];
                        if (IsPointEnabled(point1, blacklist))
                        {
                            pointDistance = Vector3.Distance(point1.Position, targetPos);
                            if (pointDistance < finishDistance)
                            {
                                nextPos = point1.Position;
                                nextI = currentI - 1; nextJ = currentJ + 1;
                                finishDistance = pointDistance;
                            }
                        }
                    }

                    PointNavMesh point4 = CustomNavMesh[currentI - 1][currentJ];
                    if (IsPointEnabled(point4, blacklist))
                    {
                        pointDistance = Vector3.Distance(point4.Position, targetPos);
                        if (pointDistance < finishDistance)
                        {
                            nextPos = point4.Position;
                            nextI = currentI - 1; nextJ = currentJ;
                            finishDistance = pointDistance;
                        }
                    }

                    if (currentJ > 0)
                    {
                        PointNavMesh point6 = CustomNavMesh[currentI - 1][currentJ - 1];
                        if (IsPointEnabled(point6, blacklist))
                        {
                            pointDistance = Vector3.Distance(point6.Position, targetPos);
                            if (pointDistance < finishDistance)
                            {
                                nextPos = point6.Position;
                                nextI = currentI - 1; nextJ = currentJ - 1;
                                finishDistance = pointDistance;
                            }
                        }
                    }
                }

                if (currentJ + 1 < CustomNavMesh[currentI].Count)
                {
                    PointNavMesh point2 = CustomNavMesh[currentI][currentJ + 1];
                    if (IsPointEnabled(point2, blacklist))
                    {
                        pointDistance = Vector3.Distance(point2.Position, targetPos);
                        if (pointDistance < finishDistance)
                        {
                            nextPos = point2.Position;
                            nextI = currentI; nextJ = currentJ + 1;
                            finishDistance = pointDistance;
                        }
                    }
                }

                if (currentJ > 0)
                {
                    PointNavMesh point7 = CustomNavMesh[currentI][currentJ - 1];
                    if (IsPointEnabled(point7, blacklist))
                    {
                        pointDistance = Vector3.Distance(point7.Position, targetPos);
                        if (pointDistance < finishDistance)
                        {
                            nextPos = point7.Position;
                            nextI = currentI; nextJ = currentJ - 1;
                            finishDistance = pointDistance;
                        }
                    }
                }

                if (currentI + 1 < CustomNavMesh.Count)
                {
                    if (currentJ + 1 < CustomNavMesh[currentI + 1].Count)
                    {
                        PointNavMesh point3 = CustomNavMesh[currentI + 1][currentJ + 1];
                        if (IsPointEnabled(point3, blacklist))
                        {
                            pointDistance = Vector3.Distance(point3.Position, targetPos);
                            if (pointDistance < finishDistance)
                            {
                                nextPos = point3.Position;
                                nextI = currentI + 1; nextJ = currentJ + 1;
                                finishDistance = pointDistance;
                            }
                        }
                    }

                    PointNavMesh point5 = CustomNavMesh[currentI + 1][currentJ];
                    if (IsPointEnabled(point5, blacklist))
                    {
                        pointDistance = Vector3.Distance(point5.Position, targetPos);
                        if (pointDistance < finishDistance)
                        {
                            nextPos = point5.Position;
                            nextI = currentI + 1; nextJ = currentJ;
                            finishDistance = pointDistance;
                        }
                    }

                    if (currentJ > 0)
                    {
                        PointNavMesh point8 = CustomNavMesh[currentI + 1][currentJ - 1];
                        if (IsPointEnabled(point8, blacklist))
                        {
                            pointDistance = Vector3.Distance(point8.Position, targetPos);
                            if (pointDistance < finishDistance)
                            {
                                nextPos = point8.Position;
                                nextI = currentI + 1; nextJ = currentJ - 1;
                                finishDistance = pointDistance;
                            }
                        }
                    }
                }
            }

            private static bool IsPointEnabled(PointNavMesh point, ICollection<Vector3> blacklist) => point.Enabled && !blacklist.Contains(point.Position);

            private Vector3 GetNearPos(Vector3 targetPos)
            {
                Vector3 result = Vector3.zero;
                float finishDistance = float.PositiveInfinity;
                for (int i = 0; i < CustomNavMesh.Count; i++)
                {
                    for (int j = 0; j < CustomNavMesh[i].Count; j++)
                    {
                        PointNavMesh pointNavMesh = CustomNavMesh[i][j];
                        if (!pointNavMesh.Enabled) continue;
                        float pointDistance = Vector3.Distance(pointNavMesh.Position, targetPos);
                        if (pointDistance < finishDistance)
                        {
                            result = pointNavMesh.Position;
                            finishDistance = pointDistance;
                        }
                    }
                }
                return result;
            }

            internal class CustomNavMeshController : FacepunchBehaviour
            {
                private Vector3 StartPos { get; set; } = Vector3.zero;
                private Vector3 FinishPos { get; set; } = Vector3.zero;

                private float SecondsTaken { get; set; } = 0f;
                private float SecondsToTake { get; set; } = 0f;
                private float WaypointDone { get; set; } = 1f;

                internal float Speed { get; set; } = 0f;

                private CustomScientistNpc Npc { get; set; } = null;

                private void Awake() { Npc = GetComponent<CustomScientistNpc>(); }

                private void FixedUpdate()
                {
                    if (WaypointDone >= 1f)
                    {
                        if (Npc.Path.Count == 0)
                        {
                            enabled = false;
                            return;
                        }
                        StartPos = Npc.transform.position;
                        PointPath point = Npc.Path[0];
                        if (point.Position != StartPos)
                        {
                            Npc.CurrentI = point.I; Npc.CurrentJ = point.J;
                            FinishPos = point.Position;
                            SecondsTaken = 0f;
                            SecondsToTake = Vector3.Distance(FinishPos, StartPos) / Speed;
                            WaypointDone = 0f;
                        }
                        Npc.Path.RemoveAt(0);
                    }
                    if (StartPos != FinishPos)
                    {
                        SecondsTaken += Time.deltaTime;
                        WaypointDone = Mathf.InverseLerp(0f, SecondsToTake, SecondsTaken);
                        Npc.transform.position = Vector3.Lerp(StartPos, FinishPos, WaypointDone);
                        if (!Npc.Brain.Navigator.IsOverridingFacingDirection) Npc.viewAngles = Quaternion.LookRotation(FinishPos - StartPos).eulerAngles;
                    }
                    else WaypointDone = 1f;
                }
            }
            #endregion Custom Move

            #region Move
            internal void SetDestination(Vector3 pos, float radius, BaseNavigator.NavigationSpeed speed)
            {
                if (CustomNavMesh.Count > 0 && NavMeshController != null)
                {
                    CalculatePath(pos);
                    NavMeshController.Speed = Brain.Navigator.Speed * Brain.Navigator.GetSpeedFraction(speed);
                }
                else
                {
                    Vector3 sample = GetSamplePosition(pos, radius);
                    sample.y += 2f;
                    if (!sample.IsEqualVector3(Brain.Navigator.Destination)) Brain.Navigator.SetDestination(sample, speed);
                }
            }

            internal Vector3 GetSamplePosition(Vector3 source, float radius)
            {
                NavMeshHit navMeshHit;
                if (NavMesh.SamplePosition(source, out navMeshHit, radius, NavAgent.areaMask))
                {
                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(transform.position, navMeshHit.position, NavAgent.areaMask, path))
                    {
                        if (path.status == NavMeshPathStatus.PathComplete) return navMeshHit.position;
                        else return path.corners.Last();
                    }
                }
                return source;
            }

            internal Vector3 GetRandomPos(Vector3 source, float radius)
            {
                Vector2 vector2 = UnityEngine.Random.insideUnitCircle * radius;
                return source + new Vector3(vector2.x, 0f, vector2.y);
            }

            internal bool IsPath(Vector3 start, Vector3 finish)
            {
                if (CurrentWeapon == null || NavAgent == null) return false;
                NavMeshHit navMeshHit;
                if (NavMesh.SamplePosition(finish, out navMeshHit, CurrentWeapon.effectiveRange * 2f, NavAgent.areaMask))
                {
                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(start, navMeshHit.position, NavAgent.areaMask, path))
                    {
                        if (path.status == NavMeshPathStatus.PathComplete) return Vector3.Distance(navMeshHit.position, finish) < CurrentWeapon.effectiveRange * 2f;
                        else return Vector3.Distance(path.corners.Last(), finish) < CurrentWeapon.effectiveRange * 2f;
                    }
                    else return false;
                }
                else return false;
            }

            internal bool IsMoving => NavMeshController != null ? NavMeshController.enabled : Brain.Navigator.Moving;
            #endregion Move

            #region States
            internal bool CanChaseState()
            {
                if (IsRunAwayWater) return false;
                if (IsFireC4 || IsFireRocketLauncher) return false;
                if (DistanceFromBase > Config.ChaseRange) return false;
                if (IsRaidState && CurrentRaidTarget != null) return false;
                if (CurrentTarget == null) return false;
                if (_ins.IsGasStationNpc(CurrentTarget)) return false;
                return true;
            }

            internal bool CanCombatState()
            {
                if (CurrentWeapon == null) return false;
                if (CurrentWeapon.ShortPrefabName == "mgl.entity" && IsReloadGrenadeLauncher) return false;
                if (CurrentWeapon is FlameThrower && IsReloadFlameThrower) return false;
                if (IsRunAwayWater) return false;
                if (IsFireC4 || IsFireRocketLauncher) return false;
                if (CurrentTarget == null) return false;
                if (DistanceFromBase > Config.ChaseRange)
                {
                    if (CurrentWeapon is BaseMelee) return false;
                    if (GuardTarget != null) return false;
                }
                if (DistanceToTarget > EngagementRange()) return false;
                if (_ins.IsGasStationNpc(CurrentTarget) && DistanceFromBase > Config.RoamRange) return false;
                if (!CanSeeTarget(CurrentTarget)) return false;
                if (IsBehindBarricade()) return false;
                return true;
            }

            internal bool CanCombatStationaryState()
            {
                if (CurrentWeapon == null) return false;
                if (CurrentWeapon.ShortPrefabName == "mgl.entity" && IsReloadGrenadeLauncher) return false;
                if (CurrentWeapon is FlameThrower && IsReloadFlameThrower) return false;
                if (IsFireC4 || IsFireRocketLauncher) return false;
                if (CurrentTarget == null) return false;
                if (DistanceToTarget > EngagementRange()) return false;
                if (!CanSeeTarget(CurrentTarget)) return false;
                if (IsBehindBarricade()) return false;
                return true;
            }

            internal bool CanRaidState()
            {
                if (IsFireC4 || IsFireRocketLauncher) return true;
                if (IsRunAwayWater) return false;
                if (CurrentRaidTarget == null) return false;
                if (CurrentTarget != null && CanSeeTarget(CurrentTarget) && DistanceToTarget < EngagementRange()) return false;
                if (HasRocketLauncher() || HasC4()) return true;
                return false;
            }

            internal bool CanRaidStateMelee()
            {
                if (IsRunAwayWater) return false;
                if (CurrentRaidTarget == null) return false;
                if (CurrentTarget != null && CanSeeTarget(CurrentTarget) && IsPath(transform.position, CurrentTarget.transform.position)) return false;
                if (CurrentWeapon is BaseMelee || IsTimedExplosiveCurrentWeapon) return true;
                return false;
            }
            #endregion States

            #region Bomber
            internal bool IsBomber => displayName == "Bomber";

            internal bool IsTimedExplosiveCurrentWeapon => CurrentWeapon != null && CurrentWeapon.ShortPrefabName == "explosive.timed.entity";

            private RFTimedExplosive BomberTimedExplosive { get; set; } = null;

            private void SpawnTimedExplosive()
            {
                BomberTimedExplosive = GameManager.server.CreateEntity("assets/prefabs/tools/c4/explosive.timed.deployed.prefab") as RFTimedExplosive;
                BomberTimedExplosive.enableSaving = false;
                BomberTimedExplosive.timerAmountMin = float.PositiveInfinity;
                BomberTimedExplosive.timerAmountMax = float.PositiveInfinity;
                BomberTimedExplosive.transform.localPosition = new Vector3(0f, 1f, 0f);
                BomberTimedExplosive.SetParent(this);
                BomberTimedExplosive.Spawn();
            }

            internal void ExplosionBomber(BaseEntity target = null)
            {
                Effect.server.Run("assets/prefabs/tools/c4/effects/c4_explosion.prefab", transform.position + new Vector3(0f, 1f, 0f), Vector3.up, null, true);
                Interface.Oxide.CallHook("OnBomberExplosion", this, target);
                Kill();
            }
            #endregion Bomber

            #region Sleep
            private void UpdateSleep()
            {
                if (!Config.CanSleep) return;
                bool sleep = Query.Server.PlayerGrid.Query(transform.position.x, transform.position.z, Config.SleepDistance, AIBrainSenses.playerQueryResults, x => x.IsPlayer() && !x.IsSleeping()) == 0;
                if (Brain.sleeping == sleep) return;
                Brain.sleeping = sleep;
                if (Brain.sleeping) SetDestination(HomePosition, 2f, BaseNavigator.NavigationSpeed.Fast);
                else NavAgent.enabled = true;
            }
            #endregion Sleep
        }

        public class CustomScientistBrain : ScientistBrain
        {
            private CustomScientistNpc Npc { get; set; } = null;

            public override void AddStates()
            {
                if (Npc == null) Npc = GetEntity() as CustomScientistNpc;
                states = new Dictionary<AIState, BasicAIState>();
                if (Npc.Config.States.Contains("RoamState")) AddState(new RoamState(Npc));
                if (Npc.Config.States.Contains("ChaseState")) AddState(new ChaseState(Npc));
                if (Npc.Config.States.Contains("CombatState")) AddState(new CombatState(Npc));
                if (Npc.Config.States.Contains("IdleState"))
                {
                    if (Npc.NavAgent.enabled) Npc.NavAgent.enabled = false;
                    AddState(new IdleState(Npc));
                }
                if (Npc.Config.States.Contains("CombatStationaryState"))
                {
                    if (Npc.NavAgent.enabled) Npc.NavAgent.enabled = false;
                    AddState(new CombatStationaryState(Npc));
                }
                if (Npc.Config.States.Contains("RaidState"))
                {
                    Npc.IsRaidState = true;
                    AddState(new RaidState(Npc));
                }
                if (Npc.Config.States.Contains("RaidStateMelee"))
                {
                    Npc.IsRaidStateMelee = true;
                    AddState(new RaidStateMelee(Npc));
                }
                if (Npc.Config.States.Contains("SledgeState")) AddState(new SledgeState(Npc));
                if (Npc.Config.States.Contains("BlazerState")) AddState(new BlazerState(Npc));
            }

            public override void InitializeAI()
            {
                if (Npc == null) Npc = GetEntity() as CustomScientistNpc;
                Npc.HasBrain = true;
                Navigator = GetComponent<BaseNavigator>();
                Navigator.Speed = Npc.Config.Speed;
                InvokeRandomized(DoMovementTick, 1f, 0.1f, 0.01f);

                AttackRangeMultiplier = Npc.Config.AttackRangeMultiplier;
                MemoryDuration = Npc.Config.MemoryDuration;
                SenseRange = Npc.Config.SenseRange;
                TargetLostRange = SenseRange * 2f;
                VisionCone = Vector3.Dot(Vector3.forward, Quaternion.Euler(0f, Npc.Config.VisionCone, 0f) * Vector3.forward);
                CheckVisionCone = Npc.Config.CheckVisionCone;
                CheckLOS = true;
                IgnoreNonVisionSneakers = true;
                MaxGroupSize = 0;
                ListenRange = Npc.Config.ListenRange;
                HostileTargetsOnly = Npc.Config.HostileTargetsOnly;
                IgnoreSafeZonePlayers = !HostileTargetsOnly;
                SenseTypes = EntityType.Player;
                if (_ins._config.CanTargetNpc) SenseTypes |= EntityType.BasePlayerNPC;
                if (_ins._config.CanTargetAnimal) SenseTypes |= EntityType.NPC;
                RefreshKnownLOS = false;
                IgnoreNonVisionMaxDistance = ListenRange / 3f;
                IgnoreSneakersMaxDistance = IgnoreNonVisionMaxDistance / 3f;
                Senses.Init(Npc, this, MemoryDuration, SenseRange, TargetLostRange, VisionCone, CheckVisionCone, CheckLOS, IgnoreNonVisionSneakers, ListenRange, HostileTargetsOnly, false, IgnoreSafeZonePlayers, SenseTypes, RefreshKnownLOS);

                ThinkMode = AIThinkMode.Interval;
                thinkRate = 0.25f;
                PathFinder = new HumanPathFinder();
                ((HumanPathFinder)PathFinder).Init(Npc);
            }

            public override void Think(float delta)
            {
                if (Npc == null) return;
                lastThinkTime = Time.time;
                if (sleeping)
                {
                    if (Npc.NavAgent.enabled && Npc.DistanceFromBase < Npc.Config.RoamRange) Npc.NavAgent.enabled = false;
                    return;
                }
                if (!Npc.IsRunAwayWater)
                {
                    Senses.Update();
                    Npc.CurrentTarget = Npc.GetBestTarget();
                    if (Npc.IsRaidState || Npc.IsRaidStateMelee) Npc.CurrentRaidTarget = Npc.GetRaidTarget();
                }
                CurrentState?.StateThink(delta, this, Npc);
                float single = 0f;
                BasicAIState newState = null;
                foreach (BasicAIState value in states.Values)
                {
                    if (value == null) continue;
                    float weight = value.GetWeight();
                    if (weight < single) continue;
                    single = weight;
                    newState = value;
                }
                if (newState != CurrentState)
                {
                    CurrentState?.StateLeave(this, Npc);
                    CurrentState = newState;
                    CurrentState?.StateEnter(this, Npc);
                }
            }

            public new class RoamState : BasicAIState
            {
                private readonly CustomScientistNpc _npc;

                public RoamState(CustomScientistNpc npc) : base(AIState.Roam) { _npc = npc; }

                public override float GetWeight() => 20f;

                public override void StateLeave(BaseAIBrain brain, BaseEntity entity) { _npc.ThrownSmoke(); }

                public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
                {
                    if (_npc.DistanceFromBase > _npc.Config.RoamRange)
                    {
                        BaseNavigator.NavigationSpeed speed = _npc.DistanceFromBase > 10f ? BaseNavigator.NavigationSpeed.Fast : _npc.DistanceFromBase > 5f ? BaseNavigator.NavigationSpeed.Normal : BaseNavigator.NavigationSpeed.Slowest;
                        _npc.SetDestination(_npc.HomePosition, 2f, speed);
                    }
                    else if (!_npc.IsMoving && _npc.Config.RoamRange > 2f) _npc.SetDestination(_npc.GetRandomPos(_npc.HomePosition, _npc.Config.RoamRange - 2f), 2f, BaseNavigator.NavigationSpeed.Slowest);
                    return StateStatus.Running;
                }
            }

            public new class ChaseState : BasicAIState
            {
                private readonly CustomScientistNpc _npc;

                public ChaseState(CustomScientistNpc npc) : base(AIState.Chase) { _npc = npc; }

                public override float GetWeight() => _npc.CanChaseState() ? 30f : 0f;

                public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
                {
                    if (_npc.CurrentTarget == null) return StateStatus.Error;
                    Vector3 targetPos = _npc.CurrentTarget.transform.position;
                    float distance = 2f;
                    float height = targetPos.y - TerrainMeta.HeightMap.GetHeight(targetPos);
                    if (height > 0f) distance += height;
                    if (_npc.CurrentWeapon is BaseProjectile) _npc.SetDestination(targetPos, distance, _npc.DistanceToTarget > 10f ? BaseNavigator.NavigationSpeed.Fast : BaseNavigator.NavigationSpeed.Normal);
                    else _npc.SetDestination(targetPos, distance, BaseNavigator.NavigationSpeed.Fast);
                    return StateStatus.Running;
                }
            }

            public new class CombatState : BasicAIState
            {
                private readonly CustomScientistNpc _npc;
                private float _nextStrafeTime;

                public CombatState(CustomScientistNpc npc) : base(AIState.Combat) { _npc = npc; }

                public override float GetWeight() => _npc.CanCombatState() ? 40f : 0f;

                public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
                {
                    _npc.SetDucked(false);
                    brain.Navigator.ClearFacingDirectionOverride();
                }

                public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
                {
                    if (_npc.CurrentTarget == null) return StateStatus.Error;
                    brain.Navigator.SetFacingDirectionEntity(_npc.CurrentTarget);
                    if (_npc.CurrentWeapon is BaseProjectile)
                    {
                        if (Time.time > _nextStrafeTime)
                        {
                            if (UnityEngine.Random.Range(0, 3) == 1)
                            {
                                float deltaTime = _npc.CurrentWeapon is BaseLauncher ? UnityEngine.Random.Range(0.5f, 1f) : UnityEngine.Random.Range(1f, 2f);
                                _nextStrafeTime = Time.time + deltaTime;
                                _npc.SetDucked(true);
                                brain.Navigator.Stop();
                            }
                            else
                            {
                                float deltaTime = _npc.CurrentWeapon is BaseLauncher ? UnityEngine.Random.Range(1f, 1.5f) : UnityEngine.Random.Range(2f, 3f);
                                _nextStrafeTime = Time.time + deltaTime;
                                _npc.SetDucked(false);
                                _npc.SetDestination(_npc.GetRandomPos(_npc.transform.position, 2f), 2f, BaseNavigator.NavigationSpeed.Normal);
                            }
                            if (_npc.CurrentWeapon is BaseLauncher) _npc.FireGrenadeLauncher();
                            else
                            {
                                _npc.IsAttackingBaseProjectile = true;
                                _npc.ShotTest(_npc.DistanceToTarget);
                                _npc.IsAttackingBaseProjectile = false;
                            }
                        }
                    }
                    else if (_npc.CurrentWeapon is FlameThrower)
                    {
                        if (_npc.CurrentWeapon.ShortPrefabName == "militaryflamethrower.entity")
                        {
                            if (_npc.DistanceToTarget < _npc.CurrentWeapon.effectiveRange)
                            {
                                _npc.SetDestination(_npc.GetRandomPos(_npc.transform.position, 2f), 2f, BaseNavigator.NavigationSpeed.Normal);
                                _npc.FireFlameThrower();
                            }
                            else _npc.SetDestination(GetDestinationPos(_npc.CurrentTarget.transform.position), 2f, BaseNavigator.NavigationSpeed.Fast);
                        }
                        else if (_npc.CurrentWeapon.ShortPrefabName == "flamethrower.entity")
                        {
                            if (_npc.DistanceToTarget < _npc.CurrentWeapon.effectiveRange) _npc.FireFlameThrower();
                            _npc.SetDestination(GetDestinationPos(_npc.CurrentTarget.transform.position), 2f, BaseNavigator.NavigationSpeed.Fast);
                        }
                    }
                    else if (_npc.CurrentWeapon is BaseMelee)
                    {
                        if (_npc.DistanceToTarget < _npc.CurrentWeapon.effectiveRange * 2f) _npc.UseMeleeWeapon();
                        _npc.SetDestination(GetDestinationPos(_npc.CurrentTarget.transform.position), 2f, BaseNavigator.NavigationSpeed.Fast);
                    }
                    else if (_npc.IsTimedExplosiveCurrentWeapon)
                    {
                        _npc.ExplosionBomber(_npc.CurrentTarget);
                    }
                    return StateStatus.Running;
                }

                private Vector3 GetDestinationPos(Vector3 pos)
                {
                    if ((_ins._config.CanTargetNpc && _npc.CurrentTarget is NPCPlayer) || (_ins._config.CanTargetAnimal && _npc.CurrentTarget is BaseAnimalNPC)) return _npc.GetRandomPos(pos, 2f);
                    else return pos;
                }
            }

            public new class IdleState : BasicAIState
            {
                private readonly CustomScientistNpc _npc;

                public IdleState(CustomScientistNpc npc) : base(AIState.Idle) { _npc = npc; }

                public override float GetWeight() => 10f;

                public override void StateLeave(BaseAIBrain brain, BaseEntity entity) { _npc.ThrownSmoke(); }
            }

            public new class CombatStationaryState : BasicAIState
            {
                private readonly CustomScientistNpc _npc;
                private float _nextStrafeTime;

                public CombatStationaryState(CustomScientistNpc npc) : base(AIState.CombatStationary) { _npc = npc; }

                public override float GetWeight() => _npc.CanCombatStationaryState() ? 40f : 0f;

                public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
                {
                    if (!_npc.IsMounted()) _npc.SetDucked(false);
                    brain.Navigator.ClearFacingDirectionOverride();
                }

                public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
                {
                    if (_npc.CurrentTarget == null) return StateStatus.Error;
                    brain.Navigator.SetFacingDirectionEntity(_npc.CurrentTarget);
                    if (_npc.CurrentWeapon is BaseProjectile)
                    {
                        if (Time.time > _nextStrafeTime)
                        {
                            if (UnityEngine.Random.Range(0, 3) == 1)
                            {
                                float deltaTime = _npc.CurrentWeapon is BaseLauncher ? UnityEngine.Random.Range(0.5f, 1f) : UnityEngine.Random.Range(1f, 2f);
                                _nextStrafeTime = Time.time + deltaTime;
                                if (!_npc.IsMounted()) _npc.SetDucked(true);
                            }
                            else
                            {
                                float deltaTime = _npc.CurrentWeapon is BaseLauncher ? UnityEngine.Random.Range(1f, 1.5f) : UnityEngine.Random.Range(2f, 3f);
                                _nextStrafeTime = Time.time + deltaTime;
                                if (!_npc.IsMounted()) _npc.SetDucked(false);
                            }
                            if (_npc.CurrentWeapon is BaseLauncher) _npc.FireGrenadeLauncher();
                            else
                            {
                                _npc.IsAttackingBaseProjectile = true;
                                _npc.ShotTest(_npc.DistanceToTarget);
                                _npc.IsAttackingBaseProjectile = false;
                            }
                        }
                    }
                    else if (_npc.CurrentWeapon is FlameThrower && _npc.DistanceToTarget < _npc.CurrentWeapon.effectiveRange) _npc.FireFlameThrower();
                    else if (_npc.CurrentWeapon is BaseMelee && _npc.DistanceToTarget < _npc.CurrentWeapon.effectiveRange * 2f) _npc.UseMeleeWeapon();
                    return StateStatus.Running;
                }
            }

            public class RaidState : BasicAIState
            {
                private readonly CustomScientistNpc _npc;

                public RaidState(CustomScientistNpc npc) : base(AIState.Cooldown) { _npc = npc; }

                public override float GetWeight() => _npc.CanRaidState() ? 50f : 0f;

                public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
                {
                    if (!_npc.IsMounted()) _npc.SetDucked(false);
                    brain.Navigator.ClearFacingDirectionOverride();
                }

                public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
                {
                    if (_npc.IsFireC4 || _npc.IsFireRocketLauncher) return StateStatus.Running;
                    if (_npc.CurrentRaidTarget == null) return StateStatus.Error;
                    float distance = _npc.DistanceToCurrentRaidTarget;
                    if (distance > 5f && !_npc.StartExplosion(_npc.CurrentRaidTarget) && !_npc.IsMounted())
                    {
                        _npc.SetDucked(false);
                        _npc.SetDestination(_npc.CurrentRaidTarget.transform.position, 5f, _npc.CurrentRaidTarget is AutoTurret || _npc.CurrentRaidTarget is GunTrap || _npc.CurrentRaidTarget is FlameTurret || distance > 30f ? BaseNavigator.NavigationSpeed.Fast : distance > 5f ? BaseNavigator.NavigationSpeed.Normal : BaseNavigator.NavigationSpeed.Slow);
                    }
                    return StateStatus.Running;
                }
            }

            public class RaidStateMelee : BasicAIState
            {
                private readonly CustomScientistNpc _npc;

                public RaidStateMelee(CustomScientistNpc npc) : base(AIState.Cooldown) { _npc = npc; }

                public override float GetWeight() => _npc.CanRaidStateMelee() ? 50f : 0f;

                public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
                {
                    if (_npc.CurrentRaidTarget == null) return StateStatus.Error;
                    if (_npc.DistanceToCurrentRaidTarget < 6f)
                    {
                        _npc.viewAngles = Quaternion.LookRotation(_npc.CurrentRaidTarget.transform.position - _npc.transform.position).eulerAngles;
                        if (_npc.CurrentWeapon is BaseMelee)
                        {
                            BaseMelee weapon = _npc.CurrentWeapon as BaseMelee;
                            if (!weapon.HasAttackCooldown())
                            {
                                DealDamage(weapon);
                                _npc.UseMeleeWeapon(false);
                            }
                        }
                        else if (_npc.IsTimedExplosiveCurrentWeapon) _npc.ExplosionBomber(_npc.CurrentRaidTarget);
                        else return StateStatus.Error;
                    }
                    else _npc.SetDestination(_npc.CurrentRaidTarget.transform.position, 6f, BaseNavigator.NavigationSpeed.Fast);
                    return StateStatus.Running;
                }

                private void DealDamage(BaseMelee weapon)
                {
                    _npc.CurrentRaidTarget.health -= weapon.damageTypes.Sum(x => x.amount) * weapon.npcDamageScale * _npc.Config.DamageScale;
                    _npc.CurrentRaidTarget.SendNetworkUpdate();
                    if (_npc.CurrentRaidTarget.health <= 0f && _npc.CurrentRaidTarget.IsExists()) _npc.CurrentRaidTarget.Kill(BaseNetworkable.DestroyMode.Gib);
                }
            }

            public class SledgeState : BasicAIState
            {
                private readonly CustomScientistNpc _npc;
                private readonly HashSet<Vector3> _positions;

                public SledgeState(CustomScientistNpc npc) : base(AIState.Cooldown)
                {
                    _npc = npc;
                    _positions = _ins.WallFrames.ToHashSet();
                    _positions.Add(_ins.GeneralPosition);
                }

                public override float GetWeight()
                {
                    if (_npc.CurrentTarget != null && _npc.CanSeeTarget(_npc.CurrentTarget) && _npc.IsPath(_npc.transform.position, _npc.CurrentTarget.transform.position)) return 0f;
                    return 50f;
                }

                public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
                {
                    Vector3 barricadePos = _ins.CustomBarricades.Count == 0 ? Vector3.zero : _ins.CustomBarricades.Min(DistanceToPos);
                    bool haveBarricade = barricadePos != Vector3.zero;

                    Vector3 generalPos = _ins.GeneralPosition;
                    bool haveGeneral = _ins.GeneralPosition != Vector3.zero;

                    bool nearBarricade = haveBarricade && DistanceToPos(barricadePos) < 1.5f;
                    bool nearGeneral = haveGeneral && DistanceToPos(generalPos) < 1.5f;

                    if (nearBarricade || nearGeneral)
                    {
                        _npc.viewAngles = nearBarricade ? Quaternion.LookRotation(barricadePos + new Vector3(0f, 0.5f, 0f) - _npc.transform.position).eulerAngles : Quaternion.LookRotation(generalPos - _npc.transform.position).eulerAngles;
                        if (_npc.CurrentWeapon is BaseMelee) _npc.UseMeleeWeapon(false);
                        else if (_npc.IsTimedExplosiveCurrentWeapon) _npc.ExplosionBomber();
                    }
                    else if (!brain.Navigator.Moving) _npc.SetDestination(GetResultPos(), 1.5f, BaseNavigator.NavigationSpeed.Fast);

                    return StateStatus.Running;
                }

                private Vector3 GetResultPos()
                {
                    List<Vector3> list = Pool.Get<List<Vector3>>();
                    foreach (Vector3 pos in _positions) if (NecessaryPos(pos)) list.Add(pos);
                    list = list.OrderByQuickSort(DistanceToPos);

                    Vector3 point1 = list[0];
                    Vector3 point2 = list[1];

                    float distance0 = DistanceToPos(_ins.GeneralPosition);
                    float distance3 = Vector3.Distance(_ins.GeneralPosition, point1);

                    Vector3 result = _npc.GetRandomPos(distance3 < Vector3.Distance(_ins.GeneralPosition, point2) ? point1 : distance0 >= DistanceToPos(point2) ? distance0 < distance3 ? point2 : point1 : point2, 1.5f);

                    Pool.FreeUnmanaged(ref list);

                    return result;
                }

                private float DistanceToPos(Vector3 pos) => Vector3.Distance(_npc.transform.position, pos);

                private bool NecessaryPos(Vector3 pos) => pos.IsEqualVector3(_ins.GeneralPosition) || Vector3.Distance(_npc.transform.position, pos) > 0.5f || _ins.CustomBarricades.Any(x => pos.IsEqualVector3(x));
            }

            public class BlazerState : BasicAIState
            {
                private readonly CustomScientistNpc _npc;
                private readonly float _radius;
                private readonly Vector3 _center;
                private readonly List<Vector3> _circlePositions = new List<Vector3>();

                public BlazerState(CustomScientistNpc npc) : base(AIState.Cooldown)
                {
                    _npc = npc;
                    _radius = _npc.Config.VisionCone;
                    _center = _ins.GeneralPosition;
                    for (int i = 1; i <= 36; i++) _circlePositions.Add(new Vector3(_center.x + _radius * Mathf.Sin(i * 10f * Mathf.Deg2Rad), _center.y, _center.z + _radius * Mathf.Cos(i * 10f * Mathf.Deg2Rad)));
                }

                public override float GetWeight()
                {
                    if (IsInside) return 45f;
                    if (_npc.CurrentTarget == null) return 45f;
                    else
                    {
                        if (IsOutsideTarget) return 0f;
                        else
                        {
                            Vector3 vector3 = GetCirclePos(GetMovePos(_npc.CurrentTarget.transform.position));
                            if (DistanceToPos(vector3) > 2f) return 45f;
                            else return 0f;
                        }
                    }
                }

                public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
                {
                    if (IsInside) _npc.SetDestination(GetCirclePos(GetMovePos(_npc.transform.position)), 2f, BaseNavigator.NavigationSpeed.Fast);
                    if (_npc.CurrentTarget == null) _npc.CurrentTarget = GetTargetPlayer();
                    if (_npc.CurrentTarget == null) _npc.SetDestination(GetCirclePos(GetMovePos(_npc.transform.position)), 2f, BaseNavigator.NavigationSpeed.Fast);
                    else _npc.SetDestination(GetNextPos(GetMovePos(_npc.CurrentTarget.transform.position)), 2f, BaseNavigator.NavigationSpeed.Fast);
                    return StateStatus.Running;
                }

                private Vector3 GetNextPos(Vector3 targetPos)
                {
                    int numberTarget = _circlePositions.IndexOf(GetCirclePos(targetPos));
                    int numberNear = _circlePositions.IndexOf(GetNearCirclePos);
                    int countNext = numberTarget < numberNear ? _circlePositions.Count - 1 - numberNear + numberTarget : numberTarget - numberNear;
                    if (countNext < 18)
                    {
                        if (numberNear + 1 > 35) return _circlePositions[0];
                        else return _circlePositions[numberNear + 1];
                    }
                    else
                    {
                        if (numberNear - 1 < 0) return _circlePositions[35];
                        else return _circlePositions[numberNear - 1];
                    }
                }

                private Vector3 GetCirclePos(Vector3 targetPos) => _circlePositions.Min(x => Vector3.Distance(targetPos, x));

                private Vector3 GetMovePos(Vector3 targetPos)
                {
                    Vector3 normal3 = (targetPos - _center).normalized;
                    Vector2 vector2 = new Vector2(normal3.x, normal3.z) * _radius;
                    return _center + new Vector3(vector2.x, _center.y, vector2.y);
                }

                private BasePlayer GetTargetPlayer()
                {
                    List<BasePlayer> list = Pool.Get<List<BasePlayer>>();
                    Vis.Entities(_center, _npc.Config.ChaseRange, list, 1 << 17);
                    HashSet<BasePlayer> players = list.Where(x => x.IsPlayer());
                    Pool.FreeUnmanaged(ref list);
                    return players.Count == 0 ? null : players.Min(x => DistanceToPos(x.transform.position));
                }

                private Vector3 GetNearCirclePos => _circlePositions.Min(DistanceToPos);

                private bool IsInside => DistanceToPos(_center) < _radius - 2f;

                private bool IsOutsideTarget => Vector3.Distance(_center, _npc.CurrentTarget.transform.position) > _radius + 2f;

                private float DistanceToPos(Vector3 pos) => Vector3.Distance(_npc.transform.position, pos);
            }
        }
        #endregion Controller

        #region Oxide Hooks
        private static NpcSpawn _ins;

        private void Init() => _ins = this;

        private void OnServerInitialized()
        {
            CreateAllFolders();
            LoadNavMeshes();
            GeneratePositions();
            CheckVersionPlugin();
        }

        private void Unload()
        {
            foreach (CustomScientistNpc npc in Scientists.Values) if (npc.IsExists()) npc.Kill();
            _ins = null;
        }

        private void OnEntityKill(CustomScientistNpc npc)
        {
            if (npc == null || npc.net == null) return;
            ulong id = npc.net.ID.Value;
            if (Scientists.ContainsKey(id)) Scientists.Remove(id);
        }

        private void OnCorpsePopulate(CustomScientistNpc entity, NPCPlayerCorpse corpse) { if (corpse != null && IsCustomScientist(entity)) corpse.containers[1].ClearItemsContainer(); }

        private object CanBradleyApcTarget(BradleyAPC apc, CustomScientistNpc entity)
        {
            if (IsCustomScientist(entity)) return false;
            else return null;
        }

        private object OnNpcTarget(NPCPlayer attacker, CustomScientistNpc victim)
        {
            if (attacker == null || !IsCustomScientist(victim)) return null;
            if (_config.CanTargetNpc) return null;
            else return true;
        }

        private object OnNpcTarget(BaseAnimalNPC attacker, CustomScientistNpc victim)
        {
            if (attacker == null || !IsCustomScientist(victim)) return null;
            if (_config.CanTargetAnimal) return null;
            else return true;
        }

        private object OnEntityTakeDamage(BaseCombatEntity victim, HitInfo info)
        {
            if (victim == null || info == null) return null;

            BaseEntity attacker = info.Initiator;

            if (IsCustomScientist(victim))
            {
                if (attacker == null || attacker.skinID == 11162132011012) return true;

                CustomScientistNpc victimNpc = victim as CustomScientistNpc;
                if (victimNpc == null) return null;

                if (attacker is AutoTurret || attacker is GunTrap || attacker is FlameTurret)
                {
                    if (attacker.OwnerID.IsSteamId()) victimNpc.AddTurret(attacker as BaseCombatEntity);
                    info.damageTypes.ScaleAll(victimNpc.Config.TurretDamageScale);
                    return null;
                }

                if (attacker is BasePlayer)
                {
                    BasePlayer attackerBp = attacker as BasePlayer;

                    if (attackerBp.userID.IsSteamId())
                    {
                        victimNpc.SetKnown(attackerBp);
                        return null;
                    }

                    if (attackerBp.skinID != 0 && SkinIDs.Contains(attackerBp.skinID)) return null;

                    if (attackerBp is NPCPlayer)
                    {
                        NPCPlayer attackerNpc = attackerBp as NPCPlayer;

                        if (attackerNpc is FrankensteinPet || _config.CanTargetNpc)
                        {
                            victimNpc.SetKnown(attackerNpc);
                            return null;
                        }
                    }
                }

                if (attacker is BaseAnimalNPC)
                {
                    BaseAnimalNPC attackerAnimal = attacker as BaseAnimalNPC;
                    if (_config.CanTargetAnimal)
                    {
                        victimNpc.SetKnown(attackerAnimal);
                        return null;
                    }
                }

                return true;
            }

            if (IsCustomScientist(attacker))
            {
                if (victim.skinID == 11162132011012) return true;

                if (victim is BasePlayer)
                {
                    BasePlayer victimBp = victim as BasePlayer;

                    if (victimBp.userID.IsSteamId()) return null;

                    if (victimBp.skinID != 0 && SkinIDs.Contains(victimBp.skinID)) return null;

                    if (victimBp is NPCPlayer && (victimBp is FrankensteinPet || _config.CanTargetNpc)) return null;
                }

                if (victim is BaseAnimalNPC && _config.CanTargetAnimal)
                {
                    info.damageTypes.ScaleAll(4.28571f);
                    return null;
                }

                if (victim is Drone) return null;
                if (victim is Tugboat) return null;
                if (victim is SubmarineDuo) return null;
                if (victim is BaseSubmarine) return null;

                if (victim.OwnerID.IsSteamId())
                {
                    BaseEntity weaponPrefab = info.WeaponPrefab;
                    if (weaponPrefab != null && (weaponPrefab.ShortPrefabName == "rocket_basic" || weaponPrefab.ShortPrefabName == "explosive.timed.deployed"))
                    {
                        CustomScientistNpc attackerNpc = attacker as CustomScientistNpc;
                        if (attackerNpc == null) return null;
                        info.damageTypes.ScaleAll(attackerNpc.Config.DamageScale);
                        return null;
                    }
                }

                return true;
            }

            return null;
        }

        private void OnLoseCondition(Item item, ref float amount)
        {
            if (item == null || amount == 0f) return;
            ScientistNPC npc = item.GetOwnerPlayer() as ScientistNPC;
            if (npc == null)
            {
                if (!item.info.shortname.Contains("mod")) return;
                ItemContainer container = item.GetRootContainer();
                if (container == null) return;
                npc = container.GetOwnerPlayer() as ScientistNPC;
            }
            if (IsCustomScientist(npc)) amount = 0f;
        }
        #endregion Oxide Hooks

        #region True PVE
        private object CanEntityTakeDamage(BaseCombatEntity victim, HitInfo info)
        {
            if (victim == null || info == null) return null;

            BaseEntity attacker = info.Initiator;

            if (IsCustomScientist(victim))
            {
                if (attacker == null || attacker.skinID == 11162132011012) return false;

                if (attacker is AutoTurret || attacker is GunTrap || attacker is FlameTurret) return null;

                if (attacker is BasePlayer)
                {
                    BasePlayer attackerBp = attacker as BasePlayer;

                    if (attackerBp.userID.IsSteamId()) return null;

                    if (attackerBp.skinID != 0 && SkinIDs.Contains(attackerBp.skinID)) return true;

                    if (attackerBp is NPCPlayer && (attackerBp is FrankensteinPet || _config.CanTargetNpc)) return true;
                }

                if (attacker is BaseAnimalNPC && _config.CanTargetAnimal) return true;

                return false;
            }

            if (IsCustomScientist(attacker))
            {
                if (victim.skinID == 11162132011012) return false;

                if (victim is BasePlayer)
                {
                    BasePlayer victimBp = victim as BasePlayer;

                    if (victimBp.userID.IsSteamId()) return null;

                    if (victimBp.skinID != 0 && SkinIDs.Contains(victimBp.skinID)) return true;

                    if (victimBp is NPCPlayer && (victimBp is FrankensteinPet || _config.CanTargetNpc)) return true;
                }

                if (victim is BaseAnimalNPC && _config.CanTargetAnimal) return true;

                if (victim is Drone) return null;
                if (victim is Tugboat) return null;
                if (victim is SubmarineDuo) return null;
                if (victim is BaseSubmarine) return null;

                if (victim.OwnerID.IsSteamId())
                {
                    BaseEntity weaponPrefab = info.WeaponPrefab;
                    if (weaponPrefab != null && (weaponPrefab.ShortPrefabName == "rocket_basic" || weaponPrefab.ShortPrefabName == "explosive.timed.deployed")) return true;
                }

                return false;
            }

            return null;
        }
        #endregion True PVE

        #region Npc Kits
        private object OnNpcKits(CustomScientistNpc npc)
        {
            if (IsCustomScientist(npc)) return true;
            else return null;
        }
        #endregion Npc Kits

        #region Defendable Bases
        internal Vector3 GeneralPosition { get; set; } = Vector3.zero;
        internal HashSet<Vector3> WallFrames { get; } = new HashSet<Vector3>();
        internal HashSet<Vector3> CustomBarricades { get; } = new HashSet<Vector3>();

        private void SetGeneralPos(Vector3 pos) => GeneralPosition = pos;
        private void OnGeneralKill() => GeneralPosition = Vector3.zero;

        private void SetWallFramesPos(List<Vector3> positions)
        {
            foreach (Vector3 pos in positions)
                WallFrames.Add(pos);
        }

        private void OnCustomBarricadeSpawn(Vector3 pos) => CustomBarricades.Add(pos);
        private void OnCustomBarricadeKill(Vector3 pos) => CustomBarricades.Remove(pos);

        private void OnDefendableBasesEnd()
        {
            GeneralPosition = Vector3.zero;
            WallFrames.Clear();
            CustomBarricades.Clear();
        }
        #endregion Defendable Bases

        #region Gas Station Event
        internal HashSet<ulong> GasStationNpc = new HashSet<ulong>();

        internal bool IsGasStationNpc(BaseEntity entity)
        {
            if (entity.skinID != 11162132011012 || entity.net == null) return false;
            return GasStationNpc.Contains(entity.net.ID.Value);
        }

        private void OnGasStationNpcSpawn(HashSet<ulong> ids)
        {
            foreach (ulong id in ids)
                if (!GasStationNpc.Contains(id))
                    GasStationNpc.Add(id);
        }

        private void OnGasStationEventEnd()
        {
            GasStationNpc.Clear();
        }
        #endregion Gas Station Event

        #region Custom Navigation Mesh
        public class PointNavMeshFile { public string Position; public bool Enabled; public bool Border; }

        public class PointNavMesh { public Vector3 Position; public bool Enabled; }

        private Dictionary<string, Dictionary<int, Dictionary<int, PointNavMeshFile>>> AllNavMeshes { get; } = new Dictionary<string, Dictionary<int, Dictionary<int, PointNavMeshFile>>>();

        private void LoadNavMeshes()
        {
            Puts("Loading custom navigation mesh files...");
            foreach (string name in Interface.Oxide.DataFileSystem.GetFiles("NpcSpawn/NavMesh/"))
            {
                string fileName = name.Split('/').Last().Split('.')[0];
                Dictionary<int, Dictionary<int, PointNavMeshFile>> navMesh = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<int, Dictionary<int, PointNavMeshFile>>>($"NpcSpawn/NavMesh/{fileName}");
                if (navMesh == null || navMesh.Count == 0) PrintError($"File {fileName} is corrupted and cannot be loaded!");
                else
                {
                    AllNavMeshes.Add(fileName, navMesh);
                    Puts($"File {fileName} has been loaded successfully!");
                }
            }
            Puts("All custom navigation mesh files have loaded successfully!");
        }
        #endregion Custom Navigation Mesh

        #region Find Random Points
        private void GeneratePositions()
        {
            GenerateBiomePositions(10000);
            GenerateRoadPositions();
            GenerateRailPositions();
        }

        private Dictionary<string, List<Vector3>> BiomePoints { get; } = new Dictionary<string, List<Vector3>>
        {
            ["Arid"] = new List<Vector3>(),
            ["Temperate"] = new List<Vector3>(),
            ["Tundra"] = new List<Vector3>(),
            ["Arctic"] = new List<Vector3>()
        };

        private const int EntityLayers = 1 << 8 | 1 << 21;
        private const int GroundLayers = 1 << 4 | 1 << 10 | 1 << 16 | 1 << 23 | 1 << 25;
        private const int BlockedTopology = (int)(TerrainTopology.Enum.Cliff | TerrainTopology.Enum.Cliffside |
                                                  TerrainTopology.Enum.Beach | TerrainTopology.Enum.Beachside |
                                                  TerrainTopology.Enum.Ocean | TerrainTopology.Enum.Oceanside |
                                                  TerrainTopology.Enum.Monument | TerrainTopology.Enum.Building |
                                                  TerrainTopology.Enum.River | TerrainTopology.Enum.Riverside |
                                                  TerrainTopology.Enum.Lake | TerrainTopology.Enum.Lakeside);

        private void GenerateBiomePositions(int attempts)
        {
            for (int i = 0; i < attempts; i++)
            {
                Vector2 random = World.Size * 0.475f * UnityEngine.Random.insideUnitCircle;
                Vector3 position = new Vector3(random.x, 500f, random.y);

                if (!IsAvailableTopology(position)) continue;

                if (IsRaycast(position, out RaycastHit raycastHit)) position.y = raycastHit.point.y;
                else continue;

                if (IsNavMesh(position, out NavMeshHit navMeshHit)) position = navMeshHit.position;
                else continue;

                if (IsEntities(position, 6f)) continue;

                TerrainBiome.Enum majorityBiome = (TerrainBiome.Enum)TerrainMeta.BiomeMap.GetBiomeMaxType(position);

                BiomePoints[majorityBiome.ToString()].Add(position);
            }
            Puts($"List of biome positions: Arid = {BiomePoints["Arid"].Count}, Temperate = {BiomePoints["Temperate"].Count}, Tundra = {BiomePoints["Tundra"].Count}, Arctic = {BiomePoints["Arctic"].Count}");
        }

        private object GetSpawnPoint(string biome)
        {
            if (!BiomePoints.TryGetValue(biome, out List<Vector3> positions)) return null;

            if (positions.Count < 100) GenerateBiomePositions(1000);

            int attempts = 100;
            while (attempts > 0)
            {
                attempts--;
                if (positions.Count == 0) continue;

                Vector3 position = positions.GetRandom();

                if (IsEntities(position, 6f))
                {
                    positions.Remove(position);
                    if (positions.Count < 100) GenerateBiomePositions(1000);
                    continue;
                }

                return position;
            }

            return null;
        }

        private static bool IsAvailableTopology(Vector3 position) => (TerrainMeta.TopologyMap.GetTopology(position) & BlockedTopology) == 0;

        private static bool IsRaycast(Vector3 position, out RaycastHit raycastHit) => Physics.Raycast(position, Vector3.down, out raycastHit, 500f, GroundLayers);

        private static bool IsNavMesh(Vector3 position, out NavMeshHit navMeshHit) => NavMesh.SamplePosition(position, out navMeshHit, 2f, 1);

        private static bool IsEntities(Vector3 position, float radius)
        {
            List<BaseEntity> list = Pool.Get<List<BaseEntity>>();
            Vis.Entities(position, radius, list, EntityLayers);
            bool hasEntity = list.Count > 0;
            Pool.FreeUnmanaged(ref list);
            return hasEntity;
        }

        private Dictionary<string, List<Vector3>> RoadPoints { get; } = new Dictionary<string, List<Vector3>>
        {
            ["ExtraWide"] = new List<Vector3>(),
            ["Standard"] = new List<Vector3>(),
            ["ExtraNarrow"] = new List<Vector3>()
        };

        private void GenerateRoadPositions()
        {
            foreach (PathList path in TerrainMeta.Path.Roads)
            {
                string name = path.Width < 5f ? "ExtraNarrow" : path.Width > 10 ? "ExtraWide" : "Standard";
                foreach (Vector3 vector3 in path.Path.Points) RoadPoints[name].Add(vector3);
            }
            Puts($"List of road positions: ExtraWide = {RoadPoints["ExtraWide"].Count}, Standard = {RoadPoints["Standard"].Count}, ExtraNarrow = {RoadPoints["ExtraNarrow"].Count}");
        }

        private object GetRoadSpawnPoint(string road)
        {
            if (!RoadPoints.ContainsKey(road)) return null;
            List<Vector3> positions = RoadPoints[road];
            if (positions.Count == 0) return null;
            return positions.GetRandom();
        }

        private List<Vector3> RailPositions { get; } = new List<Vector3>();

        private void GenerateRailPositions()
        {
            foreach (PathList path in TerrainMeta.Path.Rails)
                foreach (Vector3 vector3 in path.Path.Points)
                    RailPositions.Add(vector3);
            Puts($"{RailPositions.Count} railway positions found");
        }

        private object GetRailSpawnPoint()
        {
            if (RailPositions.Count == 0) return null;
            return RailPositions.GetRandom();
        }
        #endregion Find Random Points

        #region Helpers
        [PluginReference] private readonly Plugin Kits, Friends, Clans;

        private Dictionary<ulong, CustomScientistNpc> Scientists { get; } = new Dictionary<ulong, CustomScientistNpc>();

        private static void CreateAllFolders()
        {
            string url = Interface.Oxide.DataDirectory + "/NpcSpawn/";
            if (!Directory.Exists(url)) Directory.CreateDirectory(url);
            if (!Directory.Exists(url + "NavMesh/")) Directory.CreateDirectory(url + "NavMesh/");
            if (!Directory.Exists(url + "Preset/")) Directory.CreateDirectory(url + "Preset/");
        }

        private void CheckVersionPlugin()
        {
            webrequest.Enqueue("http://37.153.157.216:5000/Api/GetPluginVersions?pluginName=NpcSpawn", null, (code, response) =>
            {
                if (code != 200 || string.IsNullOrEmpty(response)) return;
                string[] array = response.Replace("\"", string.Empty).Split('.');
                VersionNumber latestVersion = new VersionNumber(Convert.ToInt32(array[0]), Convert.ToInt32(array[1]), Convert.ToInt32(array[2]));
                if (Version < latestVersion) PrintWarning($"A new version ({latestVersion}) of the plugin is available! You need to update the plugin (https://drive.google.com/drive/folders/1-18L-mG7yiGxR-PQYvd11VvXC2RQ4ZCu?usp=sharing)");
            }, this);
        }

        internal HashSet<ulong> SkinIDs = new HashSet<ulong>
        {
            14922524,
            19395142091920,
            8151920175
        };
        #endregion Helpers
    }
}

namespace Oxide.Plugins.NpcSpawnExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return true;
            return false;
        }

        public static HashSet<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) result.Add(enumerator.Current);
            return result;
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return enumerator.Current;
            return default(TSource);
        }

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(enumerator.Current);
            return result;
        }

        public static HashSet<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> predicate)
        {
            HashSet<TResult> result = new HashSet<TResult>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(predicate(enumerator.Current));
            return result;
        }

        private static void Replace<TSource>(this IList<TSource> source, int x, int y)
        {
            TSource t = source[x];
            source[x] = source[y];
            source[y] = t;
        }

        private static List<TSource> QuickSort<TSource>(this List<TSource> source, Func<TSource, float> predicate, int minIndex, int maxIndex)
        {
            if (minIndex >= maxIndex) return source;

            int pivotIndex = minIndex - 1;
            for (int i = minIndex; i < maxIndex; i++)
            {
                if (predicate(source[i]) < predicate(source[maxIndex]))
                {
                    pivotIndex++;
                    source.Replace(pivotIndex, i);
                }
            }
            pivotIndex++;
            source.Replace(pivotIndex, maxIndex);

            QuickSort(source, predicate, minIndex, pivotIndex - 1);
            QuickSort(source, predicate, pivotIndex + 1, maxIndex);

            return source;
        }

        public static List<TSource> OrderByQuickSort<TSource>(this List<TSource> source, Func<TSource, float> predicate) => source.QuickSort(predicate, 0, source.Count - 1);

        public static float Sum<TSource>(this IList<TSource> source, Func<TSource, float> predicate)
        {
            float result = 0;
            for (int i = 0; i < source.Count; i++) result += predicate(source[i]);
            return result;
        }

        public static TSource Last<TSource>(this IList<TSource> source) => source[source.Count - 1];

        public static TSource Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            TSource result = default(TSource);
            float resultValue = float.MaxValue;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TSource element = enumerator.Current;
                    float elementValue = predicate(element);
                    if (elementValue < resultValue)
                    {
                        result = element;
                        resultValue = elementValue;
                    }
                }
            }
            return result;
        }

        public static bool IsPlayer(this BasePlayer player) => player != null && player.userID.IsSteamId();

        public static bool IsExists(this BaseNetworkable entity) => entity != null && !entity.IsDestroyed;

        public static void ClearItemsContainer(this ItemContainer container)
        {
            for (int i = container.itemList.Count - 1; i >= 0; i--)
            {
                Item item = container.itemList[i];
                item.RemoveFromContainer();
                item.Remove();
            }
        }

        public static bool IsEqualVector3(this Vector3 a, Vector3 b) => Vector3.Distance(a, b) < 0.1f;
    }
}