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
        public static int interationsPerUpdate = 250;//lower this if your getting lag
        public static int chainUpdatesPerIter = 3;//keep this between 3 and 10, anything above will not have any effect

        public static float DefaultDrawScale => 14f;
        public static Vector2 DefaultDrawOffset => Vector2.Zero;//-Game1.viewSize / 2.5f;//offsets everything on the screen (you can also move the camera)

        public static bool DrawSprite => true;
        public static string TexturePath => "Path/To/Png/Here.png";
        public static Vector2 BackSpriteOffset => new Vector2(0, -16f);
        public static float BackSpriteScale => 1f;
    }

    public class ChainInfo
    {
        //these 3 variables dont matter much, just keep them the same as your chain
        public float VertexDrag = 1.15f;
        public int PhysicsRepetitions = 5;//this effects how this chain acts in real-time, if you wish to change sim speed change the values in SimInfo instead


        public Vector2 ChainOffset = new Vector2(0, 0);//the origin of the chain

        public float MinimumGravStrength => 0.7f;//this increases the rigidness of the chain, the longer the chain the lower this should be
        public float MaximumGravStrength => 1000f;//this increases the rigidness of the chain, the longer the chain the lower this should be
        public float MaxAverageStrength => vertexCount * 3f;//the max average strength, keep this between vertexCount x1 and x3

        public static float TotalLengthMult => 0.93f;//allows you scale the length of the entire chain, useful if the forces are stretching it out too far

        public float Width => 9;
        public Vector2 TailSpriteSize => new Vector2(0.2f, 17.6f);//rename
        public Vector2 TailSpriteOffset => new Vector2(0, -18.70f);//rename

        public List<VertexOverride> ConstraintOverrides => new List<VertexOverride>//this is for setting fine tuning a chain
        {
            //new VertexOverride()
            //{
            //    index = 12,
            //    setY = 1f
            //},
            //new VertexOverride()
            //{
            //    index = 7,
            //    setX = 1f
            //},
            //new VertexOverride()
            //{
            //    index = 1 ,
            //    minStr = 1.4f,
            //},
            //new VertexOverride()
            //{
            //    index = 2,
            //    minStr = 1.2f 
            //},
            //new VertexOverride()
            //{
            //    index = 4,
            //    minStr = 0.8f,
            //    setX = 0.5f,
            //},
        };

        //dont change these if using build mode
        public int vertexCount = 9;
        public float vertexDefaultDistance = 9;
        /// <summary>
        /// this is for changing each length on its own in order to get close to the desired shape
        /// copy these over to your chain when done
        /// leave null to have each use the defualt distance value
        /// </summary>
        public float[] VertexDistanceArray = new float[] {
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
    }

    public class VertexOverride
    {
        public int index = int.MaxValue;
        public float? setX = null;
        public float? setY = null;
        public float? minStr = null;
        public float? maxStr = null;
    }
}
