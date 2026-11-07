using System;
using System.Linq;
using NUnit.Framework;
using Reborn.Core;

namespace Reborn.Tests
{
    /// <summary>Historia sintetica para verificar a arquitetura. Nao reproduz o jogo original.</summary>
    public sealed class CampaignRuntimeTests
    {
        private static CampaignScript SampleScript()
        {
            var opponents = new[] { new OpponentDefinition(7,"Duellist Test") };
            var nodes = new[]
            {
                new CampaignNode("intro","templo","Guia","Escolha um caminho.",-1,"","",new []
                {
                    new CampaignChoice("Explorar","explore",setsFlag:"explored"),
                    new CampaignChoice("Duelar","duel")
                }),
                new CampaignNode("explore","sala","Guia","Voce explorou.",-1,"","",new []
                {
                    new CampaignChoice("Voltar","intro"),
                    new CampaignChoice("Segredo","secret",requiresFlag:"explored")
                }),
                new CampaignNode("secret","sala","Guia","Caminho secreto.",-1,"","",new []
                {
                    new CampaignChoice("Ir ao duelo","duel")
                }),
                new CampaignNode("duel","arena","","Duelo de teste.",7,"win","lose",new CampaignChoice[0],winSetsFlag:"won"),
                new CampaignNode("win","final","Guia","Vitoria.",-1,"","",new CampaignChoice[0]),
                new CampaignNode("lose","final","Guia","Derrota.",-1,"","",new CampaignChoice[0])
            };
            return new CampaignScript("intro",nodes,opponents);
        }

        [Test] public void HistoriaComecaNoEventoCerto()
        {
            Assert.AreEqual("intro",new CampaignRuntime(SampleScript()).Current.Id);
        }
        [Test] public void EscolhaLevaAoDestinoCerto()
        {
            var c = new CampaignRuntime(SampleScript());
            Assert.IsTrue(c.TryChoose(0,out _));
            Assert.AreEqual("explore",c.Current.Id);
        }
        [Test] public void EscolhaPodeRegistrarFlag()
        {
            var c = new CampaignRuntime(SampleScript());
            c.TryChoose(0,out _);
            Assert.IsTrue(c.Flags.Contains("explored"));
            Assert.AreEqual(2,c.AvailableChoices().Count);
        }
        [Test] public void HistoriaNaoAvancaDuranteDueloPendente()
        {
            var c = new CampaignRuntime(SampleScript());
            c.TryChoose(1,out _);
            Assert.IsTrue(c.WaitingForDuel);
            Assert.IsFalse(c.TryChoose(0,out _));
            Assert.AreEqual("duel",c.Current.Id);
        }
        [Test] public void VitoriaSegueCaminhoProprio()
        {
            var c = new CampaignRuntime(SampleScript());
            c.TryChoose(1,out _);
            Assert.IsTrue(c.TryResolveDuel(true,out _));
            Assert.AreEqual("win",c.Current.Id);
            Assert.IsTrue(c.Flags.Contains("won"));
        }
        [Test] public void DerrotaSegueCaminhoDiferente()
        {
            var c = new CampaignRuntime(SampleScript());
            c.TryChoose(1,out _);
            Assert.IsTrue(c.TryResolveDuel(false,out _));
            Assert.AreEqual("lose",c.Current.Id);
        }
        [Test] public void NaoAceitaEventoDeDestinoInexistente()
        {
            var nodes = new[] { new CampaignNode("intro","","","",-1,"","",new []
                { new CampaignChoice("Vai","missing") }) };
            Assert.Throws<ArgumentException>(() => new CampaignScript("intro",nodes,new OpponentDefinition[0]));
        }
        [Test] public void ResetRetornaAoInicioELimpaFlags()
        {
            var c = new CampaignRuntime(SampleScript());
            c.TryChoose(0,out _);
            c.Reset();
            Assert.AreEqual("intro",c.Current.Id);
            Assert.AreEqual(0,c.Flags.Count);
        }
        [Test] public void EquipERitualSaoCartasDeSuporte()
        {
            var deck = new System.Collections.Generic.List<CardDefinition>();
            for (int i = 0; i < 40; i++)
                deck.Add(new CardDefinition(i.ToString("000"),"Carta " + i,
                    i == 0 ? CardKind.Equip : (i==1 ? CardKind.Ritual : CardKind.Monster),0,0,""));
            var session = new DuelSession(deck);
            Assert.IsFalse(session.TryPlace(0,FieldLane.Monster,0,out _));
            Assert.IsTrue(session.TryPlace(0,FieldLane.Support,0,out _));
            Assert.IsTrue(session.TryPlace(0,FieldLane.Support,1,out _));
        }
    }
}
