using System.Collections.Generic;
using NUnit.Framework;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class PhylogeneticTreeUITests
    {
        [Test]
        public void FormatTreeLines_Null_ReturnsNoSpeciesYet()
        {
            var lines = PhylogeneticTreeUI.FormatTreeLines(null);
            Assert.NotNull(lines);
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("No species yet.", lines[0]);
        }

        [Test]
        public void FormatTreeLines_Empty_ReturnsNoSpeciesYet()
        {
            var lines = PhylogeneticTreeUI.FormatTreeLines(new List<Species>());
            Assert.NotNull(lines);
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("No species yet.", lines[0]);
        }

        [Test]
        public void FormatTreeLines_SingleSpecies_ShowsOneLine()
        {
            var list = new List<Species> { new Species(0, -1, Genome.Default()) };
            var lines = PhylogeneticTreeUI.FormatTreeLines(list);
            Assert.AreEqual(1, lines.Count);
            Assert.That(lines[0], Does.Contain("Species 0"));
            Assert.That(lines[0], Does.Contain("pop: 0"));
            Assert.That(lines[0], Does.Contain("extinct"));
        }

        [Test]
        public void FormatTreeLines_ActiveSpecies_NoExtinctSuffix()
        {
            var s = new Species(0, -1, Genome.Default());
            s.Population = 5;
            var list = new List<Species> { s };
            var lines = PhylogeneticTreeUI.FormatTreeLines(list);
            Assert.AreEqual(1, lines.Count);
            Assert.That(lines[0], Does.Not.Contain("extinct"));
            Assert.That(lines[0], Does.Contain("pop: 5"));
        }

        [Test]
        public void FormatTreeLines_ParentAndChild_ChildIndented()
        {
            var parent = new Species(0, -1, Genome.Default());
            var child = new Species(1, 0, Genome.Default());
            var list = new List<Species> { parent, child };
            var lines = PhylogeneticTreeUI.FormatTreeLines(list);
            Assert.AreEqual(2, lines.Count);
            Assert.That(lines[0], Does.StartWith("Species 0"));
            Assert.That(lines[1], Does.StartWith("  Species 1"));
        }

        [Test]
        public void FormatTreeLines_ThreeLevels_IndentationReflectsDepth()
        {
            var g = Genome.Default();
            var list = new List<Species>
            {
                new Species(0, -1, g),
                new Species(1, 0, g),
                new Species(2, 1, g)
            };
            var lines = PhylogeneticTreeUI.FormatTreeLines(list);
            Assert.AreEqual(3, lines.Count);
            Assert.That(lines[0], Does.Not.Contain("  "));
            Assert.That(lines[1], Does.StartWith("  "));
            Assert.That(lines[2], Does.StartWith("    "));
        }
    }

    public class EvolutionStatsUITests
    {
        [Test]
        public void FormatSummary_Null_ReturnsZeroStats()
        {
            var s = EvolutionStatsUI.FormatSummary(null);
            Assert.That(s, Does.Contain("Total"));
            Assert.That(s, Does.Contain("Species: 0"));
        }

        [Test]
        public void FormatSummary_Empty_ReturnsZeroSpecies()
        {
            var s = EvolutionStatsUI.FormatSummary(new List<Species>());
            Assert.That(s, Does.Contain("Total organisms: 0"));
            Assert.That(s, Does.Contain("Species: 0"));
        }

        [Test]
        public void FormatSummary_OneSpecies_ShowsTotalAndCount()
        {
            var sp = new Species(0, -1, Genome.Default());
            sp.Population = 3;
            var list = new List<Species> { sp };
            var s = EvolutionStatsUI.FormatSummary(list);
            Assert.That(s, Does.Contain("Total organisms: 3"));
            Assert.That(s, Does.Contain("Species: 1"));
            Assert.That(s, Does.Contain("1 active"));
        }

        [Test]
        public void FormatSummary_ExtinctAndActive_CountsActiveCorrectly()
        {
            var a = new Species(0, -1, Genome.Default());
            a.Population = 2;
            var b = new Species(1, 0, Genome.Default());
            b.Population = 0;
            var list = new List<Species> { a, b };
            var s = EvolutionStatsUI.FormatSummary(list);
            Assert.That(s, Does.Contain("Total organisms: 2"));
            Assert.That(s, Does.Contain("Species: 2"));
            Assert.That(s, Does.Contain("1 active"));
            Assert.That(s, Does.Contain("1 extinct"));
        }

        [Test]
        public void FormatTraits_Null_ReturnsEmpty()
        {
            Assert.AreEqual("", EvolutionStatsUI.FormatTraits(null));
        }

        [Test]
        public void FormatTraits_Genome_ReturnsSpeedSizeAtkDef()
        {
            var g = Genome.Default();
            g.Speed = 2.5f;
            g.Size = 1.2f;
            var s = EvolutionStatsUI.FormatTraits(g);
            Assert.That(s, Does.Contain("Spd"));
            Assert.That(s, Does.Contain("Sz"));
            Assert.That(s, Does.Contain("Atk"));
            Assert.That(s, Does.Contain("Def"));
        }
    }
}
