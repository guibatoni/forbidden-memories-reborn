using NUnit.Framework;
using Reborn.Core;

namespace Reborn.Tests
{
    public sealed class DuelSessionTests
    {
        [Test]
        public void ComecaComMaoDeCincoEDeckDeTrintaECinco()
        {
            var state = new DuelSession(DemoDeck.Create());
            Assert.AreEqual(5,state.Hand.Count);
            Assert.AreEqual(35,state.DeckCount);
        }

        [Test]
        public void MonstroEntraEmZonaDeMonstro()
        {
            var state = new DuelSession(DemoDeck.Create());
            Assert.IsTrue(state.TryPlace(0,FieldLane.Monster,2,out _));
            Assert.AreEqual("p01",state.At(FieldLane.Monster,2).Id);
            Assert.AreEqual(4,state.Hand.Count);
        }

        [Test]
        public void MonstroNaoPodeOcuparSuporte()
        {
            var state = new DuelSession(DemoDeck.Create());
            Assert.IsFalse(state.TryPlace(0,FieldLane.Support,2,out _));
            Assert.IsNull(state.At(FieldLane.Support,2));
            Assert.AreEqual(5,state.Hand.Count);
        }

        [Test]
        public void NaoSobrescreveZonaOcupada()
        {
            var state = new DuelSession(DemoDeck.Create());
            Assert.IsTrue(state.TryPlace(0,FieldLane.Monster,0,out _));
            Assert.IsFalse(state.TryPlace(0,FieldLane.Monster,0,out _));
            Assert.AreEqual(4,state.Hand.Count);
        }

        [Test]
        public void ProximoTurnoSandboxRecompoeAteCinco()
        {
            var state = new DuelSession(DemoDeck.Create());
            state.TryPlace(0,FieldLane.Monster,1,out _);
            state.NextSandboxTurn();
            Assert.AreEqual(2,state.Turn);
            Assert.AreEqual(5,state.Hand.Count);
            Assert.AreEqual(34,state.DeckCount);
        }

        [Test]
        public void ResetLimpaCampoEReiniciaValores()
        {
            var state = new DuelSession(DemoDeck.Create());
            state.TryPlace(0,FieldLane.Monster,1,out _);
            state.Reset();
            Assert.IsNull(state.At(FieldLane.Monster,1));
            Assert.AreEqual(5,state.Hand.Count);
            Assert.AreEqual(8000,state.PlayerLife);
        }
    }
}
