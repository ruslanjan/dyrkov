using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace eft_dma_radar.Source.Tarkov
{
    public class FPSCamera
    {
        public ulong p;
        private ulong ThermalVision;

        public FPSCamera(ulong p)
        {
            this.p = p;
            ThermalVision = this.GetComponent(p, "ThermalVision");
            Program.Log($"ThermalVision{ThermalVision.ToString("X")}");
        }

        private bool thermal = false;
        public void ToggleThermalVision()
        {
            Program.Log(this.GetViewMatrix().ToString());
            thermal = !thermal;
            if (thermal)
            {
                Memory.Write(ThermalVision + Offsets.ThermalVision.On, new byte[] { 0x1, 0x0, 0x0, 0x0, 0x0, 0x0,  });
            } else
            {
                Memory.Write(ThermalVision + Offsets.ThermalVision.On, new byte[] { 0x0 });
            }
        }



        public Matrix4x4 GetViewMatrix()
        {
            var Matrix = Memory.ReadPtrChain(p, new uint[] { 0x30, 0x18});
            ulong b = 0x2E4 ; // 0xDC 0x2E4 0x5b0
            return Memory.ReadValue<Matrix4x4>(Matrix + b);
        }

        public ulong GetComponent(ulong obj, string s)
        {
            ulong comps = Memory.ReadPtr(p + 0x30);
            for (ulong i = 0x8; i < 0x1000; i += 0x10)
            {
                var fields = Memory.ReadPtr(Memory.ReadPtr(comps + i) + 0x28);
                try
                {
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
