using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Facepunch;
using UnityEngine.Networking;
using System.Reflection;
using Oxide.Plugins.PowerPlantEventExtensionMethods;

namespace Oxide.Plugins
{
    [Info("PowerPlantEvent", "rustmods.ru", "2.2.4")]
    internal class PowerPlantEvent : RustPlugin
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
            if (_config.PluginVersion < new VersionNumber(2, 0, 3))
            {
                _config.Gui = new GuiConfig
                {
                    IsGui = true,
                    OffsetMinY = "-56"
                };
                foreach (PresetConfig preset in _config.NpcStart) foreach (NpcBelt belt in preset.Config.BeltItems) belt.Ammo = string.Empty;
                foreach (PresetConfig preset in _config.NpcTrain) foreach (NpcBelt belt in preset.Config.BeltItems) belt.Ammo = string.Empty;
                foreach (PresetConfig preset in _config.NpcButton) foreach (NpcBelt belt in preset.Config.BeltItems) belt.Ammo = string.Empty;
            }
            if (_config.PluginVersion < new VersionNumber(2, 0, 7))
            {
                _config.Commands = new HashSet<string>
                {
                    "/remove",
                    "remove.toggle"
                };
            }
            if (_config.PluginVersion < new VersionNumber(2, 0, 8))
            {
                _config.Radius = 140f;
            }
            if (_config.PluginVersion < new VersionNumber(2, 1, 1))
            {
                _config.MainPoint = new PointConfig
                {
                    Enabled = true,
                    Text = "◈",
                    Size = 45,
                    Color = "#CCFF00"
                };
                _config.AdditionalPoint = new PointConfig
                {
                    Enabled = true,
                    Text = "◆",
                    Size = 25,
                    Color = "#FFC700"
                };
            }
            if (_config.PluginVersion < new VersionNumber(2, 1, 3))
            {
                _config.GameTip = new GameTipConfig
                {
                    IsGameTip = false,
                    Style = 2
                };
                _config.Marker = new MarkerConfig
                {
                    Enabled = true,
                    Type = 1,
                    Radius = 0.37967f,
                    Alpha = 0.35f,
                    Color = new ColorConfig { R = 0.81f, G = 0.25f, B = 0.15f },
                    Text = "PowerPlantEvent"
                };
            }
            if (_config.PluginVersion < new VersionNumber(2, 1, 6))
            {
                _config.PveMode.ScaleDamage = new Dictionary<string, float>
                {
                    ["Npc"] = 1f
                };
            }
            if (_config.PluginVersion < new VersionNumber(2, 1, 7))
            {
                _config.Chat = new ChatConfig
                {
                    IsChat = true,
                    Prefix = "[PowerPlantEvent]"
                };
                _config.DistanceAlerts = 0f;
                _config.Notify.Type = 0;
            }
            _config.PluginVersion = Version;
            Puts("Config update completed!");
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        public class ItemConfig
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty(En ? "Minimum" : "Минимальное кол-во")] public int MinAmount { get; set; }
            [JsonProperty(En ? "Maximum" : "Максимальное кол-во")] public int MaxAmount { get; set; }
            [JsonProperty(En ? "Chance [0.0-100.0]" : "Шанс выпадения предмета [0.0-100.0]")] public float Chance { get; set; }
            [JsonProperty(En ? "Is this a blueprint? [true/false]" : "Это чертеж? [true/false]")] public bool IsBluePrint { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinId { get; set; }
            [JsonProperty(En ? "Name (empty - default)" : "Название (empty - default)")] public string Name { get; set; }
        }

        public class LootTableConfig
        {
            [JsonProperty(En ? "Minimum numbers of items" : "Минимальное кол-во элементов")] public int Min { get; set; }
            [JsonProperty(En ? "Maximum numbers of items" : "Максимальное кол-во элементов")] public int Max { get; set; }
            [JsonProperty(En ? "Use minimum and maximum values? [true/false]" : "Использовать минимальное и максимальное значение? [true/false]")] public bool UseCount { get; set; }
            [JsonProperty(En ? "List of items" : "Список предметов")] public List<ItemConfig> Items { get; set; }
        }

        public class PrefabConfig
        {
            [JsonProperty(En ? "Chance [0.0-100.0]" : "Шанс выпадения [0.0-100.0]")] public float Chance { get; set; }
            [JsonProperty(En ? "The path to the prefab" : "Путь к prefab-у")] public string PrefabDefinition { get; set; }
        }

        public class PrefabLootTableConfig
        {
            [JsonProperty(En ? "Minimum numbers of prefabs" : "Минимальное кол-во prefab-ов")] public int Min { get; set; }
            [JsonProperty(En ? "Maximum numbers of prefabs" : "Максимальное кол-во prefab-ов")] public int Max { get; set; }
            [JsonProperty(En ? "Use minimum and maximum values? [true/false]" : "Использовать минимальное и максимальное значение? [true/false]")] public bool UseCount { get; set; }
            [JsonProperty(En ? "List of prefabs" : "Список prefab-ов")] public List<PrefabConfig> Prefabs { get; set; }
        }

        public class CrateConfig
        {
            [JsonProperty("Prefab")] public string Prefab { get; set; }
            [JsonProperty(En ? "Position" : "Позиция")] public string Position { get; set; }
            [JsonProperty(En ? "Rotation" : "Вращение")] public string Rotation { get; set; }
            [JsonProperty(En ? "Loot table from prefabs (if the loot table type is 4 or 5)" : "Таблица предметов из prefab-ов (если тип таблицы предметов - 4 или 5)")] public PrefabLootTableConfig PrefabLootTable { get; set; }
            [JsonProperty(En ? "Own loot table (if the loot table type is 1 or 5)" : "Собственная таблица предметов (если тип таблицы предметов - 1 или 5)")] public LootTableConfig OwnLootTable { get; set; }
        }

        public class HackCrateConfig
        {
            [JsonProperty(En ? "Location of all Crates" : "Расположение всех ящиков")] public HashSet<CoordConfig> Coordinates { get; set; }
            [JsonProperty(En ? "Time to unlock the Crates [sec.]" : "Время разблокировки ящиков [sec.]")] public float UnlockTime { get; set; }
            [JsonProperty(En ? "Increase the event time if it's not enough to unlock the locked crate? [true/false]" : "Увеличивать время ивента, если недостаточно чтобы разблокировать заблокированный ящик? [true/false]")] public bool IncreaseEventTime { get; set; }
            [JsonProperty(En ? "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - loot table of the Rust objects; 5 - combine the 1 and 4 methods)" : "Какую таблицу лута необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - таблица предметов объектов Rust; 5 - совместить 1 и 4 методы)")] public int TypeLootTable { get; set; }
            [JsonProperty(En ? "Loot table from prefabs (if the loot table type is 4 or 5)" : "Таблица предметов из prefab-ов (если тип таблицы предметов - 4 или 5)")] public PrefabLootTableConfig PrefabLootTable { get; set; }
            [JsonProperty(En ? "Own loot table (if the loot table type is 1 or 5)" : "Собственная таблица предметов (если тип таблицы предметов - 1 или 5)")] public LootTableConfig OwnLootTable { get; set; }
        }

        public class ColorConfig
        {
            [JsonProperty("r")] public float R { get; set; }
            [JsonProperty("g")] public float G { get; set; }
            [JsonProperty("b")] public float B { get; set; }
        }

        public class MarkerConfig
        {
            [JsonProperty(En ? "Use map marker? [true/false]" : "Использовать маркер на карте? [true/false]")] public bool Enabled { get; set; }
            [JsonProperty(En ? "Type (0 - simple, 1 - advanced)" : "Тип (0 - упрощенный, 1 - расширенный)")] public int Type { get; set; }
            [JsonProperty(En ? "Background radius (if the marker type is 0)" : "Радиус фона (если тип маркера - 0)")] public float Radius { get; set; }
            [JsonProperty(En ? "Background transparency" : "Прозрачность фона")] public float Alpha { get; set; }
            [JsonProperty(En ? "Color" : "Цвет")] public ColorConfig Color { get; set; }
            [JsonProperty(En ? "Text" : "Текст")] public string Text { get; set; }
        }

        public class PointConfig
        {
            [JsonProperty(En ? "Enabled? [true/false]" : "Включен? [true/false]")] public bool Enabled { get; set; }
            [JsonProperty(En ? "Text" : "Текст")] public string Text { get; set; }
            [JsonProperty(En ? "Size" : "Размер")] public int Size { get; set; }
            [JsonProperty(En ? "Color" : "Цвет")] public string Color { get; set; }
        }

        public class GuiConfig
        {
            [JsonProperty(En ? "Do you use the countdown GUI? [true/false]" : "Использовать ли GUI обратного отсчета? [true/false]")] public bool IsGui { get; set; }
            [JsonProperty("OffsetMin Y")] public string OffsetMinY { get; set; }
        }

        public class ChatConfig
        {
            [JsonProperty(En ? "Do you use the chat? [true/false]" : "Использовать ли чат? [true/false]")] public bool IsChat { get; set; }
            [JsonProperty(En ? "Prefix of chat messages" : "Префикс сообщений в чате")] public string Prefix { get; set; }
        }

        public class GameTipConfig
        {
            [JsonProperty(En ? "Use Facepunch Game Tips (notification bar above hotbar)? [true/false]" : "Использовать ли Facepunch Game Tip (оповещения над слотами быстрого доступа игрока)? [true/false]")] public bool IsGameTip { get; set; }
            [JsonProperty(En ? "Style (0 - Blue Normal, 1 - Red Normal, 2 - Blue Long, 3 - Blue Short, 4 - Server Event)" : "Стиль (0 - Blue Normal, 1 - Red Normal, 2 - Blue Long, 3 - Blue Short, 4 - Server Event)")] public int Style { get; set; }
        }

        public class GuiAnnouncementsConfig
        {
            [JsonProperty(En ? "Do you use the GUI Announcements? [true/false]" : "Использовать ли GUI Announcements? [true/false]")] public bool IsGuiAnnouncements { get; set; }
            [JsonProperty(En ? "Banner color" : "Цвет баннера")] public string BannerColor { get; set; }
            [JsonProperty(En ? "Text color" : "Цвет текста")] public string TextColor { get; set; }
            [JsonProperty(En ? "Adjust Vertical Position" : "Отступ от верхнего края")] public float ApiAdjustVPosition { get; set; }
        }

        public class NotifyConfig
        {
            [JsonProperty(En ? "Do you use the Notify? [true/false]" : "Использовать ли Notify? [true/false]")] public bool IsNotify { get; set; }
            [JsonProperty(En ? "Type" : "Тип")] public int Type { get; set; }
        }

        public class DiscordConfig
        {
            [JsonProperty(En ? "Do you use the Discord? [true/false]" : "Использовать ли Discord? [true/false]")] public bool IsDiscord { get; set; }
            [JsonProperty("Webhook URL")] public string WebhookUrl { get; set; }
            [JsonProperty(En ? "Embed Color (DECIMAL)" : "Цвет полосы (DECIMAL)")] public int EmbedColor { get; set; }
            [JsonProperty(En ? "Keys of required messages" : "Ключи необходимых сообщений")] public HashSet<string> Keys { get; set; }
        }

        public class EconomyConfig
        {
            [JsonProperty(En ? "Which economy plugins do you want to use? (Economics, Server Rewards, IQEconomic)" : "Какие плагины экономики вы хотите использовать? (Economics, Server Rewards, IQEconomic)")] public HashSet<string> Plugins { get; set; }
            [JsonProperty(En ? "The minimum value that a player must collect to get points for the economy" : "Минимальное значение, которое игрок должен заработать, чтобы получить баллы за экономику")] public double Min { get; set; }
            [JsonProperty(En ? "Looting of crates" : "Ограбление ящиков")] public Dictionary<string, double> Crates { get; set; }
            [JsonProperty(En ? "Killing an NPC" : "Убийство NPC")] public double Npc { get; set; }
            [JsonProperty(En ? "Hacking a locked crate" : "Взлом заблокированного ящика")] public double LockedCrate { get; set; }
            [JsonProperty(En ? "Pressing the button" : "Нажатие кнопки")] public double Button { get; set; }
            [JsonProperty(En ? "List of commands that are executed in the console at the end of the event ({steamid} - the player who collected the highest number of points)" : "Список команд, которые выполняются в консоли по окончанию ивента ({steamid} - игрок, который набрал наибольшее кол-во баллов)")] public HashSet<string> Commands { get; set; }
        }

        public class PveModeConfig
        {
            [JsonProperty(En ? "Use the PVE mode of the plugin? [true/false]" : "Использовать PVE режим работы плагина? [true/false]")] public bool Pve { get; set; }
            [JsonProperty(En ? "The amount of damage that the player has to do to become the Event Owner" : "Кол-во урона, которое должен нанести игрок, чтобы стать владельцем ивента")] public float Damage { get; set; }
            [JsonProperty(En ? "Damage Multipliers for calculate to become the Event Owner" : "Коэффициенты урона для подсчета, чтобы стать владельцем ивента")] public Dictionary<string, float> ScaleDamage { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event loot the crates? [true/false]" : "Может ли не владелец ивента грабить ящики? [true/false]")] public bool LootCrate { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event hack locked crates? [true/false]" : "Может ли не владелец ивента взламывать заблокированные ящики? [true/false]")] public bool HackCrate { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event loot NPC corpses? [true/false]" : "Может ли не владелец ивента грабить трупы NPC? [true/false]")] public bool LootNpc { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event deal damage to the NPC? [true/false]" : "Может ли не владелец ивента наносить урон по NPC? [true/false]")] public bool DamageNpc { get; set; }
            [JsonProperty(En ? "Can an Npc attack a non-owner of the event? [true/false]" : "Может ли Npc атаковать не владельца ивента? [true/false]")] public bool TargetNpc { get; set; }
            [JsonProperty(En ? "Allow the non-owner of the event to enter the event zone? [true/false]" : "Разрешать входить внутрь зоны ивента не владельцу ивента? [true/false]")] public bool CanEnter { get; set; }
            [JsonProperty(En ? "Allow a player who has an active cooldown of the Event Owner to enter the event zone? [true/false]" : "Разрешать входить внутрь зоны ивента игроку, у которого активен кулдаун на получение статуса владельца ивента? [true/false]")] public bool CanEnterCooldownPlayer { get; set; }
            [JsonProperty(En ? "The time that the Event Owner may not be inside the event zone [sec.]" : "Время, которое владелец ивента может не находиться внутри зоны ивента [сек.]")] public int TimeExitOwner { get; set; }
            [JsonProperty(En ? "The time until the end of Event Owner status when it is necessary to warn the player [sec.]" : "Время таймера до окончания действия статуса владельца ивента, когда необходимо предупредить игрока [сек.]")] public int AlertTime { get; set; }
            [JsonProperty(En ? "Prevent the actions of the RestoreUponDeath plugin in the event zone? [true/false]" : "Запрещать работу плагина RestoreUponDeath в зоне действия ивента? [true/false]")] public bool RestoreUponDeath { get; set; }
            [JsonProperty(En ? "The time that the player can`t become the Event Owner, after the end of the event and the player was its owner [sec.]" : "Время, которое игрок не сможет стать владельцем ивента, после того как ивент окончен и игрок был его владельцем [sec.]")] public double CooldownOwner { get; set; }
            [JsonProperty(En ? "Darkening the dome (0 - disables the dome)" : "Затемнение купола (0 - отключает купол)")] public int Darkening { get; set; }
        }

        public class NpcBelt
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty(En ? "Amount" : "Кол-во")] public int Amount { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinId { get; set; }
            [JsonProperty(En ? "Mods" : "Модификации на оружие")] public HashSet<string> Mods { get; set; }
            [JsonProperty(En ? "Ammo" : "Боеприпасы")] public string Ammo { get; set; }
        }

        public class NpcWear
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinId { get; set; }
        }

        public class NpcConfig
        {
            [JsonProperty(En ? "Name" : "Название")] public string Name { get; set; }
            [JsonProperty(En ? "Health" : "Кол-во ХП")] public float Health { get; set; }
            [JsonProperty(En ? "Roam Range" : "Дальность патрулирования местности")] public float RoamRange { get; set; }
            [JsonProperty(En ? "Chase Range" : "Дальность погони за целью")] public float ChaseRange { get; set; }
            [JsonProperty(En ? "Attack Range Multiplier" : "Множитель радиуса атаки")] public float AttackRangeMultiplier { get; set; }
            [JsonProperty(En ? "Sense Range" : "Радиус обнаружения цели")] public float SenseRange { get; set; }
            [JsonProperty(En ? "Target Memory Duration [sec.]" : "Длительность памяти цели [sec.]")] public float MemoryDuration { get; set; }
            [JsonProperty(En ? "Scale damage" : "Множитель урона")] public float DamageScale { get; set; }
            [JsonProperty(En ? "Aim Cone Scale" : "Множитель разброса")] public float AimConeScale { get; set; }
            [JsonProperty(En ? "Detect the target only in the NPC's viewing vision cone? [true/false]" : "Обнаруживать цель только в углу обзора NPC? [true/false]")] public bool CheckVisionCone { get; set; }
            [JsonProperty(En ? "Vision Cone" : "Угол обзора")] public float VisionCone { get; set; }
            [JsonProperty(En ? "Speed" : "Скорость")] public float Speed { get; set; }
            [JsonProperty(En ? "Disable radio effects? [true/false]" : "Отключать эффекты рации? [true/false]")] public bool DisableRadio { get; set; }
            [JsonProperty(En ? "Is this a stationary NPC? [true/false]" : "Это стационарный NPC? [true/false]")] public bool Stationary { get; set; }
            [JsonProperty(En ? "Remove a corpse after death? (it is recommended to use the true value to improve performance) [true/false]" : "Удалять труп после смерти? (рекомендуется использовать значение true для повышения производительности) [true/false]")] public bool IsRemoveCorpse { get; set; }
            [JsonProperty(En ? "Wear items" : "Одежда")] public HashSet<NpcWear> WearItems { get; set; }
            [JsonProperty(En ? "Belt items" : "Быстрые слоты")] public HashSet<NpcBelt> BeltItems { get; set; }
            [JsonProperty(En ? "Kit (it is recommended to use the previous 2 settings to improve performance)" : "Kit (рекомендуется использовать предыдущие 2 пункта настройки для повышения производительности)")] public string Kit { get; set; }
        }

        public class PresetConfig
        {
            [JsonProperty(En ? "Minimum" : "Минимальное кол-во")] public int Min { get; set; }
            [JsonProperty(En ? "Maximum" : "Максимальное кол-во")] public int Max { get; set; }
            [JsonProperty(En ? "List of locations" : "Список расположений")] public HashSet<string> Positions { get; set; }
            [JsonProperty(En ? "NPCs setting" : "Настройки NPC")] public NpcConfig Config { get; set; }
            [JsonProperty(En ? "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - loot table of the Rust objects; 5 - combine the 1 and 4 methods)" : "Какую таблицу предметов необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - таблица предметов объектов Rust; 5 - совместить 1 и 4 методы)")] public int TypeLootTable { get; set; }
            [JsonProperty(En ? "Loot table from prefabs (if the loot table type is 4 or 5)" : "Таблица предметов из prefab-ов (если тип таблицы предметов - 4 или 5)")] public PrefabLootTableConfig PrefabLootTable { get; set; }
            [JsonProperty(En ? "Own loot table (if the loot table type is 1 or 5)" : "Собственная таблица предметов (если тип таблицы предметов - 1 или 5)")] public LootTableConfig OwnLootTable { get; set; }
        }

        public class CoordConfig
        {
            [JsonProperty(En ? "Position" : "Позиция")] public string Position { get; set; }
            [JsonProperty(En ? "Rotation" : "Вращение")] public string Rotation { get; set; }
        }

        private class PluginConfig
        {
            [JsonProperty(En ? "Minimum time between events [sec.]" : "Минимальное время между ивентами [sec.]")] public float MinStartTime { get; set; }
            [JsonProperty(En ? "Maximum time between events [sec.]" : "Максимальное время между ивентами [sec.]")] public float MaxStartTime { get; set; }
            [JsonProperty(En ? "Is active the timer on to start the event? [true/false]" : "Активен ли таймер для запуска ивента? [true/false]")] public bool EnabledTimer { get; set; }
            [JsonProperty(En ? "Duration of the event [sec.]" : "Время проведения ивента [sec.]")] public int FinishTime { get; set; }
            [JsonProperty(En ? "Time before the starting of the event after receiving a chat message [sec.]" : "Время до начала ивента после сообщения в чате [sec.]")] public float PreStartTime { get; set; }
            [JsonProperty(En ? "Notification time until the end of the event [sec.]" : "Время оповещения до окончания ивента [sec.]")] public int PreFinishTime { get; set; }
            [JsonProperty(En ? "Which loot table should the plugin use in the crates? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - loot table of the Rust objects; 5 - combine the 1 and 4 methods)" : "Какую таблицу лута необходимо использовать в ящиках? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - таблица предметов объектов Rust; 5 - совместить 1 и 4 методы)")] public int TypeLootTableCrates { get; set; }
            [JsonProperty(En ? "Crates setting" : "Настройка ящиков")] public HashSet<CrateConfig> DefaultCrates { get; set; }
            [JsonProperty(En ? "Locked Crates setting" : "Настройка заблокированных ящиков")] public HackCrateConfig HackCrate { get; set; }
            [JsonProperty(En ? "NPCs setting at the beginning of the event" : "Настройка NPC в начале ивента")] public HashSet<PresetConfig> NpcStart { get; set; }
            [JsonProperty(En ? "NPCs setting when the train arrives" : "Настройка NPC, когда приезжает поезд")] public HashSet<PresetConfig> NpcTrain { get; set; }
            [JsonProperty(En ? "NPCs setting when the fire extinguishing system is activated" : "Настройка NPC, когда активирована система пожаротушения")] public HashSet<PresetConfig> NpcButton { get; set; }
            [JsonProperty(En ? "Marker configuration on the map" : "Настройка маркера на карте")] public MarkerConfig Marker { get; set; }
            [JsonProperty(En ? "Main marker settings for key event points shown on players screen" : "Настройки основного маркера на экране игрока")] public PointConfig MainPoint { get; set; }
            [JsonProperty(En ? "Additional marker settings for key event points shown on players screen" : "Настройки дополнительного маркера на экране игрока")] public PointConfig AdditionalPoint { get; set; }
            [JsonProperty(En ? "GUI setting" : "Настройки GUI")] public GuiConfig Gui { get; set; }
            [JsonProperty(En ? "Chat setting" : "Настройки чата")] public ChatConfig Chat { get; set; }
            [JsonProperty(En ? "Facepunch Game Tips setting" : "Настройка сообщений Facepunch Game Tip")] public GameTipConfig GameTip { get; set; }
            [JsonProperty(En ? "GUI Announcements setting" : "Настройка GUI Announcements")] public GuiAnnouncementsConfig GuiAnnouncements { get; set; }
            [JsonProperty(En ? "Notify setting" : "Настройка Notify")] public NotifyConfig Notify { get; set; }
            [JsonProperty(En ? "The distance from the event to the player for global alerts (0 - no limit)" : "Расстояние от ивента до игрока для глобальных оповещений (0 - нет ограничений)")] public float DistanceAlerts { get; set; }
            [JsonProperty(En ? "Discord setting (only for users DiscordMessages plugin)" : "Настройка оповещений в Discord (только для тех, кто использует плагин DiscordMessages)")] public DiscordConfig Discord { get; set; }
            [JsonProperty(En ? "Radius of the event zone" : "Радиус зоны ивента")] public float Radius { get; set; }
            [JsonProperty(En ? "Do you create a PVP zone in the event area? (only for users TruePVE plugin) [true/false]" : "Создавать зону PVP в зоне проведения ивента? (только для тех, кто использует плагин TruePVE) [true/false]")] public bool IsCreateZonePvp { get; set; }
            [JsonProperty(En ? "PVE Mode Setting (only for users PveMode plugin)" : "Настройка PVE режима работы плагина (только для тех, кто использует плагин PveMode)")] public PveModeConfig PveMode { get; set; }
            [JsonProperty(En ? "Interrupt the teleport in Power Plant? (only for users NTeleportation plugin) [true/false]" : "Запрещать телепорт на электростанции? (только для тех, кто использует плагин NTeleportation) [true/false]")] public bool NTeleportationInterrupt { get; set; }
            [JsonProperty(En ? "Disable NPCs from the BetterNpc plugin on the monument while the event is on? [true/false]" : "Отключать NPC из плагина BetterNpc на монументе пока проходит ивент? [true/false]")] public bool RemoveBetterNpc { get; set; }
            [JsonProperty(En ? "Economy setting (total values will be added up and rewarded at the end of the event)" : "Настройка экономики (конечное значение суммируется и будет выдано игрокам по окончанию ивента)")] public EconomyConfig Economy { get; set; }
            [JsonProperty(En ? "List of commands banned in the event zone" : "Список команд запрещенных в зоне ивента")] public HashSet<string> Commands { get; set; }
            [JsonProperty(En ? "The CCTV camera" : "Название камеры")] public string Cctv { get; set; }
            [JsonProperty(En ? "Can SAM Site turrets appear in the event zone? [true/false]" : "Должны ли появляться Sam Site турели в зоне ивента? [true/false]")] public bool IsSamSites { get; set; }
            [JsonProperty(En ? "Delayed departure of CH47 after the start of the event [sec.]" : "Задержка вылета CH47 после начала ивента [sec.]")] public float DelayCh47 { get; set; }
            [JsonProperty(En ? "Delayed departure of workcart after the crash of CH47 [sec.]" : "Задержка выезда вагона после крушения CH47 [sec.]")] public float DelayWorkcart { get; set; }
            [JsonProperty(En ? "Flight altitude CH47 [m.]" : "Высота полета CH47 [m.]")] public float HeightCh47 { get; set; }
            [JsonProperty(En ? "The required amount of water in a barrel on the roof of the building to extinguish the fire" : "Необходимое кол-во воды в бочке на крыше, чтобы потушить пожар")] public int WaterAmount { get; set; }
            [JsonProperty(En ? "Configuration version" : "Версия конфигурации")] public VersionNumber PluginVersion { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig
                {
                    MinStartTime = 10800f,
                    MaxStartTime = 10800f,
                    EnabledTimer = true,
                    FinishTime = 3600,
                    PreStartTime = 300f,
                    PreFinishTime = 300,
                    TypeLootTableCrates = 0,
                    DefaultCrates = new HashSet<CrateConfig>
                    {
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_elite.prefab",
                            Position = "(38.271, 15.515, -83.755)",
                            Rotation = "(0, 243.51, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_elite.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinId = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_elite.prefab",
                            Position = "(48.199, 15.517, -72.949)",
                            Rotation = "(0, 310.953, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_elite.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinId = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                            Position = "(47.605, 15.515, -63.634)",
                            Rotation = "(0, 0, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinId = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                            Position = "(47.447, 15.513, -82.216)",
                            Rotation = "(0, 0, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinId = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
                            Position = "(28.779, 18.304, -76.519)",
                            Rotation = "(5.233, 0.302, 8.405)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal_2.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinId = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
                            Position = "(38.1, 15.512, -65.748)",
                            Rotation = "(0, 0, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal_2.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinId = 0, Name = "" } }
                            }
                        }
                    },
                    HackCrate = new HackCrateConfig
                    {
                        Coordinates = new HashSet<CoordConfig>
                        {
                            new CoordConfig { Position = "(28.399, 15.517, -63.13)", Rotation = "(0, 135, 0)" },
                            new CoordConfig { Position = "(43.372, 16.521, -75.476)", Rotation = "(6.397, 1.034, 5.285)" }
                        },
                        UnlockTime = 600f,
                        IncreaseEventTime = true,
                        TypeLootTable = 0,
                        PrefabLootTable = new PrefabLootTableConfig
                        {
                            Min = 1,
                            Max = 1,
                            UseCount = true,
                            Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab" } }
                        },
                        OwnLootTable = new LootTableConfig
                        {
                            Min = 1,
                            Max = 1,
                            UseCount = true,
                            Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinId = 0, Name = "" } }
                        }
                    },
                    NpcStart = new HashSet<PresetConfig>
                    {
                        new PresetConfig
                        {
                            Min = 6,
                            Max = 6,
                            Positions = new HashSet<string>
                            {
                                "(39.3, 0.3, 66.9)",
                                "(34.6, 0.3, 71.3)",
                                "(46.9, 0.5, 73.3)",
                                "(-88, 3.3, -78.1)",
                                "(-95.2, 0.3, -84.8)",
                                "(-96.1, 0.3, -66.0)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Mercenary",
                                Health = 125f,
                                RoamRange = 10f,
                                ChaseRange = 100f,
                                AttackRangeMultiplier = 2.5f,
                                SenseRange = 40f,
                                MemoryDuration = 60f,
                                DamageScale = 1.5f,
                                AimConeScale = 0.8f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 7.5f,
                                DisableRadio = true,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hazmatsuit_scientist", SkinId = 0 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "smg.mp5", Amount = 1, SkinId = 2373921258, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "grenade.f1", Amount = 2, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            TypeLootTable = 5,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig>
                                {
                                    new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinId = 0, Name = "" },
                                    new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinId = 0, Name = "" }
                                }
                            }
                        },
                        new PresetConfig
                        {
                            Min = 11,
                            Max = 11,
                            Positions = new HashSet<string>
                            {
                                "(-22.9, 18.3, 17.2)",
                                "(-22.9, 18.3, 4.4)",
                                "(-32.4, 18.3, 7.9)",
                                "(-41.4, 18.3, 17.0)",
                                "(-25.4, 0.3, -2.0)",
                                "(-42.8, 0.3, -8.1)",
                                "(-12.8, 0.3, 10.7)",
                                "(-26.0, 0.3, 18.3)",
                                "(-27.7, 12.2, 12.6)",
                                "(-42.8, 0.3, 18.6)",
                                "(-32.9, 12.3, 17.8)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Worker",
                                Health = 125f,
                                RoamRange = 8f,
                                ChaseRange = 50f,
                                AttackRangeMultiplier = 2f,
                                SenseRange = 30f,
                                MemoryDuration = 30f,
                                DamageScale = 0.7f,
                                AimConeScale = 2f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 7.5f,
                                DisableRadio = true,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hoodie", SkinId = 2000507925 },
                                    new NpcWear { ShortName = "pants", SkinId = 1987863036 },
                                    new NpcWear { ShortName = "shoes.boots", SkinId = 2006541391 },
                                    new NpcWear { ShortName = "riot.helmet", SkinId = 840685680 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinId = 2006542444 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "pistol.m92", Amount = 1, SkinId = 0, Mods = new HashSet<string> { "weapon.mod.flashlight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 5, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            TypeLootTable = 5,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig>
                                {
                                    new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinId = 0, Name = "" },
                                    new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinId = 0, Name = "" }
                                }
                            }
                        }
                    },
                    NpcTrain = new HashSet<PresetConfig>
                    {
                        new PresetConfig
                        {
                            Min = 3,
                            Max = 3,
                            Positions = new HashSet<string>
                            {
                                "(-59.6, 0.2, 9.0)",
                                "(-68.9, 0.0, -2.4)",
                                "(-65.5, 0.3, 17.0)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Machinist",
                                Health = 200f,
                                RoamRange = 10f,
                                ChaseRange = 50f,
                                AttackRangeMultiplier = 1f,
                                SenseRange = 50f,
                                MemoryDuration = 10f,
                                DamageScale = 2f,
                                AimConeScale = 1f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 7.5f,
                                DisableRadio = true,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hoodie", SkinId = 2000507925 },
                                    new NpcWear { ShortName = "pants", SkinId = 1987863036 },
                                    new NpcWear { ShortName = "shoes.boots", SkinId = 2006541391 },
                                    new NpcWear { ShortName = "riot.helmet", SkinId = 840685680 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinId = 2006542444 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "pistol.m92", Amount = 1, SkinId = 0, Mods = new HashSet<string> { "weapon.mod.flashlight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 5, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "grenade.f1", Amount = 2, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            TypeLootTable = 5,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig>
                                {
                                    new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinId = 0, Name = "" },
                                    new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinId = 0, Name = "" }
                                }
                            }
                        }
                    },
                    NpcButton = new HashSet<PresetConfig>
                    {
                        new PresetConfig
                        {
                            Min = 5,
                            Max = 5,
                            Positions = new HashSet<string>
                            {
                                "(38.0, 0.3, -55.2)",
                                "(56.9, 0.3, -73.6)",
                                "(18.2, 0.3, -71.5)",
                                "(25.6, 0.3, -58.3)",
                                "(48.7, 0.3, -59.1)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "PowerPlantEvent",
                                Health = 200f,
                                RoamRange = 10f,
                                ChaseRange = 100f,
                                AttackRangeMultiplier = 1f,
                                SenseRange = 50f,
                                MemoryDuration = 10f,
                                DamageScale = 2f,
                                AimConeScale = 1f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 7.5f,
                                DisableRadio = false,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hazmatsuit_scientist", SkinId = 0 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.lr300", Amount = 1, SkinId = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "grenade.f1", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            TypeLootTable = 5,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig>
                                {
                                    new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinId = 0, Name = "" },
                                    new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinId = 0, Name = "" }
                                }
                            }
                        }
                    },
                    Marker = new MarkerConfig
                    {
                        Enabled = true,
                        Type = 1,
                        Radius = 0.37967f,
                        Alpha = 0.35f,
                        Color = new ColorConfig { R = 0.81f, G = 0.25f, B = 0.15f },
                        Text = "PowerPlantEvent"
                    },
                    MainPoint = new PointConfig
                    {
                        Enabled = true,
                        Text = "◈",
                        Size = 45,
                        Color = "#CCFF00"
                    },
                    AdditionalPoint = new PointConfig
                    {
                        Enabled = true,
                        Text = "◆",
                        Size = 25,
                        Color = "#FFC700"
                    },
                    Gui = new GuiConfig
                    {
                        IsGui = true,
                        OffsetMinY = "-56"
                    },
                    Chat = new ChatConfig
                    {
                        IsChat = true,
                        Prefix = "[PowerPlantEvent]"
                    },
                    GameTip = new GameTipConfig
                    {
                        IsGameTip = false,
                        Style = 2
                    },
                    GuiAnnouncements = new GuiAnnouncementsConfig
                    {
                        IsGuiAnnouncements = false,
                        BannerColor = "Orange",
                        TextColor = "White",
                        ApiAdjustVPosition = 0.03f
                    },
                    Notify = new NotifyConfig
                    {
                        IsNotify = false,
                        Type = 0
                    },
                    DistanceAlerts = 0f,
                    Discord = new DiscordConfig
                    {
                        IsDiscord = false,
                        WebhookUrl = "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks",
                        EmbedColor = 13516583,
                        Keys = new HashSet<string>
                        {
                            "PreStart",
                            "Start",
                            "PreFinish",
                            "Finish",
                            "CrashCh47",
                            "FinishWorkcart",
                            "Button"
                        }
                    },
                    Radius = 140f,
                    IsCreateZonePvp = false,
                    PveMode = new PveModeConfig
                    {
                        Pve = false,
                        Damage = 500f,
                        ScaleDamage = new Dictionary<string, float> { ["Npc"] = 1f },
                        LootCrate = false,
                        HackCrate = false,
                        LootNpc = false,
                        DamageNpc = false,
                        TargetNpc = false,
                        CanEnter = false,
                        CanEnterCooldownPlayer = true,
                        TimeExitOwner = 300,
                        AlertTime = 60,
                        RestoreUponDeath = true,
                        CooldownOwner = 86400,
                        Darkening = 12
                    },
                    NTeleportationInterrupt = true,
                    RemoveBetterNpc = true,
                    Economy = new EconomyConfig
                    {
                        Plugins = new HashSet<string> { "Economics", "Server Rewards", "IQEconomic" },
                        Min = 0,
                        Crates = new Dictionary<string, double>
                        {
                            ["crate_elite"] = 0.4,
                            ["crate_normal"] = 0.2,
                            ["crate_normal_2"] = 0.1
                        },
                        Npc = 0.3,
                        LockedCrate = 0.5,
                        Button = 0.4,
                        Commands = new HashSet<string>()
                    },
                    Commands = new HashSet<string>
                    {
                        "/remove",
                        "remove.toggle"
                    },
                    Cctv = "PowerPlant",
                    IsSamSites = true,
                    DelayCh47 = 0f,
                    DelayWorkcart = 0f,
                    HeightCh47 = 200f,
                    WaterAmount = 10000,
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion Config

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PreStart"] = "{0} A Chinook brings <color=#738d43>Crates</color> with loot for scientists to the <color=#55aaff>Power Plant</color> in <color=#55aaff>{1}</color>!",
                ["Start"] = "{0} A Chinook <color=#738d43>has flown</color> to grid <color=#55aaff>{1}</color> to get Crates with loot!",
                ["PreFinish"] = "{0} The Power Plant Event <color=#ce3f27>will end</color> in <color=#55aaff>{1}</color>!",
                ["Finish"] = "{0} The Power Plant Event <color=#ce3f27>has concluded</color>!",
                ["CrashCh47"] = "{0} The Chinook failed to control safe flying conditions and <color=#55aaff>crashed</color> into the broken cooling tower at The Power Plant! You need to <color=#55aaff>put out the fire</color> to get access to the Loot up top!\nCCTV: <color=#55aaff>{1}</color>",
                ["StartWorkcart"] = "{0} The Power Plant Workcart <color=#738d43>will arrive</color> soon. You can put out the fire at the power plant using the water on the Workcart.",
                ["FinishWorkcart"] = "{0} The Workcart has arrived. You need to <color=#55aaff>transfer water</color> from the Workcart to the Water Barrel on the top of the building. You need <color=#55aaff>{1}</color> units of water",
                ["FinishWaterBarrel"] = "{0} Water Barrel is <color=#55aaff>full</color> of enough water to put out The Fire. You <color=#55aaff>have to turn on the fire system</color> using the button on the Water Barrel",
                ["NoWaterInBarrel"] = "{0} There is <color=#ce3f27>not enough</color> water to put out the fire. You need <color=#55aaff>{1}</color> units of water",
                ["Button"] = "{0} Fire Sprinklers <color=#738d43>activated</color> by <color=#55aaff>{1}</color>! The Crates in the broken cooling tower of The Power Plant are <color=#738d43>available</color>",
                ["SetOwner"] = "{0} Player <color=#55aaff>{1}</color> <color=#738d43>has received</color> the owner status for the <color=#55aaff>Power Plant Event</color>",
                ["EventActive"] = "{0} This event is active. To finish this event (<color=#55aaff>/ppstop</color>), then (<color=#55aaff>/ppstart</color>) to start the next one!",
                ["EnterPVP"] = "{0} You <color=#ce3f27>have entered</color> the PVP zone, now other players <color=#ce3f27>can damage</color> you!",
                ["ExitPVP"] = "{0} You <color=#738d43>have left</color> the PVP zone, now other players <color=#738d43>cannot damage</color> you!",
                ["NTeleportation"] = "{0} You <color=#ce3f27>cannot</color> teleport into the event zone!",
                ["SendEconomy"] = "{0} You <color=#738d43>have earned</color> <color=#55aaff>{1}</color> points in economics for participating in the event",
                ["NoCommand"] = "{0} You <color=#ce3f27>cannot</color> use this command in the event zone!"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PreStart"] = "{0} Через <color=#55aaff>{1}</color> в локацию <color=#55aaff>Электростанция</color> CH47 <color=#738d43>доставит</color> новое снаряжение для ученых!",
                ["Start"] = "{0} CH47 <color=#738d43>вылетел</color> в квадрат <color=#55aaff>{1}</color>, чтобы доставить снаряжение!",
                ["PreFinish"] = "{0} Ивент на электростанции <color=#ce3f27>закончится</color> через <color=#55aaff>{1}</color>!",
                ["Finish"] = "{0} Ивент на электростанции <color=#ce3f27>закончен</color>!",
                ["CrashCh47"] = "{0} CH47 не справился с управлением и <color=#55aaff>потерпел крушение</color> в градирне электростанции! Необходимо <color=#55aaff>потушить пожар</color>, чтобы получить доступ к снаряжению\nКамера: <color=#55aaff>{1}</color>",
                ["StartWorkcart"] = "{0} В скором времени прибудет поезд с водой, которой вы сможете потушить пожар на электростанции",
                ["FinishWorkcart"] = "{0} Поезд <color=#738d43>прибыл</color>, необходимо <color=#55aaff>переместить воду</color> из поезда в бочку на крыше здания. Необходимо <color=#55aaff>{1}</color> ед. воды",
                ["FinishWaterBarrel"] = "{0} В бочке <color=#738d43>достаточно</color> воды, чтобы потушить пожар. Необходимо <color=#55aaff>включить систему пожаротушения</color> кнопкой на корпусе бочки с водой",
                ["NoWaterInBarrel"] = "{0} В бочке <color=#ce3f27>недостаточно</color> воды, чтобы потушить пожар. Необходимо еще <color=#55aaff>{1}</color> ед. воды",
                ["Button"] = "{0} Режим пожаротушения <color=#738d43>активировал</color> <color=#55aaff>{1}</color>! Ящики в градирне электростанции <color=#738d43>доступны</color>",
                ["SetOwner"] = "{0} Игрок <color=#55aaff>{1}</color> <color=#738d43>получил</color> статус владельца ивента для <color=#55aaff>Power Plant Event</color>",
                ["EventActive"] = "{0} Ивент в данный момент активен, сначала завершите текущий ивент (<color=#55aaff>/ppstop</color>), чтобы начать следующий!",
                ["EnterPVP"] = "{0} Вы <color=#ce3f27>вошли</color> в PVP зону, теперь другие игроки <color=#ce3f27>могут</color> наносить вам урон!",
                ["ExitPVP"] = "{0} Вы <color=#738d43>вышли</color> из PVP зоны, теперь другие игроки <color=#738d43>не могут</color> наносить вам урон!",
                ["NTeleportation"] = "{0} Вы <color=#ce3f27>не можете</color> телепортироваться в зоне ивента!",
                ["SendEconomy"] = "{0} Вы <color=#738d43>получили</color> <color=#55aaff>{1}</color> баллов в экономику за прохождение ивента",
                ["NoCommand"] = "{0} Вы <color=#ce3f27>не можете</color> использовать данную команду в зоне ивента!"
            }, this, "ru");
        }

        private string GetMessage(string langKey, string userId) => lang.GetMessage(langKey, _ins, userId);

        private string GetMessage(string langKey, string userId, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userId) : string.Format(GetMessage(langKey, userId), args);
        #endregion Lang

        #region Oxide Hooks
        private static PowerPlantEvent _ins;

        private void Init()
        {
            _ins = this;
            ToggleHooks(false);
        }

        private void OnServerInitialized()
        {
            if (GetMonument() == null)
            {
                PrintError("The Power Plant location is missing on the map. The plugin cannot be loaded!");
                NextTick(() => Interface.Oxide.UnloadPlugin(Name));
                return;
            }
            CheckAllLootTables();
            ServerMgr.Instance.StartCoroutine(DownloadImages());
            StartTimer();
        }

        private void Unload()
        {
            if (Controller != null) Finish();
            _ins = null;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) return null;
            if (Controller.Entities.Contains(entity) ||
                (entity is CH47Helicopter && entity == Controller.Ch47) ||
                (entity is TrainEngine && entity == Controller.Train) ||
                (entity is NPCShopKeeper && entity == Controller.Conductor)) return true;
            return null;
        }

        private object OnEntityKill(BaseEntity entity)
        {
            if (entity == null || Controller == null) return null;

            HackableLockedCrate hackCrate = entity as HackableLockedCrate;
            if (hackCrate != null && Controller.HackCrates.Contains(hackCrate))
            {
                Controller.HackCrates.Remove(hackCrate);
                return null;
            }

            LootContainer lootContainer = entity as LootContainer;
            if (lootContainer != null && Controller.Crates.Contains(lootContainer))
            {
                Controller.Crates.Remove(lootContainer);
                return null;
            }

            if (!Controller.KillEntities && Controller.Entities.Contains(entity)) return true;

            return null;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!_config.Marker.Enabled || Controller == null || !player.IsPlayer()) return;
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot)) timer.In(2f, () => OnPlayerConnected(player));
            else Controller.UpdateMapMarkers();
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player != null && Controller.Players.Contains(player))
                Controller.ExitPlayer(player);
        }

        private void OnEntityDeath(ScientistNPC npc, HitInfo info)
        {
            if (npc == null || info == null) return;
            BasePlayer attacker = info.InitiatorPlayer;
            if (Controller.Scientists.Contains(npc) && attacker.IsPlayer()) ActionEconomy(attacker.userID, "Npc");
        }

        private object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            if (player == null || entity == null || Controller.Train == null) return null;
            if (player != Controller.Conductor && entity == Controller.Train.mountPoints[0].mountable) return true;
            else return null;
        }

        private Dictionary<ulong, ulong> StartHackCrates { get; } = new Dictionary<ulong, ulong>();

        private object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (player == null || crate == null) return null;
            if (Controller.HackCrates.Contains(crate))
            {
                if (!Controller.IsExtinguish) return true;
                ulong crateId = crate.net.ID.Value;
                if (StartHackCrates.ContainsKey(crateId)) StartHackCrates[crateId] = player.userID;
                else StartHackCrates.Add(crateId, player.userID);
            }
            return null;
        }

        private void OnCrateHack(HackableLockedCrate crate)
        {
            if (crate == null) return;
            ulong crateId = crate.net.ID.Value;
            ulong playerId;
            if (StartHackCrates.TryGetValue(crateId, out playerId))
            {
                StartHackCrates.Remove(crateId);
                if (_config.HackCrate.IncreaseEventTime && Controller.TimeToFinish < (int)_config.HackCrate.UnlockTime) Controller.TimeToFinish += (int)_config.HackCrate.UnlockTime;
                ActionEconomy(playerId, "LockedCrate");
            }
        }

        private object OnButtonPress(PressButton button, BasePlayer player)
        {
            if (button == null || player == null) return null;
            if (button == Controller.Button && Controller.WaterBarrel != null && Controller.TrainWaterBarrel != null)
            {
                if (Controller.WaterBarrel.GetLiquidCount() >= _config.WaterAmount)
                {
                    if (ActivePveMode && PveMode.Call("CanActionEvent", Name, player) != null) return true;
                    Controller.Extinguish();
                    foreach (PresetConfig preset in _config.NpcButton) Controller.SpawnPreset(preset);
                    if (ActivePveMode) PveMode.Call("EventAddScientists", Name, Controller.Scientists.Select(x => x.net.ID.Value));
                    ActionEconomy(player.userID, "Button");
                    AlertToAllPlayers("Button", _config.Chat.Prefix, player.displayName);
                    Unsubscribe("OnButtonPress");
                }
                else if (!Controller.IsExtinguish) AlertToPlayer(player, GetMessage("NoWaterInBarrel", player.UserIDString, _config.Chat.Prefix, _config.WaterAmount - Controller.WaterBarrel.GetLiquidCount()));
            }
            return null;
        }

        private object OnNpcTarget(BaseEntity attacker, NPCShopKeeper victim)
        {
            if (victim == null) return null;
            if (victim == Controller.Conductor) return true;
            else return null;
        }

        private HashSet<ulong> LootableCrates { get; } = new HashSet<ulong>();

        private void OnLootEntity(BasePlayer player, LootContainer container)
        {
            if (!player.IsPlayer() || container == null || LootableCrates.Contains(container.net.ID.Value)) return;
            if (Controller.Crates.Contains(container))
            {
                LootableCrates.Add(container.net.ID.Value);
                ActionEconomy(player.userID, "Crates", container.ShortPrefabName);
            }
        }

        private object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (player != null && Controller.Players.Contains(player))
            {
                command = "/" + command;
                if (_config.Commands.Contains(command.ToLower()))
                {
                    AlertToPlayer(player, GetMessage("NoCommand", player.UserIDString, _config.Chat.Prefix));
                    return true;
                }
            }
            return null;
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.cmd == null) return null;
            BasePlayer player = arg.Player();
            if (player != null && Controller.Players.Contains(player))
            {
                if (_config.Commands.Contains(arg.cmd.Name.ToLower()) || _config.Commands.Contains(arg.cmd.FullName.ToLower()))
                {
                    AlertToPlayer(player, GetMessage("NoCommand", player.UserIDString, _config.Chat.Prefix));
                    return true;
                }
            }
            return null;
        }
        #endregion Oxide Hooks

        #region Controller
        public class Prefab { public string Path; public Vector3 Pos; public Vector3 Rot; }
        internal HashSet<Prefab> Prefabs { get; } = new HashSet<Prefab>
        {
            //sam_static
            new Prefab { Path = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", Pos = new Vector3(-32.231f, 18.25f, 19.184f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", Pos = new Vector3(-21.197f, 18.25f, 11.122f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", Pos = new Vector3(-32.231f, 18.25f, 3.062f), Rot = new Vector3(0f, 180f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", Pos = new Vector3(-43.304f, 18.25f, 11.122f), Rot = new Vector3(0f, 270f, 0f) },
            //waterbarrel
            new Prefab { Path = "assets/prefabs/deployable/liquidbarrel/waterbarrel.prefab", Pos = new Vector3(-38.125f, 18.25f, 12.609f), Rot = new Vector3(0f, 0f, 0f) },
            //button
            new Prefab { Path = "assets/prefabs/deployable/playerioents/button/button.prefab", Pos = new Vector3(-38.766f, 19.761f, 12.606f), Rot = new Vector3(0f, 270f, 0f) },
            //electric.sprinkler.deployed
            new Prefab { Path = "assets/prefabs/deployable/playerioents/sprinkler/electric.sprinkler.deployed.prefab", Pos = new Vector3(31.676f, 18.939f, -56.999f), Rot = new Vector3(296.716f, 150.739f, 186.214f) },
            new Prefab { Path = "assets/prefabs/deployable/playerioents/sprinkler/electric.sprinkler.deployed.prefab", Pos = new Vector3(44.946f, 18.94f, -57.049f), Rot = new Vector3(295.963f, 199.152f, 185.441f) },
            new Prefab { Path = "assets/prefabs/deployable/playerioents/sprinkler/electric.sprinkler.deployed.prefab", Pos = new Vector3(54.255f, 18.945f, -66.452f), Rot = new Vector3(296.042f, 242.526f, 184.222f) },
            new Prefab { Path = "assets/prefabs/deployable/playerioents/sprinkler/electric.sprinkler.deployed.prefab", Pos = new Vector3(54.216f, 18.943f, -79.646f), Rot = new Vector3(296.249f, 297.468f, 176.694f) },
            new Prefab { Path = "assets/prefabs/deployable/playerioents/sprinkler/electric.sprinkler.deployed.prefab", Pos = new Vector3(45.223f, 18.934f, -88.831f), Rot = new Vector3(63.195f, 155.179f, 180f) },
            new Prefab { Path = "assets/prefabs/deployable/playerioents/sprinkler/electric.sprinkler.deployed.prefab", Pos = new Vector3(31.609f, 18.954f, -88.97f), Rot = new Vector3(0.889f, 291.783f, 243.052f) },
            //cctv_deployed
            new Prefab { Path = "assets/prefabs/deployable/cctvcamera/cctv_deployed.prefab", Pos = new Vector3(48.717f, 35.249f, -75.75f), Rot = new Vector3(53.057f, 270f, 0f) }
        };

        internal HashSet<Vector3> Marker { get; } = new HashSet<Vector3>
        {
            new Vector3(48f, 0f, 10f),
            new Vector3(48f, 0f, 8f),
            new Vector3(48f, 0f, 6f),
            new Vector3(48f, 0f, 4f),
            new Vector3(48f, 0f, 2f),
            new Vector3(48f, 0f, 0f),
            new Vector3(48f, 0f, -2f),
            new Vector3(48f, 0f, -4f),
            new Vector3(48f, 0f, -6f),
            new Vector3(46f, 0f, 18f),
            new Vector3(46f, 0f, 16f),
            new Vector3(46f, 0f, 14f),
            new Vector3(46f, 0f, 12f),
            new Vector3(46f, 0f, 10f),
            new Vector3(46f, 0f, 8f),
            new Vector3(46f, 0f, 6f),
            new Vector3(46f, 0f, 4f),
            new Vector3(46f, 0f, 2f),
            new Vector3(46f, 0f, 0f),
            new Vector3(46f, 0f, -2f),
            new Vector3(46f, 0f, -4f),
            new Vector3(46f, 0f, -6f),
            new Vector3(46f, 0f, -8f),
            new Vector3(46f, 0f, -10f),
            new Vector3(46f, 0f, -12f),
            new Vector3(46f, 0f, -14f),
            new Vector3(44f, 0f, 22f),
            new Vector3(44f, 0f, 20f),
            new Vector3(44f, 0f, 18f),
            new Vector3(44f, 0f, 16f),
            new Vector3(44f, 0f, 14f),
            new Vector3(44f, 0f, -10f),
            new Vector3(44f, 0f, -12f),
            new Vector3(44f, 0f, -14f),
            new Vector3(44f, 0f, -16f),
            new Vector3(44f, 0f, -18f),
            new Vector3(42f, 0f, 26f),
            new Vector3(42f, 0f, 24f),
            new Vector3(42f, 0f, 22f),
            new Vector3(42f, 0f, 20f),
            new Vector3(42f, 0f, -16f),
            new Vector3(42f, 0f, -18f),
            new Vector3(42f, 0f, -20f),
            new Vector3(42f, 0f, -22f),
            new Vector3(40f, 0f, 30f),
            new Vector3(40f, 0f, 28f),
            new Vector3(40f, 0f, 26f),
            new Vector3(40f, 0f, 24f),
            new Vector3(40f, 0f, -20f),
            new Vector3(40f, 0f, -22f),
            new Vector3(40f, 0f, -24f),
            new Vector3(40f, 0f, -26f),
            new Vector3(38f, 0f, 32f),
            new Vector3(38f, 0f, 30f),
            new Vector3(38f, 0f, 28f),
            new Vector3(38f, 0f, -24f),
            new Vector3(38f, 0f, -26f),
            new Vector3(38f, 0f, -28f),
            new Vector3(36f, 0f, 34f),
            new Vector3(36f, 0f, 32f),
            new Vector3(36f, 0f, 30f),
            new Vector3(36f, 0f, -26f),
            new Vector3(36f, 0f, -28f),
            new Vector3(36f, 0f, -30f),
            new Vector3(34f, 0f, 36f),
            new Vector3(34f, 0f, 34f),
            new Vector3(34f, 0f, 32f),
            new Vector3(34f, 0f, -28f),
            new Vector3(34f, 0f, -30f),
            new Vector3(34f, 0f, -32f),
            new Vector3(32f, 0f, 38f),
            new Vector3(32f, 0f, 36f),
            new Vector3(32f, 0f, 34f),
            new Vector3(32f, 0f, -32f),
            new Vector3(32f, 0f, -34f),
            new Vector3(30f, 0f, 40f),
            new Vector3(30f, 0f, 38f),
            new Vector3(30f, 0f, 36f),
            new Vector3(30f, 0f, -4f),
            new Vector3(30f, 0f, -6f),
            new Vector3(30f, 0f, -32f),
            new Vector3(30f, 0f, -34f),
            new Vector3(30f, 0f, -36f),
            new Vector3(28f, 0f, 42f),
            new Vector3(28f, 0f, 40f),
            new Vector3(28f, 0f, 38f),
            new Vector3(28f, 0f, 2f),
            new Vector3(28f, 0f, 0f),
            new Vector3(28f, 0f, -2f),
            new Vector3(28f, 0f, -4f),
            new Vector3(28f, 0f, -6f),
            new Vector3(28f, 0f, -8f),
            new Vector3(28f, 0f, -10f),
            new Vector3(28f, 0f, -12f),
            new Vector3(28f, 0f, -34f),
            new Vector3(28f, 0f, -36f),
            new Vector3(28f, 0f, -38f),
            new Vector3(26f, 0f, 42f),
            new Vector3(26f, 0f, 40f),
            new Vector3(26f, 0f, 4f),
            new Vector3(26f, 0f, 2f),
            new Vector3(26f, 0f, 0f),
            new Vector3(26f, 0f, -2f),
            new Vector3(26f, 0f, -4f),
            new Vector3(26f, 0f, -6f),
            new Vector3(26f, 0f, -8f),
            new Vector3(26f, 0f, -10f),
            new Vector3(26f, 0f, -12f),
            new Vector3(26f, 0f, -14f),
            new Vector3(26f, 0f, -36f),
            new Vector3(26f, 0f, -38f),
            new Vector3(24f, 0f, 44f),
            new Vector3(24f, 0f, 42f),
            new Vector3(24f, 0f, 4f),
            new Vector3(24f, 0f, 2f),
            new Vector3(24f, 0f, -14f),
            new Vector3(24f, 0f, -38f),
            new Vector3(24f, 0f, -40f),
            new Vector3(22f, 0f, 44f),
            new Vector3(22f, 0f, 42f),
            new Vector3(22f, 0f, 4f),
            new Vector3(22f, 0f, 2f),
            new Vector3(22f, 0f, -14f),
            new Vector3(22f, 0f, -38f),
            new Vector3(22f, 0f, -40f),
            new Vector3(20f, 0f, 46f),
            new Vector3(20f, 0f, 44f),
            new Vector3(20f, 0f, 4f),
            new Vector3(20f, 0f, 2f),
            new Vector3(20f, 0f, -14f),
            new Vector3(20f, 0f, -40f),
            new Vector3(20f, 0f, -42f),
            new Vector3(18f, 0f, 46f),
            new Vector3(18f, 0f, 44f),
            new Vector3(18f, 0f, 12f),
            new Vector3(18f, 0f, 4f),
            new Vector3(18f, 0f, 2f),
            new Vector3(18f, 0f, -14f),
            new Vector3(18f, 0f, -30f),
            new Vector3(18f, 0f, -40f),
            new Vector3(18f, 0f, -42f),
            new Vector3(16f, 0f, 48f),
            new Vector3(16f, 0f, 46f),
            new Vector3(16f, 0f, 22f),
            new Vector3(16f, 0f, 20f),
            new Vector3(16f, 0f, 18f),
            new Vector3(16f, 0f, 16f),
            new Vector3(16f, 0f, 14f),
            new Vector3(16f, 0f, 12f),
            new Vector3(16f, 0f, 10f),
            new Vector3(16f, 0f, 8f),
            new Vector3(16f, 0f, 6f),
            new Vector3(16f, 0f, 4f),
            new Vector3(16f, 0f, 2f),
            new Vector3(16f, 0f, 0f),
            new Vector3(16f, 0f, -2f),
            new Vector3(16f, 0f, -4f),
            new Vector3(16f, 0f, -6f),
            new Vector3(16f, 0f, -8f),
            new Vector3(16f, 0f, -10f),
            new Vector3(16f, 0f, -12f),
            new Vector3(16f, 0f, -14f),
            new Vector3(16f, 0f, -16f),
            new Vector3(16f, 0f, -18f),
            new Vector3(16f, 0f, -20f),
            new Vector3(16f, 0f, -22f),
            new Vector3(16f, 0f, -24f),
            new Vector3(16f, 0f, -26f),
            new Vector3(16f, 0f, -28f),
            new Vector3(16f, 0f, -30f),
            new Vector3(16f, 0f, -42f),
            new Vector3(16f, 0f, -44f),
            new Vector3(14f, 0f, 48f),
            new Vector3(14f, 0f, 46f),
            new Vector3(14f, 0f, 26f),
            new Vector3(14f, 0f, 24f),
            new Vector3(14f, 0f, 22f),
            new Vector3(14f, 0f, 12f),
            new Vector3(14f, 0f, -30f),
            new Vector3(14f, 0f, -42f),
            new Vector3(14f, 0f, -44f),
            new Vector3(12f, 0f, 48f),
            new Vector3(12f, 0f, 46f),
            new Vector3(12f, 0f, 28f),
            new Vector3(12f, 0f, 26f),
            new Vector3(12f, 0f, 12f),
            new Vector3(12f, 0f, -30f),
            new Vector3(12f, 0f, -42f),
            new Vector3(12f, 0f, -44f),
            new Vector3(10f, 0f, 48f),
            new Vector3(10f, 0f, 46f),
            new Vector3(10f, 0f, 30f),
            new Vector3(10f, 0f, 28f),
            new Vector3(10f, 0f, 12f),
            new Vector3(10f, 0f, -4f),
            new Vector3(10f, 0f, -6f),
            new Vector3(10f, 0f, -30f),
            new Vector3(10f, 0f, -42f),
            new Vector3(10f, 0f, -44f),
            new Vector3(8f, 0f, 50f),
            new Vector3(8f, 0f, 48f),
            new Vector3(8f, 0f, 46f),
            new Vector3(8f, 0f, 30f),
            new Vector3(8f, 0f, 12f),
            new Vector3(8f, 0f, 0f),
            new Vector3(8f, 0f, -2f),
            new Vector3(8f, 0f, -4f),
            new Vector3(8f, 0f, -6f),
            new Vector3(8f, 0f, -8f),
            new Vector3(8f, 0f, -10f),
            new Vector3(8f, 0f, -30f),
            new Vector3(8f, 0f, -42f),
            new Vector3(8f, 0f, -44f),
            new Vector3(8f, 0f, -46f),
            new Vector3(6f, 0f, 50f),
            new Vector3(6f, 0f, 48f),
            new Vector3(6f, 0f, 32f),
            new Vector3(6f, 0f, 30f),
            new Vector3(6f, 0f, 12f),
            new Vector3(6f, 0f, 2f),
            new Vector3(6f, 0f, 0f),
            new Vector3(6f, 0f, -10f),
            new Vector3(6f, 0f, -12f),
            new Vector3(6f, 0f, -30f),
            new Vector3(6f, 0f, -44f),
            new Vector3(6f, 0f, -46f),
            new Vector3(4f, 0f, 50f),
            new Vector3(4f, 0f, 48f),
            new Vector3(4f, 0f, 32f),
            new Vector3(4f, 0f, 30f),
            new Vector3(4f, 0f, 12f),
            new Vector3(4f, 0f, 2f),
            new Vector3(4f, 0f, -12f),
            new Vector3(4f, 0f, -14f),
            new Vector3(4f, 0f, -30f),
            new Vector3(4f, 0f, -44f),
            new Vector3(4f, 0f, -46f),
            new Vector3(2f, 0f, 50f),
            new Vector3(2f, 0f, 48f),
            new Vector3(2f, 0f, 34f),
            new Vector3(2f, 0f, 32f),
            new Vector3(2f, 0f, 12f),
            new Vector3(2f, 0f, 4f),
            new Vector3(2f, 0f, 2f),
            new Vector3(2f, 0f, -12f),
            new Vector3(2f, 0f, -14f),
            new Vector3(2f, 0f, -30f),
            new Vector3(2f, 0f, -44f),
            new Vector3(2f, 0f, -46f),
            new Vector3(0f, 0f, 50f),
            new Vector3(0f, 0f, 48f),
            new Vector3(0f, 0f, 34f),
            new Vector3(0f, 0f, 32f),
            new Vector3(0f, 0f, 12f),
            new Vector3(0f, 0f, 4f),
            new Vector3(0f, 0f, 2f),
            new Vector3(0f, 0f, -14f),
            new Vector3(0f, 0f, -30f),
            new Vector3(0f, 0f, -44f),
            new Vector3(0f, 0f, -46f),
            new Vector3(-2f, 0f, 50f),
            new Vector3(-2f, 0f, 48f),
            new Vector3(-2f, 0f, 32f),
            new Vector3(-2f, 0f, 12f),
            new Vector3(-2f, 0f, 4f),
            new Vector3(-2f, 0f, 2f),
            new Vector3(-2f, 0f, -12f),
            new Vector3(-2f, 0f, -14f),
            new Vector3(-2f, 0f, -30f),
            new Vector3(-2f, 0f, -44f),
            new Vector3(-2f, 0f, -46f),
            new Vector3(-4f, 0f, 50f),
            new Vector3(-4f, 0f, 48f),
            new Vector3(-4f, 0f, 32f),
            new Vector3(-4f, 0f, 30f),
            new Vector3(-4f, 0f, 12f),
            new Vector3(-4f, 0f, 2f),
            new Vector3(-4f, 0f, 0f),
            new Vector3(-4f, 0f, -12f),
            new Vector3(-4f, 0f, -30f),
            new Vector3(-4f, 0f, -44f),
            new Vector3(-4f, 0f, -46f),
            new Vector3(-6f, 0f, 50f),
            new Vector3(-6f, 0f, 48f),
            new Vector3(-6f, 0f, 30f),
            new Vector3(-6f, 0f, 12f),
            new Vector3(-6f, 0f, 0f),
            new Vector3(-6f, 0f, -2f),
            new Vector3(-6f, 0f, -4f),
            new Vector3(-6f, 0f, -6f),
            new Vector3(-6f, 0f, -8f),
            new Vector3(-6f, 0f, -10f),
            new Vector3(-6f, 0f, -12f),
            new Vector3(-6f, 0f, -30f),
            new Vector3(-6f, 0f, -44f),
            new Vector3(-6f, 0f, -46f),
            new Vector3(-8f, 0f, 50f),
            new Vector3(-8f, 0f, 48f),
            new Vector3(-8f, 0f, 46f),
            new Vector3(-8f, 0f, 30f),
            new Vector3(-8f, 0f, 28f),
            new Vector3(-8f, 0f, 12f),
            new Vector3(-8f, 0f, -2f),
            new Vector3(-8f, 0f, -4f),
            new Vector3(-8f, 0f, -6f),
            new Vector3(-8f, 0f, -8f),
            new Vector3(-8f, 0f, -10f),
            new Vector3(-8f, 0f, -30f),
            new Vector3(-8f, 0f, -42f),
            new Vector3(-8f, 0f, -44f),
            new Vector3(-8f, 0f, -46f),
            new Vector3(-10f, 0f, 48f),
            new Vector3(-10f, 0f, 46f),
            new Vector3(-10f, 0f, 28f),
            new Vector3(-10f, 0f, 26f),
            new Vector3(-10f, 0f, 12f),
            new Vector3(-10f, 0f, -30f),
            new Vector3(-10f, 0f, -42f),
            new Vector3(-10f, 0f, -44f),
            new Vector3(-12f, 0f, 48f),
            new Vector3(-12f, 0f, 46f),
            new Vector3(-12f, 0f, 26f),
            new Vector3(-12f, 0f, 24f),
            new Vector3(-12f, 0f, 12f),
            new Vector3(-12f, 0f, -30f),
            new Vector3(-12f, 0f, -42f),
            new Vector3(-12f, 0f, -44f),
            new Vector3(-14f, 0f, 48f),
            new Vector3(-14f, 0f, 46f),
            new Vector3(-14f, 0f, 24f),
            new Vector3(-14f, 0f, 22f),
            new Vector3(-14f, 0f, 20f),
            new Vector3(-14f, 0f, 18f),
            new Vector3(-14f, 0f, 16f),
            new Vector3(-14f, 0f, 14f),
            new Vector3(-14f, 0f, 12f),
            new Vector3(-14f, 0f, 10f),
            new Vector3(-14f, 0f, 8f),
            new Vector3(-14f, 0f, 6f),
            new Vector3(-14f, 0f, 4f),
            new Vector3(-14f, 0f, 2f),
            new Vector3(-14f, 0f, 0f),
            new Vector3(-14f, 0f, -2f),
            new Vector3(-14f, 0f, -4f),
            new Vector3(-14f, 0f, -6f),
            new Vector3(-14f, 0f, -8f),
            new Vector3(-14f, 0f, -10f),
            new Vector3(-14f, 0f, -12f),
            new Vector3(-14f, 0f, -14f),
            new Vector3(-14f, 0f, -16f),
            new Vector3(-14f, 0f, -18f),
            new Vector3(-14f, 0f, -20f),
            new Vector3(-14f, 0f, -22f),
            new Vector3(-14f, 0f, -24f),
            new Vector3(-14f, 0f, -26f),
            new Vector3(-14f, 0f, -28f),
            new Vector3(-14f, 0f, -30f),
            new Vector3(-14f, 0f, -42f),
            new Vector3(-14f, 0f, -44f),
            new Vector3(-16f, 0f, 46f),
            new Vector3(-16f, 0f, 44f),
            new Vector3(-16f, 0f, 20f),
            new Vector3(-16f, 0f, 18f),
            new Vector3(-16f, 0f, 16f),
            new Vector3(-16f, 0f, 14f),
            new Vector3(-16f, 0f, 12f),
            new Vector3(-16f, 0f, 10f),
            new Vector3(-16f, 0f, 8f),
            new Vector3(-16f, 0f, 6f),
            new Vector3(-16f, 0f, 4f),
            new Vector3(-16f, 0f, 2f),
            new Vector3(-16f, 0f, 0f),
            new Vector3(-16f, 0f, -2f),
            new Vector3(-16f, 0f, -4f),
            new Vector3(-16f, 0f, -6f),
            new Vector3(-16f, 0f, -8f),
            new Vector3(-16f, 0f, -10f),
            new Vector3(-16f, 0f, -12f),
            new Vector3(-16f, 0f, -14f),
            new Vector3(-16f, 0f, -16f),
            new Vector3(-16f, 0f, -18f),
            new Vector3(-16f, 0f, -20f),
            new Vector3(-16f, 0f, -22f),
            new Vector3(-16f, 0f, -24f),
            new Vector3(-16f, 0f, -26f),
            new Vector3(-16f, 0f, -28f),
            new Vector3(-16f, 0f, -30f),
            new Vector3(-16f, 0f, -40f),
            new Vector3(-16f, 0f, -42f),
            new Vector3(-18f, 0f, 46f),
            new Vector3(-18f, 0f, 44f),
            new Vector3(-18f, 0f, 12f),
            new Vector3(-18f, 0f, 4f),
            new Vector3(-18f, 0f, 2f),
            new Vector3(-18f, 0f, -14f),
            new Vector3(-18f, 0f, -30f),
            new Vector3(-18f, 0f, -40f),
            new Vector3(-18f, 0f, -42f),
            new Vector3(-20f, 0f, 46f),
            new Vector3(-20f, 0f, 44f),
            new Vector3(-20f, 0f, 42f),
            new Vector3(-20f, 0f, 4f),
            new Vector3(-20f, 0f, 2f),
            new Vector3(-20f, 0f, -14f),
            new Vector3(-20f, 0f, -38f),
            new Vector3(-20f, 0f, -40f),
            new Vector3(-20f, 0f, -42f),
            new Vector3(-22f, 0f, 44f),
            new Vector3(-22f, 0f, 42f),
            new Vector3(-22f, 0f, 4f),
            new Vector3(-22f, 0f, 2f),
            new Vector3(-22f, 0f, -14f),
            new Vector3(-22f, 0f, -38f),
            new Vector3(-22f, 0f, -40f),
            new Vector3(-24f, 0f, 42f),
            new Vector3(-24f, 0f, 40f),
            new Vector3(-24f, 0f, 4f),
            new Vector3(-24f, 0f, 2f),
            new Vector3(-24f, 0f, -12f),
            new Vector3(-24f, 0f, -14f),
            new Vector3(-24f, 0f, -36f),
            new Vector3(-24f, 0f, -38f),
            new Vector3(-26f, 0f, 42f),
            new Vector3(-26f, 0f, 40f),
            new Vector3(-26f, 0f, 2f),
            new Vector3(-26f, 0f, 0f),
            new Vector3(-26f, 0f, -2f),
            new Vector3(-26f, 0f, -4f),
            new Vector3(-26f, 0f, -6f),
            new Vector3(-26f, 0f, -8f),
            new Vector3(-26f, 0f, -10f),
            new Vector3(-26f, 0f, -12f),
            new Vector3(-26f, 0f, -14f),
            new Vector3(-26f, 0f, -36f),
            new Vector3(-26f, 0f, -38f),
            new Vector3(-28f, 0f, 40f),
            new Vector3(-28f, 0f, 38f),
            new Vector3(-28f, 0f, -4f),
            new Vector3(-28f, 0f, -6f),
            new Vector3(-28f, 0f, -8f),
            new Vector3(-28f, 0f, -34f),
            new Vector3(-28f, 0f, -36f),
            new Vector3(-30f, 0f, 38f),
            new Vector3(-30f, 0f, 36f),
            new Vector3(-30f, 0f, -6f),
            new Vector3(-30f, 0f, -32f),
            new Vector3(-30f, 0f, -34f),
            new Vector3(-32f, 0f, 36f),
            new Vector3(-32f, 0f, 34f),
            new Vector3(-32f, 0f, -30f),
            new Vector3(-32f, 0f, -32f),
            new Vector3(-34f, 0f, 34f),
            new Vector3(-34f, 0f, 32f),
            new Vector3(-34f, 0f, -28f),
            new Vector3(-34f, 0f, -30f),
            new Vector3(-36f, 0f, 32f),
            new Vector3(-36f, 0f, 30f),
            new Vector3(-36f, 0f, -26f),
            new Vector3(-36f, 0f, -28f),
            new Vector3(-38f, 0f, 30f),
            new Vector3(-38f, 0f, 28f),
            new Vector3(-38f, 0f, 26f),
            new Vector3(-38f, 0f, -22f),
            new Vector3(-38f, 0f, -24f),
            new Vector3(-38f, 0f, -26f),
            new Vector3(-40f, 0f, 28f),
            new Vector3(-40f, 0f, 26f),
            new Vector3(-40f, 0f, 24f),
            new Vector3(-40f, 0f, 22f),
            new Vector3(-40f, 0f, -18f),
            new Vector3(-40f, 0f, -20f),
            new Vector3(-40f, 0f, -22f),
            new Vector3(-40f, 0f, -24f),
            new Vector3(-42f, 0f, 24f),
            new Vector3(-42f, 0f, 22f),
            new Vector3(-42f, 0f, 20f),
            new Vector3(-42f, 0f, 18f),
            new Vector3(-42f, 0f, -14f),
            new Vector3(-42f, 0f, -16f),
            new Vector3(-42f, 0f, -18f),
            new Vector3(-42f, 0f, -20f),
            new Vector3(-44f, 0f, 20f),
            new Vector3(-44f, 0f, 18f),
            new Vector3(-44f, 0f, 16f),
            new Vector3(-44f, 0f, 14f),
            new Vector3(-44f, 0f, 12f),
            new Vector3(-44f, 0f, 10f),
            new Vector3(-44f, 0f, 8f),
            new Vector3(-44f, 0f, -4f),
            new Vector3(-44f, 0f, -6f),
            new Vector3(-44f, 0f, -8f),
            new Vector3(-44f, 0f, -10f),
            new Vector3(-44f, 0f, -12f),
            new Vector3(-44f, 0f, -14f),
            new Vector3(-44f, 0f, -16f),
            new Vector3(-46f, 0f, 14f),
            new Vector3(-46f, 0f, 12f),
            new Vector3(-46f, 0f, 10f),
            new Vector3(-46f, 0f, 8f),
            new Vector3(-46f, 0f, 6f),
            new Vector3(-46f, 0f, 4f),
            new Vector3(-46f, 0f, 2f),
            new Vector3(-46f, 0f, 0f),
            new Vector3(-46f, 0f, -2f),
            new Vector3(-46f, 0f, -4f),
            new Vector3(-46f, 0f, -6f),
            new Vector3(-46f, 0f, -8f),
            new Vector3(-46f, 0f, -10)
        };

        private ControllerPowerPlantEvent Controller { get; set; } = null;
        private bool Active { get; set; } = false;

        private void StartTimer()
        {
            if (!_config.EnabledTimer) return;
            timer.In(UnityEngine.Random.Range(_config.MinStartTime, _config.MaxStartTime), () =>
            {
                if (!Active) Start(null);
                else Puts("This event is active now. To finish this event (ppstop), then to start the next one");
            });
        }

        private void Start(BasePlayer player)
        {
            if (!PluginExistsForStart("NpcSpawn")) return;
            CheckVersionPlugin();
            Active = true;
            AlertToAllPlayers("PreStart", _config.Chat.Prefix, GetTimeFormat((int)_config.PreStartTime));
            timer.In(_config.PreStartTime, () =>
            {
                Puts($"{Name} has begun");
                if (_config.RemoveBetterNpc && plugins.Exists("BetterNpc")) BetterNpc.Call("DestroyController", "Power Plant");
                ToggleHooks(true);
                Controller = new GameObject().AddComponent<ControllerPowerPlantEvent>();
                if (plugins.Exists("MonumentOwner")) MonumentOwner.Call("RemoveZone", Controller.Monument);
                Controller.EnablePveMode(_config.PveMode, player);
                Interface.Oxide.CallHook($"On{Name}Start", Controller.transform.position, _config.Radius);
                AlertToAllPlayers("Start", _config.Chat.Prefix, MapHelper.PositionToString(Controller.transform.position));
            });
        }

        private void Finish()
        {
            ToggleHooks(false);
            if (ActivePveMode) PveMode.Call("EventRemovePveMode", Name, true);
            if (Controller != null)
            {
                if (plugins.Exists("MonumentOwner")) MonumentOwner.Call("CreateZone", Controller.Monument);
                UnityEngine.Object.Destroy(Controller.gameObject);
            }
            Active = false;
            SendBalance();
            StartHackCrates.Clear();
            LootableCrates.Clear();
            AlertToAllPlayers("Finish", _config.Chat.Prefix);
            Interface.Oxide.CallHook($"On{Name}End");
            if (_config.RemoveBetterNpc && plugins.Exists("BetterNpc")) BetterNpc.Call("CreateController", "Power Plant");
            Puts($"{Name} has ended");
            StartTimer();
        }

        internal class ControllerPowerPlantEvent : FacepunchBehaviour
        {
            private PluginConfig _config => _ins._config;

            internal MonumentInfo Monument { get; set; } = null;

            private SphereCollider SphereCollider { get; set; } = null;

            private VendingMachineMapMarker VendingMarker { get; set; } = null;
            private HashSet<MapMarkerGenericRadius> Markers { get; } = new HashSet<MapMarkerGenericRadius>();

            private Coroutine ProcessCoroutine { get; set; } = null;

            internal CH47Helicopter Ch47 { get; set; } = null;
            private CH47HelicopterAIController Ch47Ai { get; set; } = null;

            internal TrainEngine Train { get; set; } = null;
            internal NPCShopKeeper Conductor { get; set; } = null;
            internal LiquidContainer TrainWaterBarrel { get; set; } = null;
            internal LootContainer TrainCrate { get; set; } = null;

            internal PressButton Button { get; set; } = null;
            internal LiquidContainer WaterBarrel { get; set; } = null;
            internal bool IsExtinguish { get; set; } = false;
            private bool NotificationFinishWaterBarrel { get; set; } = false;

            internal bool KillEntities { get; set; } = false;
            internal HashSet<BaseEntity> Entities { get; } = new HashSet<BaseEntity>();
            private HashSet<Sprinkler> Sprinklers { get; } = new HashSet<Sprinkler>();
            private HashSet<FireBall> FireBalls { get; } = new HashSet<FireBall>();

            internal HashSet<ScientistNPC> Scientists { get; } = new HashSet<ScientistNPC>();

            internal HashSet<LootContainer> Crates { get; } = new HashSet<LootContainer>();
            internal HashSet<HackableLockedCrate> HackCrates { get; } = new HashSet<HackableLockedCrate>();

            internal int TimeToFinish { get; set; } = _ins._config.FinishTime;

            internal HashSet<BasePlayer> Players { get; } = new HashSet<BasePlayer>();
            internal BasePlayer Owner { get; set; } = null;

            private void Awake()
            {
                Monument = _ins.GetMonument();
                transform.position = Monument.transform.position;
                transform.rotation = Monument.transform.rotation;

                gameObject.layer = 3;
                SphereCollider = gameObject.AddComponent<SphereCollider>();
                SphereCollider.isTrigger = true;
                SphereCollider.radius = _config.Radius;

                SpawnEntities();

                foreach (PresetConfig preset in _config.NpcStart) SpawnPreset(preset);

                SpawnMapMarker(_config.Marker);

                ProcessCoroutine = ServerMgr.Instance.StartCoroutine(ProcessEvent());

                InvokeRepeating(InvokeUpdates, 0f, 1f);
            }

            private void OnDestroy()
            {
                if (ProcessCoroutine != null) ServerMgr.Instance.StopCoroutine(ProcessCoroutine);

                CancelInvoke(InvokeUpdates);

                if (SphereCollider != null) Destroy(SphereCollider);

                if (VendingMarker.IsExists()) VendingMarker.Kill();
                foreach (MapMarkerGenericRadius marker in Markers) if (marker.IsExists()) marker.Kill();

                foreach (BasePlayer player in Players) CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");

                foreach (ScientistNPC npc in Scientists) if (npc.IsExists()) npc.Kill();

                foreach (LootContainer crate in Crates) if (crate.IsExists()) crate.Kill();
                foreach (HackableLockedCrate crate in HackCrates) if (crate.IsExists()) crate.Kill();

                foreach (FireBall fireBall in FireBalls) if (fireBall.IsExists()) fireBall.Kill();

                if (Ch47.IsExists()) Ch47.Kill();

                if (Conductor.IsExists()) Conductor.Kill();
                if (TrainCrate.IsExists()) TrainCrate.Kill();
                if (Train.IsExists()) Train.Kill();

                KillEntities = true;
                foreach (BaseEntity entity in Entities) if (entity.IsExists()) entity.Kill();
            }

            private void OnTriggerEnter(Collider other) => EnterPlayer(other.GetComponentInParent<BasePlayer>());

            internal void EnterPlayer(BasePlayer player)
            {
                if (!player.IsPlayer()) return;
                if (Players.Contains(player)) return;
                Players.Add(player);
                Interface.Oxide.CallHook($"OnPlayerEnter{_ins.Name}", player);
                if (_config.IsCreateZonePvp) _ins.AlertToPlayer(player, _ins.GetMessage("EnterPVP", player.UserIDString, _config.Chat.Prefix));
                if (_config.Gui.IsGui) UpdateGui(player);
            }

            private void OnTriggerExit(Collider other) => ExitPlayer(other.GetComponentInParent<BasePlayer>());

            internal void ExitPlayer(BasePlayer player)
            {
                if (!player.IsPlayer()) return;
                if (!Players.Contains(player)) return;
                Players.Remove(player);
                Interface.Oxide.CallHook($"OnPlayerExit{_ins.Name}", player);
                if (_config.IsCreateZonePvp) _ins.AlertToPlayer(player, _ins.GetMessage("ExitPVP", player.UserIDString, _config.Chat.Prefix));
                if (_config.Gui.IsGui) CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");
            }

            private void InvokeUpdates()
            {
                if (_config.Gui.IsGui) foreach (BasePlayer player in Players) UpdateGui(player);
                if (_config.Marker.Enabled) UpdateVendingMarker();
                UpdateMarkerForPlayers();
                UpdateTimeToFinish();
            }

            private void UpdateGui(BasePlayer player)
            {
                Dictionary<string, string> dic = new Dictionary<string, string> { ["Clock_KpucTaJl"] = GetTimeFormat(TimeToFinish) };
                if (IsExtinguish && Crates.Count + HackCrates.Count > 0) dic.Add("Crate_KpucTaJl", $"{Crates.Count + HackCrates.Count}");
                if (Scientists.Count > 0) dic.Add("Npc_KpucTaJl", Scientists.Count.ToString());
                _ins.CreateTabs(player, dic);
            }

            private void SpawnMapMarker(MarkerConfig config)
            {
                if (!config.Enabled) return;

                MapMarkerGenericRadius background = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", transform.position) as MapMarkerGenericRadius;
                background.Spawn();
                background.radius = config.Type == 0 ? config.Radius : 0.37967f;
                background.alpha = config.Alpha;
                background.color1 = new Color(config.Color.R, config.Color.G, config.Color.B);
                background.color2 = new Color(config.Color.R, config.Color.G, config.Color.B);
                Markers.Add(background);

                if (config.Type == 1)
                {
                    foreach (Vector3 pos in _ins.Marker)
                    {
                        MapMarkerGenericRadius marker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", transform.position + pos) as MapMarkerGenericRadius;
                        marker.Spawn();
                        marker.radius = 0.008f;
                        marker.alpha = 1f;
                        marker.color1 = new Color(config.Color.R, config.Color.G, config.Color.B);
                        marker.color2 = new Color(config.Color.R, config.Color.G, config.Color.B);
                        Markers.Add(marker);
                    }
                }

                VendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", transform.position) as VendingMachineMapMarker;
                VendingMarker.Spawn();

                UpdateVendingMarker();
                UpdateMapMarkers();
            }

            private void UpdateVendingMarker()
            {
                VendingMarker.markerShopName = $"{_config.Marker.Text}\n{GetTimeFormat(TimeToFinish)}";
                if (_ins.ActivePveMode) VendingMarker.markerShopName += Owner == null ? "\nNo Owner" : $"\n{Owner.displayName}";
                VendingMarker.SendNetworkUpdate();
            }

            internal void UpdateMapMarkers() { foreach (MapMarkerGenericRadius marker in Markers) marker.SendUpdate(); }

            private void UpdateMarkerForPlayers()
            {
                if (Players.Count == 0) return;

                if (_config.MainPoint.Enabled && !IsExtinguish)
                {
                    HashSet<Vector3> points = new HashSet<Vector3>();
                    if (NotificationFinishWaterBarrel)
                    {
                        if (Button != null) points.Add(Button.transform.position);
                    }
                    else
                    {
                        if (WaterBarrel != null && TrainWaterBarrel != null)
                        {
                            points.Add(WaterBarrel.transform.position);
                            points.Add(TrainWaterBarrel.transform.position);
                        }
                    }
                    if (points.Count > 0) foreach (BasePlayer player in Players) foreach (Vector3 point in points) UpdateMarkerForPlayer(player, point, _config.MainPoint);
                    points = null;
                }

                if (_config.AdditionalPoint.Enabled && IsExtinguish)
                {
                    HashSet<Vector3> points = new HashSet<Vector3>();
                    foreach (LootContainer crate in Crates) if (crate.IsExists()) points.Add(crate.transform.position);
                    foreach (HackableLockedCrate crate in HackCrates) if (crate.IsExists()) points.Add(crate.transform.position);
                    if (points.Count > 0) foreach (BasePlayer player in Players) foreach (Vector3 point in points) UpdateMarkerForPlayer(player, point, _config.AdditionalPoint);
                    points = null;
                }
            }

            private void UpdateTimeToFinish()
            {
                if (IsExtinguish && Crates.Count + HackCrates.Count == 0 && TimeToFinish > _config.PreFinishTime) TimeToFinish = _config.PreFinishTime;
                else TimeToFinish--;

                if (TrainWaterBarrel != null && TrainWaterBarrel.GetLiquidCount() < 20000) TrainWaterBarrel.GetLiquidItem().amount = 20000;

                if (!NotificationFinishWaterBarrel && WaterBarrel != null && TrainWaterBarrel != null && WaterBarrel.GetLiquidCount() >= _config.WaterAmount)
                {
                    _ins.AlertToAllPlayers("FinishWaterBarrel", _config.Chat.Prefix);
                    NotificationFinishWaterBarrel = true;
                }

                if (TimeToFinish == _config.PreFinishTime) _ins.AlertToAllPlayers("PreFinish", _config.Chat.Prefix, GetTimeFormat(_config.PreFinishTime));
                else if (TimeToFinish == 0)
                {
                    CancelInvoke(InvokeUpdates);
                    _ins.Finish();
                }
            }

            private Vector3 GetGlobalPosition(Vector3 localPosition) => transform.TransformPoint(localPosition);

            private Quaternion GetGlobalRotation(Vector3 localRotation) => transform.rotation * Quaternion.Euler(localRotation);

            private void SpawnEntities()
            {
                foreach (Prefab prefab in _ins.Prefabs)
                {
                    if (prefab.Path == "assets/prefabs/npc/sam_site_turret/sam_static.prefab" && !_config.IsSamSites) continue;

                    BaseEntity entity = SpawnEntity(prefab.Path, GetGlobalPosition(prefab.Pos), GetGlobalRotation(prefab.Rot));

                    if (entity is LiquidContainer) WaterBarrel = entity as LiquidContainer;

                    if (entity is PressButton) Button = entity as PressButton;

                    if (entity is Sprinkler) Sprinklers.Add(entity as Sprinkler);

                    if (entity is CCTV_RC)
                    {
                        CCTV_RC cctv = entity as CCTV_RC;
                        cctv.UpdateFromInput(5, 0);
                        cctv.rcIdentifier = _config.Cctv;
                    }

                    Entities.Add(entity);
                }
            }

            private void SpawnCrates()
            {
                foreach (CrateConfig crateConfig in _config.DefaultCrates)
                {
                    Vector3 pos = GetGlobalPosition(crateConfig.Position.ToVector3());
                    LootContainer crate = GameManager.server.CreateEntity(crateConfig.Prefab, pos, GetGlobalRotation(crateConfig.Rotation.ToVector3())) as LootContainer;
                    crate.enableSaving = false;
                    crate.Spawn();
                    Crates.Add(crate);
                    SpawnFireBall("assets/bundled/prefabs/fireball.prefab", pos);
                    crate.SetFlag(BaseEntity.Flags.Locked, true);
                    if (_config.TypeLootTableCrates == 1 || _config.TypeLootTableCrates == 4 || _config.TypeLootTableCrates == 5)
                    {
                        _ins.NextTick(() =>
                        {
                            crate.inventory.ClearItemsContainer();
                            if (_config.TypeLootTableCrates == 4 || _config.TypeLootTableCrates == 5) _ins.AddToContainerPrefab(crate.inventory, crateConfig.PrefabLootTable);
                            if (_config.TypeLootTableCrates == 1 || _config.TypeLootTableCrates == 5) _ins.AddToContainerItem(crate.inventory, crateConfig.OwnLootTable);
                        });
                    }
                }
            }

            private void SpawnHackCrates()
            {
                foreach (CoordConfig coord in _config.HackCrate.Coordinates)
                {
                    Vector3 pos = GetGlobalPosition(coord.Position.ToVector3());

                    HackableLockedCrate hackCrate = GameManager.server.CreateEntity("assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab", pos, GetGlobalRotation(coord.Rotation.ToVector3())) as HackableLockedCrate;
                    hackCrate.enableSaving = false;
                    hackCrate.Spawn();

                    hackCrate.hackSeconds = HackableLockedCrate.requiredHackSeconds - _config.HackCrate.UnlockTime;

                    hackCrate.shouldDecay = false;
                    hackCrate.CancelInvoke(hackCrate.DelayedDestroy);

                    hackCrate.KillMapMarker();

                    HackCrates.Add(hackCrate);

                    SpawnFireBall("assets/bundled/prefabs/fireball.prefab", pos);
                    hackCrate.SetFlag(BaseEntity.Flags.Locked, true);

                    if (_config.HackCrate.TypeLootTable is 1 or 4 or 5)
                    {
                        _ins.NextTick(() =>
                        {
                            hackCrate.inventory.ClearItemsContainer();
                            if (_config.HackCrate.TypeLootTable is 4 or 5) _ins.AddToContainerPrefab(hackCrate.inventory, _config.HackCrate.PrefabLootTable);
                            if (_config.HackCrate.TypeLootTable is 1 or 5) _ins.AddToContainerItem(hackCrate.inventory, _config.HackCrate.OwnLootTable);
                        });
                    }
                }
            }

            internal void SpawnPreset(PresetConfig preset)
            {
                int count = UnityEngine.Random.Range(preset.Min, preset.Max + 1);

                List<Vector3> positions = Pool.Get<List<Vector3>>();
                foreach (string pos in preset.Positions) positions.Add(GetGlobalPosition(pos.ToVector3()));

                JObject config = GetObjectConfig(preset.Config);

                for (int i = 0; i < count; i++)
                {
                    Vector3 pos = positions.GetRandom();
                    positions.Remove(pos);

                    ScientistNPC npc = (ScientistNPC)_ins.NpcSpawn.Call("SpawnNpc", pos, config);

                    Scientists.Add(npc);
                }

                Pool.FreeUnmanaged(ref positions);
            }

            private static JObject GetObjectConfig(NpcConfig config)
            {
                HashSet<string> states = config.Stationary ? new HashSet<string> { "IdleState", "CombatStationaryState" } : new HashSet<string> { "RoamState", "ChaseState", "CombatState" };
                if (config.BeltItems.Any(x => x.ShortName == "rocket.launcher" || x.ShortName == "explosive.timed")) states.Add("RaidState");
                return new JObject
                {
                    ["Name"] = config.Name,
                    ["WearItems"] = new JArray { config.WearItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["SkinID"] = x.SkinId }) },
                    ["BeltItems"] = new JArray { config.BeltItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["Amount"] = x.Amount, ["SkinID"] = x.SkinId, ["Mods"] = new JArray { x.Mods }, ["Ammo"] = x.Ammo }) },
                    ["Kit"] = config.Kit,
                    ["Health"] = config.Health,
                    ["RoamRange"] = config.RoamRange,
                    ["ChaseRange"] = config.ChaseRange,
                    ["SenseRange"] = config.SenseRange,
                    ["ListenRange"] = config.SenseRange / 2f,
                    ["AttackRangeMultiplier"] = config.AttackRangeMultiplier,
                    ["CheckVisionCone"] = config.CheckVisionCone,
                    ["VisionCone"] = config.VisionCone,
                    ["HostileTargetsOnly"] = false,
                    ["DamageScale"] = config.DamageScale,
                    ["TurretDamageScale"] = 0f,
                    ["AimConeScale"] = config.AimConeScale,
                    ["DisableRadio"] = config.DisableRadio,
                    ["CanRunAwayWater"] = true,
                    ["CanSleep"] = false,
                    ["SleepDistance"] = 100f,
                    ["Speed"] = config.Speed,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = string.Empty,
                    ["MemoryDuration"] = config.MemoryDuration,
                    ["States"] = new JArray { states }
                };
            }

            private IEnumerator ProcessEvent()
            {
                yield return CoroutineEx.waitForSeconds(_config.DelayCh47);

                Vector3 targetPos3 = transform.TransformPoint(new Vector3(-40.128f, 0f, -76.298f));
                Vector2 targetPos2 = new Vector2(targetPos3.x, targetPos3.z);
                Vector3 crashPos3 = transform.TransformPoint(new Vector3(38.094f, 0f, -72.874f));
                Vector2 crashPos2 = new Vector2(crashPos3.x, crashPos3.z);

                List<Vector2> list = Pool.Get<List<Vector2>>();
                float size = World.Size / 2f;
                list.Add(new Vector2(-size, -size));
                list.Add(new Vector2(-size, size));
                list.Add(new Vector2(size, -size));
                list.Add(new Vector2(size, size));
                Vector2 spawnPos2 = list.FirstOrDefault(x => Vector2.Distance(x, targetPos2) < Vector2.Distance(x, crashPos2));
                Vector3 spawnPos3 = new Vector3(spawnPos2.x, _config.HeightCh47, spawnPos2.y);
                Pool.FreeUnmanaged(ref list);

                SpawnNewCh47(spawnPos3, Quaternion.identity, new Vector3(targetPos2.x, _config.HeightCh47, targetPos2.y));

                while (Vector2.Distance(new Vector2(Ch47.transform.position.x, Ch47.transform.position.z), targetPos2) > _config.Radius * 3f) yield return CoroutineEx.waitForSeconds(1f);

                SpawnNewCh47(Ch47.transform.position, Ch47.transform.rotation, new Vector3(targetPos2.x, transform.position.y + 16f, targetPos2.y));

                while (Vector2.Distance(new Vector2(Ch47.transform.position.x, Ch47.transform.position.z), targetPos2) > _config.Radius) yield return CoroutineEx.waitForSeconds(1f);

                SpawnNewCh47(Ch47.transform.position, Ch47.transform.rotation, new Vector3(crashPos2.x, transform.position.y + 16f, crashPos2.y));
                Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab", Ch47.transform.position, Vector3.up, null, true);
                Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/component_damged.prefab", Ch47.transform.position, Vector3.up, null, true);
                SpawnFireBall("assets/bundled/prefabs/oilfireballsmall.prefab", new Vector3(-1, 3, 1), Ch47);
                SpawnFireBall("assets/bundled/prefabs/oilfireballsmall.prefab", new Vector3(1, 3, 1), Ch47);

                while (Vector2.Distance(new Vector2(Ch47.transform.position.x, Ch47.transform.position.z), crashPos2) > 15f) yield return CoroutineEx.waitForSeconds(1f);

                foreach (FireBall fireBall in FireBalls) if (fireBall.IsExists()) fireBall.Kill();
                FireBalls.Clear();
                Ch47.Die(new HitInfo(Ch47, Ch47, DamageType.Explosion, 1000f));
                SpawnCrates();
                SpawnHackCrates();
                if (_ins.ActivePveMode)
                {
                    HashSet<ulong> crates = Crates.Select(x => x.net.ID.Value);
                    foreach (HackableLockedCrate crate in HackCrates) crates.Add(crate.net.ID.Value);
                    _ins.PveMode.Call("EventAddCrates", _ins.Name, crates);
                    crates = null;
                }
                SpawnFireBall("assets/bundled/prefabs/fireball.prefab", GetGlobalPosition(new Vector3(38.181f, 15.512f, -57.818f)));
                SpawnFireBall("assets/bundled/prefabs/fireball.prefab", GetGlobalPosition(new Vector3(23.125f, 15.514f, -72.957f)));
                SpawnFireBall("assets/bundled/prefabs/fireball.prefab", GetGlobalPosition(new Vector3(53.493f, 15.511f, -73.002f)));
                _ins.AlertToAllPlayers("CrashCh47", _config.Chat.Prefix, _config.Cctv);

                yield return CoroutineEx.waitForSeconds(_config.DelayWorkcart);

                SpawnWorkcart();
                Vector3 finishTrainPos = GetGlobalPosition(new Vector3(-65.747f, 0.282f, 8.075f));

                while (Vector3.Distance(Train.transform.position, finishTrainPos) > 5f) yield return CoroutineEx.waitForSeconds(1f);

                FinishMoveTrain();
                foreach (PresetConfig preset in _config.NpcTrain) SpawnPreset(preset);
                if (_ins.ActivePveMode) _ins.PveMode.Call("EventAddScientists", _ins.Name, Scientists.Select(x => x.net.ID.Value));
                _ins.AlertToAllPlayers("FinishWorkcart", _config.Chat.Prefix, _config.WaterAmount);

                yield return CoroutineEx.waitForSeconds(2.5f);

                SpawnTrainEntities();
            }

            private void SpawnNewCh47(Vector3 pos, Quaternion rot, Vector3 landingTarget)
            {
                CH47Helicopter ch47New = GameManager.server.CreateEntity("assets/prefabs/npc/ch47/ch47scientists.entity.prefab", pos, rot) as CH47Helicopter;
                CH47HelicopterAIController ch47AInew = ch47New.GetComponent<CH47HelicopterAIController>();

                ch47AInew.SetLandingTarget(landingTarget);

                if (Ch47.IsExists()) Ch47.Kill();

                Ch47 = ch47New;
                Ch47Ai = ch47AInew;

                Ch47.Spawn();
                Ch47Ai.CancelInvoke(Ch47Ai.GetPrivateAction("CheckSpawnScientists"));
                Ch47.rigidBody.detectCollisions = false;
                Ch47Ai.numCrates = 0;
                Ch47Ai.SetMinHoverHeight(0f);
            }

            private void SpawnFireBall(string prefab, Vector3 pos, BaseEntity parent = null)
            {
                FireBall fireBall = GameManager.server.CreateEntity(prefab, parent != null ? parent.transform.position : pos) as FireBall;
                fireBall.enableSaving = false;
                if (parent != null) fireBall.transform.localPosition = pos;
                fireBall.Spawn();
                if (parent != null)
                {
                    fireBall.SetParent(parent, false, true);
                    fireBall.GetComponent<Rigidbody>().isKinematic = true;
                    fireBall.GetComponent<Collider>().enabled = false;
                }
                fireBall.lifeTimeMin = TimeToFinish;
                fireBall.lifeTimeMax = TimeToFinish;
                fireBall.AddLife(TimeToFinish);
                FireBalls.Add(fireBall);
            }

            private void SpawnWorkcart()
            {
                Train = GameManager.server.CreateEntity("assets/content/vehicles/trains/workcart/workcart.entity.prefab", GetGlobalPosition(new Vector3(-27.654f, 0.282f, 116.994f)), GetGlobalRotation(new Vector3(0f, 225f, 0f))) as TrainEngine;
                Train.enableSaving = false;
                Train.Spawn();
                Train.decayingFor = 12000f;
                EntityFuelSystem fuelSystem = Train.GetFuelSystem() as EntityFuelSystem;
                fuelSystem.cachedHasFuel = true;
                fuelSystem.nextFuelCheckTime = float.MaxValue;
                StartMoveTrain();
                _ins.AlertToAllPlayers("StartWorkcart", _config.Chat.Prefix);
            }

            private void SpawnTrainEntities()
            {
                TrainWaterBarrel = GameManager.server.CreateEntity("assets/prefabs/deployable/liquidbarrel/waterbarrel.prefab", Train.transform.TransformPoint(new Vector3(0.853f, 1.422f, -1.196f)), Train.transform.rotation * Quaternion.Euler(new Vector3(0f, 270f, 0f))) as LiquidContainer;
                TrainWaterBarrel.enableSaving = false;
                TrainWaterBarrel.startingAmount = 20000;
                GroundWatch groundWatch = TrainWaterBarrel.GetComponent<GroundWatch>();
                if (groundWatch != null) DestroyImmediate(groundWatch);
                DestroyOnGroundMissing destroyOnGroundMissing = TrainWaterBarrel.GetComponent<DestroyOnGroundMissing>();
                if (destroyOnGroundMissing != null) DestroyImmediate(destroyOnGroundMissing);
                TrainWaterBarrel.Spawn();
                TrainWaterBarrel.pickup.enabled = false;
                Entities.Add(TrainWaterBarrel);

                TrainCrate = GameManager.server.CreateEntity("assets/bundled/prefabs/radtown/underwater_labs/crate_normal.prefab", Train.transform.TransformPoint(new Vector3(0.888f, 2.604f, 0.836f)), Train.transform.rotation * Quaternion.Euler(new Vector3(0f, 270f, 0f))) as LootContainer;
                TrainCrate.enableSaving = false;
                TrainCrate.Spawn();
                _ins.NextTick(() =>
                {
                    TrainCrate.inventory.ClearItemsContainer();
                    for (int i = 0; i < 6; i++)
                    {
                        Item item = ItemManager.CreateByName("waterjug");
                        if (!item.MoveToContainer(TrainCrate.inventory)) item.Remove();
                    }
                });
            }

            private void SpawnConductor()
            {
                Conductor = GameManager.server.CreateEntity("assets/prefabs/npc/bandit/shopkeepers/bandit_shopkeeper.prefab", Train.transform.position, Train.transform.rotation) as NPCShopKeeper;
                Conductor.enableSaving = false;
                Conductor.Spawn();
                Conductor.CancelInvoke(Conductor.Greeting);
                Conductor.CancelInvoke(Conductor.TickMovement);
                Train.mountPoints[0].mountable.AttemptMount(Conductor, false);
            }

            private void StartMoveTrain()
            {
                SpawnConductor();
                Train.engineController.TryStartEngine(Conductor);
                Train.SetThrottle(TrainEngine.EngineSpeeds.Fwd_Lo);
            }

            private void FinishMoveTrain()
            {
                Train.SetThrottle(TrainEngine.EngineSpeeds.Zero);
                Train.engineController.StopEngine();
                if (Conductor.IsExists()) Conductor.Kill();
            }

            internal void Extinguish()
            {
                if (IsExtinguish) return;
                IsExtinguish = true;
                foreach (Sprinkler sprinkler in Sprinklers) sprinkler.SetFlag(BaseEntity.Flags.On, true);
                foreach (FireBall fireBall in FireBalls) if (fireBall.IsExists()) fireBall.Kill();
                foreach (LootContainer crate in Crates) crate.SetFlag(BaseEntity.Flags.Locked, false);
                foreach (HackableLockedCrate crate in HackCrates) crate.SetFlag(BaseEntity.Flags.Locked, false);
                Item item = WaterBarrel.GetLiquidItem();
                if (item.amount == _config.WaterAmount) item.Remove();
                else if (item.amount > _config.WaterAmount)
                {
                    item.amount -= _config.WaterAmount;
                    item.MarkDirty();
                }
            }

            internal void EnablePveMode(PveModeConfig config, BasePlayer player)
            {
                if (!_ins.ActivePveMode) return;

                Dictionary<string, object> dic = new Dictionary<string, object>
                {
                    ["Damage"] = config.Damage,
                    ["ScaleDamage"] = config.ScaleDamage,
                    ["LootCrate"] = config.LootCrate,
                    ["HackCrate"] = config.HackCrate,
                    ["LootNpc"] = config.LootNpc,
                    ["DamageNpc"] = config.DamageNpc,
                    ["DamageTank"] = false,
                    ["DamageHelicopter"] = false,
                    ["DamageTurret"] = false,
                    ["TargetNpc"] = config.TargetNpc,
                    ["TargetTank"] = false,
                    ["TargetHelicopter"] = false,
                    ["TargetTurret"] = false,
                    ["CanEnter"] = config.CanEnter,
                    ["CanEnterCooldownPlayer"] = config.CanEnterCooldownPlayer,
                    ["TimeExitOwner"] = config.TimeExitOwner,
                    ["AlertTime"] = config.AlertTime,
                    ["RestoreUponDeath"] = config.RestoreUponDeath,
                    ["CooldownOwner"] = config.CooldownOwner,
                    ["Darkening"] = config.Darkening
                };

                _ins.PveMode.Call("EventAddPveMode", _ins.Name, dic, transform.position, _config.Radius, new HashSet<ulong>(), Scientists.Select(x => x.net.ID.Value), new HashSet<ulong>(), new HashSet<ulong>(), new HashSet<ulong>(), new HashSet<ulong>(), player);
            }
        }
        #endregion Controller

        #region Find Position
        internal MonumentInfo GetMonument()
        {
            List<MonumentInfo> list = Pool.Get<List<MonumentInfo>>();
            foreach (MonumentInfo monument in TerrainMeta.Path.Monuments)
            {
                if (monument.displayPhrase.english != "Power Plant") continue;
                list.Add(monument);
            }
            MonumentInfo result = list.Count > 0 ? list.GetRandom() : null;
            Pool.FreeUnmanaged(ref list);
            return result;
        }
        #endregion Find Position

        #region Spawn Loot
        #region NPC
        private void OnCorpsePopulate(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (entity == null) return;

            if (!Controller.Scientists.Contains(entity)) return;
            Controller.Scientists.Remove(entity);

            PresetConfig preset = GetPresetConfig(entity.displayName);

            NextTick(() =>
            {
                if (corpse == null) return;
                ItemContainer container = corpse.containers[0];
                if (preset.TypeLootTable == 1 || preset.TypeLootTable == 4 || preset.TypeLootTable == 5)
                {
                    container.ClearItemsContainer();
                    if (preset.TypeLootTable == 4 || preset.TypeLootTable == 5) AddToContainerPrefab(container, preset.PrefabLootTable);
                    if (preset.TypeLootTable == 1 || preset.TypeLootTable == 5) AddToContainerItem(container, preset.OwnLootTable);
                }
                if (preset.Config.IsRemoveCorpse && corpse.IsExists()) corpse.Kill();
            });
        }

        private object CanPopulateLoot(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (entity == null || Controller == null) return null;

            if (!Controller.Scientists.Contains(entity)) return null;

            if (GetPresetConfig(entity.displayName).TypeLootTable == 2) return null;
            else return true;
        }

        private object OnCustomLootNPC(NetworkableId netId)
        {
            if (Controller == null) return null;

            ScientistNPC entity = Controller.Scientists.FirstOrDefault(x => x.IsExists() && x.net.ID.Value == netId.Value);
            if (entity == null) return null;

            if (GetPresetConfig(entity.displayName).TypeLootTable == 3) return null;
            else return true;
        }

        private PresetConfig GetPresetConfig(string name)
        {
            PresetConfig result = _config.NpcStart.FirstOrDefault(x => x.Config.Name == name);
            if (result != null) return result;

            result = _config.NpcTrain.FirstOrDefault(x => x.Config.Name == name);
            if (result != null) return result;

            return _config.NpcButton.FirstOrDefault(x => x.Config.Name == name);
        }
        #endregion NPC

        #region Crates
        private object CanPopulateLoot(LootContainer container)
        {
            if (container == null || Controller == null) return null;
            if (container == Controller.TrainCrate) return true;
            else if (Controller.Crates.Contains(container))
            {
                if (_config.TypeLootTableCrates == 2) return null;
                else return true;
            }
            else if (container is HackableLockedCrate && Controller.HackCrates.Contains(container as HackableLockedCrate))
            {
                if (_config.HackCrate.TypeLootTable == 2) return null;
                else return true;
            }
            else return null;
        }

        private object OnCustomLootContainer(NetworkableId netId)
        {
            if (Controller == null) return null;
            if (Controller.TrainCrate.IsExists() && Controller.TrainCrate.net.ID.Value == netId.Value) return true;
            else if (Controller.Crates.Any(x => x.IsExists() && x.net.ID.Value == netId.Value))
            {
                if (_config.TypeLootTableCrates == 3) return null;
                else return true;
            }
            else if (Controller.HackCrates.Any(x => x.IsExists() && x.net.ID.Value == netId.Value))
            {
                if (_config.HackCrate.TypeLootTable == 3) return null;
                else return true;
            }
            return null;
        }

        private object OnContainerPopulate(LootContainer container)
        {
            if (container == null || Controller == null) return null;
            if (container == Controller.TrainCrate) return true;
            else if (Controller.Crates.Contains(container))
            {
                if (_config.TypeLootTableCrates == 6) return null;
                else return true;
            }
            else if (container is HackableLockedCrate && Controller.HackCrates.Contains(container as HackableLockedCrate))
            {
                if (_config.HackCrate.TypeLootTable == 6) return null;
                else return true;
            }
            else return null;
        }
        #endregion Crates

        private void AddToContainerPrefab(ItemContainer container, PrefabLootTableConfig lootTable)
        {
            if (lootTable.UseCount)
            {
                int count = 0, max = UnityEngine.Random.Range(lootTable.Min, lootTable.Max + 1);
                while (count < max)
                {
                    foreach (PrefabConfig prefab in lootTable.Prefabs)
                    {
                        if (UnityEngine.Random.Range(0f, 100f) > prefab.Chance) continue;
                        SpawnIntoContainer(container, prefab.PrefabDefinition);
                        count++;
                        if (count == max) break;
                    }
                }
            }
            else foreach (PrefabConfig prefab in lootTable.Prefabs) if (UnityEngine.Random.Range(0f, 100f) <= prefab.Chance) SpawnIntoContainer(container, prefab.PrefabDefinition);
        }

        private void SpawnIntoContainer(ItemContainer container, string prefab)
        {
            if (AllLootSpawnSlots.ContainsKey(prefab))
            {
                foreach (LootContainer.LootSpawnSlot lootSpawnSlot in AllLootSpawnSlots[prefab])
                    for (int j = 0; j < lootSpawnSlot.numberToSpawn; j++)
                        if (UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.probability)
                            lootSpawnSlot.definition.SpawnIntoContainer(container);
            }
            else AllLootSpawn[prefab].SpawnIntoContainer(container);
        }

        private void AddToContainerItem(ItemContainer container, LootTableConfig lootTable)
        {
            if (lootTable.UseCount)
            {
                HashSet<int> indexMove = new HashSet<int>();
                int count = UnityEngine.Random.Range(lootTable.Min, lootTable.Max + 1);
                while (indexMove.Count < count)
                {
                    for (int i = 0; i < lootTable.Items.Count; i++)
                    {
                        if (indexMove.Contains(i)) continue;
                        if (SpawnIntoContainer(container, lootTable.Items[i]))
                        {
                            indexMove.Add(i);
                            if (indexMove.Count == count) break;
                        }
                    }
                }
                indexMove = null;
            }
            else foreach (ItemConfig item in lootTable.Items) SpawnIntoContainer(container, item);
        }

        private bool SpawnIntoContainer(ItemContainer container, ItemConfig config)
        {
            if (UnityEngine.Random.Range(0f, 100f) > config.Chance) return false;
            Item item = config.IsBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(config.ShortName, UnityEngine.Random.Range(config.MinAmount, config.MaxAmount + 1), config.SkinId);
            if (item == null)
            {
                PrintWarning($"Failed to create item! ({config.ShortName})");
                return false;
            }
            if (config.IsBluePrint) item.blueprintTarget = ItemManager.FindItemDefinition(config.ShortName).itemid;
            if (!string.IsNullOrEmpty(config.Name)) item.name = config.Name;
            if (container.capacity < container.itemList.Count + 1) container.capacity++;
            if (!item.MoveToContainer(container))
            {
                item.Remove();
                return false;
            }
            return true;
        }

        private void CheckAllLootTables()
        {
            foreach (CrateConfig crateConfig in _config.DefaultCrates)
            {
                CheckLootTable(crateConfig.OwnLootTable);
                CheckPrefabLootTable(crateConfig.PrefabLootTable);
            }

            CheckLootTable(_config.HackCrate.OwnLootTable);
            CheckPrefabLootTable(_config.HackCrate.PrefabLootTable);

            foreach (PresetConfig preset in _config.NpcStart)
            {
                CheckLootTable(preset.OwnLootTable);
                CheckPrefabLootTable(preset.PrefabLootTable);
            }
            foreach (PresetConfig preset in _config.NpcTrain)
            {
                CheckLootTable(preset.OwnLootTable);
                CheckPrefabLootTable(preset.PrefabLootTable);
            }
            foreach (PresetConfig preset in _config.NpcButton)
            {
                CheckLootTable(preset.OwnLootTable);
                CheckPrefabLootTable(preset.PrefabLootTable);
            }

            SaveConfig();
        }

        private void CheckLootTable(LootTableConfig lootTable)
        {
            for (int i = lootTable.Items.Count - 1; i >= 0; i--)
            {
                ItemConfig item = lootTable.Items[i];

                if (!ItemManager.itemList.Any(x => x.shortname == item.ShortName))
                {
                    PrintWarning($"Unknown item removed! ({item.ShortName})");
                    lootTable.Items.Remove(item);
                    continue;
                }
                if (item.Chance <= 0f)
                {
                    PrintWarning($"An item with an incorrect probability has been removed from the loot table ({item.ShortName})");
                    lootTable.Items.Remove(item);
                    continue;
                }

                if (item.MinAmount <= 0) item.MinAmount = 1;
                if (item.MaxAmount < item.MinAmount) item.MaxAmount = item.MinAmount;
            }

            lootTable.Items = lootTable.Items.OrderByQuickSort(x => x.Chance);
            if (lootTable.Items.Any(x => x.Chance >= 100f))
            {
                HashSet<ItemConfig> newItems = new HashSet<ItemConfig>();

                for (int i = lootTable.Items.Count - 1; i >= 0; i--)
                {
                    ItemConfig itemConfig = lootTable.Items[i];
                    if (itemConfig.Chance < 100f) break;
                    newItems.Add(itemConfig);
                    lootTable.Items.Remove(itemConfig);
                }

                int count = newItems.Count;

                if (count > 0)
                {
                    foreach (ItemConfig itemConfig in lootTable.Items) newItems.Add(itemConfig);
                    lootTable.Items.Clear();
                    foreach (ItemConfig itemConfig in newItems) lootTable.Items.Add(itemConfig);
                }

                newItems = null;

                if (lootTable.Min < count) lootTable.Min = count;
                if (lootTable.Max < count) lootTable.Max = count;
            }

            if (lootTable.Max > lootTable.Items.Count) lootTable.Max = lootTable.Items.Count;
            if (lootTable.Min > lootTable.Max) lootTable.Min = lootTable.Max;
            if (lootTable.Items.Count == 0) lootTable.UseCount = false;
        }

        private void CheckPrefabLootTable(PrefabLootTableConfig lootTable)
        {
            HashSet<string> prefabs = new HashSet<string>();

            for (int i = lootTable.Prefabs.Count - 1; i >= 0; i--)
            {
                PrefabConfig prefab = lootTable.Prefabs[i];
                if (prefabs.Any(x => x == prefab.PrefabDefinition))
                {
                    lootTable.Prefabs.Remove(prefab);
                    PrintWarning($"Duplicate prefab removed from loot table! ({prefab.PrefabDefinition})");
                }
                else
                {
                    GameObject gameObject = GameManager.server.FindPrefab(prefab.PrefabDefinition);
                    global::HumanNPC humanNpc = gameObject.GetComponent<global::HumanNPC>();
                    ScarecrowNPC scarecrowNpc = gameObject.GetComponent<ScarecrowNPC>();
                    LootContainer lootContainer = gameObject.GetComponent<LootContainer>();
                    if (humanNpc != null && humanNpc.LootSpawnSlots.Length != 0)
                    {
                        if (!AllLootSpawnSlots.ContainsKey(prefab.PrefabDefinition)) AllLootSpawnSlots.Add(prefab.PrefabDefinition, humanNpc.LootSpawnSlots);
                        prefabs.Add(prefab.PrefabDefinition);
                    }
                    else if (scarecrowNpc != null && scarecrowNpc.LootSpawnSlots.Length != 0)
                    {
                        if (!AllLootSpawnSlots.ContainsKey(prefab.PrefabDefinition)) AllLootSpawnSlots.Add(prefab.PrefabDefinition, scarecrowNpc.LootSpawnSlots);
                        prefabs.Add(prefab.PrefabDefinition);
                    }
                    else if (lootContainer != null && lootContainer.LootSpawnSlots.Length != 0)
                    {
                        if (!AllLootSpawnSlots.ContainsKey(prefab.PrefabDefinition)) AllLootSpawnSlots.Add(prefab.PrefabDefinition, lootContainer.LootSpawnSlots);
                        prefabs.Add(prefab.PrefabDefinition);
                    }
                    else if (lootContainer != null && lootContainer.lootDefinition != null)
                    {
                        if (!AllLootSpawn.ContainsKey(prefab.PrefabDefinition)) AllLootSpawn.Add(prefab.PrefabDefinition, lootContainer.lootDefinition);
                        prefabs.Add(prefab.PrefabDefinition);
                    }
                    else
                    {
                        lootTable.Prefabs.Remove(prefab);
                        PrintWarning($"Unknown prefab removed! ({prefab.PrefabDefinition})");
                    }
                }
            }

            prefabs = null;

            lootTable.Prefabs = lootTable.Prefabs.OrderByQuickSort(x => x.Chance);
            if (lootTable.Prefabs.Any(x => x.Chance >= 100f))
            {
                HashSet<PrefabConfig> newPrefabs = new HashSet<PrefabConfig>();

                for (int i = lootTable.Prefabs.Count - 1; i >= 0; i--)
                {
                    PrefabConfig prefabConfig = lootTable.Prefabs[i];
                    if (prefabConfig.Chance < 100f) break;
                    newPrefabs.Add(prefabConfig);
                    lootTable.Prefabs.Remove(prefabConfig);
                }

                int count = newPrefabs.Count;

                if (count > 0)
                {
                    foreach (PrefabConfig prefabConfig in lootTable.Prefabs) newPrefabs.Add(prefabConfig);
                    lootTable.Prefabs.Clear();
                    foreach (PrefabConfig prefabConfig in newPrefabs) lootTable.Prefabs.Add(prefabConfig);
                }

                newPrefabs = null;

                if (lootTable.Min < count) lootTable.Min = count;
                if (lootTable.Max < count) lootTable.Max = count;
            }

            if (lootTable.Min > lootTable.Max) lootTable.Min = lootTable.Max;
            if (lootTable.Prefabs.Count == 0) lootTable.UseCount = false;
        }

        private Dictionary<string, LootSpawn> AllLootSpawn { get; } = new Dictionary<string, LootSpawn>();
        private Dictionary<string, LootContainer.LootSpawnSlot[]> AllLootSpawnSlots { get; } = new Dictionary<string, LootContainer.LootSpawnSlot[]>();
        #endregion Spawn Loot

        #region PveMode
        [PluginReference] private readonly Plugin PveMode;

        internal bool ActivePveMode => _config.PveMode.Pve && plugins.Exists("PveMode");

        private void SetOwnerPveMode(string shortname, BasePlayer player)
        {
            if (string.IsNullOrEmpty(shortname) || shortname != Name || !player.IsPlayer()) return;
            Controller.Owner = player;
            AlertToAllPlayers("SetOwner", _config.Chat.Prefix, player.displayName);
        }

        private void ClearOwnerPveMode(string shortname)
        {
            if (string.IsNullOrEmpty(shortname) || shortname != Name) return;
            Controller.Owner = null;
        }
        #endregion PveMode

        #region TruePVE
        private object CanEntityTakeDamage(BasePlayer victim, HitInfo hitinfo)
        {
            if (!_config.IsCreateZonePvp || victim == null || hitinfo == null || Controller == null) return null;
            BasePlayer attacker = hitinfo.InitiatorPlayer;
            if (Controller.Players.Contains(victim) && (attacker == null || Controller.Players.Contains(attacker))) return true;
            else return null;
        }
        #endregion TruePVE

        #region NTeleportation
        private object CanTeleport(BasePlayer player, Vector3 to)
        {
            if (_config.NTeleportationInterrupt && Controller != null && (Controller.Players.Contains(player) || Vector3.Distance(Controller.transform.position, to) < _config.Radius)) return GetMessage("NTeleportation", player.UserIDString, _config.Chat.Prefix);
            else return null;
        }

        private void OnPlayerTeleported(BasePlayer player, Vector3 oldPos, Vector3 newPos)
        {
            if (Controller == null || !player.IsPlayer()) return;
            if (!Controller.Players.Contains(player) && Vector3.Distance(Controller.transform.position, newPos) < _config.Radius) Controller.EnterPlayer(player);
            if (Controller.Players.Contains(player) && Vector3.Distance(Controller.transform.position, newPos) > _config.Radius) Controller.ExitPlayer(player);
        }
        #endregion NTeleportation

        #region Economy
        [PluginReference] private readonly Plugin Economics, ServerRewards, IQEconomic, XPerience;

        private Dictionary<ulong, double> PlayersBalance = new Dictionary<ulong, double>();

        private void ActionEconomy(ulong playerId, string type, string arg = "")
        {
            switch (type)
            {
                case "Crates":
                    if (_config.Economy.Crates.ContainsKey(arg)) AddBalance(playerId, _config.Economy.Crates[arg]);
                    break;
                case "Npc":
                    AddBalance(playerId, _config.Economy.Npc);
                    break;
                case "LockedCrate":
                    AddBalance(playerId, _config.Economy.LockedCrate);
                    break;
                case "Button":
                    AddBalance(playerId, _config.Economy.Button);
                    break;
            }
        }

        private void AddBalance(ulong playerId, double balance)
        {
            if (balance == 0) return;
            if (PlayersBalance.ContainsKey(playerId)) PlayersBalance[playerId] += balance;
            else PlayersBalance.Add(playerId, balance);
        }

        private void SendBalance()
        {
            if (PlayersBalance.Count == 0) return;
            if (_config.Economy.Plugins.Count > 0)
            {
                foreach (KeyValuePair<ulong, double> dic in PlayersBalance)
                {
                    if (dic.Value < _config.Economy.Min) continue;
                    int intCount = Convert.ToInt32(dic.Value);
                    if (_config.Economy.Plugins.Contains("Economics") && plugins.Exists("Economics") && dic.Value > 0) Economics.Call("Deposit", dic.Key.ToString(), dic.Value);
                    if (_config.Economy.Plugins.Contains("Server Rewards") && plugins.Exists("ServerRewards") && intCount > 0) ServerRewards.Call("AddPoints", dic.Key, intCount);
                    if (_config.Economy.Plugins.Contains("IQEconomic") && plugins.Exists("IQEconomic") && intCount > 0) IQEconomic.Call("API_SET_BALANCE", dic.Key, intCount);
                    BasePlayer player = BasePlayer.FindByID(dic.Key);
                    if (player != null)
                    {
                        if (_config.Economy.Plugins.Contains("XPerience") && plugins.Exists("XPerience") && dic.Value > 0) XPerience?.Call("GiveXP", player, dic.Value);
                        AlertToPlayer(player, GetMessage("SendEconomy", player.UserIDString, _config.Chat.Prefix, dic.Value));
                    }
                }
            }
            ulong winnerId = PlayersBalance.Max(x => x.Value).Key;
            Interface.Oxide.CallHook($"On{Name}Winner", winnerId);
            foreach (string command in _config.Economy.Commands) Server.Command(command.Replace("{steamid}", $"{winnerId}"));
            PlayersBalance.Clear();
        }
        #endregion Economy

        #region Alerts
        [PluginReference] private readonly Plugin GUIAnnouncements, DiscordMessages, Notify;

        private string ClearColorAndSize(string message)
        {
            message = message.Replace("</color>", string.Empty);
            message = message.Replace("</size>", string.Empty);
            while (message.Contains("<color="))
            {
                int index = message.IndexOf("<color=", StringComparison.Ordinal);
                message = message.Remove(index, message.IndexOf(">", index, StringComparison.Ordinal) - index + 1);
            }
            while (message.Contains("<size="))
            {
                int index = message.IndexOf("<size=", StringComparison.Ordinal);
                message = message.Remove(index, message.IndexOf(">", index, StringComparison.Ordinal) - index + 1);
            }
            if (!string.IsNullOrEmpty(_config.Chat.Prefix)) message = message.Replace(_config.Chat.Prefix + " ", string.Empty);
            return message;
        }

        private bool CanSendDiscordMessage => _config.Discord.IsDiscord && !string.IsNullOrEmpty(_config.Discord.WebhookUrl) && _config.Discord.WebhookUrl != "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

        private void AlertToAllPlayers(string langKey, params object[] args)
        {
            if (CanSendDiscordMessage && _config.Discord.Keys.Contains(langKey))
            {
                object fields = new[] { new { name = Title, value = ClearColorAndSize(GetMessage(langKey, null, args)), inline = false } };
                DiscordMessages?.Call("API_SendFancyMessage", _config.Discord.WebhookUrl, "", _config.Discord.EmbedColor, JsonConvert.SerializeObject(fields), null, this);
            }
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (_config.DistanceAlerts == 0f || Vector3.Distance(player.transform.position, Controller.transform.position) <= _config.DistanceAlerts)
                    AlertToPlayer(player, GetMessage(langKey, player.UserIDString, args));
        }

        private void AlertToPlayer(BasePlayer player, string message)
        {
            if (_config.Chat.IsChat) PrintToChat(player, message);
            if (_config.GameTip.IsGameTip) player.SendConsoleCommand("gametip.showtoast", _config.GameTip.Style, ClearColorAndSize(message), string.Empty);
            if (_config.GuiAnnouncements.IsGuiAnnouncements) GUIAnnouncements?.Call("CreateAnnouncement", ClearColorAndSize(message), _config.GuiAnnouncements.BannerColor, _config.GuiAnnouncements.TextColor, player, _config.GuiAnnouncements.ApiAdjustVPosition);
            if (_config.Notify.IsNotify && plugins.Exists("Notify")) Notify?.Call("SendNotify", player, _config.Notify.Type, ClearColorAndSize(message));
        }
        #endregion Alerts

        #region GUI
        private HashSet<string> Names { get; } = new HashSet<string>
        {
            "Tab_KpucTaJl",
            "Clock_KpucTaJl",
            "Npc_KpucTaJl",
            "Crate_KpucTaJl"
        };
        private Dictionary<string, string> Images { get; } = new Dictionary<string, string>();

        private IEnumerator DownloadImages()
        {
            foreach (string name in Names)
            {
                string url = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + "Images" + Path.DirectorySeparatorChar + name + ".png";
                using (UnityWebRequest unityWebRequest = UnityWebRequestTexture.GetTexture(url))
                {
                    yield return unityWebRequest.SendWebRequest();
                    if (unityWebRequest.result != UnityWebRequest.Result.Success)
                    {
                        PrintError($"Image {name} was not found. Maybe you didn't upload it to the .../oxide/data/Images/ folder");
                        break;
                    }
                    else
                    {
                        Texture2D tex = DownloadHandlerTexture.GetContent(unityWebRequest);
                        Images.Add(name, FileStorage.server.Store(tex.EncodeToPNG(), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString());
                        Puts($"Image {name} download is complete");
                        UnityEngine.Object.DestroyImmediate(tex);
                    }
                }
            }
            if (Images.Count < Names.Count) Interface.Oxide.UnloadPlugin(Name);
        }

        private void CreateTabs(BasePlayer player, Dictionary<string, string> tabs)
        {
            CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");

            CuiElementContainer container = new CuiElementContainer();

            float border = 52.5f + 54.5f * (tabs.Count - 1);
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"{-border} {_config.Gui.OffsetMinY}", OffsetMax = $"{border} {_config.Gui.OffsetMinY + 20}" },
                CursorEnabled = false,
            }, "Under", "Tabs_KpucTaJl");

            int i = 0;

            foreach (KeyValuePair<string, string> dic in tabs)
            {
                i++;
                float xmin = 109f * (i - 1);
                container.Add(new CuiElement
                {
                    Name = $"Tab_{i}_KpucTaJl",
                    Parent = "Tabs_KpucTaJl",
                    Components =
                    {
                        new CuiRawImageComponent { Png = Images["Tab_KpucTaJl"] },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"{xmin} 0", OffsetMax = $"{xmin + 105f} 20" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = $"Tab_{i}_KpucTaJl",
                    Components =
                    {
                        new CuiRawImageComponent { Png = Images[dic.Key] },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "9 3", OffsetMax = "23 17" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = $"Tab_{i}_KpucTaJl",
                    Components =
                    {
                        new CuiTextComponent() { Color = "1 1 1 1", Text = dic.Value, Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "28 0", OffsetMax = "100 20" }
                    }
                });
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion GUI

        #region Helpers
        [PluginReference] private readonly Plugin NpcSpawn, BetterNpc, MonumentOwner;

        private HashSet<string> HooksInsidePlugin { get; } = new HashSet<string>
        {
            "OnEntityTakeDamage",
            "OnEntityKill",
            "OnPlayerConnected",
            "OnPlayerDeath",
            "OnEntityDeath",
            "CanMountEntity",
            "CanHackCrate",
            "OnCrateHack",
            "OnButtonPress",
            "OnNpcTarget",
            "OnLootEntity",
            "OnPlayerCommand",
            "OnServerCommand",
            "OnCorpsePopulate",
            "CanPopulateLoot",
            "OnCustomLootNPC",
            "OnCustomLootContainer",
            "OnContainerPopulate",
            "SetOwnerPveMode",
            "ClearOwnerPveMode",
            "CanEntityTakeDamage",
            "CanTeleport",
            "OnPlayerTeleported"
        };

        private void ToggleHooks(bool subscribe)
        {
            foreach (string hook in HooksInsidePlugin)
            {
                if (subscribe) Subscribe(hook);
                else Unsubscribe(hook);
            }
        }

        private const string StrSec = En ? "sec." : "сек.";
        private const string StrMin = En ? "min." : "мин.";
        private const string StrH = En ? "h." : "ч.";

        private static string GetTimeFormat(int time)
        {
            if (time <= 60) return $"{time} {StrSec}";
            else if (time <= 3600)
            {
                int sec = time % 60;
                int min = (time - sec) / 60;
                return sec == 0 ? $"{min} {StrMin}" : $"{min} {StrMin} {sec} {StrSec}";
            }
            else
            {
                int minSec = time % 3600;
                int hour = (time - minSec) / 3600;
                int sec = minSec % 60;
                int min = (minSec - sec) / 60;
                if (min == 0 && sec == 0) return $"{hour} {StrH}";
                else if (sec == 0) return $"{hour} {StrH} {min} {StrMin}";
                else return $"{hour} {StrH} {min} {StrMin} {sec} {StrSec}";
            }
        }

        private static BaseEntity SpawnEntity(string prefab, Vector3 pos, Quaternion rot)
        {
            BaseEntity entity = GameManager.server.CreateEntity(prefab, pos, rot);
            entity.enableSaving = false;

            GroundWatch groundWatch = entity.GetComponent<GroundWatch>();
            if (groundWatch != null) UnityEngine.Object.DestroyImmediate(groundWatch);

            DestroyOnGroundMissing destroyOnGroundMissing = entity.GetComponent<DestroyOnGroundMissing>();
            if (destroyOnGroundMissing != null) UnityEngine.Object.DestroyImmediate(destroyOnGroundMissing);

            entity.Spawn();

            if (entity is StabilityEntity) (entity as StabilityEntity).grounded = true;
            if (entity is BaseCombatEntity) (entity as BaseCombatEntity).pickup.enabled = false;

            return entity;
        }

        private static void UpdateMarkerForPlayer(BasePlayer player, Vector3 pos, PointConfig config)
        {
            if (player == null || player.IsSleeping()) return;
            bool isAdmin = player.IsAdmin;
            if (!isAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                player.SendNetworkUpdateImmediate();
            }
            try
            {
                player.SendConsoleCommand("ddraw.text", 1f, Color.white, pos, $"<size={config.Size}><color={config.Color}>{config.Text}</color></size>");
            }
            finally
            {
                if (!isAdmin)
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    player.SendNetworkUpdateImmediate();
                }
            }
        }

        private void CheckVersionPlugin()
        {
            webrequest.Enqueue("http://37.153.157.216:5000/Api/GetPluginVersions?pluginName=PowerPlantEvent", null, (code, response) =>
            {
                if (code != 200 || string.IsNullOrEmpty(response)) return;
                string[] array = response.Replace("\"", string.Empty).Split('.');
                VersionNumber latestVersion = new VersionNumber(Convert.ToInt32(array[0]), Convert.ToInt32(array[1]), Convert.ToInt32(array[2]));
                if (Version < latestVersion) PrintWarning($"A new version ({latestVersion}) of the plugin is available! You need to update the plugin:\n- https://lone.design/product/power-plant-event\n- https://codefling.com/plugins/power-plant-event");
            }, this);
        }

        private bool PluginExistsForStart(string pluginName)
        {
            if (plugins.Exists(pluginName)) return true;
            PrintError($"{pluginName} plugin doesn`t exist! (https://drive.google.com/drive/folders/1-18L-mG7yiGxR-PQYvd11VvXC2RQ4ZCu?usp=sharing)");
            Interface.Oxide.UnloadPlugin(Name);
            return false;
        }
        #endregion Helpers

        #region Commands
        [ChatCommand("ppstart")]
        private void ChatStartEvent(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (!Active) Start(null);
                else PrintToChat(player, GetMessage("EventActive", player.UserIDString, _config.Chat.Prefix));
            }
        }

        [ChatCommand("ppstop")]
        private void ChatStopEvent(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (Controller != null) Finish();
                else Interface.Oxide.ReloadPlugin(Name);
            }
        }

        [ChatCommand("pppos")]
        private void ChatCommandPos(BasePlayer player)
        {
            if (!player.IsAdmin || Controller == null) return;
            Vector3 pos = Controller.transform.InverseTransformPoint(player.transform.position);
            Puts($"Position: {pos}");
            PrintToChat(player, $"Position: {pos}");
        }

        [ConsoleCommand("ppstart")]
        private void ConsoleStartEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;
            if (!Active)
            {
                if (arg.Args == null || arg.Args.Length != 1)
                {
                    Start(null);
                    return;
                }
                ulong steamId = Convert.ToUInt64(arg.Args[0]);
                BasePlayer target = BasePlayer.FindByID(steamId);
                if (target == null)
                {
                    Start(null);
                    Puts($"Player with SteamID {steamId} not found!");
                    return;
                }
                Start(target);
            }
            else Puts("This event is active now. To finish this event (ppstop), then to start the next one");
        }

        [ConsoleCommand("ppstop")]
        private void ConsoleStopEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                if (Controller != null) Finish();
                else Interface.Oxide.ReloadPlugin(Name);
            }
        }
        #endregion Commands
    }
}

namespace Oxide.Plugins.PowerPlantEventExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return true;
            return false;
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return enumerator.Current;
            return default(TSource);
        }

        public static HashSet<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> predicate)
        {
            HashSet<TResult> result = new HashSet<TResult>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(predicate(enumerator.Current));
            return result;
        }

        public static TSource Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double> predicate)
        {
            TSource result = default(TSource);
            double resultValue = double.MinValue;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TSource element = enumerator.Current;
                    double elementValue = predicate(element);
                    if (elementValue > resultValue)
                    {
                        result = element;
                        resultValue = elementValue;
                    }
                }
            }
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

        public static void KillMapMarker(this HackableLockedCrate crate)
        {
            if (!crate.mapMarkerInstance.IsExists()) return;
            crate.mapMarkerInstance.Kill();
            crate.mapMarkerInstance = null;
        }

        public static Action GetPrivateAction(this object obj, string methodName)
        {
            MethodInfo mi = obj.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi != null) return (Action)Delegate.CreateDelegate(typeof(Action), obj, mi);
            else return null;
        }
    }
}