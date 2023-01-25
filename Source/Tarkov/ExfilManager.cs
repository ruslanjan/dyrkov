using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;

namespace eft_dma_radar
{
    public class ExfilManager
    {
        private readonly Stopwatch _sw = new();
        /// <summary>
        /// List of PMC Exfils in Local Game World and their position/status.
        /// </summary>
        public ReadOnlyCollection<Exfil> Exfils { get; }

        public ExfilManager(ulong localGameWorld)
        {
            var exfilController = Memory.ReadPtr(localGameWorld + Offsets.LocalGameWorld.ExfilController);
            var exfilPoints = Memory.ReadPtr(exfilController + Offsets.ExfilController.ExfilList);
            var count = Memory.ReadValue<int>(exfilPoints + Offsets.UnityList.Count);
            if (count < 1 || count > 24) throw new ArgumentOutOfRangeException();
            var list = new List<Exfil>();
            for (uint i = 0; i < count; i++)
            {
                var exfilAddr = Memory.ReadPtr(exfilPoints + Offsets.UnityListBase.Start + (i * 0x08));
                var exfil = new Exfil(exfilAddr);
                list.Add(exfil);
            }
            try
            {
                if (false)
                {
                    var scavExfilPoints = Memory.ReadPtr(exfilController + Offsets.ExfilController.ScavExfilList);
                    var scavCount = Memory.ReadValue<int>(scavExfilPoints + Offsets.UnityList.Count);
                    if (scavCount < 1 || scavCount > 24) throw new ArgumentOutOfRangeException();
                    for (uint i = 0; i < scavCount; i++)
                    {
                        var exfilAddr = Memory.ReadPtr(scavExfilPoints + Offsets.UnityListBase.Start + (i * 0x08));
                        var exfil = new Exfil(exfilAddr, true);
                        list.Add(exfil);
                    }
                }
            } catch {
                Program.Log("Failed to load scav exfils");
            }
            
            Exfils = new(list); // update readonly ref
            UpdateExfils(); // Get initial statuses
            _sw.Start();
        }

        /// <summary>
        /// Checks if Exfils are due for a refresh, and then refreshes them.
        /// </summary>
        public void Refresh()
        {
            if (_sw.ElapsedMilliseconds < 5000) return;
            UpdateExfils();
            _sw.Restart();
        }

        /// <summary>
        /// Updates exfil statuses.
        /// </summary>
        private void UpdateExfils()
        {
            try
            {
                var map = new ScatterReadMap();
                var round1 = map.AddRound();
                for (int i = 0; i < Exfils.Count; i++)
                {
                    round1.AddEntry(i, 0, Exfils[i].BaseAddr + Offsets.Exfil.Status, typeof(int));
                }
                map.Execute(Exfils.Count);
                for (int i = 0; i < Exfils.Count; i++)
                {
                    try
                    {
                        var status = (int)map.Results[i][0].Result;
                        Exfils[i].UpdateStatus(status);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }

    #region Classes_Enums
    public class Exfil
    {
        public ulong BaseAddr { get; }
        public Vector3 Position { get; }
        public String name { get; }

        public bool isScav { get; }
        public ExfilStatus Status { get; private set; } = ExfilStatus.Closed;

        public Exfil(ulong baseAddr, bool isScav = false)
        {
            this.BaseAddr = baseAddr;
            var transform_internal = Memory.ReadPtrChain(baseAddr, Offsets.GameObject.To_TransformInternal);
            Position = new Transform(transform_internal).GetPosition();
            var name_ptr = Memory.ReadPtr(Memory.ReadPtr(baseAddr + Offsets.Exfil.ExfilTriggerSettings) + Offsets.ExfilTriggerSettings.Name);
            name = Memory.ReadUnityString(name_ptr);
            this.isScav = isScav;
        }

        /// <summary>
        /// Update status of exfil.
        /// </summary>
        public void UpdateStatus(int status)
        {
            switch (status)
            {
                case 1: // NotOpen
                    this.Status = ExfilStatus.Closed;
                    break;
                case 2: // IncompleteRequirement
                    this.Status = ExfilStatus.Pending;
                    break;
                case 3: // Countdown
                    this.Status = ExfilStatus.Open;
                    break;
                case 4: // Open
                    this.Status = ExfilStatus.Open;
                    break;
                case 5: // Pending
                    this.Status = ExfilStatus.Pending;
                    break;
                case 6: // AwaitActivation
                    this.Status = ExfilStatus.Pending;
                    break;
                default:
                    break;
            }
        }
    }

    public enum ExfilStatus
    {
        Open,
        Pending,
        Closed
    }
    #endregion
}
