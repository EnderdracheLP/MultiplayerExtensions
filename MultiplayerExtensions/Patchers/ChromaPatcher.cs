using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IPA.Loader;
using SiraUtil.Affinity;

namespace MultiplayerExtensions.Patchers
{
	public class ChromaPatcher : IAffinity
	{
		private readonly EnvironmentPatcher _environmentPatcher;
		private readonly Type _chromaPatchType;
		public ChromaPatcher(EnvironmentPatcher environmentPatcher)
		{
			_environmentPatcher = environmentPatcher;
			_chromaPatchType = PluginManager.GetPlugin("Chroma").Assembly.GetType("RingAwakeInstantiator");
		}

		[AffinityPatch()]
		private void ChromaPatch()
		{
			
		}
	}
}
