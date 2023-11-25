namespace Rebus.Correlate;

public class TestMessage
{
    public string Value { get; set; } = string.Empty;

    public TestMessage Extend(int value)
    {
        return new TestMessage { Value = Value + value };
    }
}
