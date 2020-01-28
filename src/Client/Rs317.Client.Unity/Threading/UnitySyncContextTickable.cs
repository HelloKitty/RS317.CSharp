using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Rs317.Sharp
{
	public sealed class UnitySyncContextTickable : MonoBehaviour
	{
		private void Update()
		{
			CustomUnitySynchronizationContext.ExecuteTasks();
		}
	}
}
