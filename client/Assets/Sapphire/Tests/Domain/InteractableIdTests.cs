using System;
using NUnit.Framework;
using Sapphire.Domain.Interaction;

namespace Sapphire.Domain.Tests
{
    public class InteractableIdTests
    {
        [Test]
        public void Constructor_NullValue_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new InteractableId(null));
        }

        [Test]
        public void Constructor_EmptyValue_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new InteractableId(string.Empty));
        }

        [Test]
        public void Constructor_ValidValue_SetsValue()
        {
            var id = new InteractableId("npc_villager_01");

            Assert.AreEqual("npc_villager_01", id.Value);
        }

        [Test]
        public void None_HasNullValueAndEqualsDefault()
        {
            Assert.AreEqual(default(InteractableId), InteractableId.None);
            Assert.IsNull(InteractableId.None.Value);
        }
    }
}
