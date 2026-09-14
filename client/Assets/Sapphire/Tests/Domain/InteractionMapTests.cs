using NUnit.Framework;
using Sapphire.Domain.Grid;
using Sapphire.Domain.Interaction;

namespace Sapphire.Domain.Tests
{
    public class InteractionMapTests
    {
        [Test]
        public void TryGet_AfterRegister_ReturnsTrueAndCorrectId()
        {
            var map = new InteractionMap();
            var coord = new GridCoord(4, 5);
            var id = new InteractableId("npc_villager_01");

            map.Register(coord, id);
            bool found = map.TryGet(coord, out InteractableId result);

            Assert.IsTrue(found);
            Assert.AreEqual(id, result);
        }

        [Test]
        public void TryGet_UnregisteredCoord_ReturnsFalse()
        {
            var map = new InteractionMap();

            bool found = map.TryGet(new GridCoord(9, 9), out InteractableId result);

            Assert.IsFalse(found);
            Assert.AreEqual(InteractableId.None, result);
        }

        [Test]
        public void Register_OverwritesExistingEntryAtSameCoord()
        {
            var map = new InteractionMap();
            var coord = new GridCoord(1, 1);
            map.Register(coord, new InteractableId("chest_a"));
            map.Register(coord, new InteractableId("chest_b"));

            map.TryGet(coord, out InteractableId result);

            Assert.AreEqual(new InteractableId("chest_b"), result);
        }
    }
}
