using NUnit.Framework;
using System;
using MtcgServer.Cards;
using MtcgServer.Cards.MonsterCards;
using MtcgServer.Cards.SpellCards;

namespace MtcgServerTests
{
    public class CardTest
    {
        [Test]
        public void TestFireVSWaterSpellCards()
        {
            var fire = new FireSpell();
            var water = new WaterSpell();

            int fireDamage = fire.CalculateDamage(water);
            int waterDamage = water.CalculateDamage(fire);

            // water > fire
            Assert.AreEqual(20, fireDamage);
            Assert.AreEqual(40, waterDamage);
        }

        [Test]
        public void TestFireVSNormalSpellCards()
        {
            var fire = new FireSpell();
            var normal = new NormalSpell();

            int fireDamage = fire.CalculateDamage(normal);
            int normalDamage = normal.CalculateDamage(fire);

            // fire > normal
            Assert.AreEqual(40, fireDamage);
            Assert.AreEqual(20, normalDamage);
        }

        [Test]
        public void TestNormalVSWaterSpellCards()
        {
            var normal = new NormalSpell();
            var water = new WaterSpell();

            int normalDamage = normal.CalculateDamage(water);
            int waterDamage = water.CalculateDamage(normal);

            // normal > water
            Assert.AreEqual(40, normalDamage);
            Assert.AreEqual(20, waterDamage);
        }
    }
}
