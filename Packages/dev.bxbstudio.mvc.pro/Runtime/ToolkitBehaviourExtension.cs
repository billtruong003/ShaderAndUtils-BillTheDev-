#region Namespaces

using UnityEngine;

#endregion

namespace MVC
{
	[AddComponentMenu("")]
	public class ToolkitBehaviourExtension<T> : ToolkitBehaviour where T : ToolkitBehaviour
	{
		public T Base
		{
			get
			{
				if (!@base)
				{
					@base = GetComponent<T>();

					if (!@base)
						@base = GetComponentInParent<T>();
				}

				return @base;
			}
		}

		private T @base;
	}
}
