namespace BimTools.BelowCeilingCheck.Models
{
    public sealed class OperationReport
    {
        public int Succeeded { get; set; }

        public int Failed { get; set; }

        /// <summary>Items that could not be processed, such as elements that live in linked models.</summary>
        public int Skipped { get; set; }

        /// <summary>Explanation shown to the user when an action could only be partly carried out.</summary>
        public string Note { get; set; }

        public override string ToString()
        {
            return Succeeded + " done, " + Failed + " failed, " + Skipped + " skipped";
        }
    }
}
