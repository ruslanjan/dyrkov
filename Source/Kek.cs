using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;

namespace eft_dma_radar.Source
{

    public partial class Kek : Form
    {
        [DllImport("user32.dll")]
        private static extern long SetWindowLongA(IntPtr hWnd, int nIndex, long dwNewLong);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern long GetWindowLongA(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, UInt32 uFlags);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMargins);

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
            get
            {
                var p = Memory.Players?.FirstOrDefault(x => x.Value.Type is PlayerType.LocalPlayer);
                return p?.Value;
            }
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
        public const string WINDOW_NAME = "Escape" + "FromTarkov";
        IntPtr handle = FindWindow(null, WINDOW_NAME);
        RECT rect = new RECT();

        Graphics g;
        Pen pen = new Pen(Color.Red);
        Pen gpen = new Pen(Color.Green);
        Pen dpen = new Pen(Color.Brown);
        private readonly Stopwatch _fpsWatch = new();
        private int _fps = 0; // temp
        private int fps = 0;
        public readonly Config _config;
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
        public static Kek kek;
        private bool showLoot = true;

        public Kek()
        {
            if (kek is not null)
            {
                throw new Exception("Kek already created");
            }
            _config = Program.Config;
            InitializeComponent();
            kek = this;

        }

        private void Kek_Load(object sender, EventArgs e)
        {
            instance = this;
            //this.BackColor = Color.Wheat;
            //this.TransparencyKey = Color.Wheat;
            //this.Size = new Size(2560, 1440);
            //this.TopMost = true;
            //this.TopLevel = true;
            //this.Focus();
            //this.AllowTransparency = true;
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;

            // register global hook
            // risky!
            //this.RegisterHooks();

            // Canvas
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
            tmr.Interval = 15;   // milliseconds
            tmr.Tick += TmrTick;  // set handler
                                  //tmr.Start();



            Timer ftmr = new Timer();
            ftmr.Interval = 2000;   // milliseconds
            ftmr.Tick += fTmrTick;  // set handler
            //ftmr.Start();

            _fpsWatch.Start();

        }

        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED = 0x80000;
        public const int LWA_ALPHA = 0x2;
        public const int LWA_COLORKEY = 0x1;


        async private void Kek_Shown(object sender, EventArgs e)
        {
            long initialStyle = GetWindowLongA(this.Handle, -20);
            SetWindowLongA(this.Handle, -20, initialStyle | WS_EX_LAYERED | 0x20L | 0x08000000L);
            handle = FindWindow(null, WINDOW_NAME);

            MARGINS marg = new MARGINS() { Left = 0, Right = 0, Top = 2560, Bottom = 1440 };
            DwmExtendFrameIntoClientArea(this.Handle, ref marg);

            SetLayeredWindowAttributes(this.Handle, 0, 255, LWA_ALPHA);

            GetWindowRect(handle, out rect);
            this.Size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
            this.Top = rect.Top;
            this.Left = rect.Left;

            while (_canvas.GRContext is null) await Task.Delay(1);
            _canvas.GRContext.SetResourceCacheLimit(503316480); // Fixes low FPS on big maps
            while (true)
            {
                //await Task.Run(() => Thread.SpinWait(8*50000)); // High performance async delay
                await Task.Run(() => Thread.Sleep(10));

                _canvas.Refresh(); // draw next frame
            }
        }

        [DllImport("user32.dll")]
        static extern ushort GetAsyncKeyState(int vKey);

        public static bool IsKeyPushedDown(Keys vKey)
        {
            return 0 != (GetAsyncKeyState((int)vKey) & 0x8000);
        }

        private bool[] inputMask = new bool[7];
        private void ProcessInput()
        {
            if (LocalPlayer == null)
            {
                return;
            }
            var i = 0;
            if (IsKeyPushedDown(Keys.F2) && inputMask[i])
            {
                inputMask[i] = false;
                Memory.Game.FPSCamera.ToggleThermalVision();
            }
            else if (!IsKeyPushedDown(Keys.F2) && !inputMask[i]) inputMask[i] = true;

            i = 1;
            if (IsKeyPushedDown(Keys.F3) && inputMask[i])
            {
                inputMask[i] = false;
                showLoot = !showLoot;
            }
            else if (!IsKeyPushedDown(Keys.F3) && !inputMask[i]) inputMask[i] = true;

            i = 2;
            if (IsKeyPushedDown(Keys.F4) && inputMask[i])
            {
                inputMask[i] = false;
                LocalPlayer.ToggleMaxStamina(); LocalPlayer.noRecoil = !LocalPlayer.noRecoil;
            }
            else if (!IsKeyPushedDown(Keys.F4) && !inputMask[i]) inputMask[i] = true;

            i = 3;
            if (IsKeyPushedDown(Keys.Alt) && IsKeyPushedDown(Keys.F5) && inputMask[i])
            {

                inputMask[i] = false;
                // set window over target
                if (handle.ToInt64() == 0)
                {
                    handle = FindWindow(null, WINDOW_NAME);
                }
                this.BringToFront();
                MARGINS marg = new MARGINS() { Left = 0, Right = 0, Top = 2560, Bottom = 1440 };
                DwmExtendFrameIntoClientArea(this.Handle, ref marg);
            }
            else if (!IsKeyPushedDown(Keys.Alt) && !IsKeyPushedDown(Keys.F5) && !inputMask[i]) inputMask[i] = true;

            i = 4;
            if (IsKeyPushedDown(Keys.F5) && inputMask[i])
            {

                inputMask[i] = false;
                if (LocalPlayer is not null)
                    LocalPlayer.IsScope = !LocalPlayer.IsScope;
            }
            else if (!IsKeyPushedDown(Keys.F5) && !inputMask[i]) inputMask[i] = true;

            i = 5;
            if (IsKeyPushedDown(Keys.Z))
            {
                if (LocalPlayer is not null)
                    LocalPlayer.kekBotOn = true;
            }
            else if (!IsKeyPushedDown(Keys.Z))
            {
                if (LocalPlayer is not null)
                    LocalPlayer.kekBotOn = false;
            }

            i = 6;
            if (IsKeyPushedDown(Keys.H) && inputMask[i])
            {

                inputMask[i] = false;
                if (LocalPlayer is not null)
                    LocalPlayer.kekBotBoneIdx++;
            }
            else if (!IsKeyPushedDown(Keys.H) && !inputMask[i]) inputMask[i] = true;
        }


        private void TmrTick(object sender, EventArgs e)  //run this logic each timer tick
        {
            //this.Invalidate();  // move image across screen, picture box is control so no repaint needed
            // set window over target
            //this.BringToFront();

        }

        private void fTmrTick(object sender, EventArgs e)  //run this logic each timer tick
        {

        }

        private new void BringToFront()
        {
            if (handle.ToInt64() == 0)
            {
                handle = FindWindow(null, WINDOW_NAME);
            }
            IntPtr windowOverTarget = GetWindow(handle, GetWindowType.GW_HWNDPREV);
            SetWindowPos(this.Handle, windowOverTarget, 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0010); // SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE
        }

        private void Canvas_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            SKSurface surface = e.Surface;
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            if (_fpsWatch.ElapsedMilliseconds >= 1000)
            {
                this.BringToFront();
                _canvas.GRContext.PurgeResources(); // Seems to fix mem leak issue on increasing resource 
                _fpsWatch.Restart();
                fps = _fps;
                _fps = 0;

            }
            else _fps++;
            this.DrawHud(canvas);
            ProcessInput();
            try
            {
                //this.BringToFront();
                if (Memory.InGame && Memory.Players != null)
                {
                    var players = AllPlayers.Select(p => p.Value);
                    var sourcePlayer = LocalPlayer;
                    var view_matrix = Memory.Game.ViewMatrix;
                    var view_optic_matrix = Memory.Game.ViewOpticMatrix;
                    if (players is null || sourcePlayer is null)
                    {
                        return;
                    }

                    if (sourcePlayer.IsAiming && sourcePlayer.IsScope)
                        view_matrix = view_optic_matrix;

                    try
                    {
                        if (Memory.Exfils != null)
                        {
                            DrawExfils(canvas, view_matrix, sourcePlayer);
                        }
                        if (Memory.Loot != null && Memory.Loot.Filter != null && showLoot)
                        {
                            this.DrawLoot(canvas, Memory.Loot.Filter, view_matrix, sourcePlayer);
                        }

                        if (sourcePlayer is not null && sourcePlayer.IsActive && sourcePlayer.IsAlive)
                        {
                            if (players is not null)
                            {
                                foreach (var player in players)
                                {
                                    try
                                    {
                                        if (player.Type == PlayerType.LocalPlayer)
                                        {
                                           continue; // don't draw self
                                        }
                                        this.DrawPlayerKek(canvas, player, view_matrix, sourcePlayer);
                                    }
                                    catch { }
                                }
                            }
                        }

                        if (Memory.Grenades != null)
                        {
                            this.DrawGrenades(canvas, Memory.Grenades, view_matrix, sourcePlayer);
                        }
                    }
                    catch { }
                }
            }
            catch { }
            if (Memory.InGame && Memory.Players != null)
                this.drawCrosshair(canvas);
        }

        private void DrawHud(SKCanvas canvas)
        {
            canvas.DrawText($"FPS: {fps}", 10, 20, SKPaints.TextImportantLoot);
            if (LocalPlayer is not null)
            {
                var i = 1;
                var LineHeight = 20;
                canvas.DrawText($"IsScope?: {LocalPlayer.IsScope}", 10, 20 + i * LineHeight + 5, SKPaints.TextImportantLoot);
                i++;
                canvas.DrawText($"IsNoRecoil?: {LocalPlayer.noRecoil}", 10, 20 + i * LineHeight + 5, SKPaints.TextImportantLoot);
                i++;
                canvas.DrawText($"kekBotOn?: {LocalPlayer.kekBotOn}", 10, 20 + i * LineHeight + 5, SKPaints.TextImportantLoot);
                i++;
                canvas.DrawText($"kekBotBone?: {LocalPlayer.kekBotBone.ToString()}", 10, 20 + i * LineHeight + 5, SKPaints.TextImportantLoot);

            }
        }

        private void drawCrosshair(SKCanvas canvas)
        {
            var m = new Vector2(this.Width / 2, this.Height / 2);
            canvas.DrawLine(m.X - 5, m.Y, m.X + 5, m.Y, SKPaints.Crosshair);
            canvas.DrawLine(m.X, m.Y - 5, m.X, m.Y + 5, SKPaints.Crosshair);
        }
        public void DrawGrenades(SKCanvas canvas, ReadOnlyCollection<Grenade> items, Matrix4x4 view_matrix, Player sourcePlayer)
        {
            foreach (var item in items)
            {
                SKPaint paint = SKPaints.PaintGrenades;
                SKPaint text = SKPaints.TextLoot;

                var itemPos = item.Position;
                float dist = Vector3.Distance(sourcePlayer.Position, itemPos);
                if (dist > 100f)
                    continue;
                Vector2 pos;
                if (!w2s(view_matrix, new Vector3(itemPos.X, itemPos.Z, itemPos.Y), out pos))
                {
                    continue;
                }
                canvas.DrawText($"{(int)dist}", pos.X, pos.Y + 17, text);
                canvas.DrawCircle(pos.X, pos.Y, 3, paint);
            }
        }

        public void DrawLoot(SKCanvas canvas, ReadOnlyCollection<LootItem> items, Matrix4x4 view_matrix, Player sourcePlayer)
        {
            List<Tuple<LootItem, Vector2, float>> points = new List<Tuple<LootItem, Vector2, float>>();
            List<Tuple<LootItem, float>> sorted = new List<Tuple<LootItem, float>>();
            foreach (var item in items)
            {
                var itemPos = item.Position;
                float dist = Vector3.Distance(sourcePlayer.Position, itemPos);
                sorted.Add(Tuple.Create(item, dist));
            }
            sorted.Sort((a, b) => a.Item2 == b.Item2 ? 0 : (a.Item2 > b.Item2 ? 1 : -1));

            int Yfixer = 0;
            foreach (var itemSorted in sorted)
            {
                var item = itemSorted.Item1;
                var important = item.isImportant(_config.MinImportantLootValue, _config.MinImportantLootValuePerSlot);
                SKPaint paint = important ? SKPaints.PaintImportantLoot : SKPaints.PaintLoot;
                SKPaint text = important ? SKPaints.TextImportantLoot : SKPaints.TextLootBox;

                var itemPos = item.Position;
                float dist = itemSorted.Item2;
                var itemCollapse = sourcePlayer.kekBotOn;
                if (dist > 50f && !important)
                {
                    if (dist < 150f)
                        text = SKPaints.TextCloseLootBox;
                    else
                        text = SKPaints.TextFarLootBox;
                    if (dist > 200f)
                    {
                        continue;
                    }
                }
                Vector2 pos;
                if (!w2s(view_matrix, new Vector3(itemPos.X, itemPos.Z, itemPos.Y), out pos))
                {
                    continue;
                }
                

                if (itemCollapse)
                {
                    Yfixer += 11;
                    pos.Y += Yfixer;
                }
                if (pos.X > this.Width || pos.X < 0 || pos.Y > this.Height || pos.Y < 0)
                {
                    continue;
                }
                if (Vector2.Distance(new Vector2(this.Width / 2, this.Height / 2), pos) < 50)
                {
                    points.Add(new Tuple<LootItem, Vector2, float>(item, pos, dist));
                }
                else
                {
                    canvas.DrawText($"{item.Label} : {(int)dist}", pos.X, pos.Y, text);
                }
            }
            if (points.Count == 0)
            {
                return;
            }
            points.Sort((a, b) =>
            {
                if (a.Item2.Y < b.Item2.Y)
                {
                    return -1;
                }
                if (a.Item2.Y > b.Item2.Y)
                {
                    return 1;
                }
                {
                    return 0;
                }
            });
            {
                // loot in the center
                points.Sort((a, b) => a.Item3 == b.Item3 ? 0 : (a.Item3 < b.Item3 ? -1 : 1));
                Vector2 pos = points[0].Item2;
                var LineHeight = 16;
                canvas.DrawRect(pos.X, pos.Y - 10, LineHeight * (points.Select(p => p.Item1.Label.Length).Max() + 6), 18 * points.Count, SKPaints.DarkTextbg);
                foreach (var i in points)
                {
                    var item = i.Item1;
                    var important = item.isImportant(_config.MinImportantLootValue, _config.MinImportantLootValuePerSlot);
                    SKPaint text = important ? SKPaints.TextImportantLoot : SKPaints.TextLootBox;
                    var itemPos = item.Position;
                    float dist = i.Item3;
                    if (dist > 150f && !important)
                    {
                        if (dist < 250f)
                            text = SKPaints.TextCloseLootBox;
                        else
                            text = SKPaints.TextFarLootBox;
                    }

                    canvas.DrawText($"{item.Label} | {(int)dist}", pos.X, pos.Y, text);
                    pos.Y += 18;
                }

            }
        }

        public void DrawExfils(SKCanvas canvas, Matrix4x4 view_matrix, Player sourcePlayer)
        {
            var exfils = Memory.Game.Exfils;
            foreach (var exfil in exfils.Reverse())
            {
                SKPaint paint = exfil.Status == ExfilStatus.Open ? SKPaints.PaintExfilOpenBox : (exfil.Status == ExfilStatus.Pending ? SKPaints.PaintExfilPendingBox : SKPaints.PaintExfilClosedBox);
                
                var exfilPos = exfil.Position;
                float dist = Vector3.Distance(sourcePlayer.Position, exfilPos);
                Vector2 pos;
                if (!w2s(view_matrix, new Vector3(exfilPos.X, exfilPos.Z, exfilPos.Y), out pos))
                {
                    continue;
                }
                else
                {
                    if (exfil.isScav && exfil.Status != ExfilStatus.Open) {
                        continue;
                    }
                    canvas.DrawText($"{(exfil.isScav ? "ScavEx " : "")}{exfil.name} : {(int)dist}", pos.X, pos.Y, paint);
                }
            }
        }

        public void DrawPlayerKek(SKCanvas canvas, Player player, Matrix4x4 view_matrix, Player sourcePlayer)
        {
            var playerPos = player.Position;
            float dist = Vector3.Distance(sourcePlayer.Position, playerPos);
            SKPaint paint = player.GetKekPaint(dist);
            SKPaint text = player.GetKekText(dist);

            if (player.Type == PlayerType.LocalPlayer) return; // don't draw self
            var headPos = player.getBonePose(Player.bones.HumanHead);
            var spinePos = player.getBonePose(Player.bones.HumanSpine3);
            var pelvisPos = player.getBonePose(Player.bones.HumanPelvis);
            
            var visible = dist < _config.MaxKekDistance;

            //var bone_matrix = Memory.ReadPtrChain(player.Base, Offsets.Player.bone_matrix);
            //var headTransform = Memory.ReadPtr(bone_matrix + 0x20 + (((ulong)133) * 0x8));
            //headPos = new Transform(player.headTransform).GetPosition();

            Vector2 pos;
            if (!w2s(view_matrix, new Vector3(playerPos.X, playerPos.Z, playerPos.Y), out pos))
            {
                return;
            }
            if (pos.X == 0 || pos.Y == 0 || pos.X > this.Width || pos.Y > this.Height)
            {
                return;
            }
            var health = player.IsAlive ? player.Health : 0; 
            canvas.DrawText($"{player.Name} {health} {(int)dist}", pos.X, pos.Y, text);
            var wep = "";
            if (false && player.Gear is not null) // Get weapon info via GearManager
            {
                wep = "None";
                GearItem gearItem = null;
                if (!player.Gear.TryGetValue("FirstPrimaryWeapon", out gearItem))
                    if (!player.Gear.TryGetValue("SecondPrimaryWeapon", out gearItem))
                        player.Gear.TryGetValue("Holster", out gearItem);
                if (gearItem is not null)
                {
                    wep = gearItem.Short; // Get 'short' weapon name/info
                    //canvas.DrawText($"Wep:{wep}", pos.X, pos.Y + 14, text);
                }
            }
            var side = player.isUsec ? "Usec:" : "";
            if (side == "")
                side = player.isBear ? "Bear:" : "";
            canvas.DrawText($"{side}{wep}", pos.X, pos.Y + 14, text);
            if (!player.IsActive || !player.IsAlive)
                return;

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
            canvas.DrawCircle(headScreen.X, headScreen.Y, h / 9, paint);

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
                canvas.DrawRect(spineScreen.X - 1.5f, spineScreen.Y - 1.5f, 3, 3, paint);
            }

            // Pelvis
            Vector2 pelvisScreen;
            if (w2s(view_matrix, new Vector3(pelvisPos.X, pelvisPos.Z, pelvisPos.Y), out pelvisScreen))
            {
                canvas.DrawRect(pelvisScreen.X - 1.5f, pelvisScreen.Y - 1.5f, 3, 3, paint);
            }


            // bounding bugx, sorry
            headPos.Z += 0.2f;
            Vector2 topHeadScreen;
            if (!w2s(view_matrix, new Vector3(headPos.X, headPos.Z, headPos.Y), out topHeadScreen))
            {
                return;
            }
            h = pos.Y - topHeadScreen.Y;

            canvas.DrawRect(topHeadScreen.X - h / 2 / 2, topHeadScreen.Y, h / 2, h, paint);

            if (!visible)
            {
                return;
            }

            var bonesScreen = new Dictionary<Player.bones, Vector2>();
            foreach (var b in Player.TargetBones)
            {
                Vector3 Pos = player.getBonePose(b);
                Vector2 bpos;
                if (!w2s(view_matrix, new Vector3(Pos.X, Pos.Z, Pos.Y), out bpos))
                {
                    continue;
                }
                bonesScreen[b] = bpos;
            }

            //skeleton
            SKPath path = new SKPath();

            path.MoveTo(headScreen.X, headScreen.Y);
            path.LineTo(spineScreen.X, spineScreen.Y);
            path.LineTo(pelvisScreen.X, pelvisScreen.Y);

            path.MoveTo(bonesScreen[Player.bones.HumanLUpperarm].X, bonesScreen[Player.bones.HumanLUpperarm].Y);
            path.LineTo(bonesScreen[Player.bones.HumanLForearm1].X, bonesScreen[Player.bones.HumanLForearm1].Y);
            path.LineTo(bonesScreen[Player.bones.HumanLPalm].X, bonesScreen[Player.bones.HumanLPalm].Y);

            path.MoveTo(bonesScreen[Player.bones.HumanRUpperarm].X, bonesScreen[Player.bones.HumanRUpperarm].Y);
            path.LineTo(bonesScreen[Player.bones.HumanRForearm1].X, bonesScreen[Player.bones.HumanRForearm1].Y);
            path.LineTo(bonesScreen[Player.bones.HumanRPalm].X, bonesScreen[Player.bones.HumanRPalm].Y);

            path.MoveTo(pelvisScreen.X, pelvisScreen.Y);
            path.LineTo(bonesScreen[Player.bones.HumanLCalf].X, bonesScreen[Player.bones.HumanLCalf].Y);
            path.LineTo(bonesScreen[Player.bones.HumanLFoot].X, bonesScreen[Player.bones.HumanLFoot].Y);

            path.MoveTo(pelvisScreen.X, pelvisScreen.Y);
            path.LineTo(bonesScreen[Player.bones.HumanRCalf].X, bonesScreen[Player.bones.HumanRCalf].Y);
            path.LineTo(bonesScreen[Player.bones.HumanRFoot].X, bonesScreen[Player.bones.HumanRFoot].Y);


            canvas.DrawPath(path, paint);

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

            if (LocalPlayer.IsAiming && LocalPlayer.IsScope)
            {
                float angle_rad_half = ((float)Math.PI / 180f) * 35f * 0.5f;
                float angle_ctg = (float)(Math.Cos(angle_rad_half) / Math.Sin(angle_rad_half));

                var aspect_ratio = 16f / 9f;
                x /= angle_ctg * aspect_ratio * 0.5f;
                y /= angle_ctg * 0.5f;
            }



            screen = new Vector2((this.Width / 2) * (1.0f + x / w), ((this.Height / 2) * (1.0f - y / w)));

            return true;
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


        private void Kek_MouseEnter(object sender, EventArgs e)
        {

        }

        internal static void redraw()
        {
            if (instance != null && !Memory.InGame)
            {
                instance.Invalidate();
            }
        }

        private void Kek_KeyPress(object sender, KeyPressEventArgs e)
        {
            //if (e.GetType() == typeof(KeyPressEventArgs))
            Program.Log($"ke pressed{e.ToString()}");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.F1))
            {
                Program.Log("pressed F1");
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

    }
}
