using NUnit.Framework;
using Xilo.Core;
using Xilo.Painting;

namespace Xilo.Tests
{
    public sealed class CoverageCalculatorTests
    {
        static byte[] Pixels(params byte[] grays)
        {
            var rgba = new byte[grays.Length * 4];
            for (int i = 0; i < grays.Length; i++)
            {
                rgba[i * 4] = grays[i];
                rgba[i * 4 + 1] = grays[i];
                rgba[i * 4 + 2] = grays[i];
                rgba[i * 4 + 3] = 255;
            }
            return rgba;
        }

        [Test]
        public void DarkTargetSelectsDarkPixels()
        {
            bool[] target = CoverageCalculator.BuildTarget(Pixels(0, 255, 10, 200), CoverageTarget.ArtDark);

            CollectionAssert.AreEqual(new[] { true, false, true, false }, target);
        }

        [Test]
        public void LightTargetIsInverseOfDark()
        {
            bool[] target = CoverageCalculator.BuildTarget(Pixels(0, 255, 10, 200), CoverageTarget.ArtLight);

            CollectionAssert.AreEqual(new[] { false, true, false, true }, target);
        }

        [Test]
        public void FullSurfaceTargetsEverything()
        {
            bool[] target = CoverageCalculator.BuildTarget(Pixels(0, 255), CoverageTarget.FullSurface);

            CollectionAssert.AreEqual(new[] { true, true }, target);
        }

        [Test]
        public void CoverageCountsOnlyTargetPixels()
        {
            bool[] target = { true, true, false, false };
            byte[] mask = Pixels(255, 0, 255, 255);

            Assert.AreEqual(0.5f, CoverageCalculator.Compute(mask, target), 1e-5f);
        }

        [Test]
        public void EmptyMaskHasZeroCoverage()
        {
            Assert.AreEqual(0f, CoverageCalculator.Compute(Pixels(0, 0, 0), CoverageCalculator.BuildFullTarget(3)));
        }

        [Test]
        public void TargetWithoutPixelsCountsAsComplete()
        {
            Assert.AreEqual(1f, CoverageCalculator.Compute(Pixels(0, 0), new[] { false, false }));
        }
    }
}
