using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using BowlingGame.Infrastructure;
using FluentAssertions;
using NUnit.Framework;

namespace BowlingGame
{
    public class Game
    {
        private Frame[] frames = new Frame[10];
        private byte currentFrameNum = 0;

        public Game()
        {
            frames[9] = new Frame(null);
            for (int i = 8; i >= 0; i--)
                frames[i] = new Frame(frames[i + 1]);
        }


        public void Roll(int pins)
        {
            if (pins > 10 || pins < 0)
                throw new ArgumentException(nameof(pins));
            if (currentFrameNum >= 10) return;
            if (frames[currentFrameNum].IsFinished)
                currentFrameNum++;
            if (currentFrameNum == 10)
                return;
            frames[currentFrameNum].Roll(pins);
        }


        public int GetScore() => frames.Select(f => f.GetScore()).Sum();

        private class Frame
        {
            internal bool IsStrike { get; private set; }
            internal bool IsSpare { get; private set; }
            internal int[] Rolls { get; private set; }
            internal readonly Frame nextFrame;

            internal Frame(Frame nextFrame)
            {
                this.nextFrame = nextFrame;
                Rolls = nextFrame is null ? new int[3] : new int[2];
            }

            private byte currentRollNum = 0;

            internal void Roll(int pins)
            {
                if (IsFinished)
                    return;
                Rolls[currentRollNum++] = pins;
                if (Rolls.Any(r => r == 10) && nextFrame!=null)
                {
                    IsStrike = true;
                    IsFinished = true;
                    return;
                }

                if (nextFrame == null && Rolls[0] == 10 && Rolls[1] == 10)
                    IsFinished = true;
                if (currentRollNum != Rolls.Length) return;
                IsFinished = true;
                if (Rolls.Sum() == 10 && !IsStrike && nextFrame != null)
                    IsSpare = true;
            }

            internal int GetScore()
            {
                var score = Rolls.Sum();
                if (IsSpare && nextFrame != null)
                    score += nextFrame.Rolls[0];
                if (IsStrike && nextFrame != null)
                    score += nextFrame.Rolls.Sum() + (nextFrame.IsStrike ? nextFrame.nextFrame.Rolls[0] : 0);
                if (nextFrame == null && Rolls[0] == 10)
                {
                    score += Rolls[1] + Rolls[2];
                }

                if (nextFrame == null && Rolls[1] + Rolls[0] == 10)
                {
                    score += Rolls[2];
                }

                return score;
            }

            internal bool IsFinished = false;
        }
    }

    [TestFixture]
    public class Game_should : ReportingTest<Game_should>
    {
        private Game game;

        [SetUp]
        public void SetUp()
        {
            game = new Game();
        }

        [TearDown]
        public void TearDown()
        {
            game = null;
        }

        [Test]
        public void HaveZeroScore_BeforeAnyRolls()
        {
            new Game()
                .GetScore()
                .Should().Be(0);
        }

        [Test]
        public void HaveTheScore_TheSameWithTheFirstRoll()
        {
            game.Roll(1);
            game
                .GetScore()
                .Should()
                .Be(1);
        }

        [Test]
        public void ThrowsArgumentException_onMoreThan10Pins()
        {
            Assert.Throws<ArgumentException>(() => game.Roll(11));
        }

        [Test]
        public void ThrowsArgumentException_onNegativeNumbers()
        {
            Assert.Throws<ArgumentException>(() => game.Roll(-1));
        }

        [Test]
        public void ScoreDoestChange_After21Rolls()
        {
            for (int i = 0; i < 21; i++)
            {
                game.Roll(1);
            }

            var score = game.GetScore();
            game.Roll(1);
            game.GetScore().Should().Be(score);
        }

        [Test]
        public void GetBonusAfterSpareInNotLastFrame()
        {
            game.Roll(5);
            game.Roll(5);
            game.Roll(2);
            game.Roll(1);

            game.GetScore().Should().Be(15);
        }

        [Test]
        public void GetBonusAfterStrikeInNotLastFrame()
        {
            game.Roll(10);
            game.Roll(2);
            game.Roll(1);

            game.GetScore().Should().Be(16);
        }

        [Test]
        public void GetBonusAfter2StrikesInNotLastFrame()
        {
            game.Roll(10);
            game.Roll(10);
            game.Roll(1);
            game.Roll(1);
            game.GetScore().Should().Be(35);
        }
        [Test]
        public void CorrectWorkWith2StrikesInLastFrame()
        {
            for (int i = 0; i < 18; i++)
            {
                game.Roll(1);
            }

            game.Roll(10);
            game.Roll(10);
            game.GetScore().Should().Be(48);
        }
        [Test]
        public void CorrectWorkWithSpareInLastFrame()
        {
            for (int i = 0; i < 18; i++)
            {
                game.Roll(1);
            }

            game.Roll(5);
            game.Roll(5);
            game.Roll(1);
            game.GetScore().Should().Be(30);
        }
        [Test]
        public void PinsInFrameThrowsArgumentExceptionIfGreaterThan10()
        {
            game.Roll(6);
            Assert.Throws<ArgumentException>(() => game.Roll(6));
        }
    }
}