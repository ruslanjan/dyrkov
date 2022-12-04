using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace eft_dma_radar
{
    public class RegisteredPlayers
    {
        private readonly ulong _base;
        private readonly ulong _listBase;
        private readonly Stopwatch _regSw = new();
        private readonly Stopwatch _healthSw = new();
        private readonly Stopwatch _posSw = new();
        private readonly ConcurrentDictionary<string, Player> _players = new(StringComparer.OrdinalIgnoreCase);

        private int _localPlayerGroup = -100;

        #region Getters
        public ReadOnlyDictionary<string, Player> Players { get; }
        public int PlayerCount
        {
            get
            {
                for (int i = 0; i < 5; i++) // Re-attempt if read fails
                {
                    try
                    {
                        var count = Memory.ReadValue<int>(_base + Offsets.UnityList.Count);
                        if (count < 1 || count > 1024) throw new ArgumentOutOfRangeException();
                        return count;
                    }
                    catch (DMAShutdown) { throw; }
                    catch { Thread.Sleep(1000); } // short delay between read attempts
                }
                return -1; // error
            }
        }
        #endregion

        /// <summary>
        /// RegisteredPlayers List Constructor.
        /// </summary>
        public RegisteredPlayers(ulong baseAddr)
        {
            _base = baseAddr;
            Players = new(_players); // update readonly ref
            _listBase = Memory.ReadPtr(_base + Offsets.UnityList.Base);
            _regSw.Start();
            _healthSw.Start();
            _posSw.Start();
        }

        #region UpdateList
        /// <summary>
        /// Updates the ConcurrentDictionary of 'Players'
        /// </summary>
        public void UpdateList()
        {
            if (_regSw.ElapsedMilliseconds < 500) return; // Update every 500ms
            try
            {
                var count = this.PlayerCount; // cache count
                if (count < 1 || count > 1024)
                    throw new RaidEnded();
                var registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var scatterMap = new ScatterReadMap();
                var round1 = scatterMap.AddRound();
                var round2 = scatterMap.AddRound();
                var round3 = scatterMap.AddRound();
                var round4 = scatterMap.AddRound();
                var round5 = scatterMap.AddRound();
                for (int i = 0; i < count; i++)
                {
                    var playerBase = round1.AddEntry(i,
                        0,
                        _listBase + Offsets.UnityListBase.Start + (uint)(i * 0x8),
                        typeof(ulong));
                    var playerProfile = round2.AddEntry(i, 1, playerBase,
                        typeof(ulong), 0, Offsets.Player.Profile);

                    var playerId = round3.AddEntry(i, 2, playerProfile, typeof(ulong),
                        0, Offsets.Profile.Id);
                    var playerIdLen = round4.AddEntry(i, 3, playerId, typeof(int),
                        0, Offsets.UnityString.Length);
                    var playerIdStr = round5.AddEntry(i, 4, playerId, typeof(UnityString),
                        playerIdLen, Offsets.UnityString.Value);
                    playerIdStr.SizeMult = 2; // Unity String twice the length
                }
                scatterMap.Execute(count); // Execute scatter read
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        if (scatterMap.Results[i][0].Result is null
                            || scatterMap.Results[i][1].Result is null
                            || scatterMap.Results[i][4].Result is null)
                            throw new Exception("Unable to acquire Player ID due to NULL Reads!");
                        var playerBase = (ulong)scatterMap.Results[i][0].Result;
                        var playerProfile = (ulong)scatterMap.Results[i][1].Result;
                        string id = (string)scatterMap.Results[i][4].Result;
                        if (id.Length != 24 && id.Length != 27 && id.Length != 36) throw new ArgumentOutOfRangeException("id"); // Ensure valid ID length
                        registered.Add(id); // ID is registered, cache it
                        if (_players.TryGetValue(id, out var existingPlayer)) // Player already exists, check for problems
                        {
                            existingPlayer.ToggleNoRecoil();
                            if (existingPlayer.ErrorCount > 100) // Erroring out a lot? Re-Alloc
                            {
                                Program.Log($"WARNING - Existing player '{existingPlayer.Name}' being re-allocated due to excessive errors...");
                                ReallocPlayer(id, playerBase, playerProfile);
                            }
                            else if (existingPlayer.Base != playerBase) // Base address changed? Re-Alloc
                            {
                                Program.Log($"WARNING - Existing player '{existingPlayer.Name}' being re-allocated due to new base address...");
                                ReallocPlayer(id, playerBase, playerProfile);
                            }
                            else // Mark active & alive
                            {
                                existingPlayer.IsActive = true;
                                existingPlayer.IsAlive = true;
                            }
                        }
                        else // Does not exist - allocate new player
                        {
                            var player = new Player(playerBase, playerProfile); // allocate new player object
                            if (player.Type is PlayerType.LocalPlayer &&
                                _players.Any(x => x.Value.Type is PlayerType.LocalPlayer))
                            {
                                // Don't allocate more than one LocalPlayer on accident
                            }
                            else
                            {
                                if (_players.TryAdd(id, player))
                                    Program.Log($"Player '{player.Name}' allocated.");
                            }
                        }
                    }
                    catch (DMAShutdown) { throw; }
                    catch (Exception ex)
                    {
                        Program.Log($"ERROR processing RegisteredPlayer at index {i}: {ex}");
                    }
                }
                var inactivePlayers = _players.Where(x => !registered.Contains(x.Key) && x.Value.IsActive);
                foreach (var player in inactivePlayers)
                {
                    player.Value.LastUpdate = true;
                }
            }
            catch (DMAShutdown) { throw; }
            catch (RaidEnded) { throw; }
            catch (Exception ex)
            {
                Program.Log($"CRITICAL ERROR - RegisteredPlayers Loop FAILED: {ex}");
            }
            finally
            {
                _regSw.Restart();
            }
            void ReallocPlayer(string id, ulong newPlayerBase, ulong newPlayerProfile)
            {
                try
                {
                    var player = new Player(newPlayerBase, newPlayerProfile, _players[id].Position); // alloc
                    _players[id] = player; // update ref to new object
                    Program.Log($"Player '{player.Name}' Re-Allocated successfully.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"ERROR re-allocating player '{_players[id].Name}': ", ex);
                }
            }
        }
        #endregion

        #region UpdatePlayers
        /// <summary>
        /// Updates all 'Player' values (Position,health,direction,etc.)
        /// </summary>
        public void UpdateAllPlayers()
        {
            try
            {
                var players = _players.Select(x => x.Value)
                    .Where(x => x.IsActive && x.IsAlive).ToArray();
                if (players.Length == 0) return; // No players
                if (_localPlayerGroup == -100) // Check if current player group is set
                {
                    var localPlayer = _players.FirstOrDefault(x => x.Value.Type is PlayerType.LocalPlayer).Value;
                    if (localPlayer is not null)
                    {
                        _localPlayerGroup = localPlayer.GroupID;
                    }
                }
                bool checkHealth = _healthSw.ElapsedMilliseconds > 250; // every 250 ms
                bool checkPos = _posSw.ElapsedMilliseconds > 10000 &&
                    players.Any(x => x.IsHumanActive); // every 10 sec & at least 1 active human player
                var scatterMap = new ScatterReadMap();
                var round1 = scatterMap.AddRound();
                ScatterReadRound round2 = null;
                if (checkPos) // allocate and add extra rounds to map
                {
                    round2 = scatterMap.AddRound();
                }
                for (int i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    if (player.LastUpdate) // player may be dead/exfil'd
                    {
                        var corpse = round1.AddEntry(i, 10, player.CorpsePtr, typeof(ulong));
                    }
                    else
                    {
                        if (checkHealth) for (int p = 0; p < 7; p++)
                        {
                            var health = round1.AddEntry(i, p, player.HealthEntries[p] + Offsets.HealthEntry.Value,
                                typeof(float), null);
                        }
                        var rotation = round1.AddEntry(i, 7, player.MovementContext + Offsets.MovementContext.Rotation,
                            typeof(System.Numerics.Vector2), null); // x = dir , y = pitch


                        if (checkPos && player.IsHumanActive)
                        {
                            var hierarchy = round1.AddEntry(i, 11, player.TransformInternal, typeof(ulong), null, Offsets.TransformInternal.Hierarchy);
                            var indicesAddr = round2?.AddEntry(i, 12, hierarchy, typeof(ulong), null, Offsets.TransformHierarchy.Indices);
                            var verticesAddr = round2?.AddEntry(i, 13, hierarchy, typeof(ulong), null, Offsets.TransformHierarchy.Vertices);
                        }


                        {
                            var posAddr = player.TransformScatterReadParameters;
                            var indices = round1.AddEntry(i, 8, posAddr.Item1,
                                typeof(List<int>), posAddr.Item2);
                            indices.SizeMult = 4;
                            var vertices = round1.AddEntry(i, 9, posAddr.Item3,
                                typeof(List<Vector128<float>>), posAddr.Item4);
                            vertices.SizeMult = 16;
                        }


                        // head
                        {
                            var hposAddr = player.HeadTransformScatterReadParameters;
                            var hindices = round1.AddEntry(i, 14, hposAddr.Item1,
                                typeof(List<int>), hposAddr.Item2);
                            hindices.SizeMult = 4;
                            var hvertices = round1.AddEntry(i, 15, hposAddr.Item3,
                                typeof(List<Vector128<float>>), hposAddr.Item4);
                            hvertices.SizeMult = 16;
                        }

                        // spine
                        {
                            var sposAddr = player.SpineTransformScatterReadParameters;
                            var sindices = round1.AddEntry(i, 16, sposAddr.Item1,
                                typeof(List<int>), sposAddr.Item2);
                            sindices.SizeMult = 4;
                            var svertices = round1.AddEntry(i, 17, sposAddr.Item3,
                                typeof(List<Vector128<float>>), sposAddr.Item4);
                            svertices.SizeMult = 16;
                        }

                        // pelvis
                        {
                            var pposAddr = player.PelvisTransformScatterReadParameters;
                            var pindices = round1.AddEntry(i, 18, pposAddr.Item1,
                                typeof(List<int>), pposAddr.Item2);
                            pindices.SizeMult = 4;
                            var pvertices = round1.AddEntry(i, 19, pposAddr.Item3,
                                typeof(List<Vector128<float>>), pposAddr.Item4);
                            pvertices.SizeMult = 16;
                        }

                        // bones
                        {
                            var id = 20; // next id
                            foreach (var b in Player.TargetBones)
                            {
                                var posAddr = player.BonesTransformScatterReadParameters(b);
                                var indices = round1.AddEntry(i, id, posAddr.Item1,
                                    typeof(List<int>), posAddr.Item2);
                                indices.SizeMult = 4;
                                var vertices = round1.AddEntry(i, id + 1, posAddr.Item3,
                                    typeof(List<Vector128<float>>), posAddr.Item4);
                                vertices.SizeMult = 16;
                                id += 2;
                            }
                        }
                    }
                }
                scatterMap.Execute(players.Length); // Execute scatter read

                for (int i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    if (_localPlayerGroup != -100
                        && player.GroupID != -1
                        && player.IsHumanHostile)
                    { // Teammate check
                        if (player.GroupID == _localPlayerGroup)
                            player.Type = PlayerType.Teammate;
                    }
                    if (player.LastUpdate) // player may be dead/exfil'd
                    {
                        var corpse = (ulong?)scatterMap.Results[i][10].Result;
                        if (corpse is not null && corpse != 0x0) // dead
                        {
                            player.IsAlive = false;
                        }
                        player.IsActive = false; // mark inactive
                        player.LastUpdate = false; // Last update processed, clear flag
                    }
                    else
                    {
                        bool posOK = true;
                        if (checkPos && player.IsHumanActive) // Position integrity check for active human players
                        {
                            if (scatterMap.Results[i].TryGetValue(12, out var i12) &&
                                    i12?.Result is not null &&
                                    scatterMap.Results[i].TryGetValue(13, out var i13) &&
                                    i13?.Result is not null)
                            {
                                var indicesAddr = (ulong)i12.Result;
                                var verticesAddr = (ulong)i13.Result;
                                if (player.IndicesAddr != indicesAddr ||
                                    player.VerticesAddr != verticesAddr) // check if any addr changed
                                {
                                    Program.Log($"WARNING - Transform has changed for Player '{player.Name}'");
                                    player.SetPosition(null); // alloc new transform
                                    posOK = false; // Don't try update pos with old vertices/indices
                                }
                            }
                        }
                        bool p1 = true;
                        if (checkHealth)
                        {
                            var bodyParts = new object[7];
                            for (int p = 0; p < 7; p++)
                            {
                                bodyParts[p] = scatterMap.Results[i][p].Result;
                            }
                            p1 = player.SetHealth(bodyParts);
                        }
                        var rotation = scatterMap.Results[i][7].Result;
                        bool p2 = player.SetRotation(rotation);
                        var posBufs = new List<object>
                        {
                            scatterMap.Results[i][8].Result,
                            scatterMap.Results[i][9].Result,
                            // head
                            scatterMap.Results[i][14].Result,
                            scatterMap.Results[i][15].Result,
                            // Spine
                            scatterMap.Results[i][16].Result,
                            scatterMap.Results[i][17].Result,
                            // pelvis
                            scatterMap.Results[i][18].Result,
                            scatterMap.Results[i][19].Result,
                        };
                        var id = 20;
                        foreach (var b in Player.TargetBones)
                        {
                            posBufs.Add(scatterMap.Results[i][id].Result);
                            posBufs.Add(scatterMap.Results[i][id + 1].Result);
                            id += 2;
                        }
                        bool p3 = true;
                        if (posOK) p3 = player.SetPosition(posBufs.ToArray());
                        //player.SetKD(); // set KD if not already set
                        if (p1 && p2 && p3) player.ErrorCount = 0;
                        else player.ErrorCount++;
                    }
                }
                if (_players.FirstOrDefault(x => x.Value.Type is PlayerType.LocalPlayer).Value.Base != 0)
                    _players.FirstOrDefault(x => x.Value.Type is PlayerType.LocalPlayer).Value.UpdateIsAiming();
                if (checkHealth) _healthSw.Restart();
                if (checkPos) _posSw.Restart();
            }
            catch (DMAShutdown) { throw; }
            catch (Exception ex)
            {
                Program.Log($"CRITICAL ERROR - UpdatePlayers Loop FAILED: {ex}");
            }
        }
        #endregion
    }
}
