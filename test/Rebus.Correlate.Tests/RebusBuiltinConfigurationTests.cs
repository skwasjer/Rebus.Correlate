using Rebus.Correlate.Fixtures;

namespace Rebus.Correlate
{
	public class RebusBuiltinConfigurationTests : RebusIntegrationTests, IClassFixture<DefaultRebusFixture>
	{
		// ReSharper disable once SuggestBaseTypeForParameter
		public RebusBuiltinConfigurationTests(DefaultRebusFixture fixture)
			: base(fixture)
		{
		}
	}
}
