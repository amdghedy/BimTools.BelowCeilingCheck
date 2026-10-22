namespace BimTools.BelowCeilingCheck.Models
{
    public sealed class SummaryLine
    {
        public SummaryLine(string label, int count)
        {
            Label = label;
            Count = count;
        }

        public string Label { get; private set; }

        public int Count { get; private set; }
    }
}
