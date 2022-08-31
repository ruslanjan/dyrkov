using System;
using System.Runtime.InteropServices;

namespace eft_dma_radar
{
    // ref https://www.unknowncheats.me/forum/2882667-post2741.html
    public class KDManager
    {
        private static uint? KillHash;
        private static uint? DeathHash;

        /// <summary>
        /// As obtained via TestGetIndexes() reversing
        /// </summary>
        private const uint KillIndex = 0;
        /// <summary>
        /// As obtained via TestGetIndexes() reversing
        /// </summary>
        private const uint DeathIndex = 0;

        #region Constructor
        /// <summary>
        /// Only construct via 'LocalPlayer'
        /// </summary>
        public KDManager(ulong localPlayerProfile)
        {
            if (KillHash is null || DeathHash is null)
            {
                var stats = Memory.ReadPtr(localPlayerProfile + Offsets.Profile.Stats);
                var overallCounters = Memory.ReadPtr(stats + Offsets.Stats.OverallCounters); // SessionCounters is at 0x10
                var counters = Memory.ReadPtr(overallCounters + Offsets.OverallCounters.Counters); // Dictionary<IntPtr, ulong>

                var arrayBase = Memory.ReadPtr(counters + 0x18) + 0x28;

                var killCounterKey = Memory.ReadValue<ulong>(arrayBase + KillIndex * 0x18);
                KillHash = Memory.ReadValue<uint>(killCounterKey + 0x18);

                var deathCounterKey = Memory.ReadValue<ulong>(arrayBase + DeathIndex * 0x18);
                DeathHash = Memory.ReadValue<uint>(deathCounterKey + 0x18);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Returns Kill/Death ratio.
        /// </summary>
        public float GetKD(ulong profile)
        {
            if (KillHash is not null && DeathHash is not null)
            {
                ulong kills = 0; ulong deaths = 0;
                var stats = Memory.ReadPtr(profile + Offsets.Profile.Stats);
                var overallCounters = Memory.ReadPtr(stats + Offsets.Stats.OverallCounters); // SessionCounters is at 0x10
                var counters = Memory.ReadPtr(overallCounters + Offsets.OverallCounters.Counters); // Dictionary<IntPtr, ulong>

                var countersDict = new MemDictionary<ulong, ulong>(counters); // Key param is GClass141C in this post
                foreach (var entry in countersDict.Data)
                {
                    var keyClass = entry.Key;
                    var value = entry.Value;

                    var hash = Memory.ReadValue<uint>(keyClass + 0x18);

                    if (hash == KillHash) // This hash should work with PMCs and Player Scavs
                        kills = value;
                    else if (hash == DeathHash) // This hash is PMC's only
                        deaths = value;
                }

                if (deaths != 0) return (float)kills / (float)deaths;
                else return kills;
            }
            else return -1f;
        }

        /// <summary>
        /// Resets static class fields to null, and also nulls out the provided class instance.
        /// </summary>
        /// <param name="objectToReset">Class 'instance' to be reset.</param>
        public static void Reset(ref KDManager objectToReset)
        {
            KillHash = null; // Set this static field to null
            DeathHash = null; // Set this static field to null
            objectToReset = null; // Set this object instance to null
        }

        /// <summary>
        /// **Testing/Debug Method**
        /// Takes provided "Kills" and "Deaths" for LocalPlayer, and attempts to find the proper indexes for
        /// their associated hashes.
        /// Obtain K/D from the Character -> Overall tab in game.
        /// </summary>
        public static void TestGetIndexes(ulong localPlayerProfile, ulong localPlayerKills, ulong localPlayerDeaths)
        {
            // Tested as a PMC only, solving Player Scavs will be a similar process. Have fun.
            var stats = Memory.ReadPtr(localPlayerProfile + Offsets.Profile.Stats);
            var overallCounters = Memory.ReadPtr(stats + Offsets.Stats.OverallCounters); // SessionCounters is at 0x10
            var counters = Memory.ReadPtr(overallCounters + Offsets.UnityList.Base); // Dictionary<IntPtr, ulong>
            var countersDict = new MemDictionary<ulong, ulong>(counters); // Key param is GClass141C in this post
            var arrayBase = Memory.ReadPtr(counters + 0x18) + 0x28;

            for (uint i = 0; i < countersDict.Count; i++)
            {
                try
                {
                    var killCounterKey = Memory.ReadValue<ulong>(arrayBase + i * 0x18);
                    var killHash = Memory.ReadValue<uint>(killCounterKey + 0x18);

                    var deathCounterKey = Memory.ReadValue<ulong>(arrayBase + i * 0x18);
                    var deathHash = Memory.ReadValue<uint>(deathCounterKey + 0x18);

                    foreach (var entry in countersDict.Data)
                    {
                        var keyClass = entry.Key;
                        var value = entry.Value;

                        var hash = Memory.ReadValue<uint>(keyClass + 0x18);

                        if (hash == killHash && value == localPlayerKills)
                            System.Diagnostics.Debug.WriteLine($"Possible kills index: {i}");
                        if (hash == deathHash && value == localPlayerDeaths)
                            System.Diagnostics.Debug.WriteLine($"Possible deaths index: {i}");
                    }
                }
                catch { }
            }
        }
    }
    #endregion

    #region Classes
    // helper class
    public class MemDictionary<T1, T2>
        where T1 : struct where T2 : struct
    {
        public ulong Address { get; }

        public int Count { get; }

        public Dictionary<T1, T2> Data { get; }

        public MemDictionary(ulong address)
        {
            Address = address;
            Count = Memory.ReadValue<int>(address + 0x40);
            if (Count > 4096 || Count < 0) throw new ArgumentOutOfRangeException();

            var arrayBase = Memory.ReadPtr(address + 0x18) + 0x28;

            var t1Size = (uint)Marshal.SizeOf(typeof(T1));
            var t2Size = (uint)Marshal.SizeOf(typeof(T2));

            var retDict = new Dictionary<T1, T2>();
            var dictSize = (t1Size + t2Size + 0x8) * Count;

            var buf = Memory.ReadBuffer(arrayBase, (int)dictSize); // Single read into mem buffer

            for (uint i = 0; i < Count; i++) // parse buffer for entries
            {
                var index = i * (t1Size + t2Size + 0x8);
                var t1 = MemoryMarshal.Read<T1>(buf.Slice((int)index, (int)t1Size));
                var t2 = MemoryMarshal.Read<T2>(buf.Slice((int)index + (int)t1Size, (int)t2Size));

                retDict.Add(t1, t2);
            }

            Data = retDict;
        }
    }
}
#endregion

#region Example
/*
Originally Posted by Promptitude View Post
In EFT.Profile there is 'Stats' at offset 0xD8.
[D8] Stats : -.GClass04DC

In stats, there is 'OverallCounters' at offset 0x18.
[18] OverallCounters : -.GClass141A

In OverallCounters, Deaths and Kills are at 0x70 and 0x78 respectively.
[70][S] Deaths : -.GClass141A.GClass141B
[78][S] Kills : -.GClass141A.GClass141B
---end quote
You're almost there, but I think you have misinterpreted something (the static fields {Deaths, Kills} are not a part of the OverallCounters class, they are part of a separate class used just to store the initialized type from the enum). (Image: Stats & OverallCounters)

The OverallCounters class contains two fields. Importantly the first field is the pointer to the counters dictionary.
The first param (key) of that dictionary is a pointer to a class which is initialized using the enum as an argument. The notable field in that class is the int32 at offset 0x18, which is the hash code for the arguments passed through the constructor.
The second param (value) is 8 bytes, usually uint64 (but is casted to other types for other counters). For kills and deaths, you can interpret the value as uint64.
Now the 'hard' part is getting the correct key for the counter you want.
I collected the hashes in the code below just through reverse engineering. I acquired my kills & deaths from the values in the dictionary, then validated the corresponding hashes against other players.

You may be able to calculate the hash from the enum (listed below), but I didn't try to attempt this.

The code below is costly, so cache the results and use the cache on future iterations.
You should also note that the 'OverallCounters' only contain statistics from the moment the player connected to that raid.
For example: any kills in that match are not appended to those counters. Those are accumulated in the SessionCounters field instead.

Edit: The hashes in the code below are dynamic. This means you will need to figure out how to calculate them or update them manually each restart of the game. :(
I'll take another look at it later and see if I can solve it.

Edit2: The way I've temporarily solved this is by using index 6 (kills) and index 2 (deaths) of the local player's OverallCounters dict to get the hashes for future iterations.
These indices are constant for me on the local player only, hence grabbing the hashes this way. I might look into this further if I get bored. But for now, I consider that a working solution.

Code:
// Tested as a PMC only, solving Player Scavs will be a similar process. Have fun.
var profile = Mem.Read<UInt64>(entity.Address + 0x3D8);
var stats = Mem.Read<UInt64>(profile + 0xD8);
var overallCounters = Mem.Read<UInt64>(stats + 0x18); // SessionCounters is at 0x10
var counters = Mem.Read<UInt64>(overallCounters + 0x10); // Dictionary<IntPtr, ulong>
 
if(KillHash == 0)
{
    if (!entity.IsMe) continue;
 
    var arrayBase = Mem.Read<UInt64>(counters + 0x18) + 0x28;
    
    var killCounterKey = Mem.Read<UInt64>(arrayBase + 6 * 0x18);
    KillHash = Mem.Read<UInt32>(killCounterKey + 0x18);
    
    var deathCounterKey = Mem.Read<UInt64>(arrayBase + 2 * 0x18);
    DeathHash = Mem.Read<UInt32>(deathCounterKey + 0x18);
}
 
var countersDict = new MemDictionary<ulong, ulong>(counters); // Key param is GClass141C in this post
foreach (var entry in countersDict.Data)
{
    var keyClass = entry.Key;
    var value = entry.Value;
 
    var hash = Mem.Read<UInt32>(keyClass + 0x18);
 
    if (hash == KillHash) // This hash should work with PMCs and Player Scavs
        kills = value;
 
    if (hash == DeathHash) // This hash is PMC's only
        deaths = value;
}
 
var kd = deaths != 0 ? (float)kills / deaths : kills;

*/
#endregion