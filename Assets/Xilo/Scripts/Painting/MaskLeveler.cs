namespace Xilo.Painting
{
    // Levels a mask where another mask was painted: each pixel there becomes fully white or fully black (RGBA32).
    // Plain arrays only, so it can run on a worker thread.
    public static class MaskLeveler
    {
        // Changes the target in place; returns how many pixels changed.
        public static int Level(byte[] painted, byte[] target, byte paintedThreshold, byte levelThreshold)
        {
            int changed = 0;
            for (int i = 0; i + 3 < target.Length && i + 3 < painted.Length; i += 4)
            {
                if (painted[i] < paintedThreshold)
                    continue;

                byte value = target[i] >= levelThreshold ? (byte)255 : (byte)0;
                if (target[i] == value && target[i + 1] == value && target[i + 2] == value && target[i + 3] == value)
                    continue;

                target[i] = target[i + 1] = target[i + 2] = target[i + 3] = value;
                changed++;
            }
            return changed;
        }
    }
}
