using System.Collections.Generic;

namespace Xilo.Painting
{
    // Finds tiny dark spots in an RGBA32 mask: small groups of pixels below the threshold (4-connected), also those
    // touching the mask border. Plain arrays only, so it can run on a worker thread.
    public static class SpeckFinder
    {
        // Writes alpha 255 into the patch (RGBA32, same size) for every speck pixel; returns how many were found.
        public static int Find(byte[] rgba, int width, int height, byte threshold, int maxArea, byte[] patch)
        {
            var visited = new bool[width * height];
            var region = new List<int>(maxArea + 1);
            var stack = new Stack<int>();
            int found = 0;

            for (int start = 0; start < visited.Length; start++)
            {
                if (visited[start] || !IsDark(rgba, start, threshold))
                    continue;

                if (Flood(rgba, width, height, threshold, maxArea, start, visited, region, stack) > maxArea)
                    continue;

                foreach (int pixel in region)
                    patch[pixel * 4 + 3] = 255;
                found += region.Count;
            }
            return found;
        }

        // Visits the whole dark region from start, keeping its first maxArea pixels; returns its full area.
        static int Flood(byte[] rgba, int width, int height, byte threshold, int maxArea, int start,
            bool[] visited, List<int> region, Stack<int> stack)
        {
            region.Clear();
            stack.Clear();
            stack.Push(start);
            visited[start] = true;
            int area = 0;

            while (stack.Count > 0)
            {
                int pixel = stack.Pop();
                area++;
                if (area <= maxArea)
                    region.Add(pixel);

                int x = pixel % width;
                int y = pixel / width;

                TryPush(rgba, threshold, x > 0, pixel - 1, visited, stack);
                TryPush(rgba, threshold, x < width - 1, pixel + 1, visited, stack);
                TryPush(rgba, threshold, y > 0, pixel - width, visited, stack);
                TryPush(rgba, threshold, y < height - 1, pixel + width, visited, stack);
            }
            return area;
        }

        static void TryPush(byte[] rgba, byte threshold, bool inside, int pixel, bool[] visited, Stack<int> stack)
        {
            if (!inside || visited[pixel] || !IsDark(rgba, pixel, threshold))
                return;
            visited[pixel] = true;
            stack.Push(pixel);
        }

        static bool IsDark(byte[] rgba, int pixel, byte threshold) => rgba[pixel * 4] < threshold;
    }
}
