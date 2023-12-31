using System;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

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

    public struct Kernel
    {
        public static readonly uint[] ClassName = new uint[] { 0x0, 0x0, 0x48 };
    }

    public struct ModuleBase
    {
        public const uint GameObjectManager = 0x17FFD28; // to eft_dma_radar.GameObjectManager
        public const uint AllCameras = 0x179F500;
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

    public struct MainApp
    {
        // MainApplication(Parent ClientApplication) -> _backEnd -> \uE00B (class214_0) -> BackEndConfig -> Config -> Inertia.
        public static readonly uint[] ToConfig = new uint[] {
            0x28, 0x48, 0xA8, 0x10
            /* 0x28, // _backEnd // class213
            0x48, // class214_0 0x58
            0x138,//0x110, // BackendConfigClass \uE510
            0x10 // Config GClass1173 \uE553 */
        };
    }

    public struct Config
    {
        public const uint Inertia = 0xD0; // \uE02B
    }

    public struct Inertia
    {
        public const uint BaseJumpPenaltyDuration = 0x4C; // float
        public const uint DurationPower = 0x50; // float
        public const uint BaseJumpPenalty = 0x54; // float
        public const uint PenaltyPower = 0x58; // float
        public const uint MoveTimeRange = 0xF4; // Vector2
        public const uint FallThreshold = 0x20; // float
        public const uint MinDirectionBlendTime = 0xf0; // float
    }
    public struct LocalGameWorld // -.ClientLocalGameWorld : ClientGameWorld
    {
        public const uint ExfilController = 0x18; // to ExfilController
        public const uint LootList = 0xc8; // to UnityList
        public const uint RegisteredPlayers = 0xF0; // to RegisteredPlayers
        public const uint AllAlivePlayers = 0x108; // to AllAlivePlayers
        public const uint Grenades = 0x1A0; // to Grenades
    }

    public struct ThermalVision
    {
        public const uint On = 0xe0;
        public const uint IsNoisy = 0xe1;
        public const uint IsFpsStuck = 0xe2;
        public const uint IsMotionBlurred= 0xe3;
        public const uint IsGlitch = 0xe4;
        public const uint IsPixelated = 0xe5;
        public const uint material = 0x90;
    }

    public struct NightVision
    {
        public const uint On = 0xE4;
        public const uint IsNoisy = 0xe1;
        public const uint IsFpsStuck = 0xe2;
        public const uint IsMotionBlurred = 0xe3;
        public const uint IsGlitch = 0xe4;
        public const uint IsPixelated = 0xe5;
        public const uint material = 0x90;
    }
    public struct ExfilController // -.GClass0B67
    {
        public const uint ExfilList = 0x20; // to UnityListBase
        public const uint ScavExfilList = 0x28; // to UnityListBase
    }
    public struct Exfil
    {
        public const uint Status = 0xA8; // int32
        public const uint ExfilTriggerSettings = 0x58; 
    }

    public struct ExfilTriggerSettings
    {
        public const uint Name = 0x10; // String
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
        public static readonly uint[] bone_matrix = new uint[]
        {
            0xA8, 0x28, 0x28, Offsets.UnityList.Base
        };
        public const uint PlayerBody = 0xA8;

        public const uint MovementContext = 0x40; // to MovementContext
        public const uint Corpse = 0x390; // EFT.Interactive.Corpse
        public const uint Profile = 0x588; // to Profile
        public const uint HealthController = 0x5c8; // to HealthController
        public const uint InventoryController = 0x568; // to InventoryController
        public const uint Physical = 0x598;
        public const uint ProceduralWeaponAnimation = 0x1A0;
        public const uint IsLocalPlayer = 0x906; // bool
    }
    public struct ObservedPlayerView {
        public static readonly uint[] To_TransformInternal = new uint[] {
            0x60, // EFT.PlayerBody
            0x28, // SkeletonRootJoint : Diz.Skinning.Skeleton
            0x28, // System.Collections.Generic.List<Transform>
            Offsets.UnityList.Base,
            Offsets.UnityListBase.Start + (0 * 0x8),
            0x10 }; // to TransformInternal
        public static readonly uint[] bone_matrix = new uint[]
        {
            0x60, 0x28, 0x28, Offsets.UnityList.Base
        };

        public static readonly uint[] Player = new uint[]
        {
            0x30, 0x10
        };
        public static readonly uint Id = 0x40;
        public static readonly uint Nickname = 0x48;
    }
    public struct PlayerBody
    {
        public const uint SlotViews = 0x50;
    }

    public struct SlotViews
    {
        public const uint Dresses = 0x40;
    }
    public struct Dress
    {
        public const uint Renderers = 0x28;
    }

    public struct Renderer
    {
        public const uint Materials = 0x10;
    }

    public struct ProceduralWeaponAnimation
    {
        public const uint Breath = 0x28;
        public const uint Shooting = 0x48;
        public const uint Mask = 0x138;
        public const uint FirearmController = 0xA8;
    }
    public struct Breath
    {
        public const uint IsAiming = 0xA0;
        public const uint Intensity = 0xA4  ;
    }
    public struct Physical
    {
        public const uint MaxStamina = 0x38;
        public const uint MaxHandStamina = 0x40;
        public const uint MaxOxygen = 0x48;
        public const uint buff = 0x50;
    }

    public struct Profile // EFT.Profile
    {
        public const uint Id = 0x10; // unity string
        public const uint AccountId = 0x18; // unity string
        public const uint PlayerInfo = 0x28; // to PlayerInfo
        public const uint Stats = 0xF0; // to Stats
        public const uint Skills = 0x60;
    }
    public struct Stats // -.GClass05E4
    {
        public const uint OverallCounters = 0x18; // to OverallCounters
    }
    public struct OverallCounters // GClass1872
    {
        public const uint Counters = 0x10; // to Dictionary<IntPtr, ulong>
    }

    public struct Skills // EFT.Profile
    {
        public const uint AttentionLootSpeed = 0x160; 
        public const uint AttentionExamine = 0x168;
        public const uint MagDrillsUnLoadSpeed = 0x188; 
        public const uint MagDrillsLoadSpeed = 0x180;
        public const uint PerceptionLootDot = 0x120;
        public const uint StrengthBuffJumpHeightInc = 0x60;
        public const uint PerceptionHearing = 0x118;
        public const uint MagDrillsInventoryCheckSpeed = 0x190;
        public const uint MagDrillsInventoryCheckAccuracy = 0x198;
        public const uint AimMasterSpeed = 0x2D0;
        public const uint AimMasterWiggle = 0x2D8;
        public const uint AimMasterElite = 0x2E0;
        public const uint RecoilControlImprove = 0x2E8;
        public const uint TroubleFixing = 0x2F8;
        public const uint ThrowingStrengthBuff = 0x320;
        public const uint ThrowingEnergyExpenses = 0x328;
        public const uint DrawSpeed = 0x338;
        //public const uint ProneMovementSpeed = 0x468;
        //public const uint SearchBuffSpeed = 0x480;
        //public const uint SurgerySpeed = 0x498;

        // Weapons
        public const uint Revolver = 0x250;

        // bools
        public const uint SearchDouble = 0x4C0;
        public const uint MagDrillsInstantCheck = 0x1A0;
        public const uint MagDrillsLoadProgression = 0x1A8;
        public const uint StressBerserk = 0xF0;
        public const uint IntellectEliteAmmoCounter = 0x148;

        public const uint Value = 0x30; // float or boolean
    }

    public struct WeaponSkill
    {
        public const uint Value = 0x2C;
    }
    public struct PlayerInfo // -.GClass1118
    {
        public const uint Nickname = 0x10; // unity string
        public const uint MainProfileNickname = 0x18; // unity string
        public const uint GroupId = 0x20; // ptr to UnityString (0/null if solo or bot)
        public const uint Settings = 0x50; // to PlayerSettings 
        public const uint PlayerSide = 0x70; // int32
        public const uint RegDate = 0x74; // int32
        public const uint MemberCategory = 0x8C; // int32 enum
        public const uint Experience = 0x90; // int32
    }
    public struct PlayerSettings // GClass10F9
    {
        public const uint Role = 0x10; // int32 enum
    }
    public struct MovementContext
    {
        public const uint Rotation = 0x234;// 0x22C; // vector2
    }
    public struct InventoryController // -.GClass1A98
    {
        public const uint Inventory = 0x130; // to Inventory
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
        public static readonly uint[] To_HealthEntries = { 0x68, 0x18 }; // to HealthEntries // if its wrong try { 0x50, 0x18 }
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
        public const uint LootItemBase = 0x0b0;//0x50; // to LootItemBase
        public const uint ContainerItemOwner = 0x110; // to LootableContainer.ItemOwner
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
    public struct LootItemBase // GClass1AE8 // EFT.InventoryLogic.Item
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
        public const uint NotShownInSlot = 0xD8;
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
        public const uint LootItemBase = 0xC0; // to LootItemBase
    }
    public struct Grids
    {
        public const uint GridsEnumerableClass = 0x40;
    }
    public struct TransformInternal
    {
        public const uint Hierarchy  = 0x38; // to TransformHierarchy
        public const uint HierarchyIndex = 0x40; // int32
    }
    public struct TransformHierarchy
    {
        public const uint Vertices = 0x18; // List<Vector128<float>>
        public const uint Indices = 0x20; // List<int>
    }
}