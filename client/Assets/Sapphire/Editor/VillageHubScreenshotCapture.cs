using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Headful (non-batchmode) diagnostic tool. Opens VillageHub.unity directly -
    /// bypassing Login -> CharacterSelect/Create entirely (CharacterSessionService
    /// lazily creates a default session if none exists, per SceneComposer.cs) -
    /// enters Play mode, waits for rendering to settle, captures a PNG of the full
    /// composited Game view (camera + ScreenSpaceOverlay UI canvas, see
    /// VillageHubUiBuilder.cs), then exits Play mode and (when CLI-invoked) the
    /// Unity process.
    ///
    /// Must be run WITHOUT -batchmode: batchmode does not render frames, so
    /// ScreenCapture.CaptureScreenshot would produce nothing.
    ///
    /// CLI usage (headful, requires a display):
    ///   "<UnityEditor>/Unity.exe" -projectPath "<ROOT>\client"
    ///     -executeMethod Sapphire.EditorTools.VillageHubScreenshotCapture.CaptureFromCli
    ///     -logFile villagehub_shot.log
    ///
    /// The pending/exit flags live in SessionState (not a static field) because
    /// entering Play mode triggers a domain reload by default, which would wipe
    /// a plain static field before the wait-for-frames callback ever fires.
    /// </summary>
    [InitializeOnLoad]
    public static class VillageHubScreenshotCapture
    {
        private const string ScenePath = "Assets/Sapphire/Scenes/VillageHub.unity";
        private const string PendingKey = "VillageHubScreenshotCapture_Pending";
        private const string ExitAfterKey = "VillageHubScreenshotCapture_ExitAfter";
        private const string FrameCountKey = "VillageHubScreenshotCapture_FrameCount";
        private const string ShotTakenKey = "VillageHubScreenshotCapture_ShotTaken";
        private const string FullMapKey = "VillageHubScreenshotCapture_FullMap";
        private const int FramesToWaitBeforeShot = 60;
        private const int FramesToWaitAfterShot = 5;

        static VillageHubScreenshotCapture()
        {
            if (SessionState.GetBool(PendingKey, false))
            {
                EditorApplication.update -= OnUpdate;
                EditorApplication.update += OnUpdate;
            }
        }

        [MenuItem("Sapphire/Diagnostics/Capture VillageHub Screenshot")]
        public static void CaptureFromMenu()
        {
            BeginCapture(exitEditorAfter: false, fullMap: false);
        }

        [MenuItem("Sapphire/Diagnostics/Capture VillageHub Full Map")]
        public static void CaptureFullMapFromMenu()
        {
            BeginCapture(exitEditorAfter: false, fullMap: true);
        }

        // -executeMethod entry point. Run headful (no -batchmode).
        public static void CaptureFromCli()
        {
            BeginCapture(exitEditorAfter: true, fullMap: false);
        }

        /// <summary>
        /// Same flow as <see cref="CaptureFromCli"/>, but just before the shot is taken this
        /// disables the scene's CameraFollowRig (so its LateUpdate can't re-clamp/re-follow
        /// on top of us) and force-places the camera at the map center with an
        /// orthographicSize wide enough to fit the whole 32x32 grid, for a one-off top-down
        /// diagnostic of the entire map layout. This does NOT touch CameraFollowRig.cs or any
        /// gameplay camera behavior - the override lives here, only for this capture, and the
        /// component is merely disabled (not deleted) so tearing down Play mode restores it.
        /// </summary>
        public static void CaptureFullMap()
        {
            BeginCapture(exitEditorAfter: true, fullMap: true);
        }

        private static void BeginCapture(bool exitEditorAfter, bool fullMap)
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(ExitAfterKey, exitEditorAfter);
            SessionState.SetInt(FrameCountKey, 0);
            SessionState.SetBool(ShotTakenKey, false);
            SessionState.SetBool(FullMapKey, fullMap);

            EditorSceneManager.OpenScene(ScenePath);
            ForceGameViewVisible();

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            EditorApplication.isPlaying = true;
        }

        private static void ForceGameViewVisible()
        {
            // ScreenCapture.CaptureScreenshot reads the Game view's render target;
            // if no Game view tab is open/visible it captures nothing. This opens
            // and focuses the Game view via the Editor's own window API - not OS
            // input simulation.
            Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null)
            {
                return;
            }

            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            gameView.Show();
            gameView.Focus();
        }

        private static void OnUpdate()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                EditorApplication.update -= OnUpdate;
                return;
            }

            if (!EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.isCompiling)
            {
                return;
            }

            if (SessionState.GetBool(FullMapKey, false))
            {
                // Re-applied every frame (not just once) so nothing - including
                // CameraFollowRig re-enabling itself via some other path - can sneak a
                // frame of the normal follow/clamp position into the recorded frames.
                ApplyFullMapCameraOverride();
            }

            bool shotTaken = SessionState.GetBool(ShotTakenKey, false);
            int frames = SessionState.GetInt(FrameCountKey, 0) + 1;
            SessionState.SetInt(FrameCountKey, frames);

            if (!shotTaken)
            {
                if (frames < FramesToWaitBeforeShot)
                {
                    return;
                }

                SaveScreenshot();
                SessionState.SetBool(ShotTakenKey, true);
                SessionState.SetInt(FrameCountKey, 0);
                return;
            }

            // Give ScreenCapture a few extra frames to finish its async write
            // before we tear down Play mode / the process.
            if (frames < FramesToWaitAfterShot)
            {
                return;
            }

            EditorApplication.update -= OnUpdate;
            SessionState.SetBool(PendingKey, false);

            bool exitAfter = SessionState.GetBool(ExitAfterKey, false);
            EditorApplication.isPlaying = false;

            if (exitAfter)
            {
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }

        /// <summary>
        /// Disables the scene's CameraFollowRig for this Play session and hard-places the
        /// main camera at the map center with an orthographicSize wide enough to fit the
        /// full MapWidth x MapHeight grid (whichever axis is tighter given the Game view's
        /// current aspect ratio), plus a 5% margin so border fences aren't clipped at the
        /// very edge of frame.
        /// </summary>
        private static void ApplyFullMapCameraOverride()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                return;
            }

            var followRig = cam.GetComponent<Sapphire.Presentation.Camera.CameraFollowRig>();
            if (followRig != null)
            {
                followRig.enabled = false;
            }

            float mapWidth = SapphireSceneBuilder.MapWidth;
            float mapHeight = SapphireSceneBuilder.MapHeight;

            float halfSizeToFitWidth = (mapWidth * 0.5f) / cam.aspect;
            float halfSizeToFitHeight = mapHeight * 0.5f;
            cam.orthographicSize = Mathf.Max(halfSizeToFitWidth, halfSizeToFitHeight) * 1.05f;

            Vector3 pos = cam.transform.position;
            cam.transform.position = new Vector3(mapWidth * 0.5f, mapHeight * 0.5f, pos.z);
        }

        private static void SaveScreenshot()
        {
            string outputDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../generated-images/diagnostics"));
            Directory.CreateDirectory(outputDir);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            bool fullMap = SessionState.GetBool(FullMapKey, false);
            string fileTag = fullMap ? "fullmap" : "review";
            string path = Path.Combine(outputDir, $"villagehub_{fileTag}_{timestamp}.png");

            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("VILLAGEHUB_SCREENSHOT SUCCESS path=" + path);
        }
    }
}
