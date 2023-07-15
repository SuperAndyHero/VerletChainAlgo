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
        public static string TexturePath => "D:\\Documents\\My Games\\Terraria\\tModLoader\\ModSources\\TailLibExample\\Tails\\ExampleTail.png";//"Path/To/Png/Here.png";
        public static Vector2 BackSpriteOffset => new Vector2(0, -6f);
        public static float BackSpriteScale => 1f;
    }

    public class ChainInfo
    {
        //these 3 variables dont matter much, just keep them the same as your chain
        public float VertexDrag = 1.15f;
        public int PhysicsRepetitions = 5;//this effects how this chain acts in real-time, if you wish to change sim speed change the values in SimInfo instead


        public Vector2 ChainOffset = new Vector2(0, 0);//the origin of the chain

        public float MinimumGravStrength => 0.3f;//this increases the rigidness of the chain, the longer the chain the lower this should be
        public float MaximumGravStrength => 1f;
        public float MaxAverageStrength => vertexCount * 3f;//the max average strength, keep this between vertexCount x1 and x3 for simple chains, or higher for complex ones

        public static float TotalLengthMult => 0.97f;//allows you scale the distances between every point in chain, useful to account for forces stretching it out too far

        public float Width => 9.75f;//width of tail, outwards at a right angle to the chain
        public Vector2 SpriteMaxSizeOffset => new Vector2(6f, 12.2f);//offsets the largest edge of the sprite 
        public Vector2 SpriteMinSizeOffset => new Vector2(0f, 0.80f);//offsets the smallest edge of the sprite (good for aligning bottom edge)

        public List<VertexOverride> ConstraintOverrides => new List<VertexOverride>//this is for fine tuning a chain
        {
            new VertexOverride()
            {
                index = 6,
                maxStr = 1.1f
            },
            //new VertexOverride()
            //{
            //    index = 4,
            //    maxStr = 1.3f,
            //},
            //new VertexOverride()
            //{
            //    index = 3,
            //    setX = -0.2f
            //},
            //new VertexOverride()
            //{
            //    index = 1 ,
            //    minStr = 1.4f,
            //},

            //new VertexOverride()
            //{
            //    index = 6,
            //    setX = 0.75f,
            //    setY = 0f
            //},
            //            new VertexOverride()
            //{
            //    index = 5,
            //    setX = 1f,
            //    setY = -0.2f
            //}
        };

        #region dont change these if using build mode
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
        public Vector2[] WantedVertexPoints = new Vector2[] {//default, is defined when running
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
        #endregion
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
