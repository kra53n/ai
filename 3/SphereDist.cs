/// <summary>
/// https://en.wikipedia.org/wiki/Taxicab_geometry#Spheres
/// </summary>
public class Sphere
{
    public static int Dist((byte x, byte y) p, (byte x, byte y) c)
    {
        return Math.Abs(p.x - c.x) + Math.Abs(p.y - c.y);
    }
}