using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;

using SharpDX;
using SharpDX.Direct3D9;

namespace PRubick
{
    internal class Rubick
    {

        #region Fields

        private static string VERSION = "v1.0.0.0";

        private static bool enabled = true;
        private static bool spaceHold = false;

        private static Font font;

        private static Hero myHero = null;
        private static Ability spellSteal = null;
        private static int[] castRange = new int[] {
			1000, 1400
		};

        private static Dictionary<string, string> abilitiesFix = new Dictionary<string, string>();
        private static List<ClassID> excludedHeroes = new List<ClassID>();
        private static List<string> includedAbilities = new List<string>();

        #endregion

        #region Init

        public static void Init()
        {
            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
            font = new Font(
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

            // abilitiesFix
            abilitiesFix.Add("ancient_apparition_ice_blast_release", "ancient_apparition_ice_blast");

            // excludedHeroes
            excludedHeroes.Add(ClassID.CDOTA_Unit_Hero_Nevermore);
            excludedHeroes.Add(ClassID.CDOTA_Unit_Hero_Phoenix);
            excludedHeroes.Add(ClassID.CDOTA_Unit_Hero_Axe);

            // includedAbilities
            includedAbilities.Add("undying_tombstone");
            includedAbilities.Add("lone_druid_spirit_bear");
        }

        #endregion

        #region Wnd

        static void Game_OnWndProc(WndEventArgs args)
        {
            if (!Game.IsChatOpen && Game.IsInGame)
            {
                switch (args.Msg)
                {
                    case (uint)Utils.WindowsMessages.WM_KEYDOWN:
                        switch (args.WParam)
                        {
                            case 0x20:
                                spaceHold = true;
                                break;
                        }
                        break;
                    case (uint)Utils.WindowsMessages.WM_KEYUP:
                        switch (args.WParam)
                        {
                            case 0x20:
                                spaceHold = false;
                                break;
                            case 'P':
                                enabled = !enabled;
                                break;
                        }
                        break;
                }
            }
        }

        #endregion

        #region Drawing

        static void Drawing_OnEndScene(EventArgs args)
        {
            if (!Game.IsInGame || myHero == null) return;
            //
            if (myHero.ClassID != ClassID.CDOTA_Unit_Hero_Rubick) return;
            //

            var str = "[ KiKRee PRubick (" + VERSION + "): ";
            str += enabled ? "Enabled" : "Disabled";
            str += " ]";
            str += " P - Toggle, Space - Auto steal (Hold)";

            font.DrawText(null, str, 5, 40, Color.White);
        }

        private static void Drawing_OnPostReset(EventArgs args)
        {
            font.OnResetDevice();
        }

        private static void Drawing_OnPreReset(EventArgs args)
        {
            font.OnLostDevice();
        }

        #endregion

        #region Update

        static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame) { myHero = null; spellSteal = null; return; }
            //
            if (myHero == null) myHero = ObjectMgr.LocalHero;
            //
            if (myHero.ClassID != ClassID.CDOTA_Unit_Hero_Rubick) return;
            //
            if (spellSteal == null) spellSteal = myHero.Spellbook.SpellR;
            //
            Hero[] enemies = ObjectMgr.GetEntities<Hero>()
                .Where(
            x => x.Team != myHero.Team && x.IsAlive && x.IsVisible && Utils.SleepCheck(x.ClassID.ToString()) && !x.IsIllusion).ToArray();
            //
            if (enabled && spaceHold)
            {
                foreach (Hero enemy in enemies)
                {
                    if (!excludedHeroes.Contains(enemy.ClassID) && Utils.SleepCheck(enemy.ClassID.ToString()))
                    {
                        foreach (Ability ability in enemy.Spellbook.Spells)
                        {
                            if ((ability.AbilityType == AbilityType.Ultimate || includedAbilities.Contains(ability.Name)) && ability.CooldownLength - ability.Cooldown < ( (float)0.7 + ( Game.Ping / 1000 ) ) && !spellOnCooldown(ability.Name) && iCanSteal(enemy) && myHero.Spellbook.SpellD.Name != ability.Name && ability.CooldownLength != 0)
                            {
                                if (spellSteal.CanBeCasted())
                                    spellSteal.UseAbility(enemy);
                            }
                        }
                        Utils.Sleep(125, enemy.ClassID.ToString());
                    }
                }
            }
        }

        #endregion

        #region Methods

        private static bool iCanSteal(Hero hero)
        {
            switch (myHero.AghanimState())
            {
                case true:
                    if (myHero.Distance2D(hero) <= castRange[1]) return true;
                    break;
                case false:
                    if (myHero.Distance2D(hero) <= castRange[0]) return true;
                    break;
            }
            return false;
        }

        private static bool spellOnCooldown(string abilityName)
        {
            if (abilitiesFix.ContainsKey(abilityName)) abilityName = abilitiesFix[abilityName];
            Ability[] Spells = myHero.Spellbook.Spells.ToArray();
            Ability[] SpellsF = Spells.Where(x => x.Name == abilityName).ToArray();
            if (SpellsF.Length > 0)
            {
                Ability SpellF = SpellsF.First();
                Console.WriteLine(SpellF.Name + " = " + SpellF.Cooldown + " = " + SpellF.CooldownLength);
                if (SpellF.Cooldown > 10) return true;
                return false;
            }
            else return false;
        }

        #endregion
    }


}