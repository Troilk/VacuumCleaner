using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace VacuumCleaner.Env
{
    public partial class Environment
    {
        public class TileMap2D : IEnvironmentRenderer
        {
            #region Declarations

            private const float MAX_DIRT = 50.0f;

            public string Name { get { return "TileMap2D"; } }

            private Texture2D WallTex;
            private Texture2D FloorTex;
            private Texture2D DirtTex;
            private Texture2D CleanerTex;

            #endregion

            #region Methods

            public TileMap2D(ContentManager content)
            {
                WallTex = content.Load<Texture2D>("Textures/TileMap2D/WallTex");
                FloorTex = content.Load<Texture2D>("Textures/TileMap2D/FloorTex");
                DirtTex = content.Load<Texture2D>("Textures/TileMap2D/DirtTex");
                CleanerTex = content.Load<Texture2D>("Textures/TileMap2D/CleanerTex");
            }

            public void Render(SpriteBatch spriteBatch, Environment env, GameTime time, Rectangle mapDest, float posLerp)
            {
                int tile;
                Rectangle destination;
                int x = mapDest.X, y = mapDest.Y;
                int tileWidth = mapDest.Width / Environment.mazeSize_;
                int tileHeight = mapDest.Height / Environment.mazeSize_;

                for (int i = 0; i < Environment.mazeSize_; ++i)
                    for (int j = 0; j < Environment.mazeSize_; ++j)
                    {
                        tile = env.maze_[i][j];
                        destination = new Rectangle(x + j * tileWidth, y + i * tileHeight, tileWidth, tileHeight);
                        if (tile == -1)
                            spriteBatch.Draw(WallTex, destination, Color.White);
                        else
                        {
                            spriteBatch.Draw(FloorTex, destination, Color.White);
                            spriteBatch.Draw(DirtTex, destination, Color.Lerp(Color.Transparent, Color.White, tile / MAX_DIRT));
                        }
                    }

                destination = new Rectangle(x + env.agentPosY_ * tileWidth, y + env.agentPosX_ * tileHeight,
                    tileWidth, tileHeight);
                
                switch (env.preAction_)
                {
                    case ActionType.actIDLE:
                        spriteBatch.Draw(CleanerTex, destination, Color.Yellow);
                        break;
                    case ActionType.actSUCK:
                        spriteBatch.Draw(CleanerTex, destination, Color.Green);
                        break;
                    case ActionType.actUP:
                        spriteBatch.Draw(CleanerTex, destination, env.isJustBump ? Color.Red : Color.White);
                        break;
                    case ActionType.actRIGHT:
                        destination.Offset(tileWidth, 0);
                        
                        spriteBatch.Draw(CleanerTex, destination, null, env.isJustBump ? Color.Red : Color.White,
                            MathHelper.PiOver2, Vector2.Zero, SpriteEffects.None, 0f);
                        break;
                    case ActionType.actDOWN:
                        destination.Offset(tileWidth, tileHeight);
                        spriteBatch.Draw(CleanerTex, destination, null, env.isJustBump ? Color.Red : Color.White,
                            MathHelper.Pi, Vector2.Zero, SpriteEffects.None, 0f);
                        break;
                    case ActionType.actLEFT:
                        destination.Offset(0, tileHeight);       
                        spriteBatch.Draw(CleanerTex, destination, null, env.isJustBump ? Color.Red : Color.White,
                            -MathHelper.PiOver2, Vector2.Zero, SpriteEffects.None, 0f);
                        break;
                }
            }

            public void Update(GameTime gameTime) { }

            public override string ToString()
            {
                return Name;
            }

            #endregion
        }
    }
}
