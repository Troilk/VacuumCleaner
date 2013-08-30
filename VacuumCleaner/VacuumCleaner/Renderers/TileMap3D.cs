using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace VacuumCleaner.Env
{
    public partial class Environment
    {
        private class Camera
        {
            #region Declarations

            private const float MIN_ANGLE = MathHelper.PiOver4 * 0.5f;
            private const float ZOOM_SPEED = 1 / 1000.0f;
            private const float SHIFT_SPEED = 1 / 50.0f;
            private const float ROTATION_SPEED = 1 / 200.0f;

            public Matrix View;
            public Matrix Proj;
            public Matrix ViewProj;

            private GraphicsDevice GraphicsDevice;
            private MouseState OldMouseST;
            private Vector3 Target = Vector3.Zero;
            private Vector3 TargetOffset = new Vector3(-10.0f, -10.0f, -10.0f);

            #endregion

            #region Methods

            public Camera(float aspectRatio, GraphicsDevice graphicsDevice)
            {
                this.GraphicsDevice = graphicsDevice;
                View = Matrix.CreateLookAt(Target - TargetOffset, Target, Vector3.Up);
                Proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.3f, 100.0f);
                Matrix.Multiply(ref View, ref Proj, out ViewProj);
                OldMouseST = Mouse.GetState();
            }

            public void AdjustAspectRatio(float newAspectRatio)
            {
                Proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, newAspectRatio, 0.3f, 100.0f);
                Matrix.Multiply(ref View, ref Proj, out ViewProj);
            }

            public void Zoom(float zoomAmount, int mousePosX, int mousePosY)
            {
                Vector3 zoomDir = GraphicsDevice.Viewport.Unproject(new Vector3(mousePosX, mousePosY, 0f),
                        Proj, View, Matrix.Identity) -
                            GraphicsDevice.Viewport.Unproject(new Vector3(mousePosX, mousePosY, 1f),
                                Proj, View, Matrix.Identity);

                zoomDir.Normalize();
                Target += zoomDir * (-zoomAmount);
            }

            public void Shift(float shiftX, float shiftY)
            {
                if (Vector3.Cross(TargetOffset, Vector3.Up) == Vector3.Zero)
                {
                    Target += new Vector3(shiftY, 0f, -shiftX);
                }
                else
                {
                    Vector3 targetOffsetNormal = TargetOffset;
                    targetOffsetNormal.Normalize();
                    Vector3 side = Vector3.Cross(targetOffsetNormal, Vector3.Up);
                    side.Normalize();
                    Target -= (side * shiftX) + (Vector3.Cross(targetOffsetNormal, side) * shiftY);
                }
            }

            public void Rotate(float rotX, float rotY)
            {
                if (rotY - (float)System.Math.Asin(TargetOffset.Y / TargetOffset.Length()) >= MathHelper.PiOver2)
                    return;
                if (Vector3.Cross(TargetOffset, Vector3.Up) == Vector3.Zero)
                    TargetOffset.X -= 0.0001f;
                Vector3 targetOffsetNormal = TargetOffset;
                targetOffsetNormal.Normalize();
                Vector3 side = Vector3.Cross(targetOffsetNormal, Vector3.Up);
                side.Normalize();

                Quaternion rot = Quaternion.CreateFromAxisAngle(side, -rotY);

                rot *= Quaternion.CreateFromAxisAngle(Vector3.Up, -rotX);
                TargetOffset = -Vector3.Transform(-TargetOffset, rot);
            }

            public void Update(float time)
            {
                MouseState mouseST = Mouse.GetState();
                KeyboardState keyST = Keyboard.GetState();
                int deltaMoveX = mouseST.X - OldMouseST.X;
                int deltaMoveY = mouseST.Y - OldMouseST.Y;
                int deltaScroll = mouseST.ScrollWheelValue - OldMouseST.ScrollWheelValue;
                

                if (keyST.IsKeyDown(Keys.LeftShift))
                {
                    if (deltaMoveX != 0 || deltaMoveY != 0)
                        Shift(deltaMoveX * SHIFT_SPEED, deltaMoveY * SHIFT_SPEED);
                }
                else
                    if (deltaScroll != 0)
                        Zoom(deltaScroll * ZOOM_SPEED, mouseST.X, mouseST.Y);
                    else
                        if (mouseST.LeftButton == ButtonState.Pressed && (deltaMoveX != 0 || deltaMoveY != 0))
                            Rotate(deltaMoveX * ROTATION_SPEED, deltaMoveY * ROTATION_SPEED);

                View = Matrix.CreateLookAt(Target - TargetOffset, Target, Vector3.Up);
                Matrix.Multiply(ref View, ref Proj, out ViewProj);

                OldMouseST = mouseST;
            }

            #endregion
        }

        public class TileMap3D : IEnvironmentRenderer
        {
            #region Declarations

            private const float MAX_DIRT = 1 / 50.0f;

            public string Name { get { return "TileMap3D"; } }

            private GraphicsDevice GraphicsDevice;
            private Model Wall;
            private Model FloorTile;
            private Model Cleaner;
            private Model Dirt;
            private Camera Camera;
            private DepthStencilState dss = new DepthStencilState();
            private RasterizerState rs = new RasterizerState();
            private SamplerState ss = new SamplerState();
            private Vector2 LastPos = new Vector2(float.NaN, float.NaN), CurPos;
            private float LastRot = 0f;

            #endregion

            #region Methods

            public TileMap3D(GraphicsDevice graphicsDevice, ContentManager contentManager, int width, int height)
            {
                this.GraphicsDevice = graphicsDevice;
                Wall = contentManager.Load<Model>("Models/TileMap3D/Wall");
                FloorTile = contentManager.Load<Model>("Models/TileMap3D/FloorTile");
                Dirt = contentManager.Load<Model>("Models/TileMap3D/Dirt");
                Cleaner = contentManager.Load<Model>("Models/TileMap3D/Cleaner");

                ((BasicEffect)Wall.Meshes[0].Effects[0]).EnableDefaultLighting();
                ((BasicEffect)FloorTile.Meshes[0].Effects[0]).EnableDefaultLighting();
                ((BasicEffect)Dirt.Meshes[0].Effects[0]).EnableDefaultLighting();
                ((BasicEffect)Cleaner.Meshes[0].Effects[0]).EnableDefaultLighting();
                foreach (BasicEffect effect in Cleaner.Meshes[0].Effects)
                    effect.Tag = effect.DiffuseColor;
                foreach (BasicEffect effect in FloorTile.Meshes[0].Effects)
                    effect.Tag = effect.DiffuseColor;

                dss.DepthBufferEnable = true;
                dss.DepthBufferFunction = CompareFunction.LessEqual;
                dss.DepthBufferWriteEnable = true;
                rs.CullMode = CullMode.CullCounterClockwiseFace;
                ss.AddressU = TextureAddressMode.Wrap;
                ss.AddressV = TextureAddressMode.Wrap;
                ss.Filter = TextureFilter.Anisotropic;

                Camera = new Camera(graphicsDevice.Viewport.AspectRatio, graphicsDevice);
            }

            public void AdjustAspectRatio(int width, int height)
            {
                Camera.AdjustAspectRatio((float)width / height);
            }

            public void Render(SpriteBatch spriteBatch, Environment env, GameTime time, Rectangle mapDest, float posLerp)
            {
                Vector2 currentPos = new Vector2(env.agentPosX_, env.agentPosY_);
                if (LastPos.X == float.NaN)
                {
                    LastPos = currentPos;
                    CurPos = LastPos;
                }
                if (posLerp == 0.0f)
                {
                    LastPos = CurPos;
                    CurPos = currentPos;
                }
                if (System.Math.Abs(LastPos.X - currentPos.X + LastPos.Y - currentPos.Y) > 1.5f)
                    posLerp = 1.0f;
                Vector2 lerpPos = Vector2.Lerp(LastPos, currentPos, posLerp);
                if (posLerp > 0.5f)
                     System.Console.WriteLine();

                int tile;
                float x = -Environment.mazeSize_ * 0.5f, z = -Environment.mazeSize_ * 0.5f;

                Matrix worldTransform = Matrix.Identity;
                Matrix wvp;

                ModelMesh wallMesh = Wall.Meshes[0];
                ModelMesh floorMesh = FloorTile.Meshes[0];
                ModelMesh dirtMesh = Dirt.Meshes[0];
                Effect wallEffect = wallMesh.Effects[0];
                BasicEffect floorEffect = (BasicEffect)floorMesh.Effects[0];
                Effect dirtEffect = dirtMesh.Effects[0];

                DepthStencilState oldDss = GraphicsDevice.DepthStencilState;
                RasterizerState oldRs = GraphicsDevice.RasterizerState;
                GraphicsDevice.DepthStencilState = dss;
                GraphicsDevice.RasterizerState = rs;
                GraphicsDevice.SamplerStates[0] = ss;

                for (int i = 0; i < Environment.mazeSize_; ++i)
                    for (int j = 0; j < Environment.mazeSize_; ++j)
                    {
                        tile = env.maze_[i][j];
                        worldTransform.M41 = x + i;
                        worldTransform.M42 = 0.0f;
                        worldTransform.M43 = z + j;
                        Matrix.Multiply(ref worldTransform, ref Camera.ViewProj, out wvp);
                        if (tile == -1)
                        {
                            wallEffect.Parameters["WorldViewProj"].SetValue(wvp);
                            wallMesh.Draw();
                        }
                        else
                        {
                            floorEffect.Parameters["WorldViewProj"].SetValue(wvp);
                            floorEffect.DiffuseColor = Vector3.Lerp((Vector3)floorEffect.Tag, Vector3.Zero, tile * MAX_DIRT); 
                            floorMesh.Draw();
                            ////worldTransform.M42 = MathHelper.Lerp(0.04f, 0.8f, tile * MAX_DIRT);
                            ////Matrix.Multiply(ref worldTransform, ref Camera.ViewProj, out wvp);
                            //dirtEffect.Parameters["WorldViewProj"].SetValue(wvp);
                            //dirtMesh.Draw();
                        }
                    }

                worldTransform.M41 = x + lerpPos.X;
                worldTransform.M42 = 0.0f;
                worldTransform.M43 = z + lerpPos.Y + 0.5f;
                float rotation = 0f;
                Color col = Color.White;
                
                switch (env.preAction_)
                {
                    case ActionType.actIDLE:
                        rotation = LastRot;
                        col = Color.Yellow;
                        break;
                    case ActionType.actSUCK:
                        col = Color.Green;
                        break;
                    case ActionType.actUP:
                        if (env.isJustBump)
                            col = Color.Red;
                        break;
                    case ActionType.actRIGHT:
                        if (env.isJustBump)
                            col = Color.Red;
                        rotation = MathHelper.PiOver2;
                        break;
                    case ActionType.actDOWN:
                        if (env.isJustBump)
                            col = Color.Red;
                        rotation = MathHelper.Pi;
                        break;
                    case ActionType.actLEFT:
                        if (env.isJustBump)
                            col = Color.Red;
                        rotation = -MathHelper.PiOver2;
                        break;
                }

                var val1 = (float)System.Math.Cos(rotation);
                var val2 = (float)System.Math.Sin(rotation);

                worldTransform.M11 = val1;
                worldTransform.M13 = -val2;
                worldTransform.M31 = val2;
                worldTransform.M33 = val1;
                ModelMesh cleanerMesh = Cleaner.Meshes[0];
                Matrix.Multiply(ref worldTransform, ref Camera.ViewProj, out wvp);
                foreach (BasicEffect effect in cleanerMesh.Effects)
                {
                    effect.DiffuseColor = (Vector3)effect.Tag * col.ToVector3();
                    effect.Parameters["WorldViewProj"].SetValue(wvp);
                }
                cleanerMesh.Draw();

                GraphicsDevice.DepthStencilState = oldDss;
                GraphicsDevice.RasterizerState = oldRs;
            }

            public void Update(GameTime gameTime) 
            {
                Camera.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            public override string ToString()
            {
                return Name;
            }

            #endregion
        }
    }
}
