using eft_dma_radar.Source;
using eft_dma_radar.Source.Tarkov;
using Offsets;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using System.Security.AccessControl;
using TarkovDev;
using static vmmsharp.lc;

namespace eft_dma_radar
{

    /// <summary>
    /// Class containing Game (Raid) instance.
    /// </summary>
    public class Game
    {
        private readonly ulong _unityBase;
        private GameObjectManager _gom;
        public ulong _app;
        public ulong MainApplication;
        private ulong _localGameWorld;
        private FPSCamera _fpsCamera;
        private OpticCamera _opticCamera;
        private LootManager _lootManager;
        private RegisteredPlayers _rgtPlayers;
        private GrenadeManager _grenadeManager;
        private ExfilManager _exfilManager;
        private volatile bool _inGame = false;
        private volatile bool _loadingLoot = false;
        private volatile bool _refreshLoot = false;
        private volatile bool _refreshPlayers = false;
        #region Getters
        public bool InGame
        {
            get => _inGame;
            set => _inGame = value;
        }
        public bool LoadingLoot
        {
            get => _loadingLoot;
        }
        public ReadOnlyDictionary<string, Player> Players
        {
            get => _rgtPlayers?.Players;
        }
        public Player LocalPlayer
        {
            get => _rgtPlayers?.Players.Where(p => p.Value.Type == PlayerType.LocalPlayer).First().Value;
        }

        public OpticCamera OpticCamera => _opticCamera;

        private object viewMatrixLock = new object();
        private Matrix4x4 _viewMatrix;
        public Matrix4x4 ViewMatrix
        {
            get
            {
                lock(viewMatrixLock)
                {
                    return _viewMatrix;
                }
            }
            set
            {
                lock(viewMatrixLock)
                {
                    _viewMatrix = value;
                }            
            }
        }
        private object viewOpticMatrixLock = new object();
        private Matrix4x4 _viewOpticMatrix;
        public Matrix4x4 ViewOpticMatrix
        {
            get
            {
                lock (viewOpticMatrixLock)
                {
                    return _viewOpticMatrix;
                }
            }
            set
            {
                lock (viewOpticMatrixLock)
                {
                    _viewOpticMatrix = value;
                }
            }
        }
        public LootManager Loot
        {
            get => _lootManager;
        }
        public FPSCamera FPSCamera
        {
            get => _fpsCamera;
        }
        public ReadOnlyCollection<Grenade> Grenades
        {
            get => _grenadeManager?.Grenades;
        }
        public ReadOnlyCollection<Exfil> Exfils
        {
            get => _exfilManager?.Exfils;
        }
        #endregion

        /// <summary>
        /// Game Constructor.
        /// </summary>
        public Game(ulong unityBase)
        {
            _unityBase = unityBase;
        }

        #region GameLoop
        /// <summary>
        /// Main Game Loop executed by Memory Worker Thread. Updates player list, and updates all player values.
        /// </summary>
        public void GameLoop()
        {
            try
            {
                if (_refreshPlayers)
                {
                    _refreshPlayers = false;
                    var rgtPlayers = new RegisteredPlayers(Memory.ReadPtr(_localGameWorld + Offsets.LocalGameWorld.RegisteredPlayers));
                    rgtPlayers.UpdateList(); // Check for new players, add to list
                    _rgtPlayers = rgtPlayers;
                    Thread.Sleep(1000);
                }
                _rgtPlayers.UpdateList(); // Check for new players, add to list
                _rgtPlayers.UpdateAllPlayers(); // Update all player locations,etc.
                ViewMatrix = FPSCamera.GetViewMatrix();
                ViewOpticMatrix = _opticCamera.GetViewMatrix();
                if (InGame && LocalPlayer is not null)
                {
                    LocalPlayer.NoRecoil();
                    LocalPlayer.Kekbot();
                }

                UpdateMisc(); // Loot, grenades, exfils,etc.
                
            }
            catch (DMAShutdown)
            {
                _inGame = false;
                throw;
            }
            catch (RaidEnded)
            {
                Program.Log("Raid has ended!");
                _inGame = false;
            }
            catch (Exception ex)
            {
                Program.Log($"CRITICAL ERROR - Raid ended due to unhandled exception: {ex}");
                _inGame = false;
                throw;
            }

        }
        #endregion

        #region Methods
        /// <summary>
        /// Waits until Raid has started before returning to caller.
        /// </summary>
        public void WaitForGame()
        {
            while (true)
            {
                if (GetGOM() && GetApp() && GetLGW() && GetFPSCamera()  && GetOptic())
                {
                    Thread.Sleep(1000);
                    break;
                }
                else Thread.Sleep(1500);
            }
            Program.Log("Raid has started!");
            _inGame = true;
            Thread.Sleep(1500); // brief pause before entering main loop / loading loot
        }

        /// <summary>
        /// Helper method to locate Game World object.
        /// </summary>
        private ulong GetObjectFromList(ulong activeObjectsPtr, ulong lastObjectPtr, string objectName)
        {
            var activeObject = Memory.ReadValue<BaseObject>(Memory.ReadPtr(activeObjectsPtr));
            var lastObject = Memory.ReadValue<BaseObject>(Memory.ReadPtr(lastObjectPtr));

           

            if (activeObject.obj != 0x0)
            {
                bool f = true;
                while (activeObject.obj != 0x0 && (f || activeObject.obj != lastObject.obj))
                {
                    f = false;
                    var objectNamePtr = Memory.ReadPtr(activeObject.obj + Offsets.GameObject.ObjectName);
                    var objectNameStr = Memory.ReadString(objectNamePtr, 64);

                    if (objectNameStr.Contains(objectName, StringComparison.OrdinalIgnoreCase))
                    {
                        Program.Log($"Found object {objectNameStr} 0x{activeObject.obj.ToString("X")}");
                        return activeObject.obj;
                    }

                    activeObject = Memory.ReadValue<BaseObject>(activeObject.nextObjectLink); // Read next object
                } 
            }
            Program.Log($"Couldn't find object {objectName}");
            return 0;
        }

        /// <summary>
        /// Gets Game Object Manager structure.
        /// </summary>
        private bool GetGOM()
        {
            try
            {
                var addr = Memory.ReadPtr(_unityBase + Offsets.ModuleBase.GameObjectManager);
                _gom = Memory.ReadValue<GameObjectManager>(addr);
                Program.Log($"Found Game Object Manager at 0x{addr.ToString("X")}");
                return true;
            }
            catch (DMAShutdown) { throw; }
            catch (Exception ex)
            {
                throw new GameNotRunningException($"ERROR getting Game Object Manager, game may not be running: {ex}");
            }
        }

        /// <summary>
        /// Gets Local Game World address.
        /// </summary>
        private bool GetLGW()
        {
            try
            {
                ulong activeNodes = Memory.ReadPtr(_gom.ActiveNodes);
                ulong lastActiveNode = Memory.ReadPtr(_gom.LastActiveNode);
                var gameWorld = GetObjectFromList(activeNodes, lastActiveNode, "GameWorld");
                if (gameWorld == 0) throw new Exception("Unable to find GameWorld Object, likely not in raid.");
                
                _localGameWorld = Memory.ReadPtrChain(gameWorld, Offsets.GameWorld.To_LocalGameWorld); // Game world >> Local Game World
                Program.Log($"Found Local GameWorld Object at 0x{_localGameWorld.ToString("X")}");
                var rgtPlayers = new RegisteredPlayers(Memory.ReadPtr(_localGameWorld + Offsets.LocalGameWorld.RegisteredPlayers));
                //var rgtPlayers = new RegisteredPlayers(Memory.ReadPtr(_localGameWorld + Offsets.LocalGameWorld.AllAlivePlayers));
                if (rgtPlayers.PlayerCount > 0) // Make sure not in hideout,etc.
                {
                    _rgtPlayers = rgtPlayers; // update ref
                    return true;
                }
                else
                {
                    Program.Log("ERROR - Local Game World does not contain players (hideout?)");
                    return false;
                }
            }
            catch (DMAShutdown) { throw; }
            catch (Exception ex)
            {
                Program.Log($"ERROR getting Local Game World: {ex}");
                return false;
            }
        }

        public void UpdateRegPlayers()
        {
            _refreshPlayers = true;
            /*foreach (var player in _rgtPlayers.Players)
            {
                player.Value.SetPosition(null);
            }*/
        }
        
        public ulong GetComponent(ulong obj, string s)
        {
            ulong comps = Memory.ReadPtr(obj + 0x30);
            for (ulong i = 0x8; i < 0x400; i += 0x10)
            {
                try
                {
                    var fields = Memory.ReadPtr(Memory.ReadPtr(comps + i) + 0x28);

                    var name = Memory.ReadPtrChain(fields, Offsets.Kernel.ClassName);
                    var nameStr = Memory.ReadString(name, 64);
                    Program.Log($"{nameStr} = {fields.ToString("X")}");
                    if (nameStr.Contains(s, StringComparison.OrdinalIgnoreCase))
                    {
                        return fields;
                    }
                }
                catch { }
            }
            return 0;
        }

        /// <summary>
        /// Gets Application (Main Client).
        /// </summary>
        private bool GetApp()
        {
            try
            {
                if (_app != 0)
                {
                    return true;
                }
                ulong activeNodes = Memory.ReadPtr(_gom.ActiveNodes);
                ulong lastActiveNode = Memory.ReadPtr(_gom.LastActiveNode);
                var app = GetObjectFromList(activeNodes, lastActiveNode, "Application");
                if (app == 0) throw new Exception("Unable to find Application (Main Client) Object, likely not in raid.");
                _app = Memory.ReadPtrChain(app, new uint[] { GameObject.ObjectClass, 0x18, 0x28 });
                Program.Log($"App {_app.ToString("X")}");
                MainApplication = GetComponent(app, "TarkovApplication");
                Program.Log($"TarkovApplication {MainApplication.ToString("X")}");
                if (MainApplication == 0) throw new Exception("Unable to find TarkovApplication Component, likely not in raid.");

                return true;
            }
            catch (DMAShutdown) { throw; }
            catch (Exception ex)
            {
                Program.Log($"ERROR getting Local Application (Main Client): {ex}");
                return false;
            }
        }

        /// <summary>
        /// Gets FPS Camera.
        /// </summary>
        private bool GetFPSCamera()
        {
            try
            {
                ulong activeNodes = Memory.ReadPtr(_gom.MainCameraTaggedNodes);
                ulong lastActiveNode = Memory.ReadPtr(_gom.LastMainCameraTaggedNode);
                _fpsCamera = new FPSCamera(GetObjectFromList(activeNodes, lastActiveNode, "FPS Camera"));
                if (_fpsCamera.p == 0) throw new Exception("Unable to find FPS Camera Object, likely not in raid.");
                return true;
            }
            catch (DMAShutdown) { throw; }
            catch (Exception ex)
            {
                Program.Log($"ERROR getting FPS Camera: {ex}");
                return false;
            }
        }

        private bool GetOptic()
        {

            var temp = Memory.ReadPtr(this._unityBase + Offsets.ModuleBase.AllCameras + 0x0);
            temp = Memory.ReadPtr(temp + 0x0);

            var y = 0; 
            var loop_count = 400;
            do
            {
                ulong camera_object;
                camera_object = Memory.ReadPtr(temp + 0x0);
                if (camera_object != 0)
                {
                    try
                    {
                        var camera_name_ptr = Memory.ReadPtr(camera_object + 0x30);
                        camera_name_ptr = Memory.ReadPtr(camera_name_ptr + 0x60);
                        var camera_target_name = Memory.ReadString(camera_name_ptr, 64);
                        //Program.Log($"FoundCamera {camera_target_name} 0x{camera_object.ToString("X")}");
                        if (camera_target_name.Contains("BaseOpticCamera(Clone)", StringComparison.OrdinalIgnoreCase))
                        {
                            Program.Log($"FoundCamera BaseOpticCamera(Clone) 0x{camera_object.ToString("X")}");
                            _opticCamera = new OpticCamera(camera_object);
                            if (_opticCamera.p == 0)
                                return false;
                            return true;
                        }
                        /*if (camera_target_name.Contains("FPS Camera", StringComparison.OrdinalIgnoreCase))
                        {
                            Program.Log($"Found2 FPS Camera 0x{camera_object.ToString("X")}");
                            return true;
                        }*/
                    }
                    catch { }
                    
                }
                temp = temp + 0x8; y++;
            } while (y < loop_count) ;

            throw new Exception("Unable to find BaseOpticCamera(Clone) Object, likely not in raid.");
            return false;
        }
 

        /// <summary>
        /// Loot, grenades, exfils,etc.
        /// </summary>
        private void UpdateMisc()
        {
            if (_lootManager is null || _refreshLoot)
            {
                _loadingLoot = true;
                try
                {
                    var loot = new LootManager(_localGameWorld);
                    _lootManager = loot; // update ref
                    _refreshLoot = false;
                }
                catch (Exception ex)
                {
                    Program.Log($"ERROR loading LootEngine: {ex}");
                }
                _loadingLoot = false;
            }
            if (_grenadeManager is null)
            {
                try
                {
                    var grenadeManager = new GrenadeManager(_localGameWorld);
                    _grenadeManager = grenadeManager; // update ref
                }
                catch (Exception ex)
                {
                    Program.Log($"ERROR loading GrenadeManager: {ex}");
                }
            }
            else _grenadeManager.Refresh(); // refresh via internal stopwatch
            if (_exfilManager is null)
            {
                try
                {
                    var exfils = new ExfilManager(_localGameWorld);
                    _exfilManager = exfils; // update ref
                }
                catch (Exception ex)
                {
                    Program.Log($"ERROR loading ExfilController: {ex}");
                }
            }
            else _exfilManager.Refresh(); // periodically refreshes (internal stopwatch)
        }
        public void RefreshLoot()
        {
            if (_inGame)
            {
                _refreshLoot = true;
            }
        }
        #endregion
    }

    #region Exceptions
    public class GameNotRunningException : Exception
    {
        public GameNotRunningException()
        {
        }

        public GameNotRunningException(string message)
            : base(message)
        {
        }

        public GameNotRunningException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class RaidEnded : Exception
    {
        public RaidEnded()
        {
        }

        public RaidEnded(string message)
            : base(message)
        {
        }

        public RaidEnded(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
    #endregion
}
