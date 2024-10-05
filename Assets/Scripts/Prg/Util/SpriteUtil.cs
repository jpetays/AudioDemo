using UnityEngine;

namespace Prg.Util
{
    /// <summary>
    /// Extension methods for UNITY Sprites (<c>SpriteRenderer</c>).
    /// </summary>
    public static class SpriteUtil
    {
        public static SpriteRenderer[] FindSprites(this GameObject gameObject)
        {
            return gameObject.GetComponentsInChildren<SpriteRenderer>(true);
        }

        public static void SetEnabled(this SpriteRenderer[] sprites, bool isEnabled)
        {
            if (sprites.Length == 0)
            {
                return;
            }
            foreach (var sprite in sprites)
            {
                sprite.enabled = isEnabled;
            }
        }
    }
}
