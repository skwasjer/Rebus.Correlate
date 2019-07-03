using Rebus.Correlate.Fixtures;
using Xunit;

namespace Rebus.Correlate
{
	public class RebusServiceProviderTests : RebusIntegrationTests, IClassFixture<RebusServiceProviderFixture>
	{
		// ReSharper disable once SuggestBaseTypeForParameter
		public RebusServiceProviderTests(RebusServiceProviderFixture fixture)
			: base(fixture)
		{
		}
	}
}