using SlimDX;
using SlimDX.Multimedia;
using SlimDX.RawInput;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace RawInputDump {
	[System.ComponentModel.DesignerCategory("")]
	class DisplayForm : Form {
		public DisplayForm() {
			ClientSize = new Size(800,600);
			DoubleBuffered = true;
			Font = new Font("Lucida Console", 10);
			Text = "DualShock 4 Gamepad Dump";

			Application.Idle += Application_Idle;
			Device.RegisterDevice(UsagePage.Generic, UsageId.Gamepad, DeviceFlags.None);
			Device.RegisterDevice(UsagePage.Generic, UsageId.Joystick, DeviceFlags.None);
			Device.RawInput += Device_RawInput;
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				Device.RawInput -= Device_RawInput;
				Application.Idle -= Application_Idle;
			}
			base.Dispose(disposing);
		}

		// Top strip
		static readonly Rectangle BatteryArea       = new Rectangle( 50,20,100, 10);
		static readonly Rectangle SignalArea        = new Rectangle(350,20,100, 10);

		// Left strip
		static readonly Rectangle LeftShoulderArea  = new Rectangle( 20,20, 10, 10);
		static readonly Rectangle LeftTriggerArea   = new Rectangle( 20,50, 10,100);

		// Right strip
		static readonly Rectangle RightShoulderArea  = new Rectangle(470,20, 10, 10);
		static readonly Rectangle RightTriggerArea   = new Rectangle(470,50, 10,100);

		// High row
		static readonly Rectangle DpadArea          = new Rectangle( 50, 50,100,100);
		static readonly Rectangle ShareButtonArea   = new Rectangle(155, 50, 15, 50);
		static readonly Rectangle TouchpadArea      = new Rectangle(175, 50,150,100);
		static readonly Rectangle OptionsButtonArea = new Rectangle(330, 50, 15, 50);
		static readonly Rectangle FaceButtonsArea   = new Rectangle(350, 50,100,100);

		// Low row
		static readonly Rectangle LeftStickArea     = new Rectangle(100,150,100,100);
		static readonly Rectangle GuideArea         = new Rectangle(225,175, 50, 50);
		static readonly Rectangle RightStickArea    = new Rectangle(300,150,100,100);

		// Lowest row
		static readonly Rectangle AccelArea         = new Rectangle( 50,300,200,200);
		static readonly Rectangle GyroArea          = new Rectangle(250,300,200,200);



		static void DrawButton(Graphics fx, Rectangle area, bool bit) {
			if (bit) fx.FillRectangle(Brushes.Black, area);
			else     fx.DrawRectangle(Pens.Black,    area);
		}

		static void DrawBits(Graphics fx, Rectangle area, bool[,] bits) {
			fx.DrawRectangle(Pens.Black, area);

			var w = bits.GetLength(0);
			var h = bits.GetLength(1);
			for (var y=0; y<h; ++y) for (var x=0; x<w; ++x) {
				var bit = bits[y,x];
				var square = new Rectangle(area.X + area.Width*x/w, area.Y + area.Height*y/h, area.Width/w, area.Height/h);
				if (bit) fx.FillRectangle(Brushes.Black, square);
			}
		}

		static void DrawAxises(Graphics fx, Rectangle area, Vector2 point, bool fill) { DrawAxises(fx, area, new[] {point}, fill); }
		static void DrawAxises(Graphics fx, Rectangle area, Vector3 point, bool fill) { DrawAxises(fx, area, new[] {point}, fill); }
		static void DrawAxises(Graphics fx, Rectangle area, Vector2[] points, bool fill) { DrawAxises(fx, area, points.Select(p => new Vector3(p,0.0f)).ToArray(), fill); }
		static void DrawAxises(Graphics fx, Rectangle area, Vector3[] points, bool fill) {
			fx.DrawRectangle(Pens.Black, area);
			var cx = area.X + area.Width/2;
			var cy = area.Y + area.Height/2;
			fx.DrawLine(Pens.Black, cx, cy-5, cx, cy+5);
			fx.DrawLine(Pens.Black, cx-5, cy, cx+5, cy);

			var baseC = 20;
			var deltaC = 20;
			foreach (var point in points) {
				var realC = baseC + deltaC * point.Z;

				var x = area.X + (area.Width -baseC) * (point.X+1)/2;
				var y = area.Y + (area.Height-baseC) * (point.Y+1)/2;
				if (fill) fx.FillEllipse(Brushes.Black, x+(baseC-realC)/2, y, realC, realC);
				else      fx.DrawEllipse(Pens.Black,    x+(baseC-realC)/2, y, realC, realC);
			}
		}

		float RefreshRate;
		int Packets=0;
		DateTime LastRRUpdate = DateTime.Now;

		DateTime PrevTime = DateTime.Now, LastTime = DateTime.Now;
		DualShock4UpdateReport PrevReport, LastReport;
		protected override void OnPaint(PaintEventArgs e) {
			var fx = e.Graphics;
			fx.Clear(BackColor);
			fx.SmoothingMode = SmoothingMode.AntiAlias;
			DrawControls(fx, LastReport);
			DrawStats(fx, LastReport);
			base.OnPaint(e);
		}

		void DrawControls(Graphics fx, DualShock4UpdateReport gp) {
			// Top strip
			DrawAxises(fx, BatteryArea, new Vector2(gp.Battery/8f*2-1, 0), false);

			// Left strip
			DrawButton(fx, LeftShoulderArea, gp.LeftShoulder);
			DrawAxises(fx, LeftTriggerArea, new Vector2(0, gp.LeftTrigger/255f*2-1), gp.LeftTriggerB);

			// Right strip
			DrawButton(fx, RightShoulderArea, gp.RightShoulder);
			DrawAxises(fx, RightTriggerArea, new Vector2(0, gp.RightTrigger/255f*2-1), gp.RightTriggerB);

			// Top Row

			fx.DrawString("DPad", Font, Brushes.Black, DpadArea);
			DrawBits(fx, DpadArea, new bool[3,3]{
				{ false,       gp.DPadUp,   false        },
				{ gp.DPadLeft, false,       gp.DPadRight },
				{ false,       gp.DPadDown, false        }
			});

			DrawButton(fx, ShareButtonArea,   gp.Share);

			var touches = gp.TouchEvents.ToList();
			var fingers = touches.SelectMany(t => t.Fingers).Where(f => f.Held).Select(f => new Vector2(f.X/1919f*2-1, f.Y/942f*2-1)).ToArray();
			fx.DrawString("Touchpad", Font, Brushes.Black, TouchpadArea);
			DrawAxises(fx, TouchpadArea, fingers, gp.TouchpadClick);

			DrawButton(fx, OptionsButtonArea, gp.Options);

			fx.DrawString("Face Buttons", Font, Brushes.Black, FaceButtonsArea);
			DrawBits(fx, FaceButtonsArea, new bool[3,3]{
				{ false,     gp.Triangle, false     },
				{ gp.Square, false,       gp.Circle },
				{ false,     gp.Cross,    false     }
			});

			// Low row
			fx.DrawString("Left Stick", Font, Brushes.Black, LeftStickArea);
			DrawAxises(fx, LeftStickArea,  new Vector2(gp.LeftStickX /255f*2-1, gp.LeftStickY /255f*2-1), gp.LeftStick);
			DrawButton(fx, GuideArea, gp.Guide);
			fx.DrawString("Right Stick", Font, Brushes.Black, RightStickArea);
			DrawAxises(fx, RightStickArea, new Vector2(gp.RightStickX/255f*2-1, gp.RightStickY/255f*2-1), gp.RightStick);

			// Lowest row
			var exaggerate = 4f;
			fx.DrawString("Accel", Font, Brushes.Black, AccelArea);
			DrawAxises(fx, AccelArea, exaggerate * new Vector3(gp.AccelX*1f/short.MaxValue, gp.AccelY*1f/short.MaxValue, gp.AccelZ*1f/short.MaxValue), false);

			fx.DrawString("Gyro", Font, Brushes.Black, GyroArea);
			DrawAxises(fx, GyroArea,  exaggerate * new Vector3(gp.GyroX *1f/short.MaxValue, gp.GyroY *1f/short.MaxValue, gp.GyroZ *1f/short.MaxValue), false);
		}

		static string ToHex(byte   v) { return Convert.ToString(v, 16).PadLeft(2, '0'); }
		static string ToHex(ushort v) { return Convert.ToString(v, 16).PadLeft(4, '0'); }
		static string ToHex(uint   v) { return Convert.ToString(v, 16).PadLeft(8, '0'); }

		int Events = 0;
		void DrawStats(Graphics fx, DualShock4UpdateReport gp) {
			int y=10;
			Action<string> writeLine = (line) => { fx.DrawString(line, Font, Brushes.Black, 500, y); y += 12; };

			writeLine(string.Format("Events:                    {0}", Events++));
			writeLine(string.Format("Rate:                     ~{0:N1}hz", RefreshRate));
			writeLine(string.Format("Time:                      {0}", gp.Time));
			writeLine(string.Format("Sensors time:              {0}", gp.SensorsTime.ToString().PadLeft(6, ' ')));
			writeLine(string.Format("                         (+{0})",((ushort)(gp.SensorsTime - PrevReport.SensorsTime)).ToString().PadLeft(6, ' '))); // ~ 1500 dT with ~ 125hz packets would imply 187.5 khz clock?  I note this is half the USB rate http://www.psdevwiki.com/ps4/DS4-USB lists (250hz)
			writeLine(string.Format("Temperature:               {0}", ToHex(gp.Temperature         ) ));
			writeLine(string.Format("_Unknown_1:                {0}", ToHex(gp._Unknown_1         ) ));
			writeLine(string.Format("Flags_Unknown_Flags:       {0}", ToHex(gp.Flags_Unknown_Flags) ));
			writeLine(string.Format("  Connected:               {0}", gp.Connected));
			writeLine(string.Format("  UsbConnected:            {0}", gp.UsbConnected));
			writeLine(string.Format("  HeadphonesConnected:     {0}", gp.HeadphonesConnected));
			writeLine(string.Format("  Battery:                 {0}", ToHex(gp.Battery    ) ));
			writeLine(string.Format("_Unknown_TouchEventCount:  {0}", ToHex((byte)(gp._Unknown_TouchEventCount & 0x7C)) ));
			writeLine(string.Format("Final padding:             {0} {1} {2}", ToHex(gp._Unknown_End_0), ToHex(gp._Unknown_End_1), ToHex(gp._Unknown_End_2)));
			writeLine("");
		}

		private void Device_RawInput(object sender, RawInputEventArgs e) {
			var device = Device.GetDevices().FirstOrDefault(d => d.Handle == e.Device);
			if (device == null) return; // XXX: Display state?

			var hidInfo = device as HidInfo;
			if (hidInfo == null) return;
			if (hidInfo.ProductId != (int)ProductId.Dualshock4_v2) return;
			//if (hidInfo.ProductId != (int)ProductId.WirelessDongle) return;

			var now = DateTime.Now;

			++Packets;
			if (Packets >= 100) {
				RefreshRate = (float)(Packets / (now - LastRRUpdate).TotalSeconds);
				LastRRUpdate = now;
				Packets = 0;
			}

			PrevTime = LastTime;
			PrevReport = LastReport;

			LastTime   = now;
			LastReport = DualShock4UpdateReport.TryParse(hidInfo.VendorId, hidInfo.ProductId, e.RawData) ?? LastReport;
		}

		private void Application_Idle(object sender, EventArgs e) { Invalidate(); }
		static void Main(string[] args) { Application.Run(new DisplayForm()); }
		//static void Main(string[] args) { Application.Run(new DumpForm()); }
	}
}
