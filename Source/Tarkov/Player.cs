using System;
using System.Diagnostics;
using System.Numerics;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace eft_dma_radar
{
    /// <summary>
    /// Class containing Game Player Data.
    /// </summary>
    public class Player
    {
        private static readonly FileSystemWatcher _watchlistMonitor;
        private static readonly object _watchlistLock = new();
        private static readonly ConcurrentStack<PlayerHistoryEntry> _history = new();
        private static Dictionary<string, int> _groups = new(StringComparer.OrdinalIgnoreCase);
        private static KDManager _kdManager;

        public static readonly bones[] TargetBones = new bones[]
        {
            bones.HumanNeck,
            bones.HumanLCollarbone,
            bones.HumanRCollarbone,
            bones.HumanLUpperarm,
            bones.HumanRUpperarm,
            bones.HumanLForearm1,
            bones.HumanRForearm1,
            bones.HumanLPalm,
            bones.HumanRPalm,
            bones.HumanLThigh1,
            bones.HumanLThigh2,
            bones.HumanRThigh1,
            bones.HumanRThigh2,
            bones.HumanLCalf,
            bones.HumanRCalf,
            bones.HumanLFoot,
            bones.HumanRFoot
        };

        private readonly Stopwatch _posRefreshSw = new();
        private readonly Stopwatch _kdRefreshSw = new();
        private readonly object _posLock = new(); // sync access to this.Position (non-atomic)
        private readonly GearManager _gearManager;
        private Transform _transform;
        private Transform _headTransform;
        private Transform _spineTransform;
        private Transform _pelvisTransform;
        private Dictionary<bones, Transform> _bonesTransforms = new();

        #region PlayerProperties
        /// <summary>
        /// Player is a PMC Operator.
        /// </summary>
        public bool IsPmc { get; }
        /// <summary>
        /// Player is Alive/Not Dead.
        /// </summary>
        public volatile bool IsAlive = true;
        /// <summary>
        /// Player is Active (has not exfil'd).
        /// </summary>
        public volatile bool IsActive = true;
        /// <summary>
        /// Account UUID for Human Controlled Players.
        /// </summary>
        public string AccountID { get; }
        /// <summary>
        /// Player name.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Player Level (Based on experience).
        /// </summary>
        public int Lvl { get; } = 0;
        /// <summary>
        /// MemberCategory of Player Account (Developer,Sherpa,etc.) null if ordinary account/eod.
        /// </summary>
        public string Category { get; }
        /// <summary>
        /// Player's Kill/Death Average
        /// </summary>
        public float KDA { get; private set; } = -1f;
        /// <summary>
        /// Group that the player belongs to.
        /// </summary>
        public int GroupID { get; } = -1;
        /// <summary>
        /// Type of player unit.
        /// </summary>
        public PlayerType Type { get; set; }
        /// <summary>
        /// Player's current health (sum of all 7 body parts).
        /// </summary>
        public int Health { get; private set; } = -1;
        private Vector3 _pos = new(0, 0, 0); // backing field
        private readonly object _hposLock = new(); // sync access to this.Position (non-atomic)
        private Vector3 _hpos = new(0, 0, 0); // backing field
        private readonly object _sposLock = new(); // sync access to this.Position (non-atomic)
        private Vector3 _spos = new(0, 0, 0); // backing field
        private readonly object _pposLock = new(); // sync access to this.Position (non-atomic)
        private Vector3 _ppos = new(0, 0, 0); // backing field

        // Bones poses
        private readonly object _bposLock = new(); // sync access to this.Position (non-atomic)
        private Dictionary<bones, Vector3> _bpos = new(); // backing field
        /// <summary>
        /// Player's Unity Position in Local Game World.
        /// </summary>
        public Vector3 Position // 96 bits, cannot set atomically
        {
            get
            {
                lock (_posLock)
                {
                    return _pos;
                }
            }
            private set
            {
                lock (_posLock)
                {
                    _pos = value;
                }
            }
        }

        public Vector3 HeadPos
        {
            get
            {
                lock (_hposLock)
                {
                    return _hpos;
                }
            }
            private set
            {
                lock (_hposLock)
                {
                    _hpos = value;
                }
            }
        }

        public bool IsAiming = false;
        public bool IsScope = false;

        public Vector3 SpinePos
        {
            get
            {
                lock (_sposLock)
                {
                    return _spos;
                }
            }
            private set
            {
                lock (_sposLock)
                {
                    _spos = value;
                }
            }
        }

        public Vector3 PelvisPos
        {
            get
            {
                lock (_pposLock)
                {
                    return _ppos;
                }
            }
            private set
            {
                lock (_pposLock)
                {
                    _ppos = value;
                }
            }
        }

        public Vector3 getBonePose(bones b)
        {
            lock (_bposLock)
            {
                return _bpos[b];
            }
        }

        public void setBonePose(bones b, Vector3 v)
        {
            lock (_bposLock)
            {
                _bpos[b] = v;
            }
        }

        /// <summary>
        /// Cached 'Zoomed Position' on the Radar GUI. Used for mouseover events.
        /// </summary>
        public Vector2 ZoomedPosition { get; set; } = new();
        /// <summary>
        /// Player's Rotation (direction/pitch) in Local Game World.
        /// 90 degree offset ~already~ applied to account for 2D-Map orientation.
        /// </summary>
        public Vector2 Rotation { get; private set; } = new Vector2(0, 0); // 64 bits will be atomic
        /// <summary>
        /// (PMC ONLY) Player's Gear Loadout.
        /// Key = Slot Name, Value = Item 'Long Name' in Slot
        /// </summary>
        public ReadOnlyDictionary<string, GearItem> Gear
        {
            get => _gearManager?.Gear;
        }
        /// <summary>
        /// If 'true', Player object is no longer in the RegisteredPlayers list.
        /// Will be checked if dead/exfil'd on next loop.
        /// </summary>
        public bool LastUpdate { get; set; } = false;
        /// <summary>
        /// Consecutive number of errors that this Player object has 'errored out' while updating.
        /// </summary>
        public int ErrorCount { get; set; } = 0;
        #endregion

        #region Getters
        /// <summary>
        /// Contains 'Acct UUIDs' of tracked players for the Key, and the 'Reason' for the Value.
        /// </summary>
        private static ReadOnlyDictionary<string, string> Watchlist { get; set; } // init in Static Constructor
        /// <summary>
        /// Contains history of Enemy Players (human-controlled) that are allocated during program runtime.
        /// </summary>
        public static ListViewItem[] History
        {
            get => _history.Select(x => x.View).ToArray();
        }
        /// <summary>
        /// Player is a Hostile PMC Operator.
        /// </summary>
        public bool IsHostilePmc
        {
            get => IsPmc && (Type is PlayerType.PMC || Type is PlayerType.SpecialPlayer);
        }
        /// <summary>
        /// Player is human-controlled.
        /// </summary>
        public bool IsHuman 
        { 
            get => (Type is PlayerType.LocalPlayer ||
                Type is PlayerType.Teammate ||
                Type is PlayerType.PMC ||
                Type is PlayerType.SpecialPlayer ||
                Type is PlayerType.PScav);
        }
        /// <summary>
        /// Player is human-controlled and Active/Alive.
        /// </summary>
        public bool IsHumanActive
        {
            get => (Type is PlayerType.LocalPlayer ||
                Type is PlayerType.Teammate ||
                Type is PlayerType.PMC ||
                Type is PlayerType.SpecialPlayer ||
                Type is PlayerType.PScav) && IsActive && IsAlive;
        }
        /// <summary>
        /// Player is human-controlled & Hostile.
        /// </summary>
        public bool IsHumanHostile
        {
            get => (Type is PlayerType.PMC ||
                Type is PlayerType.SpecialPlayer ||
                Type is PlayerType.PScav);
        }
        /// <summary>
        /// Player is human-controlled, hostile, and Active/Alive.
        /// </summary>
        public bool IsHumanHostileActive
        {
            get => ((Type is PlayerType.PMC ||
                    Type is PlayerType.SpecialPlayer ||
                    Type is PlayerType.PScav)
                    && IsActive && IsAlive);
        }
        /// <summary>
        /// Player is friendly to LocalPlayer (including LocalPlayer) and Active/Alive.
        /// </summary>
        public bool IsFriendlyActive
        {
            get => ((Type is PlayerType.LocalPlayer ||
                Type is PlayerType.Teammate) && IsActive && IsAlive);
        }
        /// <summary>
        /// Player has exfil'd/left the raid.
        /// </summary>
        public bool HasExfild
        {
            get => !IsActive && IsAlive;
        }
        /// <summary>
        /// EFT.Player Address
        /// </summary>
        public ulong Base { get; }
        /// <summary>
        /// EFT.Profile Address
        /// </summary>
        public ulong Profile { get; }
        /// <summary>
        /// PlayerInfo Address (GClass1044)
        /// </summary>
        public ulong Info { get; }
        public ulong TransformInternal { get; }
        public ulong headTransform { get; }
        public ulong spineTransform { get; }
        public ulong pelvisTransform { get; }
        public Dictionary<bones, ulong> bonesTransforms = new(); // backing field

        public ulong VerticesAddr
        {
            get => _transform?.VerticesAddr ?? 0x0;
        }
        public ulong IndicesAddr
        {
            get => _transform?.IndicesAddr ?? 0x0;
        }
        /// <summary>
        /// Health Entries for each Body Part.
        /// </summary>
        public ulong[] HealthEntries { get; }
        public ulong MovementContext { get; }
        public ulong CorpsePtr
        {
            get => Base + Offsets.Player.Corpse;
        }
        /// <summary>
        /// IndicesAddress -> IndicesSize -> VerticesAddress -> VerticesSize
        /// </summary>
        public Tuple<ulong, int, ulong, int> TransformScatterReadParameters
        {
            get => _transform?.GetScatterReadParameters() ?? new Tuple<ulong, int, ulong, int>(0, 0, 0, 0);
        }

        public Tuple<ulong, int, ulong, int> HeadTransformScatterReadParameters
        {
            get => _headTransform?.GetScatterReadParameters() ?? new Tuple<ulong, int, ulong, int>(0, 0, 0, 0);
        }
        public Tuple<ulong, int, ulong, int> SpineTransformScatterReadParameters
        {
            get => _spineTransform?.GetScatterReadParameters() ?? new Tuple<ulong, int, ulong, int>(0, 0, 0, 0);
        }
        public Tuple<ulong, int, ulong, int> PelvisTransformScatterReadParameters
        {
            get => _pelvisTransform?.GetScatterReadParameters() ?? new Tuple<ulong, int, ulong, int>(0, 0, 0, 0);
        }

        public Tuple<ulong, int, ulong, int> BonesTransformScatterReadParameters(bones b)
        {
            return _bonesTransforms[b].GetScatterReadParameters() ?? new Tuple<ulong, int, ulong, int>(0, 0, 0, 0);
        }
        #endregion

        #region Static_Constructor
        static Player()
        {
            LoadWatchlist();
            _watchlistMonitor = new FileSystemWatcher(".")
            {
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "playerWatchlist.txt",
                EnableRaisingEvents = true
            };
            _watchlistMonitor.Changed += new FileSystemEventHandler(watchlist_Changed);
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Player Constructor.
        /// </summary>
        public enum bones : ulong
        {
            HumanBase = 0,
            HumanPelvis = 14,
            HumanLThigh1 = 15,
            HumanLThigh2 = 16,
            HumanLCalf = 17,
            HumanLFoot = 18,
            HumanLToe = 19,
            HumanRThigh1 = 20,
            HumanRThigh2 = 21,
            HumanRCalf = 22,
            HumanRFoot = 23,
            HumanRToe = 24,
            HumanSpine1 = 29,
            HumanSpine2 = 36,
            HumanSpine3 = 37,
            HumanLCollarbone = 89,
            HumanLUpperarm = 90,
            HumanLForearm1 = 91,
            HumanLForearm2 = 92,
            HumanLForearm3 = 93,
            HumanLPalm = 94,
            HumanRCollarbone = 110,
            HumanRUpperarm = 111,
            HumanRForearm1 = 112,
            HumanRForearm2 = 113,
            HumanRForearm3 = 114,
            HumanRPalm = 115,
            HumanNeck = 132,
            HumanHead = 133
        };
        public Player(ulong playerBase, ulong playerProfile, Vector3? pos = null)
        {
            try
            {
                Base = playerBase;
                Profile = playerProfile;
                if (pos is not null)
                {
                    this.Position = (Vector3)pos; // Populate provided Position (usually only for a re-alloc)
                }
                Info = Memory.ReadPtr(playerProfile + Offsets.Profile.PlayerInfo);
                var healthEntriesList = Memory.ReadPtrChain(playerBase, 
                    new uint[] { Offsets.Player.HealthController , 
                        Offsets.HealthController.To_HealthEntries[0], 
                        Offsets.HealthController.To_HealthEntries[1] });
                HealthEntries = new ulong[7];
                for (uint i = 0; i < 7; i++)
                {
                    HealthEntries[i] = Memory.ReadPtrChain(healthEntriesList, new uint[] { 0x30 + (i * 0x18), Offsets.HealthEntry.Value });
                }
                MovementContext = Memory.ReadPtr(playerBase + Offsets.Player.MovementContext);
                
                
                var bone_matrix = Memory.ReadPtrChain(Base, Offsets.Player.bone_matrix);
                headTransform = Memory.ReadPtr(Memory.ReadPtr(bone_matrix + 0x20 + (((ulong)bones.HumanHead) * 0x8)) + 0x10);
                _headTransform = new Transform(headTransform);
                
                pelvisTransform = Memory.ReadPtr(Memory.ReadPtr(bone_matrix + 0x20 + (((ulong)bones.HumanPelvis) * 0x8)) + 0x10);
                _pelvisTransform = new Transform(pelvisTransform);
                
                spineTransform = Memory.ReadPtr(Memory.ReadPtr(bone_matrix + 0x20 + (((ulong)bones.HumanSpine3) * 0x8)) + 0x10);
                _spineTransform = new Transform(spineTransform);

                foreach (var b in TargetBones)
                {
                    bonesTransforms[b] = Memory.ReadPtr(Memory.ReadPtr(bone_matrix + 0x20 + (((ulong)b) * 0x8)) + 0x10);
                    _bonesTransforms[b] = new Transform(bonesTransforms[b]);
                }


                TransformInternal = Memory.ReadPtrChain(playerBase, Offsets.Player.To_TransformInternal);
                _transform = new Transform(TransformInternal, true);
                
                var isLocalPlayer = Memory.ReadValue<bool>(playerBase + Offsets.Player.IsLocalPlayer);
                var playerSide = Memory.ReadValue<int>(Info + Offsets.PlayerInfo.PlayerSide); // Scav, PMC, etc.
                IsPmc = playerSide == 0x1 || playerSide == 0x2;
                if (isLocalPlayer)
                {
                    Program.Log($"LocalPlayer:0x{Base.ToString("X")}");
#if DEBUG
                    // Run this section while 'In-Raid' as a PMC (not Scav)
                    Debug.WriteLine($"LocalPlayer Acct Id: {GetAccountID()}");
                    /* Change 0, 0 with the Kills, Deaths for your player as obtained in the 'Overall' game tab
                     * Use result(s) to set KillIndex/DeathIndex in KDManager.cs */
                    //KDManager.TestGetIndexes(Profile, 0, 0);
                    //try { _kdManager = new KDManager(Profile); } catch { } // Attempt to instantiate KDManager
                    //Debug.WriteLine($"LocalPlayer K/D: {_kdManager?.GetKD(Profile)}"); // Check if K/D is correct, reference 'Overall' game tab
#endif
                    GroupID = GetGroupID();
                    Type = PlayerType.LocalPlayer;
                }
                else
                {
                    if (playerSide == 0x4) // scav
                    {
                        var regDate = Memory.ReadValue<int>(Info + Offsets.PlayerInfo.RegDate); // Bots wont have 'reg date'
                        if (regDate == 0) // AI SCAV
                        {
                            var settings = Memory.ReadPtr(Info + Offsets.PlayerInfo.Settings);
                            var roleFlag = (WildSpawnType)Memory.ReadValue<int>(settings + Offsets.PlayerSettings.Role);
                            var role = roleFlag.GetRole();
                            Name = role.Name;
                            Type = role.Type;
                        }
                        else // PLAYER SCAV
                        {
                            Type = PlayerType.PScav;
                            try { _gearManager = new GearManager(playerBase, false); } catch { } // Don't fail allocation - low prio
                            GroupID = GetGroupID();
                            Lvl = GetPlayerLevel();
                            Category = GetMemberCategory();
                            var namePtr = Memory.ReadPtr(Info + Offsets.PlayerInfo.MainProfileNickname); // Get Player Scav's PMC Profile Nickname
                            Name = Memory.ReadUnityString(namePtr);
                            AccountID = GetAccountID();
                        }
                    }
                    else if (IsPmc) // 0x1 0x2 usec/bear
                    {
                        Type = PlayerType.PMC;
                        try { _gearManager = new GearManager(playerBase, true); } catch { } // Don't fail allocation - low prio
                        GroupID = GetGroupID();
                        Lvl = GetPlayerLevel();
                        Category = GetMemberCategory();
                        var namePtr = Memory.ReadPtr(Info + Offsets.PlayerInfo.Nickname);
                        Name = Memory.ReadUnityString(namePtr);
                        AccountID = GetAccountID();

                    }
                    else throw new ArgumentOutOfRangeException("playerSide");
                }
                FinishAlloc(); // Finish allocation (check watchlist, member type,etc.)
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR during Player constructor for base addr 0x{playerBase.ToString("X")}", ex);
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Event fires when the Player Watchlist Textfile is updated.
        /// </summary>
        private static void watchlist_Changed(object sender, FileSystemEventArgs e)
        {
            LoadWatchlist();
        }
        #endregion

        #region Setters
        /// <summary>
        /// Set player health.
        /// </summary>
        public bool SetHealth(object[] obj)
        {
            try
            {
                float totalHealth = 0;
                for (uint i = 0; i < HealthEntries.Length; i++)
                {
                    float health = (float)obj[i]; // unbox
                    totalHealth += health;
                }
                this.Health = (int)Math.Round(totalHealth);
                return true;
            }
            catch (Exception ex)
            {
                Program.Log($"ERROR getting Player '{Name}' Health: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Set player rotation (Direction/Pitch)
        /// </summary>
        public bool SetRotation(object obj)
        {
            try
            {
                Vector2 rotation = (Vector2)obj; // unbox
                Vector2 result;
                rotation.X -= 90; // degs offset
                if (rotation.X < 0) rotation.X += 360f; // handle if neg

                if (rotation.X < 0) result.X = 360f + rotation.X;
                else result.X = rotation.X;
                if (rotation.Y < 0) result.Y = 360f + rotation.Y;
                else result.Y = rotation.Y;
                this.Rotation = result;

                return true;
            }
            catch (Exception ex)
            {
                Program.Log($"ERROR getting Player '{Name}' Rotation: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Set player position (Vector3 X,Y,Z)
        /// </summary>
        public bool SetPosition(object[] obj)
        {
            try
            {
                if (obj is null) throw new NullReferenceException();
                var input = new object[] { obj[0], obj[1] };
                if (input[0] is null || input[1] is null)
                    input = null;
                this.Position = _transform.GetPosition(input);
                input = new object[] { obj[2], obj[3] };
                if (input[0] is null || input[1] is null)
                    input = null;
                this.HeadPos = _headTransform.GetPosition(input);
                input = new object[] { obj[4], obj[5] };
                if (input[0] is null || input[1] is null)
                    input = null;
                this.SpinePos = _spineTransform.GetPosition(input);
                input = new object[] { obj[6], obj[7] };
                if (input[0] is null || input[1] is null)
                    input = null;
                this.PelvisPos = _pelvisTransform.GetPosition(input);

                var i = 8;
                foreach (var b in TargetBones)
                {
                    try
                    {
                        if (obj[i] is not null && obj[i + 1] is not null)
                            setBonePose(b, _bonesTransforms[b].GetPosition(new object[] { obj[i], obj[i + 1] }));
                        else
                            setBonePose(b, _bonesTransforms[b].GetPosition());
                    } catch { }
                    i += 2;
                }
                return true;
            }
            catch (Exception ex) // Attempt to re-allocate Transform on error
            {
                Program.Log($"ERROR getting Player '{Name}' Position: {ex}");
                if (!_posRefreshSw.IsRunning) _posRefreshSw.Start();
                else if (_posRefreshSw.ElapsedMilliseconds < 50) // Rate limit attempts on getting pos to prevent stutters
                {
                    return false;
                }
                try
                {
                    Program.Log($"Attempting to get new Transform for Player '{Name}'...");
                    var transform = new Transform(TransformInternal, true);
                    _transform = transform;
                    Program.Log($"Player '{Name}' obtained new Position Transform OK.");
                    _headTransform = new Transform(headTransform);
                    _spineTransform = new Transform(spineTransform);
                    _pelvisTransform = new Transform(pelvisTransform);
                    foreach (var b in TargetBones)
                    {
                        _bonesTransforms[b] = new Transform(bonesTransforms[b]);
                    }
                }
                catch (Exception ex2)
                {
                    Program.Log($"ERROR getting new Transform for Player '{Name}': {ex2}");
                } 
                finally { _posRefreshSw.Restart(); }
                return false;
            }
        }

        /// <summary>
        /// Set PMC Player K/D.
        /// </summary>
        public void SetKD()
        {
            try
            {
                if (_kdManager is null && Type is PlayerType.LocalPlayer && IsPmc)
                {
                    if (_kdRefreshSw.IsRunning)
                        if (_kdRefreshSw.ElapsedMilliseconds < 3000) return; // rate-limit
                        else _kdRefreshSw.Restart();
                    //_kdManager = new KDManager(Profile); // Construct KDManager if LocalPlayer
                }
                else if (IsHostilePmc && KDA == -1f && _kdManager is not null) // Get K/D for Hostile PMCs
                {
                    if (_kdRefreshSw.IsRunning)
                        if (_kdRefreshSw.ElapsedMilliseconds < 3000) return; // rate-limit
                        else _kdRefreshSw.Restart();
                    this.KDA = _kdManager.GetKD(Profile);
                }
            }
            catch (Exception ex)
            {
                Program.Log($"ERROR getting Player '{Name}' K/D: {ex}");
                if (!_kdRefreshSw.IsRunning) _kdRefreshSw.Start();
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Allocation wrap-up.
        /// </summary>
        private void FinishAlloc()
        {
            if (IsHumanHostile) // Hostile Human Controlled Players
            {
                // build log message
                string baseMsg = $"{Name} ({Type}),  L:{Lvl}, "; // append name, type, level
                if (GroupID != -1) baseMsg += $"G:{GroupID}, "; // append group (if in group)
                if (Category is not null)
                {
                    Type = PlayerType.SpecialPlayer; // Flag Special Account Types
                    baseMsg += $"Special Acct: {Category}, "; // append acct type (if special)
                }
                baseMsg += $"@{DateTime.Now.ToLongTimeString()}"; // append timestamp
                if (AccountID is not null &&
                    Watchlist is not null &&
                    Watchlist.TryGetValue(AccountID, out var reason)) // player is on watchlist
                {
                    Type = PlayerType.SpecialPlayer; // Flag watchlist player
                    var entry = new PlayerHistoryEntry(AccountID, $"** WATCHLIST ALERT for {baseMsg} ~~ Reason: {reason}");
                    _history.Push(entry);
                }
                else // Not on watchlist
                {
                    var entry = new PlayerHistoryEntry(AccountID, baseMsg);
                    _history.Push(entry);
                }
            }
        }
        /// <summary>
        /// Checks account type of player. Flags special accounts (Sherpa,etc.)
        /// </summary>
        private string GetMemberCategory()
        {
            var member = Memory.ReadValue<int>(Info + Offsets.PlayerInfo.MemberCategory);
            if (member == 0x0 || member == 0x2) return null; // Ignore 0x0 (Standard Acct) and 0x2 (EOD Acct)
            else
            {
                var flags = (MemberCategory)member;
                return flags.ToString("G"); // Returns all flags that are set
            }
        }
        /// <summary>
        /// Get Account ID for Human-Controlled Players.
        /// </summary>
        private string GetAccountID()
        {
            var idPtr = Memory.ReadPtr(Profile + Offsets.Profile.AccountId);
            return Memory.ReadUnityString(idPtr);
        }
        /// <summary>
        /// Gets player Level based on XP.
        /// </summary>
        private int GetPlayerLevel()
        {
            var exp = Memory.ReadValue<int>(Info + Offsets.PlayerInfo.Experience);
            return _expTable.Where(x => x.Key > exp).FirstOrDefault().Value - 1;
        }

        /// <summary>
        /// Gets player's Group Number.
        /// </summary>
        private int GetGroupID()
        {
            try
            {
                var grpPtr = Memory.ReadPtr(Info + Offsets.PlayerInfo.GroupId);
                var grp = Memory.ReadUnityString(grpPtr);
                _groups.TryAdd(grp, _groups.Count);
                return _groups[grp];
            }
            catch { return -1; } // will return null if Solo / Don't have a team
        }
        /// <summary>
        /// Resets/Updates 'static' assets in preparation for a new game/raid instance.
        /// </summary>
        public static void Reset()
        {
            _groups = new(StringComparer.OrdinalIgnoreCase);
            if (_history.TryPeek(out var last) && last.Entry == "---NEW GAME---") { } // Don't spam repeated entries
            else _history.Push(new PlayerHistoryEntry(null, "---NEW GAME---")); // Insert separator in PMC History Log
        }

        /// <summary>
        /// Resets KDManager if the game has closed/re-opened, since hash values *will* be different.
        /// </summary>
        public static void ResetKDManager()
        {
            KDManager.Reset(ref _kdManager);
        }

        /// <summary>
        /// Reloads playerWatchlist.txt into Memory.
        /// </summary>
        public static void LoadWatchlist()
        {
            lock (_watchlistLock) // Sync access to File IO, Resources
            {
                var watchlist = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // Allocate new Dictionary (case insensitive keys)
                if (!File.Exists("playerWatchlist.txt"))
                {
                    File.WriteAllText("playerWatchlist.txt",
                        "PlayerAcctID : Watchlist reason/comment here (one entry per line)");
                }
                else
                {
                    var lines = File.ReadAllLines("playerWatchlist.txt");
                    foreach (var line in lines)
                    {
                        var split = line.Split(':'); // remove single delimiting ':' character
                        if (split.Length == 2)
                        {
                            var id = split[0].Trim();
                            var reason = split[1].Trim();
                            watchlist.TryAdd(id, reason);
                        }
                    }
                }
                Watchlist = new(watchlist); // Update ref
            }
        }

        public void UpdateIsAiming()
        {
            try
            {
                var proceduralWepAnim = Memory.ReadPtr(Base + Offsets.Player.ProceduralWeaponAnimation);
                var breath = Memory.ReadPtr(proceduralWepAnim + Offsets.ProceduralWeaponAnimation.Breath);
                IsAiming = Memory.ReadValue<bool>(breath + Offsets.Breath.IsAiming);
            } catch { }
        }

        
        public void ToggleMaxStamina()
        {
            if (Type == PlayerType.LocalPlayer)
            {
                var playerBody = Memory.ReadPtr(Base + Offsets.Player.Physical);
                var stamina = Memory.ReadPtr(playerBody + Offsets.Physical.MaxStamina);
                // TotalCapacity = { 0x10, 0x1c };
                Memory.Write(Memory.ReadPtr(stamina + 0x10) + 0x1c, BitConverter.GetBytes(240.0f));
                Memory.Write(stamina + Offsets.Physical.buff, BitConverter.GetBytes(10.0f));
                stamina = Memory.ReadPtr(playerBody + Offsets.Physical.MaxOxygen);
                // TotalCapacity = { 0x10, 0x1c };
                Memory.Write(Memory.ReadPtr(stamina + 0x10) + 0x1c, BitConverter.GetBytes(240.0f));
                Memory.Write(stamina + Offsets.Physical.buff, BitConverter.GetBytes(10.0f));
                stamina = Memory.ReadPtr(playerBody + Offsets.Physical.MaxHandStamina);
                // TotalCapacity = { 0x10, 0x1c };
                Memory.Write(Memory.ReadPtr(stamina + 0x10) + 0x1c, BitConverter.GetBytes(240.0f));
                Memory.Write(stamina + Offsets.Physical.buff, BitConverter.GetBytes(10.0f));
            }
        }

        /// <summary>
        /// set `ProceduralWeaponAnimation ] +0x28 (breath) ] + 0x0A4 (intensity:float)` to 0.0f (nosway)
        /// and set `ProceduralWeaponAnimation ] +0x48 (shotingg) ] +0x40 (RecoilStrengthXy:vector2<float>)` to { 0, 0 }  and `]+0x48 (RecoilStrengthZ:vector2<float>)` to { 0, ??? } (i just write vector3 { 0,0,0 } to RecoilStrengthXy) (norecoil)
        /// or just null the mask `ProceduralWeaponAnimation ] + 0xF8 (?: int32_t)`, idk is nospread is server sided
        /// </summary>
        public bool noRecoil = false;
        public bool noRecoilFirstOn = true;
        internal void ToggleNoRecoil()
        {
            if (noRecoil == false)
            {
                noRecoilFirstOn = true;
            }
            //Program.Log($"ToggleNoRecoil{noRecoil}");
            if (Type == PlayerType.LocalPlayer && noRecoil)
            {
                try
                {
                    var ProceduralWeaponAnimation = Memory.ReadPtr(Base + Offsets.Player.ProceduralWeaponAnimation);
                    Memory.Write(ProceduralWeaponAnimation + 0x100, BitConverter.GetBytes(noRecoilFirstOn ? 1 : 0)); // mask
                    // breath
                    var breath = Memory.ReadPtr(ProceduralWeaponAnimation + Offsets.ProceduralWeaponAnimation.Breath); // +0x28 (breath)
                    Memory.Write(breath + Offsets.Breath.Intensity, BitConverter.GetBytes(.0f)); // +0x0A4(intensity: float
                    Memory.Write(breath + 0x0B8, new byte[] { 0x0, 0x0}); // +0x0B8(TremorOn: float + Fracture

                    // shooting
                    var shotingg = Memory.ReadPtr(ProceduralWeaponAnimation + Offsets.ProceduralWeaponAnimation.Shooting); // +0x48 (shotingg)
                    Memory.Write(shotingg + 0x40, new float[] { .0f, .0f, .0f }.SelectMany(f => BitConverter.GetBytes(f)).ToArray()); // +0x40 (RecoilStrengthXy:vector2<float>)` to { 0, 0 }  and `]+0x48 (RecoilStrengthZ:vector2<float>)` to { 0, ??? } (i just write vector3 { 0,0,0 } to RecoilStrengthXy) (norecoil)
                    Memory.Write(shotingg + 0x6c, BitConverter.GetBytes(.0f));
                    Memory.Write(shotingg + 0x70, BitConverter.GetBytes(.0f));
                    Memory.Write(shotingg + 0x74, BitConverter.GetBytes(.0f));
                    Memory.Write(shotingg + 0x78, BitConverter.GetBytes(.0f));
                    Memory.Write(shotingg + 0x7c, BitConverter.GetBytes(.0f));

                    // Walk
                    var walk = Memory.ReadPtr(ProceduralWeaponAnimation + 0x30);
                    Memory.Write(walk + 0x44, BitConverter.GetBytes(.0f)); // intensity
                    // motionReact
                    var motionReact = Memory.ReadPtr(ProceduralWeaponAnimation + 0x38);
                    Memory.Write(motionReact + 0xD0, BitConverter.GetBytes(.0f)); // intensity
                    // forceReact
                    var forceReact = Memory.ReadPtr(ProceduralWeaponAnimation + 0x40);
                    Memory.Write(forceReact + 0x28, BitConverter.GetBytes(.0f)); // intensity

                    if (noRecoilFirstOn)
                    {
                        noRecoilFirstOn = false;

                        var skills = Memory.ReadPtr(Memory.ReadPtr(Base + Offsets.Player.Profile) + Offsets.Profile.Skills);
                        var skill = Memory.ReadPtr(skills + Offsets.Skills.AttentionExamine);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));
                        skill = Memory.ReadPtr(skills + Offsets.Skills.MagDrillsLoadSpeed);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));

                        skill = Memory.ReadPtr(skills + Offsets.Skills.PerceptionLootDot);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));
                        skill = Memory.ReadPtr(skills + Offsets.Skills.AttentionLootSpeed);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));
                        skill = Memory.ReadPtr(skills + Offsets.Skills.MagDrillsUnLoadSpeed);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));

                        skill = Memory.ReadPtr(skills + Offsets.Skills.StrengthBuffJumpHeightInc);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(0.3f));
                        //skill = Memory.ReadPtr(skills + Offsets.Skills.PerceptionHearing);
                        //Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));
                        skill = Memory.ReadPtr(skills + Offsets.Skills.MagDrillsInventoryCheckSpeed);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));

                        skill = Memory.ReadPtr(skills + Offsets.Skills.MagDrillsInventoryCheckAccuracy);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));
                        skill = Memory.ReadPtr(skills + Offsets.Skills.AimMasterSpeed);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));
                        skill = Memory.ReadPtr(skills + Offsets.Skills.AimMasterWiggle);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));

                        skill = Memory.ReadPtr(skills + Offsets.Skills.RecoilControlImprove);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));
                        skill = Memory.ReadPtr(skills + Offsets.Skills.TroubleFixing);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));
                        
                        skill = Memory.ReadPtr(skills + Offsets.Skills.ThrowingStrengthBuff);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));
                        skill = Memory.ReadPtr(skills + Offsets.Skills.ThrowingEnergyExpenses);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));
                        skill = Memory.ReadPtr(skills + Offsets.Skills.DrawSpeed);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(50.0f));


                        // booleans
                        skill = Memory.ReadPtr(skills + Offsets.Skills.SearchDouble);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(true));
                        skill = Memory.ReadPtr(skills + Offsets.Skills.AimMasterElite);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(true));
                        skill = Memory.ReadPtr(skills + Offsets.Skills.MagDrillsInstantCheck);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(true));

                        skill = Memory.ReadPtr(skills + Offsets.Skills.MagDrillsLoadProgression);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(true));
                        skill = Memory.ReadPtr(skills + Offsets.Skills.StressBerserk);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(true));
                        skill = Memory.ReadPtr(skills + Offsets.Skills.IntellectEliteAmmoCounter);
                        Memory.Write(skill + Offsets.Skills.Value, BitConverter.GetBytes(true));


                        this.disableInertia();
                    }

                } catch (Exception ex)
                {
                    Program.Log($"NoRecoil {ex.ToString()}");
                }
            }
        }

        public void disableInertia()
        {
            var _config = Memory.ReadPtrChain(Memory.Game.MainApplication, Offsets.MainApp.ToConfig);
            var inertia = Memory.ReadPtr(_config + Offsets.Config.Inertia);
            if (inertia != 0)
            {
                Program.Log($"Inertia {inertia.ToString("X")}");
                Program.Log($"Config {_config}");
                Memory.Write(inertia + Offsets.Inertia.FallThreshold, BitConverter.GetBytes(99999.0f));
                Memory.Write(inertia + Offsets.Inertia.BaseJumpPenaltyDuration, BitConverter.GetBytes(.0f));
                Memory.Write(inertia + Offsets.Inertia.DurationPower, BitConverter.GetBytes(.0f));
                Memory.Write(inertia + Offsets.Inertia.BaseJumpPenalty, BitConverter.GetBytes(.0f));
                Memory.Write(inertia + Offsets.Inertia.PenaltyPower, BitConverter.GetBytes(.0f));
                Memory.Write(inertia + Offsets.Inertia.MoveTimeRange, BitConverter.GetBytes(0L));
                Memory.Write(inertia + Offsets.Inertia.MinDirectionBlendTime, BitConverter.GetBytes(.0f));

                //Memory.Write(inertia + 0x10C, BitConverter.GetBytes(3.0f));
                Memory.Write(inertia + 0xA8, BitConverter.GetBytes(9999.0f));
                
            }
        }
        #endregion

        #region XP Table
        private static readonly Dictionary<int, int> _expTable = new Dictionary<int, int>
        {
            {0, 1},
            {1000, 2},
            {4017, 3},
            {8432, 4},
            {14256, 5},
            {21477, 6},
            {30023, 7},
            {39936, 8},
            {51204, 9},
            {63723, 10},
            {77563, 11},
            {92713, 12},
            {111881, 13},
            {134674, 14},
            {161139, 15},
            {191417, 16},
            {225194, 17},
            {262366, 18},
            {302484, 19},
            {345751, 20},
            {391649, 21},
            {440444, 22},
            {492366, 23},
            {547896, 24},
            {609066, 25},
            {675913, 26},
            {748474, 27},
            {826786, 28},
            {910885, 29},
            {1000809, 30},
            {1096593, 31},
            {1198275, 32},
            {1309251, 33},
            {1429580, 34},
            {1559321, 35},
            {1698532, 36},
            {1847272, 37},
            {2005600, 38},
            {2173575, 39},
            {2351255, 40},
            {2538699, 41},
            {2735966, 42},
            {2946585, 43},
            {3170637, 44},
            {3408202, 45},
            {3659361, 46},
            {3924195, 47},
            {4202784, 48},
            {4495210, 49},
            {4801553, 50},
            {5121894, 51},
            {5456314, 52},
            {5809667, 53},
            {6182063, 54},
            {6573613, 55},
            {6984426, 56},
            {7414613, 57},
            {7864284, 58},
            {8333549, 59},
            {8831052, 60},
            {9360623, 61},
            {9928578, 62},
            {10541848, 63},
            {11206300, 64},
            {11946977, 65},
            {12789143, 66},
            {13820522, 67},
            {15229487, 68},
            {17206065, 69},
            {19706065, 70},
            {22706065, 71},
            {26206065, 72},
            {30206065, 73},
            {34706065, 74},
            {39706065, 75},
        };
        #endregion
    }
}
