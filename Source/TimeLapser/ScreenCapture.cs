using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using Harmony;

namespace TimeLapser
{
    class ScreenCapture
    {
        private static readonly System.DateTime Jan1st1970 = new System.DateTime
       (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static void TakeScreenshot()
        {
            var game = Game.Instance;

            var camera = new Traverse(game)
                .Field("cameraController")
                .GetValue<CameraController>()
                .baseCamera;

            var screenGrab = GenerateScreenGrab(GetCameraCopy(camera));
            SaveTextureToFile(screenGrab, String.Format("TimeLapse{0}", (System.DateTime.UtcNow - Jan1st1970).TotalMilliseconds));
        }

        private static Camera GetCameraCopy(Camera input)
        {
            //TODO
            return input;
        }

        private static void SaveTextureToFile(Texture2D texture, string fileName)
        {
            var bytes = ImageConversion.EncodeToPNG(texture);
            var file = File.Open(Application.dataPath + "/" + fileName, FileMode.Create);
            var binary = new BinaryWriter(file);
            binary.Write(bytes);
            file.Close();
        }

        private static Texture2D GenerateScreenGrab(Camera screen)
        {
            Debug.Log(" === TimeLapser GenerateScreenGrab 1");
            var oldTargetTexture = screen.targetTexture;

            Debug.Log(" === TimeLapser GenerateScreenGrab 2");
            RenderTexture newTexture = null;
            try
            {
                newTexture = new RenderTexture(screen.pixelWidth, screen.pixelHeight, 24);
            }catch(Exception e)
            {
                Debug.Log(" === TimeLapser GenerateScreenGrab error");
                Debug.LogException(e);
            }
            finally
            {
                Debug.Log(" === TimeLapser GenerateScreenGrab finally");
            }

            //newTexture.Create();
            Debug.Log(" === TimeLapser GenerateScreenGrab 3");
            screen.targetTexture = newTexture;

            screen.Render();

            var result = RenderTexToTex2D(newTexture);

            screen.targetTexture = oldTargetTexture;

            return result;
        }

        private static Texture2D RenderTexToTex2D(RenderTexture render)
        {
            // Remember currently active render texture
            RenderTexture currentActiveRT = RenderTexture.active;

            // Set the supplied RenderTexture as the active one
            RenderTexture.active = render;

            // Create a new Texture2D and read the RenderTexture image into it
            Texture2D tex = new Texture2D(render.width, render.height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

            // Restore previously active render texture
            RenderTexture.active = currentActiveRT;
            return tex;
        }
        
        // Taken from elsewhere, forgot the source
        public static Texture2D DuplicateTexture(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }
    }
}
