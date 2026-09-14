using System;

namespace Sapphire.Domain.Interaction
{
    /// <summary>
    /// String-backed identifier for an interactable definition (NPC, chest, sign, etc.).
    /// </summary>
    public readonly struct InteractableId : IEquatable<InteractableId>
    {
        /// <summary>
        /// Explicit "no interactable" sentinel. Note this is identical to
        /// <c>default(InteractableId)</c> - the constructor's non-empty
        /// invariant only runs when the constructor is actually called, so
        /// <c>default</c> (e.g. an uninitialized field, or `out` params on a
        /// failed TryGet) bypasses that guard and produces a value with
        /// Value == null. Prefer this named constant over `default` so call
        /// sites read as an intentional sentinel rather than an unchecked gap.
        /// </summary>
        public static readonly InteractableId None = default;

        public readonly string Value;

        public InteractableId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("InteractableId value must not be null or empty.", nameof(value));
            }

            Value = value;
        }

        public bool Equals(InteractableId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is InteractableId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value != null ? Value.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(InteractableId a, InteractableId b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(InteractableId a, InteractableId b)
        {
            return !a.Equals(b);
        }
    }
}
