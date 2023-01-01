using System;
using System.Collections.Generic;
using System.Linq;

namespace MinecraftLaunch.Modules.Toolkits;

public class CryptoToolkit
{
	private static readonly char[] _hexTable = new char[16]
	{
		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
		'a', 'b', 'c', 'd', 'e', 'f'
	};

	public static IEnumerable<byte> Remove(ReadOnlySpan<byte> data)
	{
		IEnumerable<byte> data2 = new List<byte>();
		ReadOnlySpan<byte> readOnlySpan;
		if (data[0] == 239 && data[1] == 187 && data[2] == 191)
		{
			readOnlySpan = data;
			readOnlySpan = readOnlySpan.Slice(3, readOnlySpan.Length - 3);
			for (int k = 0; k < readOnlySpan.Length; k++)
			{
				byte j = readOnlySpan[k];
				data2.Append(j);
			}
			return data2;
		}
		readOnlySpan = data;
		for (int k = 0; k < readOnlySpan.Length; k++)
		{
			byte i = readOnlySpan[k];
			data2.Append(i);
		}
		return data2;
	}

	public static ReadOnlySpan<byte> Remove(ReadOnlySpan<byte> data, int i = 2)
	{
		if (data[0] == 239 && data[1] == 187 && data[2] == 191)
		{
			ReadOnlySpan<byte> readOnlySpan = data;
			return readOnlySpan.Slice(3, readOnlySpan.Length - 3);
		}
		return data;
	}
}
