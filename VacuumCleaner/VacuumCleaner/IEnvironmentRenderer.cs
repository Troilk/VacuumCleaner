using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VacuumCleaner.Env;

namespace VacuumCleaner
{
    interface IEnvironmentRenderer
    {
        string Name { get; }

        void Render(SpriteBatch spriteBatch, Environment env, GameTime time, Rectangle mapDest, float posLerp);
        void Update(GameTime gameTime);
    }
}
