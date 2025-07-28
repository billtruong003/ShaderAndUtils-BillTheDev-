namespace MVC.Editor
{
	public class ToolkitBehaviourExtensionEditor<T> : ToolkitBehaviourEditor where T : ToolkitBehaviour
	{
		public T Base
		{
			get
			{
				if (!@base && Instance)
					@base = Instance.Base;

				return @base;
			}
		}

		private ToolkitBehaviourExtension<T> Instance
		{
			get
			{
				if (!instance)
					instance = (ToolkitBehaviourExtension<T>)target;

				return instance;
			}
		}

		private ToolkitBehaviourExtension<T> instance;
		private T @base;
	}
}
