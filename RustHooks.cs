// Init
// Called when a plugin is being initialized
// Other plugins may or may not be present, dependant on load order
// No return behavior
void Init()
{
    Puts("Init works!");
}

// OnServerRestartInterrupt
// Called when a server restart is being cancelled
// Returning a non-null value overrides default behavior
object OnServerRestartInterrupt()
{
    Puts("OnServerRestartInterrupt works!");
    return null;
}

// OnServerShutdown
// Useful for saving something / etc on server shutdown
// No return behavior
void OnServerShutdown()
{
    Puts("OnServerShutdown works!");
}

// OnServerCommand
// Useful for intercepting commands before they get to their intended target
// Returning a non-null value overrides default behavior
object OnServerCommand(ConsoleSystem.Arg arg)
{
    Puts("OnServerCommand works!");
    return null;
}

// OnMessagePlayer
// Useful for intercepting server messages before they get to their intended target
// Returning a non-null value overrides default behavior
object OnMessagePlayer(string message, BasePlayer player)
{
    Puts("OnMessagePlayer works!");
    return null;
}

// OnFrame
// Called each frame
// No return behavior
void OnFrame()
{
    Puts("OnFrame works!");
}

// OnServerInformationUpdated
// Called after all steam information for the server has has been updated
// No return behavior
void OnServerInformationUpdated()
{
    Puts("OnServerInformationUpdated works!");
}

// OnRconCommand
// Called when an RCON command is run
// No return behavior
void OnRconCommand(IPAddress ip, string command, string[] args)
{
    Puts("OnRconCommand works!");
}

// OnRconConnection
// Called when a new RCON connection is opened
// Returning a non-null value overrides default behavior
object OnRconConnection(IPAddress ip)
{
    Puts("OnRconConnection works!");
    return null;
}

// OnPluginLoaded
// Called when any plugin has been loaded
// No return behavior
// Not to be confused with Loaded
void OnPluginLoaded(Plugin plugin)
{
    Puts($"Plugin '{plugin.Name}' has been loaded");
}

// OnNewSave
// Called when a new savefile is created (usually when map has wiped)
// No return behavior
void OnNewSave(string filename)
{
    Puts("OnNewSave works!");
}

// OnSaveLoad
// Called when a save file is loaded
// Returning a non-null value overrides default behavior
object OnSaveLoad(Dictionary<BaseEntity, ProtoBuf.Entity> entities)
{
    Puts("OnSaveLoad works!");
    return null;
}

// OnPluginUnloaded
// Called when any plugin has been unloaded
// No return behavior
// Not to be confused with Unload
void OnPluginUnloaded(Plugin plugin)
{
    Puts($"Plugin '{plugin.Name}' has been unloaded");
}

// OnServerMessage
// Called before a SERVER message is sent to a player
// Return a non-null value to stop message from being sent
object OnServerMessage(string message, string playerName, string color, ulong playerId)
{
    if (message.Contains("gave"))
    {
        Puts($"Message to {playerName} ({playerId}) cancelled");
        return false;
    }

    return null;
}

// OnServerInitialized
// Called after the server startup has been completed and is awaiting connections
// Also called for plugins that are hotloaded while the server is already started running
// Boolean parameter, false if called on plugin hotload and true if called on server initialization
// No return behavior
void OnServerInitialized(bool initial)
{
    Puts("OnServerInitialized works!");
}

// OnTick
// Called every tick (defined by the tick rate of the server)
// For better performance, avoid using heavy calculations in this hook.
// No return behavior
void OnTick()
{
    Puts("OnTick works!");
}

// OnServerSave
// Called before the server saves
// No return behavior
void OnServerSave()
{
    Puts("OnServerSave works!");
}

// Player Hooks

// CanAffordUpgrade
// Called when the resources for an upgrade are checked
// Returning true or false overrides default behavior
bool CanAffordUpgrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
{
    Puts("CanAffordUpgrade works!");
    return true;
}

// CanAssignBed
// Called when a player attempts to assign a bed or sleeping bag to another player
// Returning a non-null value overrides default behavior
object CanAssignBed(BasePlayer player, SleepingBag bag, ulong targetPlayerId)
{
    Puts("CanAssignBed works!");
    return null;
}

// CanUpdateSign
// Called when the player attempts to change the text on a sign or lock it, or update a photo frame
// Returning true or false overrides default behavior
bool CanUpdateSign(BasePlayer player, Signage sign)
{
    Puts("CanUpdateSign works!");
    return true;
}

bool CanUpdateSign(BasePlayer player, PhotoFrame photoFrame)
{
    Puts("CanUpdateSign works!");
    return true;
}

// OnUserChat
// Called when a player sends a chat message to the server
// Returning true overrides default behavior of chat, not commands
object OnUserChat(IPlayer player, string message)
{
    Puts($"{player.Name} said: {message}");
    return null;
}

// OnPlayerCommand
// Useful for intercepting players' commands before their handling
// Returning a non-null value overrides default behavior
object OnPlayerCommand(BasePlayer player, string command, string[] args)
{
    Puts("OnPlayerCommand works!");
    return null;
}

// OnUserCommand
// Useful for intercepting players' commands before their handling
// Returning a non-null value overrides default behavior
object OnUserCommand(IPlayer player, string command, string[] args)
{
    Puts("OnUserCommand works!");
    return null;
}

// OnPlayerRevive
// Called before the recover after reviving with a medical tool
// Useful for canceling the reviving
// Returning a non-null value cancels default behavior
object OnPlayerRevive(BasePlayer reviver, BasePlayer player)
{
    Puts($"{reviver.displayName} revived {player.displayName}");
    return null;
}

// CanLock
// Useful for canceling the lock action
// Returning a non-null value cancels default behavior
object CanLock(BasePlayer player, BaseLock baseLock)
{
    Puts("CanLock works!");
    return null;
}

// OnMeleeAttack
// Useful for canceling melee attacks
// Returning a non-null value cancels default behavior
object OnMeleeAttack(BasePlayer player, HitInfo info)
{
    Puts("OnMeleeAttack works!");
    return null;
}

// OnPlayerRecovered
// Called when the player was recovered
// No return behavior
void OnPlayerRecovered(BasePlayer player)
{
    Puts("OnPlayerRecovered works!");
}

// CanPushBoat
// Useful for canceling boat push
// Returning a non-null value cancels default behavior
object CanPushBoat(BasePlayer player, MotorRowboat boat)
{
    Puts("CanPushBoat works!");
    return null;
}

// CanDeployItem
// Useful for denying items' deployment
// Returning a non-null value cancels default behavior
object CanDeployItem(BasePlayer player, Deployer deployer, NetworkableId entityId)
{
    Puts("CanDeployItem works!");
    return null;
}

// OnPlayerAssist
// Called when a player tries to assist target player (when target is wounded)
// Returning a non-null value cancels default behavior
object OnPlayerAssist(BasePlayer target, BasePlayer player)
{
    Puts("OnPlayerAssist works!");
    return null;
}

// OnPlayerSetInfo
// Called when setting player's information (aka console variables)
// No return behavior
void OnPlayerSetInfo(Connection connection, string key, string value)
{
    Puts($"{connection.userid}: {key} was set to {value}");
}

// CanSpectateTarget
// Called when spectate target is attempting to update
// Returning a non-null value cancels default behavior
object CanSpectateTarget(BasePlayer player, string filter)
{
    Puts($"{player.displayName} tries to spectate with a filter: {filter}");
    return null;
}

// OnActiveItemChange
// Called when active item is attempting to update
// Returning a non-null value cancels default behavior
object OnActiveItemChange(BasePlayer player, Item oldItem, ItemId newItemId)
{
    Puts("OnActiveItemChange works!");
    return null;
}

// OnActiveItemChanged
// Called when active item was changed
// No return behavior
void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
{
    Puts("OnActiveItemChanged works!");
}

// OnPayForUpgrade
// Called when player is paying for an upgrade. Useful for preventing paying for block upgrade
// Returning a non-null value cancels default behavior
object OnPayForUpgrade(BasePlayer player, BuildingBlock block, ConstructionGrade gradeTarget)
{
    Puts("OnPayForUpgrade works!");
    return null;
}

// OnMapMarkerRemove
// Called when trying to remove a marker
// Returning a non-null value cancels default behaviour
object OnMapMarkerRemove(BasePlayer player, MapNote note)
{
    Puts("OnMapMarkerRemove works!");
    return null;
}

// OnMapMarkerAdd
// Called when trying to add a marker
// Returning a non-null value cancels default behavior
object OnMapMarkerAdd(BasePlayer player, MapNote note)
{
    Puts("OnMapMarkerAdd works!");
    return null;
}

// OnMapMarkerAdded
// Called after a marker was added
// No return behavior
void OnMapMarkerAdded(BasePlayer player, MapNote note)
{
    Puts("OnMapMarkerAdded works!");
}

// OnMapMarkersClear
// Called when trying to clear map markers
// Returning a non-null value cancels default behavior
object OnMapMarkersClear(BasePlayer player, List<MapNote> notes)
{
    Puts("OnMapMarkersClear works!");
    return null;
}

// OnMapMarkersCleared
// Called after markers were cleared
// No return behavior
void OnMapMarkersCleared(BasePlayer player, List<MapNote> notes)
{
    Puts("OnMapMarkersCleared works!");
}

// OnPayForPlacement
// Called when a player is paying for placement. Useful for preventing paying for placing deployables, building blocks and etc
// Returning a non-null value cancels default behavior
object OnPayForPlacement(BasePlayer player, Planner planner, Construction construction)
{
    Puts("OnPayForPlacement works!");
    return null;
}

// CanAffordToPlace
// Useful for ignoring resource requirements for placement
// Returning true or false overrides default behavior
bool CanAffordToPlace(BasePlayer player, Planner planner, Construction construction)
{
    Puts("CanAffordToPlace works!");
    return false;
}

// OnPlayerKeepAlive
// Called before a player is kept alive (Example: You started "helping" player, it keeps him alive for at least 10 seconds more to be sure he won't die until you finish picking him up)
// Returning a non-null value cancels default behavior
object OnPlayerKeepAlive(BasePlayer player, BasePlayer target)
{
    Puts("OnPlayerKeepAlive works!");
    return null;
}

// OnSendCommand
// Called before a command is sent from the server to a player (or a group of players)
// Usually commands aren't sent to a group of players, so in most cases it's safe to use only OnSendCommand with a single Connection.
// Returning a non-null value overwrites command arguments
object OnSendCommand(List<Connection> connections, string command, object[] args)
{
    Puts("OnSendCommand works!");
    return null;
}

object OnSendCommand(Connection connection, string command, object[] args)
{
    Puts("OnSendCommand works!");
    return null;
}

// OnBroadcastCommand
// Called before a command is broadcasted to all connected clients
// Returning a non-null value overwrites command arguments
object OnBroadcastCommand(string command, object[] args)
{
    Puts("OnBroadcastCommand works!");
    return null;
}

// OnUserRespawn
// Called when a player is respawning
// No return behavior
void OnUserRespawn(IPlayer player)
{
    Puts("OnUserRespawn works!");
}

// OnUserRespawned
// Called after a player has respawned
// No return behavior
void OnUserRespawned(IPlayer player)
{
    Puts("OnUserRespawned works!");
}

// OnPlayerReported
// Called when a player has reported someone via F7
// No return behavior
void OnPlayerReported(BasePlayer reporter, string targetName, string targetId, string subject, string message, string type)
{
    Puts($"{reporter.displayName} reported {targetName} for {subject}.");
}

// OnPlayerCorpse
// Called when a non-null corpse has just been spawned
// No return behavior
void OnPlayerCorpse(BasePlayer player, BaseCorpse corpse)
{
    Puts($"A corpse for {player.displayName} has just been spawned!");
}

// CanUseWires
// Useful for allowing or preventing a player from using wires
// Returning a non-null value overrides default behavior
object CanUseWires(BasePlayer player)
{
    Puts($"{player.displayName} has just tried to use wires");
    return null;
}

// OnPlayerCorpseSpawn
// Called when a non-null corpse is about to spawn
// Returning a non-null value overrides default behavior
object OnPlayerCorpseSpawn(BasePlayer player)
{
    Puts("OnPlayerCorpseSpawn works!");
    return null;
}

// OnPlayerCorpseSpawned
// Called when a non-null corpse has just been spawned
// No return behavior
void OnPlayerCorpseSpawned(BasePlayer player, PlayerCorpse corpse)
{
    Puts("OnPlayerCorpseSpawned works!");
}

// OnUserConnected
// Called after a player has been approved and has connected to the server
// No return behavior
void OnUserConnected(IPlayer player)
{
    Puts($"{player.Name} ({player.Id}) connected from {player.Address}");

    if (player.IsAdmin)
    {
        Puts($"{player.Name} ({player.Id}) is admin");
    }

    Puts($"{player.Name} is {(player.IsBanned ? "banned" : "not banned")}");

    server.Broadcast($"Welcome {player.Name} to {server.Name}!");
}

// OnUserDisconnected
// Called after a player has disconnected from the server
// No return behavior
void OnUserDisconnected(IPlayer player)
{
    Puts($"{player.Name} ({player.Id}) disconnected");
}

// CanBeTargeted
// Called when an autoturret, flame turret, shotgun trap, or helicopter turret is attempting to target the player
// Returning true or false overrides default behavior
bool CanBeTargeted(BaseCombatEntity player, MonoBehaviour behaviour)
{
    Puts("CanBeTargeted works!");
    return true;
}

// CanBeWounded
// Called when any damage is attempted on player
// Returning true or false overrides default behavior
bool CanBeWounded(BasePlayer player, HitInfo info)
{
    Puts("CanBeWounded works!");
    return true;
}

// CanBuild
// Called when the player tries to build something
// Returning a non-null value overrides default behavior
object CanBuild(Planner planner, Construction prefab, Construction.Target target)
{
    Puts("CanBuild works!");
    return null;
}

// CanBypassQueue
// Called before the player is added to the connection queue
// Returning true will bypass the queue, returning nothing will by default queue the player
bool CanBypassQueue(Network.Connection connection)
{
    Puts("CanBypassQueue works!");
    return true;
}

// CanChangeCode
// Called when a player tries to change the code on a codelock
// Returning a non-null value overrides default behavior
object CanChangeCode(BasePlayer player, CodeLock codeLock, string newCode, bool isGuestCode)
{
    Puts("CanChangeCode works!");
    return null;
}

// CanChangeGrade
// Called when a player tries to change a building grade
// Returning true or false overrides default behavior
bool CanChangeGrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
{
    Puts("CanChangeGrade works!");
    return true;
}

// CanCraft
// Called when the player attempts to craft an item
// Returning true or false overrides default behavior
bool CanCraft(ItemCrafter itemCrafter, ItemBlueprint bp, int amount)
{
    Puts("CanCraft works!");
    return true;
}

bool CanCraft(PlayerBlueprints playerBlueprints, ItemDefinition itemDefinition, int skinItemId)
{
    Puts("CanCraft works!");
    return true;
}

// CanClientLogin
// Called when the player is attempting to login
// Returning a string will use the string as the error message
// Returning true allows the connection, returning nothing will by default allow the connection, returning anything else will reject it with an error message
object CanClientLogin(Network.Connection connection)
{
    Puts("CanClientLogin works!");
    return true;
}

// CanUserLogin
// Called when a player is attempting to connect to the server
// Returning a string will kick the user with this reason. Returning a non-null value overrides default behavior
object CanUserLogin(string name, string id, string ipAddress)
{
    if (name.ToLower().Contains("admin"))
    {
        Puts($"{name} ({id}) at {ipAddress} tried to connect with 'admin' in name");
        return "Sorry, your name cannot have 'admin' in it";
    }

    return true;
}

// OnUserApproved
// Called after a player is approved to connect to the server
// No return behavior
void OnUserApproved(string name, string id, string ipAddress)
{
    Puts($"{name} ({id}) at {ipAddress} has been approved to connect");
}

// CanDemolish
// Called when a player tries to demolish a building block
// Returning true or false overrides default behavior
bool CanDemolish(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
{
    Puts("CanDemolish works!");
    return true;
}

// CanHackCrate
// Called when a player starts hacking a locked crate
// Returning a non-null value overrides default behavior
object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
{
    Puts("CanHackCrate works!");
    return null;
}

// OnUserNameUpdated
// Called when a player's stored nickname has been changed
// No return behavior
void OnUserNameUpdated(string id, string oldName, string newName)
{
    Puts($"Player name changed from {oldName} to {newName} for ID {id}");
}

// CanDismountEntity
// Called when the player attempts to dismount an entity
// Returning a non-null value overrides default behavior
object CanDismountEntity(BasePlayer player, BaseMountable entity)
{
    Puts("CanDismountEntity works!");
    return null;
}

// OnUserGroupAdded
// Called when a player has been added to a group
// No return behavior
void OnUserGroupAdded(string id, string groupName)
{
    Puts($"Player '{id}' added to group: {groupName}");
}

// OnUserGroupRemoved
// Called when a player has been removed from a group
// No return behavior
void OnUserGroupRemoved(string id, string groupName)
{
    Puts($"Player '{id}' removed from group: {groupName}");
}

// CanEquipItem
// Called when the player attempts to equip an item
// Returning true or false overrides default behavior
bool CanEquipItem(PlayerInventory inventory, Item item, int targetPos)
{
    Puts("CanEquipItem works!");
    return true;
}

// OnExperimentStart
// Called when the player attempts to experiment with at a workbench
// Returning a non-null value overrides default behavior
object OnExperimentStart(Workbench workbench, BasePlayer player)
{
    Puts("OnExperimentStart works!");
    return null;
}

// OnUserPermissionGranted
// Called when a permission has been granted to a player
// No return behavior
void OnUserPermissionGranted(string id, string permName)
{
    Puts($"Player '{id}' granted permission: {permName}");
}

// OnUserPermissionRevoked
// Called when a permission has been revoked from a player
// No return behavior
void OnUserPermissionRevoked(string id, string permName)
{
    Puts($"Player '{id}' revoked permission: {permName}");
}

// OnExperimentStarted
// Called after the experimentation has started
// No return behaviour
void OnExperimentStarted(Workbench workbench, BasePlayer player)
{
    Puts("OnExperimentStarted works!");
}

// OnUserKicked
// Called when a player has been kicked from the server
// No return behavior
void OnUserKicked(IPlayer player, string reason)
{
    Puts($"Player {player.Name} ({player.Id}) was kicked");
}

// OnExperimentEnd
// Called when an experiment is about to end
// Returning a non-null value overrides defualt behaviour.
object OnExperimentEnd(Workbench workbench)
{
    Puts("OnExperimentEnd works!");
    return null;
}

// OnExperimentEnded
// Called after the experiment has finished
// No return behaviour
void OnExperimentEnded(Workbench workbench)
{
    Puts("OnExperimentEnded works!");
}

// OnUserBanned
// Called when a player has been banned from the server
// Will have reason available if provided
// No return behavior
void OnUserBanned(string name, string id, string ipAddress, string reason)
{
    Puts($"Player {name} ({id}) at {ipAddress} was banned: {reason}");
}

// CanHideStash
// Called when a player tries to hide a stash
// Returning a non-null value overrides default behavior
object CanHideStash(BasePlayer player, StashContainer stash)
{
    Puts("CanHideStash works!");
    return null;
}

// OnUserUnbanned
// Called when a player has been unbanned from the server
// No return behavior
void OnUserUnbanned(string name, string id, string ipAddress)
{
    Puts($"Player {name} ({id}) at {ipAddress} was unbanned");
}

// CanLootPlayer
// Called when the player attempts to loot another player
// Returning true or false overrides default behavior
bool CanLootPlayer(BasePlayer target, BasePlayer looter)
{
    Puts("CanLootPlayer works!");
    return true;
}

// CanLootEntity
// Called when the player starts looting a DroppedItemContainer, LootableCorpse, ResourceContainer, BaseRidableAnimal, or StorageContainer entity
// Returning a non-null value overrides default behavior
object CanLootEntity(BasePlayer player, DroppedItemContainer container)
{
    Puts("CanLootEntity works!");
    return null;
}

object CanLootEntity(BasePlayer player, LootableCorpse corpse)
{
    Puts("CanLootEntity works!");
    return null;
}

object CanLootEntity(BasePlayer player, ResourceContainer container)
{
    Puts("CanLootEntity works!");
    return null;
}

object CanLootEntity(BasePlayer player, BaseRidableAnimal animal)
{
    Puts("CanLootEntity works!");
    return null;
}

object CanLootEntity(BasePlayer player, StorageContainer container)
{
    Puts("CanLootEntity works!");
    return null;
}

// CanMountEntity
// Called when the player attempts to mount an entity
// Returning a non-null value overrides default behavior
object CanMountEntity(BasePlayer player, BaseMountable entity)
{
    Puts("CanMountEntity works!");
    return null;
}

// CanPickupEntity
// Called when a player attempts to pickup a deployed entity (AutoTurret, BaseMountable, BearTrap, DecorDeployable, Door, DoorCloser, ReactiveTarget, SamSite, SleepingBag, SpinnerWheel, StorageContainer, etc.)
// Returning true or false overrides default behavior
bool CanPickupEntity(BasePlayer player, BaseEntity entity)
{
    Puts("CanPickupEntity works!");
    return true;
}

// CanPickupLock
// Called when a player attempts to pickup a lock
// Returning true or false overrides default behavior
bool CanPickupLock(BasePlayer player, BaseLock baseLock)
{
    Puts("CanPickupLock works!");
    return true;
}

// CanRenameBed
// Called when the player attempts to rename a bed or sleeping bag
// Returning a non-null value overrides default behavior
object CanRenameBed(BasePlayer player, SleepingBag bed, string bedName)
{
    Puts("CanRenameBed works!");
    return null;
}

// CanResearchItem
// Called when the player attempts to research an item
// Returning a non-null value overrides default behavior
object CanResearchItem(BasePlayer player, Item targetItem)
{
    Puts("CanResearchItem works!");
    return null;
}

// CanUseLockedEntity
// Called when the player tries to use an entity that is locked
// Returning true or false overrides default behavior
bool CanUseLockedEntity(BasePlayer player, BaseLock baseLock)
{
    Puts("CanUseLockedEntity works!");
    return true;
}

// CanSeeStash
// Called when a player is looking at a hidden stash
// Returning a non-null value overrides default behavior
object CanSeeStash(BasePlayer player, StashContainer stash)
{
    Puts("CanSeeStash works!");
    return null;
}

// OnStashExposed
// Called when a player reveals a hidden stash
// No return behavior
void OnStashExposed(StashContainer stash, BasePlayer player)
{
    Puts("OnStashExposed works");
}

// OnStashHidden
// Called when a player hides a stash
// No return behavior
void OnStashHidden(StashContainer stash, BasePlayer player)
{
    Puts("OnStashHidden works!");
}

// CanSetBedPublic
// Called when a player tries to set a bed public
// Returning a non - null value overrides default behavior
object CanSetBedPublic(BasePlayer player, SleepingBag bed)
{
    Puts("CanSetBedPublic works!");
    return null;
}

// CanUnlock
// Called when the player tries to unlock a keylock or codelock
// Returning a non-null value overrides default behavior
object CanUnlock(BasePlayer player, BaseLock baseLock)
{
    Puts("CanUnlock works!");
    return null;
}

// CanUseMailbox
// Called when the player tries to use a mailbox
// Returning true or false overrides default behavior
bool CanUseMailbox(BasePlayer player, Mailbox mailbox)
{
    Puts("CanUseMailbox works!");
    return true;
}

// CanUseUI
// Called when the player attempts to use a custom UI
// Returning a non-null value overrides default behavior
object CanUseUI(BasePlayer player, string json)
{
    Puts("CanUseUI works!");
    return null;
}

// CanWearItem
// Called when the player attempts to equip an item
// Returning a non-null value overrides default behavior
object CanWearItem(PlayerInventory inventory, Item item, int targetSlot)
{
    Puts("CanWearItem works!");
    return null;
}

// OnClientAuth
// Called when the player is giving server connection authorization information
// No return behavior
void OnClientAuth(Connection connection)
{
    Puts("OnClientAuth works!");
}

// OnDestroyUI
// Called when a custom UI is destroyed for the player
// No return behavior
void OnDestroyUI(BasePlayer player, string json)
{
    Puts("OnDestroyUI works!");
}

// OnFindSpawnPoint
// Useful for controlling player spawnpoints (like making all spawns occur in a set area)
// Return a BasePlayer.SpawnPoint object to use that spawnpoint
void OnFindSpawnPoint(BasePlayer player)
{
    Puts("OnFindSpawnPoint works!");
}

// OnLootEntity
// Called when the player starts looting an entity
// No return behavior
void OnLootEntity(BasePlayer player, BaseEntity entity)
{
    Puts("OnLootEntity works!");
}

// OnLootEntityEnd
// Called when the player stops looting an entity
// No return behavior
void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
{
    Puts("OnLootEntityEnd works!");
}

// OnLootItem
// Called when the player starts looting an item
// No return behavior
void OnLootItem(PlayerLoot playerLoot, Item item)
{
    Puts("OnLootItem works!");
}

// OnLootPlayer
// Called when the player starts looting another player
// No return behavior
void OnLootPlayer(BasePlayer player, BasePlayer target)
{
    Puts("OnLootPlayer works!");
}

// OnPlayerAttack
// Useful for modifying an attack before it goes out
// hitInfo.HitEntity should be the entity that this attack would hit
// Returning a non-null value overrides default behavior
object OnPlayerAttack(BasePlayer attacker, HitInfo info)
{
    Puts("OnPlayerAttack works!");
    return null;
}

// OnPlayerBanned
// Called when the player is banned (Facepunch, EAC, server ban, etc.)
// No return behavior
void OnPlayerBanned(string name, ulong id, string address, string reason)
{
    Puts("OnPlayerBanned works!");
}

// OnPlayerChat
// Called when the player sends chat to the server
// Returning a non-null value overrides default behavior of chat, not commands
object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
{
    Puts("OnPlayerChat works!");
    return null;
}

// OnPlayerConnected
// Called after the player object is created, but before the player has spawned
// No return behavior
void OnPlayerConnected(BasePlayer player)
{
    Puts("OnPlayerConnected works!");
}

// OnPlayerDeath
// Called when the player is about to die
// HitInfo may be null sometimes
// Returning a non-null value overrides default behavior
object OnPlayerDeath(BasePlayer player, HitInfo info)
{
    Puts("OnPlayerDeath works!");
    return null;
}

// OnPlayerDisconnected
// Called after the player has disconnected from the server
// No return behavior
void OnPlayerDisconnected(BasePlayer player, string reason)
{
    Puts("OnPlayerDisconnected works!");
}

// OnPlayerDropActiveItem
// Called when the player drops their active held item
// No return behavior
void OnPlayerDropActiveItem(BasePlayer player, Item item)
{
    Puts("OnPlayerDropActiveItem works!");
}

// OnPlayerHealthChange
// Called just before the player's health changes
// Returning a non-null value cancels the health change
object OnPlayerHealthChange(BasePlayer player, float oldValue, float newValue)
{
    Puts("OnPlayerHealthChange works!");
    return null;
}

// OnPlayerInput
// Called when input is received from a connected client
// No return behavior
void OnPlayerInput(BasePlayer player, InputState input)
{
    Puts("OnPlayerInput works!");
}

// OnPlayerKicked
// Called after the player is kicked from the server
// No return behavior
void OnPlayerKicked(BasePlayer player, string reason)
{
    Puts("OnPlayerKicked works!");
}

// OnPlayerLand
// Called just before the player lands on the ground
// Returning a non-null value overrides default behavior
object OnPlayerLand(BasePlayer player, float num)
{
    Puts("OnPlayerLand works!");
    return null;
}

// OnPlayerLanded
// Called when the player landed on the ground
// No return behavior
void OnPlayerLanded(BasePlayer player, float num)
{
    Puts("OnPlayerLanded works!");
}

// OnPlayerLootEnd
// Called when the player stops looting
// No return behavior
void OnPlayerLootEnd(PlayerLoot inventory)
{
    Puts("OnPlayerLootEnd works!");
}

// OnPlayerMetabolize
// Called after the player's metabolism has been changed
// No return behavior
void OnPlayerMetabolize(PlayerMetabolism metabolism, BaseCombatEntity entity, float delta)
{
    Puts("OnPlayerMetabolize works!");
}

// OnPlayerRecover
// Called when the player is about to recover from the 'wounded' state
// Returning a non-null value overrides default behavior
object OnPlayerRecover(BasePlayer player)
{
    Puts("OnPlayerRecover works!");
    return null;
}

// OnPlayerRespawn
// Called when a player is attempting to respawn
// Returning a BasePlayer.SpawnPoint (takes a position and rotation), overrides the respawn location, for the variant that receives a BasePlayer.SpawnPoint argument
// Returning a SleepingBag overrides the respawn location, for the variant that receives a SleepingBag argument
object OnPlayerRespawn(BasePlayer player, BasePlayer.SpawnPoint spawnPoint)
{
    Puts("OnPlayerRespawn works!");
    return null;
}

object OnPlayerRespawn(BasePlayer player, SleepingBag sleepingBag)
{
    Puts("OnPlayerRespawn works!");
    return null;
}

// OnPlayerRespawned
// Called when the player has respawned (specifically when they click the "Respawn" button)
// ONLY called after the player has transitioned from dead to not-dead, so not when they're waking up
// This means it's possible for the player to connect and disconnect from a server without OnPlayerRespawned ever triggering for them
// No return behavior
void OnPlayerRespawned(BasePlayer player)
{
    Puts("OnPlayerRespawned works!");
}

// OnRespawnInformationGiven
// Called when a player is about to be sent respawn information
// No return behavior
void OnRespawnInformationGiven(BasePlayer player, RespawnInformation respawnInformation)
{
    Puts("OnRespawnInformationGiven works!");
}

// OnPlayerSleep
// Called when the player is about to go to sleep
// Returning a non-null value overrides default behavior
object OnPlayerSleep(BasePlayer player)
{
    Puts("OnPlayerSleep works!");
    return null;
}

// OnPlayerSleepEnded
// Called when the player awakes
// No return behavior
void OnPlayerSleepEnded(BasePlayer player)
{
    Puts("OnPlayerSleepEnded works!");
}

// OnPlayerSpawn
// Called when a player is attempting to spawn for the first time
// Returning true overrides default behavior
object OnPlayerSpawn(BasePlayer player)
{
    Puts("OnPlayerSpawn works!");
    return null;
}

// OnPlayerSpectate
// Called when the player starts spectating
// Returning a non-null value overrides default behavior
object OnPlayerSpectate(BasePlayer player, string spectateFilter)
{
    Puts("OnPlayerSpectate works!");
    return null;
}

// OnPlayerSpectateEnd
// Called when the player stops spectating
// Returning a non-null value stops the spectate from ending
object OnPlayerSpectate(BasePlayer player, string spectateFilter)
{
    Puts("OnPlayerSpectate works!");
return null;
}

// OnPlayerSpectateEnd
// Called when the player stops spectating
// Returning a non-null value stops the spectate from ending
object OnPlayerSpectateEnd(BasePlayer player, string spectateFilter)
{
    Puts("OnPlayerSpectateEnd works!");
    return null;
}

// OnPlayerTick
// Returning a non-null value overrides default behavior
object OnPlayerTick(BasePlayer player, PlayerTick msg, bool wasPlayerStalled)
{
    Puts("OnPlayerTick works!");
    return null;
}

// OnPlayerViolation
// Called when the player triggers an anti-hack violation
// Returning a non-null value overrides default behavior
object OnPlayerViolation(BasePlayer player, AntiHackType type, float amount)
{
    Puts("OnPlayerViolation works!");
    return null;
}

// OnPlayerVoice
// Called when the player uses the in-game voice chat
// Returning a non-null value overrides default behavior
object OnPlayerVoice(BasePlayer player, Byte[] data)
{
    Puts("OnPlayerVoice works!");
    return null;
}

// OnPlayerWound
// Called when the player is about to go down to the 'wounded' state
// source might be null, check it before use
// Returning a non-null value cancels the wounded state
object OnPlayerWound(BasePlayer player, HitInfo info)
{
    Puts("OnPlayerWound works!");
    return null;
}

// OnRunPlayerMetabolism
// Called before a metabolism update occurs for the specified player
// Metabolism update consists of managing the player's temperature, health etc.
// You can use this to turn off or change certain aspects of the metabolism, either by editing values before returning, or taking complete control of the method
// Returning a non-null value cancels the update
object OnRunPlayerMetabolism(PlayerMetabolism metabolism, BasePlayer player, float delta)
{
    Puts("OnRunPlayerMetabolism works!");
    return null;
}

// OnUserApprove
// Used by RustCore and abstracted into CanClientLogin
// Returning a non-null value overrides default behavior, plugin should call Reject if it does this
object OnUserApprove(Network.Connection connection)
{
    Puts("OnUserApprove works!");
    return null;
}

// OnDefaultItemsReceive
// Called when a player is about to receive default items
// Returning a non-null value overrides default behavior
object OnDefaultItemsReceive(PlayerInventory inventory)
{
    Puts("OnDefaultItemsReceive works!");
    return null;
}

// OnDefaultItemsReceived
// Called after a player has received default items
// Returning a non-null value overrides default behavior
void OnDefaultItemsReceived(PlayerInventory inventory)
{
    Puts("OnDefaultItemsReceived works!");
}

// OnAnalysisComplete
// Called right after a player completes a survey crater analysis
// No return behavior
void OnAnalysisComplete(SurveyCrater surveyCrater, BasePlayer player)
{
    Puts("OnAnalysisComplete works!");
}

// OnNpcConversationStart
// Called when a player tries to start a conversation with an NPC
// Returning a non-null value overrides default behavior
object OnNpcConversationStart(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData)
{
    Puts("OnNpcConversationStart works!");
    return null;
}

// OnNpcConversationRespond
// Called when a player chooses an NPC conversation response
// Returning a non-null value overrides default behavior
object OnNpcConversationRespond(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ConversationData.ResponseNode responseNode)
{
    Puts("OnNpcConversationRespond works!");
    return null;
}

// OnNpcConversationResponded
// Called right after a player's chosen NPC conversation response has been processed
// No return behavior
void OnNpcConversationResponded(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ConversationData.ResponseNode responseNode)
{
    Puts("OnNpcConversationResponded works!");
}

// OnNpcConversationEnded
// Called right after a player has ended an NPC conversation
// No return behavior
void OnNpcConversationEnded(NPCTalking npcTalking, BasePlayer player)
{
    Puts("OnNpcConversationEnded works!");
}

// OnNetworkGroupEntered
// Called after a player has entered a network group
// No return behavior
void OnNetworkGroupEntered(BasePlayer player, Network.Visibility.Group group)
{
    Puts("OnNetworkGroupEntered works!");
}

// OnNetworkGroupLeft
// Called after a player has left a network group
// No return behavior
void OnNetworkGroupLeft(BasePlayer player, Network.Visibility.Group group)
{
    Puts("OnNetworkGroupLeft works!");
}

// OnDemoRecordingStart
// Called right before a demo of a player starts recording
// Returning a non-null value overrides default behavior
object OnDemoRecordingStart(string filename, BasePlayer player)
{
    Puts("OnDemoRecordingStart works!");
    return null;
}

// OnDemoRecordingStarted
// Called after a demo of a player has started recording
// No return behavior
void OnDemoRecordingStarted(string filename, BasePlayer player)
{
    Puts("OnDemoRecordingStarted works!");
}

// OnDemoRecordingStop
// Called right before a demo of a player stops recording
// Returning a non-null value overrides default behavior
object OnDemoRecordingStop(string filename, BasePlayer player)
{
    Puts("OnDemoRecordingStop works!");
    return null;
}

// OnDemoRecordingStopped
// Called after a demo of a player has stopped recording
// No return behavior
void OnDemoRecordingStopped(string filename, BasePlayer player)
{
    Puts("OnDemoRecordingStopped works!");
}

// OnLootNetworkUpdate
// Called when a player is trying to loot a container or a container they are looting has changed its contents
// Returning a non-null value overrides default behavior
object OnLootNetworkUpdate(PlayerLoot loot)
{
    Puts("OnLootNetworkUpdate works!");
    return null;
}

// OnInventoryNetworkUpdate
// Called after a player's inventory contents have changed, before it is sent over the network to one or more clients
// Returning a non-null value overrides default behavior
object OnInventoryNetworkUpdate(PlayerInventory inventory, ItemContainer container, ProtoBuf.UpdateItemContainer updateItemContainer, PlayerInventory.Type type, PlayerInventory.PlayerInventory.NetworkInventoryMode networkInventoryMode)
{
    Puts("OnInventoryNetworkUpdate works!");
    return null;
}

// OnPlayerAddModifiers
// Called after a player consumes an item such as tea that is about to apply modifiers
// Returning a non-null value overrides default behavior (prevents the modifiers from being applied)
object OnPlayerAddModifiers(BasePlayer player, Item item, ItemModConsumable consumable)
{
    Puts("OnPlayerAddModifiers works!");
    return null;
}

// OnThreatLevelUpdate
// Called when a player's threat level is about to be updated
// Returning a non-null value cancels default behavior
object OnThreatLevelUpdate(BasePlayer player)
{
    Puts("OnThreatLevelUpdate works!");
    return null;
}

// CanUseGesture
// Called when a player attempts to use a gesture
// Returning true or false overrides default behavior
bool? CanUseGesture(BasePlayer player, GestureConfig gesture)
{
    Puts("CanUseGesture works!");
    return null;
}

// OnCentralizedBanCheck
// Called when a player is about to be checked in the centralized ban database
// Returning a non-null value cancels default behavior
object OnCentralizedBanCheck(Network.Connection connection)
{
    Puts("OnCentralizedBanCheck works!");
    return null;
}

// OnClientCommand
// Useful for intercepting players' commands before their handling
// Called before OnPlayerCommand and OnUserCommand
// Returning a non-null value overrides default behavior
object OnClientCommand(Connection connection, string command)
{
    Puts("OnClientCommand works!");
    return null;
}

// OnCorpsePopulate
// Called when an NPC player corpse is about to be populated with loot
// Returning a BaseCorpse overrides default behavior
BaseCorpse OnCorpsePopulate(BasePlayer npcPlayer, BaseCorpse corpse)
{
    Puts("OnCorpsePopulate works!");
    return null;
}

// CanSetRelationship
// Called when a player's relationship with another is about to be updated
// Returning true or false overrides default behavior
bool? CanSetRelationship(BasePlayer player, BasePlayer otherPlayer, RelationshipManager.RelationshipType relationshipType, int weight)
{
    Puts("CanSetRelationship works!");
    return null;
}
// Entity Hooks

// CanBradleyApcTarget
// Called when an APC targets an entity
// Returning true or false overrides default behavior
bool CanBradleyApcTarget(BradleyAPC apc, BaseEntity entity)
{
    Puts("CanBradleyApcTarget works!");
    return true;
}

// OnNpcPlayerResume
// Useful for canceling the invoke of TryForceToNavmesh
// Returning a non-null value cancels default behavior
object OnNpcPlayerResume(NPCPlayerApex npc)
{
    Puts("OnNpcPlayerResume works!");
    return null;
}

// OnNpcDestinationSet
// Useful for canceling the destination change on NPCs
// Returning a non-null value cancels default behavior
object OnNpcDestinationSet(NPCPlayerApex npc, Vector3 newPosition)
{
    Puts("OnNpcDestinationSet works!");
    return null;
}

// OnNpcStopMoving
// Useful for denying the move stop of NPCs
// Returning a non-null value cancels default behavior
object OnNpcStopMoving(NPCPlayerApex npc)
{
    Puts("OnNpcStopMoving works!");
    return null;
}

// OnEntityMarkHostile
// Useful for denying marking the entity hostile
// Returning a non-null value cancels default behavior
object OnEntityMarkHostile(BaseCombatEntity entity, float duration)
{
    Puts("OnEntityMarkHostile works!");
    return null;
}

// CanEntityBeHostile
// Useful for overriding hostility of an entity
// Returning a non-null value overrides default behavior
object CanEntityBeHostile(BaseCombatEntity entity)
{
    Puts("CanEntityBeHostile works!");
    return null;
}

// CanSamSiteShoot
// Useful for canceling the shoot of SamSite
// Returning a non-null value cancels default behavior
object CanSamSiteShoot(SamSite samSite)
{
    Puts("CanSamSiteShoot works!");
    return null;
}

// OnDieselEngineToggle
// Called when a player is trying to toggle diesel engine
// Returning a non-null value cancels default behavior
object OnDieselEngineToggle(DieselEngine engine, BasePlayer player)
{
    Puts("OnDieselEngineToggle works!");
    return null;
}

// OnDieselEngineToggled
// Called when diesel engine is toggled
// No return behavior
void OnDieselEngineToggled(DieselEngine engine)
{
    Puts("OnDieselEngineToggled works!");
}

// OnExcavatorMiningToggled
// Called when excavator mining arm is toggled
// No return behavior
void OnExcavatorMiningToggled(ExcavatorArm arm)
{
    Puts("OnExcavatorMiningToggled works!");
}

// OnExcavatorGather
// Called right before moving gathered resource to container
// Returning a non-null value cancels default behavior
object OnExcavatorGather(ExcavatorArm arm, Item item)
{
    Puts("OnExcavatorGather works!");
    return null;
}

// OnExcavatorResourceSet
// Called when a player is trying to set a new resource target
// Returning a non-null value cancels default behavior
object OnExcavatorResourceSet(ExcavatorArm arm, string resourceName, BasePlayer player)
{
    Puts("OnExcavatorResourceSet works!");
    return null;
}

// OnInputUpdate
// Called when an input of IOEntity is updated
// Returning a non-null value cancels default behavior
object OnInputUpdate(IOEntity entity, int inputAmount, int slot)
{
    Puts("OnInputUpdate works!");
    return null;
}

// OnOutputUpdate
// Called when outputs of IOEntity are updated
// Returning a non-null value cancels default behavior
object OnOutputUpdate(IOEntity entity)
{
    Puts("OnOutputUpdate works!");
    return null;
}

// OnButtonPress
// Called when a player is trying to press a button
// Returning a non-null value cancels default behavior
object OnButtonPress(PressButton button, BasePlayer player)
{
    Puts("OnButtonPress works!");
    return null;
}

// OnShopAcceptClick
// Called when a player is trying to accept a trade in ShopFront
// Returning a non-null value cancels default behavior
object OnShopAcceptClick(ShopFront entity, BasePlayer player)
{
    Puts("OnShopAcceptClick works!");
    return null;
}

// OnShopCancelClick
// Called when a player is cancelling a trade
// Returning a non-null value cancels default behavior
object OnShopCancelClick(ShopFront entity, BasePlayer player)
{
    Puts("OnShopCancelClick works!");
    return null;
}

// OnShopCompleteTrade
// Called before the trade is completed in ShopFront
// Returning a non-null value cancels default behavior
object OnShopCompleteTrade(ShopFront entity)
{
    Puts("OnShopCompleteTrade works!");
    return null;
}

// OnSamSiteTarget
// Called before last target visible time is updated
// Returning a non-null value cancels default behavior
object OnSamSiteTarget(SamSite samSite, BaseCombatEntity target)
{
    Puts("OnSamSiteTarget works!");
    return null;
}

// OnHelicopterStrafeEnter
// Called when helicopter is entering strafe
// Returning a non-null value cancels default behavior
object OnHelicopterStrafeEnter(PatrolHelicopterAI helicopter, Vector3 strafePosition)
{
    Puts("OnHelicopterStrafeEnter works!");
    return null;
}

// OnSupplyDropLanded
// Called after Supply Drop has landed
// No return behavior
void OnSupplyDropLanded(SupplyDrop entity)
{
    Puts("OnSupplyDropLanded works!");
}

// OnEntityStabilityCheck
// Called when stability of an entity is checked
// Returning a non-null value cancels default behavior
object OnEntityStabilityCheck(StabilityEntity entity)
{
    Puts("OnEntityStabilityCheck works!");
    return null;
}

// OnBuildingPrivilege
// Useful for overriding a building privilege on specific entities and etc.
// Returning BuildingPrivlidge value overrides default behavior
BuildingPrivlidge OnBuildingPrivilege(BaseEntity entity, OBB obb)
{
    Puts($"Getting a building privilege for {entity.ShortPrefabName}!");
    return null;
}

// OnHorseLead
// Called when a player tries to lead a horse
// Returning a non-null value overrides default behavior
object OnHorseLead(BaseRidableAnimal animal, BasePlayer player)
{
    Puts($"{player.displayName} tries to lead {animal.ShortPrefabName}");
    return null;
}

// OnHorseHitch
// Called just before setting the hitch
// Returning a non-null value overrides default behavior
object OnHorseHitch(RidableHorse horse, HitchTrough hitch)
{
    Puts("OnHorseHitch works!");
    return null;
}

// OnWireConnect
// Useful for preventing a wire to connect
// Returning a non-null value cancels default behavior
object OnWireConnect(BasePlayer player, IOEntity entity1, int inputs, IOEntity entity2, int outputs)
{
    Puts("OnWireConnect works!");
    return null;
}

// OnWireClear
// Useful for preventing clearing wires
// Returning a non-null value cancels default behavior
object OnWireClear(BasePlayer player, IOEntity entity1, int connected, IOEntity entity2, bool flag)
{
    Puts("OnWireClear works!");
    return null;
}

// OnReactiveTargetReset
// Called after the reactive target is reset
// No return behaviour
void OnReactiveTargetReset(ReactiveTarget target)
{
    Puts("OnReactiveTargetReset works!");
}

// OnMlrsFire
// Called just before the MLRS is fired.
// Returning a non-null value overrides default behaviour.
object OnMlrsFire(MLRS mlrs, BasePlayer player)
{
    Puts("OnMlrsFire works!");
    return null;
}

// OnMlrsFired
// Called just after the MLRS has been fired.
// No return behaviour.
void OnMlrsFired(MLRS mlrs, BasePlayer player)
{
    Puts("OnMlrsFired works!");
}

// OnTurretAssign
// Called when a player attempts to authorize another player on a turret
// Returning a non-null value cancels default behavior
object OnTurretAssign(AutoTurret turret, ulong targetId, BasePlayer initiator)
{
    Puts("{targetId} has been authorized on a turret by {initiator.displayName}");
    return null;
}

// OnTurretAssigned
// Called when a player has been authorized on a turret by another player
// No return behaviour
void OnTurretAssigned(AutoTurret turret, ulong targetId, BasePlayer initiator)
{
    Puts($"{targetId} has been authorized on a turret by {initiator.displayName}");
}

// CanHelicopterDropCrate
// Called when a CH47 helicopter attempts to drop a crate
// Returning true or false overrides default behavior
bool CanHelicopterDropCrate(CH47HelicopterAIController heli)
{
    Puts("CanHelicopterDropCrate works!");
    return true;
}

// OnEngineLoadoutRefresh
// Called before engine loadout data is refreshed
// Returning a non-null value overrides default behaviour
object OnEngineLoadoutRefresh(EngineStorage storage)
{
    Puts("OnEngineLoadoutRefresh works!");
    return null;
}

// CanHelicopterStrafe
// Called when a patrol helicopter attempts to strafe
// Returning true or false overrides default behavior
bool CanHelicopterStrafe(PatrolHelicopterAI heli)
{
    Puts("CanHelicopterStrafe works!");
    return true;
}

// CanHelicopterStrafeTarget
// Called when a patrol helicopter attempts to target a player to attack while strafing
// Returning true or false overrides default behavior
bool CanHelicopterStrafeTarget(PatrolHelicopterAI entity, BasePlayer target)
{
    Puts("CanHelicopterStrafeTarget works!");
    return true;
}

// CanHelicopterTarget
// Called when a patrol helicopter attempts to target a player to attack
// Returning true or false overrides default behavior
bool CanHelicopterTarget(PatrolHelicopterAI heli, BasePlayer player)
{
    Puts("CanHelicopterTarget works!");
    return true;
}

// CanHelicopterUseNapalm
// Called when a patrol helicopter attempts to use napalm
// Returning true or false overrides default behavior
bool CanHelicopterUseNapalm(PatrolHelicopterAI heli)
{
    Puts("CanHelicopterUseNapalm works!");
    return true;
}

// CanNetworkTo
// Called when an entity attempts to network with a player
// For better performance, avoid using heavy calculations in this hook.
// Returning true or false overrides default behavior
bool CanNetworkTo(BaseNetworkable entity, BasePlayer target)
{
    Puts("CanNetworkTo works!");
    return true;
}

// OnHelicopterRetire
// Called before the patrol helicopter starts leaving the map.
// Returning a non-null value overrides default behaviour.
object OnHelicopterRetire(PatrolHelicopterAI helicopter)
{
    Puts("OnHelicopterRetire works!");
    return null;
}

// CanNpcAttack
// Called when a NPC attempts to attack another entity
// Returning true or false overrides default behavior
bool CanNpcAttack(BaseNpc npc, BaseEntity target)
{
    Puts("CanNpcAttack works!");
    return true;
}

// CanNpcEat
// Called when a NPC attempts to eat another entity
// Returning true or false overrides default behavior
bool CanNpcEat(BaseNpc npc, BaseEntity target)
{
    Puts("CanNpcEat works!");
    return true;
}

// CanRecycle
// Called when the recycler attempts to recycle an item
// Returning true or false overrides default behavior
bool CanRecycle(Recycler recycler, Item item)
{
    Puts("CanRecycle works!");
    return true;
}

// OnAirdrop
// Called when an airdrop has been called
// No return behavior
void OnAirdrop(CargoPlane plane, Vector3 dropPosition)
{
    Puts("OnAirdrop works!");
}

// OnBradleyApcInitialize
// Called when an APC initializes
// Returning a non-null value overrides default behavior
object OnBradleyApcInitialize(BradleyAPC apc)
{
    Puts("OnBradleyApcInitialize works!");
    return null;
}

// OnBradleyApcHunt
// Called when an APC starts hunting
// Returning a non-null value overrides default behavior
object OnBradleyApcHunt(BradleyAPC apc)
{
    Puts("OnBradleyApcHunt works!");
    return null;
}

// OnBradleyApcPatrol
// Called when an APC is starts patrolling
// Returning a non-null value overrides default behavior
object OnBradleyApcPatrol(BradleyAPC apc)
{
    Puts("OnBradleyApcPatrol works!");
    return null;
}

// OnContainerDropItems
// Called when a container is destroyed and all items are about to be dropped
// Returning a non-null value overrides default behavior
object OnContainerDropItems(ItemContainer container)
{
    Puts("OnContainerDropItems works!");
    return null;
}

// OnCrateDropped
// Called when a locked crate from the CH47 (Chinook) has dropped
// No return behavior
void OnCrateDropped(HackableLockedCrate crate)
{
    Puts("OnCrateDropped works!");
}

// OnCrateHack
// Called when a player starts hacking a locked crate
// No return behavior
void OnCrateHack(HackableLockedCrate crate)
{
    Puts("OnCrateHack works!");
}

// OnCrateHackEnd
// Called when a player stops hacking a locked crate
// No return behavior
void OnCrateHackEnd(HackableLockedCrate crate)
{
    Puts("OnCrateHackEnd works!");
}

// OnCrateLanded
// Called when a locked crate from the CH47 (Chinook) has landed
// No return behavior
void OnCrateLanded(HackableLockedCrate crate)
{
    Puts("OnCrateLanded works!");
}

// OnEntityDeath
// HitInfo might be null, check it before use
// Editing hitInfo has no effect because the death has already happened
// No return behavior
void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
{
    Puts("OnEntityDeath works!");
}

// OnEntityDismounted
// Called when an entity is dismounted by a player
// No return behavior
void OnEntityDismounted(BaseMountable entity, BasePlayer player)
{
    Puts("OnEntityDismounted works!");
}

// OnEntityEnter
// Called when an entity enters a trigger (water area, radiation zone, hurt zone, etc.)
// No return behavior
void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
{
    Puts("OnEntityEnter works!");
}

// OnEntityGroundMissing
// Called when an entity (sleepingbag, sign, furnace,...) is going to be destroyed because the buildingblock it is on was removed
// Returning a non-null value overrides default behavior
object OnEntityGroundMissing(BaseEntity entity)
{
    Puts("OnEntityGroundMissing works!");
    return null;
}

// OnEntityKill
// Called when an entity is destroyed
// Returning a non-null value overrides default behavior
object OnEntityKill(BaseNetworkable entity)
{
    Puts("OnEntityKill works!");
    return null;
}

// OnEntityLeave
// Called when an entity leaves a trigger (water area, radiation zone, hurt zone, etc.)
// No return behavior
void OnEntityLeave(TriggerBase trigger, BaseEntity entity)
{
    Puts("OnEntityLeave works!");
}

// OnEntityMounted
// Called when an entity is mounted by a player
// No return behavior
void OnEntityMounted(BaseMountable entity, BasePlayer player)
{
    Puts("OnEntityMounted works!");
}

// OnEntitySpawned
// Called after any networked entity has spawned (including trees)
// No return behavior
void OnEntitySpawned(BaseNetworkable entity)
{
    Puts("OnEntitySpawned works!");
}

// OnEntityTakeDamage
// Alternatively, modify the HitInfo object to change the damage
// It should be okay to set the damage to 0, but if you don't return non-null, the player's client will receive a damage indicator (if entity is a BasePlayer)
// HitInfo has all kinds of useful things in it, such as Weapon, damageProperties or damageTypes
object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
{
    Puts("OnEntityTakeDamage works!");
    return null;
}

// OnFireBallDamage
// Called when a fire ball does damage to another entity
// No return behavior
void OnFireBallDamage(FireBall fire, BaseCombatEntity entity, HitInfo info)
{
    Puts("OnFireBallDamage works!");
}

// OnFireBallSpread
// Called when a fire ball fire spreads
// No return behavior
void OnFireBallSpread(FireBall ball, BaseEntity fire)
{
    Puts("OnFireBallSpread works!");
}

// OnFlameExplosion
// Called when a flame explodes
// No return behavior
void OnFlameExplosion(FlameExplosive explosive, BaseEntity flame)
{
    Puts("OnFlameExplosion works!");
}

// OnHelicopterAttack
// Called when a CH47 helicopter is being attacked
// Returning a non-null value overrides default behavior
object OnHelicopterAttack(CH47HelicopterAIController heli)
{
    Puts("OnHelicopterAttack works!");
    return null;
}

// OnHelicopterDropCrate
// Called when a CH47 helicopter is dropping a crate
// Returning a non-null value overrides default behavior
object OnHelicopterDropCrate(CH47HelicopterAIController heli)
{
    Puts("OnHelicopterDropCrate works!");
    return null;
}

// OnHelicopterDropDoorOpen
// Called when a CH47 helicopter is opening its drop door
// Returning a non-null value overrides default behavior
object OnHelicopterDropDoorOpen(CH47HelicopterAIController heli)
{
    Puts("OnHelicopterDropDoorOpen works!");
    return null;
}

// OnHelicopterKilled
// Called when a CH47 helicopter is going to be killed
// Returning a non-null value overrides default behavior
object OnHelicopterKilled(CH47HelicopterAIController heli)
{
    Puts("OnHelicopterKilled works!");
    return null;
}

// OnHelicopterOutOfCrates
// Called when a CH47 helicopter runs out of crates
// Returning true or false overrides default behavior
bool? OnHelicopterOutOfCrates(CH47HelicopterAIController heli)
{
    Puts("OnHelicopterOutOfCrates works!");
    return null;
}

// OnHelicopterTarget
// Called when a helicopter turret attempts to target an entity
// Returning a non-null value overrides default behavior
object OnHelicopterTarget(HelicopterTurret turret, BaseCombatEntity entity)
{
    Puts("OnHelicopterTarget works!");
    return null;
}

// OnLiftUse
// Called when a player calls a lift or procedural lift
// Returning a non-null value overrides default behavior
object OnLiftUse(Lift lift, BasePlayer player)
{
    Puts("OnLiftUse works!");
    return null;
}

object OnLiftUse(ProceduralLift lift, BasePlayer player)
{
    Puts("OnLiftUse works!");
    return null;
}

// OnLootSpawn
// Called when loot spawns in a container
// Returning a non-null value overrides default behavior
object OnLootSpawn(LootContainer container)
{
    Puts("OnLootSpawn works!");
    return null;
}

// OnNpcTargetSense
// Called when an NPC becomes aware of another entity
// Returning a non-null value overrides default behavior
object OnNpcTargetSense(BaseEntity owner, BaseEntity entity, AIBrainSenses brainSenses)
{
    Puts("OnNpcTargetSense works!");
    return null;
}

// OnNpcTarget
// Called when an NPC targets another entity
// Returning a non-null value overrides default behavior
object OnNpcTarget(BaseEntity npc, BaseEntity entity)
{
    Puts("OnNpcTarget works!");
    return null;
}

// OnOvenToggle
// Called when an oven (Campfire, Furnace,...) is turned on or off
// Returning a non-null value overrides default behavior
object OnOvenToggle(BaseOven oven, BasePlayer player)
{
    Puts("OnOvenToggle works!");
    return null;
}

// OnItemRecycle
// Called when an item is recycled in a recycler
// Returning a non-null value overrides default behavior
object OnItemRecycle(Item item, Recycler recycler)
{
    Puts("OnItemRecycle works!");
    return null;
}

// OnOvenCook
// Called before an oven cooks an item
// Returning a non-null value overrides default behavior
object OnOvenCook(BaseOven oven, Item item)
{
    Puts("OnOvenCook works!");
    return null;
}

// OnOvenCooked
// Called after an oven cooks an item
// No return behavior
void OnOvenCooked(BaseOven oven, Item item, BaseEntity slot)
{
    Puts("OnOvenCooked works!");
}

// OnRecyclerToggle
// Called when a recycler is turned on or off
// Returning a non-null value overrides default behavior
object OnRecyclerToggle(Recycler recycler, BasePlayer player)
{
    Puts("OnRecyclerToggle works!");
    return null;
}

// OnResourceDepositCreated
// Called when a resource deposit has been created
// No return behavior
void OnResourceDepositCreated(ResourceDepositManager.ResourceDeposit deposit)
{
    Puts("OnResourceDepositCreated works!");
}

// OnShopCompleteTrade
// Called when a shopfront trade is complete
// Returning a non-null value overrides default behavior
object OnShopCompleteTrade(ShopFront shop, BasePlayer customer)
{
    Puts("OnShopCompleteTrade works!");
    return null;
}

// OnTurretAuthorize
// Called when a player attempts to authorize on a turret
// Returning a non-null value overrides default behavior
object OnTurretAuthorize(AutoTurret turret, BasePlayer player)
{
    Puts("OnTurretAuthorize works!");
    return null;
}

// OnTurretClearList
// Called when a player attempts to clear an autoturret's authorized list
// Returning a non-null value overrides default behavior
object OnTurretClearList(AutoTurret turret, BasePlayer player)
{
    Puts("OnTurretClearList works!");
    return null;
}

// OnTurretDeauthorize
// Called when a player is deauthorized on an autoturret
// Returning a non-null value overrides default behavior
object OnTurretDeauthorize(AutoTurret turret, BasePlayer player)
{
    Puts("OnTurretDeauthorize works!");
    return null;
}

// OnTurretModeToggle
// Called when the mode of an autoturrent is toggled
// No return behavior
void OnTurretModeToggle(AutoTurret turret)
{
    Puts("OnTurretModeToggle works!");
}

// OnTurretShutdown
// Called when an autoturret is shut down
// Returning a non-null value overrides default behavior
object OnTurretShutdown(AutoTurret turret)
{
    Puts("OnTurretShutdown works!");
    return null;
}

// OnTurretStartup
// Called when an autoturret starts up
// Returning a non-null value overrides default behavior
object OnTurretStartup(AutoTurret turret)
{
    Puts("OnTurretStartup works!");
    return null;
}

// OnTurretTarget
// Called when an autoturret attempts to target an entity
// Returning a non-null value overrides default behavior
object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity)
{
    Puts("OnTurretTarget works!");
    return null;
}

// OnTurretToggle
// Called when an autoturret toggles powerstate (on/off)
// Returning a non-null value overrides default behavior
object OnTurretToggle(AutoTurret turret)
{
    Puts("OnTurretToggle works!");
    return null;
}

// OnBigWheelWin
// Called before multiplier is applied.
// Returning non-null value overrides default behaviour.
object OnBigWheelWin(BigWheelGame bigWheel, Item scrap, BigWheelBettingTerminal terminal, int multiplier)
{
    Puts("OnBigWheelWin works!");
    return null;
}

// OnBigWheelLoss
// Called when a specific item is lost on the big wheel game
// No return behavior
void OnBigWheelLoss(BigWheelGame wheel, Item item)
{
    Puts("OnBigWheelLoss works!");
}

// OnSolarPanelSunUpdate
// Called when a solar panel updates the amount of energy it is getting from the sun
// Returning a non-null value overrides default behavior
object OnSolarPanelSunUpdate(SolarPanel panel, int currentEnergy)
{
    Puts("OnSolarPanelSunUpdate works!");
    return null;
}

// OnBookmarkControl
// Called when a player tries to select a bookmark at a computer station
// Returning a non-null value overrides default behavior
object OnBookmarkControl(ComputerStation computerStation, BasePlayer player, string bookmarkName, IRemoteControllable remoteControllable)
{
    Puts("OnBookmarkControl works!");
    return null;
}

// OnBookmarkControlStarted
// Called after a player has selected a bookmark at a computer station
// No return behavior
void OnBookmarkControlStarted(ComputerStation computerStation, BasePlayer player, string bookmarkName, IRemoteControllable remoteControllable)
{
    Puts("OnBookmarkControlStarted works!");
}

// OnBookmarkDelete
// Called when a player tries to delete a bookmark at a computer station
// Returning a non-null value overrides default behavior
object OnBookmarkDelete(ComputerStation computerStation, BasePlayer player, string bookmarkName)
{
    Puts("OnBookmarkDelete works!");
    return null;
}

// OnBookmarkAdd
// Called when a player tries to add a bookmark at a computer station
// Returning a non-null value overrides default behavior
object OnBookmarkAdd(ComputerStation computerStation, BasePlayer player, string bookmarkName)
{
    Puts("OnBookmarkAdd works!");
    return null;
}

// OnBookmarksSendControl
// Called when a player is being sent a list of bookmarks for a computer station
// Returning a non-null value overrides default behavior
object OnBookmarksSendControl(ComputerStation computerStation, BasePlayer player, string bookmarks)
{
    Puts("OnBookmarksSendControl works!");
    return null;
}

// OnBookmarkControlEnd
// Called when a player tries to stop viewing/controlling an entity at a computer station
// Returning a non-null value overrides default behavior
object OnBookmarkControlEnd(ComputerStation computerStation, BasePlayer player, BaseEntity controlledEntity)
{
    Puts("OnBookmarkControlEnd works!");
    return null;
}

// OnBookmarkControlEnded
// Called after a player has stopped viewing/controlling an entity at a computer station
// No return behavior
void OnBookmarkControlEnded(ComputerStation computerStation, BasePlayer player, BaseEntity controlledEntity)
{
    Puts("OnBookmarkControlEnded works!");
}

// OnRfBroadcasterAdd
// Called right before an object starts broadcasting an RF frequency
// Returning a non-null value overrides default behavior
object OnRfBroadcasterAdd(IRFObject obj, int frequency)
{
    Puts("OnRfBroadcasterAdd works!");
    return null;
}

// OnRfBroadcasterAdded
// Called right after an object has started broadcasting an RF frequency
// No return behavior
void OnRfBroadcasterAdded(IRFObject obj, int frequency)
{
    Puts("OnRfBroadcasterAdded works!");
}

// OnRfBroadcasterRemove
// Called right before an object stops broadasting an RF frequency
// Returning a non-null value overrides default behavior
object OnRfBroadcasterRemove(IRFObject obj, int frequency)
{
    Puts("OnRfBroadcasterRemove works!");
    return null;
}

// OnRfBroadcasterRemoved
// Called right after an object has stopped broadcasting an RF frequency
// No return behavior
void OnRfBroadcasterRemoved(IRFObject obj, int frequency)
{
    Puts("OnRfBroadcasterRemoved works!");
}

// OnRfListenerAdd
// Called right before an object starts listening to an RF frequency
// Returning a non-null value overrides default behavior
object OnRfListenerAdd(IRFObject obj, int frequency)
{
    Puts("OnRfListenerAdd works!");
    return null;
}

// OnRfListenerAdded
// Called right after an object has started listening to an RF frequency
// No return behavior
void OnRfListenerAdded(IRFObject obj, int frequency)
{
    Puts("OnRfListenerAdded works!");
}

// OnRfListenerRemove
// Called right before an object stops listening to an RF frequency
// Returning a non-null value overrides default behavior
object OnRfListenerRemove(IRFObject obj, int frequency)
{
    Puts("OnRfListenerRemove works!");
    return null;
}

// OnRfListenerRemoved
// Called right after an object has stopped listening to an RF frequency
// No return behavior
void OnRfListenerRemoved(IRFObject obj, int frequency)
{
    Puts("OnRfListenerRemoved works!");
}

// OnSleepingBagDestroy
// Called when a player tries to remove a sleeping bag from their respawn screen
// Returning a non-null value overrides default behavior
object OnSleepingBagDestroy(SleepingBag sleepingBag, BasePlayer player)
{
    Puts("OnSleepingBagDestroy works!");
    return null;
}

// OnRfFrequencyChange
// Called when a player tries to change the frequency of an RF broadcaster or receiver
// Returning a non-null value overrides default behavior
// Useful for preventing particular reserved frequencies from being selected
object OnRfFrequencyChange(IRFObject obj, int frequency, BasePlayer player)
{
    Puts("OnRfFrequencyChange works!");
    return null;
}

// OnRfFrequencyChanged
// Called after a player has changed the frequency of an RF broadcaster or receiver
// No return behavior
void OnRfFrequencyChanged(IRFObject obj, int frequency, BasePlayer player)
{
    Puts("OnRfFrequencyChanged works!");
}

// OnSleepingBagDestroyed
// Called after a player removes a sleeping bag from their respawn screen
// No return behavior
void OnSleepingBagDestroyed(SleepingBag sleepingBag, BasePlayer player)
{
    Puts("OnSleepingBagDestroyed works!");
}

// OnNetworkSubscriptionsUpdate
// Called after the Rust has determined which network groups to subscribe a player to (`groupsToAdd`), and which network groups to unsubscribe the player from (`groupsToRemove`)
// Returning a non-null value prevents Rust from applying the proposed subscription changes
// This hook is useful for situations where you want to subscribe a player to a network group that is outside their network range -- To do so, you can prevent Rust from automatically unsubscribing them by removing that group from the `groupsToRemove` list
object OnNetworkSubscriptionsUpdate(Network.Networkable networkable, List<Network.Visibility.Group> groupsToAdd, List<Network.Visibility.Group> groupsToRemove)
{
    Puts("OnNetworkSubscriptionsUpdate works!");
    return null;
}

// OnBookmarkInput
// Called when input is received from a player who is using a computer station with a bookmark selected
// Returning a non-null value overrides default behavior
object OnBookmarkInput(ComputerStation computerStation, BasePlayer player, InputState inputState)
{
    Puts("OnBookmarkInput works!");
    return null;
}

// OnEntityControl
// Called when a player tries to remote control an entity
// Returning true or false overrides default behavior
object OnEntityControl(IRemoteControllable entity)
{
    Puts("OnEntityControl works!");
    return null;
}

// OnTurretRotate
// Called when a player tries to rotate an auto turret
// Returning a non-null value overrides default behavior
object OnTurretRotate(AutoTurret turret, BasePlayer player)
{
    Puts("OnTurretRotate works!");
    return null;
}

// OnCounterTargetChange
// Called when a player tries to change the target number of a power counter
// Returning a non-null value overrides default behavior
object OnCounterTargetChange(PowerCounter counter, BasePlayer player, int targetNumber)
{
    Puts("OnCounterTargetChange works!");
    return null;
}

// OnCounterModeToggle
// Called when a player ties to toggle a power counter between modes
// Returning a non-null value overrides default behavior
object OnCounterModeToggle(PowerCounter counter, BasePlayer player, bool mode)
{
    Puts("OnCounterModeToggle works!");
    return null;
}

// OnSwitchToggle
// Called when a player tries to switch and ElectricSwitch or FuelGenerator
// Returning a non-null value cancels default behavior
object OnSwitchToggle(IOEntity entity, BasePlayer player)
{
    Puts("OnSwitchToggle works!");
    return null;
}

// OnSwitchToggled
// Called right after a player switches an ElectricSwitch or FuelGenerator
// No return behavior
void OnSwitchToggled(IOEntity entity, BasePlayer player)
{
    Puts("OnSwitchToggled works!");
}

// OnEntityDestroy
// Called right before a CH47Helicopter or BradleyAPC is destroyed
// Returning a non-null value overrides default behavior
object OnEntityDestroy(BaseCombatEntity entity)
{
    Puts("OnEntityDestroy works!");
    return null;
}

// OnElevatorButtonPress
// Called when a player presses a button on an elevator lift
// Returning a non-null value overrides default behavior
object OnElevatorButtonPress(ElevatorLift lift, BasePlayer player, Elevator.Direction direction, bool toTopOrBottom)
{
    Puts("OnElevatorButtonPress works!");
    return null;
}

// OnElevatorCall
// Called when an elevator lift is called to a specific floor by electricity
// Returning a non-null value overrides default behavior
object OnElevatorCall(Elevator elevator, Elevator topElevator)
{
    Puts("OnElevatorCall works!");
    return null;
}

// OnElevatorMove
// Called right before an elevator starts moving to the target floor
// Returning a non-null value overrides default behavior
object OnElevatorMove(Elevator topElevator, int targetFloor)
{
    Puts("OnElevatorMove works!");
    return null;
}

// CanSwapToSeat
// Called when a player tries to switch seats, to determine whether each seat is eligible to be swapped to
// Returning true or false overrides default behavior
bool CanSwapToSeat(BasePlayer player, BaseMountable mountable)
{
    Puts("CanSwapToSeat works!");
    return true;
}

// OnRidableAnimalClaim
// Called when a player tries to claim a horse
// Returning a non-null value overrides default behavior
object OnRidableAnimalClaim(BaseRidableAnimal animal, BasePlayer player)
{
    Puts("OnRidableAnimalClaim works!");
    return null;
}

// OnRidableAnimalClaimed
// Called after a player has claimed a horse
// No return behavior
void OnRidableAnimalClaimed(BaseRidableAnimal animal, BasePlayer player)
{
    Puts("OnRidableAnimalClaimed works!");
}

// OnEntitySaved
// Called after a BaseNetworkable has been saved to a ProtoBuf object and is about to be serialized to a network stream or network cache
// No return behavior
void OnEntitySaved(BaseNetworkable entity, BaseNetworkable.SaveInfo saveInfo)
{
    Puts("OnEntitySaved works!");
}

// OnEntitySnapshot
// Called when an entity snapshot is about to be sent to a client connection
// Returning a non-null value overrides default behavior
object OnEntitySnapshot(BaseNetworkable entity, Connection connection)
{
    Puts("OnEntitySnapshot works!");
    return null;
}

// OnIORefCleared
// Called after a wire has been disconnected from an electrical entity, such as when its connected entity was destroyed or when a player removed the wire
// No return behavior
void OnIORefCleared(IOEntity.IORef ioRef, IOEntity ioEntity)
{
    Puts("OnIORefCleared works!");
}

// OnEntityFlagsNetworkUpdate
// Called after an entity's flags have been updated on the server, before they are sent over the network
// Returning a non-null value overrides default behavior
object OnEntityFlagsNetworkUpdate(BaseEntity entity)
{
    Puts("OnEntityFlagsNetworkUpdate works!");
    return null;
}

// OnSupplyDropDropped
// Called right after a cargo plane has dropped a supply drop
// No return behavior
void OnSupplyDropDropped(SupplyDrop supplyDrop, CargoPlane cargoPlane)
{
    Puts("OnSupplyDropDropped works!");
}

// OnCargoPlaneSignaled
// Called right after a supply signal has called a cargo plane
// No return behavior
void OnCargoPlaneSignaled(CargoPlane cargoPlane, SupplySignal supplySignal)
{
    Puts("OnCargoPlaneSignaled works!");
}

// OnWaterPurify
// Called when salt water is about to be converted to fresh water in a water purifier
// Returning a non-null value cancels default behavior
object OnWaterPurify(WaterPurifier waterPurifier, float timeCooked)
{
    Puts("OnWaterPurify works!");
    return null;
}

// OnWaterPurified
// Called after salt water has been converted to fresh water in a water purifier
// No return behavior
void OnWaterPurified(WaterPurifier waterPurifier, float timeCooked)
{
    Puts("OnWaterPurified works!");
}

// OnSleepingBagValidCheck
// Called when determining if a sleeping bag is a valid respawn location for a player
// Useful in conjunction with OnRespawnInformationGiven since a custom sleeping bag will need to pass this check
// Returning true or false overrides default behavior
bool? OnSleepingBagValidCheck(SleepingBag bag, ulong targetPlayerID, bool ignoreTimers)
{
    Puts("OnSleepingBagValidCheck works!");
    return null;
}

// OnCCTVDirectionChange
// Called when a player attempts to change the direction of a CCTV camera to face them
// Returning a non-null value cancels default behavior
object OnCCTVDirectionChange(CCTV_RC camera, BasePlayer player)
{
    Puts("OnCCTVDirectionChange works!");
    return null;
}

// OnWaterCollect
// Called when a water catcher is about to collect water
// Returning a non-null value cancels default behavior
object OnWaterCollect(WaterCatcher waterCatcher)
{
    Puts("OnWaterCollect works!");
    return null;
}

// OnLiquidVesselFill
// Called when a player is attempting to fill a liquid vessel
// Returning a non-null value cancels default behavior
object OnLiquidVesselFill(BaseLiquidVessel liquidVessel, BasePlayer player, LiquidContainer facingLiquidContainer)
{
    Puts("OnLiquidVesselFill works!");
    return null;
}

// OnLockerSwap
// Called when a player clicks the "Swap" button while viewing a locker interface
// Returning non - null value overrides default behavior
object OnLockerSwap(Locker locker, int startIndex, BasePlayer player)
{
    Puts("OnLockerSwap works!");
    return null;
}

// CanLockerAcceptItem
// Called before an item is attempted to be placed inside a locker
// Returning true or false overrides default behavior
object CanLockerAcceptItem(Locker locker, Item item, int targetPos)
{
    Puts("CanLockerAcceptItem works!");
    return null;
}

// Item Hooks

// CanAcceptItem
// Called when attempting to put an item in a container
// Returning CanAcceptResult value overrides default behavior
ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
{
    Puts("CanAcceptItem works!");
    return null;
}

// OnBackpackDrop
// Called just before a backpack is dropped from a player on death.
// Return non-null to override default behaviour.
object OnBackpackDrop(Item backpack, BasePlayer player)
{
    Puts("OnBackpackDrop works!");
    return null;
}

// OnCardSwipe
// Called when a player is trying to swipe a card
// Returning a non-null value cancels default behavior
object OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
{
    Puts("OnCardSwipe works!");
    return null;
}

// OnItemRemove
// Called before an item is destroyed
// Return a non-null value stop item from being destroyed
object OnItemRemove(Item item)
{
    Puts("OnItemRemove works!");
    return null;
}

// OnMapImageUpdated
// Called when player updates map item's image
// Useful for executing any action when map image is updated
// No return behavior
void OnMapImageUpdated()
{
    Puts("OnMapImageUpdated works!");
}

// CanDropActiveItem
// Called when a player attempts to drop their active item
// Returning true or false overrides default behavior
bool CanDropActiveItem(BasePlayer player)
{
    Puts("CanDropActiveItem works!");
    return true;
}

// CanCombineDroppedItem
// Called when an item is dropped on another item
// Returning a non-null value overwrites command arguments
object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
{
    Puts("CanCombineDroppedItem works!");
    return null;
}

// CanMoveItem
// Called when moving an item from one inventory slot to another
// Returning a non-null value overrides default behavior
object CanMoveItem(Item item, PlayerInventory playerLoot, ItemContainerId targetContainer, int targetSlot, int amount, ItemMoveModifier itemMoveModifier)
{
    Puts("CanMoveItem works!");
    return null;
}

// CanStackItem
// Called when moving an item onto another item
// Returning true or false overrides default behavior
bool CanStackItem(Item item, Item targetItem)
{
    Puts("CanStackItem works!");
    return true;
}

// OnFuelConsumed
// Called after fuel is used (furnace, lanterns, camp fires, etc.)
// No return behavior
void OnFuelConsumed(BaseOven oven, Item fuel, ItemModBurnable burnable)
{
    Puts("OnFuelConsumed works!");
}

// OnFuelConsume
// Called right before fuel is used (furnace, lanterns, camp fires, etc.)
// Returning a non-null value overrides default behavior
object OnFuelConsume(BaseOven oven, Item fuel, ItemModBurnable burnable)
{
    Puts("OnFuelConsume works!");
    return null;
}

// OnFindBurnable
// Called when looking for fuel for the oven
// Returning an Item overrides default behavior
Item OnFindBurnable(BaseOven oven)
{
    Puts("OnFindBurnable works!");
    return null;
}

// OnHealingItemUse
// Called when a player attempts to use a medical tool
// Returning a non-null value overrides default behavior
object OnHealingItemUse(MedicalTool tool, BasePlayer player)
{
    Puts("OnHealingItemUse works!");
    return null;
}

// OnItemAction
// Called when a button is clicked on an item in the inventory (drop, unwrap, ...)
// Returning a non-null value overrides default behavior
object OnItemAction(Item item, string action, BasePlayer player)
{
    Puts("OnItemAction works!");
    return null;
}

// OnItemAddedToContainer
// Called right after an item was added to a container
// An entire stack has to be created, not just adding more wood to a wood stack for example
// No return behavior
void OnItemAddedToContainer(ItemContainer container, Item item)
{
    Puts("OnItemAddedToContainer works!");
}

// OnItemCraft
// Called just before an item is added to the crafting queue
// Returning true or false overrides default behavior
object OnItemCraft(ItemCraftTask task, BasePlayer player, Item item)
{
    Puts("OnItemCraft works!");
    return null;
}

// OnItemCraftCancelled
// Called before an item has been crafted
// No return behavior
void OnItemCraftCancelled(ItemCraftTask task)
{
    Puts("OnItemCraftCancelled works!");
}

// OnItemCraftFinished
// Called right after an item has been crafted
// No return behavior
void OnItemCraftFinished(ItemCraftTask task, Item item)
{
    Puts("OnItemCraftFinished works!");
}

// OnItemDeployed
// Called right after an item has been deployed
// No return behavior
void OnItemDeployed(Deployer deployer, BaseEntity entity, BaseEntity slotEntity)
{
    Puts("OnItemDeployed works!");
}

// OnItemDropped
// Called right after an item has been dropped
// No return behavior
void OnItemDropped(Item item, BaseEntity entity)
{
    Puts("OnItemDropped works!");
}

// OnItemPickup
// Called right after an item has been picked up
// Returning a non-null value overrides default behavior
object OnItemPickup(Item item, BasePlayer player)
{
    Puts("OnItemPickup works!");
    return null;
}

// OnItemRemovedFromContainer
// Called right after an item was removed from a container
// The entire stack has to be removed for this to be called, not just a little bit
// No return behavior
void OnItemRemovedFromContainer(ItemContainer container, Item item)
{
    Puts("OnItemRemovedFromContainer works!");
}

// OnItemRepair
// Called right before an item is repaired
// Returning a non-null value overrides default behavior
object OnItemRepair(BasePlayer player, Item item)
{
    Puts("OnItemRepair works!");
    return null;
}

// OnItemResearch
// Called right before a player begins to research an item
// No return behavior
void OnItemResearch(ResearchTable table, Item targetItem, BasePlayer player)
{
    Puts("OnItemResearch works!");
}

// OnItemResearched
// Called right before a player finishes researching an item
// Returning a float will affect if researching is successful or not
float OnItemResearched(ResearchTable table, float chance)
{
    Puts("OnItemResearched works!");
    return 1;
}

// OnResearchCostDetermine
// Called when an item is being scrapped at a research table or when a blueprint is being unlocked in a tech tree
// Returning a numeric value (int) overrides the default value
object OnResearchCostDetermine(Item item, ResearchTable researchTable)
{
    Puts("OnResearchCostDetermine works!");
    return null;
}

object OnResearchCostDetermine(ItemDefinition itemDefinition)
{
    Puts("OnResearchCostDetermine works!");
    return null;
}

// OnItemSplit
// Called right before an item is split into multiple stacks
// Returning an Item overrides default behavior
Item OnItemSplit(Item item, int amount)
{
    Puts("OnItemSplit works!");
    return null;
}

// OnItemUpgrade
// Called right before an item is upgraded
// No return behavior
void OnItemUpgrade(Item item, Item upgraded, BasePlayer player)
{
    Puts("OnItemUpgrade works!");
}

// OnItemUse
// Called when an item is used
// Returning an int overrides the amount consumed.
int OnItemUse(Item item, int amountToUse)
{
    Puts("OnItemUse works!");
    return amountToUse;
}

// OnLoseCondition
// Called right before the condition of the item is modified
// No return behavior
void OnLoseCondition(Item item, ref float amount)
{
    Puts("OnLoseCondition works!");
}

// OnMaxStackable
// Called when an items max stackable is calculated
// Returning a numeric value (int) overrides the default value
int OnMaxStackable(Item item)
{
    Puts("OnMaxStackable works!");
    return 1;
}

// OnTrapArm
// Called when the player arms a bear trap
// Returning a non-null value overrides default behavior
object OnTrapArm(BearTrap trap, BasePlayer player)
{
    Puts("OnTrapArm works!");
    return null;
}

// OnTrapDisarm
// Called when the player disarms a land mine
// Returning a non-null value overrides default behavior
object OnTrapDisarm(Landmine trap, BasePlayer player)
{
    Puts("OnTrapDisarm works!");
    return null;
}

// OnTrapSnapped
// Called when a trap is triggered by a game object
// No return behavior
void OnTrapSnapped(BaseTrapTrigger trap, GameObject go)
{
    Puts("OnTrapSnapped works!");
}

// OnTrapTrigger
// Called when a trap is triggered by a game object
// Returning a non-null value overrides default behavior
object OnTrapTrigger(BaseTrap trap, GameObject go)
{
    Puts("OnTrapTrigger works!");
    return null;
}

// OnBonusItemDrop
// Called when a loot container is about to drop bonus scrap for a player who has a corresponding tea buff
// Returning a non-null value overrides default behavior
object OnBonusItemDrop(Item item, BasePlayer player)
{
    Puts("OnBonusItemDrop works!");
    return null;
}

// OnBonusItemDropped
// Called after a loot container has dropped bonus scrap for a player who has a corresponding tea buff
// No return behavior
void OnBonusItemDropped(Item item, BasePlayer player)
{
    Puts("OnBonusItemDropped works!");
}

// OnItemRefill
// Called right before an item such as a diving tank is repaired without using a repair bench
// Returning a non-null value overrides default behavior
object OnItemRefill(Item item, BasePlayer player)
{
    Puts("OnItemRefill works!");
    return null;
}

// OnItemLock
// Called right before an item is locked, such as in a modular car inventory
// Returning a non-null value overrides default behavior
object OnItemLock(Item item)
{
    Puts("OnItemLock works!");
    return null;
}

// OnItemUnlock
// Called right before an item is unlocked, such as in a modular car inventory
// Returning a non-null value overrides default behavior
object OnItemUnlock(Item item)
{
    Puts("OnItemUnlock works!");
    return null;
}

// OnItemSubmit
// Called when a player submits an item into a mailbox or dropbox
// Returning a non-null value cancels default behavior
object OnItemSubmit(Item item, Mailbox mailbox, BasePlayer player)
{
    Puts("OnItemSubmit works!");
    return null;
}

// OnItemStacked
// Called after an item has been stacked
// No return behavior
void OnItemStacked(Item destinationItem, Item sourceItem, ItemContainer destinationContainer)
{
    Puts("OnItemStacked works!");
}

// OnIngredientsCollect
// Called when ingredients are about to be collected for crafting an item
// Returning a non-null value cancels default behavior
bool? OnIngredientsCollect(ItemCrafter itemCrafter, ItemBlueprint blueprint, ItemCraftTask task, int amount, BasePlayer player)
{
    Puts("OnIngredientsCollect works!");
    return null;
}

// Resource Hooks

// OnCollectiblePickup
// Called when the player collects an item
// Returning a non-null value overrides default behavior
object OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
{
    Puts("OnCollectiblePickup works!");
    return null;
}

// CanTakeCutting
// Called when a player is trying to take a cutting (clone) of a GrowableEntity
// Returning a non-null value cancels default behavior
object CanTakeCutting(BasePlayer player, GrowableEntity entity)
{
    Puts("CanTakeCutting works!");
    return null;
}

// OnQuarryToggled
// Called when a quarry has just been toggled
// No return behavior
void OnQuarryToggled(MiningQuarry quarry, BasePlayer player)
{
    Puts($"{player.displayName} has toggled a quarry");
}

// OnGrowableGathered
// Called before the player receives an item from gathering a growable entity
// No return behavior
void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
{
    Puts($"{player.displayName} has gathered {item.info.shortname} x {item.amount}.");
}

// OnRemoveDying
// Called when a player is trying to harvest a dying growable entity
// Returning a non-null value overrides default behavior
object OnRemoveDying(GrowableEntity plant, BasePlayer player)
{
    Puts("OnRemoveDying works!");
    return null;
}

// OnGrowableGather
// Called when the player gathers a growable entity
// Returning a non-null value overrides default behavior
object OnGrowableGather(GrowableEntity plant, BasePlayer player)
{
    Puts("OnGrowableGather works!");
    return null;
}

// OnDispenserBonus
// Called before the player is given a bonus item for gathering
// Returning an Item replaces the existing Item
void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
{
    Puts("OnDispenserBonus works!");
}

// OnDispenserGather
// Called before the player is given items from a resource
// Returning a non-null value overrides default behavior
object OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
{
    Puts("OnDispenserGather works!");
    return null;
}

// OnQuarryEnabled
// Called when a mining quarry is turned on/enabled
// No return behavior
void OnQuarryEnabled(MiningQuarry quarry)
{
    Puts("OnQuarryEnabled works!");
}

// OnTreeMarkerHit
// Called when a player hits a tree with a tool (rock, hatchet, etc.)
// Returning true or false overrides default behaviour
bool? OnTreeMarkerHit(TreeEntity tree, HitInfo info)
{
    Puts("OnTreeMarkerHit works!");
    return null;
}

// OnQuarryGather
// Called before items are gathered from a quarry
// No return behavior
void OnQuarryGather(MiningQuarry quarry, Item item)
{
    Puts("OnQuarryGather works!");
}

// OnSurveyGather
// Called before items are gathered from a survey charge
// No return behavior
void OnSurveyGather(SurveyCharge survey, Item item)
{
    Puts("OnSurveyGather works!");
}

// Structure Hooks

// OnCodeEntered
// Called when the player has entered a code in a codelock
// Returning a non-null value overrides default behavior
object OnCodeEntered(CodeLock codeLock, BasePlayer player, string code)
{
    Puts("OnCodeEntered works!");
    return null;
}

// OnCupboardAuthorize
// Called when a cupboard attempts to authorize a player
// Returning a non-null value overrides default behavior
object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
{
    Puts("OnCupboardAuthorize works!");
    return null;
}

// OnCupboardClearList
// Called when an attempt is made to clear a cupboard authorized list
// Returning a non-null value overrides default behavior
object OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player)
{
    Puts("OnCupboardClearList works!");
    return null;
}

// OnCupboardDeauthorize
// Called when a cupboard attempts to deauthorize a player
// Returning a non-null value overrides default behavior
object OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
{
    Puts("OnCupboardDeauthorize works!");
    return null;
}

// OnDoorClosed
// Called when the player closed a door
// No return behavior
void OnDoorClosed(Door door, BasePlayer player)
{
    Puts("OnDoorClosed works!");
}

// OnDoorOpened
// Called when the player opened a door
// No return behavior
void OnDoorOpened(Door door, BasePlayer player)
{
    Puts("OnDoorOpened works!");
}

// OnDoorKnocked
// Called when the player knocks on a door
// No return behavior
void OnDoorKnocked(Door door, BasePlayer player)
{
    Puts("OnDoorKnocked works!");
}

// OnEntityBuilt
// Called when any structure is built (walls, ceilings, stairs, etc.)
// No return behavior
void OnEntityBuilt(Planner plan, GameObject go)
{
    Puts("OnEntityBuilt works!");
}

// OnHammerHit
// Called when the player has hit something with a hammer
// Returning a non-null value overrides default behavior
object OnHammerHit(BasePlayer player, HitInfo info)
{
    Puts("OnHammerHit works!");
    return null;
}

// OnStructureDemolish
// Called when the player selects Demolish or DemolishImmediate from the BuildingBlock or BaseCombatEntity menu
// Returning a non-null value overrides default behavior
object OnStructureDemolish(BaseCombatEntity entity, BasePlayer player, bool immediate)
{
    Puts("OnStructureDemolish works!");
    return null;
}

// OnStructureRepair
// Called when the player repairs a BuildingBlock or BaseCombatEntity
// Returning a non-null value cancels repair
object OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
{
    Puts("OnStructureRepair works!");
    return null;
}

// OnStructureRotate
// Called when the player rotates a BuildingBlock or BaseCombatEntity
// Returning a non-null value cancels rotate
object OnStructureRotate(BaseCombatEntity entity, BasePlayer player)
{
    Puts("OnStructureRotate works!");
    return null;
}

// OnStructureUpgrade
// Called when the player upgrades the grade of a BuildingBlock or BaseCombatEntity
// Returning a non-null value overrides default behavior
object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
{
    Puts("OnStructureUpgrade works!");
    return null;
}

// OnConstructionPlace
// Called when a player tries to place a building block
// Returning a non-null value overrides default behavior
object OnConstructionPlace(BaseEntity entity, Construction component, Construction.Target constructionTarget, BasePlayer player)
{
    Puts("OnConstructionPlace works!");
    return null;
}

// OnBuildingSplit
// Called when a building is split into two
// No return behavior
void OnBuildingSplit(BuildingManager.Building building, uint newBuildingId)
{
    Puts("OnBuildingSplit works!");
}

// Terrain Hooks

// OnTerrainInitialized
// Called after the terrain generation process has completed
// No return behavior
void OnTerrainInitialized()
{
    Puts("OnTerrainInitialized works!");
}

// Vending Hooks

// CanAdministerVending
// Called when a player attempts to administer a vending machine
// Returning true or false overrides default behavior
bool CanAdministerVending(BasePlayer player, VendingMachine machine)
{
    Puts("CanAdministerVending works!");
    return true;
}

// OnTakeCurrencyItem
// Called before currency item is taken
// Returning a non-null value cancels default behavior
object OnTakeCurrencyItem(VendingMachine vending, Item item)
{
    Puts("OnTakeCurrencyItem works!");
    return null;
}

// OnGiveSoldItem
// Called before a sold item is given
// Returning a non-null value cancels default behavior
object OnGiveSoldItem(NPCVendingMachine vending, Item soldItem, BasePlayer buyer)
{
    Puts("OnGiveSoldItem works!");
    return null;
}

object OnGiveSoldItem(VendingMachine vending, Item soldItem, BasePlayer buyer)
{
    Puts("OnGiveSoldItem works!");
    return null;
}

// OnVendingShopRename
// Called when a player tries to rename vending shop
// Returning a non-null value cancels default behavior
object OnVendingShopRename(VendingMachine vending, string newName, BasePlayer player)
{
    Puts("OnVendingShopRename works!");
    return null;
}

// CanUseVending
// Called when a player attempts to use a vending machine
// Returning true or false overrides default behavior
bool CanUseVending(BasePlayer player, VendingMachine machine)
{
    Puts("CanUseVending works!");
    return true;
}

// CanVendingAcceptItem
// Called when a player attempts to administer a vending machine
// Returning true or false overrides default behavior
bool CanVendingAcceptItem(VendingMachine vending, Item item, int targetPos)
{
    Puts("CanVendingAcceptItem works!");
    return true;
}

// OnAddVendingOffer
// Called when a sell order/offer is added to a vending machine
// No return behavior
void OnAddVendingOffer(VendingMachine machine, ProtoBuf.VendingMachine.SellOrder sellOrder)
{
    Puts("OnAddVendingOffer works!");
}

// OnBuyVendingItem
// Called when a player buys an item from a vending machine
// Returning a non-null value overrides default behavior
object OnBuyVendingItem(VendingMachine machine, BasePlayer player, int sellOrderId, int numberOfTransactions)
{
    Puts("OnBuyVendingItem works!");
    return null;
}

// OnDeleteVendingOffer
// Called when a sell order/offer is deleted from a vending machine
// No return behavior
void OnDeleteVendingOffer(VendingMachine machine, int offerId)
{
    Puts("OnDeleteVendingOffer works!");
}

// OnOpenVendingAdmin
// Called when a player opens the admin ui for a vending machine
// No return behavior
void OnOpenVendingAdmin(VendingMachine machine, BasePlayer player)
{
    Puts("OnOpenVendingAdmin works!");
}

// OnVendingShopOpened
// Called when a player opens the customer ui for a vending machine
// No return behavior
void OnVendingShopOpened(VendingMachine machine, BasePlayer player)
{
    Puts("OnVendingShopOpened works!");
}

// OnRefreshVendingStock
// Called when the stock on a vending machine is updated
// No return behavior
void OnRefreshVendingStock(VendingMachine machine, Item item)
{
    Puts("OnRefreshVendingStock works!");
}

// OnRotateVendingMachine
// Called when a player attempts to rotate a vending machine
// Returning a non-null value overrides default behavior
object OnRotateVendingMachine(VendingMachine machine, BasePlayer player)
{
    Puts("OnRotateVendingMachine works!");
    return null;
}

// OnToggleVendingBroadcast
// Called when a player toggles the broadcasting of the vending machine
// No return behavior
void OnToggleVendingBroadcast(VendingMachine machine, BasePlayer player)
{
    Puts("OnToggleVendingBroadcast works!");
}

// OnVendingTransaction
// Called when a player attempts to buy an item from a vending machine
// Returning true or false overrides default behavior
bool OnVendingTransaction(VendingMachine machine, BasePlayer buyer, int sellOrderId, int numberOfTransactions)
{
    Puts("OnVendingTransaction works!");
    return true;
}

// OnNpcGiveSoldItem
// Called before a non-player controlled vending machine (at outpost etc.) gives the player the item they purchased.
// Returning a non-null value overrides default behaviour.
object OnNpcGiveSoldItem(NPCVendingMachine machine, Item soldItem, BasePlayer buyer)
{
    Puts("OnNpcGiveSoldItem works!");
    return null;
}

// Weapon Hooks

// CanCreateWorldProjectile
// Called when the item creates a projectile in the world
// Returning a non-null value overrides default behavior
object CanCreateWorldProjectile(HitInfo info, ItemDefinition itemDef)
{
    Puts("CanCreateWorldProjectile works!");
    return null;
}

// OnProjectileRicochet
// Called when a player's weapon projectile ricochets
// Returning a non-null value overrides default behavior
object OnProjectileRicochet(BasePlayer player, PlayerProjectileRicochet ricochet)
{
    Puts("OnProjectileRicochet works!");
    return null;
}

// OnExplosiveDud
// Called when explosive tries to become dud
// Returning a non-null value cancels default behavior
object OnExplosiveDud(DudTimedExplosive explosive)
{
    Puts("OnExplosiveDud works!");
    return null;
}

// OnAmmoUnload
// Called when a player is trying to unload ammo
// Returning a non-null value cancels default behavior
object OnAmmoUnload(BaseProjectile projectile, Item item, BasePlayer player)
{
    Puts("OnAmmoUnload works!");
    return null;
}

// OnExplosiveFuseSet
// Called when a fuse of an explosive is set
// Returning a non-null value overwrites fuse length
object OnExplosiveFuseSet(TimedExplosive explosive, float fuseLength)
{
    Puts("OnExplosiveFuseSet works!");
    return null;
}

// CanExplosiveStick
// Called when a Timed Explosive is attempting to stick to another entity
// Returning a non-null value overwrites the default behavior
object CanExplosiveStick(TimedExplosive explosive, BaseEntity entity)
{
    Puts("CanExplosiveStick works!");
    return null;
}

// OnWorldProjectileCreate
// Called when a projectile is created
// Returning a non-null value overrides default behavior
object OnWorldProjectileCreate(HitInfo hitInfo, Item item)
{
    Puts("OnWorldProjectileCreate works!");
    return null;
}

// OnExplosiveDropped
// Called when the player drops an explosive item (C4, grenade, ...)
// No return behavior
void OnExplosiveDropped(BasePlayer player, BaseEntity entity, ThrownWeapon item)
{
    Puts("OnExplosiveDropped works!");
}

// OnExplosiveThrown
// Called when the player throws an explosive item (C4, grenade, ...)
// No return behavior
void OnExplosiveThrown(BasePlayer player, BaseEntity entity, ThrownWeapon item)
{
    Puts("OnExplosiveThrown works!");
}

// OnFlameThrowerBurn
// Called when the burn from a flame thrower spreads
// No return behavior
void OnFlameThrowerBurn(FlameThrower thrower, BaseEntity flame)
{
    Puts("OnFlameThrowerBurn works!");
}

// OnMeleeThrown
// Called when the player throws a melee item (axe, rock, ...)
// No return behavior
void OnMeleeThrown(BasePlayer player, Item item)
{
    Puts("OnMeleeThrown works!");
}

// OnMagazineReload
// Called when the player reloads a magazine
// Returning a non-null value overrides default behavior
object OnMagazineReload(BaseProjectile weapon, IAmmoContainer desiredAmount, BasePlayer player)
{
    Puts("OnMagazineReload works!");
    return null;
}

// OnWeaponReload
// Called when the player reloads a weapon
// Returning a non-null value overrides default behavior
object OnWeaponReload(BaseProjectile weapon, BasePlayer player)
{
    Puts("OnWeaponReload works!");
    return null;
}

// OnRocketLaunched
// Called when the player launches a rocket
// No return behavior
void OnRocketLaunched(BasePlayer player, BaseEntity entity)
{
    Puts("OnRocketLaunched works!");
}

// OnAmmoSwitch
// Called when the player starts to switch the ammo in a weapon
// Returning a non-null value overrides default behavior
object OnAmmoSwitch(BaseProjectile weapon, BasePlayer player)
{
    Puts("OnAmmoSwitch works!");
    return null;
}

// OnWeaponFired
// Called when the player fires a weapon
// No return behavior
void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
{
    Puts("OnWeaponFired works!");
}

// Vehicle Hooks

// CanUseHelicopter
// Useful for denying to mount a CH47 helicopter
// Returning a non-null value cancels default behavior
object CanUseHelicopter(BasePlayer player, CH47HelicopterAIController helicopter)
{
    Puts("CanUseHelicopter works!");
    return null;
}

// OnBoatPathGenerate
// Called when generating ocean patrol path for CargoShip
// Returning a List<Vector3> overrides default behavior
List<Vector3> OnBoatPathGenerate()
{
    Puts("OnBoatPathGenerate works!");
    return null;
}

// OnVehicleModuleMove
// Called when a player tries to move a vehicle module item that is currently on a vehicle
// Returning a non-null value overrides default behavior
object OnVehicleModuleMove(BaseVehicleModule module, BaseModularVehicle vehicle, BasePlayer player)
{
    Puts("OnVehicleModuleMove works!");
    return null;
}

// OnEngineStart
// Called when a player tries to start a vehicle engine
// Returning a non-null value overrides default behavior
object OnEngineStart(BaseVehicle vehicle, BasePlayer driver)
{
    Puts("OnEngineStart works!");
    return null;
}

// OnEngineStarted
// Called right after a vehicle engine has started
// No return behavior
void OnEngineStarted(BaseVehicle vehicle, BasePlayer driver)
{
    Puts("OnEngineStarted works!");
}

// OnEngineStopped
// Called right after a vehicle engine has stopped
// No return behavior
void OnEngineStopped(BaseVehicle vehicle)
{
    Puts("OnEngineStopped works!");
}

// OnEngineStop
// Called when a vehicle engine is about to stop
// Returning a non-null value overrides default behavior
object OnEngineStop(BaseVehicle vehicle)
{
    Puts("OnEngineStop works!");
    return null;
}

// OnEngineStatsRefresh
// Called right before the stats of a modular car engine are refreshed
// Returning a non-null value overrides default behavior
object OnEngineStatsRefresh(VehicleModuleEngine engineModule, EngineStorage engineStorage)
{
    Puts("OnEngineStatsRefresh works!");
    return null;
}

// OnEngineStatsRefreshed
// Called right after the stats of a modular car engine are refreshed
// No return behavior
void OnEngineStatsRefreshed(VehicleModuleEngine engineModule, EngineStorage engineStorage)
{
    Puts("OnEngineStatsRefreshed works!");
}

// OnVehicleModuleSelect
// Called right after a player has selected a vehicle module item in a car inventory, but before they are shown the corresponding storage container
// Returning a non-null value overrides default behavior
object OnVehicleModuleSelect(Item moduleItem, ModularCarGarage carLift, BasePlayer player)
{
    Puts("OnVehicleModuleSelect works!");
    return null;
}

// OnVehicleModuleSelected
// Called right after a player has selected a vehicle module item in a car's inventory, and after they have been shown the corresponding storage container if applicable
// No return behavior
void OnVehicleModuleSelected(Item moduleItem, ModularCarGarage carLift, BasePlayer player)
{
    Puts("OnVehicleModuleSelected works!");
}

// OnVehicleModuleDeselected
// Called right after a player deselects a vehicle module item in a car's inventory
// No return behavior
void OnVehicleModuleDeselected(ModularCarGarage carLift, BasePlayer player)
{
    Puts("OnVehicleModuleDeselected works!");
}

// OnHotAirBalloonToggle
// Called when a player tries to toggle a hot air balloon on or off
// Returning a non-null value overrides default behavior
object OnHotAirBalloonToggle(HotAirBalloon balloon, BasePlayer player)
{
    Puts("OnHotAirBalloonToggle works!");
    return null;
}

// OnHotAirBalloonToggled
// Called right after a player has toggled a hot air balloon on or off
// No return behavior
void OnHotAirBalloonToggled(HotAirBalloon balloon, BasePlayer player)
{
    Puts("OnHotAirBalloonToggled works!");
}

// OnVehicleModulesAssign
// Called right after a modular car has spawned, but before module items are added to its inventory from a preset
// Returning a non-null value overrides default behavior
object OnVehicleModulesAssign(ModularCar car, ItemModVehicleModule[] modulePreset)
{
    Puts("OnVehicleModulesAssign works!");
    return null;
}

// OnVehicleModulesAssigned
// Called right after a car has spawned and its module inventory has been filled with module items from a preset
// No return behavior
void OnVehicleModulesAssigned(ModularCar car, ItemModVehicleModule[] modulePreset)
{
    Puts("OnVehicleModulesAssigned works!");
}

// OnVehiclePush
// Called when a player tries to push a vehicle
// Returning a non-null value overrides default behavior
object OnVehiclePush(BaseVehicle vehicle, BasePlayer player)
{
    Puts("OnVehiclePush works!");
    return null;
}

// CanCheckFuel
// Called when a player tries to loot a vehicle's fuel container
// Returning true or false overrides default behavior
object CanCheckFuel(EntityFuelSystem fuelSystem, StorageContainer fuelContainer, BasePlayer player)
{
    Puts("CanCheckFuel works!");
    return null;
}

// CanUseFuel
// Called before a vehicle fuel system consumes fuel
// Returning true or false overrides default behavior
object CanUseFuel(EntityFuelSystem fuelSystem, StorageContainer fuelContainer, float currentSeconds, float fuelPerSecond)
{
    Puts("CanUseFuel works!");
    return null;
}

// OnFuelCheck
// Called when determining whether a vehicle has sufficient fuel
// Returning true or false overrides default behavior
object OnFuelCheck(EntityFuelSystem fuelSystem)
{
    Puts("OnFuelCheck works!");
    return null;
}

// OnFuelAmountCheck
// Called when the amount of fuel in a vehicle is being determined
// Returning a numeric value (int) overrides the default value
object OnFuelAmountCheck(EntityFuelSystem fuelSystem, Item fuelItem)
{
    Puts("OnFuelAmountCheck works!");
    return null;
}

// OnFuelItemCheck
// Called when determining which item should be used to fuel a vehicle
// Returning an Item overrides default behavior
object OnFuelItemCheck(EntityFuelSystem fuelSystem, StorageContainer fuelContainer)
{
    Puts("OnFuelItemCheck works!");
    return null;
}

// Team Hooks

// OnTeamCreate
// Useful for canceling team creation
// Returning a non-null value cancels default behavior
object OnTeamCreate(BasePlayer player)
{
    Puts("OnTeamCreate works!");
    return null;
}

// OnTeamInvite
// Useful for canceling an invitation
// Returning a non-null value cancels default behavior
object OnTeamInvite(BasePlayer inviter, BasePlayer target)
{
    Puts($"{inviter.displayName} invited {target.displayName} to his team");
    return null;
}

// OnTeamRejectInvite
// Useful for canceling the invitation rejection
// Returning a non-null value cancels default behavior
object OnTeamRejectInvite(BasePlayer rejector, RelationshipManager.PlayerTeam team)
{
    Puts("OnTeamRejectInvite works!");
    return null;
}

// OnTeamPromote
// Useful for canceling player's promotion in the team
// Returning a non-null value cancels default behavior
object OnTeamPromote(RelationshipManager.PlayerTeam team, BasePlayer newLeader)
{
    Puts("OnTeamPromote works!");
    return null;
}

// OnTeamLeave
// Useful for canceling the leave from the team
// Returning a non-null value cancels default behavior
object OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
{
    Puts("OnTeamLeave works!");
    return null;
}

// OnTeamKick
// Useful for canceling kick of the player from the team
// Returning a non-null value cancels default behavior
object OnTeamKick(RelationshipManager.PlayerTeam team, BasePlayer player, ulong target)
{
    Puts("OnTeamKick works!");
    return null;
}

// OnTeamAcceptInvite
// Useful for canceling team invitation acceptation
// Returning a non-null value cancels default behavior
object OnTeamAcceptInvite(RelationshipManager.PlayerTeam team, BasePlayer player)
{
    Puts("OnTeamAcceptInvite works!");
    return null;
}

// OnTeamDisband
// Useful for canceling team disbandment
// Returning a non-null value cancels default behavior
object OnTeamDisband(RelationshipManager.PlayerTeam team)
{
    Puts("OnTeamDisband works!");
    return null;
}

// OnTeamDisbanded
// Called when the team was disbanded
// No return behavior
void OnTeamDisbanded(RelationshipManager.PlayerTeam team)
{
    Puts("OnTeamDisbanded works!");
}

// OnTeamUpdate
// Called when player's team is updated
// Returning a non-null value cancels default behavior
object OnTeamUpdate(ulong currentTeam, ulong newTeam, BasePlayer player)
{
    Puts("OnTeamUpdate works!");
    return null;
}

// OnTeamUpdated
// Called before sending team info to player
// Returning a non-null value cancels default behavior
object OnTeamUpdated(ulong currentTeam, PlayerTeam playerTeam, BasePlayer player)
{
    Puts("OnTeamUpdate works!");
    return null;
}

// OnTeamCreated
// Called after a team was created
// No return behavior
void OnTeamCreated(BasePlayer player, RelationshipManager.PlayerTeam team)
{
    Puts("OnTeamCreated works!");
}

// World Hooks

// OnWorldPrefabSpawned
// Called when a world prefab was spawned
// No return behavior
void OnWorldPrefabSpawned(GameObject gameObject, string category)
{
    Puts("OnWorldPrefabSpawned works!");
}

// Fishing Hooks

// OnFishCaught
// Called after a fish is caught
// No return behavior
void OnFishCaught(ItemDefinition definition, BaseFishingRod rod, BasePlayer player)
{
    Puts($"A fish ({definition.shortname}) has just been caught by {player.displayName}!");
}

// CanCatchFish
// Called before a fish is caught
// Returning true or false overrides default behavior
bool? CanCatchFish(BasePlayer player, BaseFishingRod rod, Item item)
{
    Puts("can we catch em fishes?");
    return null;
}

// OnFishCatch
// Called after a fish is caught, before the item is given
// Returning a non-null value overrides default behavior
Item OnFishCatch(Item item, BaseFishingRod rod, BasePlayer player)
{
    Puts($"{player.displayName} has just caught {item.amount} x {item.info.shortname}!");
    return null;
}

// OnFishingStopped
// Called just after the fishing minigame has been stopped.
// No return behaviour
void OnFishingStopped(BaseFishingRod rod, BaseFishingRod.FailReason reason)
{
    Puts("OnFishingStopped works!");
}

// Electronic Hooks

// OnExcavatorSuppliesRequest
// Called just after the excavator supply computer is triggered, before the plane is spawned.
// Returning non-null overrides default behaviour.
object OnExcavatorSuppliesRequest(ExcavatorSignalComputer computer, BasePlayer player)
{
    Puts("OnExcavatorSuppliesRequest works!");
    return null;
}

// OnExcavatorSuppliesRequested
// Called after the excavator signal computer was triggered, just after the supply plane is spawned.
void OnExcavatorSuppliesRequested(ExcavatorSignalComputer computer, BasePlayer player, BaseEntity cargoPlane)
{
    Puts("OnExcavatorSuppliesRequested works!");
}

// Clan Hooks

// OnClanLogoChanged
// Called after a player changes a clan logo
// No return behavior
void OnClanLogoChanged(LocalClan clan, byte[] logo, ulong steamId)
{
    Puts("OnClanLogoChanged works!");
}

// OnClanColorChanged
// Called after a player changes a clan color
// No return behavior
void OnClanColorChanged(LocalClan clan, Color32 color, ulong steamId)
{
    Puts("OnClanColorChanged works!");
}

// OnClanMemberKicked
// Called after a player has kicked another player from a clan
// No return behavior
void OnClanMemberKicked(LocalClan clan, ulong steamId, ulong bySteamId)
{
    Puts("OnClanMemberKicked works!");
}

// OnClanMemberLeft
// Called after a player has voluntarily left a clan
// No return behavior
void OnClanMemberLeft(LocalClan clan, ulong steamId)
{
    Puts("OnClanMemberLeft works!");
}

// OnClanMemberAdded
// Called after a player has accepted a clan invite
// No return behavior
void OnClanMemberAdded(long clanId, ulong steamId)
{
    Puts("OnClanMemberAdded works!");
}

// OnClanCreated
// Called after a player has created a clan
// No return behavior
void OnClanCreated(LocalClan clan, ulong steamId)
{
    Puts("OnClanCreated works!");
}

// OnClanDisbanded
// Called after a player disbands a clan
// No return behavior
void OnClanDisbanded(LocalClan clan, ulong steamId)
{
    Puts("OnClanDisbanded works!");
}

// Sign Hooks

// OnSignLocked
// Called after the player has locked a sign or photo frame
// No return behavior
void OnSignLocked(Signage sign, BasePlayer player)
{
    Puts("OnSignLocked works!");
}

void OnSignLocked(PhotoFrame photoFrame, BasePlayer player)
{
    Puts("OnSignLocked works!");
}

// OnSignUpdated
// Called after the player has changed the text on a sign or updated a photo frame
// No return behavior
void OnSignUpdated(Signage sign, BasePlayer player, int textureIndex)
{
    Puts("OnSignUpdated works!");
}

void OnSignUpdated(PhotoFrame photoFrame, BasePlayer player)
{
    Puts("OnSignUpdated works!");
}

void OnSignUpdated(CarvablePumpkin pumpkin, BasePlayer player)
{
    Puts("OnSignUpdated works!");
}

// OnSpinWheel
// Called when the player spins a spinner wheel
// No return behavior
void OnSpinWheel(BasePlayer player, SpinnerWheel wheel)
{
    Puts("OnSpinWheel works!");
}

// TechTree Hooks

// CanUnlockTechTreeNode
// Called when a player is attempting to unlock a blueprint in a tech tree
// Returning true or false overrides default behavior
// Useful for bypassing unlock requirements or disallowing particular blueprints from being unlocked
object CanUnlockTechTreeNode(BasePlayer player, TechTreeData.NodeInstance node, TechTreeData techTree)
{
    Puts("CanUnlockTechTreeNode works!");
    return null;
}

// CanUnlockTechTreeNodePath
// Called when a player is attempting to unlock a blueprint in a tech tree, after the CanUnlockTechTreeNode hook, when determining whether they have the prerequisite blueprints unlocked
// Returning true or false overrides default behavior
// Useful for customizing prerequisites without conflicting with the CanPlayerUnlock hook
object CanUnlockTechTreeNodePath(BasePlayer player, TechTreeData.NodeInstance node, TechTreeData techTree)
{
    Puts("CanUnlockTechTreeNodePath works!");
    return null;
}

// OnTechTreeNodeUnlock
// Called when a player is attempting to unlock a blueprint in a tech tree, after the CanUnlockTechTreeNode and CanUnlockTechTreeNodePath hooks, before the player is charged scrap
// Returning a non-null value overrides default behavior
// Useful for replacing the default behavior to charge the player a different currency, or to unlock multiple blueprints at once (such as those leading up to the one selected)
object OnTechTreeNodeUnlock(Workbench workbench, TechTreeData.NodeInstance node, BasePlayer player)
{
    Puts("OnTechTreeNodeUnlock works!");
    return null;
}

// OnTechTreeNodeUnlocked
// Called after a player has unlocked a blueprint in a tech tree
// No return behavior
// Useful for automatically unlocking blueprints for team members
void OnTechTreeNodeUnlocked(Workbench workbench, TechTreeData.NodeInstance node, BasePlayer player)
{
    Puts("OnTechTreeNodeUnlocked works!");
}

// Phone Hooks

// OnPhoneDial
// Called when a player places a phone call
// Returning a non-null value overrides default behavior
object OnPhoneDial(PhoneController callerPhone, PhoneController receiverPhone, BasePlayer player)
{
    Puts("OnPhoneDial works!");
    return null;
}

// CanReceiveCall
// Called when a player tries to place a phone call
// Returning true or false overrides default behavior
object CanReceiveCall(PhoneController receiverPhone)
{
    Puts("CanReceiveCall works!");
    return null;
}

// OnPhoneAnswer
// Called when a player tries to answer a phone call
// Returning a non-null value overrides default behavior
object OnPhoneAnswer(PhoneController receiverPhone, PhoneController callerPhone)
{
    Puts("OnPhoneAnswer works!");
    return null;
}

// OnPhoneAnswered
// Called right after a player has answered a phone call
// No return behavior
void OnPhoneAnswered(PhoneController receiverPhone, PhoneController callerPhone)
{
    Puts("OnPhoneAnswered works!");
}

// OnPhoneCallStart
// Called after a phone has been answered, right before voice communication is established
// Returning a non-null value overrides default behavior
object OnPhoneCallStart(PhoneController phone, PhoneController otherPhone, BasePlayer player)
{
    Puts("OnPhoneCallStart works!");
    return null;
}

// OnPhoneCallStarted
// Called right after a phone call has been answered and voice communication has been established
void OnPhoneCallStarted(PhoneController phone, PhoneController otherPhone, BasePlayer player)
{
    Puts("OnPhoneCallStarted works!");
}

// OnPhoneDialFail
// Called when a phone call is about to end or fail to start
// Returning a non-null value overrides default behavior
object OnPhoneDialFail(PhoneController phone, Telephone.DialFailReason reason, BasePlayer player)
{
    Puts("OnPhoneDialFail works!");
    return null;
}

// OnPhoneDialFailed
// Called after a phone call has ended or failed to start
// No return behavior
void OnPhoneDialFailed(PhoneController phone, Telephone.DialFailReason reason, BasePlayer player)
{
    Puts("OnPhoneDialFailed works!");
}

// OnPhoneDialTimeout
// Called when a phone is about to automatically hang up because the receiver phone wasn't answered in time
// Returning a non-null value overrides default behavior
object OnPhoneDialTimeout(PhoneController callerPhone, PhoneController receiverPhone, BasePlayer player)
{
    Puts("OnPhoneDialTimeout works!");
    return null;
}

// OnPhoneDialTimedOut
// Called right after a phone was automatically hung up because the receiver phone wasn't answered in time
// No return behavior
void OnPhoneDialTimedOut(PhoneController callerPhone, PhoneController receiverPhone, BasePlayer player)
{
    Puts("OnPhoneDialTimedOut works!");
}

// OnPhoneNameUpdate
// Called when a player tries to update a phone name
// Returning a non-null value overrides default behavior
object OnPhoneNameUpdate(PhoneController phoneController, string name, BasePlayer player)
{
    Puts("OnPhoneNameUpdate works!");
    return null;
}

// OnPhoneNameUpdated
// Called after a player has updated a phone name
// No return behavior
void OnPhoneNameUpdated(PhoneController phoneController, string name, BasePlayer player)
{
    Puts("OnPhoneNameUpdated works!");
}

// Plugin Hooks

// Loaded
// Called when a plugin has finished loading
// Other plugins may or may not be present, dependant on load order
// No return behavior
void Loaded()
{
    Puts("Loaded works!");
}

// Unload
// Called when a plugin is being unloaded
// No return behavior
void Unload()
{
    Puts("Save data, nullify static variables, etc.");
}

// LoadDefaultConfig
// Called when the config for a plugin should be initialized
// Only called if the config file does not already exist
// No return behavior
protected override void LoadDefaultConfig()
{
    Puts("Default configuration created");
}

// LoadDefaultMessages
// Called when the localization for a plugin should be registered
// No return behavior
protected override void LoadDefaultMessages()
{
    lang.RegisterMessages(new Dictionary<string, string>
    {
        ["Example"] = "This is an example message!",
        ["AnotherExample"] = "Here is another example"
    }, this);
}

// Permission Hooks

// OnPermissionRegistered
// Called when a permission has been registered
// No return behavior
void OnPermissionRegistered(string name, Plugin owner)
{
    Puts($"Permission '{name}' has been registered {(owner != null ? $"for {owner.Title}" : "")}");
}

// OnGroupPermissionGranted
// Called when a permission has been granted to a group
// No return behavior
void OnGroupPermissionGranted(string name, string perm)
{
    Puts($"Group '{name}' granted permission: {perm}");
}

// OnGroupPermissionRevoked
// Called when a permission has been revoked from a group
// No return behavior
void OnGroupPermissionRevoked(string name, string perm)
{
    Puts($"Group '{name}' revoked permission: {perm}");
}

// OnGroupCreated
// Called when a group has been created successfully
// No return behavior
void OnGroupCreated(string name)
{
    Puts($"Group '{name}' has been created!");
}

// OnGroupDeleted
// Called when a group has been deleted successfully
// No return behavior
void OnGroupDeleted(string name)
{
    Puts($"Group '{name}' has been deleted!");
}

// OnGroupTitleSet
// Called when a group title has been updated
// No return behavior
void OnGroupTitleSet(string name, string title)
{
    Puts($"Title '{title}' set on group '{name}'");
}

// OnGroupRankSet
// Called when a group rank has been updated
// No return behavior
void OnGroupRankSet(string name, int rank)
{
    Puts($"Rank '{rank}' set on group '{name}'");
}

// OnGroupParentSet
// Called when a group parent has been updated
// No return behavior
void OnGroupParentSet(string name, string parent)
{
    Puts($"Parent '{parent}' set on group '{name}'");
}