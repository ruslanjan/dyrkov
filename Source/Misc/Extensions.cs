﻿using SkiaSharp;
using System.Collections.Generic;

namespace eft_dma_radar
{
    /// <summary>
    /// Extension methods go here.
    /// </summary>
    public static class Extensions
    {
        #region Generic Extensions
        /// <summary>
        /// Restarts a timer from 0. (Timer will be started if not already running)
        /// </summary>
        public static void Restart(this System.Timers.Timer t)
        {
            t.Stop();
            t.Start();
        }

        /// <summary>
        /// Converts 'Degrees' to 'Radians'.
        /// </summary>
        public static double ToRadians(this float degrees)
        {
            return (Math.PI / 180) * degrees;
        }
        /// <summary>
        /// Converts 'Radians' to 'Degrees'.
        /// </summary>
        public static double ToDegrees(this float radians)
        {
            return (180 / Math.PI) * radians;
        }
        /// <summary>
        /// Converts 'Degrees' to 'Radians'.
        /// </summary>
        public static double ToRadians(this double degrees)
        {
            return (Math.PI / 180) * degrees;
        }
        /// <summary>
        /// Converts 'Radians' to 'Degrees'.
        /// </summary>
        public static double ToDegrees(this double radians)
        {
            return (180 / Math.PI) * radians;
        }
        #endregion

        #region GUI Extensions
        
        
        /// <summary>
        /// Convert game position to 'Bitmap' Map Position coordinates.
        /// </summary>
        public static MapPosition ToMapPos(this System.Numerics.Vector3 vector, Map map)
        {
            var X = (Math.Cos(map.ConfigFile.R) * vector.X - Math.Sin(map.ConfigFile.R) * vector.Y) * map.ConfigFile.Scale;
            var Y = (Math.Sin(map.ConfigFile.R) * vector.X + Math.Cos(map.ConfigFile.R) * vector.Y) * map.ConfigFile.Scale; // Invert 'Y' unity 0,0 bottom left, C# top left
            X = map.ConfigFile.X + X;
            Y = map.ConfigFile.Y - Y;
            return new MapPosition()
            {
                X = (float)X, Y = (float)Y,
                Height = vector.Z // Keep as float, calculation done later
            };
        }
        public static double ToMapRad(this double r, Map map)
        {
            return r - map.ConfigFile.R;
        }
        /// <summary>
        /// Gets 'Zoomed' map position coordinates.
        /// </summary>
        public static MapPosition ToZoomedPos(this MapPosition location, MapParameters mapParams)
        {
            return new MapPosition()
            {
                UIScale = mapParams.UIScale,
                X = (location.X - mapParams.Bounds.Left) * mapParams.XScale,
                Y = (location.Y - mapParams.Bounds.Top) * mapParams.YScale,
                Height = location.Height
            };
        }
        /// <summary>
        /// Gets drawing paintbrush based on Player Type.
        /// </summary>
        public static SKPaint GetPaint(this Player player)
        {
            switch (player.Type)
            {
                case PlayerType.LocalPlayer:
                    return SKPaints.PaintLocalPlayer;
                case PlayerType.Teammate:
                    return SKPaints.PaintTeammate;
                case PlayerType.PMC:
                    return SKPaints.PaintPMC;
                case PlayerType.AIScav:
                    return SKPaints.PaintScav;
                case PlayerType.AIRaider:
                    return SKPaints.PaintRaider;
                case PlayerType.AIBoss:
                    return SKPaints.PaintBoss;
                case PlayerType.PScav:
                    return SKPaints.PaintPScav;
                case PlayerType.SpecialPlayer:
                    return SKPaints.PaintSpecial;
                default:
                    return SKPaints.PaintPMC;
            }
        }

        public static SKPaint GetKekPaint(this Player player, float dist)
        {
            var close = dist < 250f;
            switch (player.Type)
            {
                case PlayerType.LocalPlayer:
                    return SKPaints.PaintLocalPlayer;
                case PlayerType.Teammate:
                    return SKPaints.PaintTeammate;
                case PlayerType.PMC:
                    return close ? SKPaints.PaintPMCBox : SKPaints.PaintFarPMCBox;
                case PlayerType.AIScav:
                    return close ? SKPaints.PaintScavBox : SKPaints.PaintFarScavBox;
                case PlayerType.AIRaider:
                    return SKPaints.PaintRaiderBox;
                case PlayerType.AIBoss:
                    return SKPaints.PaintBossBox;
                case PlayerType.PScav:
                    return close ? SKPaints.PaintPScavBox : SKPaints.PaintFarPScavBox;
                case PlayerType.SpecialPlayer:
                    return SKPaints.PaintSpecial;
                default:
                    return SKPaints.PaintPMCBox;
            }
        }
        /// <summary>
        /// Gets text paintbrush based on Player Type.
        /// </summary>
        public static SKPaint GetText(this Player player)
        {
            switch (player.Type)
            {
                case PlayerType.Teammate:
                    return SKPaints.TextTeammate;
                case PlayerType.PMC:
                    return SKPaints.TextPMC;
                case PlayerType.AIScav:
                    return SKPaints.TextScav;
                case PlayerType.AIRaider:
                    return SKPaints.TextRaider;
                case PlayerType.AIBoss:
                    return SKPaints.TextBoss;
                case PlayerType.PScav:
                    return SKPaints.TextWhite;
                case PlayerType.SpecialPlayer:
                    return SKPaints.TextSpecial;
                default:
                    return SKPaints.TextPMC;
            }
           
        }

        public static SKPaint GetKekText(this Player player, float dist)
        {
            var close = dist < 250f;
            switch (player.Type)
            {
                case PlayerType.Teammate:
                    return SKPaints.TextTeammate;
                case PlayerType.PMC:
                    return close ? SKPaints.TextPMCBox : SKPaints.TextFarPMCBox;
                case PlayerType.AIScav:
                    return close ? SKPaints.TextScavBox : SKPaints.TextFarScavBox;
                case PlayerType.AIRaider:
                    return SKPaints.TextRaiderBox;
                case PlayerType.AIBoss:
                    return SKPaints.TextBossBox;
                case PlayerType.PScav:
                    return close ? SKPaints.TextPScavBox : SKPaints.TextFarPScavBox;
                case PlayerType.SpecialPlayer:
                    return SKPaints.TextSpecial;
                default:
                    return SKPaints.TextPMCBox;
            }
        }

        /// <summary>
        /// Gets Aimview drawing paintbrush based on Player Type.
        /// </summary>
        public static SKPaint GetAimviewPaint(this Player player)
        {
            switch (player.Type)
            {
                case PlayerType.LocalPlayer:
                    return SKPaints.PaintAimviewLocalPlayer;
                case PlayerType.Teammate:
                    return SKPaints.PaintAimviewTeammate;
                case PlayerType.PMC:
                    return SKPaints.PaintAimviewPMC;
                case PlayerType.AIScav:
                    return SKPaints.PaintAimviewScav;
                case PlayerType.AIRaider:
                    return SKPaints.PaintAimviewRaider;
                case PlayerType.AIBoss:
                    return SKPaints.PaintAimviewBoss;
                case PlayerType.PScav:
                    return SKPaints.PaintAimviewPScav;
                case PlayerType.SpecialPlayer:
                    return SKPaints.PaintAimviewSpecial;
                default:
                    return SKPaints.PaintAimviewPMC;
            }
        }

        /// <summary>
        /// Get Exfil drawing paintbrush based on status.
        /// </summary>
        public static SKPaint GetPaint(this ExfilStatus status)
        {
            switch (status)
            {
                case ExfilStatus.Open:
                    return SKPaints.PaintExfilOpen;
                case ExfilStatus.Pending:
                    return SKPaints.PaintExfilPending;
                case ExfilStatus.Closed:
                    return SKPaints.PaintExfilClosed;
                default:
                    return SKPaints.PaintExfilClosed;
            }
        }
        #endregion

        #region Custom EFT Extensions

        public static string[] Friends =
        {
            "AligatorTraktor",
            "TRANS_SVITOR",
            "LordDudets",
            "n0081kk",
            "_DED__",
            "KYBER_DED"
        };

        public static AIRole GetRole(this WildSpawnType type)
        {
            switch (type)
            {
                case WildSpawnType.marksman:
                    return new AIRole()
                    {
                        Name = "Sniper",
                        Type = PlayerType.AIScav
                    };
                case WildSpawnType.assault:
                    return new AIRole()
                    {
                        Name = "Scav",
                        Type = PlayerType.AIScav
                    };
                case WildSpawnType.bossTest:
                    return new AIRole()
                    {
                        Name = "bossTest",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.bossBully:
                    return new AIRole()
                    {
                        Name = "Reshala",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.followerTest:
                    return new AIRole()
                    {
                        Name = "followerTest",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.followerBully:
                    return new AIRole()
                    {
                        Name = "Guard",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.bossKilla:
                    return new AIRole()
                    {
                        Name = "Killa",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.bossKojaniy:
                    return new AIRole()
                    {
                        Name = "Shturman",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.followerKojaniy:
                    return new AIRole()
                    {
                        Name = "Guard",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.pmcBot:
                    return new AIRole()
                    {
                        Name = "Raider",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.cursedAssault:
                    return new AIRole()
                    {
                        Name = "Scav",
                        Type = PlayerType.AIScav
                    };
                case WildSpawnType.bossGluhar:
                    return new AIRole()
                    {
                        Name = "Gluhar",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.followerGluharAssault:
                    return new AIRole()
                    {
                        Name = "Assault",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.followerGluharSecurity:
                    return new AIRole()
                    {
                        Name = "Security",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.followerGluharScout:
                    return new AIRole()
                    {
                        Name = "Scout",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.followerGluharSnipe:
                    return new AIRole()
                    {
                        Name = "Sniper",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.followerSanitar:
                    return new AIRole()
                    {
                        Name = "Guard",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.bossSanitar:
                    return new AIRole()
                    {
                        Name = "Sanitar",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.test:
                    return new AIRole()
                    {
                        Name = "test",
                        Type = PlayerType.AIScav
                    };
                case WildSpawnType.assaultGroup:
                    return new AIRole()
                    {
                        Name = "assaultGroup",
                        Type = PlayerType.AIScav
                    };
                case WildSpawnType.sectantWarrior:
                    return new AIRole()
                    {
                        Name = "Cultist",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.sectantPriest:
                    return new AIRole()
                    {
                        Name = "Priest",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.bossTagilla:
                    return new AIRole()
                    {
                        Name = "Tagilla",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.followerTagilla:
                    return new AIRole()
                    {
                        Name = "Guard",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.exUsec:
                    return new AIRole()
                    {
                        Name = "Rogue",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.gifter:
                    return new AIRole()
                    {
                        Name = "SANTA",
                        Type = PlayerType.AIScav
                    };
                case WildSpawnType.bossKnight:
                    return new AIRole()
                    {
                        Name = "Knight",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.followerBigPipe:
                    return new AIRole()
                    {
                        Name = "BigPipe",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.followerBirdEye:
                    return new AIRole()
                    {
                        Name = "BirdEye",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.bossZryachiy:
                    return new AIRole()
                    {
                        Name = "Zryachiy",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.followerZryachiy:
                    return new AIRole()
                    {
                        Name = "followerZryachiy",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.bossBoar:
                    return new AIRole()
                    {
                        Name = "bossBoar",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.followerBoar:
                    return new AIRole()
                    {
                        Name = "followerBoar",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.arenaFighter:
                    return new AIRole()
                    {
                        Name = "arenaFighter",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.arenaFighterEvent:
                    return new AIRole()
                    {
                        Name = "arenaFighterEvent",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.bossBoarSniper:
                    return new AIRole()
                    {
                        Name = "bossBoarSniper",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.crazyAssaultEvent:
                    return new AIRole()
                    {
                        Name = "crazyAssaultEvent",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.peacefullZryachiyEvent:
                    return new AIRole()
                    {
                        Name = "peacefullZryachiyEvent",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.sectactPriestEvent:
                    return new AIRole()
                    {
                        Name = "sectactPriestEvent",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.ravangeZryachiyEvent:
                    return new AIRole()
                    {
                        Name = "ravangeZryachiyEvent",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.followerBoarClose1:
                    return new AIRole()
                    {
                        Name = "followerBoarClose1",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.followerBoarClose2:
                    return new AIRole()
                    {
                        Name = "followerBoarClose2",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.bossKolontay:
                    return new AIRole()
                    {
                        Name = "bossKolontay",
                        Type = PlayerType.AIBoss
                    };
                case WildSpawnType.followerKolontayAssault:
                    return new AIRole()
                    {
                        Name = "followerKolontayAssault",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.followerKolontaySecurity:
                    return new AIRole()
                    {
                        Name = "followerKolontaySecurity",
                        Type = PlayerType.AIRaider
                    };
                case WildSpawnType.shooterBTR:
                    return new AIRole()
                    {
                        Name = "shooterBTR",
                        Type = PlayerType.AIRaider
                    };
                default:
                    return new AIRole()
                    {
                        Name = "uknown",
                        Type = PlayerType.AIScav
                    };
                    Program.Log("Uknown player " + type);
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion
    }
}
