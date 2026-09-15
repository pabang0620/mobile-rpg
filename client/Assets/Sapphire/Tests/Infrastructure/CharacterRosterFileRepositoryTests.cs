using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Sapphire.Domain.Character;
using Sapphire.Infrastructure.Character;

namespace Sapphire.Infrastructure.Tests
{
    /// <summary>
    /// Save/load round-trip against the real Application.persistentDataPath
    /// (EditMode tests run inside the Editor process, which has one), using a
    /// throwaway account id per test so runs never collide with a real save
    /// file. Each test deletes its own file in teardown.
    /// </summary>
    public class CharacterRosterFileRepositoryTests
    {
        private const string TestAccountId = "sapphire_test_account_roundtrip";
        private CharacterRosterFileRepository repository;

        [SetUp]
        public void SetUp()
        {
            repository = new CharacterRosterFileRepository();
            DeleteTestFile();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestFile();
        }

        [Test]
        public void Load_NoFileYet_ReturnsEmpty()
        {
            IReadOnlyList<CharacterSlot> loaded = repository.Load(TestAccountId);

            Assert.AreEqual(0, loaded.Count);
        }

        [Test]
        public void SaveThenLoad_RoundTripsAllFieldsForEverySlot()
        {
            var slots = new List<CharacterSlot>
            {
                new CharacterSlot("id-1", "달빛법사", CharacterClass.Mage, 3),
                new CharacterSlot("id-2", "강철전사", CharacterClass.Warrior, 1),
            };

            repository.Save(TestAccountId, slots);
            IReadOnlyList<CharacterSlot> loaded = repository.Load(TestAccountId);

            Assert.AreEqual(2, loaded.Count);
            Assert.AreEqual("id-1", loaded[0].Id);
            Assert.AreEqual("달빛법사", loaded[0].Name);
            Assert.AreEqual(CharacterClass.Mage, loaded[0].Class);
            Assert.AreEqual(3, loaded[0].Level);
            Assert.AreEqual("id-2", loaded[1].Id);
            Assert.AreEqual("강철전사", loaded[1].Name);
            Assert.AreEqual(CharacterClass.Warrior, loaded[1].Class);
            Assert.AreEqual(1, loaded[1].Level);
        }

        [Test]
        public void Save_EmptyList_RoundTripsToEmpty()
        {
            repository.Save(TestAccountId, new List<CharacterSlot>());

            IReadOnlyList<CharacterSlot> loaded = repository.Load(TestAccountId);

            Assert.AreEqual(0, loaded.Count);
        }

        [Test]
        public void SaveThenSaveAgain_OverwritesRatherThanAppends()
        {
            repository.Save(TestAccountId, new List<CharacterSlot> { new CharacterSlot("id-1", "첫캐릭터", CharacterClass.Mage, 1) });
            repository.Save(TestAccountId, new List<CharacterSlot> { new CharacterSlot("id-2", "두번째캐릭터", CharacterClass.Warrior, 1) });

            IReadOnlyList<CharacterSlot> loaded = repository.Load(TestAccountId);

            Assert.AreEqual(1, loaded.Count);
            Assert.AreEqual("id-2", loaded[0].Id);
        }

        private static void DeleteTestFile()
        {
            string path = Path.Combine(Application.persistentDataPath, "characters_" + TestAccountId + ".json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
