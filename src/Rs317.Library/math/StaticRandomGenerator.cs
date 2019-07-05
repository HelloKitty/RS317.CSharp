﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//TODO: Add namespace
public static class StaticRandomGenerator
{
	//TODO: If we do any async/await this will potentially fail? Maybe? TODO look into it.
	//Unique per thread.
	[ThreadStatic]
	private static readonly System.Random internalRandomGenerator;

	static StaticRandomGenerator()
	{
		internalRandomGenerator = new System.Random();
	}

	public static int Next()
	{
		return internalRandomGenerator.Next();
	}

	public static int Next(int max)
	{
		//.NET random doesn't support anything less than 0.
		if (max < 0) throw new ArgumentOutOfRangeException(nameof(max));

		return internalRandomGenerator.Next(max);
	}

	public static double NextDouble()
	{
		return internalRandomGenerator.NextDouble();
	}
}
