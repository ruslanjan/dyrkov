using Offsets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace eft_dma_radar.Source.Tarkov
{
    public class OpticCamera
    {

        public ulong p;
        public ulong SightComponent;
        private ulong ThermalVision;

        public OpticCamera(ulong p)
        {
            //ThermalVision = this.GetComponent(p, "ThermalVision");
            this.p = p;
        }
        public Matrix4x4 GetViewMatrix()
        {
            var Matrix = Memory.ReadPtrChain(p, new uint[] { 0x30, 0x18 });
            ulong b = 0x2E4; // 0xDC 0x2E4 0x5b0

            
            return Memory.ReadValue<Matrix4x4>(p + b);
        }

        public void PrintClassName()
        {
            var name = Memory.ReadPtrChain(p, Offsets.Kernel.ClassName);
            var nameStr = Memory.ReadString(name, 64);
            Program.Log(nameStr);
        }

        private int visionCycle = 0;
        public void ToggleThermalVision()
        {
            visionCycle++;
            if (visionCycle % 2 == 1)
            {
                //mask = Memory.ReadPtr(ThermalVision + Offsets.ThermalVision.material);
                Memory.Write(ThermalVision + Offsets.ThermalVision.On, new byte[] { 0x1, 0x0, 0x0, 0x0, 0x0, 0x0, });
                Memory.Write(ThermalVision + Offsets.ThermalVision.material, BitConverter.GetBytes(0L));

                //Memory.Write(Memory.ReadPtrChain(ThermalVision, new uint[] { Offsets.ThermalVision.material, 0x10, 0x38}), BitConverter.GetBytes((byte)0xE6));
                
            }
            if (visionCycle % 2 == 0)
            {
                //mask = Memory.ReadPtr(ThermalVision + Offsets.ThermalVision.material);
                Memory.Write(ThermalVision + Offsets.ThermalVision.On, new byte[] { 0x0 });
            }
        }



        public ulong GetComponent(ulong obj, string s)
        {
            ulong comps = Memory.ReadPtr(obj + 0x30);
            for (ulong i = 0x8; i < 0x1000; i += 0x10)
            {
                try
                {
                    var fields = Memory.ReadPtr(Memory.ReadPtr(comps + i) + 0x28);
                    var name = Memory.ReadPtrChain(fields, Offsets.Kernel.ClassName);
                    var nameStr = Memory.ReadString(name, 64);
                    Program.Log(nameStr);
                    if (nameStr == s)
                    {
                        return fields;
                    }
                }
                catch { }
            }
            return 0;
        }
    }
}
