using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using TextCopy;
using VerletChainAlgo;

namespace VerletChainAlgo
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        VerletChainInstance chain;
        public VertexBuffer spineBuffer;
        public VertexBuffer shapeBuffer;
        public static Vector2 viewSize;
        public Texture2D grid;
        public Texture2D circle;
        public Texture2D blank;
        public static SpriteFont font_Arial;
        public static BasicEffect basicEffect;
        public static Random rand;

        public string errorText = null;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            //this.Window.IsBorderless = true;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        #region other starting values (do not edit these)
        public static Vector2[] DefaultVertexGravityArray => new Vector2[] {
                Vector2.Zero,

                new Vector2(1, -1),

                new Vector2(1, -1),

                new Vector2(1, -1),

                new Vector2(1, -1),

                new Vector2(1, -1),

                new Vector2(1, -1),

                new Vector2(1, -1),

                new Vector2(1, -1)
        };

        public Vector2[] VertexGravityArray = DefaultVertexGravityArray;

        public Vector2[] SettledPointArray = null;
        public int facingDirection = 1; //1, -1
        #endregion

        public Vector2 position;
        public Vector2[] NextWantedVertexGravityArray;

        protected override void Initialize()
        {
            viewSize = new Vector2(_graphics.GraphicsDevice.Viewport.Width, _graphics.GraphicsDevice.Viewport.Height);
            position = viewSize / 2;
            spineBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), ChainVariables.vertexCount, BufferUsage.WriteOnly);
            shapeBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), ChainVariables.vertexCount, BufferUsage.WriteOnly);
            basicEffect = new BasicEffect(_graphics.GraphicsDevice)
            {
                VertexColorEnabled = true,
            };
            basicEffect.Projection = Matrix.CreateOrthographic(viewSize.X, viewSize.Y, 0, 1000);
            rand = new Random(123456789);

            NextWantedVertexGravityArray = Enumerable.Repeat(Vector2.Zero, ChainVariables.vertexCount).ToArray();

            SetupChain();

            base.Initialize();
        }

        public void SetupChain()
        {
            bool distances = ChainVariables.VertexDistanceArray != null;
            bool gravities = VertexGravityArray != null;

            Vector2[] settledPoints;
            if (SettledPointArray != null)
                settledPoints = SettledPointArray;
            else
            {
                settledPoints = new Vector2[ChainVariables.vertexCount];
                for (int i = 0; i < ChainVariables.vertexCount; i++)
                {
                    int dist = distances ? ChainVariables.VertexDistanceArray[i] : ChainVariables.vertexDefaultDistance;
                    Vector2 grav = gravities ? VertexGravityArray[i] * ChainVariables.VertexGravityMult : ChainVariables.VertexGravityMult;
                    settledPoints[i] = (grav * dist) + (i > 0 ? settledPoints[i - 1] : Vector2.Zero);
                }
            }


            chain = new VerletChainInstance(ChainVariables.vertexCount, position + ChainVariables.chainStartOffset,
                position + ((settledPoints[ChainVariables.vertexCount - 1]) * facingDirection), ChainVariables.vertexDefaultDistance, ChainVariables.VertexGravityMult * facingDirection,
                gravities, VertexGravityArray?.ToList(), distances, ChainVariables.VertexDistanceArray?.ToList())
            {
                drag = ChainVariables.VertexDrag,
                constraintRepetitions = ChainVariables.PhysicsRepetitions
            };
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            grid = Content.Load<Texture2D>("Grid");
            circle = Content.Load<Texture2D>("Circle");
            blank = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            blank.SetData(new Color[] { Color.White });
            font_Arial = Content.Load<SpriteFont>("Arial");


            VertexPositionColor[] vertexPos = new VertexPositionColor[shapeBuffer.VertexCount];
            Vector2 combinedPos = Vector2.Zero;

            for (int i = 0; i < vertexPos.Length; i++)
            {
                combinedPos += ChainVariables.WantedVertexPoints[i];
                vertexPos[i] = new VertexPositionColor(new Vector3(combinedPos, 0), Color.Red);
            }

            shapeBuffer.SetData(vertexPos);
        }

        #region updating

        public float lastClosenessValue = float.MaxValue - 1;
        public float closenessValue = float.MaxValue;

        public bool paused = false;

        public int selectedIndex = 0;

        public bool selectedSide = false;

        public KeyboardState lastState = Keyboard.GetState();
        public KeyboardState keyboardState = Keyboard.GetState();

        public bool ButtonPressed(Keys key) =>
            keyboardState.IsKeyDown(key) && !lastState.IsKeyDown(key);

        protected override void Update(GameTime gameTime)
        {
            keyboardState = Keyboard.GetState();
            if (ButtonPressed(Keys.Escape))
            {
                VertexGravityArray = DefaultVertexGravityArray;
                SetupChain();
            }
            else if (ButtonPressed(Keys.Space))
                paused = !paused;
            else if (ButtonPressed(Keys.Up))
                selectedIndex = selectedIndex - 1 < 0 ? ChainVariables.vertexCount - 1 : selectedIndex - 1;
            else if (ButtonPressed(Keys.Down))
                selectedIndex = selectedIndex + 1 > ChainVariables.vertexCount - 1 ? 0 : selectedIndex + 1;
            else if (ButtonPressed(Keys.Left) || ButtonPressed(Keys.Right))
                selectedSide = !selectedSide;

            if (!paused)
            {

                for (int i = 0; i < ChainVariables.interationsPerUpdate; i++)
                {
                    AdjustChainValues();

                    for (int j = 0; j < ChainVariables.chainUpdatesPerIter; j++)
                        chain.UpdateChain();

                    CalculateCloseness();
                }
            }
            else
            {
                if (ButtonPressed(Keys.Enter) || ButtonPressed(Keys.C))
                    if (selectedSide)
                        ClipboardService.SetText(CreateVector2Text(chain.ropeSegments[selectedIndex].posNow - position));
                    else
                        ClipboardService.SetText(CreateVector2Text(VertexGravityArray[selectedIndex]));
            }

            lastState = keyboardState;

            base.Update(gameTime);
        }

        public string CreateVector2Text(Vector2 vec) =>
            "new Vector2(" + vec.X + "f, " + vec.Y + "f)";

        public void AdjustChainValues()
        {
            if (closenessValue < lastClosenessValue)
            {
                for (int i = 0; i < ChainVariables.vertexCount; i++)
                    VertexGravityArray[i] = NextWantedVertexGravityArray[i];
            }

            for (int i = 0; i < ChainVariables.vertexCount; i++)
                NextWantedVertexGravityArray[i] = VertexGravityArray[i];

            int index = rand.Next(1, ChainVariables.vertexCount);
                NextWantedVertexGravityArray[index] = GiveRandomOffset(VertexGravityArray[index]);

            if(ChainVariables.minimumGravStrength > 0)
                for (int i = 0; i < ChainVariables.vertexCount; i++)
                    if (NextWantedVertexGravityArray[i].Length() < ChainVariables.minimumGravStrength)
                        NextWantedVertexGravityArray[i] *= (1 + ChainVariables.minimumGravStrength) - NextWantedVertexGravityArray[i].Length();

            //NextWantedVertexGravityArray[vertexCount - 1] *= 1.5f - NextWantedVertexGravityArray[vertexCount - 1].Length();

            chain.forceGravities = NextWantedVertexGravityArray.ToList();
        }

        public Vector2 GiveRandomOffset(Vector2 vec)
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

            for (int i = 0; i < ChainVariables.vertexCount; i++)
            {
                combinedPos += ChainVariables.WantedVertexPoints[i];
                Vector2 segPos = chain.ropeSegments[i].posNow;
                closenessValue += Vector2.Distance(combinedPos + chain.startPoint, segPos);
            }

            closenessValue /= ChainVariables.vertexCount - 1;
        }
        #endregion

        #region rendering
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
            effect.View = Matrix.CreateScale(ChainVariables.chainScale, -ChainVariables.chainScale, ChainVariables.chainScale) * Matrix.CreateTranslation(new Vector3(ChainVariables.everythingOffset, 0));

            pass.Apply();
            graphicsDevice.SetVertexBuffer(shapeBuffer);
            graphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, shapeBuffer.VertexCount - 1);
            graphicsDevice.SetVertexBuffer(spineBuffer);
            graphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, spineBuffer.VertexCount - 1);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(41, 43, 47));
            float spriteScale = ChainVariables.chainScale / 16;
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null);
            _spriteBatch.Draw(grid, viewSize / 2 - new Vector2(0, grid.Height * spriteScale) - (ChainVariables.everythingOffset * new Vector2(-1, 1)), null, new Color(54, 55, 59), 0f, default, spriteScale, SpriteEffects.FlipVertically, default);
            _spriteBatch.DrawString(font_Arial, "Closeness: " + closenessValue.ToString(), new Vector2(5, 5), Color.White);
            for (int i = 0; i < ChainVariables.vertexCount; i++)
            {
                _spriteBatch.DrawString(font_Arial, "Grav " + i.ToString() + " : " + VertexGravityArray[i].ToString(), new Vector2(5, 30 + (i * 20)), Color.White * 0.8f);
                _spriteBatch.DrawString(font_Arial, "Pos " + i.ToString() + " : " + (chain.ropeSegments[i].posNow - position).ToString(), new Vector2(viewSize.X - 265, 30 + (i * 20)), Color.White * 0.8f);
            }

            if (paused)
            {
                if (!selectedSide)
                {
                    Point size = font_Arial.MeasureString("Grav " + selectedIndex.ToString() + " : " + VertexGravityArray[selectedIndex].ToString()).ToPoint();
                    _spriteBatch.Draw(blank, new Rectangle(new Point(5, 30 + (selectedIndex * 20)), size), Color.Yellow * 0.2f);
                }
                else
                {
                    Point size = font_Arial.MeasureString("Pos " + selectedIndex.ToString() + " : " + (chain.ropeSegments[selectedIndex].posNow - position).ToString()).ToPoint();
                    _spriteBatch.Draw(blank, new Rectangle(new Point((int)(viewSize.X - 265), 30 + (selectedIndex * 20)), size), Color.Yellow * 0.2f);
                }
                _spriteBatch.DrawString(font_Arial, "Press Enter to copy to clipboard", new Vector2(viewSize.X / 2 - 90, viewSize.Y - 30), Color.White * 0.50f, 0f, default, 0.9f, default, default);
                _spriteBatch.DrawString(font_Arial, "Paused", new Vector2(viewSize.X / 2, 25) - font_Arial.MeasureString("Paused"), Color.IndianRed * 0.35f, 0f, default, 2f, default, default);
            }
            _spriteBatch.End(); 

            DrawGeometry(basicEffect, basicEffect.CurrentTechnique.Passes[0], _graphics.GraphicsDevice);

            Vector2 combinedGrav = Vector2.Zero;

            for (int i = 1; i < ChainVariables.vertexCount; i++)
            {
                combinedGrav += VertexGravityArray[i];
            }

            float average = combinedGrav.Length() / (ChainVariables.vertexCount - 1);


            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null);
            for (int i = 1; i < ChainVariables.vertexCount; i++)
            {
                float gravScale = Math.Max(VertexGravityArray[i].Length() - average, 0);
                Vector2 pos = viewSize / 2 - (ChainVariables.everythingOffset * new Vector2(-1, 1)) + (chain.ropeSegments[i].posNow - position) * ChainVariables.chainScale;
                _spriteBatch.Draw(circle, pos, null, Color.White * 0.25f, 0f, Vector2.One * 100, 0.25f * spriteScale * gravScale, default, default);
                _spriteBatch.DrawString(font_Arial, VertexGravityArray[i].Length().ToString(), pos, Color.DarkGoldenrod);

            }
            _spriteBatch.End();


            base.Draw(gameTime);
        }
        #endregion
    }
}
