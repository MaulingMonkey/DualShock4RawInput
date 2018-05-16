using SlimDX.Multimedia;
using SlimDX.RawInput;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RawInputDump {
	[System.ComponentModel.DesignerCategory("")]
	class DumpForm : Form {
		public DumpForm() {
			Device.RegisterDevice(UsagePage.Generic, UsageId.Gamepad, DeviceFlags.None);
			Device.RegisterDevice(UsagePage.Generic, UsageId.Joystick, DeviceFlags.None);
			Device.RawInput += Device_RawInput;
			
		}

		//static void Main(string[] args) { Application.Run(new DumpForm()); }

		static string ToBinary(byte b ) { return Convert.ToString(b,  2).PadLeft( 8, '0'); }
		static string ToBinary(uint dw) { return Convert.ToString(dw, 2).PadLeft(32, '0'); }
		static string ToB(uint   dw) { return Convert.ToString(dw, 16).PadLeft(8, '0'); }
		static string ToB(ushort  w) { return Convert.ToString( w, 16).PadLeft(4, '0'); }
		static string ToB(byte    b) { return BitConverter.ToString(new[] { b }); }
		static string ToB(byte[]  b) { return BitConverter.ToString(b).Replace("-",""); }
		static string ForceSign(float f) { var r = f.ToString("N2"); if (!r.StartsWith("-")) r = "+" + r; return r; }
		static string ToMask4(int mask, string mask4) {
			Debug.Assert(mask4.Length == 4);
			return
				  (((mask & 0x01) == 0) ? ' ' : mask4[0]).ToString()
				+ (((mask & 0x02) == 0) ? ' ' : mask4[1]).ToString()
				+ (((mask & 0x04) == 0) ? ' ' : mask4[2]).ToString()
				+ (((mask & 0x08) == 0) ? ' ' : mask4[3]).ToString();
		}

		static string ToDPad(int i) {
			switch (i) {
			case 0: return "up        ";
			case 1: return "up-right  ";
			case 2: return "right     ";
			case 3: return "down-right";
			case 4: return "down      ";
			case 5: return "down-left ";
			case 6: return "left      ";
			case 7: return "up-left   ";
			case 8: return "center    ";
			default:return "???       ";
			}
		}

		static ushort lastSensorsTime = 0;
		private static void Device_RawInput(object sender, RawInputEventArgs e) {
			var device = Device.GetDevices().FirstOrDefault(d => d.Handle == e.Device);
			if (device == null) {
				Trace.WriteLine("Device unavailable");
				return;
			}
			//if (!device.DeviceName.Contains("09CC")) return; // Only show the ps4 device (product 09cc)
			var hidInfo = device as HidInfo;
			if (hidInfo == null) return;
			//if (hidInfo.ProductId != 0x09cc) return;
			if (hidInfo.ProductId != 0x0ba0) return;

			var sb = new StringBuilder();

			var unk1    = e.RawData[0]; // Always 01 ?
			var lx      = (e.RawData[1] / 255f) * 2 - 1;
			var ly      = (e.RawData[2] / 255f) * 2 - 1;
			var rx      = (e.RawData[3] / 255f) * 2 - 1;
			var ry      = (e.RawData[4] / 255f) * 2 - 1;

			var xaby    = e.RawData[5] >> 4;   // x,a,b,y (square, cross, circle, triangle)
			var dpad    = e.RawData[5] & 0x0F; // 0-8 hat encoded
			var face    = e.RawData[6] >> 4;   // share, options, left stick, right stick
			var rear    = e.RawData[6] & 0x0F; // left shoulder, right shoulder, left trigger, right trigger

			var guide   = (e.RawData[7] & 0x01) != 0;
			var click   = (e.RawData[7] & 0x02) != 0; // touchpad
			var time    = (byte)(e.RawData[7] >> 2); // Increases by 1 per RAWHID packet

			var lt      = e.RawData[8] / 255f;
			var rt      = e.RawData[9] / 255f;

			var sensorsTime = ((ushort)((e.RawData[11]<<8) + (e.RawData[10]<<0)));
			var dSensorsTime = ((ushort)(sensorsTime - lastSensorsTime));
			lastSensorsTime = sensorsTime;
			var sensors = e.RawData.Skip(12).Take(13).ToArray(); // Varying E952150000050001000000251F7606 - some data appears to be tilt sensors at the very least
			var unk2    = e.RawData.Skip(25).Take(9).ToArray(); // Always 00000000001B000001 ?

			var battery = e.RawData[30];
			var touchEvents = e.RawData[33];

			// Constant when 0 fingers are touching the touchpad.
			// Typically increases by about ~7-8 per 'time' increment / RAWHID packet when 1+ fingers are touching the touchpad, although sometimes 0 instead.
			// Given this weird modulo (low bits clearly drift) I don't think the low bits are special/flags.  Nor the high bits as this wraps from ~FF -> ~00.
			// Makes a discontinuous jump when going from 0 -> 1 fingers, so this appears to be an actual time value rather than an event number.
			var touchTime = e.RawData[34];
			var touch1dw = ((uint)((e.RawData[35]<<0) + (e.RawData[36]<<8) + (e.RawData[37]<<16) + (e.RawData[38]<<24)));
			var touch2dw = ((uint)((e.RawData[39]<<0) + (e.RawData[40]<<8) + (e.RawData[41]<<16) + (e.RawData[42]<<24)));

			var touch1no = (touch1dw & 0x0000007Fu); // Increments by 1 each time a new finger touches the touchpad using "touch 1" (e.g. ~touchdown, not ~touchmove)
			var touch1   = (touch1dw & 0x00000080u) == 0; // Bit is set if *no* finger is touching
			var touch1x  = (touch1dw & 0x0007FF00u) / (float)0x0007FF00u; // mask might be 000FFF00 but I've never seen the high bit set
			var touch1y  = (touch1dw & 0x3FF00000u) / (float)0x3FF00000u; // mask might be FFF00000 but I've never seen the highest two bits set

			var touch2no = (touch2dw & 0x0000007Fu); // Increments by 1 each time a new finger touches the touchpad using "touch 2" (e.g. ~touchdown, not ~touchmove)
			var touch2   = (touch2dw & 0x00000080u) == 0; // Bit is set if *no* finger is touching
			var touch2x  = (touch2dw & 0x0007FF00u) / (float)0x0007FF00u; // mask might be 000FFF00 but I've never seen the high bit set
			var touch2y  = (touch2dw & 0x3FF00000u) / (float)0x3FF00000u; // mask might be FFF00000 but I've never seen the highest two bits set

			var unk3    = e.RawData.Skip(43).ToArray(); // Always 00800000 00800000 00008000 00008000 00000080 00 (21b?)?

			sb.AppendFormat("{0} [{1}, {2}] [{3}, {4}] ", ToB(unk1), ForceSign(lx), ForceSign(ly), ForceSign(rx), ForceSign(ry));
			sb.AppendFormat("{0} {1} {2} {3} {4}{5}    ", ToMask4(xaby, "XABY"), ToDPad(dpad), ToMask4(face, "solr"), ToMask4(rear, "()[]"), guide ? "P" : " ", click ? "T" : " ");
			sb.AppendFormat("{0}    [{1:N2}, {2:N2}]    ", ToB(time), lt, rt);
			sb.AppendFormat("{0} {1}    {2}    {3} ", ToB(sensorsTime), ToB(dSensorsTime), ToB(sensors).Insert(22," ").Insert(18," ").Insert(14," ").Insert(10," ").Insert(6," ").Insert(2, " "), ToB(unk2), ToB(touchTime));
			sb.AppendFormat("{0} [{1:N2}, {2:N2}, {3}]    ", touch1 ? "P" : " ", touch1x, touch1y, true ? "" : ToBinary(touch1dw).Insert(24," ").Insert(12," ")); // (ToBinary(touch1b)+ToBinary(touch1a)));
			sb.AppendFormat("{0} [{1:N2}, {2:N2}, {3}]    ", touch2 ? "P" : " ", touch2x, touch2y, true ? "" : ToBinary(touch2dw).Insert(24," ").Insert(12," ")); // (ToBinary(touch2b)+ToBinary(touch2a)));
			sb.AppendFormat("te: {0}  ", touchEvents);
			sb.AppendFormat("bat: {0}  ", battery);
			sb.AppendFormat("len: {0}  ", e.RawData.Length);
			sb.AppendFormat("{0}\n", ToB(unk3));

			Trace.Write(sb.ToString());
		}
	}
}
