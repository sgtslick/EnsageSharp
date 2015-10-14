namespace SFHelper
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Drawing;
    using System.Linq;

    using Ensage;
    using Ensage.Common;
    using Ensage.Common.Extensions;

    using SharpDX;
    using SharpDX.Direct3D9;

    using Color = SharpDX.Color;
    using Font = SharpDX.Direct3D9.Font;

    internal class SF
    {

        #region Static Fields

        private static readonly Dictionary<Unit, ParticleEffect> Effects = new Dictionary<Unit, ParticleEffect>();

        private static bool loaded;
        private static bool active = false;

        private static bool enabled = true;
        private static bool onlyKills = false;

        private static Hero me;

        private static Ability[] shadowRaze = new Ability[3];

        private static float[] shadowRazeDamage = new float[3];

        private static int lastLevel = -1;

        private static Font text;

        #endregion

        #region Public Methods and Operators

        public static void Init()
        {
            Game.OnUpdate += Game_OnUpdate;
            loaded = false;
            text = new Font(
            Drawing.Direct3DDevice9,
            new FontDescription
            {
                FaceName = "Tahoma",
                Height = 13,
                OutputPrecision = FontPrecision.Default,
                Quality = FontQuality.Default
            });

            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
            Drawing.OnEndScene += Drawing_OnEndScene;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainDomainUnload;
            Game.OnWndProc += Game_OnWndProc;
        }

        #endregion

        #region Methods

        private static void CurrentDomainDomainUnload(object sender, EventArgs e)
        {
            text.Dispose();
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (Drawing.Direct3DDevice9 == null || Drawing.Direct3DDevice9.IsDisposed || !Game.IsInGame)
            {
                return;
            }

            var player = ObjectMgr.LocalPlayer;

            if (player == null || player.Team == Team.Observer)
            {
                return;
            }
            
            if (loaded && me.ClassID == ClassID.CDOTA_Unit_Hero_Nevermore)
            {
                var addstr = "[ KiKRee SF Helper: ";
                addstr += enabled ? "Enabled" : "Disabled";
                addstr += onlyKills ? ", Only kills" : "";
                addstr += active ? ", Active" : "";
                addstr += " ]";
                addstr += " P - Toggle, L - Only Kills, D - Auto Raze";

                text.DrawText(null, addstr, 5, 40, Color.DarkGreen);
            }

        }

        private static void Drawing_OnPostReset(EventArgs args)
        {
            text.OnResetDevice();
        }

        private static void Drawing_OnPreReset(EventArgs args)
        {
            text.OnLostDevice();
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!loaded)
            {
                me = ObjectMgr.LocalHero;
                if (!Game.IsInGame || me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Nevermore)
                {
                    return;
                }
                loaded = true;
                shadowRaze[0] = me.Spellbook.SpellQ;
                shadowRaze[1] = me.Spellbook.SpellW;
                shadowRaze[2] = me.Spellbook.SpellE;
                Console.WriteLine("SF Helper: Loaded!");
            }

            if (!Game.IsInGame || me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Nevermore)
            {
                loaded = false;
                Console.WriteLine("SF Helper: Unloaded!");
                return;
            }

            if (Game.IsPaused)
            {
                return;
            }



            var coilLevel = shadowRaze[0].Level;

            if (coilLevel != lastLevel)
            {
                for (int i = 0; i < 3; i++)
                {
                    var firstOrDefault = shadowRaze[i].AbilityData.FirstOrDefault(x => x.Name == "#AbilityDamage");
                    if (firstOrDefault != null)
                    {
                        shadowRazeDamage[i] = firstOrDefault.GetValue(coilLevel - 1);
                    }
                    lastLevel = (int)coilLevel;
                }
            }

            if (active && enabled)
            {
                var enemyHeroes = FindTargets(100);
                if (enemyHeroes.Length > 0)
                {
                    var targetHero = enemyHeroes.First();

                    var heroDistance = me.Distance2D(RazeRange(targetHero));

                    if (heroDistance <= 400 && heroDistance >= 0)
                    {
                        CastRaze(0, targetHero);
                    }
                    if (heroDistance <= 650 && heroDistance >= 250)
                    {
                        CastRaze(1, targetHero);
                    }
                    if (heroDistance <= 900 && heroDistance >= 500)
                    {
                        CastRaze(2, targetHero);
                    }
                }
            }

        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (!Game.IsChatOpen)
            {
                if (Game.IsKeyDown(System.Windows.Input.Key.D))
                    active = true;
                else
                    active = false;
                //
                switch (args.Msg)
                {
                    case (uint)Utils.WindowsMessages.WM_KEYDOWN:
                        /* switch (args.WParam)
                        {
                            case 'D':
                                active = true;
                                break;
                        }*/
                        break;
                    case (uint)Utils.WindowsMessages.WM_KEYUP:
                        switch (args.WParam)
                        {
                            case 'P':
                                enabled = !enabled;
                                break;
                            case 'L':
                                onlyKills = !onlyKills;
                                break;
                            /*
                        case 'D':
                            active = false;
                            break;
                             */
                        }
                        break;
                }
            }
        }

        private static Hero[] FindTargets(float range)
        {
            return
            ObjectMgr.GetEntities<Hero>()
                .Where(
            x => x.Team != me.Team && x.IsAlive && x.IsVisible && !x.IsMagicImmune() && x.Modifiers.All(y => y.Name != "modifier_abaddon_borrowed_time") && Utils.SleepCheck(x.ClassID.ToString()) && !x.IsIllusion).OrderBy(s => s.Distance2D(Game.MousePosition)).ToArray();

        }

        private static Vector3 RazeRange(Hero ent)
        {
            if (ent.NetworkActivity == NetworkActivity.Move && ent.CanMove())
            {
                var turn = TurnRate(ent.Position) / 1000;
                return new Vector3((float)(ent.Position.X + ent.MovementSpeed * (0.67 + turn) * Math.Cos(ent.RotationRad)), (float)(ent.Position.Y + ent.MovementSpeed * (0.67 + turn) * Math.Sin(ent.RotationRad)), ent.Position.Z);
            }
            else
            {
                return ent.Position;
            }
        }

        private static float TurnRate(Vector3 pos)
        {
            var angel = ((((Math.Atan2(pos.Y - me.Position.Y, pos.X - me.Position.X) - me.RotationRad + Math.PI)) - Math.PI) % (2 * Math.PI)) * 180 / Math.PI;
            if (angel > 180) return (float)((360 - angel) / 2);
            else return (float)(angel / 2);
        }

        private static void CastRaze(int number, Hero target)
        {
            var raze = shadowRaze[number];
            if (raze != null && raze.CanBeCasted() && me.CanCast() && Utils.SleepCheck("raze"))
            {
                if (onlyKills)
                {
                    var dmg = CheckRazeDamage(number, target);
                    if (!(dmg >= target.Health))
                    {
                        return;
                    }
                }
                //
                ParticleEffect eff = target.AddParticleEffect("particles/items_fx/aura_shivas.vpcf");
                new Timer(new TimerCallback(delegate(object e){ eff.Dispose(); }), null, 1000, 0);
                //
                me.Attack(target);
                raze.UseAbility();
                Utils.Sleep(200, "raze");
            }
        }
        private static float CheckRazeDamage(int number, Unit hero)
        {
            return hero.DamageTaken(shadowRazeDamage[number], DamageType.Magical, me, false);
        }

        #endregion

    }
}