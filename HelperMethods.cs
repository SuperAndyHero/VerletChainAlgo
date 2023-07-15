using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerletChainAlgo
{
    public static class HelperMethods
    {
        public static Vector2 RotatedBy(this Vector2 spinningpoint, double radians, Vector2 center = default(Vector2))
        {
            float num = (float)Math.Cos(radians);
            float num2 = (float)Math.Sin(radians);
            Vector2 vector = spinningpoint - center;
            Vector2 result = center;
            result.X += vector.X * num - vector.Y * num2;
            result.Y += vector.X * num2 + vector.Y * num;
            return result;
        }
        public static float ToRotation(this Vector2 v)
        {
            return (float)Math.Atan2(v.Y, v.X);
        }
        const string pubOverride = "public override";

        public static string CreateVector2Text(Vector2 vec) =>
            "new Vector2(" + vec.X + "f, " + vec.Y + "f)";

        public static string BuildOutputText(ChainInfo info)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(pubOverride + " string Texture => \"" + SimInfo.TexturePath + "\"");                          sb.AppendLine(";");
            sb.Append(pubOverride + " Vector2 WorldOffset => " + CreateVector2Text(info.ChainOffset));              sb.AppendLine(";");
            sb.Append(pubOverride + " float Width => " + info.Width);                                               sb.AppendLine("f;");
            sb.Append(pubOverride + " Vector2 TexPosOffset => " + CreateVector2Text(info.SpriteMinSizeOffset + Game1.SpritePosTempOffset));        sb.AppendLine(";");
            sb.Append(pubOverride + " Vector2 TexSizeOffset => " + CreateVector2Text(info.SpriteMaxSizeOffset + Game1.SpriteSizeTempOffset));         sb.AppendLine(";");
            sb.Append(pubOverride + " int PhysicsRepetitions => " + info.PhysicsRepetitions);                       sb.AppendLine(";");
            sb.Append(pubOverride + " float VertexDrag => " + info.VertexDrag);                                     sb.AppendLine("f;");
            sb.Append(pubOverride + " int VertexCount => " + info.vertexCount);                                     sb.AppendLine(";");

            sb.AppendLine(pubOverride + " float[] VertexDistances => new float[] { ");
            for (int i = 0; i < info.VertexDistanceArray.Length; i++)
            {
                sb.Append("\t\t\t");  sb.AppendLine(info.VertexDistanceArray[i].ToString() + ((i == info.VertexDistanceArray.Length - 1) ? "f" : "f, "));
            }
            sb.AppendLine("\t\t};");

            sb.AppendLine(pubOverride + " Vector2[] VertexGravityForces => new Vector2[] { ");
            for (int i = 0; i < Game1.VertexGravityArray.Length; i++)
            {
                sb.Append("\t\t\t");  sb.AppendLine(CreateVector2Text(Game1.VertexGravityArray[i]) + ((i == Game1.VertexGravityArray.Length - 1) ? "" : ", "));
            }
            sb.AppendLine("\t\t};");

            sb.AppendLine(pubOverride + " Vector2[] SettledPoints => new Vector2[] { ");
            for (int i = 0; i < Game1.SettledPoints.Length; i++)
            {
                sb.Append("\t\t\t");  sb.AppendLine(CreateVector2Text(Game1.SettledPoints[i]) + ((i == Game1.SettledPoints.Length - 1) ? "" : ", "));
            }
            sb.AppendLine("\t\t};");

            return sb.ToString();
        }
    }
}
