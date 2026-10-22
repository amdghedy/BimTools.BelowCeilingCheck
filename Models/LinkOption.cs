using System;
using BimTools.BelowCeilingCheck.Infrastructure;

namespace BimTools.BelowCeilingCheck.Models
{
    public sealed class LinkOption : ObservableObject
    {
        private static readonly string[] CeilingHints = { "arch", "arc", "int", "ceil", "-a-", "_a_" };
        private static readonly string[] MepHints = { "mep", "mech", "hvac", "elec", "plumb", "pipe", "duct", "fire", "fp", "-m-", "-e-", "-p-" };

        private bool _useForCeilings;
        private bool _useForMep;

        public LinkOption(string name, int instanceId, bool isLoaded)
        {
            Name = name;
            InstanceId = instanceId;
            IsLoaded = isLoaded;
            AutoDetectRoles();
        }

        public string Name { get; private set; }

        /// <summary>Integer value of the RevitLinkInstance element id.</summary>
        public int InstanceId { get; private set; }

        public bool IsLoaded { get; private set; }

        public bool UseForCeilings
        {
            get { return _useForCeilings; }
            set { SetProperty(ref _useForCeilings, value); }
        }

        public bool UseForMep
        {
            get { return _useForMep; }
            set { SetProperty(ref _useForMep, value); }
        }

        private void AutoDetectRoles()
        {
            if (!IsLoaded)
            {
                return;
            }

            string lower = (Name ?? string.Empty).ToLowerInvariant();
            _useForMep = ContainsAny(lower, MepHints);
            _useForCeilings = !_useForMep && ContainsAny(lower, CeilingHints);
        }

        private static bool ContainsAny(string text, string[] hints)
        {
            foreach (string hint in hints)
            {
                if (text.IndexOf(hint, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
