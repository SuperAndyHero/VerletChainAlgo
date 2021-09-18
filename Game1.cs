using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace VerletChainAlgo
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        VerletChainInstance chain;
        public VertexBuffer spineBuffer;
        public VertexBuffer shapeBuffer;
        public Vector2 viewSize;
        public Texture2D grid;
        public static SpriteFont font_Arial;
        public static BasicEffect basicEffect;
        public static Random rand;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            //this.Window.IsBorderless = true;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        public Vector2 position;

        public int vertexCount = 9;

        public int vertexDistance = 9;
        public int[] vertexDistanceArray => new int[] { 
        11,
        10,
        10,
        9,
        9,
        9,
        9,
        10,
        9
        };//last number does not matter

        public Vector2 VertexGravityMult = Vector2.One;

        public Vector2[] VertexGravityArray = new Vector2[] {
                new Vector2(0, 0),

                new Vector2(1.5f, 0.5f),

                new Vector2(0.72f, 0.1f),

                new Vector2(0.80f, -0.12f),

                new Vector2(0.19f, -0.98f),

                new Vector2(-0.65f, -0.75f),

                new Vector2(-0.98f, 0.19f),

                new Vector2(-0.75f, 0.65f),

                new Vector2(-0.28f, 0.95f)
        };

        public Vector2[] NextWantedVertexGravityArray = new Vector2[] {
                new Vector2(0, 0),

                new Vector2(1, -1),

                new Vector2(0, 0),

                new Vector2(0, 0),

                new Vector2(0, 0),

                new Vector2(0, 0),

                new Vector2(0, 0),

                new Vector2(0, 0),

                new Vector2(1, -1)
        };

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

        public Vector2[] SettledPointArray = null;

        public float VertexDrag = 1.15f;

        public int PhysicsRepetitions = 5;

        public int facingDirection = 1; //1, -1

        protected override void Initialize()
        {
            viewSize = new Vector2(_graphics.GraphicsDevice.Viewport.Width, _graphics.GraphicsDevice.Viewport.Height);
            position = viewSize / 2;
            spineBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), vertexCount, BufferUsage.WriteOnly);
            shapeBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), vertexCount, BufferUsage.WriteOnly);
            basicEffect = new BasicEffect(_graphics.GraphicsDevice)
            {
                VertexColorEnabled = true,
            };
            basicEffect.Projection = Matrix.CreateOrthographic(viewSize.X, viewSize.Y, 0, 1000);
            rand = new Random(123456789);

            SetupChain();

            base.Initialize();
        }

        public void SetupChain()
        {
            bool distances = vertexDistanceArray != null;
            bool gravities = VertexGravityArray != null;

            Vector2[] settledPoints;
            if (SettledPointArray != null)
                settledPoints = SettledPointArray;
            else
            {
                settledPoints = new Vector2[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    int dist = distances ? vertexDistanceArray[i] : vertexDistance;
                    Vector2 grav = gravities ? VertexGravityArray[i] * VertexGravityMult : VertexGravityMult;
                    settledPoints[i] = (grav * dist) + (i > 0 ? settledPoints[i - 1] : Vector2.Zero);
                }
            }


            chain = new VerletChainInstance(vertexCount, position + chainStartOffset,
                position + ((settledPoints[vertexCount - 1]) * facingDirection), vertexDistance, VertexGravityMult * facingDirection,
                gravities, VertexGravityArray?.ToList(), distances, vertexDistanceArray?.ToList())
            {
                drag = VertexDrag,
                constraintRepetitions = PhysicsRepetitions
            };
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            grid = Content.Load<Texture2D>("Grid");
            font_Arial = Content.Load<SpriteFont>("Arial");


            VertexPositionColor[] vertexPos = new VertexPositionColor[shapeBuffer.VertexCount];
            Vector2 combinedPos = Vector2.Zero;

            for (int i = 0; i < vertexPos.Length; i++)
            {
                combinedPos += WantedVertexPoints[i];
                vertexPos[i] = new VertexPositionColor(new Vector3(combinedPos, 0), Color.Red);
            }

            shapeBuffer.SetData(vertexPos);
        }

        //public float closenessValue

        public int interationsPerUpdate = 500;
        public int chainUpdatesPerIter = 2;

        public float lastClosenessValue = float.MaxValue - 1;
        public float closenessValue = float.MaxValue;

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                SetupChain();

            for (int i = 0; i < interationsPerUpdate; i++)
            {
                AdjustChainValues();

                for (int j = 0; j < chainUpdatesPerIter; j++)
                    chain.UpdateChain();

                CalculateCloseness();
            }

            base.Update(gameTime);
        }

        public void AdjustChainValues()
        {
            if (closenessValue < lastClosenessValue)
            {
                for (int i = 0; i < vertexCount; i++)
                    VertexGravityArray[i] = NextWantedVertexGravityArray[i];
            }

            for (int i = 0; i < vertexCount; i++)
                NextWantedVertexGravityArray[i] = VertexGravityArray[i];

            int index = rand.Next(1, vertexCount);
                NextWantedVertexGravityArray[index] = GiveRandomOffset(VertexGravityArray[index], index);

            chain.forceGravities = NextWantedVertexGravityArray.ToList();
        }

        public Vector2 GiveRandomOffset(Vector2 vec, int i)
        {
            //if (rand.Next(2) == 0)
                return vec + (new Vector2((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f) * 0.001f);
            //if (rand.Next(2) == 0)//breaks
            //{
            //    float mult = (Math.Abs(i - vertexCount) / vertexCount);
            //    return vec * (1f + ((float)rand.NextDouble() - 0.5f * 0.001f));
            //}
            //return vec;
        }

        public void CalculateCloseness()
        {
            lastClosenessValue = closenessValue;
            closenessValue = 0;
            Vector2 combinedPos = Vector2.Zero;

            for (int i = 0; i < vertexCount; i++)
            {
                combinedPos += WantedVertexPoints[i];
                Vector2 segPos = chain.ropeSegments[i].posNow;
                closenessValue += Vector2.Distance(combinedPos + chain.startPoint, segPos);
            }

            closenessValue /= vertexCount - 1;
        }

        public Color SpineColor(int i)
        {
            Random rand = new Random(i);
            return new Color(rand.Next(255), rand.Next(255), rand.Next(255));
        }

        public void DrawGeometry(BasicEffect effect, EffectPass pass, GraphicsDevice graphicsDevice)
        {
            VertexPositionColor[] vertexPos = new VertexPositionColor[spineBuffer.VertexCount];

            for (int i = 0; i < vertexPos.Length; i++)
                vertexPos[i] = new VertexPositionColor(new Vector3(chain.ropeSegments[i].posNow - viewSize / 2, 0), SpineColor(i));

            spineBuffer.SetData(vertexPos);

            effect.TextureEnabled = false;//this or a second effect is needed
            effect.View = Matrix.CreateScale(chainScale, -chainScale, chainScale) * Matrix.CreateTranslation(new Vector3(everythingOffset, 0));

            pass.Apply();
            graphicsDevice.SetVertexBuffer(shapeBuffer);
            graphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, shapeBuffer.VertexCount - 1);
            graphicsDevice.SetVertexBuffer(spineBuffer);
            graphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, spineBuffer.VertexCount - 1);
        }

        public Vector2 chainStartOffset => new Vector2(0, 0);
        public float chainScale => 14f;
        public Vector2 everythingOffset => -viewSize / 2.5f;

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(41, 43, 47));
            float scale = chainScale / 16;
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null);
            _spriteBatch.Draw(grid, viewSize / 2 - new Vector2(0, grid.Height * scale) - (everythingOffset * new Vector2(-1, 1)), null, new Color(54, 55, 59), 0f, default, scale, SpriteEffects.FlipVertically, default);
            _spriteBatch.DrawString(font_Arial, "Closeness: " + closenessValue.ToString(), new Vector2(5, 5), Color.White);
            _spriteBatch.End();

            DrawGeometry(basicEffect, basicEffect.CurrentTechnique.Passes[0], _graphics.GraphicsDevice);

            base.Draw(gameTime);
        }
    }
}
