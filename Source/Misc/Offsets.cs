namespace Offsets
{
    public struct UnityList
    {
        public const uint Base = 0x10; // to UnityListBase
        public const uint Count = 0x18; // int32
    }
    public struct UnityListBase
    {
        public const uint Start = 0x20; // start of list +(i * 0x8)
    }
    public struct UnityString
    {
        public const uint Length = 0x10; // int32
        public const uint Value = 0x14; // string,unicode
    }
    public struct ModuleBase
    {
        public const uint GameObjectManager = 0x17FFD28; // to eft_dma_radar.GameObjectManager
    }
    public struct GameObject
    {
        public static readonly uint[] To_TransformInternal = new uint[] { 0x10, 0x30, 0x30, 0x8, 0x28, 0x10 }; // to TransformInternal
        public const uint ObjectClass = 0x30;
        public const uint ObjectName = 0x60; // string,default (null terminated)
    }
    public struct GameWorld
    {
        public static readonly uint[] To_LocalGameWorld = new uint[] { GameObject.ObjectClass, 0x18, 0x28 };
    }
    public struct LocalGameWorld // -.ClientLocalGameWorld : ClientGameWorld
    {
        public const uint ExfilController = 0x18; // to ExfilController
        public const uint LootList = 0x70; // to UnityList
        public const uint RegisteredPlayers = 0x90; // to RegisteredPlayers
        public const uint Grenades = 0x108; // to Grenades
    }
    public struct ExfilController // -.GClass0B67
    {
        public const uint ExfilCount = 0x18; // int32
        public const uint ExfilList = 0x20; // to UnityListBase
    }
    public struct Exfil
    {
        public const uint Status = 0xA8; // int32
    }
    public struct Grenades // -.GClass05FD<Int32, Throwable>
    {
        public const uint List = 0x18; // to UnityList
    }
    public struct Player // EFT.Player : MonoBehaviour, GInterface3EB2, GInterface457E, GInterface4579, GInterface4583, GInterface45BA, GInterface8D68, IDissonancePlayer
    {
        public static readonly uint[] To_TransformInternal = new uint[] { 
            0xA8, // EFT.PlayerBody
            0x28, // SkeletonRootJoint : Diz.Skinning.Skeleton
            0x28, // System.Collections.Generic.List<Transform>
            Offsets.UnityList.Base, 
            Offsets.UnityListBase.Start + (0 * 0x8), 
            0x10 }; // to TransformInternal
        public const uint MovementContext = 0x40; // to MovementContext
        public const uint Corpse = 0x338; // EFT.Interactive.Corpse
        public const uint Profile = 0x4F0; // to Profile
        public const uint HealthController = 0x528; // to HealthController
        public const uint InventoryController = 0x538; // to InventoryController
        public const uint IsLocalPlayer = 0x807; // bool
    }
    public struct Profile // EFT.Profile
    {
        public const uint Id = 0x10; // unity string
        public const uint AccountId = 0x18; // unity string
        public const uint PlayerInfo = 0x28; // to PlayerInfo
        public const uint Stats = 0xE8; // to Stats
    }
    public struct Stats // -.GClass05E4
    {
        public const uint OverallCounters = 0x18; // to OverallCounters
    }
    public struct OverallCounters // GClass1872
    {
        public const uint Counters = 0x10; // to Dictionary<IntPtr, ulong>
    }
    public struct PlayerInfo // -.GClass1118
    {
        public const uint Nickname = 0x10; // unity string
        public const uint MainProfileNickname = 0x18; // unity string
        public const uint GroupId = 0x20; // ptr to UnityString (0/null if solo or bot)
        public const uint Settings = 0x48; // to PlayerSettings 
        public const uint PlayerSide = 0x68; // int32
        public const uint RegDate = 0x6C; // int32
        public const uint MemberCategory = 0x84; // int32 enum
        public const uint Experience = 0x88; // int32
    }
    public struct PlayerSettings // GClass10F9
    {
        public const uint Role = 0x10; // int32 enum
    }
    public struct MovementContext
    {
        public const uint Rotation = 0x22C; // vector2
    }
    public struct InventoryController // -.GClass1A98
    {
        public const uint Inventory = 0x128; // to Inventory
    }
    public struct Inventory // GClass1BBE
    {
        public const uint Equipment = 0x10; // to Equipment
    }
    public struct Equipment // GClass1BF5
    {
        public const uint Slots = 0x78; // to UnityList
    }
    public struct Slot
    {
        public const uint Name = 0x10; // string,unity
        public const uint ContainedItem = 0x38; // to LootItemBase
    }
    public struct HealthController // -.GInterface7AEE
    {
        public static readonly uint[] To_HealthEntries = { 0x58, 0x18 }; // to HealthEntries // if its wrong try { 0x50, 0x18 }
    }
    public struct HealthEntries
    {
        public const uint HealthEntries_Start = 0x30; // Each body part +0x18 , to HealthEntry
    }
    public struct HealthEntry
    {
        public const uint Value = 0x10; // to HealthValue
    }
    public struct HealthValue
    {
        public const uint Current = 0x0; // float
        public const uint Maximum = 0x4; // float
        public const uint Minimum = 0x8; // float
    }
    public struct LootListItem
    {
        public const uint LootUnknownPtr = 0x10; // to LootUnknownPtr
    }

    public struct LootUnknownPtr
    {
        public const uint LootInteractiveClass = 0x28; // to LootInteractiveClass
    }
    public struct LootInteractiveClass
    {
        public const uint LootBaseObject = 0x10; // to LootBaseObject
        public const uint LootItemBase = 0x50; // to LootItemBase
        public const uint ContainerItemOwner = 0x108; // to ContainerItemOwner
    }

    /*
 [Class] -.GClass1AE8 : GClass1A55, IApplicable
    [00][S] list_0x00 : System.Collections.Generic.List<Item>
    [00][S] <ItemNoHashComparer>k__BackingField : System.Collections.Generic.IEqualityComparer<Item>
    [08][S] Replacements : System.Collections.Generic.Dictionary<Enum, String>
    [10] Id : String
    [18] OriginalAddress : EFT.InventoryLogic.ItemAddress
    [20] Components : System.Collections.Generic.List<IItemComponent>
    [28] _toStringCache : String
    [30] CurrentAddress : EFT.InventoryLogic.ItemAddress
    [38] ChildrenChanged : -.GClass25A1<Item>
    [40] <Template>k__BackingField : EFT.InventoryLogic.ItemTemplate
    [48] Attributes : System.Collections.Generic.List<GClass1C05>
    [50] <DiscardLimit>k__BackingField : System.Nullable<Int32>
    [58] UnlimitedCount : Boolean
    [5C] BuyRestrictionMax : Int32
    [60] BuyRestrictionCurrent : Int32
    [64] StackObjectsCount : Int32
    [68] Version : Int32
    [6C] SpawnedInSession : Boolean
    [70] Grids : -.GClass1A5B[]
    [78] Slots : EFT.InventoryLogic.Slot[]
     
     */
    public struct LootItemBase // GClass1AE8
    {
        public const uint ItemTemplate = 0x40; // to ItemTemplate
        public const uint Grids = 0x70; // to Grids
        public const uint Slots = 0x78; // to UnityList
        public const uint Cartridges = 0x90; // via -.GClass1B22 : GClass1AFB, IAmmoContainer , to StackSlot
    }
    public struct StackSlot // EFT.InventoryLogic.StackSlot : Object, IContainer
    {
        public const uint Items = 0x10; // to UnityList , of LootItemBase
    }
    public struct ItemTemplate //EFT.InventoryLogic.ItemTemplate
    {
        public const uint BsgId = 0x50; // string,unity     [50] _id : String
        public const uint IsQuestItem = 0x9C; // bool       [9C] QuestItem : Boolean
    }
    public struct LootBaseObject
    {
        public const uint GameObject = 0x30; // to GameObject
    }
    public struct LootGameObjectClass
    {
        public static readonly uint[] To_TransformInternal = new uint[] { 0x8, 0x28, 0x10 };
    }
    public struct ContainerItemOwner
    {
        public const uint LootItemBase = 0xB8; // to LootItemBase
    }
    public struct Grids
    {
        public const uint GridsEnumerableClass = 0x40;
    }
    public struct TransformInternal
    {
        public const uint Hierarchy = 0x38; // to TransformHierarchy
        public const uint HierarchyIndex = 0x40; // int32
    }
    public struct TransformHierarchy
    {
        public const uint Vertices = 0x18; // List<Vector128<float>>
        public const uint Indices = 0x20; // List<int>
    }
}