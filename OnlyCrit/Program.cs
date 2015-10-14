using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace OnlyCrit {
	internal class Program {

        #region Defs

		private static string VERSION = "v1.0.0.0";
		private static bool enabled = false;

		private static Hero me = null;
		private static double tickRate = 100;
		private static Font font;

		#endregion

		#region Main

		private static void Main(string[] args) {
			Game.OnUpdate += Game_OnUpdate;
			Game.OnWndProc += Game_OnWndProc;
			font = new Font(
			Drawing.Direct3DDevice9,
			new FontDescription {
				FaceName = "Tahoma",
				Height = 13,
				OutputPrecision = FontPrecision.Default,
				Quality = FontQuality.Default
			});
			AppDomain.CurrentDomain.DomainUnload += CurrentDomainDomainUnload;
			Drawing.OnPreReset += Drawing_OnPreReset;
			Drawing.OnPostReset += Drawing_OnPostReset;
			Drawing.OnEndScene += Drawing_OnEndScene;
		}

		#endregion

		#region Domain Unload

		private static void CurrentDomainDomainUnload(object sender, EventArgs e) {
			font.Dispose();
		}

		#endregion

		#region Drawing

		private static void Drawing_OnEndScene(EventArgs args) {
			if (Drawing.Direct3DDevice9 == null || Drawing.Direct3DDevice9.IsDisposed || !Game.IsInGame) return;
			//
			var str = "[ KiKRee OnlyCrit (" + VERSION + "): ";
			str += enabled ? "Enabled" : "Disabled";
			str += " | Tickrate: " + tickRate;
			str += " ]";
			str += " K - Toggle, + - Increase tickRate, - - Decrease tickRate";

			font.DrawText(null, str, 5, 60, Color.White);

		}

		private static void Drawing_OnPostReset(EventArgs args) {
			font.OnResetDevice();
		}
		private static void Drawing_OnPreReset(EventArgs args) {
			font.OnLostDevice();
		}

		#endregion

		#region Game
		static void Game_OnWndProc(WndEventArgs args) {
			if (!Game.IsChatOpen) {
				switch (args.Msg) {
					case (uint) Utils.WindowsMessages.WM_KEYUP:
						Console.WriteLine(args.WParam);
						switch (args.WParam) {
							case 'K':
								enabled = !enabled;
								break;
							case 107:
								tickRate++;
								break;
							case 109:
								tickRate--;
								break;
						}
						break;
				}
			}
		}

		private static void Game_OnUpdate(EventArgs args) {
			if (!Game.IsInGame || !enabled) return;
			if (!Utils.SleepCheck("update")) return;
			//
			try {
				if (me == null) me = ObjectMgr.LocalHero;
			} catch {
				me = ObjectMgr.LocalHero;
			}
			//
			switch ((int) me.NetworkActivity) {
				case 1503:
					// attack
					StopAndAttack();
					break;
				case 1505:
					// critical
					break;
			}
			Utils.Sleep(tickRate, "update");
		}

		#endregion

		#region Methods

		private static void StopAndAttack() {
			me.Hold();
			me.Attack(Game.MousePosition);
		}

		#endregion
	}
}