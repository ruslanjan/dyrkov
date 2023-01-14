using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace eft_dma_radar
{
    public class LootManager
    {
        private static readonly string[] _containers = new string[] { 
            "body", 
            "XXXcap", 
            "cap",
            "Ammo_crate_Cap", 
            "Grenade_box_Door", 
            "Medical_Door", 
            "Toolbox_Door", 
            "card_file_box", 
            "cover_",
            "boor_safe",
            "scontainer_",
            "Toolbox_Door",
            "lootable", 
            "scontainer_Blue_Barrel_Base_Cap", 
            "scontainer_wood_CAP", 
            "suitcase_plastic_lootable_open", 
            "weapon_box_cover", 
            "container_crete_04_COLLIDER(1)" };
        private readonly Config _config;
        /// <summary>
        /// Filtered loot ready for display by GUI.
        /// </summary>
        public ReadOnlyCollection<LootItem> Filter { get; private set; }
        /// <summary>
        /// All tracked loot/corpses in Local Game World.
        /// </summary>
        private ReadOnlyCollection<LootItem> Loot { get; }

        private string GetClassname(ulong obj)
        {
            return Memory.ReadString(Memory.ReadPtrChain(obj, new uint[] { 0x0, 0x0, 0x48 }), 64);
        }

        #region Constructor
        public LootManager(ulong localGameWorld)
        {
            _config = Program.Config;
            var lootlistPtr = Memory.ReadPtr(localGameWorld + Offsets.LocalGameWorld.LootList);
            var lootListEntity = Memory.ReadPtr(lootlistPtr + Offsets.UnityList.Base);
            var countLootListObjects = Memory.ReadValue<int>(lootListEntity + Offsets.UnityList.Count);
            if (countLootListObjects < 0 || countLootListObjects > 4096) throw new ArgumentOutOfRangeException("countLootListObjects"); // Loot list sanity check
            var loot = new List<LootItem>(countLootListObjects);

            var map = new ScatterReadMap();
            var round1 = map.AddRound();
            var round2 = map.AddRound();
            var round3 = map.AddRound();
            var round4 = map.AddRound();
            var round5 = map.AddRound();
            var round6 = map.AddRound();
            var round7 = map.AddRound();
            Program.Log($"found {countLootListObjects} potential loot objects\nParsing loot...");
            for (int i = 0; i < countLootListObjects; i++)
            {
                var lootObjectsEntity = round1.AddEntry(i, 0, lootListEntity + Offsets.UnityListBase.Start + (ulong)(0x8 * i), typeof(ulong));
                var unknownPtr = round2.AddEntry(i, 1, lootObjectsEntity, typeof(ulong), null, Offsets.LootListItem.LootUnknownPtr);
                var interactiveClass = round3.AddEntry(i, 2, unknownPtr, typeof(ulong), null, Offsets.LootUnknownPtr.LootInteractiveClass);
                var baseObject = round4.AddEntry(i, 3, interactiveClass, typeof(ulong), null, Offsets.LootInteractiveClass.LootBaseObject);
                var className0 = round4.AddEntry(i, 7, interactiveClass, typeof(ulong), null, Offsets.Kernel.ClassName[0]);
                var className1 = round5.AddEntry(i, 8, className0, typeof(ulong), null, Offsets.Kernel.ClassName[1]);
                var gameObject = round5.AddEntry(i, 4, baseObject, typeof(ulong), null, Offsets.LootBaseObject.GameObject);
                var className = round6.AddEntry(i, 9, className1, typeof(ulong), null, Offsets.Kernel.ClassName[2]);
                var pGameObjectName = round6.AddEntry(i, 5, gameObject, typeof(ulong), null, Offsets.GameObject.ObjectName);
                var name = round7.AddEntry(i, 6, pGameObjectName, typeof(string), 64);
            }
            map.Execute(countLootListObjects); // execute scatter read
            var lootNames = new List<string>();
            var lootClassNames = new List<string>();
            var possibleContainer = new List<string>();
            for (int i = 0; i < countLootListObjects; i++)
            {
                String name = "", classNameStr = "";
                ulong interactiveClass = 0;
                var added = false;
                try
                {
                    if (map.Results[i][2].Result is null ||
                        map.Results[i][4].Result is null ||
                        map.Results[i][6].Result is null) continue;
                    var lootObjectsEntity = (ulong)map.Results[i][0].Result;
                    interactiveClass = (ulong)map.Results[i][2].Result;
                    var gameObject = (ulong)map.Results[i][4].Result;
                    name = (string)map.Results[i][6].Result;
                    classNameStr = Memory.ReadString((ulong)map.Results[i][9].Result, 64);
                    if (classNameStr.Contains("", StringComparison.OrdinalIgnoreCase))
                    {
                        //return fields;
                    }
                    
                    // skip usless stuff
                    if (name.Contains("POS_Money", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    //Program.Log($"Loot:{name}");
                    if (name.Contains("script", StringComparison.OrdinalIgnoreCase))
                    {
                        //skip these. These are scripts which I think are things like landmines but not sure
                    }
                    else if (name.Contains("lootcorpse_playersuperior", StringComparison.OrdinalIgnoreCase) || classNameStr.Contains("Corpse"))
                    {
                        var objectClass = Memory.ReadPtr(gameObject + Offsets.GameObject.ObjectClass);
                        var transformInternal = Memory.ReadPtrChain(objectClass, Offsets.LootGameObjectClass.To_TransformInternal);
                        var pos = new Transform(transformInternal).GetPosition();
                        loot.Add(new LootItem
                        {
                            Position = pos,
                            AlwaysShow = true,
                            Label = "Corpse"
                        });
                        added = true;
                    }
                    else
                    {
                        //Get Position
                        var objectClass = Memory.ReadPtr(gameObject + Offsets.GameObject.ObjectClass);
                        var transformInternal = Memory.ReadPtrChain(objectClass, Offsets.LootGameObjectClass.To_TransformInternal);
                        var pos = new Transform(transformInternal).GetPosition();

                        //the WORST method to figure out if an item is a container...but no better solution now
                        bool container = classNameStr.Contains("LootableContainer");
                        if (classNameStr.Contains("ObservedLootItem"))
                        {
                            container = false;
                        }
                        try
                        {
                            /*var _itemOwner = Memory.ReadPtr(interactiveClass + Offsets.LootInteractiveClass.ContainerItemOwner);
                            if (_itemOwner != 0)
                            {
                                container = true;
                            }*/
                        }
                        catch { }


                        //If the item is a Static Container like weapon boxes, barrels, caches, safes, airdrops etc
                        if (container)
                        {
                            //Grid Logic for static containers so that we can see what's inside
                            try
                            {
                                if (name.Contains("container_crete_04_COLLIDER(1)", StringComparison.OrdinalIgnoreCase))
                                {
                                    loot.Add(new LootItem
                                    {
                                        Position = pos,
                                        Label = "!!Airdrop",
                                        Important = true,
                                        AlwaysShow = true
                                    });

                                    added = true;
                                    continue;
                                }
                                // EFT.Interactive.LootableContainer 
                                var itemOwner = Memory.ReadPtr(interactiveClass + Offsets.LootInteractiveClass.ContainerItemOwner);
                                //var itemOwnerClassName = Memory.ReadString(Memory.ReadPtrChain(itemOwner, Offsets.Kernel.ClassName), 64);
                                var itemBase = Memory.ReadPtr(itemOwner + Offsets.ContainerItemOwner.LootItemBase);
                                //var itemClassName = Memory.ReadString(Memory.ReadPtrChain(itemBase, Offsets.Kernel.ClassName), 64);
                                var grids = Memory.ReadPtr(itemBase + Offsets.LootItemBase.Grids);
                                if (grids == 0)
                                    grids = Memory.ReadPtr(itemBase + 0x110);
                                //Program.Log($"loading container {name}:{classNameStr}:{itemClassName}:{itemOwnerClassName}");
                                GetItemsInGrid(grids, "ignore", pos, loot);
                                added = true;
                            }
                            catch
                            {
                                container = false;
                            }
                        }
                        //If the item is NOT a Static Container
                        if (!container)
                        {
                            //Program.Log(GetClassname(interactiveClass));
                            var item = Memory.ReadPtr(interactiveClass + Offsets.LootInteractiveClass.LootItemBase); //EFT.InventoryLogic.Item
                            var itemTemplate = Memory.ReadPtr(item + Offsets.LootItemBase.ItemTemplate); //EFT.InventoryLogic.ItemTemplate
                            bool questItem = false;
                            try
                            {
                                questItem = Memory.ReadValue<bool>(itemTemplate + Offsets.ItemTemplate.IsQuestItem);
                            } catch { }

                            //If NOT a quest item. Quest items are like the quest related things you need to find like the pocket watch or Jaeger's Letter etc. We want to ignore these quest items.
                            if (!questItem)
                            {
                                var BSGIdPtr = Memory.ReadPtr(itemTemplate + Offsets.ItemTemplate.BsgId);
                                var id = Memory.ReadUnityString(BSGIdPtr);

                                //If the item is a corpose
                                if (id.Equals("55d7217a4bdc2d86028b456d")) // Corpse
                                {
                                    loot.Add(new LootItem
                                    {
                                        Position = pos,
                                        Label = "Corpse",
                                        AlwaysShow = true,
                                    });
                                    added = true;
                                }
                                //Finally we must have found a loose loot item, eg a keycard, backpack, gun, salewa. Anything not in a container or corpse.
                                else
                                {

                                    //Grid Logic for loose loot because some loose loot have items inside, eg a backpack or docs case. We want to check those items too. But not all loose loot have items inside, so we have a try-catch below
                                    try
                                    {
                                        var grids = Memory.ReadPtr(item + Offsets.LootItemBase.Grids);
                                        GetItemsInGrid(grids, id, pos, loot);
                                    }
                                    catch
                                    {
                                        //The loot item we found does not have any grids so it's basically like a keycard or a ledx etc. Therefore add it to our loot dictionary.
                                        if (DyrkovMarketManager.AllItems.TryGetValue(id, out var entry))
                                        {
                                            loot.Add(new LootItem
                                            {
                                                Label = entry.Label,
                                                AlwaysShow = entry.AlwaysShow,
                                                Important = entry.Important,
                                                Position = pos,
                                                Item = entry.Item
                                            });
                                            added = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
                if (!added) {
                    Program.Log($"Failed to load loot: {name}:{classNameStr} 0x{interactiveClass.ToString("X")}");
                }
            }
            Loot = new(loot); // update readonly ref
            File.WriteAllLinesAsync("LootNames.txt", lootNames);
            File.WriteAllLinesAsync("LootClassNames.txt", lootClassNames);
            File.WriteAllLinesAsync("PossibleLootNames.txt", possibleContainer);
            Program.Log("Loot parsing completed");
        }
        #endregion

        #region Methods
        /// <summary>
        /// Applies specified loot filter.s
        /// </summary>
        /// <param name="filter">Filter by item 'name'. Will use values instead if left null.</param>
        public void ApplyFilter(string filter)
        {
            var loot = this.Loot; // cache ref
            if (loot is not null)
            {
                var filteredLoot = new List<LootItem>(loot.Count);
                if (filter is null ||
                    filter.Trim() == string.Empty) // Use loot values
                {
                    foreach (var item in loot)
                    {
                        var value = Math.Max(item.Item.avg24hPrice, item.Item.traderPrice);
                        if (item.AlwaysShow || value >= _config.MinLootValue)
                        {
                            if (!filteredLoot.Contains(item))
                                filteredLoot.Add(item);
                        }
                    }
                }
                else // Use filtered name
                {
                    var alwaysShow = loot.Where(x => x.AlwaysShow); // Always show these
                    foreach (var item in alwaysShow)
                    {
                        if (!filteredLoot.Contains(item))
                            filteredLoot.Add(item);
                    }
                    var names = filter.Split(','); // Get multiple items searched
                    foreach (var name in names)
                    {
                        try
                        {
                            var search = loot.Where(x => x.Item.name.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase));
                            foreach (var item in search)
                            {
                                if (!filteredLoot.Contains(item))
                                    filteredLoot.Add(item);
                            }
                        }
                        catch { }
                    }
                }
                this.Filter = new(filteredLoot); // update ref
            }
        }
        ///This method recursively searches grids. Grids work as follows:
        ///Take a Groundcache which holds a Blackrock which holds a pistol.
        ///The Groundcache will have 1 grid array, this method searches for whats inside that grid.
        ///Then it finds a Blackrock. This method then invokes itself recursively for the Blackrock.
        ///The Blackrock has 11 grid arrays (not to be confused with slots!! - a grid array contains slots. Look at the blackrock and you'll see it has 20 slots but 11 grids).
        ///In one of those grid arrays is a pistol. This method would recursively search through each item it finds
        ///To Do: add slot logic, so we can recursively search through the pistols slots...maybe it has a high value scope or something.
        private void GetItemsInGrid(ulong gridsArrayPtr, string id, Vector3 pos, List<LootItem> loot)
        {
            var gridsArray = new MemArray(gridsArrayPtr);

            if (DyrkovMarketManager.AllItems.TryGetValue(id, out var entry))
            {
                loot.Add(new LootItem
                {
                    Label = entry.Label,
                    AlwaysShow = entry.AlwaysShow,
                    Important = entry.Important,
                    Position = pos,
                    Item = entry.Item
                });
            }

            // Check all sections of the container
            foreach (var grid in gridsArray.Data)
            {

                var gridEnumerableClass = Memory.ReadPtr(grid + Offsets.Grids.GridsEnumerableClass); // -.GClass178A->gClass1797_0x40 // Offset: 0x0040 (Type: -.GClass1797)

                var itemListPtr = Memory.ReadPtr(gridEnumerableClass + 0x18); // -.GClass1797->list_0x18 // Offset: 0x0018 (Type: System.Collections.Generic.List<Item>)
                var itemList = new MemList(itemListPtr);

                foreach (var childItem in itemList.Data)
                {
                    var gameObjectName = "";
                    var gameObjectClassName = "";
                    var childItemIdStr = "";
                    try
                    {
                        //var baseObject = Memory.ReadPtr(childItem + 0x0);
                        gameObjectClassName = Memory.ReadString(Memory.ReadPtrChain(childItem, Offsets.Kernel.ClassName), 64);
                        //gameObjectName = Memory.ReadString(Memory.ReadPtrChain(baseObject, new uint[] { Offsets.GameObject.ObjectClass, Offsets.GameObject.ObjectName }), 64);
                        var childItemTemplate = Memory.ReadPtr(childItem + Offsets.LootItemBase.ItemTemplate); // EFT.InventoryLogic.Item->_template // Offset: 0x0038 (Type: EFT.InventoryLogic.ItemTemplate)
                        var childItemIdPtr = Memory.ReadPtr(childItemTemplate + Offsets.ItemTemplate.BsgId);
                        childItemIdStr = Memory.ReadUnityString(childItemIdPtr).Replace("\\0", "");
                        
                        // Check to see if the child item has children
                        var childGridsArrayPtr = Memory.ReadPtr(childItem + Offsets.LootItemBase.Grids);   // -.GClassXXXX->Grids // Offset: 0x0068 (Type: -.GClass1497[])
                        GetItemsInGrid(childGridsArrayPtr, childItemIdStr, pos, loot);        // Recursively add children to the entity
                    }
                    catch (Exception ee) {
                        if (gameObjectClassName.Contains("String"))
                        {
                            var uknownId = Memory.ReadString(childItem, 64);
                            if (DyrkovMarketManager.AllItems.TryGetValue(uknownId, out var uknownEntry))
                            {
                                loot.Add(new LootItem
                                {
                                    Label = uknownEntry.Label,
                                    AlwaysShow = uknownEntry.AlwaysShow,
                                    Important = uknownEntry.Important,
                                    Position = pos,
                                    Item = uknownEntry.Item
                                });
                                continue;
                            }
                        }
                        if (DyrkovMarketManager.AllItems.TryGetValue(childItemIdStr, out var childItemEntry))
                        {
                            loot.Add(new LootItem
                            {
                                Label = childItemEntry.Label,
                                AlwaysShow = childItemEntry.AlwaysShow,
                                Important = childItemEntry.Important,
                                Position = pos,
                                Item = childItemEntry.Item
                            });
                            continue;
                        }
                        Program.Log($"Failed to load loot from container: {childItemIdStr}#{gameObjectName}:{gameObjectClassName} 0x{childItem.ToString("X")}");
                    }
                }

            }
        }
        #endregion
    }

    #region Classes
    //Helper class or struct
    public class MemArray
    {
        public ulong Address { get; }
        public int Count { get; }
        public ulong[] Data { get; }

        public MemArray(ulong address)
        {
            var type = typeof(ulong);

            Address = address;
            Count = Memory.ReadValue<int>(address + Offsets.UnityList.Count);
            var arrayBase = address + Offsets.UnityListBase.Start;
            var tSize = (uint)Marshal.SizeOf(type);

            // Rudimentary sanity check
            if (Count > 4096 || Count < 0)
                Count = 0;

            var retArray = new ulong[Count];
            var buf = Memory.ReadBuffer(arrayBase, Count * (int)tSize);

            for (uint i = 0; i < Count; i++)
            {
                var index = i * tSize;
                var t = MemoryMarshal.Read<ulong>(buf.Slice((int)index, (int)tSize));
                if (t == 0x0) throw new NullPtrException();
                retArray[i] = t;
            }

            Data = retArray;
        }
    }


    //Helper class or struct
    public class MemList
    {
        public ulong Address { get; }

        public int Count { get; }

        public List<ulong> Data { get; }

        public MemList(ulong address)
        {
            var type = typeof(ulong);

            Address = address;
            Count = Memory.ReadValue<int>(address + Offsets.UnityList.Count);

            if (Count > 4096 || Count < 0)
                Count = 0;

            var arrayBase = Memory.ReadPtr(address + Offsets.UnityList.Base) + Offsets.UnityListBase.Start;
            var tSize = (uint)Marshal.SizeOf(type);
            var retList = new List<ulong>(Count);
            var buf = Memory.ReadBuffer(arrayBase, Count * (int)tSize);

            for (uint i = 0; i < Count; i++)
            {
                var index = i * tSize;
                var t = MemoryMarshal.Read<ulong>(buf.Slice((int)index, (int)tSize));
                if (t == 0x0) throw new NullPtrException();
                retList.Add(t);
            }

            Data = retList;
        }
    }
    public class LootItem
    {
        public string Label { get; init; }
        public bool Important { get; init; } = false;
        public Vector3 Position { get; init; }
        public TarkovDev.Item Item { get; init; } = new();
        public bool AlwaysShow { get; init; } = false;


        public bool isImportant(int MinImportantLootValue, int MinImportantLootValuePerSlot)
        {
            return Important || DyrkovMarketManager.GetItemValuePerSlot(Item) >= MinImportantLootValuePerSlot || DyrkovMarketManager.GetItemValue(Item) >= MinImportantLootValue;
        }

    }
    #endregion
}