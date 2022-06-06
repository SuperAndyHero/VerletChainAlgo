using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
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
        public VerletChainInstance chain;
        public VertexBuffer spineBuffer;
        public VertexBuffer shapeBuffer;
        public static Vector2 viewSize;
        public static Vector2 originalViewSize;
        public Texture2D grid;
        public Texture2D circle;
        public Texture2D blank;
        public static SpriteFont font_Arial;
        public static BasicEffect basicEffect;
        public static Random rand;

        public string errorText = null;
        public Texture2D backTexture = null;

        public static float CameraZoom = 1f;
        public static float EverythingDrawScale => SimInfo.DefaultDrawScale * CameraZoom;

        public static Vector2 CameraPosition = Vector2.Zero;
        public static Vector2 CameraVelocity = Vector2.Zero;
        public static Vector2 EverythingDrawOffset => SimInfo.DefaultDrawOffset + CameraPosition;

        public Vector2 MousePosition => new Vector2(mouseState.X, mouseState.Y);
        //public Vector2 MousePositionWorld => MousePosition + CameraPosition;
        //public Vector2 MouseRealPosition => MousePositionWorld - (EverythingDrawOffset) + SimInfo.DefaultDrawOffset;

        public List<Vector2> PlacementList = new List<Vector2>();

        public ChainInfo CurrentInfo;

        public enum SimState
        {
            waiting,
            running,
            placement
        }

        public static SimState CurrentState = SimState.waiting;

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
            Window.AllowUserResizing = true;
            viewSize = originalViewSize = new Vector2(_graphics.GraphicsDevice.Viewport.Width, _graphics.GraphicsDevice.Viewport.Height);
            position = Vector2.Zero;
            basicEffect = new BasicEffect(_graphics.GraphicsDevice)
            {
                VertexColorEnabled = true,
            };
            basicEffect.Projection = Matrix.CreateOrthographic(viewSize.X, viewSize.Y, 0, 1000);
            rand = new Random(123456789);

            backTexture = LoadTextureFilePath(SimInfo.TexturePath);
            CurrentInfo = new ChainInfo();

            base.Initialize();
        }

        public Texture2D LoadTextureFilePath(string path)
        {
            using FileStream filestream = new FileStream(path, FileMode.Open);
            return Texture2D.FromStream(GraphicsDevice, filestream);
        }

        public void SetupChain(ChainInfo info)
        {
            bool distances = info.VertexDistanceArray != null;
            bool gravities = VertexGravityArray != null;

            spineBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), info.vertexCount, BufferUsage.WriteOnly);
            shapeBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), info.vertexCount, BufferUsage.WriteOnly);

            Vector2[] settledPoints;
            if (SettledPointArray != null)
                settledPoints = SettledPointArray;
            else
            {
                settledPoints = new Vector2[info.vertexCount];
                for (int i = 0; i < info.vertexCount; i++)
                {
                    int dist = distances ? info.VertexDistanceArray[i] : info.vertexDefaultDistance;
                    Vector2 grav = gravities ? VertexGravityArray[i] * info.VertexGravityMult : info.VertexGravityMult;
                    settledPoints[i] = (grav * dist) + (i > 0 ? settledPoints[i - 1] : Vector2.Zero);
                }
            }


            chain = new VerletChainInstance(info.vertexCount, position + info.ChainStartOffset,
                position + ((settledPoints[info.vertexCount - 1]) * facingDirection), info.vertexDefaultDistance, info.VertexGravityMult * facingDirection,
                gravities, VertexGravityArray?.ToList(), distances, info.VertexDistanceArray?.ToList())
            {
                drag = info.VertexDrag,
                constraintRepetitions = info.PhysicsRepetitions
            };

            VertexPositionColor[] vertexPos = new VertexPositionColor[shapeBuffer.VertexCount];
            Vector2 combinedPos = Vector2.Zero;

            for (int i = 0; i < vertexPos.Length; i++)
            {
                combinedPos += info.WantedVertexPoints[i];
                vertexPos[i] = new VertexPositionColor(new Vector3(combinedPos + info.ChainStartOffset, 0), Color.Red);
            }

            shapeBuffer.SetData(vertexPos);

            NextWantedVertexGravityArray = Enumerable.Repeat(Vector2.Zero, info.vertexCount).ToArray();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            grid = Content.Load<Texture2D>("Grid");
            circle = Content.Load<Texture2D>("Circle");
            blank = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            blank.SetData(new Color[] { Color.White });
            font_Arial = Content.Load<SpriteFont>("Arial");

            //SetupChain();
        }

        #region updating

        public float lastClosenessValue = float.MaxValue - 1;
        public float closenessValue = float.MaxValue;

        public bool paused = false;

        public int selectedIndex = 0;

        public bool selectedSide = false;

        public bool SnapToGrid = false;

        public MouseState mouseState = Mouse.GetState();
        public MouseState lastMouseState = Mouse.GetState();
        public KeyboardState lastKeyState = Keyboard.GetState();
        public KeyboardState keyboardState = Keyboard.GetState();

        public bool ButtonPressed(Keys key) =>
            keyboardState.IsKeyDown(key) && !lastKeyState.IsKeyDown(key);

        public bool LeftMousePressed() =>
            mouseState.LeftButton == ButtonState.Pressed && !(lastMouseState.LeftButton == ButtonState.Pressed);

        protected override void Update(GameTime gameTime)
        {
            Vector2 newviewSize = new Vector2(_graphics.GraphicsDevice.Viewport.Width, _graphics.GraphicsDevice.Viewport.Height);
            if (viewSize != newviewSize)
                basicEffect.Projection = Matrix.CreateOrthographic(newviewSize.X, newviewSize.Y, 0, 1000);
            viewSize = newviewSize;

            keyboardState = Keyboard.GetState();
            mouseState = Mouse.GetState();

            if (keyboardState.IsKeyDown(Keys.W))
                CameraVelocity.Y -= 0.1f;
            if (keyboardState.IsKeyDown(Keys.S))
                CameraVelocity.Y += 0.1f;
            if (keyboardState.IsKeyDown(Keys.D))
                CameraVelocity.X -= 0.1f;
            if (keyboardState.IsKeyDown(Keys.A))
                CameraVelocity.X += 0.1f;

            if (mouseState.MiddleButton == ButtonState.Pressed)
                CameraPosition += ((mouseState.Position - lastMouseState.Position).ToVector2() * new Vector2(1, -1)) / (CameraZoom * SimInfo.DefaultDrawScale);

            if (keyboardState.IsKeyDown(Keys.OemPlus))
                CameraZoom += 0.01f;
            if (keyboardState.IsKeyDown(Keys.OemMinus))
                CameraZoom -= 0.01f;
            if (ButtonPressed(Keys.Back))
                CameraZoom = 1;

            CameraZoom *= 1 + ((mouseState.ScrollWheelValue - lastMouseState.ScrollWheelValue) * 0.0005f);

            if (CameraZoom < 0.001f)
                CameraZoom = 0.001f;

            switch (CurrentState)
            {
                case SimState.waiting:
                    if (ButtonPressed(Keys.Space) || ButtonPressed(Keys.Enter))
                    {
                        StartSimMode();
                    }
                    else if (ButtonPressed(Keys.Tab))
                    {
                        StartBuildMode();
                    }
                    break;
                case SimState.placement:
                    SnapToGrid = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
                    break;
                case SimState.running:
                    if (ButtonPressed(Keys.Escape))
                    {
                        StartWaitingMode();
                    }
                    if (ButtonPressed(Keys.Space))
                        paused = !paused;
                    if (!paused)
                    {
                        if (chain != null)
                        {
                            for (int i = 0; i < SimInfo.interationsPerUpdate; i++)
                            {
                                AdjustChainValues(CurrentInfo);

                                for (int j = 0; j < SimInfo.chainUpdatesPerIter; j++)
                                    chain.UpdateChain();

                                CalculateCloseness(CurrentInfo);
                            }
                        }
                    }
                    else
                    {
                        if (ButtonPressed(Keys.Up))
                            selectedIndex = selectedIndex - 1 < 0 ? chain.segmentCount - 1 : selectedIndex - 1;
                        else if (ButtonPressed(Keys.Down))
                            selectedIndex = selectedIndex + 1 > chain.segmentCount - 1 ? 0 : selectedIndex + 1;
                        else if (ButtonPressed(Keys.Left) || ButtonPressed(Keys.Right))
                            selectedSide = !selectedSide;

                        if (ButtonPressed(Keys.Enter) || ButtonPressed(Keys.C))
                            if (selectedSide)
                                ClipboardService.SetText(CreateVector2Text(chain.ropeSegments[selectedIndex].posNow - position));
                            else
                                ClipboardService.SetText(CreateVector2Text(VertexGravityArray[selectedIndex]));
                    }
                    break;
            }

            CameraPosition += CameraVelocity;
            CameraVelocity *= 0.96f;

            lastMouseState = mouseState;
            lastKeyState = keyboardState;
            base.Update(gameTime);
        }


        public void StartBuildMode()
        {
            DeleteChain();
            CurrentState = SimState.placement;
        }
        public void StartSimMode()
        {
            SetupChain(CurrentInfo);
            CurrentState = SimState.running;
        }
        public void StartWaitingMode()
        {
            DeleteChain();
            CurrentState = SimState.waiting;
        }

        public void DeleteChain()
        {
            chain = null;
            spineBuffer = null;
            shapeBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), 2, BufferUsage.WriteOnly);
            VertexGravityArray = DefaultVertexGravityArray;
            selectedIndex = 0;
        }

        public string CreateVector2Text(Vector2 vec) =>
            "new Vector2(" + vec.X + "f, " + vec.Y + "f)";

        public void AdjustChainValues(ChainInfo info)
        {
            if (closenessValue < lastClosenessValue)
            {
                for (int i = 0; i < info.vertexCount; i++)
                    VertexGravityArray[i] = NextWantedVertexGravityArray[i];
            }

            for (int i = 0; i < info.vertexCount; i++)
                NextWantedVertexGravityArray[i] = VertexGravityArray[i];

            int index = rand.Next(1, info.vertexCount);
                NextWantedVertexGravityArray[index] = GiveRandomOffset(VertexGravityArray[index]);

            if(info.minimumGravStrength > 0)
                for (int i = 0; i < info.vertexCount; i++)
                    if (NextWantedVertexGravityArray[i].Length() < info.minimumGravStrength)
                        NextWantedVertexGravityArray[i] *= (1 + info.minimumGravStrength) - NextWantedVertexGravityArray[i].Length();

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

        public void CalculateCloseness(ChainInfo info)
        {
            lastClosenessValue = closenessValue;
            closenessValue = 0;
            Vector2 combinedPos = Vector2.Zero;

            for (int i = 0; i < info.vertexCount; i++)
            {
                combinedPos += info.WantedVertexPoints[i];
                Vector2 segPos = chain.ropeSegments[i].posNow;
                closenessValue += Vector2.Distance(combinedPos + chain.startPoint, segPos);
            }

            closenessValue /= info.vertexCount - 1;
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
            if (chain != null && spineBuffer != null)
            {
                VertexPositionColor[] vertexPos = new VertexPositionColor[spineBuffer.VertexCount];

                for (int i = 0; i < vertexPos.Length; i++)
                    vertexPos[i] = new VertexPositionColor(new Vector3(chain.ropeSegments[i].posNow, 0), SpineColor(i));

                spineBuffer.SetData(vertexPos);
            }
            effect.TextureEnabled = false;//this or a second effect is needed
            effect.View = Matrix.CreateTranslation(new Vector3(EverythingDrawOffset * new Vector2(1, -1), 0)) * Matrix.CreateScale(EverythingDrawScale, -EverythingDrawScale, 1);

            pass.Apply();
            if (shapeBuffer != null)
            {
                graphicsDevice.SetVertexBuffer(shapeBuffer);
                graphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, shapeBuffer.VertexCount - 1);
            }
            
            if (chain != null && spineBuffer != null)
            {
                graphicsDevice.SetVertexBuffer(spineBuffer);
                graphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, spineBuffer.VertexCount - 1);
            }
        }

        public void DrawTextCentered(SpriteBatch sb, SpriteFont font, string text, Vector2 center, Color color, float scale)
        {
            Vector2 textSize = font_Arial.MeasureString(text) * scale;
            Vector2 textPosition = new Vector2((int)(center.X - (textSize.X / 2)), (int)(center.Y - (textSize.Y / 2)));
            sb.DrawString(font, text, textPosition, color, 0f, default, scale, default, default);
        }

        protected override void Draw(GameTime gameTime)
        { 
            GraphicsDevice.Clear(new Color(41, 43, 47));
            const float gridscale = 0.0625f;

            Matrix mat = Matrix.CreateTranslation(new Vector3(EverythingDrawOffset * new Vector2(1, -1), 0)) * Matrix.CreateScale(EverythingDrawScale, EverythingDrawScale, 1) * Matrix.CreateTranslation(viewSize.X / 2, viewSize.Y / 2, 0);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, mat);
            _spriteBatch.Draw(backTexture, new Vector2(0,0) - new Vector2(0, (backTexture.Height * SimInfo.SpriteScale)) - (SimInfo.SpriteOffset), null, new Color(150, 150, 150), 0f, default, SimInfo.SpriteScale, SpriteEffects.None, default);
            _spriteBatch.End();

            float gridHeight = grid.Height * gridscale;
            float gridWidth = grid.Width * gridscale;
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, mat);
            _spriteBatch.Draw(grid, new Vector2(0, -gridHeight), null, new Color(74, 75, 79), 0f, default, gridscale, SpriteEffects.FlipVertically, default);
            _spriteBatch.Draw(grid, new Vector2(0, 0), null, new Color(49, 48, 52), 0f, default, gridscale, SpriteEffects.FlipVertically, default);
            _spriteBatch.Draw(grid, new Vector2(-gridWidth, -gridHeight), null, new Color(39, 38, 32), 0f, default, gridscale, SpriteEffects.FlipVertically, default);
            _spriteBatch.Draw(grid, new Vector2(-gridWidth, 0), null, new Color(39, 38, 32), 0f, default, gridscale, SpriteEffects.FlipVertically, default);
            _spriteBatch.DrawString(font_Arial, "(0, 0)", new Vector2(0, 0), Color.White, 0f, default, gridscale * 0.75f, default, default);
            _spriteBatch.End();

            switch (CurrentState)
            {
                case SimState.running:
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null);
                    _spriteBatch.DrawString(font_Arial, "Closeness: " + closenessValue.ToString(), new Vector2(5, 5), Color.White);
                    for (int i = 0; i < chain.segmentCount; i++)
                    {
                        _spriteBatch.DrawString(font_Arial, "Grav " + i.ToString() + " : " + VertexGravityArray[i].ToString(), new Vector2(5, 30 + (i * 20)), Color.White * 0.8f);
                        _spriteBatch.DrawString(font_Arial, "Pos " + i.ToString() + " : " + (chain.ropeSegments[i].posNow - position).ToString(), new Vector2(viewSize.X - 265, 30 + (i * 20)), Color.White * 0.8f);
                    }
                    _spriteBatch.End();

                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null);
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
                        _spriteBatch.DrawString(font_Arial, "Press Enter to copy to clipboard", new Vector2(viewSize.X / 2 - 90, viewSize.Y - 30), Color.White * 0.50f, 0f, default, 1.1f, default, default);
                        DrawTextCentered(_spriteBatch, font_Arial, "Paused", new Vector2(viewSize.X / 2, 25), Color.IndianRed * 0.50f, 2.2f);
                    }
                    _spriteBatch.End();
                    break;
                case SimState.waiting:
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null);
                    DrawTextCentered(_spriteBatch, font_Arial, "Press space to start/enter or tab to edit", new Vector2(viewSize.X / 2, 25), Color.IndianRed * 0.35f, 1.2f);
                    _spriteBatch.End();
                    break;

                case SimState.placement:
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null);
                    if (chain != null)
                    {
                        for (int i = 0; i < 1; i++)
                        {
                            _spriteBatch.DrawString(font_Arial, "Target Pos " + i.ToString() + " : " + (chain.ropeSegments[i].posNow - position).ToString(), new Vector2(viewSize.X - 265, 30 + (i * 20)), Color.White * 0.8f);
                        }
                    }
                    mat.Decompose(out Vector3 scale, out Quaternion qu, out Vector3 transl);
                    //Vector2 snap = (Vector2.Round(((MousePosition + (EverythingDrawOffset * new Vector2(1, -1))) / EverythingDrawScale)) * EverythingDrawScale);
                    //Vector2 snap2 = (Vector2.Round(((MousePosition) / EverythingDrawScale)) * EverythingDrawScale);
                    Vector2 mouseRealPos = (MousePosition - new Vector2(transl.X, transl.Y));
                    Vector2 round = new Vector2((float)Math.Round(mouseRealPos.X / scale.X) * scale.X, (float)Math.Round(mouseRealPos.Y / scale.Y) * scale.Y);
                    Vector2 off2 = mouseRealPos - round;

                    Vector2 snapPos = round / new Vector2(scale.X, scale.Y);
                    Point roundedSnapPos = new Point((int)Math.Round(snapPos.X), (int)Math.Round(snapPos.Y));

                    Vector2 normalPos = mouseRealPos / new Vector2(scale.X, scale.Y);
                    Vector2 roundedNormalPos = new Vector2((float)Math.Round(normalPos.X, 2), (float)Math.Round(normalPos.Y, 2));


                    _spriteBatch.Draw(circle, SnapToGrid ? MousePosition - off2 : new Vector2((float)Math.Round(MousePosition.X / scale.X, 2) * scale.X, (float)Math.Round(MousePosition.Y / scale.Y, 2) * scale.Y), null, Color.Green, 0f, new Vector2(circle.Width, circle.Height) / 2, 0.03f, default, default);
                    _spriteBatch.DrawString(font_Arial, "Placement Mode", new Vector2(viewSize.X / 2, 25) - font_Arial.MeasureString("Stopped"), Color.IndianRed * 0.35f, 0f, default, 2f, default, default);
                    _spriteBatch.DrawString(font_Arial, "Hold shift to snap to grid, lclick to place, and enter to simulate", new Vector2(viewSize.X / 2 - 90, viewSize.Y - 30), Color.White * 0.50f, 0f, default, 1.2f, default, default);
                    _spriteBatch.DrawString(font_Arial, SnapToGrid ? roundedSnapPos.ToString() : roundedNormalPos.ToString(), MousePosition, Color.White * 0.75f, 0f, default, 1f, default, default); ;
                    _spriteBatch.End();
                    break;
            }

            DrawGeometry(basicEffect, basicEffect.CurrentTechnique.Passes[0], _graphics.GraphicsDevice);


            if (CurrentState == SimState.running)
            {
                Vector2 combinedGrav = Vector2.Zero;

                for (int i = 1; i < chain.segmentCount; i++)
                {
                    combinedGrav += VertexGravityArray[i];
                }

                float average = combinedGrav.Length() / (chain.segmentCount - 1);

                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, mat);
                for (int i = 1; i < chain.segmentCount; i++)
                {
                    float gravScale = Math.Max(VertexGravityArray[i].Length() - average, 0);
                    Vector2 pos = (chain.ropeSegments[i].posNow - position);
                    _spriteBatch.Draw(circle, pos, null, Color.White * 0.25f, 0f, Vector2.One * 100, 0.25f * gravScale * gridscale, default, default);
                    _spriteBatch.DrawString(font_Arial, VertexGravityArray[i].Length().ToString(), pos, Color.DarkGoldenrod, 0f, default, gridscale, default, default);

                }
                _spriteBatch.End();
            }


            base.Draw(gameTime);
        }
        #endregion
    }
}
