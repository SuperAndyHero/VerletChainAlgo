using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

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
        public Texture2D circle;
        public Texture2D blank;
        public static SpriteFont font_Arial;
        public static BasicEffect basicEffect;
        public static Random rand;

        #region clipboard text
        [DllImport("user32.dll")]
        internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        internal static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        internal static extern bool SetClipboardData(uint uFormat, IntPtr data);

        [STAThread]
        public void SetClipboardText(string text)
        {
            try
            {
                OpenClipboard(IntPtr.Zero);
                var yourString = text;
                var ptr = Marshal.StringToHGlobalUni(yourString);
                SetClipboardData(13, ptr);
                CloseClipboard();
                Marshal.FreeHGlobal(ptr);
            }
            catch (Exception e)
            {
                errorText = e.Message;
            }
        }
        #endregion

        public string errorText = null;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            //this.Window.IsBorderless = true;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        #region chain variables

        public int vertexCount = 9;

        public int vertexDefaultDistance = 9;

        public float minimumGravStrength = 0.5f;//the minimum strength a gravity force can be, leave zero if there should not be a minimum

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

        public Vector2 VertexGravityMult = Vector2.One;

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


        //unimportant values (changing these does not effect the result much, just keep these the same as your verlet chain)
        public float VertexDrag = 1.15f;
        public int PhysicsRepetitions = 5;
        #endregion

        #region simulation variables
        //all of these variables do not effect the shape of the chain, but change how fast its simulated / how its displayed
        public int interationsPerUpdate = 500;
        public int chainUpdatesPerIter = 3;//keep this between 3 and 10, anything above will not have any effect
        public Vector2 chainStartOffset => new Vector2(0, 0);
        public float chainScale => 14f;
        public Vector2 everythingOffset => -viewSize / 2.5f;
        #endregion

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
            spineBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), vertexCount, BufferUsage.WriteOnly);
            shapeBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), vertexCount, BufferUsage.WriteOnly);
            basicEffect = new BasicEffect(_graphics.GraphicsDevice)
            {
                VertexColorEnabled = true,
            };
            basicEffect.Projection = Matrix.CreateOrthographic(viewSize.X, viewSize.Y, 0, 1000);
            rand = new Random(123456789);

            NextWantedVertexGravityArray = Enumerable.Repeat(Vector2.Zero, vertexCount).ToArray();

            SetupChain();

            base.Initialize();
        }

        public void SetupChain()
        {
            bool distances = VertexDistanceArray != null;
            bool gravities = VertexGravityArray != null;

            Vector2[] settledPoints;
            if (SettledPointArray != null)
                settledPoints = SettledPointArray;
            else
            {
                settledPoints = new Vector2[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    int dist = distances ? VertexDistanceArray[i] : vertexDefaultDistance;
                    Vector2 grav = gravities ? VertexGravityArray[i] * VertexGravityMult : VertexGravityMult;
                    settledPoints[i] = (grav * dist) + (i > 0 ? settledPoints[i - 1] : Vector2.Zero);
                }
            }


            chain = new VerletChainInstance(vertexCount, position + chainStartOffset,
                position + ((settledPoints[vertexCount - 1]) * facingDirection), vertexDefaultDistance, VertexGravityMult * facingDirection,
                gravities, VertexGravityArray?.ToList(), distances, VertexDistanceArray?.ToList())
            {
                drag = VertexDrag,
                constraintRepetitions = PhysicsRepetitions
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
                combinedPos += WantedVertexPoints[i];
                vertexPos[i] = new VertexPositionColor(new Vector3(combinedPos, 0), Color.Red);
            }

            shapeBuffer.SetData(vertexPos);
        }

        #region updating

        public float lastClosenessValue = float.MaxValue - 1;
        public float closenessValue = float.MaxValue;

        public bool paused = false;

        public int selectedIndex = 0;

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
                selectedIndex = selectedIndex - 1 < 0 ? vertexCount - 1 : selectedIndex - 1;
            else if (ButtonPressed(Keys.Down))
                selectedIndex = selectedIndex + 1 > vertexCount - 1 ? 0 : selectedIndex + 1;

            if (!paused)
            {
                for (int i = 0; i < interationsPerUpdate; i++)
                {
                    AdjustChainValues();

                    for (int j = 0; j < chainUpdatesPerIter; j++)
                        chain.UpdateChain();

                    CalculateCloseness();
                }
            }
            else
            {
                if (ButtonPressed(Keys.Enter))
                    SetClipboardText("Test123");//CreateVector2Text(VertexGravityArray[selectedIndex]));
            }

            lastState = keyboardState;

            base.Update(gameTime);
        }

        public string CreateVector2Text(Vector2 vec) =>
            "new Vector2(" + vec.X + ", " + vec.Y + ")";

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
                NextWantedVertexGravityArray[index] = GiveRandomOffset(VertexGravityArray[index]);

            if(minimumGravStrength > 0)
                for (int i = 0; i < vertexCount; i++)
                    if (NextWantedVertexGravityArray[i].Length() < minimumGravStrength)
                        NextWantedVertexGravityArray[i] *= (1 + minimumGravStrength) - NextWantedVertexGravityArray[i].Length();

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

            for (int i = 0; i < vertexCount; i++)
            {
                combinedPos += WantedVertexPoints[i];
                Vector2 segPos = chain.ropeSegments[i].posNow;
                closenessValue += Vector2.Distance(combinedPos + chain.startPoint, segPos);
            }

            closenessValue /= vertexCount - 1;
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
            effect.View = Matrix.CreateScale(chainScale, -chainScale, chainScale) * Matrix.CreateTranslation(new Vector3(everythingOffset, 0));

            pass.Apply();
            graphicsDevice.SetVertexBuffer(shapeBuffer);
            graphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, shapeBuffer.VertexCount - 1);
            graphicsDevice.SetVertexBuffer(spineBuffer);
            graphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, spineBuffer.VertexCount - 1);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(41, 43, 47));
            float spriteScale = chainScale / 16;
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null);
            _spriteBatch.Draw(grid, viewSize / 2 - new Vector2(0, grid.Height * spriteScale) - (everythingOffset * new Vector2(-1, 1)), null, new Color(54, 55, 59), 0f, default, spriteScale, SpriteEffects.FlipVertically, default);
            _spriteBatch.DrawString(font_Arial, "Closeness: " + closenessValue.ToString(), new Vector2(5, 5), Color.White);
            for (int i = 0; i < vertexCount; i++)
                _spriteBatch.DrawString(font_Arial, i.ToString() + " : " + VertexGravityArray[i].ToString(), new Vector2(5, 30 + (i * 20)), Color.White);
            if (paused)
            {
                Point size = font_Arial.MeasureString(selectedIndex.ToString() + " : " + VertexGravityArray[selectedIndex].ToString()).ToPoint();
                _spriteBatch.Draw(blank, new Rectangle(new Point(5, 30 + (selectedIndex * 20)), size), Color.Yellow * 0.2f);
                _spriteBatch.DrawString(font_Arial, "Press Enter to copy to clipboard", new Vector2(10 + size.X, 31 + (selectedIndex * 20)), Color.White * 0.33f, 0f, default, 0.9f, default, default);
                _spriteBatch.DrawString(font_Arial, "Paused", new Vector2(viewSize.X / 2, 25) - font_Arial.MeasureString("Paused"), Color.IndianRed * 0.35f, 0f, default, 2f, default, default);
            }
            _spriteBatch.End(); 

            DrawGeometry(basicEffect, basicEffect.CurrentTechnique.Passes[0], _graphics.GraphicsDevice);

            Vector2 combinedGrav = Vector2.Zero;

            for (int i = 1; i < vertexCount; i++)
            {
                combinedGrav += VertexGravityArray[i];
            }

            float average = combinedGrav.Length() / (vertexCount - 1);


            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null);
            for (int i = 1; i < vertexCount; i++)
            {
                float gravScale = Math.Max(VertexGravityArray[i].Length() - average, 0);
                Vector2 pos = viewSize / 2 - (everythingOffset * new Vector2(-1, 1)) + (chain.ropeSegments[i].posNow - position) * chainScale;
                _spriteBatch.Draw(circle, pos, null, Color.White * 0.25f, 0f, Vector2.One * 100, 0.25f * spriteScale * gravScale, default, default);
                _spriteBatch.DrawString(font_Arial, VertexGravityArray[i].Length().ToString(), pos, Color.DarkGoldenrod);

            }
            _spriteBatch.End();


            base.Draw(gameTime);
        }
        #endregion
    }
}
