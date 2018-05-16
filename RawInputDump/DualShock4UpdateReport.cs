using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RawInputDump {
	// http://eleccelerator.com/wiki/index.php?title=DualShock_4#Report_Structure
	// http://www.psdevwiki.com/ps4/DS4-USB

	enum DualShock4DPad {
		Up,
		UpRight,
		Right,
		DownRight,
		Down,
		DownLeft,
		Left,
		UpLeft,

		Center
	}

	// Recieved at a ~125hz rate for the USB dongle, ~250hz for a wired USB connection
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	struct DualShock4UpdateReport {
		public byte ReportType; // 0x01
		public byte LeftStickX;
		public byte LeftStickY;
		public byte RightStickX;
		public byte RightStickY;

		byte                  Buttons_YBAX_Dpad;
		public DualShock4DPad DPad          { get { return (DualShock4DPad)(Buttons_YBAX_Dpad & 0xf); } }
		public bool           X             { get { return (Buttons_YBAX_Dpad & 0x10) != 0; } }
		public bool           A             { get { return (Buttons_YBAX_Dpad & 0x20) != 0; } }
		public bool           B             { get { return (Buttons_YBAX_Dpad & 0x40) != 0; } }
		public bool           Y             { get { return (Buttons_YBAX_Dpad & 0x80) != 0; } }
		public bool           Square        { get { return (Buttons_YBAX_Dpad & 0x10) != 0; } }
		public bool           Cross         { get { return (Buttons_YBAX_Dpad & 0x20) != 0; } }
		public bool           Circle        { get { return (Buttons_YBAX_Dpad & 0x40) != 0; } }
		public bool           Triangle      { get { return (Buttons_YBAX_Dpad & 0x80) != 0; } }
		public bool           DPadLeft      { get { return DPad == DualShock4DPad.DownLeft  || DPad == DualShock4DPad.Left  || DPad == DualShock4DPad.UpLeft   ; } }
		public bool           DPadRight     { get { return DPad == DualShock4DPad.DownRight || DPad == DualShock4DPad.Right || DPad == DualShock4DPad.UpRight  ; } }
		public bool           DPadUp        { get { return DPad == DualShock4DPad.UpLeft    || DPad == DualShock4DPad.Up    || DPad == DualShock4DPad.UpRight  ; } }
		public bool           DPadDown      { get { return DPad == DualShock4DPad.DownLeft  || DPad == DualShock4DPad.Down  || DPad == DualShock4DPad.DownRight; } }

		byte                  Buttons_RLRL_RLOS;
		public bool           LeftShoulder  { get { return (Buttons_RLRL_RLOS & 0x01) != 0; } }
		public bool           RightShoulder { get { return (Buttons_RLRL_RLOS & 0x02) != 0; } }
		public bool           LeftTriggerB  { get { return (Buttons_RLRL_RLOS & 0x04) != 0; } }
		public bool           RightTriggerB { get { return (Buttons_RLRL_RLOS & 0x08) != 0; } }
		public bool           Share         { get { return (Buttons_RLRL_RLOS & 0x10) != 0; } }
		public bool           Options       { get { return (Buttons_RLRL_RLOS & 0x20) != 0; } }
		public bool           LeftStick     { get { return (Buttons_RLRL_RLOS & 0x40) != 0; } }
		public bool           RightStick    { get { return (Buttons_RLRL_RLOS & 0x80) != 0; } }

		byte                  Time_Touchpad_Guide;
		public bool           Guide         { get { return (Time_Touchpad_Guide & 0x01) != 0; } }
		public bool           TouchpadClick { get { return (Time_Touchpad_Guide & 0x02) != 0; } }
		public byte           Time          { get { return (byte)(Time_Touchpad_Guide >> 2); } }

		public byte           LeftTrigger;
		public byte           RightTrigger;

		// Increments by about ~1500/packet via wireless dongle (125hz updates), or about half that via wired usb connection (250hz updates).
		// This implies a ~187.5 khz clock?
		public ushort         SensorsTime;
		public byte           Temperature; // Observed: 0x0D (recently turned on)-0x21 (up to 06 from 00 charging).  Temp of some internal component (battery?) - kept rising awhile when I took it from in front of my heater to inside my fridge, then falling after I took it out of the fridge and warmed it back up some.

		// Rotational data
		public short          GyroX; // Keeping the controller flat on the table, this is positive if you spin it "leftwards"
		public short          GyroY; // Starting with the controller flat on the table, this is potivive if you rotate it towards yourself
		public short          GyroZ; // Starting with the controller flat on the table, this is positive if you lift only the right hand side / rotate the controller "left"

		// Acceleration data - this maps well to "which way is down, in screen coordinates"
		public short          AccelX; // "Left" (positive if the controller is on it's left side - e.g. as if you were turning a steering wheel left)
		public short          AccelY; // "Down" (positive if the controller is flat on the table)
		public short          AccelZ; // "Away" (positive if the controller is tilted away from you)
		// Flat on a table ~= (0, 1/4, 0) if mapped to floats - which would imply the accelerometer caps out at 4g.

		public uint           _Unknown_1; // Only seen 0000 0000

		public uint           Flags_Unknown_Flags;
		public bool           Connected           { get { return        (Flags_Unknown_Flags &  0x40000) == 0; } }
		public bool           UsbConnected        { get { return        (Flags_Unknown_Flags &   0x1000) != 0; } } // Perhaps means "charging" instead?
		public bool           HeadphonesConnected { get { return        (Flags_Unknown_Flags &   0x2000) != 0; } }
		public byte           Battery             { get { return (byte)((Flags_Unknown_Flags &   0x0f00) >> 8); } } // Observed: 00-0B

		// 0007 0000

		public byte           _Unknown_TouchEventCount; // Can be 0, but once touch has occured, always at least 1, often 2.
		public byte           TouchEventCount { get { return (byte)(_Unknown_TouchEventCount & 0x3); } }
		public TouchEvent     TouchEvent0;
		public TouchEvent     TouchEvent1;
		public TouchEvent     TouchEvent2;
		public byte           _Unknown_End_0; // Always 0x00
		public byte           _Unknown_End_1; // Always 0x00 for dongle, always 0x80 for usb - not sure if this is a real flag or uninitialized data.
		public byte           _Unknown_End_2; // Always 0x00
		// Total size: 64 bytes

		public IEnumerable<TouchEvent> RawTouchEvents { get {
			yield return TouchEvent0;
			yield return TouchEvent1;
			yield return TouchEvent2;
		}}

		public IEnumerable<TouchEvent> TouchEvents { get {
			var count = TouchEventCount;
			if (count > 0) yield return TouchEvent0;
			if (count > 1) yield return TouchEvent1;
			if (count > 2) yield return TouchEvent2;
		}}

		[StructLayout(LayoutKind.Sequential, Pack =1)]
		public struct TouchEvent {
			public byte        Time;
			public TouchFinger Finger0;
			public TouchFinger Finger1;

			public IEnumerable<TouchFinger> Fingers { get {
				yield return Finger0;
				yield return Finger1;
			}}
		}

		[StructLayout(LayoutKind.Sequential, Pack =1)]
		public struct TouchFinger {
			uint Data;
			public int  No   { get { return (int)(Data & 0x7Fu); } } // Event #, increments when you release and re-press your finger.
			public bool Held { get { return (Data & 0x80) == 0; } }
			public int  X    { get { return (int)((Data >>  8) & 0xFFF); } } // 0-1919
			public int  Y    { get { return (int)((Data >> 20) & 0xFFF); } } // 0-942 on most controllers
		}

		public static unsafe DualShock4UpdateReport? TryParse(int vendorId, int productId, byte[] rawHid) {
			if (vendorId != VendorId.Sony) return null;
			if ((productId != ProductId.Dualshock4) && (productId != ProductId.Dualshock4_v2) && (productId != ProductId.WirelessDongle)) return null;

			var report = new DualShock4UpdateReport();
			var size = Marshal.SizeOf(report);
			if (size > rawHid.Length) return null;
			if (size < rawHid.Length) return null;
			Marshal.Copy(rawHid, 0, new IntPtr(&report), size);
			if (report.ReportType != 0x01) return null;
			return report;
		}
	}
}
