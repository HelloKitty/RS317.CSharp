using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Rs317.Sharp
{
	public static class RsUnityPlatform
	{
		public static bool isWebGLBuild => Application.platform == RuntimePlatform.WebGLPlayer;

		public static bool isPlaystationBuild => Application.platform == RuntimePlatform.PS4 || Application.platform == RuntimePlatform.PS3;

		public static bool isAndroidMobileBuild => Application.platform == RuntimePlatform.Android;

		public static bool isInEditor => Application.isEditor;
	}
}
