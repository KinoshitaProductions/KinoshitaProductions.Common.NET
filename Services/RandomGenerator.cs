namespace KinoshitaProductions.Common.Services;

public static class RandomGenerator
{
    private static readonly Random Random = new ((int)DateTime.Now.Ticks);
    public static int GetRandom(int minValue, int maxValuePlusOne)
    {
        lock(Random)
        {
            return Random.Next(Math.Min(minValue, maxValuePlusOne - 1), Math.Max(minValue + 1, maxValuePlusOne));
        }
    }
}
