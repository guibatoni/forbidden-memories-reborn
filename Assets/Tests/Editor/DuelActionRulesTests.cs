using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reborn.Core;

namespace Reborn.Tests
{
    public sealed class DuelActionRulesTests
    {
        private static DuelSession NewSession() => new DuelSession(DemoDeck.Create());

        [Test]
        public void CadaLadoTemCincoPosicoesPorFileira()
        {
            Assert.AreEqual(5, DuelSession.ZoneCount);
            var session = NewSession();
            Assert.AreEqual(5, session.AvailableSlots(0, FieldLane.Monster).Count);
            Assert.AreEqual(0, session.AvailableSlots(0, FieldLane.Support).Count);
        }

        [Test]
        public void MagiaSoPodeEntrarEmSuporte()
        {
            var session = NewSession();
            Assert.AreEqual(0, session.AvailableSlots(4, FieldLane.Monster).Count);
            Assert.AreEqual(5, session.AvailableSlots(4, FieldLane.Support).Count);
            Assert.IsTrue(session.TryPlace(4, FieldLane.Support, 2, out _));
        }

        [Test]
        public void ZonaOcupadaNaoConstaNasPosicoesLegais()
        {
            var session = NewSession();
            Assert.IsTrue(session.TryPlace(0, FieldLane.Monster, 3, out _));
            var positions = session.AvailableSlots(0, FieldLane.Monster);
            Assert.AreEqual(4, positions.Count);
            CollectionAssert.DoesNotContain(positions, 3);
        }

        [Test]
        public void TentativaInvalidaNaoGeraEventoNemAlteraEstado()
        {
            var session = NewSession();
            int initialEvents = session.EventLog.Count;
            Assert.IsFalse(session.TryPlace(0, FieldLane.Support, 0, out _));
            Assert.AreEqual(initialEvents, session.EventLog.Count);
            Assert.AreEqual(5, session.Hand.Count);
            Assert.IsNull(session.At(FieldLane.Support, 0));
        }

        [Test]
        public void ColocacaoGeraEventoComSequenciaEPosicao()
        {
            var session = NewSession();
            DuelEvent received = null;
            session.EventEmitted += evt => received = evt;
            Assert.IsTrue(session.TryPlace(0, FieldLane.Monster, 1, out _));
            Assert.IsNotNull(received);
            Assert.AreEqual(DuelEventKind.CardPlaced, received.Kind);
            Assert.AreEqual("p01", received.CardId);
            Assert.AreEqual(FieldLane.Monster, received.Lane);
            Assert.AreEqual(1, received.Slot);
            Assert.AreEqual(session.EventLog[session.EventLog.Count - 1].Sequence, received.Sequence);
        }

        [Test]
        public void RemocaoSandboxLiberaZonaESinalizaEvento()
        {
            var session = NewSession();
            session.TryPlace(0, FieldLane.Monster, 0, out _);
            Assert.IsTrue(session.TryRemoveSandbox(FieldLane.Monster, 0, out _));
            Assert.IsNull(session.At(FieldLane.Monster, 0));
            Assert.AreEqual(DuelEventKind.CardRemoved,
                session.EventLog[session.EventLog.Count - 1].Kind);
        }

        [Test]
        public void ResetRestauraEstadoEEventosComSequenciaDeterministica()
        {
            var session = NewSession();
            session.TryPlace(0, FieldLane.Monster, 0, out _);
            session.Reset();
            Assert.AreEqual(DuelEventKind.Reset, session.EventLog[0].Kind);
            Assert.AreEqual(1, session.EventLog[0].Sequence);
            Assert.AreEqual(6, session.EventLog.Count); // reset + cinco cartas compradas
            Assert.AreEqual(5, session.Hand.Count);
        }
    }
}
