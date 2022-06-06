using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using VerletChainAlgo;

namespace VerletChainAlgo
{
    public static class SimInfo
    {
        //all of these variables do not effect the shape of the chain, but change how fast its simulated / how its displayed
        public static int interationsPerUpdate = 500;//lower this if your getting lag
        public static int chainUpdatesPerIter = 3;//keep this between 3 and 10, anything above will not have any effect

        public static float DefaultDrawScale => 14f;
        public static Vector2 DefaultDrawOffset => Vector2.Zero;//-Game1.viewSize / 2.5f;//offsets everything on the screen (you can also move the camera)

        public static string TexturePath => "D:/Documents/My Games/Terraria/tModLoader/ModSources/TailVanities/Tails/Fox.png";
        public static Vector2 SpriteOffset => new Vector2(0, -8f);
        public static float SpriteScale => 1f;
    }

    public class ChainInfo
    {
        public int vertexCount = 9;

        public int vertexDefaultDistance = 9;

        public float minimumGravStrength = 2f;//this increases the rigidness of the chain, but can have negative effects when left to run too long

        //these 3 variables dont matter much, just keep them the same as your chain
        public float VertexDrag = 1.15f;
        public int PhysicsRepetitions = 5;
        public Vector2 VertexGravityMult = Vector2.One;

        /// <summary>
        /// this is for changing each length on its own in order to get close to the desired shape
        /// copy these over to your chain when done
        /// leave null to have each use the defualt distance value
        /// </summary>
        public int[] VertexDistanceArray => new int[] {
        11,
        10,
        10,
        9,
        9,
        9,
        9,
        10,
        9 //last number doesn't matter
        };

        /// <summary>
        /// This is the shape it is aiming for, each point uses the previous point at the starting location
        /// </summary>
        public Vector2[] WantedVertexPoints = new Vector2[] {
                new Vector2(0, 0),

                new Vector2(11, 2) * new Vector2(1, -1),

                new Vector2(9, 4) * new Vector2(1, -1),

                new Vector2(7, 7) * new Vector2(1, -1),

                new Vector2(-2, 9) * new Vector2(1, -1),

                new Vector2(-6, 7) * new Vector2(1, -1),

                new Vector2(-9, -2) * new Vector2(1, -1),

                new Vector2(-7, -6) * new Vector2(1, -1),

                new Vector2(3, -9) * new Vector2(1, -1)
        };

        public Vector2 ChainStartOffset = new Vector2(0, 0);//the origin of the chain
    }
}
