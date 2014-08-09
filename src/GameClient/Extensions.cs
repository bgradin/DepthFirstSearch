using Microsoft.Xna.Framework.Graphics;
using RamGecXNAControls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

namespace AnglerGameClient
{
	public static class Extensions
	{
		public static bool ContainsPoint(this Microsoft.Xna.Framework.Rectangle rect, int x, int y)
		{
			return x >= rect.Left && x <= rect.Right && y >= rect.Top && y <= rect.Bottom;
		}

		// Since C# doesn't know how to convert ints to bool
		public static bool ToBool(this int value)
		{
			return value != 0;
		}

		public static Microsoft.Xna.Framework.Rectangle PositionOnVisibleMap(this Map map, int x, int y)
		{
			return new Microsoft.Xna.Framework.Rectangle(
				(int)map.UpperLeftCorner.X + (x - map.VisibleBounds.Left) * Const.TILE_SIZE,
				(int)map.UpperLeftCorner.Y + (y - map.VisibleBounds.Top) * Const.TILE_SIZE,
				Const.TILE_SIZE,
				Const.TILE_SIZE);
		}

		#region Extensions for RamGec tab order

		public static bool IsNextControl(this GUIControl control1, GUIControl control2)
		{
			if (control1.Bounds.Top > control2.Bounds.Top)
				return true;
			else if (control1.Bounds.Top == control2.Bounds.Top && control1.Bounds.Right >= control2.Bounds.Right)
				return true;

			return false;
		}

		public static void Focus(this GUIControl control)
		{
			if (control is AdvancedTextbox)
				((AdvancedTextbox)control).OnFocus(control);

			control.Focused = true;
		}

		public static void Blur(this GUIControl control)
		{
			if (control is AdvancedTextbox)
				((AdvancedTextbox)control).OnBlur(control);

			control.Focused = false;
		}

		public static void FocusNext(this GUIManager manager)
		{
			List<Window> windows = manager.Controls.Where(i => i is Window).Select(i => i as Window).ToList();

			if (windows.Count > 1)
				throw new Exception("There can only be one window.");
			else if (windows.Count != 0)
			{
				Window window = windows[0];

				List<GUIControl> focusedControls = window.Controls.Where(i => i.Focused).ToList();

				if (focusedControls.Count > 1)
					throw new Exception("There can only be one focused control.");
				else if (focusedControls.Count != 0)
				{
					List<GUIControl> sortedControls = window.Controls.OrderBy(i => i.Bounds.Right)
						.OrderBy(i => i.Bounds.Top)
						.Where(i => !i.Focused && i.IsNextControl(focusedControls[0]))
						.ToList();

					if (sortedControls.Count > 0)
					{
						focusedControls[0].Blur();
						sortedControls[0].Focus();
					}
				}
			}
		}

		public static void FocusPrevious(this GUIManager manager)
		{
			List<Window> windows = manager.Controls.Where(i => i is Window).Select(i => i as Window).ToList();

			if (windows.Count > 1)
				throw new Exception("There can only be one window.");
			else if (windows.Count != 0)
			{
				Window window = windows[0];

				List<GUIControl> focusedControls = window.Controls.Where(i => i.Focused).ToList();

				if (focusedControls.Count > 1)
					throw new Exception("There can only be one focused control.");
				else if (focusedControls.Count != 0)
				{
					List<GUIControl> sortedControls = window.Controls.OrderBy(i => i.Bounds.Right)
						.OrderBy(i => i.Bounds.Top)
						.Where(i => !i.Focused && !i.IsNextControl(focusedControls[0]))
						.ToList();

					if (sortedControls.Count > 0)
					{
						focusedControls[0].Blur();
						sortedControls[sortedControls.Count - 1].Focus();
					}
				}
			}
		}

		#endregion

		#region Stream functions

		public static void WriteInt(this Stream stream, int value)
		{
			byte[] bytes = BitConverter.GetBytes(value);

			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);

			for (int i = 0; i < bytes.Length; i++)
				stream.WriteByte(bytes[i]);
		}

		public static int ReadInt(this Stream stream)
		{
			byte[] bytes = new byte[4];

			for (int i = 0; i < 4; i++)
				bytes[i] = stream.ReadByteAsByte();

			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);

			return BitConverter.ToInt32(bytes, 0);
		}

		public static byte ReadByteAsByte(this Stream stream)
		{
			return (byte)stream.ReadByte();
		}

		public static char ReadChar(this Stream stream)
		{
			byte[] bytes = new byte[2];
			bytes[0] = stream.ReadByteAsByte();
			bytes[1] = stream.ReadByteAsByte();

			return BitConverter.ToChar(bytes, 0);
		}

		public static void WriteChar(this Stream stream, char value)
		{
			byte[] bytes = BitConverter.GetBytes(value);

			for (int i = 0; i < bytes.Length; i++)
				stream.WriteByte(bytes[i]);
		}

		public static string ReadString(this Stream stream)
		{
			int size = stream.ReadInt();

			string result = "";

			for (int i = 0; i < size; i++)
				result += stream.ReadChar();

			return result;
		}

		public static void WriteString(this Stream stream, string value)
		{
			stream.WriteInt(value.Length);

			for (int i = 0; i < value.Length; i++)
				stream.WriteChar(value[i]);
		}

		#endregion

		#region Drawing

		public static Texture2D CreateRadialGradient(this Microsoft.Xna.Framework.Game game, int radius, Color insideColor, Color outsideColor)
		{
			Rectangle bounds = new Rectangle(0, 0, radius, radius);
			using (var ellipsePath = new GraphicsPath())
			{
				ellipsePath.AddEllipse(bounds);
				using (var brush = new PathGradientBrush(ellipsePath))
				{
					// Set up radial gradient brush
					brush.CenterPoint = new PointF(bounds.Width / 2f, bounds.Height / 2f);
					brush.CenterColor = insideColor;
					brush.SurroundColors = new[] { outsideColor };
					brush.FocusScales = new PointF(0, 0);

					Bitmap bm = new Bitmap(radius, radius);
					Graphics graphicsObject = Graphics.FromImage(bm);

					// Set up elliptical clip
					GraphicsPath clipPath = new GraphicsPath();
					clipPath.AddEllipse(bounds);

					// Fill with outsideColor outside of the clip
					graphicsObject.ExcludeClip(new Region(clipPath));
					graphicsObject.FillRectangle(new SolidBrush(outsideColor), bounds);

					// Fill with gradient from outsideColor to insideColor inside of the clip
					graphicsObject.SetClip(clipPath);
					graphicsObject.FillRectangle(brush, bounds);

					// Convert Bitmap to Texture2D
					using (MemoryStream ms = new MemoryStream())
					{
						bm.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
						return Texture2D.FromStream(game.GraphicsDevice, ms);
					}
				}
			}
		}

		#endregion
	}
}
