using eft_dma_radar.Source.Tarkov;
using Offsets;
using OpenTK.Input;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static eft_dma_radar.Player;
using static vmmsharp.lc;
using Timer = System.Windows.Forms.Timer;

namespace eft_dma_radar.Source
{
    
    public partial class Kek : Form
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, UInt32 uFlags);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        /// <summary>
        /// Radar has found Escape From Dyrkov process and is ready.
        /// </summary>
        private bool Ready
        {
            get => Memory.Ready;
        }
        /// <summary>
        /// Radar has found Local Game World.
        /// </summary>
        private bool InGame
        {
            get => Memory.InGame;
        }
        /// <summary>
        /// LocalPlayer (who is running Radar) 'Player' object.
        /// </summary>
        private Player LocalPlayer
        {
            get => Memory.Players?.FirstOrDefault(x => x.Value.Type is PlayerType.LocalPlayer).Value;
        }
        /// <summary>
        /// All Players in Local Game World (including dead/exfil'd) 'Player' collection.
        /// </summary>
        private ReadOnlyDictionary<string, Player> AllPlayers
        {
            get => Memory.Players;
        }
        /// <summary>
        /// Contains all loot in Local Game World.
        /// </summary>
        private LootManager Loot
        {
            get => Memory.Loot;
        }
        /// <summary>
        /// Contains all 'Hot' grenades in Local Game World, and their position(s).
        /// </summary>
        private ReadOnlyCollection<Grenade> Grenades
        {
            get => Memory.Grenades;
        }


        // EscapeFromTarkov.exe
        public const string WINDOW_NAME = "EscapeFromTarkov";
        IntPtr handle = FindWindow(null, WINDOW_NAME);
        RECT rect = new RECT();

        Graphics g;
        Pen pen = new Pen(Color.Red);
        Pen gpen = new Pen(Color.Green);
        Pen dpen = new Pen(Color.Brown);
        private readonly Stopwatch _fpsWatch = new();
        private int _fps = 0;
        readonly Config _config;
        private SKGLControl _canvas;
        private object _renderLock = new();

        private Matrix4x4 ViewMatrix
        {
            get
            {
                return Memory.Game.ViewMatrix;
            }
        }

        private static Kek instance;

        public Kek()
        {
            _config = Program.Config;
            InitializeComponent();


        }

        private void Kek_Load(object sender, EventArgs e)
        {
            instance = this;
            this.BackColor = Color.Wheat;
            this.TransparencyKey = Color.Wheat;
            //this.TopMost = true;
            //this.TopLevel = true;
            //this.Focus();
            //this.AllowTransparency = true;
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;

            Kek_Shown(sender, e);

            _canvas = new SKGLControl()
            {
                Size = new Size(50, 50),
                Dock = DockStyle.Fill,
                BackColor = Color.Wheat,
                ForeColor = Color.Wheat,
                VSync = _config.Vsync // cap fps to refresh rate, reduce tearing
            };
            this.Controls.Add(_canvas);
            _canvas.PaintSurface += Canvas_PaintSurface;


            Timer tmr = new Timer();
            tmr.Interval = 10;   // milliseconds
            tmr.Tick += TmrTick;  // set handler
            tmr.Start();



            Timer ftmr = new Timer();
            ftmr.Interval = 2000;   // milliseconds
            ftmr.Tick += fTmrTick;  // set handler
            ftmr.Start();

            _fpsWatch.Start();

        }

        private long ticks = 0;

        private void TmrTick(object sender, EventArgs e)  //run this logic each timer tick
        {
            this.Invalidate();  // move image across screen, picture box is control so no repaint needed

        }

        private void fTmrTick(object sender, EventArgs e)  //run this logic each timer tick
        {
           
        }

        private void Canvas_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            lock (_renderLock) // Acquire lock on 'Render Resources'
            {
                if (_fpsWatch.ElapsedMilliseconds >= 1000)
                {
                    _canvas.GRContext.PurgeResources(); // Seems to fix mem leak issue on increasing resource 
                    _fpsWatch.Restart();
                    _fps = 0;
                }
                else _fps++;
                SKSurface surface = e.Surface;
                SKCanvas canvas = surface.Canvas;
                canvas.Clear(SKColors.Wheat);
                try
                {

                }
                catch { }
            }
        }
        public void DrawPlayerKek(SKCanvas canvas, Player player, Matrix4x4 view_matrix, Player source_player)
        {
            var radians = player.Rotation.X.ToRadians();
            SKPaint paint = player.GetPaint();

            if (player.Type == PlayerType.LocalPlayer) continue; // don't draw self
            var playerPos = player.Position;
            var headPos = player.HeadPos;
            var spinePos = player.SpinePos;
            var pelvisPos = player.PelvisPos;

            float dist = Vector3.Distance(sourcePlayer.Position, playerPos);

            var bone_matrix = Memory.ReadPtrChain(player.Base, Offsets.Player.bone_matrix);
            var headTransform = Memory.ReadPtr(bone_matrix + 0x20 + (((ulong)133) * 0x8));
            headPos = new Transform(player.headTransform).GetPosition();

            Vector2 pos;
            if (!w2s(view_matrix, new Vector3(playerPos.X, playerPos.Z, playerPos.Y), out pos))
            {
                return;
            }
            if (pos.X == 0 || pos.Y == 0 || pos.X > this.Width || pos.Y > this.Height)
            {
                return;
            }
            canvas.DrawRect(player.Name, drawFont, drawBrush, pos.X, pos.Y);

            // Base - Head height
            Vector2 headScreen;
            if (!w2s(view_matrix, new Vector3(headPos.X, headPos.Z, headPos.Y), out headScreen))
            {
                return;
            }
            canvas.DrawRect(headScreen.X, headScreen.Y, 3, 3, paint);
            var h = pos.Y - headScreen.Y;
            h *= 0.7f;
            if (h < 0)
                h *= -1;

            h = pos.Y - headScreen.Y;
            canvas.DrawRect(hpen, headScreen.X - h / 8 / 2, headScreen.Y - h / 8 / 2, h / 8, h / 8, paint);

            // debug
            if (false)
            {
                // Lfoot
                /*headTransform = Memory.ReadPtr(bone_matrix + 0x20 + (((ulong)94) * 0x8));
                p = new Transform(Memory.ReadPtr(headTransform + 0x10)).GetPosition();
                if (w2s(view_matrix, new Vector3(p.X, p.Z, p.Y), out headScreen))
                {
                    g.DrawRectangle(hpen, headScreen.X - h / 8 / 2, headScreen.Y - h / 8 / 2, h / 8, h / 8);
                }*/
            }

            // Spine
            Vector2 spineScreen;
            if (w2s(view_matrix, new Vector3(spinePos.X, spinePos.Z, spinePos.Y), out spineScreen))
            {
                canvas.DrawRect(spineScreen.X - h / 8 / 2, spineScreen.Y - h / 8 / 2, h / 8, h / 8, paint);
            }

            // Pelvis
            Vector2 pelvisScreen;
            if (w2s(view_matrix, new Vector3(pelvisPos.X, pelvisPos.Z, pelvisPos.Y), out pelvisScreen))
            {
                canvas.DrawRect(pelvisScreen.X - h / 8 / 2, pelvisScreen.Y - h / 8 / 2, h / 8, h / 8, paint);
            }


            // bounding bugx, sorry
            headPos.Z += 0.2f;
            if (!w2s(view_matrix, new Vector3(headPos.X, headPos.Z, headPos.Y), out headScreen))
            {
                return;
            }
            h = pos.Y - headScreen.Y;
            
            canvas.DrawRect(headScreen.X - h / 2 / 2, headScreen.Y, h / 2, h, paint);
        }

        void Kek_Paint(object sender, PaintEventArgs e)
        {


            //g = e.Graphics;
            Program.Log($"draw_kek");
            //g.DrawRectangle(pen, 50, 50, 150, 150);
            //g.Clear(Color.Wheat);
            //draw_kek();
            //Invalidate();
        }

        bool w2s(Matrix4x4 view_matrix, Vector3 pos, out Vector2 screen)
        {
            //Program.Log($"view_matrix{view_matrix.ToString()}");
            Vector3 transform = new Vector3(view_matrix.M14, view_matrix.M24, view_matrix.M34);
            Vector3 right = new Vector3(view_matrix.M11, view_matrix.M21, view_matrix.M31);
            Vector3 up = new Vector3(view_matrix.M12, view_matrix.M22, view_matrix.M32);

            float w = Vector3.Dot(transform, pos) + view_matrix.M44;

            if (w < 0.099f)
            {
                screen = new Vector2();
                return false;
            }

            float x = Vector3.Dot(right, pos) + view_matrix.M41;
            float y = Vector3.Dot(up, pos) + view_matrix.M42;

            screen = new Vector2((this.Width / 2) * (1.0f + x / w), ((this.Height / 2) * (1.0f - y / w)));

            return true;
        }

        Font drawFont = new Font("Arial", 7);
        SolidBrush drawBrush = new SolidBrush(Color.Red);
        SolidBrush lootDrawBrush = new SolidBrush(Color.White);
        SolidBrush epicLootDrawBrush = new SolidBrush(Color.SkyBlue);

        Pen hpen = new Pen(Color.Yellow);
        void draw_kek()
        {

            if (Memory.InGame && Memory.Players != null)
            {
                if (handle.ToInt64() == 0)
                {
                    handle = FindWindow(null, WINDOW_NAME);
                }
                IntPtr windowOverTarget = GetWindow(handle, GetWindowType.GW_HWNDPREV);
                SetWindowPos(this.Handle, windowOverTarget, 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0010); // SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE
                g.DrawRectangle(gpen, this.Width / 2 - 5, this.Height / 2 - 5, 10, 10);
                var players = Memory.Game.Players.ToList().Select(p => p.Value);
                if (players is null || players.ToList().Where(p => p.Type == PlayerType.LocalPlayer).ToList().Count == 0)
                {
                    return;
                }

                try
                {
                    var sourcePlayer = LocalPlayer;
                    var camera = Memory.Game.FPSCamera;
                    var view_matrix = ViewMatrix;
                    if (sourcePlayer is not null && sourcePlayer.IsActive && sourcePlayer.IsAlive)
                    {
                        if (players is not null)
                        {
                            foreach (var player in players)
                            {
                                if (player.Type == PlayerType.LocalPlayer) continue; // don't draw self
                                var playerPos = player.Position;
                                var headPos = player.HeadPos;
                                var spinePos = player.SpinePos;
                                var pelvisPos = player.PelvisPos;
                                float dist = Vector3.Distance(sourcePlayer.Position, playerPos);

                                var bone_matrix = Memory.ReadPtrChain(player.Base, Offsets.Player.bone_matrix);
                                var headTransform = Memory.ReadPtr(bone_matrix + 0x20 + (((ulong)133) * 0x8));
                                headPos = new Transform(player.headTransform).GetPosition();

                                Vector2 pos;
                                if (!w2s(view_matrix, new Vector3(playerPos.X, playerPos.Z, playerPos.Y), out pos))
                                {
                                    continue;
                                }
                                if (pos.X == 0 || pos.Y == 0 || pos.X > this.Width || pos.Y > this.Height)
                                {
                                    continue;
                                }
                                g.DrawString(player.Name, drawFont, drawBrush, pos.X, pos.Y);

                                // Base - Head height
                                Vector2 headScreen;
                                if (!w2s(view_matrix, new Vector3(headPos.X, headPos.Z, headPos.Y), out headScreen))
                                {
                                    continue;
                                }
                                g.DrawRectangle(hpen, headScreen.X, headScreen.Y, 3, 3);
                                var h = pos.Y - headScreen.Y;
                                h *= 0.7f;
                                if (h < 0)
                                    h *= -1;

                                h = pos.Y - headScreen.Y;
                                g.DrawRectangle(hpen, headScreen.X - h / 8 / 2, headScreen.Y - h / 8 / 2, h / 8, h / 8);

                                // debug
                                if (false)
                                {
                                    // Lfoot
                                    /*headTransform = Memory.ReadPtr(bone_matrix + 0x20 + (((ulong)94) * 0x8));
                                    p = new Transform(Memory.ReadPtr(headTransform + 0x10)).GetPosition();
                                    if (w2s(view_matrix, new Vector3(p.X, p.Z, p.Y), out headScreen))
                                    {
                                        g.DrawRectangle(hpen, headScreen.X - h / 8 / 2, headScreen.Y - h / 8 / 2, h / 8, h / 8);
                                    }*/
                                }

                                // Spine
                                Vector2 spineScreen;
                                if (w2s(view_matrix, new Vector3(spinePos.X, spinePos.Z, spinePos.Y), out spineScreen))
                                {
                                    g.DrawRectangle(hpen, spineScreen.X - h / 8 / 2, spineScreen.Y - h / 8 / 2, h / 8, h / 8);
                                }

                                // Pelvis
                                Vector2 pelvisScreen;
                                if (w2s(view_matrix, new Vector3(pelvisPos.X, pelvisPos.Z, pelvisPos.Y), out pelvisScreen))
                                {
                                    g.DrawRectangle(hpen, pelvisScreen.X - h / 8 / 2, pelvisScreen.Y - h / 8 / 2, h / 8, h / 8);
                                }


                                // bounding bugx, sorry
                                headPos.Z += 0.2f;
                                if (!w2s(view_matrix, new Vector3(headPos.X, headPos.Z, headPos.Y), out headScreen))
                                {
                                    continue;
                                }
                                h = pos.Y - headScreen.Y;
                                g.DrawRectangle(dpen, headScreen.X - h / 2 / 2, headScreen.Y, h / 2, h);
                                //g.DrawRectangle(dpen, Math.Min(ps.X, pos.X), Math.Min(ps.Y, pos.Y), (Math.Max(ps.X, pos.X) - Math.Min(ps.X, pos.X)) * 2, Math.Max(ps.Y, pos.Y) - Math.Min(ps.Y, pos.Y));

                                //g.DrawRectangle(dpen, ps.X - 0.2f * (1.0f / h)/2.0f, ps.Y, 0.2f * (1.0f / h), h);

                            }
                        }
                        if (Memory.Loot != null && Memory.Loot.Filter != null && _config.LootEnabled && false)
                        {
                            foreach (var item in Memory.Loot.Filter)
                            {
                                var position = item.Position;
                                Vector2 pos;
                                if (!w2s(view_matrix, new Vector3(position.X, position.Z, position.Y), out pos))
                                {
                                    continue;
                                }
                                if (!item.Important && item.Item.avg24hPrice < 80000)
                                    g.DrawString(item.Label, drawFont, lootDrawBrush, pos.X, pos.Y);
                                else
                                    g.DrawString(item.Label, drawFont, epicLootDrawBrush, pos.X, pos.Y);
                            }
                        }

                        // draw crosshair at end

                    }
                }
                catch { }
                //g.DrawRectangle(pen, 50, 50, 150, 150);
            }
        }
        

        private enum GetWindowType : uint
        {
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is highest in the Z order.
            /// <para/>
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDFIRST = 0,
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is lowest in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDLAST = 1,
            /// <summary>
            /// The retrieved handle identifies the window below the specified window in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDNEXT = 2,
            /// <summary>
            /// The retrieved handle identifies the window above the specified window in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDPREV = 3,
            /// <summary>
            /// The retrieved handle identifies the specified window's owner window, if any.
            /// </summary>
            GW_OWNER = 4,
            /// <summary>
            /// The retrieved handle identifies the child window at the top of the Z order,
            /// if the specified window is a parent window; otherwise, the retrieved handle is NULL.
            /// The function examines only child windows of the specified window. It does not examine descendant windows.
            /// </summary>
            GW_CHILD = 5,
            /// <summary>
            /// The retrieved handle identifies the enabled popup window owned by the specified window (the
            /// search uses the first such window found using GW_HWNDNEXT); otherwise, if there are no enabled
            /// popup windows, the retrieved handle is that of the specified window.
            /// </summary>
            GW_ENABLEDPOPUP = 6
        }

        private void Kek_Shown(object sender, EventArgs e)
        {
            int initialStyle = GetWindowLong(this.Handle, -20);
            SetWindowLong(this.Handle, -20, initialStyle | 0x80000 | 0x20);
            handle = FindWindow(null, WINDOW_NAME);

            GetWindowRect(handle, out rect);
            this.Size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
            this.Top = rect.Top;
            this.Left = rect.Left;
        }

        private void Kek_MouseEnter(object sender, EventArgs e)
        {
            
        }

        internal static void redraw()
        {
            if (instance != null && Memory.InGame)
            {
                instance.Invalidate();
            }
        }

        private void Kek_KeyPress(object sender, KeyPressEventArgs e)
        {
            //if (e.GetType() == typeof(KeyPressEventArgs))
        }
    }
}
