﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using ReClassNET.Memory;
using ReClassNET.UI;
using ReClassNET.Util;

namespace ReClassNET.Nodes
{
	public abstract class BaseFunctionPtrNode : BaseNode
	{
		private IntPtr address = IntPtr.Zero;
		private readonly List<string> instructions = new List<string>();

		/// <summary>Size of the node in bytes.</summary>
		public override int MemorySize => IntPtr.Size;

		public override string GetToolTipText(HotSpot spot, MemoryBuffer memory)
		{
			var ptr = memory.ReadObject<IntPtr>(Offset);

			DisassembleRemoteCode(memory, ptr);

			return string.Join("\n", instructions);
		}

		protected int Draw(ViewInfo view, int x, int y, string type, string name)
		{
			Contract.Requires(view != null);
			Contract.Requires(type != null);
			Contract.Requires(name != null);

			if (IsHidden)
			{
				return DrawHidden(view, x, y);
			}

			AddSelection(view, x, y, view.Font.Height);
			AddDelete(view, x, y);
			AddTypeDrop(view, x, y);

			x += TextPadding;

			x = AddIcon(view, x, y, Icons.Function, -1, HotSpotType.None);

			var tx = x;

			x = AddAddressOffset(view, x, y);

			x = AddText(view, x, y, view.Settings.TypeColor, HotSpot.NoneId, type) + view.Font.Width;
			x = AddText(view, x, y, view.Settings.NameColor, HotSpot.NameId, name) + view.Font.Width;

			x = AddOpenClose(view, x, y) + view.Font.Width;

			x = AddComment(view, x, y);

			if (view.Settings.ShowCommentSymbol)
			{
				var value = view.Memory.ReadObject<IntPtr>(Offset);

				var module = view.Memory.Process.GetModuleToPointer(value);
				if (module != null)
				{
					var symbols = view.Memory.Process.Symbols.GetSymbolsForModule(module);
					if (symbols != null)
					{
						var symbol = symbols.GetSymbolString(value, module);
						if (!string.IsNullOrEmpty(symbol))
						{
							x = AddText(view, x, y, view.Settings.OffsetColor, HotSpot.ReadOnlyId, symbol) + view.Font.Width;
						}
					}
				}
			}

			if (levelsOpen[view.Level])
			{
				var ptr = view.Memory.ReadObject<IntPtr>(Offset);

				DisassembleRemoteCode(view.Memory, ptr);

				foreach (var line in instructions)
				{
					y += view.Font.Height;

					AddText(view, tx, y, view.Settings.NameColor, HotSpot.ReadOnlyId, line);
				}
			}

			return y + view.Font.Height;
		}

		public override int CalculateHeight(ViewInfo view)
		{
			if (IsHidden)
			{
				return HiddenHeight;
			}

			var h = view.Font.Height;
			if (levelsOpen[view.Level])
			{
				h += instructions.Count * view.Font.Height;
			}
			return h;
		}

		private void DisassembleRemoteCode(MemoryBuffer memory, IntPtr address)
		{
			Contract.Requires(memory != null);

			if (this.address != address)
			{
				instructions.Clear();

				this.address = address;

				if (!address.IsNull() && memory.Process.IsValid)
				{
					var disassembler = new Disassembler();
					instructions.AddRange(
						disassembler.DisassembleRemoteCode(memory.Process, address, 200)
#if WIN64
							.Select(i => $"{i.Address.ToString("X08")} {i.Instruction}")
#else
							.Select(i => $"{i.Address.ToString("X04")} {i.Instruction}")
#endif
					);
				}
			}
		}
	}
}
