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
using static VerletChainAlgo.HelperMethods;

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
        public Texture2D pixel;
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
        public static Vector2[] VertexGravityArray = null;//stores current grav values

        //public Vector2[] SettledPointArray = null;
        public int facingDirection = 1; //1, -1
        #endregion

        public static Vector2[] NextWantedVertexGravityArray;
        public static Vector2[] SettledPoints;

        protected override void Initialize()
        {
            Window.AllowUserResizing = true;
            viewSize = originalViewSize = new Vector2(_graphics.GraphicsDevice.Viewport.Width, _graphics.GraphicsDevice.Viewport.Height);
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
            VertexGravityArray = Enumerable.Repeat(new Vector2(1, -1), info.vertexCount).ToArray();
            VertexGravityArray[0] = Vector2.Zero;

            bool distances = info.VertexDistanceArray != null;
            bool gravities = VertexGravityArray != null;

            spineBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), info.vertexCount, BufferUsage.WriteOnly);
            shapeBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), info.vertexCount, BufferUsage.WriteOnly);

            //if (SettledPointArray != null)
            //    settledPoints = SettledPointArray;
            //else
            //{
            SettledPoints = new Vector2[info.vertexCount];
                for (int i = 0; i < info.vertexCount; i++)
                {
                    float dist = distances ? info.VertexDistanceArray[i] : info.vertexDefaultDistance;
                    Vector2 grav = VertexGravityArray[i];
                    SettledPoints[i] = (grav * dist) + (i > 0 ? SettledPoints[i - 1] : Vector2.Zero);
                }
            //}


            chain = new VerletChainInstance(info.vertexCount, info.ChainOffset,
                ((SettledPoints[info.vertexCount - 1]) * facingDirection), info.vertexDefaultDistance, null,
                gravities, VertexGravityArray?.ToList(), distances, info.VertexDistanceArray?.ToList())
            {
                drag = info.VertexDrag,
                constraintRepetitions = info.PhysicsRepetitions
            };

            SetShapeBuffer(info.WantedVertexPoints, info.ChainOffset);
            SetTextureGeometryBuffer(info);

            NextWantedVertexGravityArray = Enumerable.Repeat(Vector2.Zero, info.vertexCount).ToArray();
        }

        public Vector2[] texCoordR;
        int[] indices;
        public VertexBuffer geometryBuffer;
        public IndexBuffer geometryIndexBuffer;
        public void SetTextureGeometryBuffer(ChainInfo info)
        {
            //removed dedserv checks since this is all wrapped in one
            geometryBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColorTexture), ((info.vertexCount - 1) * 3) + 2, BufferUsage.WriteOnly);
            geometryIndexBuffer = new IndexBuffer(_graphics.GraphicsDevice, typeof(short), (info.vertexCount - 1) * 9, BufferUsage.WriteOnly);

            #region index buffer
            indices = new int[geometryIndexBuffer.IndexCount];
            for (int i = 0; i < indices.Length / 3; i++)
            {
                int index = i * 3;
                switch (i % 3)
                {
                    case 0:
                        indices[index] = i;
                        indices[index + 1] = i + 1;
                        indices[index + 2] = i + 2;
                        break;
                    case 1:
                        indices[index] = i;
                        indices[index + 2] = i + 1;
                        indices[index + 1] = i + 3;
                        break;
                    case 2:
                        indices[index] = i - 2;
                        indices[index + 2] = i + 1;
                        indices[index + 1] = i;
                        break;
                }
            }

            geometryIndexBuffer.SetData(indices);
            #endregion

            #region tex coords
            Vector2 largestSize = Vector2.Zero;
            Vector2 smallestSize = Vector2.One * 65536;
            foreach (Vector2 vec in SettledPoints)
            {
                if (Math.Abs(vec.X) > largestSize.X)
                    largestSize.X = vec.X;
                if (vec.X < smallestSize.X)
                    smallestSize.X = vec.X;

                if (Math.Abs(vec.Y) > largestSize.Y)
                    largestSize.Y = vec.Y;
                if (-vec.Y < smallestSize.Y)
                    smallestSize.Y = -vec.Y;
            }


            largestSize += CurrentInfo.TailSpriteSize * new Vector2(1, -1);//tailBase.TexSizeOffset * new Vector2(1, -1);
            smallestSize += CurrentInfo.TailSpriteOffset * new Vector2(1, -1);//tailBase.TexPosOffset * new Vector2(1, -1);

            texCoordR = TexCoords(info, 1, SettledPoints, smallestSize, largestSize);
            #endregion
        }

        private Vector2[] TexCoords(ChainInfo info, int dir, Vector2[] settledPoints, Vector2 smallestSize, Vector2 largestSize)
        {
            Vector2[] texCoords = new Vector2[((info.vertexCount - 1) * 3) + 2];

            Vector2 TexCoordConvert(Vector2 realPosition)
            {
                Vector2 diff = (realPosition - smallestSize) - (new Vector2(0, info.Width));
                Vector2 size = diff / (largestSize - (new Vector2(0, info.Width) * 2));
                return (size * new Vector2(1, -1)) + new Vector2(0, 1);
            }
            
            texCoords[2] = TexCoordConvert(settledPoints[1]);

            texCoords[1] = TexCoordConvert(settledPoints[0] + (Vector2.UnitY * info.Width * -dir));
            texCoords[0] = TexCoordConvert(settledPoints[0] + (Vector2.UnitY * info.Width * dir));

            for (int i = 1; i < info.vertexCount - 1; i++)//sets all vertexes besides the last two
            {
                int index = i * 3;

                texCoords[index + 2] = TexCoordConvert(settledPoints[i + 1]);

                float directionRot = (settledPoints[i + 1] - settledPoints[i]).ToRotation() + ((float)Math.PI * 0.5f);

                Vector2 segLocationA = settledPoints[i] + (Vector2.UnitY.RotatedBy(directionRot + (Math.PI * 0.5f * dir)) * info.Width);
                texCoords[index + 1] = TexCoordConvert(segLocationA);

                Vector2 segLocationB = settledPoints[i] + (Vector2.UnitY.RotatedBy(directionRot - (Math.PI * 0.5f * dir)) * info.Width);
                texCoords[index] = TexCoordConvert(segLocationB);
            }


            //last two vert
            float directionRot2 = (settledPoints[info.vertexCount - 2] - settledPoints[info.vertexCount - 1]).ToRotation() + ((float)Math.PI * 0.5f);

            Vector2 segLocation2A = settledPoints[info.vertexCount - 1] + (Vector2.UnitY.RotatedBy(directionRot2 + (Math.PI * 0.5f * dir)) * info.Width);
            texCoords[geometryBuffer.VertexCount - 2] = TexCoordConvert(segLocation2A);

            Vector2 segLocation2B = settledPoints[info.vertexCount - 1] + (Vector2.UnitY.RotatedBy(directionRot2 - (Math.PI * 0.5f * dir)) * info.Width);
            texCoords[geometryBuffer.VertexCount - 1] = TexCoordConvert(segLocation2B);

            return texCoords;
        }

        

        public void SetShapeBuffer(Vector2[] RelativePoints, Vector2 offset)
        {
            VertexPositionColor[] vertexPos = new VertexPositionColor[shapeBuffer.VertexCount];
            Vector2 combinedPos = Vector2.Zero;

            for (int i = 0; i < vertexPos.Length; i++)
            {
                combinedPos += RelativePoints[i];
                vertexPos[i] = new VertexPositionColor(new Vector3(combinedPos + offset, 0), Color.Red);
            }

            shapeBuffer.SetData(vertexPos);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            grid = Content.Load<Texture2D>("Grid");
            circle = Content.Load<Texture2D>("Circle");
            pixel = Content.Load<Texture2D>("Pixel");
            blank = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            blank.SetData(new Color[] { Color.White });
            font_Arial = Content.Load<SpriteFont>("Arial");

            //SetupChain();
        }

        #region updating

        public float lastClosenessValue = float.MaxValue - 1;
        public float closenessValue = float.MaxValue;

        public bool paused = false;

        public int SelectedUiValueIndex = 0;

        public bool selectedSide = false;

        public bool SnapToGrid = false;

        public int SelectedChainIndex = -1;

        public MouseState mouseState = Mouse.GetState();
        public MouseState lastMouseState = Mouse.GetState();
        public KeyboardState lastKeyState = Keyboard.GetState();
        public KeyboardState keyboardState = Keyboard.GetState();

        public bool ButtonPressed(Keys key) =>
            keyboardState.IsKeyDown(key) && !lastKeyState.IsKeyDown(key);

        public bool LeftMousePressed() =>
            mouseState.LeftButton == ButtonState.Pressed && !(lastMouseState.LeftButton == ButtonState.Pressed);

        public bool RightMousePressed() =>
            mouseState.RightButton == ButtonState.Pressed && !(lastMouseState.RightButton == ButtonState.Pressed);

        public List<VertexOverride> CurrentOverrides = new List<VertexOverride>();

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

            Vector2 nextMouseOffset = ((mouseState.Position - lastMouseState.Position).ToVector2() * new Vector2(1, -1)) / (CameraZoom * SimInfo.DefaultDrawScale);

            if (mouseState.MiddleButton == ButtonState.Pressed)
                CameraPosition += nextMouseOffset;

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
                        StartSimMode();
                    else if (ButtonPressed(Keys.Tab))
                        StartBuildMode();
                    break;

                case SimState.placement:
                    SnapToGrid = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
                    if (ButtonPressed(Keys.Enter))
                    {
                        MakeNewChainInfo();
                        StartSimMode();
                    }
                    if (ButtonPressed(Keys.Tab))
                    {
                        MakeNewChainInfo();
                        StartWaitingMode();
                    }
                    if (ButtonPressed(Keys.Escape))
                        StartWaitingMode();

                    if (LeftMousePressed())
                        PlacementList.Add(SnapToGrid ? RoundedSnapPos.ToVector2() : RoundedNormalPos);
                    if (RightMousePressed())
                        PlacementList.RemoveAt(PlacementList.Count - 1);
                    break;

                case SimState.running:
                    if (ButtonPressed(Keys.H))
                        ClipboardService.SetText(BuildOutputText(CurrentInfo));
                    if (ButtonPressed(Keys.Escape))
                        StartWaitingMode();
                    if (ButtonPressed(Keys.Space))
                        paused = !paused;
                    if ((!paused || ButtonPressed(Keys.U)) && chain != null)
                    {
                        SettledPoints = new Vector2[chain.ropeSegments.Count];
                        for (int i = 0; i < chain.ropeSegments.Count; i++)
                        {
                            SettledPoints[i] = chain.ropeSegments[i].posNow;
                        }
                        SetTextureGeometryBuffer(CurrentInfo);
                    }
                    if (ButtonPressed(Keys.I))
                    {
                        CurrentTexMode += 1;
                        if ((int)CurrentTexMode > 2)
                            CurrentTexMode = TextureMode.None;
                    }
                    if (!paused)
                    {
                        if (chain != null)
                        {
                            CurrentOverrides = CurrentInfo.ConstraintOverrides;
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
                        if (chain != null)
                        {
                            chain.UpdateChain();
                        }

                        if (LeftMousePressed())
                        {
                            int closestIndex = 0;
                            for (int i = 1; i < chain.ropeSegments.Count; i++)
                            {
                                if (Vector2.Distance(chain.ropeSegments[i].posNow, NormalPos) < Vector2.Distance(chain.ropeSegments[closestIndex].posNow, NormalPos))
                                    closestIndex = i;
                            }

                            const int MaxMouseRange = 2;
                            if (Vector2.Distance(chain.ropeSegments[closestIndex].posNow, NormalPos) < MaxMouseRange)
                                SelectedChainIndex = closestIndex;
                        }
                        if (RightMousePressed())
                            SelectedChainIndex = -1;

                        if (mouseState.LeftButton == ButtonState.Pressed)
                        {
                            if (SelectedChainIndex != -1)
                            {
                                chain.ropeSegments[SelectedChainIndex].posNow += nextMouseOffset * new Vector2(1, -1) * 0.33f;
                            }
                            else
                                for (int i = 1; i < chain.ropeSegments.Count; i++)
                                {
                                    chain.ropeSegments[i].posNow += nextMouseOffset * new Vector2(1, -1) * 0.1f;
                                }
                        }

                        if (ButtonPressed(Keys.Up))
                            SelectedUiValueIndex = SelectedUiValueIndex - 1 < 0 ? chain.segmentCount - 1 : SelectedUiValueIndex - 1;
                        else if (ButtonPressed(Keys.Down))
                            SelectedUiValueIndex = SelectedUiValueIndex + 1 > chain.segmentCount - 1 ? 0 : SelectedUiValueIndex + 1;
                        else if (ButtonPressed(Keys.Left) || ButtonPressed(Keys.Right))
                            selectedSide = !selectedSide;

                        if (ButtonPressed(Keys.Enter) || ButtonPressed(Keys.C))
                            if (selectedSide)
                                ClipboardService.SetText(CreateVector2Text(chain.ropeSegments[SelectedUiValueIndex].posNow));
                            else
                                ClipboardService.SetText(CreateVector2Text(VertexGravityArray[SelectedUiValueIndex]));
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
            OldPlacementListCount = 0;
            CurrentState = SimState.placement;
        }
        public void StartSimMode()
        {
            SetupChain(CurrentInfo);
            SelectedChainIndex = -1;
            CurrentState = SimState.running;
        }
        public void StartWaitingMode()
        {
            DeleteChain();
            OldPlacementListCount = 0;
            CurrentState = SimState.waiting;
        }

        public void MakeNewChainInfo()
        {
            if (PlacementList.Count < 2)
                return;
            
            ChainInfo newInfo = new ChainInfo()
            {
                //minimumGravStrength = CurrentInfo.minimumGravStrength,
                //VertexDrag = CurrentInfo.VertexDrag,
                //PhysicsRepetitions = CurrentInfo.PhysicsRepetitions,
                //VertexGravityMult = CurrentInfo.VertexGravityMult,
                ChainOffset = CurrentInfo.ChainOffset,

                vertexCount = PlacementList.Count,

                VertexDistanceArray = new float[PlacementList.Count],
                WantedVertexPoints = new Vector2[PlacementList.Count]
            };
            
            for (int i = 0; i < PlacementList.Count; i++)
            {
                newInfo.WantedVertexPoints[i] = PlacementList[i] - (i > 0 ? PlacementList[i - 1] : PlacementList[i]);
                newInfo.VertexDistanceArray[i] = ((i == PlacementList.Count - 1) ? 
                    newInfo.VertexDistanceArray[i - 1] : 
                    Vector2.Distance(PlacementList[i], PlacementList[i + 1])) * ChainInfo.TotalLengthMult;
            }

            newInfo.ChainOffset = PlacementList[0];

            CurrentInfo = newInfo;
        }

        public void DeleteChain()
        {
            chain = null;
            spineBuffer = null;
            shapeBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), 2, BufferUsage.WriteOnly);
            VertexGravityArray = null;
            SelectedUiValueIndex = 0;
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

            if(info.MinimumGravStrength > 0)
                for (int i = 0; i < info.vertexCount; i++)
                    if (NextWantedVertexGravityArray[i].Length() < info.MinimumGravStrength)
                        NextWantedVertexGravityArray[i] *= (1 + info.MinimumGravStrength) - NextWantedVertexGravityArray[i].Length();

            if (info.MaximumGravStrength > 0)
                for (int i = 0; i < info.vertexCount; i++)
                    if (NextWantedVertexGravityArray[i].Length() > info.MaximumGravStrength)
                        NextWantedVertexGravityArray[i] *= (1 + info.MaximumGravStrength) - NextWantedVertexGravityArray[i].Length();

            foreach (var curOverride in CurrentOverrides)
                if (curOverride.index < info.vertexCount)
                {
                    int i = curOverride.index;
                    if (curOverride.minStr != null && NextWantedVertexGravityArray[i].Length() < curOverride.minStr.Value)
                        NextWantedVertexGravityArray[i] *= (1 + curOverride.minStr.Value) - NextWantedVertexGravityArray[i].Length();

                    if (curOverride.maxStr != null && NextWantedVertexGravityArray[i].Length() > curOverride.maxStr.Value)
                        NextWantedVertexGravityArray[i] *= (1 + curOverride.maxStr.Value) - NextWantedVertexGravityArray[i].Length();

                    if(curOverride.setX != null)
                        NextWantedVertexGravityArray[i].X = curOverride.setX.Value;

                    if (curOverride.setY != null)
                        NextWantedVertexGravityArray[i].Y = curOverride.setY.Value;
                }

            if (info.MaxAverageStrength > 0)
            {
                float average = 0;
                for (int i = 0; i < info.vertexCount; i++)
                {
                    average += NextWantedVertexGravityArray[i].Length();
                }
                average /= (info.vertexCount - 1);

                if(average > info.MaxAverageStrength)
                {
                    for (int i = 0; i < info.vertexCount; i++)
                    {
                            NextWantedVertexGravityArray[i] *= (1 + info.MaxAverageStrength) - NextWantedVertexGravityArray[i].Length();
                    }
                }
            }

            //NextWantedVertexGravityArray[2] = new Vector2(3, 1f);
            //NextWantedVertexGravityArray[3] = new Vector2(3, 1f);
            //NextWantedVertexGravityArray[4] = new Vector2(3, 1f);

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

        public enum TextureMode
        {
            None,
            Texture,
            TexturePoints
        }

        public TextureMode CurrentTexMode = TextureMode.None;
        VertexPositionColorTexture[] vertexPos;
        private bool DrawTextureGeo = false;

        public void DrawGeometry(BasicEffect effect, EffectPass pass, GraphicsDevice graphicsDevice)
        {
            if (chain != null && spineBuffer != null)
            {
                VertexPositionColor[] vertexPos = new VertexPositionColor[spineBuffer.VertexCount];

                for (int i = 0; i < vertexPos.Length; i++)
                    vertexPos[i] = new VertexPositionColor(new Vector3(chain.ropeSegments[i].posNow, 0), SpineColor(i));

                spineBuffer.SetData(vertexPos);
            }
            //effect.TextureEnabled = false;//this or a second effect is needed
            effect.View = Matrix.CreateTranslation(new Vector3(EverythingDrawOffset * new Vector2(1, -1), 0)) * Matrix.CreateScale(EverythingDrawScale, -EverythingDrawScale, 1);

            RasterizerState rs = new RasterizerState
            {
                CullMode = CullMode.None
            };
            graphicsDevice.RasterizerState = rs;

            if (DrawTextureGeo = (CurrentState == SimState.running && (CurrentTexMode == TextureMode.TexturePoints || CurrentTexMode == TextureMode.Texture) && chain != null && geometryBuffer != null))
            {
                effect.TextureEnabled = true;
                pass.Apply();

                vertexPos = new VertexPositionColorTexture[geometryBuffer.VertexCount];

                Vector2 startLocation = chain.ropeSegments[0].posNow;
                Vector2[] texCor = texCoordR;

                vertexPos[2] = new VertexPositionColorTexture(new Vector3(chain.ropeSegments[1].posNow, 0), Color.White, texCor[2]);

                vertexPos[1] = new VertexPositionColorTexture(new Vector3(startLocation + (Vector2.UnitY * -1 * CurrentInfo.Width), 0), Color.White, texCor[1]);
                vertexPos[0] = new VertexPositionColorTexture(new Vector3(startLocation + (Vector2.UnitY * CurrentInfo.Width), 0), Color.White, texCor[0]);

                for (int i = 1; i < chain.segmentCount - 1; i++)//sets all vertexes besides the last two
                {
                    int index = i * 3;
                    vertexPos[index + 2] = new VertexPositionColorTexture(new Vector3(chain.ropeSegments[i + 1].posNow, 0), Color.White, texCor[index + 2]);

                    float directionRot = (chain.ropeSegments[i + 1].posNow - chain.ropeSegments[i].posNow).ToRotation() + (float)Math.PI * 0.5f;
                    Vector2 segLocation = chain.ropeSegments[i].posNow;
                    vertexPos[index + 1] = new VertexPositionColorTexture(new Vector3(segLocation + (Vector2.UnitY.RotatedBy(directionRot + (Math.PI * 0.5f)) * CurrentInfo.Width), 0), Color.White, texCor[index + 1]);
                    vertexPos[index] = new VertexPositionColorTexture(new Vector3(segLocation + (Vector2.UnitY.RotatedBy(directionRot - (Math.PI * 0.5f)) * CurrentInfo.Width), 0), Color.White, texCor[index]);
                }

                //last two vert
                float directionRot2 = (chain.ropeSegments[chain.segmentCount - 2].posNow - chain.ropeSegments[chain.segmentCount - 1].posNow).ToRotation() + (float)Math.PI * 0.5f;
                Vector2 segLocation2 = chain.ropeSegments[chain.segmentCount - 1].posNow;
                vertexPos[geometryBuffer.VertexCount - 2] = new VertexPositionColorTexture(new Vector3(segLocation2 + (Vector2.UnitY.RotatedBy(directionRot2 + (Math.PI * 0.5f)) * CurrentInfo.Width), 0), Color.White, texCor[geometryBuffer.VertexCount - 2]);
                vertexPos[geometryBuffer.VertexCount - 1] = new VertexPositionColorTexture(new Vector3(segLocation2 + (Vector2.UnitY.RotatedBy(directionRot2 - (Math.PI * 0.5f)) * CurrentInfo.Width), 0), Color.White, texCor[geometryBuffer.VertexCount - 1]);

                geometryBuffer.SetData(vertexPos);

                effect.TextureEnabled = true;
                effect.Texture = backTexture;
                
                graphicsDevice.SetVertexBuffer(geometryBuffer);
                graphicsDevice.Indices = geometryIndexBuffer;
                //graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geometryIndexBuffer.IndexCount / 3);
                graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertexPos, 0, vertexPos.Length, indices, 0, geometryIndexBuffer.IndexCount / 3);
            }

            effect.TextureEnabled = false;
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

        public Matrix SpriteMatrix = new Matrix();
        public Vector3 SpriteMatrixScale = Vector3.One;
        public Vector3 SpriteMatrixTranslation = Vector3.One;

        public void DrawTextCentered(SpriteBatch sb, SpriteFont font, string text, Vector2 center, Color color, float scale)
        {
            Vector2 textSize = font_Arial.MeasureString(text) * scale;
            Vector2 textPosition = new Vector2((center.X - (textSize.X / 2)), (center.Y - (textSize.Y / 2)));
            sb.DrawString(font, text, textPosition, color, 0f, default, scale, default, default);
        }

        public int OldPlacementListCount = 0;

        
        public Vector2 MouseRealPos => (MousePosition - new Vector2(SpriteMatrixTranslation.X, SpriteMatrixTranslation.Y));
        public Vector2 Round => new Vector2((float)Math.Round(MouseRealPos.X / SpriteMatrixScale.X) * SpriteMatrixScale.X, (float)Math.Round(MouseRealPos.Y / SpriteMatrixScale.Y) * SpriteMatrixScale.Y);
        public Vector2 Off2 => MouseRealPos - Round;

        public Vector2 SnapPos => Round / new Vector2(SpriteMatrixScale.X, SpriteMatrixScale.Y);
        public Point RoundedSnapPos => new Point((int)Math.Round(SnapPos.X), (int)Math.Round(SnapPos.Y));

        public Vector2 NormalPos => MouseRealPos / new Vector2(SpriteMatrixScale.X, SpriteMatrixScale.Y);
        public Vector2 RoundedNormalPos => new Vector2((float)Math.Round(NormalPos.X, 2), (float)Math.Round(NormalPos.Y, 2));

        public void AddSelectedChain(GameTime gameTime, bool mouse)
        {
            if (PlacementList.Count > (mouse ? 0 : 1))
            {
                if (shapeBuffer == null || OldPlacementListCount != PlacementList.Count)
                    shapeBuffer = new VertexBuffer(_graphics.GraphicsDevice, typeof(VertexPositionColor), PlacementList.Count + (mouse ? 1 : 0), BufferUsage.WriteOnly);

                VertexPositionColor[] vertexPos = new VertexPositionColor[shapeBuffer.VertexCount];

                for (int i = 0; i < PlacementList.Count; i++)
                {
                    vertexPos[i] = new VertexPositionColor(new Vector3(PlacementList[i], 0), Color.Red);
                }

                if (mouse)
                    vertexPos[PlacementList.Count] = new VertexPositionColor(new Vector3(SnapToGrid ? RoundedSnapPos.ToVector2() : RoundedNormalPos, 0), Color.Lerp(Color.White, Color.Green, ((float)Math.Sin(gameTime.TotalGameTime.Ticks / 2000000f) + 1) / 2));

                OldPlacementListCount = PlacementList.Count;
                shapeBuffer.SetData(vertexPos);
            }
            else
                shapeBuffer = null;
        }

        protected override void Draw(GameTime gameTime)
        { 
            GraphicsDevice.Clear(new Color(41, 43, 47));
            const float spritescalefactor = 0.0625f;

            SpriteMatrix = Matrix.CreateTranslation(new Vector3(EverythingDrawOffset * new Vector2(1, -1), 0)) * Matrix.CreateScale(EverythingDrawScale, EverythingDrawScale, 1) * Matrix.CreateTranslation(viewSize.X / 2, viewSize.Y / 2, 0);
            SpriteMatrix.Decompose(out Vector3 scale, out Quaternion qu, out Vector3 transl);
            SpriteMatrixScale = scale;
            SpriteMatrixTranslation = transl;

            if (SimInfo.DrawSprite)
            {
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, SpriteMatrix);
                _spriteBatch.Draw(backTexture, new Vector2(0, 0) - new Vector2(0, (backTexture.Height * SimInfo.BackSpriteScale)) - (SimInfo.BackSpriteOffset), null, new Color(150, 150, 150), 0f, default, SimInfo.BackSpriteScale, SpriteEffects.None, default);
                _spriteBatch.End();
            }

            float gridHeight = grid.Height * spritescalefactor;
            float gridWidth = grid.Width * spritescalefactor;
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, SpriteMatrix);
            _spriteBatch.Draw(grid, new Vector2(0, -gridHeight), null, new Color(74, 75, 79), 0f, default, spritescalefactor, SpriteEffects.FlipVertically, default);
            _spriteBatch.Draw(grid, new Vector2(0, 0), null, new Color(49, 48, 52), 0f, default, spritescalefactor, SpriteEffects.FlipVertically, default);
            _spriteBatch.Draw(grid, new Vector2(-gridWidth, -gridHeight), null, new Color(39, 38, 32), 0f, default, spritescalefactor, SpriteEffects.FlipVertically, default);
            _spriteBatch.Draw(grid, new Vector2(-gridWidth, 0), null, new Color(39, 38, 32), 0f, default, spritescalefactor, SpriteEffects.FlipVertically, default);
            _spriteBatch.DrawString(font_Arial, "(0, 0)", new Vector2(0, 0), Color.White, 0f, default, spritescalefactor * 0.75f, default, default);
            _spriteBatch.End();

            switch (CurrentState)
            {
                case SimState.running:
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null);
                    _spriteBatch.DrawString(font_Arial, "Closeness: " + closenessValue.ToString(), new Vector2(5, 5), Color.White);
                    for (int i = 0; i < chain.segmentCount; i++)
                    {
                        _spriteBatch.DrawString(font_Arial, "Grav " + i.ToString() + " : " + VertexGravityArray[i].ToString(), new Vector2(5, 30 + (i * 20)), Color.White * 0.8f);
                        _spriteBatch.DrawString(font_Arial, "Pos " + i.ToString() + " : " + (chain.ropeSegments[i].posNow).ToString(), new Vector2(viewSize.X - 265, 30 + (i * 20)), Color.White * 0.8f);
                    }
                    _spriteBatch.DrawString(font_Arial, "h: Copy all chain values", new Vector2(10, viewSize.Y - 60), Color.White * 0.8f);
                    _spriteBatch.DrawString(font_Arial, "i: Swap texture mode", new Vector2(10, viewSize.Y - 40), Color.White * 0.8f);
                    _spriteBatch.DrawString(font_Arial, "u: Update texture (Auto updated when running)", new Vector2(10, viewSize.Y - 20), Color.White * 0.8f);
                    _spriteBatch.End();

                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null);
                    if (paused)
                    {
                        if (!selectedSide)
                        {
                            Point size = font_Arial.MeasureString("Grav " + SelectedUiValueIndex.ToString() + " : " + VertexGravityArray[SelectedUiValueIndex].ToString()).ToPoint();
                            _spriteBatch.Draw(blank, new Rectangle(new Point(5, 30 + (SelectedUiValueIndex * 20)), size), Color.Yellow * 0.2f);
                        }
                        else
                        {
                            Point size = font_Arial.MeasureString("Pos " + SelectedUiValueIndex.ToString() + " : " + (chain.ropeSegments[SelectedUiValueIndex].posNow).ToString()).ToPoint();
                            _spriteBatch.Draw(blank, new Rectangle(new Point((int)(viewSize.X - 265), 30 + (SelectedUiValueIndex * 20)), size), Color.Yellow * 0.2f);
                        }
                        DrawTextCentered(_spriteBatch, font_Arial, "Press Enter to copy to clipboard", new Vector2(viewSize.X / 2, viewSize.Y - 45), Color.White * 0.50f, 1.1f);
                        DrawTextCentered(_spriteBatch, font_Arial, "Lclick to drag a vertex", new Vector2(viewSize.X / 2, viewSize.Y - 20), Color.White * 0.50f, 1.1f);
                        DrawTextCentered(_spriteBatch, font_Arial, "Paused", new Vector2(viewSize.X / 2, 25), Color.IndianRed * 0.50f, 2.2f);
                    }
                    _spriteBatch.End();
                    break;

                case SimState.waiting:
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null);
                    DrawTextCentered(_spriteBatch, font_Arial, "Press space to start/enter or tab to edit", new Vector2(viewSize.X / 2, 25), Color.IndianRed * 0.35f, 1.2f);
                    _spriteBatch.End();
                    AddSelectedChain(gameTime, false);
                    break;

                case SimState.placement:
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null);
                    //if (chain != null)
                    //{
                    //    for (int i = 0; i < 1; i++)
                    //    {
                    //        _spriteBatch.DrawString(font_Arial, "Target Pos " + i.ToString() + " : " + (chain.ropeSegments[i].posNow).ToString(), new Vector2(viewSize.X - 265, 30 + (i * 20)), Color.White * 0.8f);
                    //    }
                    //}

                    _spriteBatch.Draw(circle, SnapToGrid ? MousePosition - Off2 : new Vector2((float)Math.Round(MousePosition.X / scale.X, 2) * scale.X, (float)Math.Round(MousePosition.Y / scale.Y, 2) * scale.Y), null, Color.Green, 0f, new Vector2(circle.Width, circle.Height) / 2, 0.03f, default, default);
                    DrawTextCentered(_spriteBatch, font_Arial, "Placement Mode", new Vector2(viewSize.X / 2, 25), Color.IndianRed * 0.50f, 2f);
                    DrawTextCentered(_spriteBatch, font_Arial, "Hold shift to snap to grid, Lclick to place, Rclick to remove last", new Vector2(viewSize.X / 2, viewSize.Y - 40), Color.White * 0.50f, 1.333f);
                    DrawTextCentered(_spriteBatch, font_Arial, "Esc to return w/o saving, Tab to return & save, Enter to save & simulate", new Vector2(viewSize.X / 2, viewSize.Y - 20), Color.White * 0.50f, 1.333f);
                    _spriteBatch.DrawString(font_Arial, SnapToGrid ? RoundedSnapPos.ToString() : RoundedNormalPos.ToString(), MousePosition, Color.White * 0.75f, 0f, default, 1f, default, default); ;
                    _spriteBatch.End();

                    AddSelectedChain(gameTime, true);

                    break;
            }

            DrawGeometry(basicEffect, basicEffect.CurrentTechnique.Passes[0], _graphics.GraphicsDevice);


            if (CurrentState == SimState.running)
            {
                if (DrawTextureGeo && CurrentTexMode == TextureMode.TexturePoints)
                {
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, SpriteMatrix);
                    for (int i = 0; i < vertexPos.Length; i++)
                    {
                        _spriteBatch.Draw(circle, new Vector2(vertexPos[i].Position.X, vertexPos[i].Position.Y), null, new Color(70, 70, 70, 0), 0f, Vector2.One * 100, 0.0075f, default, default);
                        DrawTextCentered(_spriteBatch, font_Arial, i.ToString(), new Vector2(vertexPos[i].Position.X, vertexPos[i].Position.Y), Color.AliceBlue, 0.1f);
                    }
                    _spriteBatch.End();
                }


                Vector2 combinedGrav = Vector2.Zero;

                for (int i = 1; i < chain.segmentCount; i++)
                {
                    combinedGrav += VertexGravityArray[i];
                }

                float average = combinedGrav.Length() / (chain.segmentCount - 1);

                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, RasterizerState.CullNone, null, SpriteMatrix);
                
                for (int i = 1; i < chain.segmentCount; i++)
                {
                    Vector2 pos = (chain.ropeSegments[i].posNow);
                    _spriteBatch.Draw(pixel, pos, null, Color.Red * 0.5f, 0f, default, new Vector2(0.1f, VertexGravityArray[i].Y), default, default);
                    _spriteBatch.Draw(pixel, pos, null, Color.Blue * 0.5f, 0f, default, new Vector2(VertexGravityArray[i].X, 0.1f), default, default);
                }

                if (paused)
                {
                    if (SelectedChainIndex >= 0)
                    {
                        _spriteBatch.Draw(circle, chain.ropeSegments[SelectedChainIndex].posNow, null, Color.Yellow * 0.75f, 0f, Vector2.One * 100, 0.02f * spritescalefactor, default, default);
                    }
                }

                for (int i = 1; i < chain.segmentCount; i++)
                {
                    float gravScale = Math.Max(VertexGravityArray[i].Length() - average, 0);
                    Vector2 pos = (chain.ropeSegments[i].posNow);
                    _spriteBatch.Draw(circle, pos, null, Color.White * 0.25f, 0f, Vector2.One * 100, 0.25f * gravScale * spritescalefactor, default, default);
                    _spriteBatch.DrawString(font_Arial, VertexGravityArray[i].Length().ToString(), pos, Color.DarkGoldenrod, 0f, default, spritescalefactor, default, default);

                }
                _spriteBatch.End();
                DrawTextureGeo = false;
            }


            base.Draw(gameTime);
        }
        #endregion
    }
}
